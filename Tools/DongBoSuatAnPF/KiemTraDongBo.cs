using Microsoft.EntityFrameworkCore;
using NhaAnMEGA.Areas.NhaAnMEGA.Service;
using NhaAnMEGA.Context;

namespace DongBoSuatAnPF
{
    // Một lượt quẹt máy ZK1 chưa có bản ghi suất ăn tương ứng
    public record LuotThieu(string MaThe, string TenNhanVien, DateTime ThoiDiem, string LyDo);

    // Số liệu đối soát của một ngày
    public record DongNgay(DateTime Ngay, int TongLuot, int DaGhi, int ThieuDoTrung, int ThieuThatSu);

    public record KetQuaKiemTra(
        List<DongNgay> TheoNgay,
        List<LuotThieu> DanhSachThieu,
        int LuotKhongCoNhanSu)
    {
        public int TongLuot => TheoNgay.Sum(x => x.TongLuot);
        public int TongDaGhi => TheoNgay.Sum(x => x.DaGhi);
        public int TongThieuThatSu => TheoNgay.Sum(x => x.ThieuThatSu);
        public int TongThieuDoTrung => TheoNgay.Sum(x => x.ThieuDoTrung);
    }

    /// <summary>
    /// Đối soát CHECKINOUT (SENSORID 109) với Time_RecodePF theo đúng khoá chống trùng mà
    /// <see cref="DongBoZk1Service"/> dùng: mã thẻ + đúng giây quẹt. Chỉ đọc, không ghi gì.
    ///
    /// Lượt thiếu được tách hai loại:
    /// - "trùng 2h": người đó đã có suất khác trong vòng <see cref="QuetTheService.GioGiuaHaiSuat"/> giờ,
    ///   nghiệp vụ cố tình không cấp thêm suất nên KHÔNG phải lỗi đồng bộ.
    /// - "chưa đồng bộ": không có suất nào quanh đó, tức là lượt quẹt bị bỏ sót thật.
    /// </summary>
    public static class KiemTraDongBo
    {
        public static async Task<KetQuaKiemTra> ChayAsync(
            DBZktimePFContext context, DateTime tu, DateTime den, CancellationToken token = default)
        {
            // Lượt quẹt máy ZK1 kèm mã thẻ; hồ sơ không có SSN thì không thể quy ra suất ăn
            var danhSachZk = await context.Checkinouts
                .AsNoTracking()
                .Where(c => c.Sensorid == DongBoZk1Service.SensorIdZk1 && c.Checktime >= tu && c.Checktime <= den)
                .Join(
                    context.Userinfos.AsNoTracking(),
                    check => check.Userid,
                    user => user.Userid,
                    (check, user) => new { user.Ssn, user.Name, check.Checktime })
                .OrderBy(x => x.Checktime)
                .ToListAsync(token);

            var luotKhongCoNhanSu = danhSachZk.Count(x => string.IsNullOrWhiteSpace(x.Ssn));
            var luotCoMa = danhSachZk
                .Where(x => !string.IsNullOrWhiteSpace(x.Ssn))
                .Select(x => new { MaThe = x.Ssn!.Trim(), Ten = x.Name?.Trim() ?? string.Empty, x.Checktime })
                .ToList();

            var danhSachMa = luotCoMa.Select(x => x.MaThe).Distinct().ToList();

            // Nới hai đầu khoảng để nhìn thấy suất ăn nằm ngay ngoài biên khi xét luật trùng 2 giờ
            var tuNoiRong = tu.AddHours(-QuetTheService.GioGiuaHaiSuat);
            var denNoiRong = den.AddHours(QuetTheService.GioGiuaHaiSuat);

            var banGhiDaCo = await context.TimeRecodePfs
                .AsNoTracking()
                .Where(t => t.WorkerId != null && danhSachMa.Contains(t.WorkerId)
                            && t.DateTime >= tuNoiRong && t.DateTime <= denNoiRong)
                .Select(t => new { t.WorkerId, t.DateTime })
                .ToListAsync(token);

            var suatTheoMa = banGhiDaCo
                .Where(x => x.DateTime.HasValue)
                .GroupBy(x => x.WorkerId!.Trim())
                .ToDictionary(g => g.Key, g => g.Select(x => x.DateTime!.Value).ToList());

            var khoangTrung = TimeSpan.FromHours(QuetTheService.GioGiuaHaiSuat);
            var theoNgay = new Dictionary<DateTime, (int Tong, int DaGhi, int Trung, int Thieu)>();
            var danhSachThieu = new List<LuotThieu>();

            foreach (var luot in luotCoMa)
            {
                var ngay = luot.Checktime.Date;
                var so = theoNgay.TryGetValue(ngay, out var cu) ? cu : (Tong: 0, DaGhi: 0, Trung: 0, Thieu: 0);
                so.Tong++;

                var suatCuaMa = suatTheoMa.TryGetValue(luot.MaThe, out var ds) ? ds : new List<DateTime>();

                if (suatCuaMa.Any(x => x == luot.Checktime))
                {
                    so.DaGhi++;
                }
                else if (suatCuaMa.Any(x => (x - luot.Checktime).Duration() <= khoangTrung))
                {
                    so.Trung++;
                }
                else
                {
                    so.Thieu++;
                    danhSachThieu.Add(new LuotThieu(luot.MaThe, luot.Ten, luot.Checktime, "chưa đồng bộ"));
                }

                theoNgay[ngay] = so;
            }

            var dong = theoNgay
                .OrderBy(x => x.Key)
                .Select(x => new DongNgay(x.Key, x.Value.Tong, x.Value.DaGhi, x.Value.Trung, x.Value.Thieu))
                .ToList();

            return new KetQuaKiemTra(dong, danhSachThieu, luotKhongCoNhanSu);
        }
    }
}
