# CORE — NhaAnMEGA (tự nạp mỗi phiên)

Quản lý suất ăn nhà ăn PF: quét thẻ nhân viên, khách ăn, ảnh quét, báo cáo.
Stack CHỐT: ASP.NET Core MVC `net10.0` · EF Core 10 (PostgreSQL/Npgsql, **DB-First**) · Areas/NhaAnMEGA ·
Session auth · jQuery + Bootstrap · ClosedXML · Newtonsoft.Json.

## Phân bổ file (rút gọn — bảng đầy đủ: `docs/architecture-workflow.md`)
| Loại code | Nơi đặt |
|---|---|
| UI / markup | `Areas/NhaAnMEGA/Views/**/*.cshtml`, partial `_*.cshtml` |
| JS của màn hình | `wwwroot/js/<feature>.js` (1 màn hình = 1 file) |
| Điều phối request | `Areas/NhaAnMEGA/Controllers/*Controller.cs` |
| Business logic | `Areas/NhaAnMEGA/Service/*.cs` |
| Truy vấn DB | `Context/DBThieuITContext.cs`, `Context/DBZktimePFContext.cs` |
| Entity / DTO / ViewModel | `Models/NhaAnMEGA/`, `Models/Zktime/` (scaffold) |
| Hàm dùng chung | `Utils/` hoặc `Helpers/` |

## CẤM TUYỆT ĐỐI
- Trộn UI với nghiệp vụ trong một file; tạo file ngoài cấu trúc trên.
- Gọi DbContext / LINQ query trong `.cshtml`; nhúng `<script>` chứa logic nghiệp vụ trong View.
- Controller "béo": nghiệp vụ dài hoặc truy vấn phức tạp thay vì gọi Service.
- Sửa tay entity DB-First để "đổi schema" — schema đổi ở DB rồi scaffold lại.
- Raw destructive query (`DROP`, `TRUNCATE`, `ALTER ... DROP`, `DELETE`/`UPDATE` không `WHERE`);
  `ExecuteSqlRaw` nối chuỗi từ input người dùng — chỉ tham số hoá.
- Xoá cứng dữ liệu nghiệp vụ (dùng soft-delete); sửa/xoá bản ghi audit log.
- Thêm package mới, đổi framework (SPA/ORM/Razor Pages/Minimal API), đổi auth Session → Identity/JWT.
- Xoá/di chuyển view, route, endpoint đang dùng mà chưa xác nhận.
- Đưa dữ liệu nhân viên / ảnh quét ra dịch vụ ngoài.

## View & tương tác web
- **View là Dumb UI**: nhận model đã chuẩn bị sẵn và render, không hơn.
- **100% thao tác KHÔNG reload trang**: submit/lọc/phân trang/xoá đều `fetch` + cập nhật đúng phần DOM.
- Luôn `event.preventDefault()`; event delegation trên `document`.
- Cấm `location.reload()`, `window.location = ...` để làm mới dữ liệu; cấm form post full-page.
  (Chỉ được điều hướng thật khi fetch lỗi và cần fallback.)
- Endpoint cho UI trả JSON `{ ok, data, message }`; JS tự xử lý loading / lỗi / rỗng.
- Thao tác đổi dữ liệu: POST + validate phía server + kiểm quyền phía server.

## Code style (tóm tắt — đầy đủ: `docs/coding-standards.md`)
- C#: `PascalCase` class/method/property, `_camelCase` private field, `var` khi kiểu đã rõ.
- `Nullable` + `ImplicitUsings` đang bật — không thêm `using` thừa, không tắt cảnh báo nullable.
- Async xuyên suốt cho I/O (`async Task<IActionResult>`, `await ...Async()`); cấm `.Result` / `.Wait()`.
- Controller < ~40 dòng/action: validate → gọi Service → trả kết quả.
- Cấm magic number/string. Cấm `catch {}` nuốt lỗi — bắt có mục đích, log rõ.
- Truy vấn chỉ đọc: `AsNoTracking()`, luôn phân trang, projection thay vì `ToList()` cả bảng.
- Tên biến / comment nghiệp vụ viết **tiếng Việt**; giữ mật độ comment như file xung quanh. UI tiếng Việt.

## Zero-fluff · Output clamping · Hot reload
Quy tắc chung nằm ở `~/.claude/CLAUDE.md` (tự nạp mọi phiên) — không chép lại. Phần riêng .NET:
- Build/test: `dotnet build -v q --nologo`, `dotnet test -v q --nologo`;
  chỉ cần lỗi thì `... | Select-String -Pattern 'error|warning|FAILED'`.
- Dev (BẮT BUỘC, chạy nền): `dotnet watch run --project NhaAnMEGA/NhaAnMEGA.csproj` — không `dotnet run` trần.
  Đổi `Program.cs`/`.csproj`/`appsettings` hoặc watcher báo cần rebuild → restart ngay, không hỏi.

## Subagent (`~/.claude/agents/`)
- `system-architect` — thẩm định kiến trúc, đổi stack, phân tầng. Đọc-only.
- `database-auditor` — bắt buộc review trước mọi migration / thay đổi schema / script dữ liệu.
- `code-reviewer` — review chất lượng & bảo mật trước khi chốt tính năng.

## Lazy-load: task nào → mở file nào
| Task | Đọc |
|---|---|
| Git flow, tạo file mới, quy trình một thay đổi | `docs/architecture-workflow.md` |
| Style, naming, xử lý lỗi, pattern frontend | `docs/coding-standards.md` |
| Cây thư mục, package, route, cạm bẫy đã cắn, lệnh hay dùng | `docs/dotnet-architecture.md` |
| Chạm DB, migration, scaffold, script dữ liệu | `docs/database-safety.md` |
| Nghi ngờ ngoài phạm vi, vùng xám | `docs/project-scope.md` |
| State hiện tại, blocker | `plans/00-context-memory.md` |
| Tra quyết định cũ | `plans/decision-log.md` |
| Lộ trình theo phase | `plans/00-master-plan.md`, `plans/phase*.md` |
