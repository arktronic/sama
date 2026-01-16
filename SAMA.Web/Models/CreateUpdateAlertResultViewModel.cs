namespace SAMA.Web.Models;

public class CreateUpdateAlertResultViewModel
{
    public bool Success { get; set; }

    public Guid AlertId { get; set; }

    public string? ErrorMessage { get; set; }

    public bool ShouldTriggerCheck { get; set; }

    public int ChannelCount { get; set; }

    public int AllChannelsCount { get; set; }
}
