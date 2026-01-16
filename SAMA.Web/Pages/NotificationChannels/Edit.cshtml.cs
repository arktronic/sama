using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAMA.Shared.Constants;
using SAMA.Web.Authorization;
using SAMA.Web.Models;
using SAMA.Web.Pages.Shared;
using SAMA.Web.Services;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.NotificationChannels;

[RequireWorkspaceEditAccess]
public class EditModel(
    WorkspaceQueryService _workspaceQueryService,
    ChannelQueryService _channelQueryService,
    NotificationChannelConfigurationService _configService,
    ChannelCommandService _channelCommandService)
    : WorkspacePageModel(_workspaceQueryService)
{
    public List<SelectListItem> ChannelTypes { get; set; } = [];

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel : NotificationChannelInputBase
    {
        public Guid Id { get; set; }

        public Guid WorkspaceId { get; set; }

        [Required(ErrorMessage = "Channel name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Channel type is required")]
        public override string ChannelType { get; set; } = string.Empty;

        public bool Enabled { get; set; }
    }

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

        Input = new InputModel
        {
            Id = channel.Id,
            WorkspaceId = channel.WorkspaceId,
            Name = channel.Name,
            ChannelType = channel.ChannelType,
            Enabled = channel.Enabled
        };

        // Populate configuration fields based on channel type
        _configService.PopulateFromConfiguration(Input, channel.ConfigurationJson);

        PopulateChannelTypes();
        return Page();
    }

    /// <summary>
    /// HTMX handler to load configuration fields based on selected channel type.
    /// </summary>
    /// <param name="channelType">Channel type string</param>
    /// <returns>Edit page with configuration fields for the selected channel type</returns>
    public IActionResult OnGetConfigFields(string channelType)
    {
        Input.ChannelType = channelType;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var channel = await _channelQueryService.GetChannelDetailsAsync(Input.Id);
        if (channel == null)
        {
            return NotFound();
        }

        // For Email channels, handle password update logic
        if (Input.ChannelType == Constants.ChannelTypes.Email)
        {
            if (!string.IsNullOrWhiteSpace(Input.EmailSmtpPassword))
            {
                // User entered a new password - use it
            }
            else
            {
                var clearPassword = Request.Form["ClearEmailPassword"].ToString() == "true";

                if (clearPassword)
                {
                    Input.EmailSmtpPassword = null;
                }
                else
                {
                    var existingPassword = channel.ConfigurationJson.TryGetValue(ConfigurationKeys.Email.Password, out var pwdElement)
                        ? pwdElement.GetString()
                        : null;
                    Input.EmailSmtpPassword = existingPassword;
                }
            }
        }

        // For EventGrid channels, handle access key update logic
        if (Input.ChannelType == Constants.ChannelTypes.EventGrid)
        {
            if (string.IsNullOrWhiteSpace(Input.EventGridAccessKey))
            {
                var existingKey = channel.ConfigurationJson.TryGetValue(ConfigurationKeys.EventGrid.AccessKey, out var keyElement)
                    ? keyElement.GetString()
                    : null;
                Input.EventGridAccessKey = existingKey;
            }
        }

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

        await _channelCommandService.UpdateChannelAsync(
            Input.Id,
            Input.Name,
            Input.ChannelType,
            configuration,
            Input.Enabled,
            User.Identity?.Name ?? "System");

        TempData["SuccessMessage"] = $"Notification channel '{Input.Name}' updated successfully.";

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
