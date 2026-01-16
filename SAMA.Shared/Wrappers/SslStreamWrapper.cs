using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace SAMA.Shared.Wrappers;

/// <summary>
/// Wrapper around SslStream with virtual methods for testability.
/// </summary>
public class SslStreamWrapper : IDisposable
{
    private SslStream? _sslStream;
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="SslStreamWrapper"/> class using the specified inner stream and optional remote.
    /// certificate validation callback.
    /// </summary>
    /// <remarks>This class takes ownership of the inner stream and closes it.</remarks>
    /// <param name="innerStream">The underlying stream to use for encrypted communication. Must be readable and writable.</param>
    /// <param name="userCertificateValidationCallback">An optional delegate to validate the remote certificate during authentication.</param>
    public SslStreamWrapper(
        Stream? innerStream = null,
        RemoteCertificateValidationCallback? userCertificateValidationCallback = null)
    {
        if (innerStream != null)
        {
            _sslStream = new SslStream(innerStream, false, userCertificateValidationCallback);
        }
    }

    /// <summary>
    /// Authenticates the client and optionally the server in a client-server connection.
    /// </summary>
    public virtual async Task AuthenticateAsClientAsync(
        SslClientAuthenticationOptions options,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposedValue, this);
        ArgumentNullException.ThrowIfNull(_sslStream, nameof(_sslStream));

        await _sslStream.AuthenticateAsClientAsync(options, cancellationToken);
    }

    /// <summary>
    /// Gets information about the remote certificate.
    /// </summary>
    public virtual X509Certificate? RemoteCertificate
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _sslStream?.RemoteCertificate;
        }
    }

    /// <summary>
    /// Gets a value indicating whether authentication was successful.
    /// </summary>
    public virtual bool IsAuthenticated
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposedValue, this);
            return _sslStream?.IsAuthenticated ?? false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _sslStream?.Dispose();
                _sslStream = null;
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
