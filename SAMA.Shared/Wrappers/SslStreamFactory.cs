using System.Net.Security;

namespace SAMA.Shared.Wrappers;

/// <summary>
/// Factory for creating SslStreamWrapper instances.
/// </summary>
public class SslStreamFactory
{
    /// <summary>
    /// Creates a new SslStreamWrapper instance.
    /// </summary>
    public virtual SslStreamWrapper CreateSslStream(
        Stream innerStream,
        RemoteCertificateValidationCallback? userCertificateValidationCallback)
    {
        return new SslStreamWrapper(innerStream, userCertificateValidationCallback);
    }
}
