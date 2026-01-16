using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.NotificationChannels;

[RequireWorkspaceViewAccess]
public class IndexModel(WorkspaceQueryService _workspaceQueryService, ChannelQueryService _channelQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public IList<ChannelListItemViewModel> NotificationChannels { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? workspaceId)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "Channels");
        if (result != null)
        {
            return result;
        }

        NotificationChannels = await _channelQueryService.GetChannelsForWorkspaceAsync(WorkspaceId);

        return Page();
    }
}
