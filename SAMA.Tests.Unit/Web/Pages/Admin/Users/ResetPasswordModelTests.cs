using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using SAMA.Tests.Unit.TestUtilities;
using SAMA.Web.Models;
using SAMA.Web.Pages.Admin.Users;
using SAMA.Web.Services.Commands;
using SAMA.Web.Services.Queries;

namespace SAMA.Tests.Unit.Web.Pages.Admin.Users;

[TestClass]
public class ResetPasswordModelTests
{
    private UserQueryService _mockUserQuery = null!;
    private UserCommandService _mockUserCommand = null!;
    private ResetPasswordModel _pageModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserQuery = Substitute.For<UserQueryService>(null!, null!);
        _mockUserCommand = Substitute.For<UserCommandService>(null!, null!, null!);

        _pageModel = new ResetPasswordModel(_mockUserQuery, _mockUserCommand);
        PageModelTestHelpers.ConfigurePageModel(_pageModel);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldReturnNotFoundWhenIdIsNull()
    {
        var result = await _pageModel.OnGetAsync(null);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldReturnNotFoundWhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _mockUserQuery.GetUserByIdAsync(userId).Returns(Task.FromResult<UserViewModel?>(null));

        var result = await _pageModel.OnGetAsync(userId);

        Assert.IsInstanceOfType<NotFoundResult>(result);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldPopulateUserEmailAndUserId()
    {
        var userId = Guid.NewGuid();
        var user = new UserViewModel
        {
            Id = userId,
            Email = "user@example.com",
            IsAdmin = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _mockUserQuery.GetUserByIdAsync(userId).Returns(user);

        var result = await _pageModel.OnGetAsync(userId);

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreEqual("user@example.com", _pageModel.UserEmail);
        Assert.AreEqual(userId, _pageModel.Input.UserId);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldReturnPageWhenModelStateIsInvalid()
    {
        _pageModel.ModelState.AddModelError("Input.NewPassword", "Password is required");
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = Guid.NewGuid(),
            NewPassword = "",
            ConfirmPassword = ""
        };

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsFalse(_pageModel.ModelState.IsValid);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldPopulateUserEmailWhenModelStateIsInvalid()
    {
        var userId = Guid.NewGuid();
        var user = new UserViewModel
        {
            Id = userId,
            Email = "user@example.com",
            IsAdmin = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _mockUserQuery.GetUserByIdAsync(userId).Returns(user);
        _pageModel.ModelState.AddModelError("Input.NewPassword", "Password is required");
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "",
            ConfirmPassword = ""
        };

        await _pageModel.OnPostAsync();

        Assert.AreEqual("user@example.com", _pageModel.UserEmail);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldCallResetPasswordAsyncWithCorrectParameters()
    {
        var userId = Guid.NewGuid();
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "New-Password-123456789!",
            ConfirmPassword = "New-Password-123456789!"
        };

        await _pageModel.OnPostAsync();

        await _mockUserCommand.Received(1).ResetPasswordAsync(
            userId,
            "New-Password-123456789!",
            Arg.Any<string>());
    }

    [TestMethod]
    public async Task OnPostAsyncShouldRedirectToDetailsAfterSuccessfulReset()
    {
        var userId = Guid.NewGuid();
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "New-Password-123456789!",
            ConfirmPassword = "New-Password-123456789!"
        };

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(result);
        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("Details", redirect.PageName);
        Assert.AreEqual(userId, redirect.RouteValues?["id"]);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldSetSuccessMessageInTempData()
    {
        var userId = Guid.NewGuid();
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "New-Password-123456789!",
            ConfirmPassword = "New-Password-123456789!"
        };

        await _pageModel.OnPostAsync();

        Assert.IsTrue(_pageModel.TempData.ContainsKey("SuccessMessage"));
        var message = _pageModel.TempData["SuccessMessage"]?.ToString();
        Assert.IsNotNull(message);
        StringAssert.Contains(message, "Password has been reset successfully");
    }

    [TestMethod]
    public async Task OnPostAsyncShouldReturnPageWhenResetPasswordAsyncThrows()
    {
        var userId = Guid.NewGuid();
        var user = new UserViewModel
        {
            Id = userId,
            Email = "user@example.com",
            IsAdmin = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _mockUserQuery.GetUserByIdAsync(userId).Returns(user);
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "Short",
            ConfirmPassword = "Short"
        };

        _mockUserCommand.When(x => x.ResetPasswordAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>()))
            .Do(x => throw new InvalidOperationException("Password is too short"));

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsFalse(_pageModel.ModelState.IsValid);
        Assert.IsTrue(_pageModel.ModelState.ContainsKey(string.Empty));
        Assert.AreEqual("user@example.com", _pageModel.UserEmail);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldNotCallResetPasswordAsyncWhenModelStateIsInvalid()
    {
        _pageModel.ModelState.AddModelError("Input.NewPassword", "Password is required");
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = Guid.NewGuid(),
            NewPassword = "",
            ConfirmPassword = ""
        };

        await _pageModel.OnPostAsync();

        await _mockUserCommand.DidNotReceive().ResetPasswordAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>());
    }

    [TestMethod]
    public async Task OnPostAsyncShouldAddModelErrorWithExceptionMessage()
    {
        var userId = Guid.NewGuid();
        var user = new UserViewModel
        {
            Id = userId,
            Email = "user@example.com",
            IsAdmin = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _mockUserQuery.GetUserByIdAsync(userId).Returns(user);
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "Test-Password-123456789!",
            ConfirmPassword = "Test-Password-123456789!"
        };

        _mockUserCommand.When(x => x.ResetPasswordAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>()))
            .Do(x => throw new InvalidOperationException("Custom error message"));

        await _pageModel.OnPostAsync();

        var errors = _pageModel.ModelState[string.Empty]?.Errors;
        Assert.IsNotNull(errors);
        Assert.HasCount(1, errors);
        Assert.AreEqual("Custom error message", errors[0].ErrorMessage);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldHandleNullUserEmail()
    {
        var userId = Guid.NewGuid();
        _mockUserQuery.GetUserByIdAsync(userId).Returns(Task.FromResult<UserViewModel?>(null));
        _pageModel.Input = new ResetPasswordModel.InputModel
        {
            UserId = userId,
            NewPassword = "Test-Password-123456789!",
            ConfirmPassword = "Test-Password-123456789!"
        };

        _mockUserCommand.When(x => x.ResetPasswordAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>()))
            .Do(x => throw new InvalidOperationException("User not found"));

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.AreEqual(string.Empty, _pageModel.UserEmail);
    }
}
