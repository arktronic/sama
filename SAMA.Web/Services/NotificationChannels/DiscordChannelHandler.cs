using System.Text;
using System.Text.Json;
using SAMA.Data.Entities;
using SAMA.Shared.Constants;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.NotificationChannels;

[ChannelType(ChannelTypes.Discord)]
public class DiscordChannelHandler(
    IHttpClientFactory _httpClientFactory,
    GlobalSettingsService _globalSettings,
    ILogger<DiscordChannelHandler> _logger) : INotificationChannelHandler
{
    public async Task<NotificationResultModel> SendStatusAlertAsync(
        NotificationChannel channel,
        StatusAlertContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusAlertPayload(context);
        return await SendDiscordMessageAsync(channel, payload, context.CheckId, "notification", cancellationToken);
    }

    public async Task<NotificationResultModel> SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildLifecycleEventPayload(context);
        return await SendDiscordMessageAsync(channel, payload, context.CheckId, "lifecycle event", cancellationToken);
    }

    public async Task<NotificationResultModel> SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusChangeEventPayload(context);
        return await SendDiscordMessageAsync(channel, payload, context.CheckId, "status change event", cancellationToken);
    }

    private static object BuildStatusAlertPayload(StatusAlertContext context)
    {
        var (emoji, color, statusName) = context.Status switch
        {
            CheckStatuses.Up => ("✅", 0x2eb886, "Up"),
            CheckStatuses.Warn => ("⚠️", 0xdaa038, "Degraded"),
            CheckStatuses.Down => ("❌", 0xa30200, "Down"),
            _ => ("❔", 0x808080, "Unknown")
        };

        string title;
        string description;

        if (context.IsRecovery)
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            title = $"{emoji} {context.CheckName} is back up";
            description = context.ConsecutiveFailures > 1
                ? $"Recovered after **{failureCountDisplay} consecutive failures**"
                : "Recovered";
        }
        else
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            title = $"{emoji} {context.CheckName} is {statusName.ToLowerInvariant()}";

            if (context.Status == CheckStatuses.Warn)
            {
                description = context.ConsecutiveFailures > 1
                    ? $"Reported degraded **{failureCountDisplay} consecutive times**"
                    : "Check reported degraded";
            }
            else if (context.Status == CheckStatuses.Down)
            {
                description = context.ConsecutiveFailures > 1
                    ? $"Failed **{failureCountDisplay} consecutive times**"
                    : "Check failed";
            }
            else
            {
                description = "Check completed";
            }
        }

        var fields = new List<object>();

        if (context.ResponseTimeMs.HasValue)
        {
            fields.Add(new
            {
                name = "Response Time",
                value = $"{context.ResponseTimeMs}ms",
                inline = true
            });
        }

        fields.Add(new
        {
            name = "Status",
            value = statusName,
            inline = true
        });

        fields.Add(new
        {
            name = "Workspace",
            value = context.WorkspaceName,
            inline = true
        });

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            fields.Add(new
            {
                name = "Error",
                value = $"```\n{TruncateErrorMessage(context.ErrorMessage)}\n```",
                inline = false
            });
        }

        return new
        {
            embeds = new[]
            {
                new
                {
                    title,
                    description,
                    color,
                    fields,
                    footer = new
                    {
                        text = $"SAMA • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC"
                    },
                    timestamp = context.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            }
        };
    }

    private static object BuildLifecycleEventPayload(LifecycleEventContext context)
    {
        var (emoji, color) = context.EventType switch
        {
            EventTypes.CheckCreated => ("✨", 0x2eb886),
            EventTypes.CheckUpdated => ("🔄", 0x439FE0),
            EventTypes.CheckDeleted => ("🗑️", 0xa30200),
            _ => ("ℹ️", 0x808080)
        };

        var (title, description) = context.EventType switch
        {
            EventTypes.CheckCreated => (
                $"{emoji} New {CheckTypes.GetShortDisplayName(context.CheckType)} check created",
                $"**{context.CheckName}**\nCreated by {context.PerformedBy}"
            ),
            EventTypes.CheckUpdated => (
                $"{emoji} Check updated",
                $"**{context.CheckName}**\nModified by {context.PerformedBy}"
            ),
            EventTypes.CheckDeleted => (
                $"{emoji} Check deleted",
                $"**{context.CheckName}**\nRemoved by {context.PerformedBy}"
            ),
            _ => (
                $"{emoji} Check {context.EventType.ToLowerInvariant()}",
                $"**{context.CheckName}**\nAction performed by {context.PerformedBy}"
            )
        };

        var fields = new List<object>
        {
            new
            {
                name = "Workspace",
                value = context.WorkspaceName,
                inline = true
            },
            new
            {
                name = "Check Type",
                value = CheckTypes.GetShortDisplayName(context.CheckType),
                inline = true
            }
        };

        if (context.ConfigurationChanges != null && context.ConfigurationChanges.Count > 0)
        {
            var changes = string.Join(", ", context.ConfigurationChanges.Keys);
            fields.Add(new
            {
                name = "Changed Fields",
                value = changes,
                inline = false
            });
        }

        return new
        {
            embeds = new[]
            {
                new
                {
                    title,
                    description,
                    color,
                    fields,
                    footer = new
                    {
                        text = $"SAMA • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC"
                    },
                    timestamp = context.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            }
        };
    }

    private static object BuildStatusChangeEventPayload(StatusChangeEventContext context)
    {
        var (emoji, color) = context.NewStatus switch
        {
            CheckStatuses.Up => ("✅", 0x2eb886),
            CheckStatuses.Warn => ("⚠️", 0xdaa038),
            CheckStatuses.Down => ("❌", 0xa30200),
            _ => ("❔", 0x808080)
        };

        var title = $"{emoji} {context.CheckName} status changed";
        var description = $"Status changed from **{context.PreviousStatus}** to **{context.NewStatus}**";

        var fields = new List<object>
        {
            new
            {
                name = "Previous Status",
                value = context.PreviousStatus,
                inline = true
            },
            new
            {
                name = "New Status",
                value = context.NewStatus,
                inline = true
            },
            new
            {
                name = "Workspace",
                value = context.WorkspaceName,
                inline = true
            }
        };

        if (context.ResponseTimeMs.HasValue)
        {
            fields.Insert(0, new
            {
                name = "Response Time",
                value = $"{context.ResponseTimeMs}ms",
                inline = true
            });
        }

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            fields.Add(new
            {
                name = "Error",
                value = $"```\n{TruncateErrorMessage(context.ErrorMessage)}\n```",
                inline = false
            });
        }

        return new
        {
            embeds = new[]
            {
                new
                {
                    title,
                    description,
                    color,
                    fields,
                    footer = new
                    {
                        text = $"SAMA • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC"
                    },
                    timestamp = context.Timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
                }
            }
        };
    }

    private static string TruncateErrorMessage(string errorMessage, int maxLength = 1000)
    {
        if (errorMessage.Length <= maxLength)
        {
            return errorMessage;
        }

        return errorMessage[..(maxLength - 3)] + "...";
    }

    private async Task<NotificationResultModel> SendDiscordMessageAsync(
        NotificationChannel channel,
        object payload,
        Guid checkId,
        string messageType,
        CancellationToken cancellationToken)
    {
        var sentAt = DateTimeOffset.UtcNow;

        try
        {
            if (!channel.ConfigurationJson.TryGetValue(ConfigurationKeys.Webhook.WebhookUrl, out var webhookUrlElement))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Webhook URL not configured",
                    SentAt = sentAt
                };
            }

            var webhookUrl = webhookUrlElement.GetString();

            if (string.IsNullOrWhiteSpace(webhookUrl))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Webhook URL not configured",
                    SentAt = sentAt
                };
            }

            var jsonContent = JsonSerializer.Serialize(payload);

            var timeoutSeconds = _globalSettings.NotificationTimeoutSeconds;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var httpClient = _httpClientFactory.CreateClient();
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, content, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Discord {MessageType} sent successfully for check {CheckId} via channel {ChannelId}",
                    messageType,
                    checkId,
                    channel.Id);

                return new NotificationResultModel
                {
                    Success = true,
                    SentAt = sentAt
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
            var errorMessage = $"Discord API returned {response.StatusCode}: {errorBody}";

            _logger.LogWarning(
                "Failed to send Discord {MessageType} for check {CheckId}: {Error}",
                messageType,
                checkId,
                errorMessage);

            return new NotificationResultModel
            {
                Success = false,
                ErrorMessage = errorMessage,
                SentAt = sentAt
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error sending Discord {MessageType} for check {CheckId}",
                messageType,
                checkId);

            return new NotificationResultModel
            {
                Success = false,
                ErrorMessage = $"HTTP error: {ex.Message}",
                SentAt = sentAt
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Discord {MessageType} cancelled for check {CheckId}",
                messageType,
                checkId);

            return new NotificationResultModel
            {
                Success = false,
                ErrorMessage = "Request cancelled",
                SentAt = sentAt
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Discord {MessageType} timeout ({TimeoutSeconds}s) for check {CheckId}",
                messageType,
                _globalSettings.NotificationTimeoutSeconds,
                checkId);

            return new NotificationResultModel
            {
                Success = false,
                ErrorMessage = $"Request timeout ({_globalSettings.NotificationTimeoutSeconds}s)",
                SentAt = sentAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error sending Discord {MessageType} for check {CheckId}",
                messageType,
                checkId);

            return new NotificationResultModel
            {
                Success = false,
                ErrorMessage = $"Unexpected error: {ex.Message}",
                SentAt = sentAt
            };
        }
    }
}
