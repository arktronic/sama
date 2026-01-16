using System.Diagnostics;
using System.Text.Json;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Shared.Utilities;
using SAMA.Shared.Wrappers;

namespace SAMA.Shared.Checks;

[CheckType(CheckTypes.Script)]
public class ScriptCheckExecutor(ProcessFactory _processFactory) : ICheckExecutor
{
    public async Task<CheckExecutionResult> ExecuteAsync(Dictionary<string, JsonElement> configuration, CancellationToken cancellationToken = default)
    {
        long? timestamp = null;

        try
        {
            var scriptPath = JsonElementHelper.GetString(configuration, ConfigurationKeys.ScriptCheck.Path);
            if (string.IsNullOrWhiteSpace(scriptPath))
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ErrorMessage = "Script path not configured"
                };
            }

            var arguments = JsonElementHelper.GetString(configuration, ConfigurationKeys.ScriptCheck.Arguments);
            var expectedExitCode = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.ScriptCheck.ExpectedExitCode,
                CheckDefaults.ScriptExpectedExitCode);

            var timeoutSeconds = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.Common.TimeoutSeconds,
                CheckDefaults.CheckTimeoutSeconds);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var startInfo = new ProcessStartInfo
            {
                FileName = scriptPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (!string.IsNullOrWhiteSpace(arguments))
            {
                startInfo.Arguments = arguments;
            }

            using var process = _processFactory.CreateProcess();

            timestamp = Stopwatch.GetTimestamp();
            process.Start(startInfo);

            await process.WaitForExitAsync(cts.Token);

            var executionTime = Stopwatch.GetElapsedTime(timestamp.Value);
            var executionTimeMs = (int)executionTime.TotalMilliseconds;

            var exitCode = process.ExitCode;

            if (exitCode == expectedExitCode)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Up,
                    ResponseTimeMs = executionTimeMs,
                    StatusCode = exitCode
                };
            }

            var stderr = await process.ReadStandardErrorAsync(cancellationToken);
            var errorMessage = string.IsNullOrWhiteSpace(stderr)
                ? $"Script exited with code {exitCode} (expected {expectedExitCode})"
                : $"Script exited with code {exitCode} (expected {expectedExitCode}): {stderr.Trim()}";

            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = executionTimeMs,
                StatusCode = exitCode,
                ErrorMessage = errorMessage
            };
        }
        catch (OperationCanceledException)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = "Script execution timeout"
            };
        }
        catch (Exception ex)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = $"Script execution failed: {ex.Message}"
            };
        }
    }
}
