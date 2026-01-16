using SAMA.Data.Entities;
using SAMA.Web.Models;

namespace SAMA.Web.Services.NotificationChannels;

/// <summary>
/// Handler for sending notifications through various channels.
/// Supports three notification types: status alerts, lifecycle events, and status change events.
/// </summary>
public interface INotificationChannelHandler
{
    /// <summary>
    /// Send a threshold-based status alert.
    /// Used when a check fails N consecutive times or recovers.
    /// </summary>
    Task<NotificationResultModel> SendStatusAlertAsync(
        NotificationChannel channel,
        StatusAlertContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a lifecycle event notification (CheckCreated, CheckUpdated, CheckDeleted).
    /// Used for audit trails and external system integration.
    /// </summary>
    Task<NotificationResultModel> SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a status change event to external systems for streaming/integration.
    /// Fires on every status change without filtering or thresholds.
    /// </summary>
    Task<NotificationResultModel> SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default);
}
