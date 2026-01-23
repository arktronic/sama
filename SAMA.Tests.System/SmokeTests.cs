namespace SAMA.Tests.System;

[TestClass]
[SystemTestCondition]
public class SmokeTests : SystemTestBase
{
    [TestMethod]
    public async Task ShouldLoadInitialSetupPage()
    {
        await Page.GotoAsync(BaseUrl);

        var title = await Page.TitleAsync();

        Assert.AreEqual("Initial Setup - SAMA", title);
    }
}
