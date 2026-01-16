using MailKit.Net.Smtp;

namespace SAMA.Web.Wrappers;

/// <summary>
/// Factory for creating ISmtpClient instances.
/// Virtual method allows for mocking in tests.
/// </summary>
public class SmtpClientFactory
{
    /// <summary>
    /// Creates a new ISmtpClient instance.
    /// </summary>
    public virtual ISmtpClient CreateClient() => new SmtpClient();
}
