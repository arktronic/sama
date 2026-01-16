using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SAMA.Web.Authorization;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.EventSubscriptions;

[RequireWorkspaceEditAccess]
public class ManageModel(
    WorkspaceQueryService _workspaceQueryService,
    EventSubscriptionQueryService _eventSubscriptionQueryService,
    EventSubscriptionCommandService _eventSubscriptionCommandService)
    : WorkspacePageModel(_workspaceQueryService)
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<EventSubscriptionChannelViewModel> Channels { get; set; } = [];

    public class InputModel
    {
        [Required]
        public string EventType { get; set; } = string.Empty;

        public List<Guid> SelectedChannelIds { get; set; } = [];
    }

    public async Task<IActionResult> OnGetAsync(Guid? workspaceId, string? eventType)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "EventSubscriptions");
        if (result != null)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            return RedirectToPage("Index", new { workspaceId });
        }

        var validEventTypes = new[]
        {
            EventTypes.CheckCreated,
            EventTypes.CheckUpdated,
            EventTypes.CheckDeleted,
            EventTypes.CheckStatusChanged
        };

        if (!validEventTypes.Contains(eventType))
        {
            return RedirectToPage("Index", new { workspaceId });
        }

        Input.EventType = eventType;

        Channels = await _eventSubscriptionQueryService.GetChannelsForEventTypeAsync(WorkspaceId, eventType);
        Input.SelectedChannelIds = Channels.Where(c => c.IsSubscribed).Select(c => c.Id).ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid? workspaceId)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "EventSubscriptions");
        if (result != null)
        {
            return result;
        }

        if (!ModelState.IsValid)
        {
            Channels = await _eventSubscriptionQueryService.GetChannelsForEventTypeAsync(WorkspaceId, Input.EventType);
            return Page();
        }

        var updateResult = await _eventSubscriptionCommandService.UpdateEventSubscriptionsAsync(
            WorkspaceId,
            Input.EventType,
            Input.SelectedChannelIds,
            User.Identity?.Name ?? "unknown");

        if (updateResult.Success)
        {
            TempData["SuccessMessage"] = $"Event subscriptions updated for {EventTypes.GetDisplayName(Input.EventType)}: {updateResult.CreatedCount} added, {updateResult.DeletedCount} removed.";
        }

        return RedirectToPage("Index", new { workspaceId });
    }
}
