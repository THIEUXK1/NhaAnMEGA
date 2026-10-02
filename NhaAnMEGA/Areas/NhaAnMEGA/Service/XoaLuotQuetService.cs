using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NhaAnMEGA.Context;
using NhaAnMEGA.Models.NhaAnMEGA;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    /// <summary>
    /// Xoá lượt quét trên màn hình báo cáo hằng ngày.
    /// Ngoại lệ đã được duyệt: xoá cứng (bảng Time_RecodePF không có cột xoá mềm),
    /// chỉ tài khoản quản trị được phép và mọi lần xoá đều ghi log.
    /// </summary>
    public static class XoaLuotQuet
    {
        // Tài khoản duy nhất được thấy nút và gọi API xoá lượt quét
        public const string TAI_KHOAN_DUOC_XOA = "thieuxk";

        // Chỉ được xoá lượt quét của các mã nhân viên test
        public static readonly string[] DS_MA_TEST = { "111", "222", "333", "444", "555", "666", "777", "888", "999" };

        private const string KHOA_SESSION_NGUOI_DUNG = "NhaAnPF";

        /// <summary>Tên đăng nhập đang giữ trong session, null nếu chưa đăng nhập.</summary>
        public static string? TenDangNhapHienTai(HttpContext context)
        {
            var chuoiNguoiDung = context.Session.GetString(KHOA_SESSION_NGUOI_DUNG);
            if (string.IsNullOrEmpty(chuoiNguoiDung)) return null;

            return JsonConvert.DeserializeObject<NhanVien>(chuoiNguoiDung)?.TenDangNhap;
        }

        /// <summary>Người dùng hiện tại có quyền xoá lượt quét không (kiểm phía server).</summary>
        public static bool DuocXoa(HttpContext context) =>
            string.Equals(TenDangNhapHienTai(context)?.Trim(), TAI_KHOAN_DUOC_XOA, StringComparison.OrdinalIgnoreCase);

        /// <summary>Xoá đúng 1 lượt quét của mã test theo id; trả về false nếu không tìm thấy hoặc không phải mã test.</summary>
        public static async Task<bool> XoaAsync(DBZktimePFContext context, int id, string nguoiXoa, ILogger logger)
        {
            var luotQuet = await context.TimeRecodePfs.AsNoTracking()
                .Where(x => x.Id == id && x.WorkerId != null && DS_MA_TEST.Contains(x.WorkerId))
                .Select(x => new { x.Id, x.WorkerId, x.WorkerName, x.Bua, x.GName, x.Mealtime })
                .FirstOrDefaultAsync();
            if (luotQuet == null) return false;

            var soDongXoa = await context.TimeRecodePfs
                .Where(x => x.Id == id && x.WorkerId != null && DS_MA_TEST.Contains(x.WorkerId))
                .ExecuteDeleteAsync();

            logger.LogWarning(
                "Xoá lượt quét id={Id} (NV {WorkerId} - {WorkerName}, bữa {Bua}, cổng {GName}, lúc {Mealtime}) bởi {NguoiXoa}",
                luotQuet.Id, luotQuet.WorkerId, luotQuet.WorkerName, luotQuet.Bua, luotQuet.GName, luotQuet.Mealtime, nguoiXoa);

            return soDongXoa > 0;
        }

        /// <summary>Xoá nhanh mọi lượt quét của mã test trong khoảng thời gian; trả về số dòng đã xoá.</summary>
        public static async Task<int> XoaTatCaTestAsync(DBZktimePFContext context, DateTime tu, DateTime den,
                                                        string nguoiXoa, ILogger logger)
        {
            var soDongXoa = await context.TimeRecodePfs
                .Where(x => x.Mealtime >= tu && x.Mealtime <= den
                            && x.WorkerId != null && DS_MA_TEST.Contains(x.WorkerId))
                .ExecuteDeleteAsync();

            logger.LogWarning("Xoá nhanh {SoDong} lượt quét mã test từ {Tu} đến {Den} bởi {NguoiXoa}",
                soDongXoa, tu, den, nguoiXoa);

            return soDongXoa;
        }
    }
}
