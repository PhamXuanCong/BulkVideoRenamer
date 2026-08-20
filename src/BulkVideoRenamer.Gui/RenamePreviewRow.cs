using BulkVideoRenamer.Core.Models;

namespace BulkVideoRenamer.Gui;

public class RenamePreviewRow(RenameItem item)
{
    public RenameItem Item { get; } = item;
    public string OriginalName => Item.OriginalName;
    public string NewName => Item.NewName;

    public string StatusText => Item.Status switch
    {
        RenameStatus.Ok => "OK",
        RenameStatus.NoIdFound => "Không có ID",
        RenameStatus.Skipped => "Bỏ qua (không đổi)",
        RenameStatus.Error => $"Lỗi: {Item.ErrorMessage}",
        _ => Item.Status.ToString()
    };
}
