using System.Text.Json;
using SAMA.Shared.Models;

namespace SAMA.Shared.Checks;

public interface ICheckExecutor
{
    Task<CheckExecutionResult> ExecuteAsync(Dictionary<string, JsonElement> configuration, CancellationToken cancellationToken = default);
}
