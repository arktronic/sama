using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;
using SAMA.Web.Models;
using SAMA.Web.Services.NotificationChannels;

namespace SAMA.Web.Services;

public class EventSubscriptionService(
    SamaDbContext _dbContext,
    IServiceProvider _serviceProvider,
    ILogger<EventSubscriptionService> _logger)
{
    public virtual async Task TriggerLifecycleEventAsync(
        Guid workspaceId,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspaceId &&
                        es.EventType == context.EventType &&
                        es.NotificationChannel.Enabled)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug(
                "No active subscriptions for event {EventType} in workspace {WorkspaceId}",
                context.EventType,
                workspaceId);
            return;
        }

        _logger.LogDebug(
            "Triggering {EventType} event for {ChannelCount} channel(s) in workspace {WorkspaceId}",
            context.EventType,
            subscriptions.Count,
            workspaceId);

        foreach (var subscription in subscriptions)
        {
            await SendLifecycleEventAsync(subscription.NotificationChannel, context, cancellationToken);
        }
    }

    public virtual async Task TriggerStatusChangeEventAsync(
        Guid workspaceId,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.EventSubscriptions
            .Include(es => es.NotificationChannel)
            .Where(es => es.NotificationChannel.WorkspaceId == workspaceId &&
                        es.EventType == SAMA.Web.Constants.EventTypes.CheckStatusChanged &&
                        es.NotificationChannel.Enabled)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug(
                "No active subscriptions for CheckStatusChanged event in workspace {WorkspaceId}",
                workspaceId);
            return;
        }

        _logger.LogDebug(
            "Triggering CheckStatusChanged event for {ChannelCount} channel(s) in workspace {WorkspaceId}",
            subscriptions.Count,
            workspaceId);

        foreach (var subscription in subscriptions)
        {
            await SendStatusChangeEventAsync(subscription.NotificationChannel, context, cancellationToken);
        }
    }

    private async Task SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = _serviceProvider.GetKeyedService<INotificationChannelHandler>(channel.ChannelType);

            if (handler == null)
            {
                _logger.LogError(
                    "No handler found for channel type {ChannelType}",
                    channel.ChannelType);
                return;
            }

            var result = await handler.SendLifecycleEventAsync(channel, context, cancellationToken);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Lifecycle event {EventType} sent for check {CheckId} via channel {ChannelName} ({ChannelType})",
                    context.EventType,
                    context.CheckId,
                    channel.Name,
                    channel.ChannelType);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send lifecycle event {EventType} for check {CheckId} via channel {ChannelName}: {Error}",
                    context.EventType,
                    context.CheckId,
                    channel.Name,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending lifecycle event {EventType} for check {CheckId} via channel {ChannelId}",
                context.EventType,
                context.CheckId,
                channel.Id);
        }
    }

    private async Task SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = _serviceProvider.GetKeyedService<INotificationChannelHandler>(channel.ChannelType);

            if (handler == null)
            {
                _logger.LogError(
                    "No handler found for channel type {ChannelType}",
                    channel.ChannelType);
                return;
            }

            var result = await handler.SendStatusChangeEventAsync(channel, context, cancellationToken);

            if (result.Success)
            {
                _logger.LogDebug(
                    "Status change event sent for check {CheckId} ({PreviousStatus} → {NewStatus}) via channel {ChannelName} ({ChannelType})",
                    context.CheckId,
                    context.PreviousStatus,
                    context.NewStatus,
                    channel.Name,
                    channel.ChannelType);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send status change event for check {CheckId} via channel {ChannelName}: {Error}",
                    context.CheckId,
                    channel.Name,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending status change event for check {CheckId} via channel {ChannelId}",
                context.CheckId,
                channel.Id);
        }
    }
}
