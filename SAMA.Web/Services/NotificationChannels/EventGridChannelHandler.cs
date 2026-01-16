using System.Text;
using System.Text.Json;
using SAMA.Data.Entities;
using SAMA.Shared.Constants;
using SAMA.Web.Constants;
using SAMA.Web.Models;

namespace SAMA.Web.Services.NotificationChannels;

[ChannelType(ChannelTypes.EventGrid)]
public class EventGridChannelHandler(
    IHttpClientFactory _httpClientFactory,
    GlobalSettingsService _globalSettings,
    ILogger<EventGridChannelHandler> _logger) : INotificationChannelHandler
{
    public async Task<NotificationResultModel> SendStatusAlertAsync(
        NotificationChannel channel,
        StatusAlertContext context,
        CancellationToken cancellationToken = default)
    {
        var eventGridEvent = BuildStatusAlertEvent(context);
        return await SendEventGridEventAsync(channel, eventGridEvent, context.CheckId, "status alert", cancellationToken);
    }

    public async Task<NotificationResultModel> SendLifecycleEventAsync(
        NotificationChannel channel,
        LifecycleEventContext context,
        CancellationToken cancellationToken = default)
    {
        var eventGridEvent = BuildLifecycleEvent(context);
        return await SendEventGridEventAsync(channel, eventGridEvent, context.CheckId, "lifecycle event", cancellationToken);
    }

    public async Task<NotificationResultModel> SendStatusChangeEventAsync(
        NotificationChannel channel,
        StatusChangeEventContext context,
        CancellationToken cancellationToken = default)
    {
        var eventGridEvent = BuildStatusChangeEvent(context);
        return await SendEventGridEventAsync(channel, eventGridEvent, context.CheckId, "status change event", cancellationToken);
    }

    private static object BuildStatusAlertEvent(StatusAlertContext context)
    {
        var eventType = context.IsRecovery
            ? "SAMA.Check.StatusAlert.Recovery"
            : $"SAMA.Check.StatusAlert.{context.Status}";

        var data = new
        {
            checkId = context.CheckId,
            checkName = context.CheckName,
            workspaceName = context.WorkspaceName,
            status = context.Status,
            isRecovery = context.IsRecovery,
            consecutiveFailures = context.ConsecutiveFailures,
            responseTimeMs = context.ResponseTimeMs,
            errorMessage = context.ErrorMessage
        };

        return new
        {
            id = Guid.NewGuid().ToString(),
            eventType,
            subject = $"workspaces/{context.WorkspaceName}/checks/{context.CheckName}",
            eventTime = context.Timestamp.ToUniversalTime().ToString("o"),
            data,
            dataVersion = "1.0"
        };
    }

    private static object BuildLifecycleEvent(LifecycleEventContext context)
    {
        var eventType = $"SAMA.Check.{context.EventType}";

        var data = new
        {
            checkId = context.CheckId,
            checkName = context.CheckName,
            checkType = CheckTypes.GetShortDisplayName(context.CheckType),
            workspaceName = context.WorkspaceName,
            performedBy = context.PerformedBy,
            configurationChanges = context.ConfigurationChanges?.Keys.ToArray()
        };

        return new
        {
            id = Guid.NewGuid().ToString(),
            eventType,
            subject = $"workspaces/{context.WorkspaceName}/checks/{context.CheckName}",
            eventTime = context.Timestamp.ToUniversalTime().ToString("o"),
            data,
            dataVersion = "1.0"
        };
    }

    private static object BuildStatusChangeEvent(StatusChangeEventContext context)
    {
        var eventType = "SAMA.Check.StatusChanged";

        var data = new
        {
            checkId = context.CheckId,
            checkName = context.CheckName,
            workspaceName = context.WorkspaceName,
            previousStatus = context.PreviousStatus,
            newStatus = context.NewStatus,
            responseTimeMs = context.ResponseTimeMs,
            errorMessage = context.ErrorMessage
        };

        return new
        {
            id = Guid.NewGuid().ToString(),
            eventType,
            subject = $"workspaces/{context.WorkspaceName}/checks/{context.CheckName}",
            eventTime = context.Timestamp.ToUniversalTime().ToString("o"),
            data,
            dataVersion = "1.0"
        };
    }

    private async Task<NotificationResultModel> SendEventGridEventAsync(
        NotificationChannel channel,
        object eventGridEvent,
        Guid checkId,
        string messageType,
        CancellationToken cancellationToken)
    {
        var sentAt = DateTimeOffset.UtcNow;

        try
        {
            if (!channel.ConfigurationJson.TryGetValue(ConfigurationKeys.EventGrid.TopicEndpoint, out var topicEndpointElement))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Topic endpoint not configured",
                    SentAt = sentAt
                };
            }

            var topicEndpoint = topicEndpointElement.GetString();

            if (string.IsNullOrWhiteSpace(topicEndpoint))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Topic endpoint not configured",
                    SentAt = sentAt
                };
            }

            if (!channel.ConfigurationJson.TryGetValue(ConfigurationKeys.EventGrid.AccessKey, out var accessKeyElement))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Access key not configured",
                    SentAt = sentAt
                };
            }

            var accessKey = accessKeyElement.GetString();

            if (string.IsNullOrWhiteSpace(accessKey))
            {
                return new NotificationResultModel
                {
                    Success = false,
                    ErrorMessage = "Access key not configured",
                    SentAt = sentAt
                };
            }

            var events = new[] { eventGridEvent };
            var jsonContent = JsonSerializer.Serialize(events);

            var timeoutSeconds = _globalSettings.NotificationTimeoutSeconds;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, topicEndpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            request.Headers.Add("aeg-sas-key", accessKey);

            var response = await httpClient.SendAsync(request, cts.Token);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Event Grid {MessageType} sent successfully for check {CheckId} via channel {ChannelId}",
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
            var errorMessage = $"Event Grid API returned {response.StatusCode}: {errorBody}";

            _logger.LogWarning(
                "Failed to send Event Grid {MessageType} for check {CheckId}: {Error}",
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
                "HTTP error sending Event Grid {MessageType} for check {CheckId}",
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
                "Event Grid {MessageType} cancelled for check {CheckId}",
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
                "Event Grid {MessageType} timeout ({TimeoutSeconds}s) for check {CheckId}",
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
                "Unexpected error sending Event Grid {MessageType} for check {CheckId}",
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
