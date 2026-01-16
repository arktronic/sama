namespace SAMA.Shared.Checks;

/// <summary>
/// Attribute to specify the check type identifier for an ICheckExecutor implementation.
/// Used for automatic service registration and discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CheckTypeAttribute : Attribute
{
    /// <summary>
    /// Gets the check type identifier (e.g., HTTP, TCP, Ping).
    /// Must match the constant defined in <see cref="Constants.CheckTypes"/>.
    /// </summary>
    public string CheckType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CheckTypeAttribute"/> class.
    /// </summary>
    /// <param name="checkType">The check type identifier</param>
    public CheckTypeAttribute(string checkType)
    {
        CheckType = checkType;
    }
}
