using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SAMA.Tests.Unit.TestUtilities;
using SAMA.Web.Pages.Admin;
using SAMA.Web.Services;

namespace SAMA.Tests.Unit.Web.Pages.Admin;

[TestClass]
public class SettingsModelTests
{
    private GlobalSettingsService _mockGlobalSettings = null!;
    private ILogger<SettingsModel> _mockLogger = null!;
    private SettingsModel _pageModel = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockGlobalSettings = Substitute.For<GlobalSettingsService>(null!, null!);
        _mockLogger = Substitute.For<ILogger<SettingsModel>>();

        _pageModel = new SettingsModel(_mockGlobalSettings, _mockLogger);
        PageModelTestHelpers.ConfigurePageModel(_pageModel);
    }

    [TestMethod]
    public void OnGetShouldLoadCurrentSettings()
    {
        _mockGlobalSettings.CheckResultsRetentionDays.Returns(180);
        _mockGlobalSettings.AlertHistoryRetentionDays.Returns(90);
        _mockGlobalSettings.AuditLogRetentionDays.Returns(730);
        _mockGlobalSettings.DashboardRefreshIntervalSeconds.Returns(10);
        _mockGlobalSettings.MaxRecentAlerts.Returns(50);
        _mockGlobalSettings.DefaultCheckTimeoutSeconds.Returns(60);
        _mockGlobalSettings.NotificationTimeoutSeconds.Returns(45);

        _pageModel.OnGet();

        Assert.AreEqual(180, _pageModel.Input.CheckResultsRetentionDays);
        Assert.AreEqual(90, _pageModel.Input.AlertHistoryRetentionDays);
        Assert.AreEqual(730, _pageModel.Input.AuditLogRetentionDays);
        Assert.AreEqual(10, _pageModel.Input.DashboardRefreshIntervalSeconds);
        Assert.AreEqual(50, _pageModel.Input.MaxRecentAlerts);
        Assert.AreEqual(60, _pageModel.Input.DefaultCheckTimeoutSeconds);
        Assert.AreEqual(45, _pageModel.Input.NotificationTimeoutSeconds);
    }

    [TestMethod]
    public void OnPostShouldUpdateAllSettingsWhenModelStateIsValid()
    {
        _pageModel.Input = new SettingsModel.InputModel
        {
            CheckResultsRetentionDays = 180,
            AlertHistoryRetentionDays = 90,
            AuditLogRetentionDays = 730,
            DashboardRefreshIntervalSeconds = 10,
            MaxRecentAlerts = 50,
            DefaultCheckTimeoutSeconds = 60,
            NotificationTimeoutSeconds = 45
        };

        var result = _pageModel.OnPost();

        Assert.IsInstanceOfType<PageResult>(result);
        _mockGlobalSettings.Received(1).CheckResultsRetentionDays = 180;
        _mockGlobalSettings.Received(1).AlertHistoryRetentionDays = 90;
        _mockGlobalSettings.Received(1).AuditLogRetentionDays = 730;
        _mockGlobalSettings.Received(1).DashboardRefreshIntervalSeconds = 10;
        _mockGlobalSettings.Received(1).MaxRecentAlerts = 50;
        _mockGlobalSettings.Received(1).DefaultCheckTimeoutSeconds = 60;
        _mockGlobalSettings.Received(1).NotificationTimeoutSeconds = 45;
    }

    [TestMethod]
    public void OnPostShouldSetSuccessMessageWhenSaveSucceeds()
    {
        _pageModel.Input = new SettingsModel.InputModel
        {
            CheckResultsRetentionDays = 365,
            AlertHistoryRetentionDays = 365,
            AuditLogRetentionDays = 365,
            DashboardRefreshIntervalSeconds = 5,
            MaxRecentAlerts = 20,
            DefaultCheckTimeoutSeconds = 30,
            NotificationTimeoutSeconds = 30
        };

        _pageModel.OnPost();

        Assert.AreEqual("Settings updated successfully.", _pageModel.TempData["SuccessMessage"]);
    }

    [TestMethod]
    public void OnPostShouldReturnPageWhenModelStateIsInvalid()
    {
        _pageModel.ModelState.AddModelError("Input.CheckResultsRetentionDays", "Required");

        var result = _pageModel.OnPost();

        Assert.IsInstanceOfType<PageResult>(result);
        _mockGlobalSettings.DidNotReceive().CheckResultsRetentionDays = Arg.Any<int>();
    }

    [TestMethod]
    public void OnPostShouldAddModelErrorWhenExceptionOccurs()
    {
        _pageModel.Input = new SettingsModel.InputModel
        {
            CheckResultsRetentionDays = 365,
            AlertHistoryRetentionDays = 365,
            AuditLogRetentionDays = 365,
            DashboardRefreshIntervalSeconds = 5,
            MaxRecentAlerts = 20,
            DefaultCheckTimeoutSeconds = 30,
            NotificationTimeoutSeconds = 30
        };

        _mockGlobalSettings.When(x => x.CheckResultsRetentionDays = Arg.Any<int>())
            .Do(_ => throw new Exception("Database error"));

        var result = _pageModel.OnPost();

        Assert.IsInstanceOfType<PageResult>(result);
        Assert.IsGreaterThan(0, _pageModel.ModelState.ErrorCount);
        Assert.IsNull(_pageModel.TempData["SuccessMessage"]);
    }

    [TestMethod]
    public void OnPostShouldHandleMinimumValidValues()
    {
        _pageModel.Input = new SettingsModel.InputModel
        {
            CheckResultsRetentionDays = 30,
            AlertHistoryRetentionDays = 30,
            AuditLogRetentionDays = 30,
            DashboardRefreshIntervalSeconds = 1,
            MaxRecentAlerts = 10,
            DefaultCheckTimeoutSeconds = 5,
            NotificationTimeoutSeconds = 5
        };

        var result = _pageModel.OnPost();

        Assert.IsInstanceOfType<PageResult>(result);
        _mockGlobalSettings.Received(1).CheckResultsRetentionDays = 30;
        _mockGlobalSettings.Received(1).DashboardRefreshIntervalSeconds = 1;
    }

    [TestMethod]
    public void OnPostShouldHandleMaximumValidValues()
    {
        _pageModel.Input = new SettingsModel.InputModel
        {
            CheckResultsRetentionDays = 3650,
            AlertHistoryRetentionDays = 3650,
            AuditLogRetentionDays = 3650,
            DashboardRefreshIntervalSeconds = 300,
            MaxRecentAlerts = 500,
            DefaultCheckTimeoutSeconds = 300,
            NotificationTimeoutSeconds = 300
        };

        var result = _pageModel.OnPost();

        Assert.IsInstanceOfType<PageResult>(result);
        _mockGlobalSettings.Received(1).CheckResultsRetentionDays = 3650;
        _mockGlobalSettings.Received(1).DashboardRefreshIntervalSeconds = 300;
    }
}
