namespace SAMA.Web.Models;

public class EventSubscriptionChannelViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string ChannelType { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public bool IsSubscribed { get; set; }
}
