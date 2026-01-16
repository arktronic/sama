using Microsoft.AspNetCore.Identity;

namespace SAMA.Data.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }


    // Navigation properties
    public ICollection<UserWorkspace> UserWorkspaces { get; set; } = new List<UserWorkspace>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
