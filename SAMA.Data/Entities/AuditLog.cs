namespace SAMA.Data.Entities;

public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public required string Action { get; set; } // Created, Updated, Deleted

    public required string EntityType { get; set; } // Check, Alert, AlertChannel, EventSubscription, WorkspaceGroupMapping, Workspace, User

    public Guid EntityId { get; set; }

    public string? Changes { get; set; } // JSON diff of changes

    public DateTimeOffset Timestamp { get; set; }

    public string? IpAddress { get; set; }


    // Navigation properties
    public ApplicationUser? User { get; set; }
}
