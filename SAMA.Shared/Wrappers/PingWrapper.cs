using System.Net.NetworkInformation;
using SAMA.Shared.Models;

namespace SAMA.Shared.Wrappers;

/// <summary>
/// Wrapper around System.Net.NetworkInformation.Ping with virtual methods for testability.
/// </summary>
public class PingWrapper : IDisposable
{
    private Ping? _ping = new();
    private bool _disposedValue;

    /// <summary>
    /// Sends an ICMP echo request to the specified host.
    /// </summary>
    public virtual async Task<PingResult> SendPingAsync(string hostNameOrAddress, int timeoutMs, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        ArgumentNullException.ThrowIfNull(_ping, nameof(_ping));

        return PingResult.FromPingReply(
            await _ping.SendPingAsync(hostNameOrAddress, TimeSpan.FromMilliseconds(timeoutMs), cancellationToken: cancellationToken));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _ping?.Dispose();
                _ping = null;
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
