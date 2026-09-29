# PHASE 1 — Tính năng cốt lõi

Phạm vi bám theo code hiện có trong `Areas/NhaAnMEGA/`. Ranh giới: [project-scope.md](../docs/project-scope.md).

| # | Tính năng | Điểm neo trong code | Ghi chú |
|---|---|---|---|
| 1 | Đăng nhập / phiên làm việc | `NhaAnMEGAController` action `Login`, Session 14 ngày + cookie ghi nhớ | Chống brute-force, không log mật khẩu |
| 2 | Quét thẻ & ghi nhận suất ăn | `Models/Zktime/{LuuQuetPf,TimeRecodePf,Checkinout}` | Chống quét trùng (idempotent theo thẻ + ca) |
| 3 | Khách ăn | `Views/NhaAnPF/KhachAn.cshtml`, `Models/Zktime/KhachAnPf` | Thêm/sửa/xoá bằng Ajax, không reload |
| 4 | Blacklist / cổng | `BlacklistPf`, `GatePf` | Phản hồi âm thanh `wwwroot/Audio` |
| 5 | Ảnh quét & dọn ảnh định kỳ | `IndexAnh.cshtml`, `Service/XoaAnhDinhKi.cs` | Xoá theo tuổi file, có log, không xoá ngoài thư mục đích |
| 6 | Báo cáo & xuất Excel | ClosedXML | Truy vấn `AsNoTracking`, phân trang |

## Nguyên tắc thực thi Phase 1
- Phân tầng, quy tắc AJAX, truy vấn an toàn: [`../rules/core.md`](../rules/core.md) — không lặp lại ở đây.
- Mỗi tính năng làm trọn một lát: View render → `wwwroot/js/<feature>.js` → action JSON → Service → Context.

## Definition of Done — mỗi tính năng
- [ ] Không có logic/query trong `.cshtml`.
- [ ] Thao tác chạy async, không reload, có trạng thái loading + báo lỗi trên UI.
- [ ] Input được validate phía server; không nối chuỗi SQL.
- [ ] Đã qua review bởi `code-reviewer`; phần chạm DB qua `database-auditor`.