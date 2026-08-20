using System.Globalization;
using System.Text;

namespace BulkVideoRenamer.Core;

public record RenameLogEntry(string OldName, string NewName, DateTime Timestamp);

public class UndoResult
{
    public int SucceededCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = [];
}

public static class RenameLogger
{
    private const string LogFilePrefix = "rename_log_";
    private const string LogFileSuffix = ".csv";
    private const string UndonePrefix = "undone_";

    /// <summary>
    /// Writes one timestamped CSV log file per rename run into the target folder.
    /// </summary>
    public static string WriteLog(string folderPath, IReadOnlyList<RenameLogEntry> entries)
    {
        var fileName = $"{LogFilePrefix}{DateTime.Now:yyyyMMdd_HHmmss}{LogFileSuffix}";
        var path = Path.Combine(folderPath, fileName);

        var sb = new StringBuilder();
        sb.AppendLine("OldName,NewName,Timestamp");
        foreach (var entry in entries)
        {
            sb.AppendLine(string.Join(',',
                CsvEscape(entry.OldName),
                CsvEscape(entry.NewName),
                entry.Timestamp.ToString("o", CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        return path;
    }

    /// <summary>
    /// Returns the path of the most recent, not-yet-undone rename log in the folder, or null if none exists.
    /// </summary>
    public static string? FindLatestLog(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return null;

        return Directory.EnumerateFiles(folderPath, $"{LogFilePrefix}*{LogFileSuffix}", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    public static List<RenameLogEntry> ReadLog(string logPath)
    {
        var entries = new List<RenameLogEntry>();
        var lines = File.ReadAllLines(logPath, Encoding.UTF8);

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Count < 3)
                continue;

            entries.Add(new RenameLogEntry(fields[0], fields[1], DateTime.Parse(fields[2], CultureInfo.InvariantCulture)));
        }

        return entries;
    }

    /// <summary>
    /// Reverses the most recent rename log for the folder: moves NewName back to OldName for each entry.
    /// Marks the log as undone afterwards so it won't be picked up by a second undo.
    /// </summary>
    public static UndoResult Undo(string folderPath)
    {
        var logPath = FindLatestLog(folderPath)
            ?? throw new FileNotFoundException("Không tìm thấy log đổi tên nào trong folder này để undo.");

        var entries = ReadLog(logPath);
        var result = new UndoResult();

        foreach (var entry in entries)
        {
            var currentPath = Path.Combine(folderPath, entry.NewName);
            var originalPath = Path.Combine(folderPath, entry.OldName);

            try
            {
                if (!File.Exists(currentPath))
                    throw new FileNotFoundException($"Không tìm thấy file '{entry.NewName}' để undo.");

                File.Move(currentPath, originalPath);
                result.SucceededCount++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or FileNotFoundException)
            {
                result.ErrorCount++;
                result.Errors.Add($"{entry.NewName} -> {entry.OldName}: {ex.Message}");
            }
        }

        var undonePath = Path.Combine(Path.GetDirectoryName(logPath)!, UndonePrefix + Path.GetFileName(logPath));
        File.Move(logPath, undonePath);
        return result;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields;
    }
}
