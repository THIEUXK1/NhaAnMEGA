# DATABASE SAFETY

Danh sách cấm tuyệt đối (destructive query, `ExecuteSqlRaw` nối chuỗi, xoá cứng, sửa audit log,
sửa tay entity DB-First) nằm ở [`../rules/core.md`](../rules/core.md). File này là quy trình chi tiết.

## Môi trường & chủ sở hữu dữ liệu
- **PostgreSQL — DB của web app** (khoá cấu hình `ConnectionStrings:NhaAnMEGA`), dùng chung cho
  `Context/DBThieuITContext.cs` (nhân viên) và `Context/DBZktimePFContext.cs` (CHECKINOUT, USERINFO,
  suất ăn, khách ăn, blacklist…). `Tools/DongBoSuatAnPF` ghi vào cùng DB này.
- **SQL Server `Zktime`** — DB của hệ thống chấm công; chỉ `Tools/ZkPullerPF` ghi vào (CHECKINOUT).
  Do hệ thống chấm công sở hữu: ngoài CHECKINOUT từ máy quét, coi như chỉ-đọc.
- Cơ chế giữ chuỗi kết nối: [dotnet-architecture.md](dotnet-architecture.md) mục *Secrets*.
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
- Viết **idempotent** (PostgreSQL): `ALTER TABLE ... ADD COLUMN IF NOT EXISTS ...`,
  `CREATE INDEX IF NOT EXISTS ...`, insert dữ liệu gốc bằng `ON CONFLICT DO NOTHING`;
  up/down bọc trong `BEGIN; ... COMMIT;` (DDL của PostgreSQL rollback được).
- Tên mô tả: `Add<Bang>_<Cot>.sql`, `Backfill<...>.sql`.
- Tách rời: script đổi schema ≠ script backfill dữ liệu.
- Đổi cột theo trình tự an toàn: thêm cột mới → backfill → chuyển đọc/ghi → bỏ cột cũ ở release sau.
- Trước khi apply: backup + chạy thử trên bản copy + đối soát số bản ghi trước/sau.
- **Review bởi `database-auditor` trước khi apply** — bắt buộc.

## Truy vấn an toàn
Nguyên tắc đọc (AsNoTracking, projection, phân trang) nằm ở `core.md`; ở đây là cách làm:
- SQL thô trong EF: `FromSqlInterpolated($"... {bienDauVao}")` / `ExecuteSqlInterpolatedAsync`,
  hoặc `NpgsqlParameter`. `Tools/ZkPullerPF` (SqlClient): `SqlParameter`, không nối chuỗi.
- Ghép dữ liệu hai DbContext: xem [dotnet-architecture.md](dotnet-architecture.md) mục *Cạm bẫy*.

## Khi nghi ngờ dữ liệu sai
1. Dừng ghi, **không** "sửa nhanh" bằng UPDATE tay.
2. Đếm & khoanh vùng bằng query chỉ đọc (`SELECT COUNT(*) ... WHERE ...`), lưu lại số liệu trước.
3. Dựng script sửa idempotent + script rollback, đưa `database-auditor` review.
4. Chạy trên bản copy, đối soát, rồi mới xin xác nhận apply production.
