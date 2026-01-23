namespace SAMA.Tests.System;

[TestClass]
public class AuthenticationTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldRegisterAndLoginWithRegisteredCredentials()
    {
        var email = "testadmin@example.com";
        var password = "SecurePassword123!";

        await Page.GotoAsync(BaseUrl);
        await Page.WaitForURLAsync($"{BaseUrl}/Setup");

        await Page.FillAsync("input[name='Input.Email']", email);
        await Page.FillAsync("input[name='Input.Password']", password);
        await Page.FillAsync("input[name='Input.ConfirmPassword']", password);
        await Page.ClickAsync("button[type='submit']");

        await Page.WaitForURLAsync(BaseUrl);
        var titleAfterSetup = await Page.TitleAsync();
        Assert.AreEqual("Welcome - SAMA", titleAfterSetup);

        await LoginAsync(email, password);

        await Page.WaitForURLAsync($"{BaseUrl}/Dashboard**");
        var titleAfterLogin = await Page.TitleAsync();
        Assert.AreEqual("Dashboard - SAMA", titleAfterLogin);

        await Page.GotoAsync($"{BaseUrl}/Account/Logout");

        await Page.WaitForURLAsync(BaseUrl);
        var titleAfterLogout = await Page.TitleAsync();
        Assert.AreEqual("Welcome - SAMA", titleAfterLogout);
    }
}
