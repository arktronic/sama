using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.NotificationChannels;

[RequireWorkspaceEditAccess]
public class CreateModel(
    WorkspaceQueryService _workspaceQueryService,
    NotificationChannelConfigurationService _configService,
    ChannelCommandService _channelCommandService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public List<SelectListItem> ChannelTypes { get; set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel : NotificationChannelInputBase
    {
        public Guid WorkspaceId { get; set; }

        [Required(ErrorMessage = "Channel name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Channel type is required")]
        public override string ChannelType { get; set; } = string.Empty;

        public bool Enabled { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(Guid? workspaceId)
    {
        var result = await LoadWorkspaceContextAsync(workspaceId, "Channels");
        if (result != null)
        {
            return result;
        }

        Input.WorkspaceId = WorkspaceId;
        PopulateChannelTypes();
        return Page();
    }

    public IActionResult OnGetConfigFields(string channelType)
    {
        Input.ChannelType = channelType;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        _configService.ValidateConfiguration(ModelState, Input);

        if (!ModelState.IsValid)
        {
            var result = await LoadWorkspaceContextAsync(Input.WorkspaceId, "Channels");
            if (result != null)
            {
                return result;
            }

            PopulateChannelTypes();
            return Page();
        }

        var configuration = _configService.BuildConfiguration(Input);

        await _channelCommandService.CreateChannelAsync(
            Input.WorkspaceId,
            Input.Name,
            Input.ChannelType,
            configuration,
            Input.Enabled,
            User.Identity?.Name ?? "System");

        TempData["SuccessMessage"] = $"Notification channel '{Input.Name}' created successfully.";

        return RedirectToPage("Index", new { workspaceId = Input.WorkspaceId });
    }

    private void PopulateChannelTypes()
    {
        ChannelTypes = Constants.ChannelTypes.AllChannelTypes.Select(ct => new SelectListItem
        {
            Value = ct,
            Text = Constants.ChannelTypes.GetFullDisplayName(ct)
        }).ToList();
    }
}
