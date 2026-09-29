# DATABASE SAFETY

Danh sách cấm tuyệt đối (destructive query, `ExecuteSqlRaw` nối chuỗi, xoá cứng, sửa audit log,
sửa tay entity DB-First) nằm ở [`../rules/core.md`](../rules/core.md). File này là quy trình chi tiết.

## Môi trường & chủ sở hữu dữ liệu
- Hai DB SQL Server trên `10.0.193.252`:
  - **ThieuIT** — dữ liệu nội bộ ứng dụng (`Context/DBThieuITContext.cs`).
  - **Zktime** — dữ liệu máy chấm công ZK (`Context/DBZktimePFContext.cs`), **do hệ thống chấm công
    sở hữu**: coi như chỉ-đọc, chỉ ghi vào các bảng suất ăn do ứng dụng này tạo.
- Không có DB dev riêng → **mặc định coi mọi kết nối là production**. Không chắc thì hỏi trước.
- Dữ liệu nhân viên + ảnh quét là dữ liệu cá nhân: không export ra ngoài, không đưa lên dịch vụ ngoài.

## Quy tắc tuyệt đối (mở rộng từ core)
- **Soft-delete** cho dữ liệu nghiệp vụ: cờ trạng thái / `NgayXoa`, không `DELETE`.
- **Idempotency** cho mọi thao tác ghi tự động (quét thẻ, đăng ký suất ăn, đồng bộ ZK1):
  chống trùng bằng khoá tự nhiên **thẻ + ngày + ca** trước khi insert.
- **Audit log append-only**: chỉ thêm, không sửa/xoá.
- Ghi nhiều bảng liên quan → một transaction.
- Trước mỗi thao tác xoá: liệt kê khoá ngoại có thể cascade mất dữ liệu lịch sử.
- Mọi thao tác rủi ro cao phải hỏi xác nhận kèm: câu lệnh chính xác, bảng & số hàng bị chạm, có rollback không.

## Migration / thay đổi schema
Dự án **DB-First**: schema đổi ở DB trước, rồi scaffold lại (lệnh trong [dotnet-architecture.md](dotnet-architecture.md)).
Script SQL thủ công đi kèm phải:
- Có cặp **up + down** chạy được (rollback thật, `down` không được để trống).
- Viết **idempotent**: `IF NOT EXISTS (SELECT 1 FROM sys.columns ...) ALTER TABLE ... ADD ...`.
- Tên mô tả: `Add<Bang>_<Cot>.sql`, `Backfill<...>.sql`.
- Tách rời: script đổi schema ≠ script backfill dữ liệu.
- Đổi cột theo trình tự an toàn: thêm cột mới → backfill → chuyển đọc/ghi → bỏ cột cũ ở release sau.
- Trước khi apply: backup + chạy thử trên bản copy + đối soát số bản ghi trước/sau.
- **Review bởi `database-auditor` trước khi apply** — bắt buộc.

## Truy vấn an toàn
- Đọc: `AsNoTracking()` + projection đúng cột + phân trang. Không `ToList()` cả bảng.
- Tham số hoá 100%: `FromSqlInterpolated($"... {bienDauVao}")` hoặc `SqlParameter`,
  không bao giờ nối chuỗi từ input người dùng.
- Join chéo ThieuIT ↔ Zktime: không làm trong một LINQ query — lọc hẹp từng bên rồi ghép trong bộ nhớ.

## Nợ bảo mật đang tồn tại
`Context/DBThieuITContext.cs:22` và `Context/DBZktimePFContext.cs:36` hardcode connection string
kèm user/password thật trong `OnConfiguring` — đã bị commit vào git.
Hướng xử lý (chờ quyết định, xem [decision-log.md](../plans/decision-log.md)):
đổi mật khẩu SQL → chuyển chuỗi kết nối sang `appsettings.Development.json` / user-secrets đã gitignore
→ inject context qua DI, bỏ `OnConfiguring`.

## Khi nghi ngờ dữ liệu sai
1. Dừng ghi, **không** "sửa nhanh" bằng UPDATE tay.
2. Đếm & khoanh vùng bằng query chỉ đọc (`SELECT COUNT(*) ... WHERE ...`), lưu lại số liệu trước.
3. Dựng script sửa idempotent + script rollback, đưa `database-auditor` review.
4. Chạy trên bản copy, đối soát, rồi mới xin xác nhận apply production.
