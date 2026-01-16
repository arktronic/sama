using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Shared.Utilities;
using SAMA.Shared.Wrappers;

namespace SAMA.Shared.Checks;

[CheckType(CheckTypes.Tcp)]
public class TcpCheckExecutor(TcpClientFactory _tcpClientFactory) : ICheckExecutor
{
    public async Task<CheckExecutionResult> ExecuteAsync(Dictionary<string, JsonElement> configuration, CancellationToken cancellationToken = default)
    {
        long? timestamp = null;

        try
        {
            var host = JsonElementHelper.GetString(configuration, ConfigurationKeys.TcpCheck.Host);

            if (string.IsNullOrWhiteSpace(host))
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ErrorMessage = "Host not configured"
                };
            }

            var port = JsonElementHelper.GetInt32(configuration, ConfigurationKeys.TcpCheck.Port);

            if (!port.HasValue || port.Value is <= 0 or > 65535)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ErrorMessage = "Valid port (1-65535) not configured"
                };
            }

            var connectionTimeWarnThresholdMs = JsonElementHelper.GetInt32(configuration, ConfigurationKeys.TcpCheck.ConnectionTimeWarnThresholdMs);

            var timeoutSeconds = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.Common.TimeoutSeconds,
                CheckDefaults.CheckTimeoutSeconds);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var client = _tcpClientFactory.CreateClient();

            timestamp = Stopwatch.GetTimestamp();
            await client.ConnectAsync(host, port.Value, cts.Token);
            var connectionTime = Stopwatch.GetElapsedTime(timestamp.Value);

            var connectionTimeMs = (int)connectionTime.TotalMilliseconds;
            var status = CheckStatuses.Up;
            string? errorMessage = null;

            if (connectionTimeWarnThresholdMs.HasValue && connectionTimeMs > connectionTimeWarnThresholdMs.Value)
            {
                status = CheckStatuses.Warn;
                errorMessage = $"Connection time ({connectionTimeMs}ms) exceeded threshold ({connectionTimeWarnThresholdMs.Value}ms)";
            }

            return new CheckExecutionResult
            {
                Status = status,
                ResponseTimeMs = connectionTimeMs,
                ErrorMessage = errorMessage
            };
        }
        catch (SocketException ex)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = $"Connection failed: {ex.Message}"
            };
        }
        catch (OperationCanceledException)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = "Connection timeout"
            };
        }
        catch (Exception ex)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }
}
