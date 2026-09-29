# ARCHITECTURE & WORKFLOW

Quy tắc cấm & phân bổ rút gọn: [`../rules/core.md`](../rules/core.md). File này là bản đầy đủ về
git flow, chỗ đặt file mới và quy trình cho mỗi thay đổi.

## Git flow
- Nhánh chính `master`. Thay đổi lớn: `feature/<ten>`, `fix/<ten>`, `hotfix/<ten>` — cấm commit thẳng.
- 1 commit = 1 mục đích. Message `<scope>: <việc đã làm>`
  (vd `khachan: them api them-khach khong reload`, `baocao: xuat excel theo ca`).
- Rebase/merge về `master` chỉ sau khi `dotnet build -v q --nologo` sạch và đã review.
- Không commit: secrets, connection string thật, `bin/`, `obj/`, `.vs/`, `App_Data/Keys/`,
  ảnh sinh ra lúc chạy.

## Tạo file mới ở đâu
| Cần thêm | Đặt tại | Ghi chú |
|---|---|---|
| Màn hình mới | `Areas/NhaAnMEGA/Views/NhaAnPF/<Ten>.cshtml` | layout dùng chung của Views/Shared |
| Mảnh UI tái dùng | `Areas/NhaAnMEGA/Views/Shared/_<Ten>.cshtml` | partial, không chứa logic |
| JS của màn hình | `wwwroot/js/<feature>.js` | 1 màn hình = 1 file; dùng chung → `quanly-chung.js` |
| CSS riêng | `wwwroot/css/<feature>.css` | không sửa `lib/` |
| Action mới | `Areas/NhaAnMEGA/Controllers/NhaAnMEGAController.cs` | < ~40 dòng, không nghiệp vụ |
| Nghiệp vụ / hosted service | `Areas/NhaAnMEGA/Service/<Ten>Service.cs` | vd `QuetTheService`, `DongBoZk1Service` |
| Xuất Excel | `Areas/NhaAnMEGA/Service/<Ten>ExcelService.cs` | ClosedXML, không nhét vào Controller |
| Truy vấn DB | qua `DBThieuITContext` / `DBZktimePFContext` | entity DB-First, không sửa tay |
| ViewModel / DTO | `Models/NhaAnMEGA/` | entity scaffold nằm ở `Models/Zktime/`, đừng trộn |
| Hàm dùng chung | `Utils/` hoặc `Helpers/` | tạo mới khi thực sự có ≥2 nơi dùng |
| Tool chạy rời | `Tools/<TenTool>/` | console riêng, không nhét vào web project |

## Quy trình cho mỗi thay đổi
1. Xác định phạm vi: nằm trong [project-scope.md](project-scope.md)? Chạm schema? Chạm UI?
2. Đọc đúng file docs theo bảng lazy-load trong `core.md` — không nạp cả `.claude/`.
3. Chạy dev bằng hot reload (lệnh trong `core.md`), giữ chạy nền suốt phiên.
4. Sửa đúng chỗ theo bảng phân bổ; UI mới thì View render + `wwwroot/js/<feature>.js` gọi fetch.
5. `dotnet build -v q --nologo` sạch.
6. Gọi subagent review (bảng dưới), sửa theo phát hiện mức Blocker.
7. Cập nhật [00-context-memory.md](../plans/00-context-memory.md) nếu đổi task / chốt quyết định
   (quyết định cũ đẩy xuống [decision-log.md](../plans/decision-log.md) khi quá 5 dòng).
8. Commit theo mẫu trên — chỉ khi người dùng yêu cầu.

## Khi nào gọi subagent nào
| Tình huống | Subagent |
|---|---|
| Đề xuất đổi tầng/stack, thêm package, tách Service/Repository | `system-architect` |
| Mọi thay đổi schema, migration, script sửa dữ liệu, scaffold lại | `database-auditor` (bắt buộc trước khi apply) |
| Trước khi chốt một tính năng, hoặc code chạm auth/quyền/input người dùng | `code-reviewer` |
