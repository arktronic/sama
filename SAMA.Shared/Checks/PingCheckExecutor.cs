using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Shared.Utilities;
using SAMA.Shared.Wrappers;

namespace SAMA.Shared.Checks;

[CheckType(CheckTypes.Ping)]
public class PingCheckExecutor(PingFactory _pingFactory) : ICheckExecutor
{
    private const int PerPingTimeoutMs = 5000;

    public async Task<CheckExecutionResult> ExecuteAsync(Dictionary<string, JsonElement> configuration, CancellationToken cancellationToken = default)
    {
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            var host = JsonElementHelper.GetString(configuration, ConfigurationKeys.PingCheck.Host);
            if (string.IsNullOrWhiteSpace(host))
            {
                return CreateDownResult("Host not configured");
            }

            var packetCount = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.PingCheck.PacketCount,
                CheckDefaults.PingPacketCount);

            if (packetCount <= 0)
            {
                return CreateDownResult("Packet count must be greater than 0");
            }

            var packetLossThresholdPercent = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.PingCheck.PacketLossThresholdPercent,
                CheckDefaults.PingPacketLossThresholdPercent);

            if (packetLossThresholdPercent is < 0 or > 100)
            {
                return CreateDownResult("Packet loss threshold must be between 0 and 100");
            }

            var timeoutSeconds = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.Common.TimeoutSeconds,
                CheckDefaults.CheckTimeoutSeconds);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using var ping = _pingFactory.CreatePing();

            var successfulPings = 0;
            var totalRoundtripTime = 0L;

            for (int i = 0; i < packetCount; i++)
            {
                try
                {
                    var result = await ping.SendPingAsync(host, PerPingTimeoutMs, cts.Token);

                    if (result.Status == IPStatus.Success)
                    {
                        successfulPings++;
                        totalRoundtripTime += result.RoundtripTime;
                    }
                }
                catch (PingException)
                {
                    // Failed ping, continue to next
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    throw;
                }
            }

            var avgResponseTimeMs = successfulPings > 0 ? (int?)(totalRoundtripTime / successfulPings) : null;
            var failedPings = packetCount - successfulPings;
            var packetLossPercent = (failedPings * 100) / packetCount;

            if (failedPings == packetCount)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ResponseTimeMs = avgResponseTimeMs,
                    ErrorMessage = $"All {packetCount} packet(s) failed (100% packet loss)"
                };
            }

            if (packetLossPercent >= packetLossThresholdPercent)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Warn,
                    ResponseTimeMs = avgResponseTimeMs,
                    ErrorMessage = $"Packet loss ({packetLossPercent}%) at or above threshold ({packetLossThresholdPercent}%)"
                };
            }

            return new CheckExecutionResult
            {
                Status = CheckStatuses.Up,
                ResponseTimeMs = avgResponseTimeMs
            };
        }
        catch (OperationCanceledException)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = (int)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds,
                ErrorMessage = "Check timeout exceeded"
            };
        }
        catch (Exception ex)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = (int)Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds,
                ErrorMessage = $"Unexpected error: {ex.Message}"
            };
        }
    }

    private static CheckExecutionResult CreateDownResult(string errorMessage)
    {
        return new CheckExecutionResult
        {
            Status = CheckStatuses.Down,
            ErrorMessage = errorMessage
        };
    }
}
