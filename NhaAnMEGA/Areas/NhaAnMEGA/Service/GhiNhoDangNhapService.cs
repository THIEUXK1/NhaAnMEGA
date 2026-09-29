using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NhaAnMEGA.Context;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    /// <summary>
    /// Giữ đăng nhập lâu dài: khi đăng nhập thành công thì phát một cookie đã mã hoá chứa tên đăng nhập.
    /// Session vẫn là nơi giữ thông tin người dùng như cũ, cookie chỉ dùng để dựng lại session
    /// khi session hết hạn hoặc khi ứng dụng khởi động lại.
    /// </summary>
    public static class GhiNhoDangNhap
    {
        public const string TEN_COOKIE = "PF_GhiNho";
        public const string MUC_DICH_MA_HOA = "NhaAnMEGA.GhiNhoDangNhap.v1";

        // Cookie được gia hạn lại mỗi lần dùng nên xem như dùng vĩnh viễn trên máy đó
        private static readonly TimeSpan HAN_COOKIE = TimeSpan.FromDays(365);

        private static IDataProtector LayBoMaHoa(HttpContext context)
        {
            var nhaCungCap = context.RequestServices.GetRequiredService<IDataProtectionProvider>();
            return nhaCungCap.CreateProtector(MUC_DICH_MA_HOA);
        }

        private static CookieOptions TuyChonCookie(HttpContext context) => new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = context.Request.IsHttps,
            Expires = DateTimeOffset.Now.Add(HAN_COOKIE)
        };

        /// <summary>Phát cookie ghi nhớ sau khi đăng nhập thành công.</summary>
        public static void Luu(HttpContext context, string tenDangNhap)
        {
            var maHoa = LayBoMaHoa(context).Protect(tenDangNhap);
            context.Response.Cookies.Append(TEN_COOKIE, maHoa, TuyChonCookie(context));
        }

        /// <summary>Xoá cookie ghi nhớ (dùng khi đăng xuất).</summary>
        public static void Xoa(HttpContext context)
        {
            context.Response.Cookies.Delete(TEN_COOKIE);
        }

        /// <summary>Đọc tên đăng nhập từ cookie; trả về null nếu không có hoặc cookie đã bị sửa.</summary>
        public static string? DocTenDangNhap(HttpContext context)
        {
            var maHoa = context.Request.Cookies[TEN_COOKIE];
            if (string.IsNullOrEmpty(maHoa)) return null;

            try
            {
                return LayBoMaHoa(context).Unprotect(maHoa);
            }
            catch (Exception)
            {
                // Cookie hỏng hoặc khoá mã hoá đã đổi → coi như chưa ghi nhớ
                return null;
            }
        }

        /// <summary>Có cookie ghi nhớ hợp lệ hay không, dùng để bỏ qua hạn 3 tiếng của session.</summary>
        public static bool CoGhiNho(HttpContext context) => DocTenDangNhap(context) != null;
    }

    /// <summary>
    /// Chạy trước controller: nếu session trống mà máy người dùng còn cookie ghi nhớ thì
    /// nạp lại nhân viên từ DB và dựng lại session, người dùng không phải đăng nhập lại.
    /// </summary>
    public class GhiNhoDangNhapMiddleware
    {
        private readonly RequestDelegate _tiepTheo;

        public GhiNhoDangNhapMiddleware(RequestDelegate tiepTheo)
        {
            _tiepTheo = tiepTheo;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Session.GetString("NhaAnPF") == null)
            {
                var tenDangNhap = GhiNhoDangNhap.DocTenDangNhap(context);
                if (tenDangNhap != null)
                {
                    await DungLaiSessionAsync(context, tenDangNhap);
                }
            }

            await _tiepTheo(context);
        }

        private static async Task DungLaiSessionAsync(HttpContext context, string tenDangNhap)
        {
            using var db = new DBThieuITContext();

            var nhanVien = await db.NhanViens
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.TenDangNhap == tenDangNhap && u.TrangThai == true);

            if (nhanVien == null)
            {
                // Tài khoản bị khoá hoặc đã xoá → bỏ cookie để lần sau vào thẳng trang đăng nhập
                GhiNhoDangNhap.Xoa(context);
                return;
            }

            context.Session.SetString("NhaAnPF", JsonConvert.SerializeObject(nhanVien));
            context.Session.SetString("LoginTime", DateTime.Now.ToString());
            context.Session.SetInt32("UserChucVu", nhanVien.ChucVu ?? 0);

            // Gia hạn cookie để càng dùng càng lâu hết hạn
            GhiNhoDangNhap.Luu(context, tenDangNhap);
        }
    }
}
