using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAMA.Data;
using SAMA.Data.Entities;
using SAMA.Web.Constants;

namespace SAMA.Web.Services;

/// <summary>
/// Service to seed initial database data including roles and default workspace.
/// </summary>
public class DatabaseSeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    SamaDbContext dbContext,
    ILogger<DatabaseSeeder> logger)
{
    /// <summary>
    /// Seeds required roles and default workspace into the database.
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedDefaultWorkspaceAsync();
    }

    private async Task SeedRolesAsync()
    {
        // Seed Admin role
        if (!await roleManager.RoleExistsAsync(AuthConstants.AdminRole))
        {
            logger.LogInformation("Creating {RoleName} role", AuthConstants.AdminRole);
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(AuthConstants.AdminRole));

            if (result.Succeeded)
            {
                logger.LogInformation("{RoleName} role created successfully", AuthConstants.AdminRole);
            }
            else
            {
                logger.LogError(
                    "Failed to create {RoleName} role: {Errors}",
                    AuthConstants.AdminRole,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogDebug("{RoleName} role already exists", AuthConstants.AdminRole);
        }
    }

    private async Task SeedDefaultWorkspaceAsync()
    {
        if (!await dbContext.Workspaces.AnyAsync() && !await dbContext.Users.AnyAsync())
        {
            logger.LogInformation("Creating Default Workspace");
            var workspace = new Workspace
            {
                Name = "Default Workspace",
                Description = "A place for your checks and notifications",
                IsPublic = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            dbContext.Workspaces.Add(workspace);
            await dbContext.SaveChangesAsync();
            logger.LogInformation("Default Workspace created successfully (Id: {WorkspaceId})", workspace.Id);
        }
        else
        {
            logger.LogDebug("Workspaces or users already exist, skipping Default Workspace creation.");
        }
    }
}
