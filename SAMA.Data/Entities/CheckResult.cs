namespace SAMA.Data.Entities;

public class CheckResult
{
    public Guid Id { get; set; }

    public Guid CheckId { get; set; }

    public required string Status { get; set; } // Up, Warn, Down

    public int? ResponseTimeMs { get; set; }

    public int? StatusCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CheckedAt { get; set; }


    // Navigation properties
    public Check Check { get; set; } = null!;
}
