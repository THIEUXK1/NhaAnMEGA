# .NET ARCHITECTURE — NhaAnMEGA

## Cây thư mục
```
NhaAnMEGA.slnx
NhaAnMEGA/
  Program.cs                      pipeline + route + đăng ký service
  appsettings.json                cấu hình chung (KHÔNG chứa secret)
  appsettings.Development.json    cấu hình dev
  App_Data/Keys/                  khoá DataProtection ghi ra đĩa — KHÔNG commit
  Areas/NhaAnMEGA/
    Controllers/NhaAnMEGAController.cs
    Service/                      QuetTheService · DongBoZk1Service · GhiNhoDangNhapService
                                  · BaoCaoNgayExcelService · XoaAnhDinhKi
    Views/NhaAnPF/                Login · Index · Index1 · IndexAnh · KhachAn · QuenThe · BaoCaoNgay
    Views/Shared/                 layout + partial dùng chung
  Context/                        DBThieuITContext · DBZktimePFContext
  Models/NhaAnMEGA/  Models/Zktime/   entity scaffold DB-First
  wwwroot/{css,js,lib,Audio}
Tools/DongBoSuatAnPF/  Tools/ZkPullerPF/   console chạy rời
```

## Phụ thuộc (mỗi gói một lý do)
| Gói | Vì sao |
|---|---|
| `Microsoft.EntityFrameworkCore` 10.0.2 + `.SqlServer` | truy cập 2 DB SQL Server |
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

## Route
| Pattern | Ghi chú |
|---|---|
| `{area:exists}/{controller=NhaAnPF}/{action=Login}/{id?}` | route Area chính |
| `{controller=NhaAnPF}/{action=Login}/{id?}` | route mặc định |
| `GET /` | redirect cứng về `/PF/Login` |

Area route prefix thực tế dùng là `/PF/...`. Đổi route = đổi mọi URL trong JS — phải rà `wwwroot/js/`.

## Cạm bẫy đã thực sự cắn
- **Connection string hardcode trong `Context/*.cs`** (`OnConfiguring` → `UseSqlServer("...")`,
  kèm user/password thật). Đây là nợ bảo mật đang tồn tại, xem [database-safety.md](database-safety.md).
- **Hai DbContext song song** `DBThieuITContext` (ThieuIT, nội bộ) và `DBZktimePFContext` (Zktime,
  máy chấm công). Join chéo hai DB trong LINQ là không được — phải lấy về rồi ghép trong bộ nhớ,
  hoặc lọc sẵn theo khoá.
- **Scaffold `--force` ghi đè cả context**, xoá mất chỉnh sửa tay → đừng sửa tay file scaffold.
- `dotnet watch` bắt được `.cshtml`/`css`/`js`; đổi `Program.cs`/`csproj`/`appsettings` thường cần
  restart thật — thấy chưa đổi thì restart ngay.
- `MapGet("/")` redirect cứng: thêm trang chủ mới mà quên dòng này thì sẽ không bao giờ vào được.
- Hosted service (`DongBoZk1Service`) chạy ngay khi app start — lỗi kết nối DB làm log ngập,
  cần try/catch từng vòng lặp.

## Lệnh hay dùng
```
dotnet watch run --project NhaAnMEGA/NhaAnMEGA.csproj      # dev, bắt buộc
dotnet build -v q --nologo                                  # build sạch
dotnet build -v q --nologo | Select-String 'error|warning'  # chỉ lỗi
dotnet ef dbcontext scaffold "<conn>" Microsoft.EntityFrameworkCore.SqlServer \
  -o Models/Zktime -c DBZktimePFContext --force             # scaffold lại khi schema đổi
```
