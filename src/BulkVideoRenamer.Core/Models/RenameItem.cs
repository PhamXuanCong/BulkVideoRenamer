namespace BulkVideoRenamer.Core.Models;

public enum RenameStatus
{
    Ok,
    NoIdFound,
    Skipped,
    Error
}

public class RenameItem
{
    public required string OriginalPath { get; init; }
    public required string OriginalName { get; init; }
    public required string NewName { get; set; }
    public RenameStatus Status { get; set; } = RenameStatus.Ok;
    public string? ErrorMessage { get; set; }

    public string Directory => Path.GetDirectoryName(OriginalPath)!;
    public string NewPath => Path.Combine(Directory, NewName);
}
