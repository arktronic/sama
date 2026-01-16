namespace SAMA.Web.Models;

/// <summary>
/// Notification context for threshold-based status alerts.
/// Used when a check fails N consecutive times or recovers.
/// </summary>
public record StatusAlertContext
{
    public required string CheckName { get; init; }

    public required Guid CheckId { get; init; }

    public required string Status { get; init; }

    public string? ErrorMessage { get; init; }

    public int? ResponseTimeMs { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string WorkspaceName { get; init; }

    public required bool IsRecovery { get; init; }

    public required int ConsecutiveFailures { get; init; }
}
