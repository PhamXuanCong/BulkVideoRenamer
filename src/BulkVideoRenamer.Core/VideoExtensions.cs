namespace BulkVideoRenamer.Core;

public static class VideoExtensions
{
    public static readonly string[] Supported =
    [
        ".mp4", ".mov", ".avi", ".mkv", ".webm", ".flv"
    ];

    public static bool IsVideoFile(string path)
    {
        var ext = Path.GetExtension(path);
        return Supported.Contains(ext, StringComparer.OrdinalIgnoreCase);
    }
}
