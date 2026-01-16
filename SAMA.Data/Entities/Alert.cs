namespace SAMA.Data.Entities;

public class Alert
{
    public Guid Id { get; set; }

    public Guid CheckId { get; set; }

    public required string Name { get; set; }

    public bool TriggerOnWarn { get; set; } = false;

    public bool TriggerOnDown { get; set; } = true;

    public int FailureThreshold { get; set; } = 1; // Number of consecutive failures required

    public bool SendRecoveryNotification { get; set; } = true;

    public bool Enabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public Check Check { get; set; } = null!;

    public ICollection<NotificationChannel> NotificationChannels { get; set; } = new List<NotificationChannel>();

    public ICollection<AlertHistory> AlertHistories { get; set; } = new List<AlertHistory>();
}
