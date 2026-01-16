using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using SAMA.Tests.Unit.TestUtilities;
using SAMA.Web.Pages.Admin.Users;
using SAMA.Web.Services.Commands;

namespace SAMA.Tests.Unit.Web.Pages.Admin.Users;

[TestClass]
public class CreateModelTests
{
    private UserCommandService _mockUserCommand = null!;
    private CreateModel _pageModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserCommand = Substitute.For<UserCommandService>(null!, null!, null!);

        _pageModel = new CreateModel(_mockUserCommand);
        PageModelTestHelpers.ConfigurePageModel(_pageModel);
    }

    [TestMethod]
    public void OnGetShouldReturnPage()
    {
        _pageModel.OnGet();

        Assert.IsNotNull(_pageModel.Input);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldReturnPageWhenModelStateIsInvalid()
    {
        _pageModel.ModelState.AddModelError("Input.Email", "Email is required");

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsFalse(_pageModel.ModelState.IsValid);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldCallCreateUserAsyncWithCorrectParameters()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "newuser@example.com",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        await _pageModel.OnPostAsync();

        await _mockUserCommand.Received(1).CreateUserAsync(
            "newuser@example.com",
            "Test-Password-1234567890!",
            false,
            Arg.Any<string>());
    }

    [TestMethod]
    public async Task OnPostAsyncShouldCallCreateUserAsyncWithAdminRole()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "admin@example.com",
            Password = "Admin-Password-1234567890!",
            ConfirmPassword = "Admin-Password-1234567890!",
            IsAdmin = true
        };

        await _pageModel.OnPostAsync();

        await _mockUserCommand.Received(1).CreateUserAsync(
            "admin@example.com",
            "Admin-Password-1234567890!",
            true,
            Arg.Any<string>());
    }

    [TestMethod]
    public async Task OnPostAsyncShouldRedirectToIndexAfterSuccessfulCreation()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "test@example.com",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(result);
        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("Index", redirect.PageName);
    }

    [TestMethod]
    public async Task OnPostAsyncShouldSetSuccessMessageInTempData()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "success@example.com",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        await _pageModel.OnPostAsync();

        Assert.IsTrue(_pageModel.TempData.ContainsKey("SuccessMessage"));
        var message = _pageModel.TempData["SuccessMessage"]?.ToString();
        Assert.IsNotNull(message);
        StringAssert.Contains(message, "success@example.com");
        StringAssert.Contains(message, "created successfully");
    }

    [TestMethod]
    public async Task OnPostAsyncShouldReturnPageWhenCreateUserAsyncThrows()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "duplicate@example.com",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        _mockUserCommand.When(x => x.CreateUserAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>()))
            .Do(x => throw new InvalidOperationException("Email already exists"));

        var result = await _pageModel.OnPostAsync();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsFalse(_pageModel.ModelState.IsValid);
        Assert.IsTrue(_pageModel.ModelState.ContainsKey(string.Empty));
    }

    [TestMethod]
    public async Task OnPostAsyncShouldNotCallCreateUserAsyncWhenModelStateIsInvalid()
    {
        _pageModel.ModelState.AddModelError("Input.Email", "Email is required");
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        await _pageModel.OnPostAsync();

        await _mockUserCommand.DidNotReceive().CreateUserAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>());
    }

    [TestMethod]
    public async Task OnPostAsyncShouldAddModelErrorWithExceptionMessage()
    {
        _pageModel.Input = new CreateModel.InputModel
        {
            Email = "test@example.com",
            Password = "Test-Password-1234567890!",
            ConfirmPassword = "Test-Password-1234567890!",
            IsAdmin = false
        };

        _mockUserCommand.When(x => x.CreateUserAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<bool>(),
            Arg.Any<string>()))
            .Do(x => throw new InvalidOperationException("Custom error message"));

        await _pageModel.OnPostAsync();

        var errors = _pageModel.ModelState[string.Empty]?.Errors;
        Assert.IsNotNull(errors);
        Assert.HasCount(1, errors);
        Assert.AreEqual("Custom error message", errors[0].ErrorMessage);
    }
}
