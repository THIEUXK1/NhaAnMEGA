using Microsoft.EntityFrameworkCore;
using NhaAnMEGA.Context;
using NhaAnMEGA.Models.Zktime;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    // Phân loại kết quả của một lượt ghi nhận quẹt thẻ, dùng chung cho quét mã vạch và đồng bộ máy chấm ZK1
    public enum LoaiKetQuaQuet
    {
        LoiNghiepVu,      // Danh sách đen / không tìm thấy nhân sự / cổng sai
        TrungLap,         // Quẹt lại trong vòng 15 giây, bỏ qua không cộng suất
        DaQuetTruocDo,    // Đã có suất trong vòng 2 giờ
        NgoaiGioAn,       // Thời điểm quẹt không thuộc bữa nào
        ThanhCong
    }

    public class KetQuaQuet
    {
        public LoaiKetQuaQuet Loai { get; set; }
        public string Message { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public string Bua { get; set; } = string.Empty;
        public int DailyScans { get; set; }
        public DateTime ThoiDiem { get; set; }
    }

    // Nghiệp vụ ghi nhận suất ăn: kiểm tra danh sách đen, tra nhân sự/khách, chống trùng và lưu bản ghi
    public class QuetTheService
    {
        public const int IdCongZk1 = 1;               // Lượt quẹt vân tay máy ZK1 được ghi nhận về cổng số 1
        public const string TenCongZk1MacDinh = "Máy chấm công ZK1"; // Dùng khi GatePF chưa khai báo cổng số 1

        public const string BuaNgoaiGio = "0";        // Lượt quẹt ngoài mọi khung giờ bữa: vẫn ghi nhận, Bua = "0"

        private const int GiaySpamToiThieu = 15;      // Quẹt lại dưới mốc này coi như trùng, không cộng suất
        public const int GioGiuaHaiSuat = 2;          // Mỗi người chỉ được một suất trong 2 giờ (tool đối soát dùng chung mốc này)
        private const int SoBanGhiToiDaMoiLuot = 20;  // Giới hạn số lượt ZK1 trả về UI mỗi vòng hỏi

        private readonly DBZktimePFContext _context;

        public QuetTheService(DBZktimePFContext context)
        {
            _context = context;
        }

        private static readonly TimeSpan SangBatDau = new(6, 0, 0);
        private static readonly TimeSpan SangKetThuc = new(9, 0, 0);
        private static readonly TimeSpan TruaBatDau = new(11, 0, 0);
        private static readonly TimeSpan TruaKetThuc = new(14, 0, 0);
        private static readonly TimeSpan ToiBatDau = new(14, 0, 0);
        private static readonly TimeSpan ToiKetThuc = new(20, 0, 0);
        private static readonly TimeSpan DemBatDau = new(23, 0, 0);
        private static readonly TimeSpan DemKetThuc = new(1, 30, 0);

        // Xác định bữa ăn theo mốc giờ quẹt thẻ
        public static string XacDinhBuaAn(DateTime thoiDiem)
        {
            var currentTime = thoiDiem.TimeOfDay;
            return currentTime switch
            {
                // Bữa sáng: 06:00 - 09:00
                var t when t >= new TimeSpan(6, 0, 0) && t <= new TimeSpan(9, 0, 0) => "Bữa sáng",
                // Bữa trưa: 11:00 - 14:00
                var t when t >= new TimeSpan(11, 0, 0) && t <= new TimeSpan(14, 0, 0) => "Bữa trưa",
                // Bữa tối: 14:00 - 20:00
                var t when t >= new TimeSpan(14, 0, 0) && t <= new TimeSpan(20, 0, 0) => "Bữa tối",
                // Bữa đêm: 23:00 - 01:30 qua ngày hôm sau
                var t when t >= new TimeSpan(23, 0, 0) || t <= new TimeSpan(1, 30, 0) => "Bữa đêm",
                _ => BuaNgoaiGio
            };
        }

        // Khung giờ của bữa ăn chứa thời điểm truyền vào; null nghĩa là ngoài mọi khung giờ bữa
        public static (DateTime BatDau, DateTime KetThuc, string Bua)? LayKhungGioBua(DateTime thoiDiem)
        {
            var ngay = thoiDiem.Date;
            var gio = thoiDiem.TimeOfDay;

            if (gio >= SangBatDau && gio <= SangKetThuc)
                return (ngay + SangBatDau, ngay + SangKetThuc, "Bữa sáng");

            if (gio >= TruaBatDau && gio <= TruaKetThuc)
                return (ngay + TruaBatDau, ngay + TruaKetThuc, "Bữa trưa");

            if (gio >= ToiBatDau && gio <= ToiKetThuc)
                return (ngay + ToiBatDau, ngay + ToiKetThuc, "Bữa tối");

            // Bữa đêm vắt qua nửa đêm nên mốc bắt đầu có thể rơi vào ngày hôm trước
            if (gio >= DemBatDau)
                return (ngay + DemBatDau, ngay.AddDays(1) + DemKetThuc, "Bữa đêm");

            if (gio <= DemKetThuc)
                return (ngay.AddDays(-1) + DemBatDau, ngay + DemKetThuc, "Bữa đêm");

            return null;
        }

        // Lấy tên cổng số 1 trong GatePF, không có thì dùng tên mặc định
        public async Task<string> LayTenCongZk1Async(CancellationToken stoppingToken = default)
        {
            var gate = await _context.GatePfs
                .AsNoTracking()
                .Where(g => g.Id == IdCongZk1)
                .Select(g => g.GName)
                .FirstOrDefaultAsync(stoppingToken);

            return string.IsNullOrWhiteSpace(gate) ? TenCongZk1MacDinh : gate!;
        }

        /// <summary>
        /// Lấy các lượt quẹt máy ZK1 đã ghi nhận trong ngày có Id lớn hơn mốc UI đang giữ,
        /// để màn hình quét hiển thị y như một lượt quét mã vạch.
        /// </summary>
        /// <param name="tuId">Mốc Id UI đã hiển thị; bằng 0 nghĩa là UI vừa mở, chỉ lấy mốc hiện tại</param>
        public async Task<(int LastId, List<QuetZk1Dto> Items)> LayQuetZk1MoiAsync(int tuId)
        {
            var tenCong = await LayTenCongZk1Async();
            var homNay = DateTime.Today;

            var truyVan = _context.TimeRecodePfs
                .AsNoTracking()
                // Chỉ lấy lượt do máy chấm công ghi, bỏ lượt quét mã vạch cùng cổng (đã tự hiển thị ở luồng quét)
                .Where(t => t.GName == tenCong && t.Date == homNay && t.So == DongBoZk1Service.GhiChuNguonZk1);

            // Lần hỏi đầu tiên chỉ trả về mốc Id để không dội lại toàn bộ lượt quẹt cũ trong ngày lên màn hình
            if (tuId <= 0)
            {
                var mocHienTai = await truyVan.MaxAsync(t => (int?)t.Id) ?? 0;
                return (mocHienTai, new List<QuetZk1Dto>());
            }

            var banGhiMoi = await truyVan
                .Where(t => t.Id > tuId)
                .OrderBy(t => t.Id)
                .Take(SoBanGhiToiDaMoiLuot)
                .Select(t => new QuetZk1Dto
                {
                    Id = t.Id,
                    WorkerId = t.WorkerId ?? string.Empty,
                    WorkerName = t.WorkerName ?? string.Empty,
                    GateName = t.GName ?? string.Empty,
                    Bua = t.Bua ?? string.Empty,
                    Time = t.DateTime.HasValue ? t.DateTime.Value.ToString("HH:mm:ss") : string.Empty
                })
                .ToListAsync();

            if (banGhiMoi.Count == 0)
            {
                return (tuId, banGhiMoi);
            }

            // Đếm số lượt ăn trong ngày của đúng những người vừa quẹt để hiển thị giống ô "Số lượt quét trong ngày"
            var danhSachMa = banGhiMoi.Select(b => b.WorkerId).Distinct().ToList();
            var thongKe = await _context.TimeRecodePfs
                .AsNoTracking()
                .Where(t => t.Date == homNay && t.WorkerId != null && danhSachMa.Contains(t.WorkerId))
                .GroupBy(t => t.WorkerId!)
                .Select(g => new { WorkerId = g.Key, SoLuot = g.Count() })
                .ToListAsync();

            foreach (var item in banGhiMoi)
            {
                item.DailyScans = thongKe.FirstOrDefault(t => t.WorkerId == item.WorkerId)?.SoLuot ?? 1;
            }

            return (banGhiMoi.Max(b => b.Id), banGhiMoi);
        }

        // Tra cứu nhân sự trong Userinfos, không thấy thì tìm tiếp trong danh sách khách ăn
        public async Task<ValidWorkerDto?> TimNhanSuAsync(string barcode)
        {
            var validWorker = await _context.Userinfos
                .AsNoTracking()
                .Where(c => c.Ssn == barcode)
                .Select(c => new ValidWorkerDto
                {
                    WorkerId = c.Ssn ?? string.Empty,
                    WorkerName = c.Name ?? string.Empty,
                    DepName = c.BoPhan ?? _context.Departments.AsNoTracking()
                                                .Where(d => d.Deptid == c.Defaultdeptid)
                                                .Select(d => d.TenTiengViet)
                                                .FirstOrDefault() ?? "N/A"
                })
                .FirstOrDefaultAsync();

            if (validWorker == null)
            {
                validWorker = await _context.KhachAnPfs
                    .AsNoTracking()
                    .Where(c => c.MaThe == barcode)
                    .Select(c => new ValidWorkerDto
                    {
                        WorkerId = c.MaThe ?? string.Empty,
                        WorkerName = c.TenThe ?? string.Empty,
                        DepName = c.NhaThau ?? string.Empty
                    })
                    .FirstOrDefaultAsync();
            }

            return validWorker;
        }

        /// <summary>
        /// Ghi nhận một lượt quẹt thẻ vào Time_RecodePF.
        /// </summary>
        /// <param name="barcode">Mã thẻ nhân viên hoặc khách</param>
        /// <param name="gateName">Tên cổng quét ghi vào bản ghi</param>
        /// <param name="thoiDiem">Mốc giờ quẹt thật (máy chấm ZK1 dùng giờ Checktime, không phải giờ server)</param>
        /// <param name="imagePath">Đường dẫn ảnh đã lưu trên file server, có thể null</param>
        /// <param name="batBuocTrongGioAn">true: bỏ qua bản ghi nằm ngoài khung giờ bữa ăn (dùng cho đồng bộ ZK1)</param>
        public async Task<KetQuaQuet> GhiNhanAsync(string barcode, string gateName, DateTime thoiDiem, string? imagePath = null, bool batBuocTrongGioAn = false, string? ghiChuNguon = null)
        {
            var now = new DateTime(thoiDiem.Year, thoiDiem.Month, thoiDiem.Day, thoiDiem.Hour, thoiDiem.Minute, thoiDiem.Second);

            // Mã rỗng phải chặn tại đây: vài hồ sơ trong USERINFO có Ssn là chuỗi rỗng,
            // để lọt xuống TimNhanSuAsync là khớp nhầm và cấp suất ăn cho người khác
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return new KetQuaQuet { Loai = LoaiKetQuaQuet.LoiNghiepVu, Message = "Mã vạch rỗng, không ghi nhận", ThoiDiem = now };
            }

            var bua = XacDinhBuaAn(now);

            if (batBuocTrongGioAn && bua == BuaNgoaiGio)
            {
                return new KetQuaQuet { Loai = LoaiKetQuaQuet.NgoaiGioAn, ThoiDiem = now };
            }

            bool isBlacklisted = await _context.BlacklistPfs
                .AsNoTracking()
                .AnyAsync(b => b.WorkerId == barcode);

            if (isBlacklisted)
            {
                return new KetQuaQuet { Loai = LoaiKetQuaQuet.LoiNghiepVu, Message = "Mã vạch này nằm trong danh sách đen", ThoiDiem = now };
            }

            var validWorker = await TimNhanSuAsync(barcode);

            if (validWorker == null)
            {
                return new KetQuaQuet { Loai = LoaiKetQuaQuet.LoiNghiepVu, Message = "Mã vạch này không có trong danh sách nhân viên hoặc khách", ThoiDiem = now };
            }

            // Kiểm tra quét lặp (Anti-Spam chống dồn hàng đợi dữ liệu)
            var twoHoursAgo = now.AddHours(-GioGiuaHaiSuat);
            var latestRecord = await _context.TimeRecodePfs
                .AsNoTracking()
                .Where(tr => tr.WorkerId == validWorker.WorkerId && tr.DateTime >= twoHoursAgo && tr.DateTime <= now)
                .OrderByDescending(tr => tr.DateTime)
                .FirstOrDefaultAsync();

            if (latestRecord != null && (now - latestRecord.DateTime.GetValueOrDefault()).TotalSeconds < GiaySpamToiThieu)
            {
                return new KetQuaQuet
                {
                    Loai = LoaiKetQuaQuet.TrungLap,
                    WorkerName = validWorker.WorkerName,
                    ThoiDiem = now
                };
            }

            // Luật 1 suất / 2 giờ chỉ tính trên suất ăn thật; lượt ngoài giờ ăn (Bữa "0") không chặn suất kế tiếp
            if (bua != BuaNgoaiGio)
            {
                var suatGanNhat = await _context.TimeRecodePfs
                    .AsNoTracking()
                    .Where(tr => tr.WorkerId == validWorker.WorkerId && tr.Bua != BuaNgoaiGio
                                 && tr.DateTime >= twoHoursAgo && tr.DateTime <= now)
                    .OrderByDescending(tr => tr.DateTime)
                    .FirstOrDefaultAsync();

                if (suatGanNhat != null && (now - suatGanNhat.DateTime.GetValueOrDefault()).TotalHours < GioGiuaHaiSuat)
                {
                    return new KetQuaQuet
                    {
                        Loai = LoaiKetQuaQuet.DaQuetTruocDo,
                        WorkerName = validWorker.WorkerName,
                        Message = $"Cảnh báo: Nhân viên đã quét thẻ lúc {suatGanNhat.DateTime:HH:mm}!",
                        ThoiDiem = now
                    };
                }
            }

            var newRecord = new TimeRecodePf
            {
                WorkerId = validWorker.WorkerId,
                WorkerName = validWorker.WorkerName,
                DepId = validWorker.DepName,
                Mealtime = now,
                Date = now.Date,
                DateTime = now,
                GName = gateName,
                So = ghiChuNguon,           // Chú thích nguồn quẹt; rỗng nghĩa là quét mã vạch tại cổng
                Bua = bua,
                ImageData = null, // Đảm bảo tuyệt đối không lưu nhị phân nặng vào DB
                ImagePath = imagePath
            };

            int existingScans = await _context.TimeRecodePfs
                .AsNoTracking()
                .CountAsync(tr => tr.WorkerId == validWorker.WorkerId && tr.Date == now.Date);

            _context.TimeRecodePfs.Add(newRecord);
            await _context.SaveChangesAsync();

            _context.Entry(newRecord).State = EntityState.Detached;

            return new KetQuaQuet
            {
                Loai = LoaiKetQuaQuet.ThanhCong,
                WorkerName = newRecord.WorkerName ?? string.Empty,
                GateName = newRecord.GName ?? string.Empty,
                Bua = newRecord.Bua ?? string.Empty,
                DailyScans = existingScans + 1,
                ThoiDiem = now
            };
        }
    }

    // Một lượt quẹt máy ZK1 đã ghi nhận, đẩy về UI màn hình quét
    public class QuetZk1Dto
    {
        public int Id { get; set; }
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string GateName { get; set; } = string.Empty;
        public string Bua { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public int DailyScans { get; set; }
    }

    public class ValidWorkerDto
    {
        public string WorkerId { get; set; } = string.Empty;
        public string WorkerName { get; set; } = string.Empty;
        public string DepName { get; set; } = string.Empty;
    }
}
