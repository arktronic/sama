using System.Net.Sockets;

namespace SAMA.Shared.Wrappers;

/// <summary>
/// Wrapper around TcpClient with virtual methods for testability.
/// </summary>
public class TcpClientWrapper : IDisposable
{
    private TcpClient? _client = new();
    private bool _disposedValue;

    /// <summary>
    /// Connects the client to a remote TCP host.
    /// </summary>
    public virtual async Task ConnectAsync(string host, int port, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        ArgumentNullException.ThrowIfNull(_client, nameof(_client));

        await _client.ConnectAsync(host, port, cancellationToken);
    }

    /// <summary>
    /// Gets a value indicating whether the underlying socket is connected.
    /// </summary>
    public virtual bool Connected => _client?.Connected ?? false;

    /// <summary>
    /// Returns the NetworkStream used to send and receive data.
    /// </summary>
    public virtual NetworkStream GetStream()
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        ArgumentNullException.ThrowIfNull(_client, nameof(_client));

        return _client.GetStream();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _client?.Dispose();
                _client = null;
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
