using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SAMA.Data.Entities;
using SAMA.Web.Constants;
using SAMA.Web.Models;
using SAMA.Web.Services.Commands;

namespace SAMA.Tests.Integration.Web.Services.Commands;

[TestClass]
public class UserCommandServiceTests : IntegrationTestBase
{
    private UserCommandService _service = null!;
    private UserManager<ApplicationUser> _userManager = null!;
    private RoleManager<IdentityRole<Guid>> _roleManager = null!;

    [TestInitialize]
    public override async Task InitializeTestAsync()
    {
        await base.InitializeTestAsync();

        _userManager = ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        _service = new UserCommandService(_userManager, DbContext, Substitute.For<ILogger<UserCommandService>>());

        await EnsureAdminRoleExistsAsync();
    }

    [TestMethod]
    public async Task CreateUserAsyncShouldCreateBasicUser()
    {
        await _service.CreateUserAsync("user@example.com", "Test-Password-123456789!", false, "admin");

        var user = await _userManager.FindByEmailAsync("user@example.com");
        Assert.IsNotNull(user);
        Assert.AreEqual("user@example.com", user.Email);
        Assert.AreEqual("user@example.com", user.UserName);
        Assert.IsTrue(user.EmailConfirmed);
    }

    [TestMethod]
    public async Task CreateUserAsyncShouldCreateAdminUser()
    {
        await _service.CreateUserAsync("admin@example.com", "Test-Password-123456789!", true, "system");

        var user = await _userManager.FindByEmailAsync("admin@example.com");
        Assert.IsNotNull(user);

        var isAdmin = await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole);
        Assert.IsTrue(isAdmin);
    }

    [TestMethod]
    public async Task CreateUserAsyncShouldThrowWhenEmailIsDuplicate()
    {
        await _service.CreateUserAsync("duplicate@example.com", "Test-Password-123456789!", false, "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.CreateUserAsync("duplicate@example.com", "Test-Password-123456789!", false, "admin"));
    }

    [TestMethod]
    public async Task UpdateUserEmailAsyncShouldUpdateEmail()
    {
        var user = await CreateUserAsync("old@example.com");

        await _service.UpdateUserEmailAsync(user.Id, "new@example.com", "admin");

        DbContext.ChangeTracker.Clear();
        var updated = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.IsNotNull(updated);
        Assert.AreEqual("new@example.com", updated.Email);
        Assert.AreEqual("new@example.com", updated.UserName);
    }

    [TestMethod]
    public async Task UpdateUserEmailAsyncShouldThrowWhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateUserEmailAsync(Guid.NewGuid(), "test@example.com", "admin"));
    }

    [TestMethod]
    public async Task UpdateUserEmailAsyncShouldThrowWhenEmailIsDuplicate()
    {
        var user1 = await CreateUserAsync("user1@example.com");
        await CreateUserAsync("user2@example.com");

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.UpdateUserEmailAsync(user1.Id, "user2@example.com", "admin"));
    }

    [TestMethod]
    public async Task GrantAdminRoleAsyncShouldGrantRole()
    {
        var user = await CreateUserAsync("user@example.com");

        var result = await _service.GrantAdminRoleAsync(user.Id, "admin");

        Assert.IsTrue(result);

        var isAdmin = await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole);
        Assert.IsTrue(isAdmin);
    }

    [TestMethod]
    public async Task GrantAdminRoleAsyncShouldReturnFalseWhenUserDoesNotExist()
    {
        var result = await _service.GrantAdminRoleAsync(Guid.NewGuid(), "admin");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GrantAdminRoleAsyncShouldBeIdempotent()
    {
        var user = await CreateUserAsync("user@example.com");

        await _service.GrantAdminRoleAsync(user.Id, "admin");
        var result = await _service.GrantAdminRoleAsync(user.Id, "admin");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task RevokeAdminRoleAsyncShouldRevokeRole()
    {
        var user = await CreateUserAsync("admin@example.com");
        await _userManager.AddToRoleAsync(user, AuthConstants.AdminRole);

        var result = await _service.RevokeAdminRoleAsync(user.Id, "admin");

        Assert.IsTrue(result);

        var isAdmin = await _userManager.IsInRoleAsync(user, AuthConstants.AdminRole);
        Assert.IsFalse(isAdmin);
    }

    [TestMethod]
    public async Task RevokeAdminRoleAsyncShouldReturnFalseWhenUserDoesNotExist()
    {
        var result = await _service.RevokeAdminRoleAsync(Guid.NewGuid(), "admin");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RevokeAdminRoleAsyncShouldBeIdempotent()
    {
        var user = await CreateUserAsync("user@example.com");

        await _service.RevokeAdminRoleAsync(user.Id, "admin");
        var result = await _service.RevokeAdminRoleAsync(user.Id, "admin");

        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldAddNewAssignments()
    {
        var user = await CreateUserAsync("user@example.com");
        var workspace1 = await CreateWorkspaceAsync("Workspace 1");
        var workspace2 = await CreateWorkspaceAsync("Workspace 2");

        var assignments = new List<WorkspaceAssignmentViewModel>
        {
            new() { WorkspaceId = workspace1.Id, WorkspaceName = "Workspace 1", Role = AuthConstants.ViewerRole, IsAssigned = true },
            new() { WorkspaceId = workspace2.Id, WorkspaceName = "Workspace 2", Role = AuthConstants.EditorRole, IsAssigned = true }
        };

        var result = await _service.UpdateWorkspaceAssignmentsAsync(user.Id, assignments, "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(2, result.AddedCount);
        Assert.AreEqual(0, result.RemovedCount);
        Assert.AreEqual(0, result.UpdatedCount);

        var userWorkspaces = await DbContext.UserWorkspaces
            .Where(uw => uw.UserId == user.Id)
            .ToListAsync();

        Assert.HasCount(2, userWorkspaces);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldRemoveUnassignedWorkspaces()
    {
        var user = await CreateUserAsync("user@example.com");
        var workspace1 = await CreateWorkspaceAsync("Workspace 1");
        var workspace2 = await CreateWorkspaceAsync("Workspace 2");

        await CreateWorkspaceAssignmentAsync(user.Id, workspace1.Id, AuthConstants.ViewerRole);
        await CreateWorkspaceAssignmentAsync(user.Id, workspace2.Id, AuthConstants.EditorRole);

        var assignments = new List<WorkspaceAssignmentViewModel>
        {
            new() { WorkspaceId = workspace1.Id, WorkspaceName = "Workspace 1", Role = AuthConstants.ViewerRole, IsAssigned = true },
            new() { WorkspaceId = workspace2.Id, WorkspaceName = "Workspace 2", Role = AuthConstants.EditorRole, IsAssigned = false }
        };

        var result = await _service.UpdateWorkspaceAssignmentsAsync(user.Id, assignments, "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.AddedCount);
        Assert.AreEqual(1, result.RemovedCount);
        Assert.AreEqual(0, result.UpdatedCount);

        var userWorkspaces = await DbContext.UserWorkspaces
            .Where(uw => uw.UserId == user.Id)
            .ToListAsync();

        Assert.HasCount(1, userWorkspaces);
        Assert.AreEqual(workspace1.Id, userWorkspaces[0].WorkspaceId);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldUpdateRoles()
    {
        var user = await CreateUserAsync("user@example.com");
        var workspace = await CreateWorkspaceAsync("Workspace");

        await CreateWorkspaceAssignmentAsync(user.Id, workspace.Id, AuthConstants.ViewerRole);

        var assignments = new List<WorkspaceAssignmentViewModel>
        {
            new() { WorkspaceId = workspace.Id, WorkspaceName = "Workspace", Role = AuthConstants.EditorRole, IsAssigned = true }
        };

        var result = await _service.UpdateWorkspaceAssignmentsAsync(user.Id, assignments, "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, result.AddedCount);
        Assert.AreEqual(0, result.RemovedCount);
        Assert.AreEqual(1, result.UpdatedCount);

        DbContext.ChangeTracker.Clear();
        var userWorkspace = await DbContext.UserWorkspaces
            .FirstAsync(uw => uw.UserId == user.Id && uw.WorkspaceId == workspace.Id);

        Assert.AreEqual(AuthConstants.EditorRole, userWorkspace.Role);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldHandleMixedOperations()
    {
        var user = await CreateUserAsync("user@example.com");
        var workspace1 = await CreateWorkspaceAsync("Keep");
        var workspace2 = await CreateWorkspaceAsync("Remove");
        var workspace3 = await CreateWorkspaceAsync("Add");
        var workspace4 = await CreateWorkspaceAsync("Update");

        await CreateWorkspaceAssignmentAsync(user.Id, workspace1.Id, AuthConstants.ViewerRole);
        await CreateWorkspaceAssignmentAsync(user.Id, workspace2.Id, AuthConstants.ViewerRole);
        await CreateWorkspaceAssignmentAsync(user.Id, workspace4.Id, AuthConstants.ViewerRole);

        var assignments = new List<WorkspaceAssignmentViewModel>
        {
            new() { WorkspaceId = workspace1.Id, WorkspaceName = "Keep", Role = AuthConstants.ViewerRole, IsAssigned = true },
            new() { WorkspaceId = workspace2.Id, WorkspaceName = "Remove", Role = AuthConstants.ViewerRole, IsAssigned = false },
            new() { WorkspaceId = workspace3.Id, WorkspaceName = "Add", Role = AuthConstants.EditorRole, IsAssigned = true },
            new() { WorkspaceId = workspace4.Id, WorkspaceName = "Update", Role = AuthConstants.EditorRole, IsAssigned = true }
        };

        var result = await _service.UpdateWorkspaceAssignmentsAsync(user.Id, assignments, "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.AddedCount);
        Assert.AreEqual(1, result.RemovedCount);
        Assert.AreEqual(1, result.UpdatedCount);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldOnlyAffectManualAssignments()
    {
        var user = await CreateUserAsync("user@example.com");
        var workspace1 = await CreateWorkspaceAsync("Manual");
        var workspace2 = await CreateWorkspaceAsync("OIDC");

        await CreateWorkspaceAssignmentAsync(user.Id, workspace1.Id, AuthConstants.ViewerRole, AuthConstants.ManualSource);
        await CreateWorkspaceAssignmentAsync(user.Id, workspace2.Id, AuthConstants.ViewerRole, AuthConstants.OidcSource);

        var assignments = new List<WorkspaceAssignmentViewModel>
        {
            new() { WorkspaceId = workspace1.Id, WorkspaceName = "Manual", Role = AuthConstants.ViewerRole, IsAssigned = false },
            new() { WorkspaceId = workspace2.Id, WorkspaceName = "OIDC", Role = AuthConstants.ViewerRole, IsAssigned = false }
        };

        var result = await _service.UpdateWorkspaceAssignmentsAsync(user.Id, assignments, "admin");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, result.RemovedCount);

        var remaining = await DbContext.UserWorkspaces
            .Where(uw => uw.UserId == user.Id)
            .ToListAsync();

        Assert.HasCount(1, remaining);
        Assert.AreEqual(workspace2.Id, remaining[0].WorkspaceId);
        Assert.AreEqual(AuthConstants.OidcSource, remaining[0].Source);
    }

    [TestMethod]
    public async Task UpdateWorkspaceAssignmentsAsyncShouldReturnFailureWhenUserDoesNotExist()
    {
        var result = await _service.UpdateWorkspaceAssignmentsAsync(
            Guid.NewGuid(),
            [],
            "admin");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("User not found", result.ErrorMessage);
    }

    [TestMethod]
    public async Task DeleteUserAsyncShouldDeleteUser()
    {
        var user = await CreateUserAsync("delete@example.com");

        await _service.DeleteUserAsync(user.Id, "admin");

        var deleted = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.IsNull(deleted);
    }

    [TestMethod]
    public async Task DeleteUserAsyncShouldCascadeDeleteWorkspaceAssignments()
    {
        var user = await CreateUserAsync("delete@example.com");
        var workspace = await CreateWorkspaceAsync("Workspace");
        await CreateWorkspaceAssignmentAsync(user.Id, workspace.Id, AuthConstants.ViewerRole);

        await _service.DeleteUserAsync(user.Id, "admin");

        var assignments = await DbContext.UserWorkspaces
            .Where(uw => uw.UserId == user.Id)
            .ToListAsync();

        Assert.HasCount(0, assignments);
    }

    [TestMethod]
    public async Task DeleteUserAsyncShouldThrowWhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.DeleteUserAsync(Guid.NewGuid(), "admin"));
    }

    [TestMethod]
    public async Task UnlockUserAsyncShouldUnlockUser()
    {
        var user = await CreateUserAsync("locked@example.com");
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddHours(1));
        user.AccessFailedCount = 5;
        await _userManager.UpdateAsync(user);

        var result = await _service.UnlockUserAsync(user.Id, "admin");

        Assert.IsTrue(result);

        DbContext.ChangeTracker.Clear();
        var unlocked = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.IsNotNull(unlocked);
        Assert.IsNull(unlocked.LockoutEnd);
        Assert.AreEqual(0, unlocked.AccessFailedCount);
    }

    [TestMethod]
    public async Task UnlockUserAsyncShouldReturnFalseWhenUserDoesNotExist()
    {
        var result = await _service.UnlockUserAsync(Guid.NewGuid(), "admin");

        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ResetPasswordAsyncShouldResetPassword()
    {
        var user = await CreateUserAsync("reset@example.com");
        var oldPassword = "Test-Password-123456789!";
        var newPassword = "New-Password-987654321!";

        await _service.ResetPasswordAsync(user.Id, newPassword, "admin");

        DbContext.ChangeTracker.Clear();
        var updated = await _userManager.FindByIdAsync(user.Id.ToString());
        Assert.IsNotNull(updated);

        var canSignInWithNew = await _userManager.CheckPasswordAsync(updated, newPassword);
        Assert.IsTrue(canSignInWithNew);

        var canSignInWithOld = await _userManager.CheckPasswordAsync(updated, oldPassword);
        Assert.IsFalse(canSignInWithOld);
    }

    [TestMethod]
    public async Task ResetPasswordAsyncShouldThrowWhenUserDoesNotExist()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _service.ResetPasswordAsync(Guid.NewGuid(), "New-Password-123456789!", "admin"));
    }

    private async Task EnsureAdminRoleExistsAsync()
    {
        if (!await _roleManager.RoleExistsAsync(AuthConstants.AdminRole))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(AuthConstants.AdminRole));
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(string email)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, "Test-Password-123456789!");
        Assert.IsTrue(result.Succeeded, $"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    private async Task<Workspace> CreateWorkspaceAsync(string name)
    {
        var workspace = new Workspace
        {
            Name = name,
            Description = null,
            IsPublic = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.Workspaces.Add(workspace);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        return workspace;
    }

    private async Task CreateWorkspaceAssignmentAsync(Guid userId, Guid workspaceId, string role, string source = AuthConstants.ManualSource)
    {
        var assignment = new UserWorkspace
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Role = role,
            Source = source,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        DbContext.UserWorkspaces.Add(assignment);
        await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();
    }
}
