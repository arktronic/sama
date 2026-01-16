namespace SAMA.Shared.Wrappers;

/// <summary>
/// Factory for creating ProcessWrapper instances.
/// Virtual method allows for mocking in tests.
/// </summary>
public class ProcessFactory
{
    /// <summary>
    /// Creates a new ProcessWrapper instance.
    /// </summary>
    public virtual ProcessWrapper CreateProcess() => new();
}
