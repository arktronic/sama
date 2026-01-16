namespace SAMA.Web.Models;

/// <summary>
/// Notification context for lifecycle events (CheckCreated, CheckUpdated, CheckDeleted).
/// Used for audit trails and external system integration.
/// </summary>
public record LifecycleEventContext
{
    public required string EventType { get; init; }

    public required Guid CheckId { get; init; }

    public required string CheckName { get; init; }

    public required string CheckType { get; init; }

    public required string WorkspaceName { get; init; }

    public required DateTimeOffset Timestamp { get; init; }

    public required string PerformedBy { get; init; }

    public Dictionary<string, object>? ConfigurationChanges { get; init; }
}
