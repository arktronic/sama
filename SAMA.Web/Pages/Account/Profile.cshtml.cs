using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAMA.Data.Entities;
using SAMA.Web.Extensions;
using SAMA.Web.Services;
using SAMA.Web.Services.Queries;

namespace SAMA.Web.Pages.Account;

[AllowAnonymous]
public class ProfileModel(
    UserManager<ApplicationUser> _userManager,
    SignInManager<ApplicationUser> _signInManager,
    UserPreferencesService _userPreferences,
    WorkspaceAuthorizationService _workspaceAuth,
    WorkspaceQueryService _workspaceQuery,
    ILogger<ProfileModel> _logger)
    : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public PreferencesInputModel PreferencesInput { get; set; } = new();

    public List<SelectListItem> AvailableWorkspaces { get; set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 14, ErrorMessage = "Password must be at least 14 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class PreferencesInputModel
    {
        public Guid? DefaultWorkspaceId { get; set; }
    }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadPreferencesAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            await LoadPreferencesAsync(user);
            return Page();
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadPreferencesAsync(user);
            return Page();
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User {Email} changed their password", user.Email);

        TempData["SuccessMessage"] = "Your password has been changed successfully.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPreferencesAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return RedirectToPage("/Account/Login");
        }

        // Validate that the selected workspace is accessible to the user
        if (PreferencesInput.DefaultWorkspaceId.HasValue)
        {
            var userId = User.GetUserId();
            var canView = await _workspaceAuth.CanViewWorkspace(userId, PreferencesInput.DefaultWorkspaceId.Value);
            if (!canView)
            {
                ModelState.AddModelError(string.Empty, "You do not have access to the selected workspace.");
                await LoadPreferencesAsync(user);
                return Page();
            }
        }

        var result = await _userPreferences.SetDefaultWorkspaceIdAsync(user, PreferencesInput.DefaultWorkspaceId);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await LoadPreferencesAsync(user);
            return Page();
        }

        _logger.LogInformation("User {Email} updated their default workspace preference", user.Email);
        TempData["SuccessMessage"] = "Your preferences have been saved.";
        return RedirectToPage();
    }

    private async Task LoadPreferencesAsync(ApplicationUser user)
    {
        var userId = User.GetUserId();
        var accessibleWorkspaceIds = await _workspaceAuth.GetAccessibleWorkspaceIds(userId);
        var workspaces = await _workspaceQuery.GetWorkspacesAsync(accessibleWorkspaceIds);

        AvailableWorkspaces =
        [
            new SelectListItem("None (show workspace list)", "")
        ];
        AvailableWorkspaces.AddRange(workspaces.Select(w => new SelectListItem(w.Name, w.Id.ToString())));

        PreferencesInput.DefaultWorkspaceId = await _userPreferences.GetDefaultWorkspaceIdAsync(user);
    }
}
