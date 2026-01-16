namespace SAMA.Shared.Constants;

/// <summary>
/// Check status constants.
/// Represents the result of a check execution.
/// </summary>
public static class CheckStatuses
{
    /// <summary>
    /// Check passed successfully.
    /// </summary>
    public const string Up = "Up";

    /// <summary>
    /// Check passed with warnings or degraded performance.
    /// </summary>
    public const string Warn = "Warn";

    /// <summary>
    /// Check failed.
    /// </summary>
    public const string Down = "Down";
}
