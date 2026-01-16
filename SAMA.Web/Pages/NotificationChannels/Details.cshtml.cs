using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.NotificationChannels;

[RequireWorkspaceViewAccess]
public class DetailsModel(WorkspaceQueryService _workspaceQueryService, ChannelQueryService _channelQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public ChannelDetailsViewModel Channel { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        if (!id.HasValue)
        {
            return NotFound();
        }

        var channel = await _channelQueryService.GetChannelDetailsAsync(id.Value);
        if (channel == null)
        {
            return NotFound();
        }

        var result = await LoadWorkspaceContextAsync(channel.WorkspaceId, "Channels");
        if (result != null)
        {
            return result;
        }

        Channel = channel;

        return Page();
    }
}
