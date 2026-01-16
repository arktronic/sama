using System.Text.Json;
using SAMA.Shared.Constants;
using SAMA.Shared.Utilities;
using SAMA.Web.Constants;

namespace SAMA.Web.Services;

/// <summary>
/// Service for masking sensitive data across the application.
/// Provides safe display of configuration details by redacting passwords, API keys, tokens, and URLs.
/// </summary>
public class SensitiveDataMaskingService
{
    private const string MaskString = "••••••••";

    /// <summary>
    /// Masks a password by replacing it with bullet characters.
    /// </summary>
    /// <param name="password">The password to mask</param>
    /// <returns>Masked password or empty string if null/empty</returns>
    public virtual string MaskPassword(string? password)
    {
        return string.IsNullOrEmpty(password) ? string.Empty : MaskString;
    }

    /// <summary>
    /// Masks a URL by showing only the scheme and host, hiding the path and query parameters.
    /// </summary>
    /// <param name="url">The URL to mask</param>
    /// <returns>Masked URL showing only scheme and host, or fully masked if parsing fails</returns>
    public virtual string MaskUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        try
        {
            var uri = new Uri(url);

            // Show scheme and host, mask path to protect webhook tokens
            return $"{uri.Scheme}://{uri.Host}/{MaskString}";
        }
        catch
        {
            // If URL parsing fails, just mask the entire thing
            return MaskString;
        }
    }

    /// <summary>
    /// Masks sensitive data in a notification channel configuration based on channel type.
    /// </summary>
    /// <param name="channelType">The type of notification channel</param>
    /// <param name="config">The configuration dictionary to mask</param>
    /// <returns>A new dictionary with sensitive values masked</returns>
    public virtual Dictionary<string, object> MaskNotificationChannelConfig(string channelType, Dictionary<string, JsonElement> config)
    {
        // Convert all JsonElement values to regular objects for display
        var masked = JsonElementHelper.ConvertToDisplayObjectDictionary(config);

        switch (channelType)
        {
            case ChannelTypes.Email:
                // Mask password but show other settings
                if (masked.ContainsKey(ConfigurationKeys.Email.Password))
                {
                    var password = JsonElementHelper.GetString(config, ConfigurationKeys.Email.Password);
                    if (!string.IsNullOrEmpty(password))
                    {
                        masked[ConfigurationKeys.Email.Password] = MaskPassword(password);
                    }
                }
                break;

            case ChannelTypes.Slack:
                if (masked.ContainsKey(ConfigurationKeys.Webhook.WebhookUrl))
                {
                    var url = JsonElementHelper.GetString(config, ConfigurationKeys.Webhook.WebhookUrl);
                    masked[ConfigurationKeys.Webhook.WebhookUrl] = MaskUrl(url);
                }
                break;

            case ChannelTypes.Teams:
                if (masked.ContainsKey(ConfigurationKeys.Webhook.WebhookUrl))
                {
                    var url = JsonElementHelper.GetString(config, ConfigurationKeys.Webhook.WebhookUrl);
                    masked[ConfigurationKeys.Webhook.WebhookUrl] = MaskUrl(url);
                }
                break;

            case ChannelTypes.Discord:
                if (masked.ContainsKey(ConfigurationKeys.Webhook.WebhookUrl))
                {
                    var url = JsonElementHelper.GetString(config, ConfigurationKeys.Webhook.WebhookUrl);
                    masked[ConfigurationKeys.Webhook.WebhookUrl] = MaskUrl(url);
                }
                break;

            case ChannelTypes.Script:
                // Script paths are shown, but we could mask arguments if they contain sensitive data
                // For now, show everything as script arguments are typically flags/parameters
                break;

            case ChannelTypes.EventGrid:
                if (masked.ContainsKey(ConfigurationKeys.EventGrid.AccessKey))
                {
                    var accessKey = JsonElementHelper.GetString(config, ConfigurationKeys.EventGrid.AccessKey);
                    if (!string.IsNullOrEmpty(accessKey))
                    {
                        masked[ConfigurationKeys.EventGrid.AccessKey] = MaskPassword(accessKey);
                    }
                }
                break;
        }

        return masked;
    }

    /// <summary>
    /// Masks sensitive data in a check configuration based on check type.
    /// Future use for when check configuration display is implemented.
    /// </summary>
    /// <param name="checkType">The type of check</param>
    /// <param name="config">The configuration dictionary to mask</param>
    /// <returns>A new dictionary with sensitive values masked</returns>
    public virtual Dictionary<string, object> MaskCheckConfig(string checkType, Dictionary<string, JsonElement> config)
    {
        // Convert all JsonElement values to regular objects for display
        var masked = JsonElementHelper.ConvertToDisplayObjectDictionary(config);

        switch (checkType)
        {
            case CheckTypes.Http:
                // Mask Authorization headers
                if (config.TryGetValue(ConfigurationKeys.HttpCheck.Headers, out var headersElement) && headersElement.ValueKind == JsonValueKind.Object)
                {
                    var maskedHeaders = new Dictionary<string, string>();
                    foreach (var prop in headersElement.EnumerateObject())
                    {
                        if (prop.Name == "Authorization")
                        {
                            maskedHeaders[prop.Name] = MaskString;
                        }
                        else
                        {
                            maskedHeaders[prop.Name] = prop.Value.GetString() ?? "";
                        }
                    }
                    masked[ConfigurationKeys.HttpCheck.Headers] = maskedHeaders;
                }
                break;

            // Other check types (Tcp, Ping, Dns, etc.) typically don't have sensitive data
        }

        return masked;
    }
}
