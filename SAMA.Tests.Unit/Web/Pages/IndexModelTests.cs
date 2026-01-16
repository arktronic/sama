using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NSubstitute;
using SAMA.Data;
using SAMA.Tests.Unit.TestUtilities;
using SAMA.Web.Pages;
using SAMA.Web.Services;

namespace SAMA.Tests.Unit.Web.Pages;

[TestClass]
public class IndexModelTests
{
    private WorkspaceAuthorizationService _mockAuthService = null!;
    private IndexModel _pageModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockAuthService = Substitute.For<WorkspaceAuthorizationService>(null!, null!);

        _pageModel = new IndexModel(_mockAuthService);
        PageModelTestHelpers.ConfigurePageModel(_pageModel);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldRedirectToDashboardWhenSingleWorkspaceAccessible()
    {
        var workspaceId = Guid.NewGuid();
        _mockAuthService.GetAccessibleWorkspaceIds(Arg.Any<Guid?>()).Returns(Task.FromResult(new List<Guid> { workspaceId }));

        var result = await _pageModel.OnGetAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(result);
        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("/Dashboard/Index", redirect.PageName);
        Assert.IsNotNull(redirect.RouteValues);
        Assert.AreEqual(workspaceId, redirect.RouteValues["workspaceId"]);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldRedirectToWorkspacesIndexWhenMultipleWorkspacesAccessible()
    {
        var workspaceIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        _mockAuthService.GetAccessibleWorkspaceIds(Arg.Any<Guid?>()).Returns(Task.FromResult(workspaceIds));

        var result = await _pageModel.OnGetAsync();

        Assert.IsInstanceOfType<RedirectToPageResult>(result);
        var redirect = (RedirectToPageResult)result;
        Assert.AreEqual("/Workspaces/Index", redirect.PageName);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldShowPageWhenNoWorkspacesAccessible()
    {
        _mockAuthService.GetAccessibleWorkspaceIds(Arg.Any<Guid?>()).Returns(Task.FromResult(new List<Guid>()));

        var result = await _pageModel.OnGetAsync();

        Assert.IsInstanceOfType<PageResult>(result);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldSetIsAuthenticatedTrueForAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        PageModelTestHelpers.ConfigureAuthenticatedUser(_pageModel, userId, "user@example.com");
        _mockAuthService.GetAccessibleWorkspaceIds(userId).Returns(Task.FromResult(new List<Guid>()));

        await _pageModel.OnGetAsync();

        Assert.IsTrue(_pageModel.IsAuthenticated);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldSetIsAuthenticatedFalseForGuest()
    {
        _mockAuthService.GetAccessibleWorkspaceIds(null).Returns(Task.FromResult(new List<Guid>()));

        await _pageModel.OnGetAsync();

        Assert.IsFalse(_pageModel.IsAuthenticated);
    }
}
