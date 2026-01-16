using NSubstitute;
using SAMA.Tests.Unit.TestUtilities;
using SAMA.Web.Models;
using SAMA.Web.Pages.Admin.Users;
using SAMA.Web.Services.Queries;

namespace SAMA.Tests.Unit.Web.Pages.Admin.Users;

[TestClass]
public class IndexModelTests
{
    private UserQueryService _mockUserQuery = null!;
    private IndexModel _pageModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUserQuery = Substitute.For<UserQueryService>(null!, null!);

        _pageModel = new IndexModel(_mockUserQuery);
        PageModelTestHelpers.ConfigurePageModel(_pageModel);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldLoadUsersFromQueryService()
    {
        var expectedUsers = new List<UserViewModel>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@example.com", IsAdmin = false, IsLockedOut = false },
            new() { Id = Guid.NewGuid(), Email = "admin@example.com", IsAdmin = true, IsLockedOut = false }
        };
        _mockUserQuery.GetAllUsersAsync().Returns(Task.FromResult(expectedUsers));

        await _pageModel.OnGetAsync();

        Assert.HasCount(2, _pageModel.Users);
        Assert.AreEqual("user1@example.com", _pageModel.Users[0].Email);
        Assert.AreEqual("admin@example.com", _pageModel.Users[1].Email);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldCallGetAllUsersAsync()
    {
        var expectedUsers = new List<UserViewModel>();
        _mockUserQuery.GetAllUsersAsync().Returns(Task.FromResult(expectedUsers));

        await _pageModel.OnGetAsync();

        await _mockUserQuery.Received(1).GetAllUsersAsync();
    }

    [TestMethod]
    public async Task OnGetAsyncShouldPopulateEmptyListWhenNoUsers()
    {
        var expectedUsers = new List<UserViewModel>();
        _mockUserQuery.GetAllUsersAsync().Returns(Task.FromResult(expectedUsers));

        await _pageModel.OnGetAsync();

        Assert.IsEmpty(_pageModel.Users);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldIncludeAdminStatus()
    {
        var expectedUsers = new List<UserViewModel>
        {
            new() { Id = Guid.NewGuid(), Email = "admin@example.com", IsAdmin = true, IsLockedOut = false },
            new() { Id = Guid.NewGuid(), Email = "user@example.com", IsAdmin = false, IsLockedOut = false }
        };
        _mockUserQuery.GetAllUsersAsync().Returns(Task.FromResult(expectedUsers));

        await _pageModel.OnGetAsync();

        Assert.IsTrue(_pageModel.Users[0].IsAdmin);
        Assert.IsFalse(_pageModel.Users[1].IsAdmin);
    }

    [TestMethod]
    public async Task OnGetAsyncShouldIncludeLockedOutStatus()
    {
        var expectedUsers = new List<UserViewModel>
        {
            new() { Id = Guid.NewGuid(), Email = "locked@example.com", IsAdmin = false, IsLockedOut = true },
            new() { Id = Guid.NewGuid(), Email = "active@example.com", IsAdmin = false, IsLockedOut = false }
        };
        _mockUserQuery.GetAllUsersAsync().Returns(Task.FromResult(expectedUsers));

        await _pageModel.OnGetAsync();

        Assert.IsTrue(_pageModel.Users[0].IsLockedOut);
        Assert.IsFalse(_pageModel.Users[1].IsLockedOut);
    }
}
