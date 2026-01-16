using System.Text;
using System.Text.Json;
using SAMA.Data.Entities;
using SAMA.Shared.Constants;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.NotificationChannels;

[ChannelType(ChannelTypes.Teams)]
public class TeamsChannelHandler(
    IHttpClientFactory _httpClientFactory,
    GlobalSettingsService _globalSettings,
    ILogger<TeamsChannelHandler> _logger) : INotificationChannelHandler
{
    public async Task<NotificationResultModel> SendStatusAlertAsync(
        NotificationChannel channel,
        StatusAlertContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusAlertPayload(context);
        return await SendTeamsMessageAsync(channel, payload, context.CheckId, "notification", cancellationToken);
    }

    public async Task<NotificationResultModel> SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildLifecycleEventPayload(context);
        return await SendTeamsMessageAsync(channel, payload, context.CheckId, "lifecycle event", cancellationToken);
    }

    public async Task<NotificationResultModel> SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default)
    {
        var payload = BuildStatusChangeEventPayload(context);
        return await SendTeamsMessageAsync(channel, payload, context.CheckId, "status change event", cancellationToken);
    }

    private static object BuildStatusAlertPayload(StatusAlertContext context)
    {
        var (color, statusName) = context.Status switch
        {
            CheckStatuses.Up => ("good", "Up"),
            CheckStatuses.Warn => ("warning", "Degraded"),
            CheckStatuses.Down => ("attention", "Down"),
            _ => ("default", "Unknown")
        };

        string title;
        string subtitle;

        if (context.IsRecovery)
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            title = $"✅ {context.CheckName} is back up";
            subtitle = context.ConsecutiveFailures > 1
                ? $"Recovered after **{failureCountDisplay} consecutive failures**"
                : "Recovered";
        }
        else
        {
            var failureCountDisplay = context.ConsecutiveFailures >= AlertConstants.ConsecutiveFailureQueryLimit
                ? $"{AlertConstants.ConsecutiveFailureQueryLimit}+"
                : context.ConsecutiveFailures.ToString();

            var emoji = context.Status switch
            {
                CheckStatuses.Up => "✅",
                CheckStatuses.Warn => "⚠️",
                CheckStatuses.Down => "❌",
                _ => "❔"
            };

            title = $"{emoji} {context.CheckName} is {statusName.ToLowerInvariant()}";

            if (context.Status == CheckStatuses.Warn)
            {
                subtitle = context.ConsecutiveFailures > 1
                    ? $"Reported degraded **{failureCountDisplay} consecutive times**"
                    : "Check reported degraded";
            }
            else if (context.Status == CheckStatuses.Down)
            {
                subtitle = context.ConsecutiveFailures > 1
                    ? $"Failed **{failureCountDisplay} consecutive times**"
                    : "Check failed";
            }
            else
            {
                subtitle = "Check completed";
            }
        }

        var facts = new List<object>
        {
            new { title = "Status", value = statusName },
            new { title = "Workspace", value = context.WorkspaceName }
        };

        if (context.ResponseTimeMs.HasValue)
        {
            facts.Insert(0, new { title = "Response Time", value = $"{context.ResponseTimeMs}ms" });
        }

        var bodyElements = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
                weight = "bolder",
                size = "large",
                wrap = true
            },
            new
            {
                type = "TextBlock",
                text = subtitle,
                wrap = true,
                spacing = "small"
            },
            new
            {
                type = "FactSet",
                facts,
                spacing = "medium"
            }
        };

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = "**Error:**",
                weight = "bolder",
                spacing = "medium"
            });
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = context.ErrorMessage,
                wrap = true,
                fontType = "monospace",
                spacing = "small"
            });
        }

        bodyElements.Add(new
        {
            type = "TextBlock",
            text = $"SAMA • {context.WorkspaceName} • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC",
            size = "small",
            isSubtle = true,
            spacing = "medium",
            wrap = true
        });

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = bodyElements,
                        msteams = new
                        {
                            width = "full"
                        }
                    }
                }
            }
        };
    }

    private static object BuildLifecycleEventPayload(LifecycleEventContext context)
    {
        var (emoji, color) = context.EventType switch
        {
            EventTypes.CheckCreated => ("✨", "good"),
            EventTypes.CheckUpdated => ("🔄", "accent"),
            EventTypes.CheckDeleted => ("🗑️", "attention"),
            _ => ("ℹ️", "default")
        };

        var (title, description) = context.EventType switch
        {
            EventTypes.CheckCreated => (
                $"{emoji} New {CheckTypes.GetShortDisplayName(context.CheckType)} check created",
                $"**{context.CheckName}**  \nCreated by {context.PerformedBy}"
            ),
            EventTypes.CheckUpdated => (
                $"{emoji} Check updated",
                $"**{context.CheckName}**  \nModified by {context.PerformedBy}"
            ),
            EventTypes.CheckDeleted => (
                $"{emoji} Check deleted",
                $"**{context.CheckName}**  \nRemoved by {context.PerformedBy}"
            ),
            _ => (
                $"{emoji} Check {context.EventType.ToLowerInvariant()}",
                $"**{context.CheckName}**  \nAction performed by {context.PerformedBy}"
            )
        };

        var facts = new List<object>
        {
            new { title = "Workspace", value = context.WorkspaceName },
            new { title = "Check Type", value = CheckTypes.GetShortDisplayName(context.CheckType) }
        };

        var bodyElements = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
                weight = "bolder",
                size = "large",
                wrap = true
            },
            new
            {
                type = "TextBlock",
                text = description,
                wrap = true,
                spacing = "small"
            },
            new
            {
                type = "FactSet",
                facts,
                spacing = "medium"
            }
        };

        if (context.ConfigurationChanges != null && context.ConfigurationChanges.Count > 0)
        {
            var changes = string.Join(", ", context.ConfigurationChanges.Keys);
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = "**Changed Fields:**",
                weight = "bolder",
                spacing = "medium"
            });
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = changes,
                wrap = true,
                spacing = "small"
            });
        }

        bodyElements.Add(new
        {
            type = "TextBlock",
            text = $"SAMA • {context.WorkspaceName} • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC",
            size = "small",
            isSubtle = true,
            spacing = "medium",
            wrap = true
        });

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = bodyElements,
                        msteams = new
                        {
                            width = "full"
                        }
                    }
                }
            }
        };
    }

    private static object BuildStatusChangeEventPayload(StatusChangeEventContext context)
    {
        var emoji = context.NewStatus switch
        {
            CheckStatuses.Up => "✅",
            CheckStatuses.Warn => "⚠️",
            CheckStatuses.Down => "❌",
            _ => "❔"
        };

        var title = $"{emoji} {context.CheckName} status changed";
        var subtitle = $"Status changed from **{context.PreviousStatus}** to **{context.NewStatus}**";

        var facts = new List<object>
        {
            new { title = "Previous Status", value = context.PreviousStatus },
            new { title = "New Status", value = context.NewStatus },
            new { title = "Workspace", value = context.WorkspaceName }
        };

        if (context.ResponseTimeMs.HasValue)
        {
            facts.Insert(0, new { title = "Response Time", value = $"{context.ResponseTimeMs}ms" });
        }

        var bodyElements = new List<object>
        {
            new
            {
                type = "TextBlock",
                text = title,
                weight = "bolder",
                size = "large",
                wrap = true
            },
            new
            {
                type = "TextBlock",
                text = subtitle,
                wrap = true,
                spacing = "small"
            },
            new
            {
                type = "FactSet",
                facts,
                spacing = "medium"
            }
        };

        if (!string.IsNullOrWhiteSpace(context.ErrorMessage))
        {
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = "**Error:**",
                weight = "bolder",
                spacing = "medium"
            });
            bodyElements.Add(new
            {
                type = "TextBlock",
                text = context.ErrorMessage,
                wrap = true,
                fontType = "monospace",
                spacing = "small"
            });
        }

        bodyElements.Add(new
        {
            type = "TextBlock",
            text = $"SAMA • {context.WorkspaceName} • {context.Timestamp:yyyy-MM-dd HH:mm:ss} UTC",
            size = "small",
            isSubtle = true,
            spacing = "medium",
            wrap = true
        });

        return new
        {
            type = "message",
            attachments = new[]
            {
                new
                {
                    contentType = "application/vnd.microsoft.card.adaptive",
                    content = new
                    {
                        type = "AdaptiveCard",
                        version = "1.4",
                        body = bodyElements,
                        msteams = new
                        {
                            width = "full"
                        }
                    }
                }
            }
        };
    }

    private async Task<NotificationResultModel> SendTeamsMessageAsync(
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
                    "Teams {MessageType} sent successfully for check {CheckId} via channel {ChannelId}",
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
            var errorMessage = $"Teams API returned {response.StatusCode}: {errorBody}";

            _logger.LogWarning(
                "Failed to send Teams {MessageType} for check {CheckId}: {Error}",
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
                "HTTP error sending Teams {MessageType} for check {CheckId}",
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
                "Teams {MessageType} cancelled for check {CheckId}",
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
                "Teams {MessageType} timeout ({TimeoutSeconds}s) for check {CheckId}",
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
                "Unexpected error sending Teams {MessageType} for check {CheckId}",
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
