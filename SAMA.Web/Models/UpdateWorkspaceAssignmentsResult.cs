namespace SAMA.Web.Models;

public class UpdateWorkspaceAssignmentsResult
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int AddedCount { get; set; }

    public int RemovedCount { get; set; }

    public int UpdatedCount { get; set; }

    public static UpdateWorkspaceAssignmentsResult SuccessResult(int added, int removed, int updated)
    {
        return new UpdateWorkspaceAssignmentsResult
        {
            Success = true,
            AddedCount = added,
            RemovedCount = removed,
            UpdatedCount = updated
        };
    }

    public static UpdateWorkspaceAssignmentsResult FailureResult(string errorMessage)
    {
        return new UpdateWorkspaceAssignmentsResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
