# MASTER PLAN — NhaAnMEGA

Stack chốt: ASP.NET Core MVC `net10.0` · EF Core 10 (SQL Server, DB-First) · Areas/NhaAnMEGA · Session auth · jQuery + Bootstrap · ClosedXML.
Chi tiết ràng buộc: [`../rules/core.md`](../rules/core.md) · state hiện tại: [00-context-memory.md](00-context-memory.md).

| Phase | Mục tiêu | Trạng thái | Kế hoạch |
|---|---|---|---|
| 0 | Khung dự án, DbContext, Hot Reload | Đã có nền | [phase0-setup.md](phase0-setup.md) |
| 1 | Tính năng cốt lõi (quét thẻ, khách ăn, ảnh, báo cáo) | Đang chạy | [phase1-core-features.md](phase1-core-features.md) |
| 2 | Chuẩn hoá tầng: tách Service/Repository khỏi Controller | Chưa | — |
| 3 | Báo cáo & xuất Excel (ClosedXML) mở rộng, đối soát dữ liệu | Chưa | — |
| 4 | Hardening: phân quyền, audit log, dọn ảnh định kỳ, tối ưu truy vấn | Chưa | — |

## Nguyên tắc xuyên suốt
- Không đổi tech-stack; mọi đề xuất kiến trúc đi qua `system-architect`.
- Mọi thay đổi schema/migration đi qua `database-auditor` — xem [database-safety.md](../docs/database-safety.md).
- View thuần túy + 100% thao tác async không reload trang — xem [architecture-workflow.md](../docs/architecture-workflow.md).