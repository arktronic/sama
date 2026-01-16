namespace SAMA.Shared.Wrappers;

/// <summary>
/// Factory for creating PingWrapper instances.
/// Virtual method allows for mocking in tests.
/// </summary>
public class PingFactory
{
    /// <summary>
    /// Creates a new PingWrapper instance.
    /// </summary>
    public virtual PingWrapper CreatePing() => new();
}
