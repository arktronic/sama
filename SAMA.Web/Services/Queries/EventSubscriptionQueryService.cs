using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.Queries;

public class EventSubscriptionQueryService(SamaDbContext _dbContext)
{
    public virtual async Task<List<EventSubscriptionGroupViewModel>> GetEventSubscriptionGroupsForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var totalChannelCount = await _dbContext.NotificationChannels
            .Where(nc => nc.WorkspaceId == workspaceId)
            .CountAsync(cancellationToken);

        var subscriptionData = await _dbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspaceId)
            .GroupBy(es => es.EventType)
            .Select(g => new
            {
                EventType = g.Key,
                ChannelNames = g.Select(es => es.NotificationChannel.Name).OrderBy(n => n).ToList()
            })
            .ToListAsync(cancellationToken);

        return EventTypes.AllEventTypes.Select(eventType =>
        {
            var subscription = subscriptionData.FirstOrDefault(s => s.EventType == eventType);
            return new EventSubscriptionGroupViewModel
            {
                EventType = eventType,
                SubscribedChannelCount = subscription?.ChannelNames.Count ?? 0,
                TotalChannelCount = totalChannelCount,
                SubscribedChannelNames = subscription?.ChannelNames ?? []
            };
        }).ToList();
    }

    public virtual async Task<List<EventSubscriptionChannelViewModel>> GetChannelsForEventTypeAsync(
        Guid workspaceId,
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var existingSubscriptions = await _dbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspaceId && es.EventType == eventType)
            .Select(es => es.NotificationChannelId)
            .ToListAsync(cancellationToken);

        return await _dbContext.NotificationChannels
            .Where(nc => nc.WorkspaceId == workspaceId)
            .OrderBy(nc => nc.Name)
            .Select(nc => new EventSubscriptionChannelViewModel
            {
                Id = nc.Id,
                Name = nc.Name,
                ChannelType = nc.ChannelType,
                Enabled = nc.Enabled,
                IsSubscribed = existingSubscriptions.Contains(nc.Id)
            })
            .ToListAsync(cancellationToken);
    }
}
