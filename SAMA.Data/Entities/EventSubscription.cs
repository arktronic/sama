namespace SAMA.Data.Entities;

public class EventSubscription
{
    public Guid Id { get; set; }

    public Guid NotificationChannelId { get; set; }

    public required string EventType { get; set; } // CheckCreated, CheckUpdated, CheckDeleted, CheckStatusChanged

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }


    // Navigation properties
    public NotificationChannel NotificationChannel { get; set; } = null!;
}
