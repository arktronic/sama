namespace SAMA.Shared.Wrappers;

/// <summary>
/// Factory for creating TcpClientWrapper instances.
/// Virtual method allows for mocking in tests.
/// </summary>
public class TcpClientFactory
{
    /// <summary>
    /// Creates a new TcpClientWrapper instance.
    /// </summary>
    public virtual TcpClientWrapper CreateClient() => new();
}
