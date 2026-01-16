namespace SAMA.Web.Models;

/// <summary>
/// Notification context for check status transition events sent to external systems for streaming/integration.
/// Fires on every check status change without filtering or thresholds.
/// </summary>
public record StatusChangeEventContext
{
    public required Guid CheckId { get; init; }

    public required string CheckName { get; init; }

    public required string WorkspaceName { get; init; }

    public required string PreviousStatus { get; init; }

    public required string NewStatus { get; init; }

    public int? ResponseTimeMs { get; init; }

    public string? ErrorMessage { get; init; }

    public required DateTimeOffset Timestamp { get; init; }
}
