using System.Text.RegularExpressions;
using BulkVideoRenamer.Core.Models;

namespace BulkVideoRenamer.Core;

public static partial class RenameService
{
    private static readonly char[] InvalidFileNameChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    [GeneratedRegex(@"\s*\[[^\[\]]*\]$")]
    private static partial Regex TrailingIdRegex();

    /// <summary>
    /// Builds a rename preview for every video file directly inside folderPath.
    /// Does not touch the filesystem (aside from reading the existing file list for collision checks).
    /// </summary>
    public static List<RenameItem> BuildPlan(string folderPath, string hashtagInput)
    {
        var hashtagSuffix = BuildHashtagSuffix(hashtagInput);
        var files = FileScanner.ScanVideoFiles(folderPath);
        var existingNamesOnDisk = new HashSet<string>(
            files.Select(Path.GetFileName)!,
            StringComparer.OrdinalIgnoreCase);
        var usedInBatch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var items = new List<RenameItem>();

        foreach (var path in files)
        {
            var originalName = Path.GetFileName(path);
            var ext = Path.GetExtension(path);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(path);

            var match = TrailingIdRegex().Match(nameWithoutExt);
            var status = match.Success ? RenameStatus.Ok : RenameStatus.NoIdFound;
            var strippedName = match.Success
                ? nameWithoutExt[..match.Index]
                : nameWithoutExt;

            var candidateBase = string.IsNullOrEmpty(hashtagSuffix)
                ? strippedName
                : $"{strippedName} {hashtagSuffix}";

            var candidateName = candidateBase + ext;
            candidateName = ResolveCollision(candidateName, existingNamesOnDisk, usedInBatch, originalName);
            usedInBatch.Add(candidateName);

            items.Add(new RenameItem
            {
                OriginalPath = path,
                OriginalName = originalName,
                NewName = candidateName,
                Status = status
            });
        }

        return items;
    }

    /// <summary>
    /// Sanitizes and joins user-provided hashtags (space separated) into a single suffix string.
    /// </summary>
    public static string BuildHashtagSuffix(string hashtagInput)
    {
        if (string.IsNullOrWhiteSpace(hashtagInput))
            return string.Empty;

        var tokens = hashtagInput
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(SanitizeFileNamePart)
            .Where(t => t.Length > 0);

        return string.Join(' ', tokens);
    }

    private static string SanitizeFileNamePart(string part)
    {
        return string.Concat(part.Where(c => !InvalidFileNameChars.Contains(c)));
    }

    /// <summary>
    /// Appends "(1)", "(2)", ... to the file name (before the extension) until it no longer
    /// collides with an existing file on disk or a name already assigned earlier in this batch.
    /// A candidate that matches the file's own original name is always allowed (no-op rename).
    /// </summary>
    private static string ResolveCollision(
        string candidateName,
        HashSet<string> existingNamesOnDisk,
        HashSet<string> usedInBatch,
        string originalName)
    {
        if (string.Equals(candidateName, originalName, StringComparison.OrdinalIgnoreCase))
            return candidateName;

        if (!existingNamesOnDisk.Contains(candidateName) && !usedInBatch.Contains(candidateName))
            return candidateName;

        var ext = Path.GetExtension(candidateName);
        var baseName = Path.GetFileNameWithoutExtension(candidateName);

        var counter = 1;
        string next;
        do
        {
            next = $"{baseName} ({counter}){ext}";
            counter++;
        } while ((existingNamesOnDisk.Contains(next) || usedInBatch.Contains(next))
                 && !string.Equals(next, originalName, StringComparison.OrdinalIgnoreCase));

        return next;
    }

    public class ExecuteResult
    {
        public int SucceededCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<RenameItem> Items { get; set; } = [];
    }

    /// <summary>
    /// Executes the renames on disk, logs the result, and returns per-item outcomes.
    /// Locked files are skipped and logged; missing folder permission raises immediately.
    /// </summary>
    public static ExecuteResult Execute(List<RenameItem> items, string folderPath)
    {
        var result = new ExecuteResult();
        var logEntries = new List<RenameLogEntry>();

        foreach (var item in items)
        {
            if (string.Equals(item.OriginalName, item.NewName, StringComparison.Ordinal))
            {
                item.Status = RenameStatus.Skipped;
                result.SkippedCount++;
                continue;
            }

            try
            {
                File.Move(item.OriginalPath, item.NewPath);
                item.Status = RenameStatus.Ok;
                result.SucceededCount++;
                logEntries.Add(new RenameLogEntry(item.OriginalName, item.NewName, DateTime.Now));
            }
            catch (IOException ex)
            {
                item.Status = RenameStatus.Error;
                item.ErrorMessage = $"File đang bị khoá/mở bởi chương trình khác: {ex.Message}";
                result.ErrorCount++;
            }
            catch (UnauthorizedAccessException ex)
            {
                item.Status = RenameStatus.Error;
                item.ErrorMessage = $"Không có quyền ghi vào folder: {ex.Message}";
                result.ErrorCount++;
            }
        }

        if (logEntries.Count > 0)
            RenameLogger.WriteLog(folderPath, logEntries);

        result.Items = items;
        return result;
    }
}
