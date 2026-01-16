using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.EventSubscriptions;

[RequireWorkspaceViewAccess]
public class IndexModel(WorkspaceQueryService _workspaceQueryService, EventSubscriptionQueryService _eventSubscriptionQueryService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public IList<EventSubscriptionGroupViewModel> EventSubscriptionGroups { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? workspaceId)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "EventSubscriptions");
        if (result != null)
        {
            return result;
        }

        EventSubscriptionGroups = await _eventSubscriptionQueryService.GetEventSubscriptionGroupsForWorkspaceAsync(WorkspaceId);

        return Page();
    }
}
