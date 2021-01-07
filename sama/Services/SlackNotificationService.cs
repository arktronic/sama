﻿using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sama.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace sama.Services
{
    public class SlackNotificationService : INotificationService
    {
        private const int NOTIFY_UP_QUEUE_DELAY_MILLISECONDS = 2500;

        private readonly ILogger<SlackNotificationService> _logger;
        private readonly SettingsService _settings;
        private readonly IServiceProvider _serviceProvider;
        private readonly BackgroundExecutionWrapper _bgExec;

        private readonly List<Endpoint> _delayNotifyUpEndpoints;
        private readonly List<Task> _delayTasks;

        public SlackNotificationService(ILogger<SlackNotificationService> logger, SettingsService settings, IServiceProvider serviceProvider, BackgroundExecutionWrapper bgExec)
        {
            _logger = logger;
            _settings = settings;
            _serviceProvider = serviceProvider;
            _bgExec = bgExec;

            _delayNotifyUpEndpoints = new List<Endpoint>();
            _delayTasks = new List<Task>();
        }

        public virtual void NotifyMisc(Endpoint endpoint, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.EndpointAdded:
                    SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} has been added and will be checked shortly.");
                    break;
                case NotificationType.EndpointRemoved:
                    SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} has been removed.");
                    break;
                case NotificationType.EndpointEnabled:
                    SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} has been enabled and will be checked shortly.");
                    break;
                case NotificationType.EndpointDisabled:
                    SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} has been disabled.");
                    break;
                case NotificationType.EndpointReconfigured:
                    SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} has been reconfigured and will be checked shortly.");
                    break;
                default:
                    return;
            }
        }

        public virtual void NotifySingleResult(Endpoint endpoint, EndpointCheckResult result)
        {
            // Ignore this notification type.
        }

        public virtual void NotifyDown(Endpoint endpoint, DateTimeOffset downAsOf, Exception reason)
        {
            var failureMessage = reason?.Message;

            if (!string.IsNullOrWhiteSpace(failureMessage))
            {
                var msg = failureMessage.Trim();
                if (!msg.EndsWith('.') && !msg.EndsWith('!') && !msg.EndsWith('?'))
                    failureMessage = failureMessage.Trim() + '.';

                if (reason is SslException sslEx){
                    failureMessage += "\n Details: ```\n" + sslEx.Details + "```";
                }
            }

            SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} is down: {failureMessage}");
        }

        public virtual void NotifyUp(Endpoint endpoint, DateTimeOffset? downAsOf)
        {
            if (downAsOf.HasValue)
            {
                var downLength = DateTimeOffset.UtcNow - downAsOf.Value;
                SendNotification($"The endpoint {FormatEndpointName(endpoint.Name)} is up after being down for {downLength.Humanize()}. Hooray!");
            }
            else
            {
                EnqueueDelayedUpNotification(endpoint);
            }
        }

        protected virtual void SendNotification(string message)
        {
            var url = _settings.Notifications_Slack_WebHook;
            if (string.IsNullOrWhiteSpace(url)) return;

            try
            {
                using (var httpHandler = _serviceProvider.GetRequiredService<HttpClientHandler>())
                using (var client = new HttpClient(httpHandler, false))
                {
                    var data = JsonConvert.SerializeObject(new { text = message });
                    client.PostAsync(url, new StringContent(data)).Wait();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(0, "Unable to send Slack notification", ex);
            }
        }

        protected virtual void EnqueueDelayedUpNotification(Endpoint endpoint)
        {
            lock(_delayNotifyUpEndpoints)
            {
                _delayNotifyUpEndpoints.Add(endpoint);
                _delayTasks.Add(_bgExec.ExecuteDelayed(() => SendDelayedNotification(), NOTIFY_UP_QUEUE_DELAY_MILLISECONDS));
            }
        }

        protected virtual void SendDelayedNotification()
        {
            var endpoints = new List<Endpoint>();
            lock (_delayNotifyUpEndpoints)
            {
                if (_delayTasks.Count > 0) _delayTasks.RemoveAt(0);

                if (_delayTasks.Count > 0)
                {
                    // Wait until the last task to do the notification.
                    return;
                }

                endpoints.AddRange(_delayNotifyUpEndpoints);
                _delayNotifyUpEndpoints.Clear();
            }

            if (endpoints.Count < 1)
            {
                return;
            }
            else if (endpoints.Count == 1)
            {
                SendNotification($"The endpoint {FormatEndpointName(endpoints.First().Name)} is up. Hooray!");
            }
            else
            {
                var stringifiedEndpoints = string.Join(", ", endpoints.Select(ep => FormatEndpointName(ep.Name)));
                SendNotification($"The following endpoints are up: {stringifiedEndpoints}. Hooray!");
            }
        }

        protected virtual string FormatEndpointName(string rawName) => $"`{rawName.Replace("`", "")}`";
    }
}
