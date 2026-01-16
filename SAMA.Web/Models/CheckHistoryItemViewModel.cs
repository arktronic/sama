namespace SAMA.Web.Models;

public class CheckHistoryItemViewModel
{
    public DateTimeOffset Timestamp { get; set; }

    public int? ResponseTimeMs { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}
