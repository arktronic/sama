namespace SAMA.Web.Models;

public record NotificationResultModel
{
    public required bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public required DateTimeOffset SentAt { get; init; }
}
