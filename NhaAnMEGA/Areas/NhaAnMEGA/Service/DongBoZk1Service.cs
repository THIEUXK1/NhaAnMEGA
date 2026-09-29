using Microsoft.EntityFrameworkCore;
using NhaAnMEGA.Context;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    // Số liệu một lượt đồng bộ, dùng cho log chạy ngầm và cho màn hình đồng bộ bù
    public class KetQuaDongBoZk1
    {
        public int TongLuot { get; set; }        // Số lượt quẹt máy đọc được trong khoảng
        public int ThanhCong { get; set; }       // Số suất ăn vừa ghi mới
        public int DaCoSan { get; set; }         // Đã có bản ghi đúng mã thẻ + đúng giây quẹt
        public int BoQuaTrung { get; set; }      // Bị luật chống trùng 15 giây / 2 giờ chặn
        public int NgoaiGioAn { get; set; }      // Quẹt ngoài khung giờ bữa và bị chặn (chỉ khi batBuocTrongGioAn)
        public int LoiNghiepVu { get; set; }     // Danh sách đen hoặc không tìm thấy nhân sự
    }

    /// <summary>
    /// Đọc dữ liệu quẹt thẻ của máy chấm công ZK1 (bảng CHECKINOUT, SENSORID = 109) và ghi ngay sang
    /// Time_RecodePF giống hệt luồng Nhập Mã Vạch. Chạy ngầm theo chu kì rất ngắn để suất ăn lên gần như tức thời.
    ///
    /// Mỗi vòng quét lại TOÀN BỘ khoảng vài giờ gần nhất thay vì chỉ lấy từ một mốc thời gian trong RAM:
    /// phần mềm ZKTime có thể đẩy log về muộn với CHECKTIME lùi về quá khứ, nếu bám theo mốc thì các bản ghi
    /// đó không bao giờ được ghi nhận. Chống trùng dựa vào chính Time_RecodePF (mã thẻ + đúng giây quẹt)
    /// nên khởi động lại ứng dụng cũng không mất dữ liệu.
    /// </summary>
    public class DongBoZk1Service : BackgroundService
    {
        public const string SensorIdZk1 = "109";                  // Mã cảm biến của máy ZK1 10.0.198.12
        public const string GhiChuNguonZk1 = "Máy chấm công";     // Ghi vào cột So để phân biệt với quét mã vạch tại cổng

        private const int GiayLapLai = 2;                         // Chu kì quét dữ liệu mới (giây)
        private const int GiayNghiKhiLoi = 30;                    // Nghỉ khi mất kết nối DB, tránh loop vô hạn
        private const int GioQuetLui = 6;                         // Mỗi vòng quét lại bấy nhiêu giờ gần nhất, đủ phủ khung bữa dài nhất

        private readonly ILogger<DongBoZk1Service> _logger;

        public DongBoZk1Service(ILogger<DongBoZk1Service> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DongBoZk1Service đã khởi động, đồng bộ quẹt thẻ máy ZK1 mỗi {Giay} giây.", GiayLapLai);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await DongBoGanDayAsync(stoppingToken);
                    await Task.Delay(TimeSpan.FromSeconds(GiayLapLai), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Ứng dụng đang tắt, thoát vòng lặp an toàn
                }
                catch (Exception ex)
                {
                    _logger.LogError("Lỗi vòng đồng bộ ZK1: {Message}", ex.Message);
                    await Task.Delay(TimeSpan.FromSeconds(GiayNghiKhiLoi), stoppingToken);
                }
            }
        }

        // Quét lại cả khoảng lùi gần đây, kể cả ngoài giờ bữa (những lượt đó ghi nhận với Bua = "0")
        private async Task DongBoGanDayAsync(CancellationToken stoppingToken)
        {
            var den = DateTime.Now;
            var tu = den.AddHours(-GioQuetLui);

            using var context = new DBZktimePFContext();
            var ketQua = await DongBoKhoangAsync(context, tu, den, _logger, stoppingToken);

            if (ketQua.ThanhCong > 0)
            {
                _logger.LogInformation("ZK1 đồng bộ: ghi mới {Moi} lượt trên tổng {Tong} lượt quẹt trong {Gio} giờ gần đây.",
                    ketQua.ThanhCong, ketQua.TongLuot, GioQuetLui);
            }
        }

        /// <summary>
        /// Ghi nhận mọi lượt quẹt máy ZK1 trong khoảng thời gian chỉ định, bỏ qua lượt đã có bản ghi.
        /// Dùng chung cho vòng chạy ngầm và cho lệnh đồng bộ bù thủ công.
        /// </summary>
        public static async Task<KetQuaDongBoZk1> DongBoKhoangAsync(
            DBZktimePFContext context,
            DateTime tu,
            DateTime den,
            ILogger? logger = null,
            CancellationToken stoppingToken = default)
        {
            var service = new QuetTheService(context);
            var ketQua = new KetQuaDongBoZk1();

            // Lấy lượt quẹt của máy ZK1 trong khoảng, kèm mã thẻ (Ssn) để ghi nhận suất ăn
            var danhSach = await context.Checkinouts
                .AsNoTracking()
                .Where(c => c.Sensorid == SensorIdZk1 && c.Checktime >= tu && c.Checktime <= den)
                .Join(
                    context.Userinfos.AsNoTracking().Where(u => u.Ssn != null && u.Ssn.Trim() != ""),
                    check => check.Userid,
                    user => user.Userid,
                    (check, user) => new { user.Ssn, check.Checktime, check.Userid }
                )
                .OrderBy(x => x.Checktime)
                .ToListAsync(stoppingToken);

            ketQua.TongLuot = danhSach.Count;

            if (danhSach.Count == 0)
            {
                return ketQua;
            }

            // Khoá tự nhiên chống trùng: mã thẻ + đúng giây quẹt, đọc thẳng từ Time_RecodePF nên restart app vẫn đúng
            var danhSachMa = danhSach.Select(x => x.Ssn!.Trim()).Distinct().ToList();
            var banGhiDaCo = await context.TimeRecodePfs
                .AsNoTracking()
                .Where(t => t.WorkerId != null && danhSachMa.Contains(t.WorkerId)
                            && t.DateTime >= tu && t.DateTime <= den)
                .Select(t => new { t.WorkerId, t.DateTime })
                .ToListAsync(stoppingToken);

            var khoaDaCo = new HashSet<string>(banGhiDaCo
                .Where(x => x.DateTime.HasValue)
                .Select(x => TaoKhoa(x.WorkerId!, x.DateTime!.Value)));

            var tenCong = await service.LayTenCongZk1Async(stoppingToken);

            foreach (var item in danhSach)
            {
                var maThe = item.Ssn!.Trim();
                var khoa = TaoKhoa(maThe, item.Checktime);

                if (!khoaDaCo.Add(khoa))
                {
                    ketQua.DaCoSan++;
                    continue;
                }

                var ghiNhan = await service.GhiNhanAsync(
                    maThe,
                    tenCong,
                    item.Checktime,
                    imagePath: null,
                    batBuocTrongGioAn: false,
                    ghiChuNguon: GhiChuNguonZk1);

                switch (ghiNhan.Loai)
                {
                    case LoaiKetQuaQuet.ThanhCong:
                        ketQua.ThanhCong++;
                        logger?.LogInformation("ZK1 đồng bộ suất ăn: {Ten} ({Ma}) - {Bua} lúc {Gio:HH:mm:ss}",
                            ghiNhan.WorkerName, maThe, ghiNhan.Bua, ghiNhan.ThoiDiem);
                        break;
                    case LoaiKetQuaQuet.TrungLap:
                    case LoaiKetQuaQuet.DaQuetTruocDo:
                        ketQua.BoQuaTrung++;
                        break;
                    case LoaiKetQuaQuet.NgoaiGioAn:
                        ketQua.NgoaiGioAn++;
                        break;
                    default:
                        ketQua.LoiNghiepVu++;
                        break;
                }
            }

            return ketQua;
        }

        private static string TaoKhoa(string maThe, DateTime thoiDiem)
            => $"{maThe.Trim()}|{thoiDiem:yyyyMMddHHmmss}";
    }
}
