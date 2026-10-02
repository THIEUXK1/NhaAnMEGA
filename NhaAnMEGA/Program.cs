using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// 0. Chuỗi kết nối DB NhaAnMEGA: user-secrets (dev) hoặc biến môi trường ConnectionStrings__NhaAnMEGA (máy chủ)
NhaAnMEGA.Utils.ChuoiKetNoi.GiaTri = builder.Configuration.GetConnectionString(NhaAnMEGA.Utils.ChuoiKetNoi.TenKhoa)
    ?? throw new InvalidOperationException($"Chưa cấu hình ConnectionStrings:{NhaAnMEGA.Utils.ChuoiKetNoi.TenKhoa}.");

// 0b. Thư mục lưu ảnh quét: AnhQuet:ThuMuc, bỏ trống thì lưu tại App_Data/AnhQuet trên chính máy chủ
NhaAnMEGA.Utils.ThuMucAnhQuet.GiaTri = NhaAnMEGA.Utils.ThuMucAnhQuet.TuCauHinh(
    builder.Configuration[NhaAnMEGA.Utils.ThuMucAnhQuet.TenKhoa], builder.Environment.ContentRootPath);

// 1. Thêm dịch vụ cho Controller và Views (Cần thiết cho Area và Controller của bạn)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 2. Thêm dịch vụ Session (Bắt buộc vì code của bạn sử dụng Session để lưu đăng nhập)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(14); // Session dài ngày, hết hạn thì cookie ghi nhớ dựng lại
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromDays(14); // Cookie session không mất khi đóng trình duyệt
});

// 2b. Khoá mã hoá cookie ghi nhớ đăng nhập lưu ra đĩa để còn dùng được sau khi khởi động lại ứng dụng
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys")))
    .SetApplicationName("NhaAnMEGA");

builder.Services.AddHttpContextAccessor();

// 3. Dịch vụ chạy ngầm đồng bộ tức thời quẹt thẻ máy chấm công ZK1 sang bảng suất ăn
builder.Services.AddHostedService<NhaAnMEGA.Areas.NhaAnMEGA.Service.DongBoZk1Service>();

var app = builder.Build();

// Cấu hình HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Thay cho MapStaticAssets nếu bạn dùng phiên bản cũ hơn hoặc để đảm bảo tương thích

app.UseRouting();

// 3. Kích hoạt Session
app.UseSession();

// 3b. Dựng lại session từ cookie ghi nhớ để đăng nhập một lần là dùng mãi
app.UseMiddleware<NhaAnMEGA.Areas.NhaAnMEGA.Service.GhiNhoDangNhapMiddleware>();

app.UseAuthorization();

// 4. Cấu hình Route cho Area và thiết lập trang mặc định
app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=NhaAnPF}/{action=Login}/{id?}");

// Cấu hình Route mặc định để khi mở Web là vào thẳng /MG/Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=NhaAnPF}/{action=Login}/{id?}");

// Nếu bạn muốn ép buộc trang chủ "/" trỏ thẳng về "/MG/Login"
app.MapGet("/", context => {
    context.Response.Redirect("/MG/Login");
    return Task.CompletedTask;
});

app.MapRazorPages();

app.Run();