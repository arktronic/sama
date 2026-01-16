namespace SAMA.Web.Services;

public class ApplicationStateService
{
    public virtual DateTimeOffset StartupTime { get; } = DateTimeOffset.UtcNow;

    public virtual string Version { get; } = typeof(ApplicationStateService).Assembly
        .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
        .FirstOrDefault() is System.Reflection.AssemblyInformationalVersionAttribute attr
            ? attr.InformationalVersion
            : typeof(ApplicationStateService).Assembly.GetName().Version?.ToString() ?? "unknown";
}
