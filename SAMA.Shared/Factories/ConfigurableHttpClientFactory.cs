namespace SAMA.Shared.Factories;

public class ConfigurableHttpClientFactory
{
    public virtual HttpClient CreateClient(bool followRedirects, bool allowInvalidSsl, int timeoutSeconds)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = followRedirects,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(1),
            EnableMultipleHttp2Connections = true,
            UseCookies = false
        };
#pragma warning restore CA2000 // Dispose objects before losing scope

        if (allowInvalidSsl)
        {
            handler.SslOptions = new System.Net.Security.SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, errors) => true
            };
        }

        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            DefaultRequestVersion = new Version(2, 0),
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
        };
    }
}
