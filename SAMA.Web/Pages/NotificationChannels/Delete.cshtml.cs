using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.NotificationChannels;

[RequireWorkspaceEditAccess]
public class DeleteModel(
    WorkspaceQueryService _workspaceQueryService,
    ChannelQueryService _channelQueryService,
    ChannelCommandService _channelCommandService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public ChannelDetailsViewModel ChannelToDelete { get; set; } = new();

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

        ChannelToDelete = channel;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? id)
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

        await _channelCommandService.DeleteChannelAsync(
            id.Value,
            User.Identity?.Name ?? "System");

        TempData["SuccessMessage"] = $"Notification channel '{channel.Name}' deleted successfully.";

        return RedirectToPage("Index", new { workspaceId = channel.WorkspaceId });
    }
}
