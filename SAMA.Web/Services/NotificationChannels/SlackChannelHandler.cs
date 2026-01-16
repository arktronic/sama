using System.Text;
using System.Text.Json;
using SAMA.Data.Entities;
using SAMA.Shared.Constants;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.NotificationChannels;

[ChannelType(ChannelTypes.Slack)]
public class SlackChannelHandler(
    IHttpClientFactory _httpClientFactory,
    GlobalSettingsService _globalSettings,
    ILogger<SlackChannelHandler> _logger) : INotificationChannelHandler
{
    public async Task<NotificationResultModel> SendStatusAlertAsync(
        NotificationChannel channel,
        StatusAlertContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusAlertPayload(context);
        return await SendSlackMessageAsync(channel, payload, context.CheckId, "notification", cancellationToken);
    }

    public async Task<NotificationResultModel> SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildLifecycleEventPayload(context);
        return await SendSlackMessageAsync(channel, payload, context.CheckId, "lifecycle event", cancellationToken);
    }

    public async Task<NotificationResultModel> SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusChangeEventPayload(context);
        return await SendSlackMessageAsync(channel, payload, context.CheckId, "status change event", cancellationToken);
    }

    private static object BuildStatusAlertPayload(StatusAlertContext context)
    {
        var (emoji, color, statusIndicator, statusName) = context.Status switch
        {
            CheckStatuses.Up => (":white_check_mark:", "#2eb886", "🟢", "Up"),
            CheckStatuses.Warn => (":warning:", "#daa038", "🟡", "Degraded"),
            CheckStatuses.Down => (":x:", "#a30200", "🔴", "Down"),
            _ => (":grey_question:", "#808080", "⚪", "Unknown")
        };

        string headerText;
        string statusText;

        if (context.IsRecovery)
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            headerText = $"{emoji} {context.CheckName} is back up";
            statusText = context.ConsecutiveFailures > 1
                ? $"Recovered after *{failureCountDisplay} consecutive failures*"
                : "Recovered";
        }
        else
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            headerText = $"{emoji} {context.CheckName} is {statusName.ToLowerInvariant()}";

            if (context.Status == CheckStatuses.Warn)
            {
                statusText = context.ConsecutiveFailures > 1
                    ? $"Reported degraded *{failureCountDisplay} consecutive times*"
                    : "Check reported degraded";
            }
            else if (context.Status == CheckStatuses.Down)
            {
                statusText = context.ConsecutiveFailures > 1
                    ? $"Failed *{failureCountDisplay} consecutive times*"
                    : "Check failed";
            }
            else
            {
                statusText = "Check completed";
            }
        }

        var blocks = new List<object>
        {
            new
            {
                type = "header",
                text = new
                {
                    type = "plain_text",
                    text = headerText,
                    emoji = true
                }
            },
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"{statusIndicator} *{statusName}* • {statusText}"
                }
            }
        };

        if (context.ResponseTimeMs.HasValue)
        {
            var fields = new List<object>
            {
                new
                {
                    type = "mrkdwn",
                    text = $"*Response Time:*\n{context.ResponseTimeMs}ms"
                }
            };

            blocks.Add(new
            {
                type = "section",
                fields
            });
        }

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            blocks.Add(new { type = "divider" });
            blocks.Add(new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"*Error:*\n```{context.ErrorMessage}```"
                }
            });
        }

        blocks.Add(new { type = "divider" });
        blocks.Add(new
        {
            type = "context",
            elements = new object[]
            {
                new
                {
                    type = "mrkdwn",
                    text = $"SAMA • {context.WorkspaceName} • <!date^{context.Timestamp.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{context.Timestamp:yyyy-MM-dd HH:mm:ss}>"
                }
            }
        });

        return new
        {
            blocks,
            attachments = new[]
            {
                new
                {
                    color,
                    blocks = Array.Empty<object>()
                }
            }
        };
    }

    private static object BuildLifecycleEventPayload(LifecycleEventContext context)
    {
        var (emoji, color, statusIndicator) = context.EventType switch
        {
            EventTypes.CheckCreated => (":sparkles:", "#2eb886", "🆕"),
            EventTypes.CheckUpdated => (":arrows_counterclockwise:", "#439FE0", "🔄"),
            EventTypes.CheckDeleted => (":wastebasket:", "#a30200", "🗑️"),
            _ => (":information_source:", "#808080", "ℹ️")
        };

        var (headerText, descriptionText) = context.EventType switch
        {
            EventTypes.CheckCreated => (
                $"{emoji} New {CheckTypes.GetShortDisplayName(context.CheckType)} check created",
                $"{statusIndicator} *{context.CheckName}*\nCreated by {context.PerformedBy}"
            ),
            EventTypes.CheckUpdated => (
                $"{emoji} Check updated",
                $"{statusIndicator} *{context.CheckName}*\nModified by {context.PerformedBy}"
            ),
            EventTypes.CheckDeleted => (
                $"{emoji} Check deleted",
                $"{statusIndicator} *{context.CheckName}*\nRemoved by {context.PerformedBy}"
            ),
            _ => (
                $"{emoji} Check {context.EventType.ToLowerInvariant()}",
                $"{statusIndicator} *{context.CheckName}*\nAction performed by {context.PerformedBy}"
            )
        };

        var fields = new List<object>
        {
            new
            {
                type = "mrkdwn",
                text = $"*Workspace:*\n{context.WorkspaceName}"
            },
            new
            {
                type = "mrkdwn",
                text = $"*Check Type:*\n{CheckTypes.GetShortDisplayName(context.CheckType)}"
            }
        };
        var blocks = new List<object>
        {
            new
            {
                type = "header",
                text = new
                {
                    type = "plain_text",
                    text = headerText,
                    emoji = true
                }
            },
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = descriptionText
                }
            },
            new
            {
                type = "section",
                fields
            }
        };

        if (context.ConfigurationChanges != null && context.ConfigurationChanges.Count > 0)
        {
            var changes = string.Join(", ", context.ConfigurationChanges.Keys);
            blocks.Add(new { type = "divider" });
            blocks.Add(new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"*Changed Fields:*\n{changes}"
                }
            });
        }

        blocks.Add(new { type = "divider" });
        blocks.Add(new
        {
            type = "context",
            elements = new object[]
            {
                new
                {
                    type = "mrkdwn",
                    text = $"SAMA • {context.WorkspaceName} • <!date^{context.Timestamp.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{context.Timestamp:yyyy-MM-dd HH:mm:ss}>"
                }
            }
        });

        return new
        {
            blocks,
            attachments = new[]
            {
                new
                {
                    color,
                    blocks = Array.Empty<object>()
                }
            }
        };
    }

    private static object BuildStatusChangeEventPayload(StatusChangeEventContext context)
    {
        var (emoji, color, statusIndicator) = context.NewStatus switch
        {
            CheckStatuses.Up => (":white_check_mark:", "#2eb886", "🟢"),
            CheckStatuses.Warn => (":warning:", "#daa038", "🟡"),
            CheckStatuses.Down => (":x:", "#a30200", "🔴"),
            _ => (":grey_question:", "#808080", "⚪")
        };

        var headerText = $"{emoji} {context.CheckName} status changed";
        var statusText = $"{statusIndicator} Status changed from *{context.PreviousStatus}* to *{context.NewStatus}*";

        var blocks = new List<object>
        {
            new
            {
                type = "header",
                text = new
                {
                    type = "plain_text",
                    text = headerText,
                    emoji = true
                }
            },
            new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = statusText
                }
            }
        };

        var fields = new List<object>
        {
            new
            {
                type = "mrkdwn",
                text = $"*Previous Status:*\n{context.PreviousStatus}"
            },
            new
            {
                type = "mrkdwn",
                text = $"*New Status:*\n{context.NewStatus}"
            }
        };

        if (context.ResponseTimeMs.HasValue)
        {
            fields.Add(new
            {
                type = "mrkdwn",
                text = $"*Response Time:*\n{context.ResponseTimeMs}ms"
            });
        }

        blocks.Add(new
        {
            type = "section",
            fields
        });

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            blocks.Add(new { type = "divider" });
            blocks.Add(new
            {
                type = "section",
                text = new
                {
                    type = "mrkdwn",
                    text = $"*Error:*\n```{context.ErrorMessage}```"
                }
            });
        }

        blocks.Add(new { type = "divider" });
        blocks.Add(new
        {
            type = "context",
            elements = new object[]
            {
                new
                {
                    type = "mrkdwn",
                    text = $"SAMA • {context.WorkspaceName} • <!date^{context.Timestamp.ToUnixTimeSeconds()}^{{date_num}} {{time_secs}}|{context.Timestamp:yyyy-MM-dd HH:mm:ss}>"
                }
            }
        });

        return new
        {
            blocks,
            attachments = new[]
            {
                new
                {
                    color,
                    blocks = Array.Empty<object>()
                }
            }
        };
    }

    private async Task<NotificationResultModel> SendSlackMessageAsync(
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
                    "Slack {MessageType} sent successfully for check {CheckId} via channel {ChannelId}",
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
            var errorMessage = $"Slack API returned {response.StatusCode}: {errorBody}";

            _logger.LogWarning(
                "Failed to send Slack {MessageType} for check {CheckId}: {Error}",
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
                "HTTP error sending Slack {MessageType} for check {CheckId}",
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
                "Slack {MessageType} cancelled for check {CheckId}",
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
                "Slack {MessageType} timeout ({TimeoutSeconds}s) for check {CheckId}",
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
                "Unexpected error sending Slack {MessageType} for check {CheckId}",
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
