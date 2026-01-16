using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Web.Models;

namespace SAMA.Web.Services.Queries;

public class ChannelQueryService(SamaDbContext _dbContext, SensitiveDataMaskingService _maskingService)
{
    public virtual async Task<List<ChannelListItemViewModel>> GetChannelsForWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationChannels
            .Where(nc => nc.WorkspaceId == workspaceId)
            .OrderBy(nc => nc.Name)
            .Select(nc => new ChannelListItemViewModel
            {
                Id = nc.Id,
                Name = nc.Name,
                ChannelType = nc.ChannelType,
                Enabled = nc.Enabled,
                CreatedAt = nc.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<ChannelDetailsViewModel?> GetChannelDetailsAsync(
        Guid channelId,
        CancellationToken cancellationToken = default)
    {
        var channel = await _dbContext.NotificationChannels
            .AsSplitQuery()
            .Include(nc => nc.Workspace)
            .Include(nc => nc.Alerts)
            .Include(nc => nc.EventSubscriptions)
            .FirstOrDefaultAsync(nc => nc.Id == channelId, cancellationToken);

        if (channel == null)
        {
            return null;
        }

        // Calculate true alert count:
        // 1. Alerts explicitly configured to use this channel
        var alertsUsingThisChannel = channel.Alerts.Count;

        // 2. Alerts with no channels (which use all workspace channels)
        var alertsUsingAllChannels = await _dbContext.Alerts
            .Include(a => a.Check)
            .Where(a => a.Check.WorkspaceId == channel.WorkspaceId && !a.NotificationChannels.Any())
            .CountAsync(cancellationToken);

        var totalAlertCount = alertsUsingThisChannel + alertsUsingAllChannels;

        return new ChannelDetailsViewModel
        {
            Id = channel.Id,
            WorkspaceId = channel.WorkspaceId,
            WorkspaceName = channel.Workspace.Name,
            Name = channel.Name,
            ChannelType = channel.ChannelType,
            Enabled = channel.Enabled,
            CreatedAt = channel.CreatedAt,
            UpdatedAt = channel.UpdatedAt,
            AlertCount = totalAlertCount,
            EventSubscriptionCount = channel.EventSubscriptions.Count,
            MaskedConfiguration = _maskingService.MaskNotificationChannelConfig(channel.ChannelType, channel.ConfigurationJson),
            ConfigurationJson = channel.ConfigurationJson
        };
    }
}
