using System.Text.Json;

namespace SAMA.Data.Entities;

public class Check
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public required string CheckType { get; set; } // Http, Tcp, Ping, Dns, Tls, Script

    public required Dictionary<string, JsonElement> ConfigurationJson { get; set; } // Encrypted JSON

    public int IntervalSeconds { get; set; }

    public int TimeoutSeconds { get; set; } = 30;

    public bool Enabled { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }


    // Navigation properties
    public Workspace Workspace { get; set; } = null!;

    public ICollection<CheckResult> CheckResults { get; set; } = [];

    public ICollection<Alert> Alerts { get; set; } = [];
}
