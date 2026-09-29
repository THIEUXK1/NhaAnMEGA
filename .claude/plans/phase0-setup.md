# PHASE 0 — Setup khung dự án, DB & Hot Reload

## 0.1 Khung dự án (đã có)
- `NhaAnMEGA.slnx` → `NhaAnMEGA/NhaAnMEGA.csproj` (`net10.0`, Nullable + ImplicitUsings bật).
- Pipeline `Program.cs`: MVC + RazorPages, Session 14 ngày + cookie ghi nhớ, DataProtection persist
  ra `App_Data/Keys`, HttpContextAccessor, StaticFiles, hosted service `DongBoZk1Service`.
- Cây thư mục & route: [dotnet-architecture.md](../docs/dotnet-architecture.md).

## 0.2 Database
- DB-First, SQL Server, EF Core 10. Hai context: `Context/DBThieuITContext.cs`, `Context/DBZktimePFContext.cs`.
- **Đang nợ:** connection string + mật khẩu hardcode trong `OnConfiguring` của cả hai context.
  Đích đến: `appsettings.Development.json` / user-secrets (đã gitignore) + inject qua DI.
- Lệnh scaffold lại khi schema đổi & quy trình đổi schema: [database-safety.md](../docs/database-safety.md).

## 0.3 Hot Reload (BẮT BUỘC khi dev)
Lệnh + quy tắc chủ động restart: [`../rules/core.md`](../rules/core.md) mục *Hot reload*.
Trường hợp `dotnet watch` không bắt được (`Program.cs`, `.csproj`, `appsettings`):
[dotnet-architecture.md](../docs/dotnet-architecture.md) mục *Cạm bẫy*.

## 0.4 Definition of Done — Phase 0
- [ ] `dotnet build -v q --nologo` sạch cảnh báo.
- [ ] `dotnet watch run` chạy được, sửa `.cshtml` thấy đổi ngay không restart.
- [ ] Connection string không nằm trong file được commit (chưa đạt).
- [ ] `.claudeignore` + `.claude/rules/core.md` + `.claude/docs/` hiện diện ở repo.
