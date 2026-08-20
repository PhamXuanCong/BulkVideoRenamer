# Bulk Video Renamer

Tool đổi tên hàng loạt video: xoá ID `[id]` ở cuối tên file (định dạng yt-dlp) và thêm hashtag do người dùng chỉ định. Xem chi tiết yêu cầu ở [`bulk-video-renamer-spec.md`](./bulk-video-renamer-spec.md).

## Cấu trúc

```
src/BulkVideoRenamer.Core/   Logic dùng chung: quét folder, đổi tên, log/undo
src/BulkVideoRenamer.Cli/    Ứng dụng console
src/BulkVideoRenamer.Gui/    Ứng dụng desktop WPF
tests/BulkVideoRenamer.Core.Tests/  Unit test (xUnit)
```

## Build & test

```
dotnet build BulkVideoRenamer.slnx
dotnet test tests/BulkVideoRenamer.Core.Tests/BulkVideoRenamer.Core.Tests.csproj
```

## CLI

```
# Xem preview, chưa đổi gì
dotnet run --project src/BulkVideoRenamer.Cli -- --folder "D:\Videos" --hashtag "#trend #fyp" --dry-run

# Đổi tên thật (sẽ hỏi xác nhận y/n)
dotnet run --project src/BulkVideoRenamer.Cli -- --folder "D:\Videos" --hashtag "#trend #fyp"

# Khôi phục lần đổi tên gần nhất trong folder (đọc log rename_log_*.csv gần nhất)
dotnet run --project src/BulkVideoRenamer.Cli -- --undo --folder "D:\Videos"
```

Chỉ xử lý file nằm trực tiếp trong folder (không đệ quy). Đuôi file được hỗ trợ: `.mp4 .mov .avi .mkv .webm .flv` (cấu hình ở `VideoExtensions.cs`).

## GUI

```
dotnet run --project src/BulkVideoRenamer.Gui
```

1. Bấm **Chọn folder...** để chọn thư mục chứa video.
2. Nhập hashtag vào ô **Hashtag** (vd: `#trend #fyp`) — bảng preview cập nhật ngay.
3. Kiểm tra cột **Tên mới**, bấm **Đổi tên hàng loạt** để thực thi (có hộp thoại xác nhận), hoặc **Huỷ** để reset form.
4. **Undo lần đổi gần nhất** khôi phục lại tên file dựa trên log CSV gần nhất trong folder đang chọn.

## Hành vi chính

- File không có `[id]` ở cuối tên → giữ nguyên tên, chỉ thêm hashtag, đánh dấu "Không có ID" (không báo lỗi).
- Ký tự không hợp lệ trong tên file Windows (`\ / : * ? " < > |`) bị loại khỏi hashtag trước khi ghép.
- Tên mới trùng file đã có (trên đĩa hoặc trong cùng batch) → tự thêm hậu tố `(1)`, `(2)`... để tránh ghi đè.
- Mỗi lần đổi tên thật ghi 1 file `rename_log_<timestamp>.csv` (OldName, NewName, Timestamp) vào chính folder đó; Undo đọc log gần nhất còn hiệu lực rồi đổi tên ngược lại, sau đó đánh dấu log là `undone_...` để không undo lại lần nữa.
- File đang bị khoá bởi chương trình khác → skip, ghi lỗi, không dừng cả batch. Không có quyền ghi vào folder → báo lỗi rõ ràng.
