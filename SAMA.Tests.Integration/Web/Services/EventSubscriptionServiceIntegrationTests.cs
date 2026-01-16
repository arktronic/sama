using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services;
using SAMA.Web.Services.NotificationChannels;

namespace SAMA.Tests.Integration.Web.Services;

[TestClass]
public class EventSubscriptionServiceIntegrationTests : IntegrationTestBase
{
    private EventSubscriptionService _service = null!;
    private ServiceProvider _testServiceProvider = null!;
    private INotificationChannelHandler _mockHandler = null!;

    [TestInitialize]
    public override async Task InitializeTestAsync()
    {
        await base.InitializeTestAsync();

        _mockHandler = Substitute.For<INotificationChannelHandler>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton("Email", _mockHandler);
        services.AddKeyedSingleton("Slack", _mockHandler);
        services.AddKeyedSingleton("Teams", _mockHandler);

        _testServiceProvider = services.BuildServiceProvider();

        var logger = Substitute.For<ILogger<EventSubscriptionService>>();
        _service = new EventSubscriptionService(DbContext, _testServiceProvider, logger);
    }

    [TestCleanup]
    public override async Task CleanupTestAsync()
    {
        await _testServiceProvider.DisposeAsync();
        await base.CleanupTestAsync();
    }

    [TestMethod]
    public async Task TriggerLifecycleEventAsyncShouldNotSendWhenNoSubscriptions()
    {
        var workspace = await CreateWorkspaceAsync();
        var context = new LifecycleEventContext
        {
            EventType = EventTypes.CheckCreated,
            CheckId = Guid.NewGuid(),
            CheckName = "Test Check",
            CheckType = "Http",
            WorkspaceName = workspace.Name,
            Timestamp = DateTimeOffset.UtcNow,
            PerformedBy = "admin"
        };

        await _service.TriggerLifecycleEventAsync(workspace.Id, context);

        await _mockHandler.DidNotReceive().SendLifecycleEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Any<LifecycleEventContext>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TriggerLifecycleEventAsyncShouldSendToSubscribedChannels()
    {
        var workspace = await CreateWorkspaceAsync();
        var channel = await CreateNotificationChannelAsync(workspace.Id, "Test Channel", "Email");
        await CreateEventSubscriptionAsync(channel.Id, EventTypes.CheckCreated);

        _mockHandler.SendLifecycleEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Any<LifecycleEventContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new NotificationResultModel { Success = true, SentAt = DateTimeOffset.UtcNow });

        var context = new LifecycleEventContext
        {
            EventType = EventTypes.CheckCreated,
            CheckId = Guid.NewGuid(),
            CheckName = "Test Check",
            CheckType = "Http",
            WorkspaceName = workspace.Name,
            Timestamp = DateTimeOffset.UtcNow,
            PerformedBy = "admin"
        };

        await _service.TriggerLifecycleEventAsync(workspace.Id, context);

        await _mockHandler.Received(1).SendLifecycleEventAsync(
            Arg.Is<NotificationChannel>(nc => nc.Id == channel.Id),
            Arg.Is<LifecycleEventContext>(ctx => ctx.EventType == EventTypes.CheckCreated),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TriggerLifecycleEventAsyncShouldSendToMultipleChannels()
    {
        var workspace = await CreateWorkspaceAsync();
        var channel1 = await CreateNotificationChannelAsync(workspace.Id, "Channel 1", "Email");
        var channel2 = await CreateNotificationChannelAsync(workspace.Id, "Channel 2", "Slack");
        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckDeleted);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckDeleted);

        _mockHandler.SendLifecycleEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Any<LifecycleEventContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new NotificationResultModel { Success = true, SentAt = DateTimeOffset.UtcNow });

        var context = new LifecycleEventContext
        {
            EventType = EventTypes.CheckDeleted,
            CheckId = Guid.NewGuid(),
            CheckName = "Test Check",
            CheckType = "Http",
            WorkspaceName = workspace.Name,
            Timestamp = DateTimeOffset.UtcNow,
            PerformedBy = "admin"
        };

        await _service.TriggerLifecycleEventAsync(workspace.Id, context);

        await _mockHandler.Received(2).SendLifecycleEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Is<LifecycleEventContext>(ctx => ctx.EventType == EventTypes.CheckDeleted),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TriggerStatusChangeEventAsyncShouldNotSendWhenNoSubscriptions()
    {
        var workspace = await CreateWorkspaceAsync();
        var context = new StatusChangeEventContext
        {
            CheckId = Guid.NewGuid(),
            CheckName = "Test Check",
            WorkspaceName = workspace.Name,
            PreviousStatus = "Up",
            NewStatus = "Down",
            Timestamp = DateTimeOffset.UtcNow
        };

        await _service.TriggerStatusChangeEventAsync(workspace.Id, context);

        await _mockHandler.DidNotReceive().SendStatusChangeEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Any<StatusChangeEventContext>(),
            Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task TriggerStatusChangeEventAsyncShouldSendToSubscribedChannels()
    {
        var workspace = await CreateWorkspaceAsync();
        var channel = await CreateNotificationChannelAsync(workspace.Id, "Test Channel", "Teams");
        await CreateEventSubscriptionAsync(channel.Id, EventTypes.CheckStatusChanged);

        _mockHandler.SendStatusChangeEventAsync(
            Arg.Any<NotificationChannel>(),
            Arg.Any<StatusChangeEventContext>(),
            Arg.Any<CancellationToken>())
            .Returns(new NotificationResultModel { Success = true, SentAt = DateTimeOffset.UtcNow });

        var context = new StatusChangeEventContext
        {
            CheckId = Guid.NewGuid(),
            CheckName = "Test Check",
            WorkspaceName = workspace.Name,
            PreviousStatus = "Up",
            NewStatus = "Warn",
            Timestamp = DateTimeOffset.UtcNow
        };

        await _service.TriggerStatusChangeEventAsync(workspace.Id, context);

        await _mockHandler.Received(1).SendStatusChangeEventAsync(
            Arg.Is<NotificationChannel>(nc => nc.Id == channel.Id),
            Arg.Is<StatusChangeEventContext>(ctx =>
                ctx.PreviousStatus == "Up" && ctx.NewStatus == "Warn"),
            Arg.Any<CancellationToken>());
    }

    private async Task<Workspace> CreateWorkspaceAsync()
    {
        var workspace = new Workspace
        {
            Name = "Test Workspace",
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.Workspaces.Add(workspace);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return workspace;
    }

    private async Task<NotificationChannel> CreateNotificationChannelAsync(
        Guid workspaceId,
        string name,
        string channelType)
    {
        var channel = new NotificationChannel
        {
            WorkspaceId = workspaceId,
            Name = name,
            ChannelType = channelType,
            ConfigurationJson = new Dictionary<string, System.Text.Json.JsonElement>(),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.NotificationChannels.Add(channel);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return channel;
    }

    private async Task<EventSubscription> CreateEventSubscriptionAsync(Guid channelId, string eventType)
    {
        var subscription = new EventSubscription
        {
            NotificationChannelId = channelId,
            EventType = eventType,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.EventSubscriptions.Add(subscription);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return subscription;
    }
}
