# CONTEXT MEMORY — state tức thời

> Bộ nhớ ngắn hạn. Cập nhật khi đổi task / gặp blocker / chốt quyết định. Không chép lại tài liệu khác.
> Quy tắc bắt buộc đọc mỗi phiên: [`../rules/core.md`](../rules/core.md).

## Ràng buộc CỨNG
Xem `core.md` (View là Dumb UI · 100% không reload trang · dev bằng `dotnet watch run`,
chưa thấy đổi thì restart ngay). Không lặp lại ở đây.

## Đang làm gì
- Sprint: chuẩn hoá bộ khung `.claude/` (rules lõi mỏng + docs tra cứu).
- Task: đã chuyển 4 file `rules/` cũ sang `docs/`, còn lại duy nhất `rules/core.md`.

## Trạng thái repo
- Nhánh `master`, chưa commit phần khung `.claude/` mới.
- Code đang chạy: quét thẻ, khách ăn, quên thẻ, blacklist, check suất ăn, báo cáo ngày + Excel,
  đồng bộ ZK1, tool `Tools/ZkPullerPF`.

## Tool vận hành
- `Tools/DongBoSuatAnPF`: đối soát + ghi bù CHECKINOUT (SENSORID 109) sang `Time_RecodePF`, dùng lại `DongBoZk1Service.DongBoKhoangAsync` nên không lệch nghiệp vụ.
- Đã deploy `C:\a_web\tools\DongBoSuatAnPF` trên 10.0.193.240, Scheduled Task "PF Canteen Sync Time_RecodePF" chạy `--dong-bo --ngay 2` mỗi 2 phút dưới quyền SYSTEM → suất ăn máy chấm công không còn phụ thuộc web app có chạy hay không.

## Blockers
- **Connection string + mật khẩu SQL hardcode** trong `Context/DBThieuITContext.cs:22` và
  `Context/DBZktimePFContext.cs:36`, đã nằm trong lịch sử git. Chờ quyết định hướng xử lý —
  chi tiết ở [../docs/database-safety.md](../docs/database-safety.md).

## 5 quyết định gần nhất
Lịch sử đầy đủ: [decision-log.md](decision-log.md).

| Ngày | Quyết định | Lý do | Hệ quả |
|---|---|---|---|
| 2026-09-21 | `.claude/rules/core.md` + `.claude/docs/` chỉ giữ `core.md`, tài liệu dài chuyển sang `.claude/docs/` | 4 file rules bị tự nạp mỗi phiên, tốn token vô ích | Phần tự nạp còn ~7 KB; task cần chi tiết tự mở `docs/` theo bảng lazy-load |
| 2026-09-21 | Coi mọi kết nối DB là production, không có DB dev riêng | Cả ThieuIT lẫn Zktime đều là DB thật trên 10.0.193.252 | Mọi script ghi phải hỏi xác nhận + có rollback |
| 2026-09-09 | Session IdleTimeout 14 ngày + cookie ghi nhớ, khoá DataProtection persist ra `App_Data/Keys` | Máy quét ở cổng không thể đăng nhập lại mỗi ca | Xoá `App_Data/Keys` hoặc đổi `SetApplicationName` = mất toàn bộ cookie ghi nhớ |
| 2026-09-09 | Hai DbContext song song, không join chéo trong LINQ | ThieuIT và Zktime là hai DB khác nhau | Ghép dữ liệu phải lọc hẹp từng bên rồi gộp trong bộ nhớ |
| 2026-09-09 | DB-First: model trong `Models/` là output scaffold, không sửa tay | Schema do DB/hệ thống chấm công sở hữu | Muốn field tính toán thì tạo ViewModel riêng |

## Ghi chú cho phiên sau
- Chưa commit khung `.claude/` mới — người dùng tự review rồi commit (`docs: ...`).
- Phase 2 (tách Service/Repository khỏi Controller) chưa bắt đầu; `NhaAnMEGAController.cs` đang béo.
