namespace SAMA.Web.Models;

public class RecentAlertViewModel
{
    public Guid Id { get; set; }

    public string CheckName { get; set; } = string.Empty;

    public Guid CheckId { get; set; }

    public string AlertName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset SentAt { get; set; }
}
