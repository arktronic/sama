using System.Diagnostics;
using System.Text.Json;
using DnsClient;
using DnsClient.Protocol;
using SAMA.Shared.Constants;
using SAMA.Shared.Models;
using SAMA.Shared.Utilities;

namespace SAMA.Shared.Checks;

[CheckType(CheckTypes.Dns)]
public class DnsCheckExecutor(ILookupClient _lookupClient) : ICheckExecutor
{
    public async Task<CheckExecutionResult> ExecuteAsync(Dictionary<string, JsonElement> configuration, CancellationToken cancellationToken = default)
    {
        long? timestamp = null;

        try
        {
            var hostname = JsonElementHelper.GetString(configuration, ConfigurationKeys.DnsCheck.Hostname);
            if (string.IsNullOrWhiteSpace(hostname))
            {
                return CreateDownResult("Hostname not configured");
            }

            var recordType = JsonElementHelper.GetString(configuration, ConfigurationKeys.DnsCheck.RecordType, CheckDefaults.DnsRecordType);
            var expectedValues = JsonElementHelper.GetStringArray(configuration, ConfigurationKeys.DnsCheck.ExpectedValues, [])
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToArray();

            var timeoutSeconds = JsonElementHelper.GetInt32(
                configuration,
                ConfigurationKeys.Common.TimeoutSeconds,
                CheckDefaults.CheckTimeoutSeconds);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var queryType = recordType.ToUpperInvariant() switch
            {
                "A" => QueryType.A,
                "AAAA" => QueryType.AAAA,
                "CNAME" => QueryType.CNAME,
                "MX" => QueryType.MX,
                "TXT" => QueryType.TXT,
                "NS" => QueryType.NS,
                _ => QueryType.A
            };

            timestamp = Stopwatch.GetTimestamp();
            var result = await _lookupClient.QueryAsync(hostname, queryType, cancellationToken: cts.Token);
            var responseTimeMs = (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds;

            if (result.HasError)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ResponseTimeMs = responseTimeMs,
                    ErrorMessage = $"DNS query failed: {result.ErrorMessage}"
                };
            }

            if (result.Answers.Count == 0)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ResponseTimeMs = responseTimeMs,
                    ErrorMessage = $"No {recordType} records found for {hostname}"
                };
            }

            var actualValues = ExtractRecordValues(result.Answers, recordType)
                .Select(v => v.Trim())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToList();

            if (actualValues.Count == 0)
            {
                return new CheckExecutionResult
                {
                    Status = CheckStatuses.Down,
                    ResponseTimeMs = responseTimeMs,
                    ErrorMessage = $"No {recordType} records found for {hostname}"
                };
            }

            if (expectedValues.Length > 0)
            {
                var comparisonType = recordType.ToUpperInvariant() == "TXT"
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                var hasMatch = expectedValues.Any(expected =>
                    actualValues.Any(actual => string.Equals(actual, expected, comparisonType)));

                if (!hasMatch)
                {
                    var expectedList = string.Join(", ", expectedValues);
                    var actualList = string.Join(", ", actualValues);
                    return new CheckExecutionResult
                    {
                        Status = CheckStatuses.Down,
                        ResponseTimeMs = responseTimeMs,
                        ErrorMessage = $"DNS records do not match expected values. Expected: [{expectedList}]. Actual: [{actualList}]"
                    };
                }
            }

            return new CheckExecutionResult
            {
                Status = CheckStatuses.Up,
                ResponseTimeMs = responseTimeMs
            };
        }
        catch (OperationCanceledException)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = "DNS query timeout exceeded"
            };
        }
        catch (DnsResponseException ex)
        {
            return new CheckExecutionResult
            {
                Status = CheckStatuses.Down,
                ResponseTimeMs = timestamp.HasValue ? (int)Stopwatch.GetElapsedTime(timestamp.Value).TotalMilliseconds : null,
                ErrorMessage = $"DNS query failed: {ex.Message}"
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

    private static List<string> ExtractRecordValues(IEnumerable<DnsResourceRecord> records, string recordType)
    {
        var values = new List<string>();

        foreach (var record in records)
        {
            switch (recordType.ToUpperInvariant())
            {
                case "A":
                    if (record is ARecord aRecord)
                    {
                        values.Add(aRecord.Address.ToString());
                    }
                    break;

                case "AAAA":
                    if (record is AaaaRecord aaaaRecord)
                    {
                        values.Add(aaaaRecord.Address.ToString());
                    }
                    break;

                case "CNAME":
                    if (record is CNameRecord cnameRecord)
                    {
                        values.Add(cnameRecord.CanonicalName.Value.TrimEnd('.'));
                    }
                    break;

                case "MX":
                    if (record is MxRecord mxRecord)
                    {
                        values.Add(mxRecord.Exchange.Value.TrimEnd('.'));
                    }
                    break;

                case "TXT":
                    if (record is TxtRecord txtRecord)
                    {
                        values.AddRange(txtRecord.Text);
                    }
                    break;

                case "NS":
                    if (record is NsRecord nsRecord)
                    {
                        values.Add(nsRecord.NSDName.Value.TrimEnd('.'));
                    }
                    break;
            }
        }

        return values;
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
