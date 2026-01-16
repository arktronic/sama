namespace SAMA.Web.Models;

public record WorkspaceMemberViewModel(
    Guid UserId,
    string Email,
    string Role,
    string Source,
    DateTimeOffset CreatedAt);
