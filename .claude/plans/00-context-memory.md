# CONTEXT MEMORY — state tức thời

> Bộ nhớ ngắn hạn. Cập nhật khi đổi task / gặp blocker / chốt quyết định. Không chép lại tài liệu khác.
> Quy tắc bắt buộc đọc mỗi phiên: [`../rules/core.md`](../rules/core.md).

## Ràng buộc CỨNG
Xem `core.md` (View là Dumb UI · 100% không reload trang · dev bằng `dotnet watch run`,
chưa thấy đổi thì restart ngay). Không lặp lại ở đây.

## Đang làm gì
- Sprint: chuẩn hoá bộ khung `.claude/` (rules lõi mỏng + docs tra cứu).
- Task 2026-09-29: rà khung theo code thật — docs cập nhật PostgreSQL + cơ chế secrets, nhánh `main`.

## Trạng thái repo
- Nhánh `main`, 1 commit `140c998` (đã gồm khung `.claude/`); bản rà 2026-09-29 chưa commit.
- Code đang chạy: quét thẻ, khách ăn, quên thẻ, blacklist, check suất ăn, báo cáo ngày + Excel,
  đồng bộ ZK1, tool `Tools/ZkPullerPF`.

## Tool vận hành
- `Tools/DongBoSuatAnPF`: đối soát + ghi bù CHECKINOUT (SENSORID 109) sang `Time_RecodePF`, dùng lại `DongBoZk1Service.DongBoKhoangAsync` nên không lệch nghiệp vụ.
- Đã deploy `C:\a_web\tools\DongBoSuatAnPF` trên 10.0.193.240, Scheduled Task "PF Canteen Sync Time_RecodePF" chạy `--dong-bo --ngay 2` mỗi 2 phút dưới quyền SYSTEM → suất ăn máy chấm công không còn phụ thuộc web app có chạy hay không.

## Blockers
- Không có. (Nợ connection string hardcode đã xử lý: `git grep` trên `140c998` không còn mật khẩu thật.)
- Câu hỏi mở: DB PostgreSQL nằm trên máy chủ nào, và CHECKINOUT từ `ZkPullerPF` (SQL Server Zktime)
  vào PostgreSQL bằng đường nào — cần người dùng xác nhận rồi ghi vào `docs/database-safety.md`.

## 5 quyết định gần nhất
Lịch sử đầy đủ: [decision-log.md](decision-log.md).

| Ngày | Quyết định | Lý do | Hệ quả |
|---|---|---|---|
| 2026-09-29 (ghi nhận) | Web app chuyển sang PostgreSQL (Npgsql); chuỗi kết nối qua user-secrets / biến môi trường `ConnectionStrings__NhaAnMEGA` qua `Utils/ChuoiKetNoi` | Bỏ secret hardcode trong `Context/*.cs` | Scaffold lại phải khôi phục `UseNpgsql(ChuoiKetNoi.GiaTri)`; thiếu cấu hình → app không khởi động |
| 2026-09-21 | `.claude/rules/core.md` + `.claude/docs/` chỉ giữ `core.md`, tài liệu dài chuyển sang `.claude/docs/` | 4 file rules bị tự nạp mỗi phiên, tốn token vô ích | Phần tự nạp còn ~7 KB; task cần chi tiết tự mở `docs/` theo bảng lazy-load |
| 2026-09-21 | Coi mọi kết nối DB là production, không có DB dev riêng | Cả ThieuIT lẫn Zktime đều là DB thật trên 10.0.193.252 | Mọi script ghi phải hỏi xác nhận + có rollback |
| 2026-09-09 | Session IdleTimeout 14 ngày + cookie ghi nhớ, khoá DataProtection persist ra `App_Data/Keys` | Máy quét ở cổng không thể đăng nhập lại mỗi ca | Xoá `App_Data/Keys` hoặc đổi `SetApplicationName` = mất toàn bộ cookie ghi nhớ |
| 2026-09-09 | Hai DbContext song song, không join chéo trong LINQ | ThieuIT và Zktime là hai DB khác nhau | Ghép dữ liệu phải lọc hẹp từng bên rồi gộp trong bộ nhớ |

## Ghi chú cho phiên sau
- Bản rà khung 2026-09-29 chưa commit — người dùng tự review rồi commit (`docs: ...`).
- Phase 2 (tách Service/Repository khỏi Controller) chưa bắt đầu; `NhaAnMEGAController.cs` đang béo.
