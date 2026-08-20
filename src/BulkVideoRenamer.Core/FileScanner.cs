namespace BulkVideoRenamer.Core;

public static class FileScanner
{
    /// <summary>
    /// Scans a single folder (non-recursive) and returns full paths of files
    /// whose extension is in the supported video extension list.
    /// </summary>
    public static List<string> ScanVideoFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder không tồn tại: {folderPath}");

        return Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly)
            .Where(VideoExtensions.IsVideoFile)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
