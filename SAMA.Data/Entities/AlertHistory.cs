namespace SAMA.Data.Entities;

public class AlertHistory
{
    public Guid Id { get; set; }

    public Guid AlertId { get; set; }

    public Guid NotificationChannelId { get; set; }

    public Guid TriggerEventId { get; set; }

    public required string Status { get; set; } // Warn, Down, Up (recovery)

    public required string Message { get; set; }

    public DateTimeOffset SentAt { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }


    // Navigation properties
    public Alert Alert { get; set; } = null!;

    public NotificationChannel NotificationChannel { get; set; } = null!;
}
