namespace SAMA.Web.Models;

public class ChannelListItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ChannelType { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
