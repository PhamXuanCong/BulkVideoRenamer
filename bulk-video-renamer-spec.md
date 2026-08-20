# Spec: Tool Đổi Tên Hàng Loạt Video (Bulk Video Renamer)

## 1. Mục tiêu
Xây dựng một tool cho phép người dùng:
1. Chọn 1 folder chứa video.
2. Tool tự động đổi tên toàn bộ file video trong folder đó theo 2 bước:
   - **Bước 1:** Xoá phần ID ở cuối tên file, được bọc trong dấu ngoặc vuông `[id]`.
   - **Bước 2:** Thêm hashtag do người dùng chỉ định vào cuối tên file (sau khi đã xoá ID).

## 2. Ví dụ minh hoạ

| Tên file gốc | Sau khi xoá `[id]` | Sau khi thêm hashtag `#trend` |
|---|---|---|
| `Con mèo dễ thương [7123456789012345678].mp4` | `Con mèo dễ thương.mp4` | `Con mèo dễ thương #trend.mp4` |
| `Funny dog compilation [7098765432109876543].mov` | `Funny dog compilation.mov` | `Funny dog compilation #trend.mov` |

> **Giả định:** ID luôn nằm ở cuối tên file (ngay trước phần mở rộng), bọc trong `[...]`, gồm số hoặc chữ (kiểu file tải bằng yt-dlp: `%(title)s [%(id)s].%(ext)s`). Nếu format thực tế khác (có dấu `_`/`-` trước ngoặc, hoặc ID nằm ở đầu tên...), sửa lại regex ở mục 3.3 cho khớp — nên nói rõ với Claude Code ví dụ tên file thật trước khi code.

## 3. Yêu cầu chức năng chi tiết

### 3.1 Chọn folder
- Folder picker dialog, chỉ xử lý file nằm trực tiếp trong folder được chọn (không đệ quy vào subfolder).

### 3.2 Lọc file video
- Chỉ xử lý các file có đuôi: `.mp4, .mov, .avi, .mkv, .webm, .flv` (nên để dạng list config, dễ thêm/bớt).

### 3.3 Xoá ID
- Regex đề xuất, áp dụng lên phần tên (không tính extension):
  `\s*\[[^\[\]]*\]$`
- Nếu file không có pattern `[id]` ở cuối → giữ nguyên tên gốc, chỉ thêm hashtag, không báo lỗi (chỉ log lại là "không tìm thấy ID").

### 3.4 Thêm hashtag
- Người dùng nhập 1 hoặc nhiều hashtag trong 1 ô, cách nhau bằng dấu cách, ví dụ: `#trending #fyp`.
- Hashtag được nối vào cuối tên (sau khi đã xoá ID xong), cách tên gốc bằng 1 dấu cách.
- Sanitize: loại các ký tự không hợp lệ trong tên file Windows: `\ / : * ? " < > |`.

### 3.5 Preview trước khi đổi tên thật (bắt buộc)
- Hiển thị danh sách "Tên cũ → Tên mới" cho người dùng xem trước.
- Nút "Đổi tên hàng loạt" để thực thi, nút "Huỷ" để thoát không đổi gì.

### 3.6 Xử lý trùng tên
- Nếu tên mới trùng file đã tồn tại → tự thêm hậu tố `(1)`, `(2)`,... để tránh ghi đè.

### 3.7 Log / Undo
- Ghi log mỗi lần đổi tên ra file `.csv`/`.log` ngay trong folder đó (tên cũ, tên mới, thời gian).
- (Nâng cao, không bắt buộc) Cho phép Undo lần đổi tên gần nhất dựa vào log này.

### 3.8 Xử lý lỗi
- File đang bị khoá/mở bởi chương trình khác → skip, log lại, không crash cả tool.
- Không có quyền ghi vào folder → báo lỗi rõ ràng cho người dùng.

## 4. Đề xuất giao diện & tech stack

**Hướng A — Desktop app (WPF hoặc WinForms):**
- Nút "Chọn folder"
- Ô nhập hashtag
- DataGridView/ListView hiển thị preview (Tên cũ | Tên mới)
- Nút "Đổi tên hàng loạt"

**Hướng B — CLI đơn giản (dotnet console), làm MVP nhanh trước:**
```
renamer --folder "D:\Videos" --hashtag "#trend" --dry-run
```
- `--dry-run`: chỉ in preview ra console, không đổi tên thật.
- Bỏ `--dry-run` thì thực thi đổi tên.

## 5. Cấu trúc project đề xuất
```
BulkVideoRenamer/
├── Program.cs               // hoặc MainWindow.xaml/.cs nếu WPF
├── RenameService.cs         // logic xoá ID + thêm hashtag
├── FileScanner.cs           // quét folder, lọc file video
├── RenameLogger.cs          // ghi log/undo
└── README.md
```

## 6. Tiêu chí hoàn thành (Acceptance Criteria)
- [ ] Chọn được folder bất kỳ.
- [ ] Xoá đúng pattern `[id]` ở cuối tên, giữ nguyên tên nếu không có ID.
- [ ] Thêm đúng hashtag người dùng nhập, đã sanitize ký tự không hợp lệ.
- [ ] Preview đầy đủ trước khi đổi tên thật.
- [ ] Không đè file khi trùng tên.
- [ ] Có log lịch sử đổi tên.
- [ ] Không crash khi gặp file lỗi/bị khoá.

---
**Ghi chú cho Claude Code:** Trước khi code, hỏi lại người dùng nếu regex ở mục 3.3 không khớp định dạng file thực tế. Ưu tiên implement Hướng B (CLI) trước để có MVP nhanh, sau đó nâng cấp lên GUI (Hướng A) nếu cần dùng thường xuyên.
