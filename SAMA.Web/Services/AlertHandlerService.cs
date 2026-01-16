using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.NotificationChannels;

namespace SAMA.Web.Services;

public class AlertHandlerService(
    SamaDbContext _dbContext,
    IServiceProvider _serviceProvider,
    EventSubscriptionService _eventSubscriptionService,
    ILogger<AlertHandlerService> _logger)
{
    public async Task ProcessCheckResultAsync(
        Guid checkId,
        CheckExecutionResult result,
        CancellationToken cancellationToken = default)
    {
        var check = await _dbContext.Checks
            .AsSplitQuery()
            .Include(c => c.Workspace)
            .Include(c => c.Alerts.Where(a => a.Enabled))
            .ThenInclude(a => a.NotificationChannels.Where(nc => nc.Enabled))
            .FirstOrDefaultAsync(c => c.Id == checkId, cancellationToken);

        if (check == null)
        {
            _logger.LogWarning("Check {CheckId} not found for alert processing", checkId);
            return;
        }

        // Get recent results to derive both previous status and consecutive failures
        var recentResults = await _dbContext.CheckResults
            .Where(cr => cr.CheckId == checkId)
            .OrderByDescending(cr => cr.CheckedAt)
            .Take(AlertConstants.ConsecutiveFailureQueryLimit)
            .Select(cr => cr.Status)
            .ToListAsync(cancellationToken);

        var previousStatus = recentResults.FirstOrDefault();
        var consecutiveFailures = GetConsecutiveFailureCount(recentResults, result.Status);

        // Trigger status change event if status has changed
        if (previousStatus != null && previousStatus != result.Status)
        {
            var statusChangeContext = new StatusChangeEventContext
            {
                CheckId = check.Id,
                CheckName = check.Name,
                WorkspaceName = check.Workspace.Name,
                PreviousStatus = previousStatus,
                NewStatus = result.Status,
                ResponseTimeMs = result.ResponseTimeMs,
                ErrorMessage = result.ErrorMessage,
                Timestamp = result.CheckedAt
            };

            await _eventSubscriptionService.TriggerStatusChangeEventAsync(check.WorkspaceId, statusChangeContext, cancellationToken);
        }

        if (check.Alerts.Count == 0)
        {
            return;
        }

        foreach (var alert in check.Alerts)
        {
            await ProcessAlertAsync(alert, check, result, consecutiveFailures, cancellationToken);
        }
    }

    private static bool ShouldTriggerWarnOrDownAlertByThreshold(Alert alert, string status, int consecutiveFailures)
    {
        if (status == CheckStatuses.Up)
        {
            return false;
        }

        if (status == CheckStatuses.Warn && !alert.TriggerOnWarn)
        {
            return false;
        }

        if (status == CheckStatuses.Down && !alert.TriggerOnDown)
        {
            return false;
        }

        return consecutiveFailures >= alert.FailureThreshold;
    }

    private static int GetConsecutiveFailureCount(
        List<string> recentStatuses,
        string currentStatus)
    {
        if (currentStatus == CheckStatuses.Up)
        {
            return 0;
        }

        var count = 0;
        foreach (var status in recentStatuses)
        {
            if (status != currentStatus)
            {
                break;
            }
            count++;
        }

        return count;
    }

    private async Task ProcessAlertAsync(
        Alert alert,
        Check check,
        CheckExecutionResult result,
        int consecutiveFailures,
        CancellationToken cancellationToken)
    {
        ICollection<NotificationChannel> channelsToUse;

        if (alert.NotificationChannels.Count == 0)
        {
            _logger.LogDebug(
                "Alert {AlertId} has no specific channels configured, using all enabled workspace channels",
                alert.Id);

            channelsToUse = await _dbContext.NotificationChannels
                .Where(nc => nc.WorkspaceId == check.WorkspaceId && nc.Enabled)
                .ToListAsync(cancellationToken);

            if (channelsToUse.Count == 0)
            {
                _logger.LogDebug(
                    "Alert {AlertId} has no channels configured and no enabled workspace channels available, skipping",
                    alert.Id);
                return;
            }
        }
        else
        {
            channelsToUse = alert.NotificationChannels;
        }

        var lastSuccessfullySentAlert = await _dbContext.AlertHistories
            .Where(ah => ah.AlertId == alert.Id && ah.Success)
            .OrderByDescending(ah => ah.SentAt)
            .Select(ah => new { ah.Status, ah.SentAt })
            .FirstOrDefaultAsync(cancellationToken);

        var hasSentAlertsSinceUpdate = (lastSuccessfullySentAlert != null && lastSuccessfullySentAlert.SentAt >= alert.UpdatedAt);

        if (!hasSentAlertsSinceUpdate)
        {
            var shouldSendInitialAlert = ShouldTriggerWarnOrDownAlertByThreshold(alert, result.Status, consecutiveFailures);

            if (shouldSendInitialAlert)
            {
                _logger.LogDebug(
                    "No alerts sent for alert {AlertId} since configuration update, sending current status notification",
                    alert.Id);
                await SendAlertsToChannelsAsync(alert, check, result, consecutiveFailures, false, channelsToUse, cancellationToken);
            }

            return;
        }

        var shouldTrigger = ShouldTriggerWarnOrDownAlertByThreshold(alert, result.Status, consecutiveFailures);

        if (shouldTrigger && lastSuccessfullySentAlert?.Status != result.Status)
        {
            await SendAlertsToChannelsAsync(alert, check, result, consecutiveFailures, false, channelsToUse, cancellationToken);
            return;
        }

        if (result.Status == CheckStatuses.Up &&
            alert.SendRecoveryNotification &&
            (lastSuccessfullySentAlert?.Status == CheckStatuses.Down || lastSuccessfullySentAlert?.Status == CheckStatuses.Warn))
        {
            await SendAlertsToChannelsAsync(alert, check, result, consecutiveFailures, true, channelsToUse, cancellationToken);
        }
    }

    private async Task SendAlertsToChannelsAsync(
        Alert alert,
        Check check,
        CheckExecutionResult result,
        int consecutiveFailures,
        bool isRecovery,
        ICollection<NotificationChannel> channels,
        CancellationToken cancellationToken)
    {
        var triggerEventId = Guid.CreateVersion7();

        var context = new StatusAlertContext
        {
            CheckName = check.Name,
            CheckId = check.Id,
            Status = result.Status,
            ErrorMessage = result.ErrorMessage,
            ResponseTimeMs = result.ResponseTimeMs,
            Timestamp = result.CheckedAt,
            WorkspaceName = check.Workspace.Name,
            IsRecovery = isRecovery,
            ConsecutiveFailures = consecutiveFailures
        };

        foreach (var channel in channels)
        {
            await SendNotificationAsync(alert, channel, context, triggerEventId, cancellationToken);
        }
    }

    private async Task SendNotificationAsync(
        Alert alert,
        NotificationChannel channel,
        StatusAlertContext context,
        Guid triggerEventId,
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

                await RecordAlertHistoryAsync(
                    alert.Id,
                    channel.Id,
                    context,
                    triggerEventId,
                    success: false,
                    errorMessage: $"No handler found for channel type {channel.ChannelType}",
                    cancellationToken);

                return;
            }

            var result = await handler.SendStatusAlertAsync(channel, context, cancellationToken);

            await RecordAlertHistoryAsync(
                alert.Id,
                channel.Id,
                context,
                triggerEventId,
                result.Success,
                result.ErrorMessage,
                cancellationToken);

            if (result.Success)
            {
                _logger.LogDebug(
                    "Alert notification sent for check {CheckId} via channel {ChannelName} ({ChannelType})",
                    context.CheckId,
                    channel.Name,
                    channel.ChannelType);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send alert notification for check {CheckId} via channel {ChannelName}: {Error}",
                    context.CheckId,
                    channel.Name,
                    result.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending alert for check {CheckId} via channel {ChannelId}",
                context.CheckId,
                channel.Id);

            await RecordAlertHistoryAsync(
                alert.Id,
                channel.Id,
                context,
                triggerEventId,
                success: false,
                errorMessage: $"Unexpected error: {ex.Message}",
                cancellationToken);
        }
    }

    private async Task RecordAlertHistoryAsync(
        Guid alertId,
        Guid notificationChannelId,
        StatusAlertContext context,
        Guid triggerEventId,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
            ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
            : context.ConsecutiveFailures.ToString();

        var message = context.IsRecovery
            ? $"Check recovered to {context.Status} status"
            : $"Check {context.Status} after {failureCountDisplay} consecutive failure(s)";

        var alertHistory = new AlertHistory
        {
            AlertId = alertId,
            NotificationChannelId = notificationChannelId,
            TriggerEventId = triggerEventId,
            Status = context.Status,
            Message = message,
            Success = success,
            ErrorMessage = errorMessage,
            SentAt = DateTimeOffset.UtcNow
        };

        _dbContext.AlertHistories.Add(alertHistory);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
