using BulkVideoRenamer.Core;
using BulkVideoRenamer.Core.Models;

var options = ParseArgs(args);

if (options is null)
{
    PrintUsage();
    return 1;
}

if (options.Undo)
{
    return RunUndo(options.Folder!);
}

if (options.Folder is null || options.Hashtag is null)
{
    Console.Error.WriteLine("Lỗi: cần cả --folder và --hashtag (hoặc dùng --undo --folder <path>).");
    PrintUsage();
    return 1;
}

return RunRename(options.Folder, options.Hashtag, options.DryRun);

static int RunRename(string folder, string hashtag, bool dryRun)
{
    List<RenameItem> plan;
    try
    {
        plan = RenameService.BuildPlan(folder, hashtag);
    }
    catch (DirectoryNotFoundException ex)
    {
        Console.Error.WriteLine($"Lỗi: {ex.Message}");
        return 1;
    }

    if (plan.Count == 0)
    {
        Console.WriteLine("Không tìm thấy file video nào trong folder này.");
        return 0;
    }

    PrintPreview(plan);

    if (dryRun)
    {
        Console.WriteLine("\n(--dry-run) Chưa đổi tên file nào. Bỏ --dry-run để thực thi thật.");
        return 0;
    }

    Console.Write($"\nĐổi tên {plan.Count} file trên? (y/n): ");
    var answer = Console.ReadLine();
    if (!string.Equals(answer?.Trim(), "y", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Đã huỷ, không đổi gì.");
        return 0;
    }

    var result = RenameService.Execute(plan, folder);

    foreach (var item in result.Items.Where(i => i.Status == RenameStatus.Error))
        Console.Error.WriteLine($"[LỖI] {item.OriginalName}: {item.ErrorMessage}");

    Console.WriteLine(
        $"\nHoàn tất: {result.SucceededCount} đổi tên thành công, " +
        $"{result.SkippedCount} bỏ qua (tên không đổi), " +
        $"{result.ErrorCount} lỗi.");

    return result.ErrorCount > 0 ? 2 : 0;
}

static int RunUndo(string? folder)
{
    if (folder is null)
    {
        Console.Error.WriteLine("Lỗi: --undo cần đi kèm --folder <path>.");
        return 1;
    }

    try
    {
        var result = RenameLogger.Undo(folder);
        foreach (var err in result.Errors)
            Console.Error.WriteLine($"[LỖI] {err}");

        Console.WriteLine($"Undo hoàn tất: {result.SucceededCount} file khôi phục, {result.ErrorCount} lỗi.");
        return result.ErrorCount > 0 ? 2 : 0;
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine($"Lỗi: {ex.Message}");
        return 1;
    }
}

static void PrintPreview(List<RenameItem> plan)
{
    Console.WriteLine("Tên cũ -> Tên mới");
    Console.WriteLine(new string('-', 60));
    foreach (var item in plan)
    {
        var note = item.Status == RenameStatus.NoIdFound ? "  (không tìm thấy ID)" : "";
        Console.WriteLine($"{item.OriginalName}\n  -> {item.NewName}{note}");
    }
}

static void PrintUsage()
{
    Console.WriteLine("""
        Bulk Video Renamer CLI

        Cách dùng:
          renamer --folder <path> --hashtag "<tags>" [--dry-run]
          renamer --undo --folder <path>

        Options:
          --folder <path>     Folder chứa video cần đổi tên (không đệ quy subfolder)
          --hashtag "<tags>"  1 hoặc nhiều hashtag cách nhau bằng dấu cách, vd: "#trend #fyp"
          --dry-run           Chỉ in preview, không đổi tên thật
          --undo              Đảo ngược lần đổi tên gần nhất trong folder (đọc log gần nhất)
        """);
}

static Options? ParseArgs(string[] args)
{
    var options = new Options();

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--folder":
                if (i + 1 >= args.Length) return null;
                options.Folder = args[++i];
                break;
            case "--hashtag":
                if (i + 1 >= args.Length) return null;
                options.Hashtag = args[++i];
                break;
            case "--dry-run":
                options.DryRun = true;
                break;
            case "--undo":
                options.Undo = true;
                break;
            default:
                return null;
        }
    }

    return options;
}

internal class Options
{
    public string? Folder { get; set; }
    public string? Hashtag { get; set; }
    public bool DryRun { get; set; }
    public bool Undo { get; set; }
}
