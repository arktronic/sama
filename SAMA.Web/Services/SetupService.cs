using Microsoft.AspNetCore.Identity;
using SAMA.Data.Entities;
using SAMA.Web.Constants;

namespace SAMA.Web.Services;

/// <summary>
/// Service to determine if initial setup is required and to complete the setup process.
/// </summary>
public class SetupService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    ILogger<SetupService> logger)
{
    /// <summary>
    /// Checks if initial setup is required (no admin users exist).
    /// </summary>
    public async Task<bool> IsSetupRequiredAsync()
    {
        // Check if any users in Admin role exist
        var adminRole = await roleManager.FindByNameAsync(AuthConstants.AdminRole);
        if (adminRole == null)
        {
            return true; // Should never happen after seeding, but just in case
        }

        var adminUsers = await userManager.GetUsersInRoleAsync(AuthConstants.AdminRole);
        return adminUsers.Count == 0;
    }

    /// <summary>
    /// Creates the initial admin user.
    /// </summary>
    public async Task<IdentityResult> CreateInitialAdminAsync(string email, string password)
    {
        logger.LogInformation("Creating initial admin user: {Email}", email);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            logger.LogError(
                "Failed to create initial admin user: {Errors}",
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return createResult;
        }

        var roleResult = await userManager.AddToRoleAsync(user, AuthConstants.AdminRole);
        if (!roleResult.Succeeded)
        {
            logger.LogError(
                "Failed to add initial admin user to {RoleName} role: {Errors}",
                AuthConstants.AdminRole,
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            return roleResult;
        }

        logger.LogInformation("Initial admin user created successfully");
        return IdentityResult.Success;
    }
}
