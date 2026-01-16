using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;

namespace SAMA.Web.Services.Commands;

public class EventSubscriptionCommandService(SamaDbContext _dbContext, ILogger<EventSubscriptionCommandService> _logger)
{
    public class UpdateEventSubscriptionsResult
    {
        public bool Success { get; set; }

        public int CreatedCount { get; set; }

        public int DeletedCount { get; set; }
    }

    public virtual async Task<UpdateEventSubscriptionsResult> UpdateEventSubscriptionsAsync(
        Guid workspaceId,
        string eventType,
        List<Guid> selectedChannelIds,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        var existingSubscriptions = await _dbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspaceId && es.EventType == eventType)
            .ToListAsync(cancellationToken);

        var existingChannelIds = existingSubscriptions.Select(es => es.NotificationChannelId).ToHashSet();
        var selectedChannelIdsSet = selectedChannelIds.ToHashSet();

        var toDelete = existingSubscriptions.Where(es => !selectedChannelIdsSet.Contains(es.NotificationChannelId)).ToList();
        var toCreate = selectedChannelIdsSet.Except(existingChannelIds).ToList();

        if (toDelete.Count > 0)
        {
            _dbContext.EventSubscriptions.RemoveRange(toDelete);
        }

        foreach (var channelId in toCreate)
        {
            _dbContext.EventSubscriptions.Add(new EventSubscription
            {
                NotificationChannelId = channelId,
                EventType = eventType,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {User} updated event subscriptions for {EventType} in workspace {WorkspaceId}: {Created} created, {Deleted} deleted",
            performedBy,
            eventType,
            workspaceId,
            toCreate.Count,
            toDelete.Count);

        return new UpdateEventSubscriptionsResult
        {
            Success = true,
            CreatedCount = toCreate.Count,
            DeletedCount = toDelete.Count
        };
    }
}
