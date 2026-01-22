using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using SAMA.Data.Entities;

namespace SAMA.Web.Services;

public class UserPreferencesService(UserManager<ApplicationUser> _userManager)
{
    public const string DefaultWorkspaceIdClaimType = "DefaultWorkspaceId";

    public virtual async Task<Guid?> GetDefaultWorkspaceIdAsync(ApplicationUser user)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var claim = claims.FirstOrDefault(c => c.Type == DefaultWorkspaceIdClaimType);

        if (claim != null && Guid.TryParse(claim.Value, out var workspaceId))
        {
            return workspaceId;
        }

        return null;
    }

    public virtual async Task<IdentityResult> SetDefaultWorkspaceIdAsync(ApplicationUser user, Guid? workspaceId)
    {
        var claims = await _userManager.GetClaimsAsync(user);
        var existingClaim = claims.FirstOrDefault(c => c.Type == DefaultWorkspaceIdClaimType);

        if (existingClaim != null)
        {
            var removeResult = await _userManager.RemoveClaimAsync(user, existingClaim);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        if (workspaceId.HasValue)
        {
            return await _userManager.AddClaimAsync(user, new Claim(DefaultWorkspaceIdClaimType, workspaceId.Value.ToString()));
        }

        return IdentityResult.Success;
    }
}
