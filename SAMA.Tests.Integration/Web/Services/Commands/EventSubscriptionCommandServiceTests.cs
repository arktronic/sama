using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Services.Commands;

namespace SAMA.Tests.Integration.Web.Services.Commands;

[TestClass]
public class EventSubscriptionCommandServiceTests : IntegrationTestBase
{
    private EventSubscriptionCommandService _service = null!;
    private Workspace _workspace = null!;

    [TestInitialize]
    public override async Task InitializeTestAsync()
    {
        await base.InitializeTestAsync();

        _workspace = await CreateWorkspaceAsync("Test Workspace");
        _service = new EventSubscriptionCommandService(
            DbContext,
            Substitute.For<ILogger<EventSubscriptionCommandService>>());
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldCreateNewSubscriptions()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel1.Id, channel2.Id],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.CreatedCount);
        Assert.AreEqual(0, result.DeletedCount);

        var subscriptions = await DbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == _workspace.Id &&
                        es.EventType == EventTypes.CheckCreated)
            .ToListAsync();

        Assert.HasCount(2, subscriptions);
        Assert.IsTrue(subscriptions.Any(s => s.NotificationChannelId == channel1.Id));
        Assert.IsTrue(subscriptions.Any(s => s.NotificationChannelId == channel2.Id));
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldDeleteExistingSubscriptions()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.CreatedCount);
        Assert.AreEqual(2, result.DeletedCount);

        var subscriptions = await DbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == _workspace.Id &&
                        es.EventType == EventTypes.CheckCreated)
            .ToListAsync();

        Assert.HasCount(0, subscriptions);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldAddAndRemoveSubscriptions()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");
        var channel3 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 3");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel2.Id, channel3.Id],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual(1, result.DeletedCount);

        var subscriptions = await DbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == _workspace.Id &&
                        es.EventType == EventTypes.CheckCreated)
            .ToListAsync();

        Assert.HasCount(2, subscriptions);
        Assert.IsFalse(subscriptions.Any(s => s.NotificationChannelId == channel1.Id));
        Assert.IsTrue(subscriptions.Any(s => s.NotificationChannelId == channel2.Id));
        Assert.IsTrue(subscriptions.Any(s => s.NotificationChannelId == channel3.Id));
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldKeepExistingSubscriptionsUnchanged()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel1.Id, channel2.Id],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.CreatedCount);
        Assert.AreEqual(0, result.DeletedCount);

        var subscriptions = await DbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == _workspace.Id &&
                        es.EventType == EventTypes.CheckCreated)
            .ToListAsync();

        Assert.HasCount(2, subscriptions);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldNotAffectOtherEventTypes()
    {
        var channel = await CreateNotificationChannelAsync(_workspace.Id, "Channel");

        await CreateEventSubscriptionAsync(channel.Id, EventTypes.CheckStatusChanged);

        DbContext.ChangeTracker.Clear();

        await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel.Id],
            "admin");

        var statusChangedSubs = await DbContext.EventSubscriptions
            .Where(es => es.EventType == EventTypes.CheckStatusChanged)
            .ToListAsync();

        Assert.HasCount(1, statusChangedSubs);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldNotAffectOtherWorkspaces()
    {
        var workspace2 = await CreateWorkspaceAsync("Workspace 2");

        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "WS1 Channel");
        var channel2 = await CreateNotificationChannelAsync(workspace2.Id, "WS2 Channel");

        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel1.Id],
            "admin");

        var workspace2Subs = await DbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspace2.Id)
            .ToListAsync();

        Assert.HasCount(1, workspace2Subs);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldHandleEmptyInitialState()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Channel");

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckDeleted,
            [],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.CreatedCount);
        Assert.AreEqual(0, result.DeletedCount);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldSetCreatedAtAndUpdatedAtForNewSubscriptions()
    {
        var channel = await CreateNotificationChannelAsync(_workspace.Id, "Channel");
        var beforeCreate = DateTimeOffset.UtcNow.AddSeconds(-1);

        DbContext.ChangeTracker.Clear();

        await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel.Id],
            "admin");

        var subscription = await DbContext.EventSubscriptions
            .SingleAsync(es => es.NotificationChannelId == channel.Id);

        Assert.IsTrue(subscription.CreatedAt > beforeCreate);
        Assert.IsTrue(subscription.UpdatedAt > beforeCreate);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldHandleMultipleChannelsSameEventType()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");
        var channel3 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 3");

        DbContext.ChangeTracker.Clear();

        var result = await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckUpdated,
            [channel1.Id, channel2.Id, channel3.Id],
            "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(3, result.CreatedCount);

        var subscriptions = await DbContext.EventSubscriptions
            .Where(es => es.EventType == EventTypes.CheckUpdated)
            .ToListAsync();

        Assert.HasCount(3, subscriptions);
    }

    [TestMethod]
    public async Task UpdateEventSubscriptionsAsyncShouldHandleSameChannelMultipleEventTypes()
    {
        var channel = await CreateNotificationChannelAsync(_workspace.Id, "Channel");

        DbContext.ChangeTracker.Clear();

        await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckCreated,
            [channel.Id],
            "admin");

        await _service.UpdateEventSubscriptionsAsync(
            _workspace.Id,
            EventTypes.CheckDeleted,
            [channel.Id],
            "admin");

        var subscriptions = await DbContext.EventSubscriptions
            .Where(es => es.NotificationChannelId == channel.Id)
            .ToListAsync();

        Assert.HasCount(2, subscriptions);
        Assert.IsTrue(subscriptions.Any(s => s.EventType == EventTypes.CheckCreated));
        Assert.IsTrue(subscriptions.Any(s => s.EventType == EventTypes.CheckDeleted));
    }

    private async Task<Workspace> CreateWorkspaceAsync(string name)
    {
        var workspace = new Workspace
        {
            Name = name,
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.Workspaces.Add(workspace);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return workspace;
    }

    private async Task<NotificationChannel> CreateNotificationChannelAsync(Guid workspaceId, string name)
    {
        var channel = new NotificationChannel
        {
            WorkspaceId = workspaceId,
            Name = name,
            ChannelType = "Email",
            ConfigurationJson = new Dictionary<string, System.Text.Json.JsonElement>(),
            Enabled = true,
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
