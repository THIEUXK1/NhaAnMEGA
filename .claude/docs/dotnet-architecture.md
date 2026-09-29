# .NET ARCHITECTURE — NhaAnMEGA

## Cây thư mục
```
NhaAnMEGA.slnx
NhaAnMEGA/
  Program.cs                      pipeline + route + đăng ký service
  appsettings.json                cấu hình chung (KHÔNG chứa secret)
  appsettings.Development.json    cấu hình dev (không chứa secret)
  App_Data/Keys/                  khoá DataProtection ghi ra đĩa — KHÔNG commit
  Utils/ChuoiKetNoi.cs            chuỗi kết nối dùng chung cho mọi DbContext (xem Secrets)
  Areas/NhaAnMEGA/
    Controllers/NhaAnMEGAController.cs
    Service/                      QuetTheService · DongBoZk1Service · GhiNhoDangNhapService
                                  · BaoCaoNgayExcelService · XoaAnhDinhKi
    Views/NhaAnPF/                Login · Index · Index1 · IndexAnh · KhachAn · QuenThe · BaoCaoNgay
    Views/Shared/                 layout + partial dùng chung
  Context/                        DBThieuITContext · DBZktimePFContext
  Models/NhaAnMEGA/  Models/Zktime/   entity scaffold DB-First
  wwwroot/{css,js,lib,Audio}
Tools/DongBoSuatAnPF/   console, ProjectReference tới web project (dùng lại Service + Context)
Tools/ZkPullerPF/       console đọc máy chấm công → ghi CHECKINOUT qua SqlClient (SQL Server)
```

## Phụ thuộc (mỗi gói một lý do)
| Gói | Vì sao |
|---|---|
| `Microsoft.EntityFrameworkCore` 10.0.4 | ORM cho 2 DbContext |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 | provider PostgreSQL — DB của web app |
| `Microsoft.Data.SqlClient` 6.1.1 (chỉ `Tools/ZkPullerPF`) | ghi CHECKINOUT vào DB Zktime SQL Server của máy chấm công |
| `.Design` + `.Tools` (PrivateAssets) | chỉ để `dotnet ef dbcontext scaffold` lúc dev |
| `ClosedXML` 0.105.0 | xuất Excel báo cáo, không cần cài Office |
| `Newtonsoft.Json` 13.0.4 | dữ liệu trả về từ máy chấm công / tool đồng bộ |

Thêm gói mới phải được duyệt trước — xem [project-scope.md](project-scope.md).

## Quy ước bắt buộc
- `net10.0`, `Nullable` + `ImplicitUsings` bật — không tắt cảnh báo nullable.
- Area duy nhất `NhaAnMEGA`; mọi View/Controller nghiệp vụ nằm trong đó.
- Auth bằng **Session** (IdleTimeout 14 ngày, cookie HttpOnly + IsEssential) +
  middleware `GhiNhoDangNhapMiddleware` dựng lại session từ cookie ghi nhớ.
- Khoá DataProtection persist vào `App_Data/Keys` với `SetApplicationName("NhaAnMEGA")` —
  đổi tên app hoặc xoá thư mục này = mọi cookie ghi nhớ mất hiệu lực.

## Secrets (cơ chế đang dùng — giữ nguyên, không ép sang `.env`)
| Nơi chạy | Chuỗi kết nối lấy từ |
|---|---|
| Web app, dev | user-secrets (`UserSecretsId` trong csproj), khoá `ConnectionStrings:NhaAnMEGA` |
| Web app, máy chủ | biến môi trường `ConnectionStrings__NhaAnMEGA` |
| `Tools/DongBoSuatAnPF` | biến môi trường `ConnectionStrings__NhaAnMEGA` (đọc qua `ChuoiKetNoi.GiaTri`) |
| `Tools/ZkPullerPF` | `appsettings.json` cạnh exe (gitignore) — mẫu `appsettings.example.json` |

Thiếu cấu hình → `Program.cs` ném lỗi ngay lúc khởi động (cố ý, không fallback).
Không commit `appsettings.json` có secret thật; không đưa chuỗi kết nối vào View/JS.

## Route
| Pattern | Ghi chú |
|---|---|
| `{area:exists}/{controller=NhaAnPF}/{action=Login}/{id?}` | route Area chính |
| `{controller=NhaAnPF}/{action=Login}/{id?}` | route mặc định |
| `GET /` | redirect cứng về `/PF/Login` |

Area route prefix thực tế dùng là `/PF/...`. Đổi route = đổi mọi URL trong JS — phải rà `wwwroot/js/`.

## Cạm bẫy đã thực sự cắn
- **DbContext tạo bằng `new ...Context()`, không qua DI** — `OnConfiguring` lấy `ChuoiKetNoi.GiaTri`.
  Scaffold lại sẽ sinh `OnConfiguring` kèm chuỗi kết nối hardcode → phải khôi phục dòng
  `UseNpgsql(ChuoiKetNoi.GiaTri)` trước khi commit.
- **Hai DbContext song song** `DBThieuITContext` và `DBZktimePFContext` (cùng một chuỗi kết nối).
  Không join chéo hai context trong một LINQ query — lọc hẹp từng bên rồi ghép trong bộ nhớ.
- **Scaffold `--force` ghi đè cả context**, xoá mất chỉnh sửa tay → đừng sửa tay file scaffold.
- **Hai hệ DB khác nhau**: web app dùng PostgreSQL, `ZkPullerPF` ghi SQL Server — SQL mẫu/hàm
  (`LOCALTIMESTAMP`, `ON CONFLICT`, `ILIKE`…) không dùng lẫn giữa hai bên.
- `dotnet watch` bắt được `.cshtml`/`css`/`js`; đổi `Program.cs`/`csproj`/`appsettings` thường cần
  restart thật — thấy chưa đổi thì restart ngay.
- `MapGet("/")` redirect cứng: thêm trang chủ mới mà quên dòng này thì sẽ không bao giờ vào được.
- Hosted service (`DongBoZk1Service`) chạy ngay khi app start — lỗi kết nối DB làm log ngập,
  cần try/catch từng vòng lặp.

## Lệnh hay dùng
```
# dev/hot reload: lệnh + quy tắc restart ở ../rules/core.md
dotnet build -v q --nologo                                  # build sạch
dotnet build -v q --nologo | Select-String 'error|warning'  # chỉ lỗi
dotnet user-secrets set "ConnectionStrings:NhaAnMEGA" "<conn>" --project NhaAnMEGA   # cấu hình dev
dotnet ef dbcontext scaffold "Name=ConnectionStrings:NhaAnMEGA" Npgsql.EntityFrameworkCore.PostgreSQL \
  -o Models/Zktime -c DBZktimePFContext --force             # scaffold lại khi schema đổi (xem Cạm bẫy)
```
