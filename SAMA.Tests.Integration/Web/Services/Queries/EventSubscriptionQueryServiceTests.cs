using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Services.Queries;

namespace SAMA.Tests.Integration.Web.Services.Queries;

[TestClass]
public class EventSubscriptionQueryServiceTests : IntegrationTestBase
{
    private EventSubscriptionQueryService _service = null!;
    private Workspace _workspace = null!;

    [TestInitialize]
    public override async Task InitializeTestAsync()
    {
        await base.InitializeTestAsync();

        _workspace = await CreateWorkspaceAsync("Test Workspace");
        _service = new EventSubscriptionQueryService(DbContext);
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldReturnAllEventTypes()
    {
        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        Assert.HasCount(4, groups);
        Assert.IsTrue(groups.Any(g => g.EventType == EventTypes.CheckCreated));
        Assert.IsTrue(groups.Any(g => g.EventType == EventTypes.CheckUpdated));
        Assert.IsTrue(groups.Any(g => g.EventType == EventTypes.CheckDeleted));
        Assert.IsTrue(groups.Any(g => g.EventType == EventTypes.CheckStatusChanged));
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldShowZeroSubscriptionsWhenNone()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Test Channel");

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        Assert.IsTrue(groups.All(g => g.SubscribedChannelCount == 0));
        Assert.IsTrue(groups.All(g => g.SubscribedChannelNames.Count == 0));
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldShowCorrectTotalChannelCount()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 3");

        DbContext.ChangeTracker.Clear();

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        Assert.IsTrue(groups.All(g => g.TotalChannelCount == 3));
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldShowSubscribedChannelsForEventType()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        var createdGroup = groups.Single(g => g.EventType == EventTypes.CheckCreated);
        Assert.AreEqual(2, createdGroup.SubscribedChannelCount);
        Assert.HasCount(2, createdGroup.SubscribedChannelNames);
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldOrderChannelNamesByName()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Zebra Channel");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Alpha Channel");
        var channel3 = await CreateNotificationChannelAsync(_workspace.Id, "Beta Channel");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel3.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        var createdGroup = groups.Single(g => g.EventType == EventTypes.CheckCreated);
        Assert.AreEqual("Alpha Channel", createdGroup.SubscribedChannelNames[0]);
        Assert.AreEqual("Beta Channel", createdGroup.SubscribedChannelNames[1]);
        Assert.AreEqual("Zebra Channel", createdGroup.SubscribedChannelNames[2]);
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldShowDifferentSubscriptionsForDifferentEventTypes()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel2.Id, EventTypes.CheckCreated);
        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckStatusChanged);

        DbContext.ChangeTracker.Clear();

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        var createdGroup = groups.Single(g => g.EventType == EventTypes.CheckCreated);
        Assert.AreEqual(2, createdGroup.SubscribedChannelCount);

        var statusChangedGroup = groups.Single(g => g.EventType == EventTypes.CheckStatusChanged);
        Assert.AreEqual(1, statusChangedGroup.SubscribedChannelCount);

        var updatedGroup = groups.Single(g => g.EventType == EventTypes.CheckUpdated);
        Assert.AreEqual(0, updatedGroup.SubscribedChannelCount);
    }

    [TestMethod]
    public async Task GetEventSubscriptionGroupsForWorkspaceAsyncShouldNotIncludeOtherWorkspaceChannels()
    {
        var workspace2 = await CreateWorkspaceAsync("Workspace 2");

        await CreateNotificationChannelAsync(_workspace.Id, "WS1 Channel");
        await CreateNotificationChannelAsync(workspace2.Id, "WS2 Channel 1");
        await CreateNotificationChannelAsync(workspace2.Id, "WS2 Channel 2");

        DbContext.ChangeTracker.Clear();

        var groups = await _service.GetEventSubscriptionGroupsForWorkspaceAsync(_workspace.Id);

        Assert.IsTrue(groups.All(g => g.TotalChannelCount == 1));
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldReturnAllWorkspaceChannels()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");
        await CreateNotificationChannelAsync(_workspace.Id, "Channel 3");

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        Assert.HasCount(3, channels);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldMarkSubscribedChannels()
    {
        var channel1 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 1");
        var channel2 = await CreateNotificationChannelAsync(_workspace.Id, "Channel 2");

        await CreateEventSubscriptionAsync(channel1.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        var subscribedChannel = channels.Single(c => c.Id == channel1.Id);
        Assert.IsTrue(subscribedChannel.IsSubscribed);

        var unsubscribedChannel = channels.Single(c => c.Id == channel2.Id);
        Assert.IsFalse(unsubscribedChannel.IsSubscribed);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldOrderChannelsByName()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Zebra Channel");
        await CreateNotificationChannelAsync(_workspace.Id, "Alpha Channel");
        await CreateNotificationChannelAsync(_workspace.Id, "Beta Channel");

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        Assert.AreEqual("Alpha Channel", channels[0].Name);
        Assert.AreEqual("Beta Channel", channels[1].Name);
        Assert.AreEqual("Zebra Channel", channels[2].Name);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldOnlyMarkSubscriptionsForSpecificEventType()
    {
        var channel = await CreateNotificationChannelAsync(_workspace.Id, "Test Channel");

        await CreateEventSubscriptionAsync(channel.Id, EventTypes.CheckCreated);

        DbContext.ChangeTracker.Clear();

        var createdChannels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);
        var createdChannel = createdChannels.Single();
        Assert.IsTrue(createdChannel.IsSubscribed);

        var updatedChannels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckUpdated);
        var updatedChannel = updatedChannels.Single();
        Assert.IsFalse(updatedChannel.IsSubscribed);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldReturnEmptyListWhenNoChannels()
    {
        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        Assert.HasCount(0, channels);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldIncludeChannelType()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Email Channel", "Email");
        await CreateNotificationChannelAsync(_workspace.Id, "Slack Channel", "Slack");

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        var emailChannel = channels.Single(c => c.Name == "Email Channel");
        Assert.AreEqual("Email", emailChannel.ChannelType);

        var slackChannel = channels.Single(c => c.Name == "Slack Channel");
        Assert.AreEqual("Slack", slackChannel.ChannelType);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldNotIncludeOtherWorkspaceChannels()
    {
        var workspace2 = await CreateWorkspaceAsync("Workspace 2");

        await CreateNotificationChannelAsync(_workspace.Id, "WS1 Channel");
        await CreateNotificationChannelAsync(workspace2.Id, "WS2 Channel");

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        Assert.HasCount(1, channels);
        Assert.AreEqual("WS1 Channel", channels[0].Name);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldIncludeEnabledStatus()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Enabled Channel", "Email", enabled: true);
        await CreateNotificationChannelAsync(_workspace.Id, "Disabled Channel", "Slack", enabled: false);

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        var enabledChannel = channels.Single(c => c.Name == "Enabled Channel");
        Assert.IsTrue(enabledChannel.Enabled);

        var disabledChannel = channels.Single(c => c.Name == "Disabled Channel");
        Assert.IsFalse(disabledChannel.Enabled);
    }

    [TestMethod]
    public async Task GetChannelsForEventTypeAsyncShouldReturnBothEnabledAndDisabledChannels()
    {
        await CreateNotificationChannelAsync(_workspace.Id, "Enabled Channel 1", "Email", enabled: true);
        await CreateNotificationChannelAsync(_workspace.Id, "Disabled Channel", "Slack", enabled: false);
        await CreateNotificationChannelAsync(_workspace.Id, "Enabled Channel 2", "Teams", enabled: true);

        DbContext.ChangeTracker.Clear();

        var channels = await _service.GetChannelsForEventTypeAsync(_workspace.Id, EventTypes.CheckCreated);

        Assert.HasCount(3, channels);
        Assert.AreEqual(2, channels.Count(c => c.Enabled));
        Assert.AreEqual(1, channels.Count(c => !c.Enabled));
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

    private async Task<NotificationChannel> CreateNotificationChannelAsync(Guid workspaceId, string name, string channelType = "Email", bool enabled = true)
    {
        var channel = new NotificationChannel
        {
            WorkspaceId = workspaceId,
            Name = name,
            ChannelType = channelType,
            ConfigurationJson = new Dictionary<string, System.Text.Json.JsonElement>(),
            Enabled = enabled,
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
