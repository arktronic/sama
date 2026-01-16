using System.Text.Json;
using SAMA.Shared.Constants;
using SAMA.Web.Constants;
using SAMA.Web.Services;

namespace SAMA.Tests.Unit.Web.Services;

[TestClass]
public class SensitiveDataMaskingServiceTests
{
    private SensitiveDataMaskingService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new SensitiveDataMaskingService();
    }

    [TestMethod]
    public void MaskPasswordShouldReturnMaskedString()
    {
        var password = "SuperSecretPassword123";

        var result = _service.MaskPassword(password);

        Assert.AreEqual("••••••••", result);
    }

    [TestMethod]
    public void MaskPasswordShouldReturnEmptyStringWhenPasswordIsNull()
    {
        var result = _service.MaskPassword(null);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void MaskPasswordShouldReturnEmptyStringWhenPasswordIsEmpty()
    {
        var result = _service.MaskPassword(string.Empty);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void MaskUrlShouldShowOnlySchemeAndHostForValidUrl()
    {
        var url = "https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXX";

        var result = _service.MaskUrl(url);

        Assert.StartsWith("https://hooks.slack.com/", result, "Should preserve scheme and host");
        Assert.Contains("••••••••", result, "Should mask the path");
        Assert.DoesNotContain("XXXXXXXXXXXXXXXXXXXX", result, "Should not contain original token");
    }

    [TestMethod]
    public void MaskUrlShouldFullyMaskInvalidUrl()
    {
        var url = "not-a-valid-url";

        var result = _service.MaskUrl(url);

        Assert.AreEqual("••••••••", result);
    }

    [TestMethod]
    public void MaskUrlShouldReturnEmptyStringWhenUrlIsNull()
    {
        var result = _service.MaskUrl(null);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldMaskOnlyPasswordForEmail()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Email.SmtpHost] = JsonSerializer.SerializeToElement("smtp.gmail.com"),
            [ConfigurationKeys.Email.SmtpPort] = JsonSerializer.SerializeToElement(587),
            [ConfigurationKeys.Email.UseSsl] = JsonSerializer.SerializeToElement(true),
            [ConfigurationKeys.Email.Username] = JsonSerializer.SerializeToElement("test@example.com"),
            [ConfigurationKeys.Email.Password] = JsonSerializer.SerializeToElement("MyPassword123"),
            [ConfigurationKeys.Email.FromAddress] = JsonSerializer.SerializeToElement("alerts@example.com"),
            [ConfigurationKeys.Email.Recipients] = JsonSerializer.SerializeToElement(new[] { "admin@example.com", "ops@example.com" })
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Email, config);

        Assert.AreEqual("smtp.gmail.com", masked[ConfigurationKeys.Email.SmtpHost]);
        Assert.AreEqual("587", masked[ConfigurationKeys.Email.SmtpPort]);
        Assert.IsTrue((bool?)masked[ConfigurationKeys.Email.UseSsl]);
        Assert.AreEqual("test@example.com", masked[ConfigurationKeys.Email.Username]);
        Assert.AreEqual("••••••••", masked[ConfigurationKeys.Email.Password], "Password should be masked");
        Assert.AreEqual("alerts@example.com", masked[ConfigurationKeys.Email.FromAddress]);
        CollectionAssert.AreEqual(new[] { "admin@example.com", "ops@example.com" }, (object[])masked[ConfigurationKeys.Email.Recipients]);
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldHandleEmptyPasswordForEmail()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Email.SmtpHost] = JsonSerializer.SerializeToElement("smtp.gmail.com"),
            [ConfigurationKeys.Email.Password] = JsonSerializer.SerializeToElement("")
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Email, config);

        Assert.AreEqual("", masked[ConfigurationKeys.Email.Password], "Empty password should remain empty");
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldMaskWebhookUrlForSlack()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Webhook.WebhookUrl] = JsonSerializer.SerializeToElement("https://hooks.slack.com/services/T00000000/B00000000/XXXXXXXXXXXXXXXXXXXX")
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Slack, config);

        var maskedUrl = masked[ConfigurationKeys.Webhook.WebhookUrl]?.ToString();
        Assert.IsNotNull(maskedUrl);
        Assert.StartsWith("https://hooks.slack.com/", maskedUrl, "Should preserve scheme and host");
        Assert.Contains("••••••••", maskedUrl, "Should mask the path/token");
        Assert.DoesNotContain("XXXXXXXXXXXXXXXXXXXX", maskedUrl, "Should not contain original token");
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldMaskWebhookUrlForTeams()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Webhook.WebhookUrl] = JsonSerializer.SerializeToElement("https://example.webhook.office.com/webhookb2/token1@token2/IncomingWebhook/token3/token4")
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Teams, config);

        var maskedUrl = masked[ConfigurationKeys.Webhook.WebhookUrl]?.ToString();
        Assert.IsNotNull(maskedUrl);
        Assert.StartsWith("https://example.webhook.office.com/", maskedUrl, "Should preserve scheme and host");
        Assert.Contains("••••••••", maskedUrl, "Should mask the path/token");
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldMaskWebhookUrlForDiscord()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Webhook.WebhookUrl] = JsonSerializer.SerializeToElement("https://discord.com/api/webhooks/123456789/AbCdEfGhIjKlMnOpQrStUvWxYz")
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Discord, config);

        var maskedUrl = masked[ConfigurationKeys.Webhook.WebhookUrl]?.ToString();
        Assert.IsNotNull(maskedUrl);
        Assert.StartsWith("https://discord.com/", maskedUrl, "Should preserve scheme and host");
        Assert.Contains("••••••••", maskedUrl, "Should mask the path/token");
        Assert.DoesNotContain("AbCdEfGhIjKlMnOpQrStUvWxYz", maskedUrl, "Should not contain original token");
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldNotMaskAnythingForScript()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Script.Path] = JsonSerializer.SerializeToElement("/usr/local/bin/alert.sh"),
            [ConfigurationKeys.Script.Arguments] = JsonSerializer.SerializeToElement("--notify --level=critical")
        };

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Script, config);

        Assert.AreEqual("/usr/local/bin/alert.sh", masked[ConfigurationKeys.Script.Path]);
        Assert.AreEqual("--notify --level=critical", masked[ConfigurationKeys.Script.Arguments]);
    }

    [TestMethod]
    public void MaskCheckConfigShouldMaskAuthorizationHeaderForHttp()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.HttpCheck.Url] = JsonSerializer.SerializeToElement("https://api.example.com/health"),
            [ConfigurationKeys.HttpCheck.Headers] = JsonSerializer.SerializeToElement(new Dictionary<string, string>
            {
                ["Authorization"] = "Bearer secret-token-12345",
                ["User-Agent"] = "SAMA-Monitor"
            })
        };

        var masked = _service.MaskCheckConfig(CheckTypes.Http, config);

        Assert.AreEqual("https://api.example.com/health", masked[ConfigurationKeys.HttpCheck.Url]);
        var headers = masked[ConfigurationKeys.HttpCheck.Headers] as Dictionary<string, string>;
        Assert.IsNotNull(headers);
        Assert.AreEqual("••••••••", headers["Authorization"], "Authorization header should be masked");
        Assert.AreEqual("SAMA-Monitor", headers["User-Agent"], "Other headers should remain");
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldReturnEmptyDictionaryForEmptyConfiguration()
    {
        var config = new Dictionary<string, JsonElement>();

        var masked = _service.MaskNotificationChannelConfig("Email", config);

        Assert.IsEmpty(masked);
    }

    [TestMethod]
    public void MaskNotificationChannelConfigShouldNotModifyOriginalDictionary()
    {
        var config = new Dictionary<string, JsonElement>
        {
            [ConfigurationKeys.Email.Password] = JsonSerializer.SerializeToElement("OriginalPassword")
        };
        var originalPasswordElement = config[ConfigurationKeys.Email.Password];

        var masked = _service.MaskNotificationChannelConfig(ChannelTypes.Email, config);

        Assert.AreEqual("OriginalPassword", config[ConfigurationKeys.Email.Password].GetString(), "Original dictionary should not be modified");
        Assert.AreEqual("••••••••", masked[ConfigurationKeys.Email.Password], "Masked dictionary should have masked password");
    }
}
