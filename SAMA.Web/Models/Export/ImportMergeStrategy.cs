namespace SAMA.Web.Models.Export;

/// <summary>
/// Strategy for handling existing workspaces during import.
/// </summary>
public enum ImportMergeStrategy
{
    /// <summary>
    /// Skip workspaces that already exist (by name).
    /// </summary>
    SkipExisting,

    /// <summary>
    /// Merge new entities into existing workspaces, skipping duplicates within.
    /// </summary>
    MergeIntoExisting,

    /// <summary>
    /// Delete and recreate workspaces that already exist.
    /// </summary>
    ReplaceExisting
}
