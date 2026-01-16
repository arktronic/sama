namespace SAMA.Web.Models;

public class EventSubscriptionGroupViewModel
{
    public string EventType { get; set; } = string.Empty;

    public int SubscribedChannelCount { get; set; }

    public int TotalChannelCount { get; set; }

    public List<string> SubscribedChannelNames { get; set; } = [];
}
