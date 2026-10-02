using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NhaAnMEGA.Areas.NhaAnMEGA.Service;
using NhaAnMEGA.Context;
using NhaAnMEGA.Models.Zktime;
using System.Data;
using System.Net.NetworkInformation;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Controllers
{
    [Area("NhaAnMEGA")]
    public class NhaAnPFController : Controller
    {

        #region khai bao
        // Thư mục lưu ảnh quét nhà ăn PF (cấu hình AnhQuet:ThuMuc, mặc định App_Data/AnhQuet trên máy chủ)
        private static string RootPathAnhQuet => global::NhaAnMEGA.Utils.ThuMucAnhQuet.GiaTri;

        // Máy chấm công ghi nhận nhân viên quên thẻ
        private const string SensorQuenThe = "109";
        private const int SoDongMoiTrangQuenThe = 50;
        private const int SoDongMoiTrangBaoCao = 0;      // 0 = xem tất cả trong 1 trang
        private const int SoDongToiDaBaoCao = 5000;      // trần an toàn khi xem tất cả
        private const int SoDongToiDaXuatExcel = 50000;  // trần an toàn khi xuất Excel chi tiết

        public DBZktimePFContext _context;
        public DBThieuITContext _DbThieuXk;
        public NhaAnPFController()
        {
            _context = new DBZktimePFContext();
            _DbThieuXk = new DBThieuITContext();
        }
        #endregion

        #region dang nhap 1
        //nhà ăn best

        // Thêm Route cụ thể cho trang Login
        [HttpGet("/MG/Login")]
        public IActionResult Login()
        {
            return View();
        }

        // Thêm Route cụ thể cho xử lý Login (POST)
        [HttpPost("/MG/Login2")]
        public IActionResult Login2(UserLoginPF user)
        {
            var authenticatedUser = _DbThieuXk.NhanViens.FirstOrDefault(u => u.TenDangNhap == user.Username && u.MatKhau == user.Password && u.TrangThai == true);

            if (authenticatedUser != null)
            {
                string serializedUser = JsonConvert.SerializeObject(authenticatedUser);

                HttpContext.Session.SetString("NhaAnPF", serializedUser);
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());

                int chucVuValue = authenticatedUser.ChucVu ?? 0;
                HttpContext.Session.SetInt32("UserChucVu", chucVuValue);

                // Ghi nhớ trên máy này để lần sau vào thẳng, không phải đăng nhập lại
                GhiNhoDangNhap.Luu(HttpContext, authenticatedUser.TenDangNhap ?? user.Username);

                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnMEGA" });
            }
            else
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View("Login", user);
            }
        }

        public class UserLoginPF
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
        // Đăng xuất: xoá session và bỏ luôn ghi nhớ trên máy này
        [HttpGet("/MG/Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            GhiNhoDangNhap.Xoa(HttpContext);
            return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnMEGA" });
        }

        private bool IsSessionExpired()
        {
            // Máy đã ghi nhớ đăng nhập thì không áp hạn 3 tiếng nữa
            if (GhiNhoDangNhap.CoGhiNho(HttpContext)) return false;

            var loginTimeString = HttpContext.Session.GetString("LoginTime");
            if (string.IsNullOrEmpty(loginTimeString))
            {
                return true;
            }

            var loginTime = DateTime.Parse(loginTimeString);
            var currentTime = DateTime.Now;

            return (currentTime - loginTime).TotalHours > 3;
        }

        // Giữ nguyên Route bạn đã có hoặc tùy chỉnh lại nếu muốn đồng bộ
        [HttpGet("/MG/GetSessionRemainingTime")]
        public JsonResult GetSessionRemainingTime()
        {
            // Máy đã ghi nhớ đăng nhập vẫn đếm ngược để trang tự F5 làm mới phiên;
            // mốc đếm chưa có (session vừa dựng lại từ cookie) thì lấy ngay lúc này.
            if (GhiNhoDangNhap.CoGhiNho(HttpContext)
                && string.IsNullOrEmpty(HttpContext.Session.GetString("LoginTime")))
            {
                HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());
            }

            var loginTimeStr = HttpContext.Session.GetString("LoginTime");

            if (string.IsNullOrEmpty(loginTimeStr))
            {
                return Json(new { success = false, message = "Chưa đăng nhập hoặc phiên đã hết.", hours = 0, minutes = 0, seconds = 0 });
            }

            if (!DateTime.TryParse(loginTimeStr, out DateTime loginTime))
            {
                return Json(new { success = false, message = "Dữ liệu thời gian đăng nhập không hợp lệ.", hours = 0, minutes = 0, seconds = 0 });
            }

            DateTime expireTime = loginTime.AddHours(3);
            TimeSpan remaining = expireTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                return Json(new { success = false, message = "Phiên đăng nhập đã hết hạn.", hours = 0, minutes = 0, seconds = 0 });
            }

            return Json(new
            {
                success = true,
                hours = remaining.Hours,
                minutes = remaining.Minutes,
                seconds = remaining.Seconds
            });
        }

        #endregion

        #region Nhà ăn PF

        #region 1. Điều hướng và Quản lý Trang chính (Index)
        [HttpGet("/MG/An")]
        public IActionResult Index()
        {
            // Kiểm tra nếu Session hết hạn
            if (IsSessionExpired())
            {
                HttpContext.Session.Clear(); // Xóa Session
                return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnMEGA" });
            }
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnMEGA" });
            }

            // Mỗi lần mở lại / F5 trang thì hạn 3 tiếng đếm lại từ đầu.
            HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString());

            return View();
        }
        #endregion

        #region 2. Xử lý Ghi nhận Quẹt thẻ chính (ProcessBarcode)
        [HttpPost("/MG/Nhanbarcode")]
        public async Task<IActionResult> ProcessBarcode(string barcode, int idgate, IFormFile? imageFile)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return Json(new { message = "Vui lòng nhập hoặc quét mã vạch!" });

            barcode = barcode.Trim('*').Trim(); // Loại bỏ dấu * nếu có và chuẩn hóa khoảng trắng

            // Quét trúng mỗi dấu * thì sau khi chuẩn hoá còn chuỗi rỗng, phải kiểm tra lại
            if (string.IsNullOrWhiteSpace(barcode))
                return Json(new { message = "Mã vạch không hợp lệ, vui lòng quét lại!" });

            if (idgate <= 0)
            {
                return Json(new { message = "Vui lòng kiểm tra lại mã vạch và cổng quét!" });
            }

            // TỐI ƯU HÓA: Khởi tạo thời gian đồng bộ và loại bỏ miligiây
            var dt = DateTime.Now;
            var now = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

            try
            {
                // Lấy thông tin cổng quét
                var selectedGate = await _context.GatePfs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.Id == idgate);

                if (selectedGate == null)
                {
                    return Json(new { message = "Cổng không hợp lệ" });
                }

                // --- CHUẨN BỊ ĐƯỜNG DẪN ẢNH TRÊN FILE SERVER, CHỈ GHI FILE KHI LƯỢT QUÉT HỢP LỆ ---
                string? savedFilePath = null;
                byte[]? imageBytes = null;
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Đặt tên file định dạng chuẩn của bạn: barcode_ddMMyyyy_HHmmss.jpg
                    string fileName = $"{barcode}_{now:ddMMyyyy}_{now:HHmmss}{Path.GetExtension(imageFile.FileName)}";
                    savedFilePath = Path.Combine(RootPathAnhQuet, fileName);

                    // TỐI ƯU CAO ĐỘ: Trích xuất mảng byte dữ liệu ảnh ngay trên RAM để giải phóng Stream yêu cầu từ Client cực nhanh
                    using (var memoryStream = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(memoryStream);
                        imageBytes = memoryStream.ToArray();
                    }
                }

                // Toàn bộ nghiệp vụ ghi nhận suất ăn nằm ở QuetTheService, dùng chung với đồng bộ máy chấm ZK1
                var service = new QuetTheService(_context);
                var ketQua = await service.GhiNhanAsync(barcode, selectedGate.GName ?? string.Empty, now, savedFilePath);

                switch (ketQua.Loai)
                {
                    case LoaiKetQuaQuet.TrungLap:
                        // ĐỒNG BỘ SUẤT 1: Trả về success và tên đầy đủ để UI nhận diện hiển thị tên nhưng không cộng dồn
                        return Json(new { success = true, workerName = ketQua.WorkerName });

                    case LoaiKetQuaQuet.ThanhCong:
                        if (imageBytes != null && savedFilePath != null)
                        {
                            LuuAnhNgam(savedFilePath, imageBytes);
                        }

                        // ĐỒNG BỘ SUẤT 1: Trả về đầy đủ cấu trúc JSON bao gồm gateName, dailyScans, bua, time
                        return Json(new
                        {
                            success = true,
                            workerName = ketQua.WorkerName,
                            gateName = ketQua.GateName,
                            dailyScans = ketQua.DailyScans,
                            bua = ketQua.Bua,
                            time = now.ToString("HH:mm:ss")
                        });

                    default:
                        return Json(new { message = ketQua.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { message = "Lỗi xử lý server: " + ex.Message });
            }
        }
        #endregion

        #region 3. Hệ thống Hàm bổ trợ nghiệp vụ (Helper Methods)

        // Fire-and-Forget: Đẩy tiến trình IO ghi file vật lý lên ổ mạng ra luồng ngầm, không bắt luồng chính phải đợi mạng IO chậm chạp
        private static void LuuAnhNgam(string savedFilePath, byte[] imageBytes)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (!Directory.Exists(RootPathAnhQuet))
                    {
                        Directory.CreateDirectory(RootPathAnhQuet);
                    }

                    using (var fileStream = new FileStream(savedFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
                    {
                        await fileStream.WriteAsync(imageBytes, 0, imageBytes.Length);
                    }
                }
                catch (Exception ex)
                {
                    // Tiến trình ngầm tự giải thoát an toàn nếu File Server mất kết nối mà không ảnh hưởng luồng quẹt thẻ chính.
                    System.Diagnostics.Trace.WriteLine($"Lỗi lưu ảnh ngầm PF: {ex.Message}");
                }
            });
        }
        #endregion

        #region 3b. Lượt quẹt máy ZK1 vừa đồng bộ (QuetZk1Moi)
        // Trả về các lượt quẹt máy ZK1 vừa được đồng bộ để màn hình quét hiển thị như một lượt quét mã vạch
        [HttpGet("/MG/QuetZk1Moi")]
        public async Task<IActionResult> QuetZk1Moi(int tuId = 0)
        {
            try
            {
                var service = new QuetTheService(_context);
                var (lastId, items) = await service.LayQuetZk1MoiAsync(tuId);
                return Json(new { ok = true, lastId, items });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Lỗi lấy dữ liệu máy ZK1: " + ex.Message });
            }
        }
        #endregion

        #region 3c. Đồng bộ bù lượt quẹt máy ZK1 theo khoảng thời gian (DongBoZk1Bu)
        private const int SoNgayBuToiDa = 7;   // Chặn quét bù khoảng quá dài làm nặng DB

        // Ghi nhận bù các lượt quẹt máy ZK1 mà phần mềm ZKTime đẩy về muộn, đã lọt khỏi vòng chạy ngầm
        [HttpPost("/MG/DongBoZk1Bu")]
        public async Task<IActionResult> DongBoZk1Bu(DateTime tu, DateTime den)
        {
            if (den <= tu)
            {
                return Json(new { ok = false, message = "Mốc kết thúc phải sau mốc bắt đầu!" });
            }

            if ((den - tu).TotalDays > SoNgayBuToiDa)
            {
                return Json(new { ok = false, message = $"Chỉ đồng bộ bù tối đa {SoNgayBuToiDa} ngày mỗi lần!" });
            }

            try
            {
                var ketQua = await DongBoZk1Service.DongBoKhoangAsync(_context, tu, den);

                return Json(new
                {
                    ok = true,
                    data = ketQua,
                    message = $"Đọc {ketQua.TongLuot} lượt quẹt: ghi mới {ketQua.ThanhCong}, đã có sẵn {ketQua.DaCoSan}, " +
                              $"bỏ qua do trùng {ketQua.BoQuaTrung}, ngoài giờ ăn {ketQua.NgoaiGioAn}, lỗi nghiệp vụ {ketQua.LoiNghiepVu}."
                });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, message = "Lỗi đồng bộ bù máy ZK1: " + ex.Message });
            }
        }
        #endregion

        #region 4. Quản lý danh sách Cổng (Gate)
        [HttpGet("/MG/Gate")]
        public async Task<IActionResult> GetGates()
        {
            var gates = await _context.GatePfs.AsNoTracking()
                .Select(g => new
                {
                    g.Id,       // ID cổng
                    g.GName     // Tên cổng
                })
                .ToListAsync();

            return Ok(gates); // Trả về danh sách cổng dạng JSON
        }
        #endregion

        #region 5. Kiểm tra và Ghi nhận Thống kê (TotalMeals)
        [HttpGet("/MG/TotalMeals")]
        public async Task<JsonResult> TotalMeals()
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");

            if (authenticatedUser == null)
            {
                return Json(new { error = "Not authenticated" });
            }
            else
            {
                int totalMeals = await GetTotalMealsFromDatabaseAsync();
                return Json(totalMeals);
            }
        }

        private async Task<int> GetTotalMealsFromDatabaseAsync()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            int todayRecordCount = await _context.TimeRecodePfs.AsNoTracking()
                .CountAsync(tr => tr.Date >= today && tr.Date < tomorrow);

            return todayRecordCount;
        }

        private int GetTotalMealsFromDatabase()
        {
            return GetTotalMealsFromDatabaseAsync().GetAwaiter().GetResult();
        }
        #endregion

        #region 6. Truy vấn Số liệu và Báo cáo (Báo cáo Ngày / Thống kê cổng quét)
        // Phương thức lấy dữ liệu báo cáo theo ngày
        [HttpGet("/MG/XuatEX")]
        public async Task<JsonResult> GetReportData(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                startDate = startDate?.Date ?? DateTime.Today;
                endDate = endDate?.Date.AddDays(1).AddMilliseconds(-1) ?? DateTime.Today.AddDays(1).AddMilliseconds(-1);

                Console.WriteLine($"Start Date: {startDate}, End Date: {endDate}");

                var reportData = await _context.TimeRecodePfs.AsNoTracking()
                    .Where(x => x.Mealtime >= startDate && x.Mealtime <= endDate)
                    .Select(x => new
                    {
                        x.WorkerId,
                        x.WorkerName,
                        x.CardId,
                        x.CardIc,
                        x.DepId,
                        x.Mealtime,
                        x.Date,
                        x.Id,
                        x.DateTime,
                        x.So,
                        x.GName,
                        x.Bua
                    })
                    .ToListAsync();

                return Json(new { data = reportData });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // Trang báo cáo suất ăn hằng ngày
        [HttpGet("/MG/An/BaoCaoNgay")]
        public IActionResult BaoCaoNgay()
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return RedirectToAction("Login", "NhaAnPF");
            }

            return View();
        }

        // Chuỗi "Sáng,Trưa" từ bộ lọc dạng tích -> danh sách giá trị để dựng câu IN
        private static List<string> TachDanhSachLoc(string? giaTri)
        {
            if (string.IsNullOrWhiteSpace(giaTri))
            {
                return new List<string>();
            }

            return giaTri
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();
        }

        // Lọc lượt quét theo khoảng ngày + từ khoá (chưa lọc bữa/cổng)
        private IQueryable<TimeRecodePf> LocBaoCaoTheoNgay(DateTime start, DateTime end, string? tuKhoa)
        {
            var query = _context.TimeRecodePfs.AsNoTracking()
                .Where(x => x.Mealtime >= start && x.Mealtime <= end);

            if (!string.IsNullOrWhiteSpace(tuKhoa))
            {
                query = query.Where(x =>
                    (x.WorkerId != null && x.WorkerId.Contains(tuKhoa)) ||
                    (x.WorkerName != null && x.WorkerName.Contains(tuKhoa)) ||
                    (x.CardId != null && x.CardId.Contains(tuKhoa)) ||
                    (x.DepId != null && x.DepId.Contains(tuKhoa)));
            }

            return query;
        }

        // Lọc tiếp theo bộ lọc dạng tích bữa / cổng quét
        private static IQueryable<TimeRecodePf> LocBuaCong(IQueryable<TimeRecodePf> query,
                                                           List<string> dsBuaChon, List<string> dsCongChon)
        {
            if (dsBuaChon.Count > 0)
            {
                query = query.Where(x => x.Bua != null && dsBuaChon.Contains(x.Bua));
            }

            if (dsCongChon.Count > 0)
            {
                query = query.Where(x => x.GName != null && dsCongChon.Contains(x.GName));
            }

            return query;
        }

        // Dữ liệu cho trang báo cáo hằng ngày (JSON, phân trang, không reload trang)
        [HttpGet("/MG/An/BaoCaoNgay/DuLieu")]
        public async Task<IActionResult> DuLieuBaoCaoNgay(DateTime? fromDate, DateTime? toDate, string? bua, string? cong, string? tuKhoa,
                                                          int page = 1, int pageSize = SoDongMoiTrangBaoCao)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            var start = (fromDate ?? DateTime.Today).Date;
            var end = (toDate ?? start).Date.AddDays(1).AddTicks(-1);

            // Bộ lọc dạng tích: client gửi nhiều giá trị ngăn bằng dấu phẩy
            var dsBuaChon = TachDanhSachLoc(bua);
            var dsCongChon = TachDanhSachLoc(cong);

            var queryGoc = LocBaoCaoTheoNgay(start, end, tuKhoa);

            // Danh sách tuỳ chọn lấy trước khi lọc bữa/cổng, để tích rồi vẫn còn đủ mục để bỏ tích
            var dsBua = await queryGoc.Where(x => x.Bua != null && x.Bua != "")
                .Select(x => x.Bua!).Distinct().OrderBy(x => x).ToListAsync();
            var dsCong = await queryGoc.Where(x => x.GName != null && x.GName != "")
                .Select(x => x.GName!).Distinct().OrderBy(x => x).ToListAsync();

            var query = LocBuaCong(queryGoc, dsBuaChon, dsCongChon);

            var totalItems = await query.CountAsync();

            // Thống kê số suất theo từng bữa trong khoảng ngày đang xem
            var tongTheoBua = await query
                .GroupBy(x => x.Bua)
                .Select(g => new { bua = g.Key ?? "Khác", soLuong = g.Count() })
                .OrderBy(x => x.bua)
                .ToListAsync();

            // pageSize <= 0: xem tất cả (vẫn chặn trần SoDongToiDaBaoCao để không kéo cả bảng)
            var xemTatCa = pageSize <= 0;
            var soDong = xemTatCa ? SoDongToiDaBaoCao : Math.Min(pageSize, SoDongToiDaBaoCao);
            var trang = xemTatCa ? 1 : Math.Max(1, page);

            var items = await query
                .OrderByDescending(x => x.Mealtime)
                .Skip((trang - 1) * soDong)
                .Take(soDong)
                .Select(x => new
                {
                    x.Id,
                    workerId = x.WorkerId,
                    workerName = x.WorkerName,
                    depId = x.DepId,
                    gName = x.GName,
                    bua = x.Bua,
                    mealtime = x.Mealtime,
                    laMaTest = x.WorkerId != null && XoaLuotQuet.DS_MA_TEST.Contains(x.WorkerId)
                })
                .ToListAsync();

            return Json(new
            {
                ok = true,
                data = new
                {
                    items,
                    totalItems,
                    page = trang,
                    pageSize = xemTatCa ? 0 : soDong,
                    gioiHanTatCa = SoDongToiDaBaoCao,
                    tongTheoBua,
                    dsBua,
                    dsCong,
                    duocXoa = XoaLuotQuet.DuocXoa(HttpContext)
                },
                message = (string?)null
            });
        }

        // Xoá 1 lượt quét (chỉ tài khoản được phép, kiểm quyền phía server)
        [HttpPost("/MG/An/BaoCaoNgay/Xoa/{id:int}")]
        public async Task<IActionResult> XoaLuotQuetBaoCao(int id,
            [FromServices] ILogger<NhaAnPFController> logger)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            if (!XoaLuotQuet.DuocXoa(HttpContext))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { ok = false, data = (object?)null, message = "Bạn không có quyền xoá lượt quét" });
            }

            var daXoa = await XoaLuotQuet.XoaAsync(_context, id, XoaLuotQuet.TenDangNhapHienTai(HttpContext)!, logger);
            return Json(new
            {
                ok = daXoa,
                data = (object?)null,
                message = daXoa ? "Đã xoá lượt quét" : "Không tìm thấy lượt quét của mã test"
            });
        }

        // Xoá nhanh mọi lượt quét mã test trong khoảng ngày đang chọn
        [HttpPost("/MG/An/BaoCaoNgay/XoaTest")]
        public async Task<IActionResult> XoaNhanhTestBaoCao(DateTime? fromDate, DateTime? toDate,
            [FromServices] ILogger<NhaAnPFController> logger)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            if (!XoaLuotQuet.DuocXoa(HttpContext))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new { ok = false, data = (object?)null, message = "Bạn không có quyền xoá lượt quét" });
            }

            var start = (fromDate ?? DateTime.Today).Date;
            var end = (toDate ?? start).Date.AddDays(1).AddTicks(-1);

            var soDong = await XoaLuotQuet.XoaTatCaTestAsync(_context, start, end,
                XoaLuotQuet.TenDangNhapHienTai(HttpContext)!, logger);
            return Json(new
            {
                ok = true,
                data = new { soDong },
                message = soDong > 0 ? $"Đã xoá {soDong} lượt quét mã test" : "Không có lượt quét mã test nào để xoá"
            });
        }

        // Tổng hợp suất ăn theo từng ngày trong khoảng (JSON, dùng cho chế độ "Gộp theo ngày")
        [HttpGet("/MG/An/BaoCaoNgay/GopNgay")]
        public async Task<IActionResult> GopNgayBaoCao(DateTime? fromDate, DateTime? toDate, string? bua, string? cong, string? tuKhoa)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            var start = (fromDate ?? DateTime.Today).Date;
            var end = (toDate ?? start).Date.AddDays(1).AddTicks(-1);

            var dsBuaChon = TachDanhSachLoc(bua);
            var dsCongChon = TachDanhSachLoc(cong);

            var query = LocBuaCong(LocBaoCaoTheoNgay(start, end, tuKhoa), dsBuaChon, dsCongChon);

            // Gom ở DB theo (ngày, bữa) rồi mới dựng ma trận ngày x bữa ở bộ nhớ
            var gom = await query
                .GroupBy(x => new { Ngay = x.Mealtime!.Value.Date, x.Bua })
                .Select(g => new { g.Key.Ngay, Bua = g.Key.Bua ?? "Khác", SoLuong = g.Count() })
                .ToListAsync();

            var cotBua = gom.Select(x => x.Bua).Distinct().OrderBy(x => x).ToList();

            var theoNgay = gom
                .GroupBy(x => x.Ngay)
                .OrderByDescending(g => g.Key)
                .Select(g => new
                {
                    ngay = g.Key.ToString("yyyy-MM-dd"),
                    tong = g.Where(x => x.Bua != QuetTheService.BuaNgoaiGio).Sum(x => x.SoLuong),
                    theoBua = cotBua.ToDictionary(b => b, b => g.Where(x => x.Bua == b).Sum(x => x.SoLuong))
                })
                .ToList();

            return Json(new
            {
                ok = true,
                data = new
                {
                    cotBua,
                    theoNgay,
                    tongChung = theoNgay.Sum(x => x.tong),
                    soNgay = theoNgay.Count
                },
                message = (string?)null
            });
        }

        // Xuất Excel đúng bộ lọc đang xem: gop = true là bảng tổng hợp theo ngày, ngược lại là bảng chi tiết
        [HttpGet("/MG/An/BaoCaoNgay/Excel")]
        public async Task<IActionResult> XuatExcelBaoCaoNgay(DateTime? fromDate, DateTime? toDate,
                                                             string? bua, string? cong, string? tuKhoa, bool gop = false)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            var tuNgay = (fromDate ?? DateTime.Today).Date;
            var denNgay = (toDate ?? tuNgay).Date;
            var query = LocBuaCong(LocBaoCaoTheoNgay(tuNgay, denNgay.AddDays(1).AddTicks(-1), tuKhoa),
                                   TachDanhSachLoc(bua), TachDanhSachLoc(cong));

            var noiDung = gop
                ? await DungExcelTongHop(query, tuNgay, denNgay)
                : await DungExcelChiTiet(query, tuNgay, denNgay);

            var tenFile = $"BaoCaoSuatAn_{(gop ? "TongHop" : "ChiTiet")}_{tuNgay:yyyyMMdd}_{denNgay:yyyyMMdd}.xlsx";
            return File(noiDung, BaoCaoNgayExcelService.KieuNoiDungExcel, tenFile);
        }

        private static async Task<byte[]> DungExcelChiTiet(IQueryable<TimeRecodePf> query, DateTime tuNgay, DateTime denNgay)
        {
            var dsDong = await query
                .OrderByDescending(x => x.Mealtime)
                .Take(SoDongToiDaXuatExcel)
                .Select(x => new BaoCaoNgayExcelService.DongChiTiet(
                    x.WorkerId, x.WorkerName, x.Id, x.DepId, x.Bua, x.GName, x.Mealtime))
                .ToListAsync();

            return BaoCaoNgayExcelService.XuatChiTiet(dsDong, tuNgay, denNgay);
        }

        private static async Task<byte[]> DungExcelTongHop(IQueryable<TimeRecodePf> query, DateTime tuNgay, DateTime denNgay)
        {
            var gom = await query
                .GroupBy(x => new { Ngay = x.Mealtime!.Value.Date, x.Bua })
                .Select(g => new { g.Key.Ngay, Bua = g.Key.Bua ?? "Khác", SoLuong = g.Count() })
                .ToListAsync();

            var cotBua = gom.Select(x => x.Bua).Distinct().OrderBy(x => x).ToList();

            var theoNgay = gom
                .GroupBy(x => x.Ngay)
                .OrderByDescending(g => g.Key)
                .Select(g => new BaoCaoNgayExcelService.DongTongHop(
                    g.Key,
                    cotBua.ToDictionary(b => b, b => g.Where(x => x.Bua == b).Sum(x => x.SoLuong)),
                    g.Where(x => x.Bua != QuetTheService.BuaNgoaiGio).Sum(x => x.SoLuong)))
                .ToList();

            return BaoCaoNgayExcelService.XuatTongHop(cotBua, theoNgay, tuNgay, denNgay);
        }

        [HttpGet("/MG/GetMealCountByGate")]
        public async Task<IActionResult> GetMealCountByGate(int idgate)
        {
            try
            {
                var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
                if (authenticatedUser == null)
                {
                    return RedirectToAction("Login", "NhaAnPF");
                }

                var selectedGate = await _context.GatePfs.AsNoTracking().FirstOrDefaultAsync(g => g.Id == idgate);
                if (selectedGate == null)
                {
                    return Json(new { count = 0, message = "Cổng không hợp lệ" });
                }

                var today = DateTime.Today;
                var now = DateTime.Now;

                TimeSpan startTime = TimeSpan.Zero;
                TimeSpan endTime = TimeSpan.Zero;
                int mealCount = 0;

                // ĐÃ SỬA: Chuyển đổi tr.DateTime.Value.TimeOfDay thành so sánh trực tiếp qua tr.DateTime kết hợp TimeOfDay
                // EF Core sẽ tự động biên dịch cú pháp này xuống SQL Server an toàn mà không sợ bản ghi bị NULL gây sập ứng dụng.
                if (now.Hour >= 6 && now.Hour < 9) // Bữa sáng
                {
                    startTime = new TimeSpan(6, 0, 0);
                    endTime = new TimeSpan(9, 0, 0);
                    mealCount = await _context.TimeRecodePfs.AsNoTracking().CountAsync(tr =>
                        tr.GName == selectedGate.GName &&
                        tr.Date == today.Date &&
                        tr.DateTime != null && tr.DateTime.Value.TimeOfDay >= startTime &&
                        tr.DateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 11 && now.Hour < 14) // Bữa trưa
                {
                    startTime = new TimeSpan(11, 0, 0);
                    endTime = new TimeSpan(14, 0, 0);
                    mealCount = await _context.TimeRecodePfs.AsNoTracking().CountAsync(tr =>
                        tr.GName == selectedGate.GName &&
                        tr.Date == today.Date &&
                        tr.DateTime != null && tr.DateTime.Value.TimeOfDay >= startTime &&
                        tr.DateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 14 && now.Hour < 20) // Bữa tối
                {
                    startTime = new TimeSpan(14, 0, 0);
                    endTime = new TimeSpan(20, 0, 0);
                    mealCount = await _context.TimeRecodePfs.AsNoTracking().CountAsync(tr =>
                        tr.GName == selectedGate.GName &&
                        tr.Date == today.Date &&
                        tr.DateTime != null && tr.DateTime.Value.TimeOfDay >= startTime &&
                        tr.DateTime.Value.TimeOfDay <= endTime);
                }
                else if (now.Hour >= 23 || now.Hour < 2) // Bữa đêm (23:00 - 01:30)
                {
                    startTime = new TimeSpan(23, 0, 0);
                    endTime = new TimeSpan(1, 30, 0);

                    var previousDay = today.AddDays(-1);

                    mealCount = await _context.TimeRecodePfs.AsNoTracking().CountAsync(tr =>
                        tr.GName == selectedGate.GName &&
                        tr.DateTime != null &&
                        ((tr.Date == previousDay.Date && tr.DateTime.Value.TimeOfDay >= startTime) ||
                         (tr.Date == today.Date && tr.DateTime.Value.TimeOfDay <= endTime)));
                }
                else
                {
                    return Json(new { count = 0, message = "Không phải giờ ăn" });
                }

                return Json(new { count = mealCount, mealTime = $"{startTime} - {endTime}" });
            }
            catch (Exception ex)
            {
                return Json(new { count = 0, message = "Đã có lỗi xảy ra", error = ex.Message });
            }
        }
        #endregion

        #region 7. Thêm mới Tài khoản và Quản lý nhân sự ngoại vi (addnv)
        [HttpPost("/MG/addnv")]
        public async Task<IActionResult> Add([FromBody] Employee data)
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");

            if (authenticatedUser == null)
            {
                return RedirectToAction("Login", "NhaAnPF");
            }
            string employeeID = data.employeeID;
            string employeeName = data.employeeName;

            if (string.IsNullOrWhiteSpace(employeeID) || string.IsNullOrWhiteSpace(employeeName))
            {
                return Json(new { message = "Mã nhân viên và tên nhân viên không được để trống" });
            }

            try
            {
                // Kiểm tra trùng lặp
                var existingEmployee = await _context.Userinfos
                    .FirstOrDefaultAsync(e => e.Ssn == employeeID);

                if (existingEmployee != null)
                {
                    return Json(new { message = "Mã nhân viên đã tồn tại trong hệ thống." });
                }

                // Thêm mới nhân viên
                var newEmployee = new Userinfo()
                {
                    Ssn = employeeID,
                    Name = employeeName
                };

                _context.Userinfos.Add(newEmployee);
                await _context.SaveChangesAsync();

                return Json(new { message = "Thêm nhân viên thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { message = $"Lỗi: {ex.Message}" });
            }
        }

        public class Employee
        {
            public string employeeID { get; set; } = string.Empty;
            public string employeeName { get; set; } = string.Empty;
        }
        #endregion

        #region 8. Giám sát thiết bị chấm vân tay ngoại vi (PingStatus)
        [HttpGet("/MG/PingStatus")]
        public async Task<IActionResult> PingStatus()
        {
            string ip = "10.0.198.12";
            try
            {
                using (var ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(ip, 1000);
                    if (reply.Status == IPStatus.Success)
                    {
                        var now = DateTime.Now;
                        var today = DateTime.Today;
                        TimeSpan startTime = TimeSpan.Zero;
                        TimeSpan endTime = TimeSpan.Zero;

                        if (now.Hour >= 6 && now.Hour < 9) // Bữa sáng
                        {
                            startTime = new TimeSpan(6, 0, 0);
                            endTime = new TimeSpan(9, 0, 0);
                        }
                        else if (now.Hour >= 11 && now.Hour < 14) // Bữa trưa
                        {
                            startTime = new TimeSpan(11, 0, 0);
                            endTime = new TimeSpan(14, 0, 0);
                        }
                        else if (now.Hour >= 14 && now.Hour < 20) // Bữa tối
                        {
                            startTime = new TimeSpan(14, 0, 0);
                            endTime = new TimeSpan(20, 0, 0);
                        }
                        else if (now.Hour >= 23 || now.Hour < 2) // Bữa đêm (23:00 - 01:30)
                        {
                            startTime = new TimeSpan(23, 0, 0);
                            endTime = new TimeSpan(1, 30, 0);
                        }
                        else
                        {
                            return Json(new { online = true, users = new List<object>() });
                        }

                        var previousDay = today.AddDays(-1);

                        var data = (startTime < endTime)
                            ? await _context.Checkinouts.AsNoTracking()
                                .Where(c => c.Sensorid == "109" && c.Checktime >= today.Add(startTime) && c.Checktime <= today.Add(endTime))
                                .Join(
                                    _context.Userinfos.AsNoTracking().Where(u => u.Defaultdeptid != null),
                                    check => check.Userid,
                                    user => user.Userid,
                                    (check, user) => new { check, user }
                                )
                                .Join(
                                    _context.Departments.AsNoTracking(),
                                    cu => cu.user.Defaultdeptid!.Value,
                                    dept => dept.Deptid,
                                    (cu, dept) => new
                                    {
                                        Ma = cu.user.Ssn,
                                        Ten = cu.user.Name,
                                        BoPhan = dept.Deptname,
                                        ThoiGian = cu.check.Checktime
                                    }
                                )
                                .OrderByDescending(x => x.ThoiGian)
                                .ToListAsync()
                            : await _context.Checkinouts.AsNoTracking()
                                .Where(c => c.Sensorid == "109" &&
                                    ((c.Checktime.Date == previousDay.Date && c.Checktime.TimeOfDay >= startTime) ||
                                     (c.Checktime.Date == today.Date && c.Checktime.TimeOfDay <= endTime)))
                                .Join(
                                    _context.Userinfos.AsNoTracking().Where(u => u.Defaultdeptid != null),
                                    check => check.Userid,
                                    user => user.Userid,
                                    (check, user) => new { check, user }
                                )
                                .Join(
                                    _context.Departments.AsNoTracking(),
                                    cu => cu.user.Defaultdeptid!.Value,
                                    dept => dept.Deptid,
                                    (cu, dept) => new
                                    {
                                        Ma = cu.user.Ssn,
                                        Ten = cu.user.Name,
                                        BoPhan = dept.Deptname,
                                        ThoiGian = cu.check.Checktime
                                    }
                                )
                                .OrderByDescending(x => x.ThoiGian)
                                .ToListAsync();

                        return Json(new { online = true, users = data });
                    }
                }
            }
            catch { }
            return Json(new { online = false, users = new List<object>() });
        }
        #endregion

        #endregion

        #region Blacklist Management (Unique Actions)

        // 1. Hiển thị danh sách - Tên hàm: GetBlacklistIndex
        [HttpGet("/MG/BlackList")]
        public IActionResult GetBlacklistIndex()
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null) return RedirectToAction("Login", "NhaAnPF");

            var blacklist = _context.BlacklistPfs.ToList();
            return View("Index1", blacklist); // Chỉ định rõ View Index1
        }

        // 2. Thêm hoặc Cập nhật - Tên hàm: PostSaveBlacklist
        [HttpPost("/MG/BlackList/Save")]
        public IActionResult PostSaveBlacklist(BlacklistPf model)
        {
            if (model.Id == 0)
            {
                _context.BlacklistPfs.Add(model);
            }
            else
            {
                _context.BlacklistPfs.Update(model);
            }
            _context.SaveChanges();
            return RedirectToAction(nameof(GetBlacklistIndex));
        }

        // 3. Xóa bản ghi - Tên hàm: PostRemoveBlacklist
        [HttpPost("/MG/BlackList/Remove/{id}")]
        public IActionResult PostRemoveBlacklist(int id)
        {
            var item = _context.BlacklistPfs.Find(id);
            if (item != null)
            {
                _context.BlacklistPfs.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(GetBlacklistIndex));
        }

        // 4. Tải File Excel Mẫu - Tên hàm: GetBlacklistTemplate
        [HttpGet("/MG/BlackList/DownloadTemplate")]
        public IActionResult GetBlacklistTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("BlacklistTemplate");
                worksheet.Cell(1, 1).Value = "WorkerId";
                worksheet.Cell(1, 2).Value = "WorkerName";

                var header = worksheet.Range("A1:B1");
                header.Style.Font.Bold = true;
                header.Style.Fill.BackgroundColor = XLColor.LightGray;
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Import_Blacklist.xlsx");
                }
            }
        }

        // 5. Thêm dữ liệu bằng file Excel - Tên hàm: PostImportBlacklistExcel
        [HttpPost("/MG/BlackList/Import")]
        public async Task<IActionResult> PostImportBlacklistExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(GetBlacklistIndex));

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);

                    // ĐÃ SỬA: Lấy phạm vi vùng dữ liệu trước để kiểm tra null an toàn
                    var usedRange = worksheet.RangeUsed();
                    if (usedRange != null)
                    {
                        // Chỉ đọc các dòng khi file thực sự có dữ liệu
                        var rows = usedRange.RowsUsed().Skip(1);

                        foreach (var row in rows)
                        {
                            var wId = row.Cell(1).GetValue<string>()?.Trim();
                            var wName = row.Cell(2).GetValue<string>()?.Trim();

                            if (!string.IsNullOrEmpty(wId) && !_context.BlacklistPfs.Any(x => x.WorkerId == wId))
                            {
                                _context.BlacklistPfs.Add(new BlacklistPf { WorkerId = wId, WorkerName = wName });
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            return RedirectToAction(nameof(GetBlacklistIndex));
        }

        #endregion

        #region Check ảnh

        [HttpGet("/MG/An/CheckAnh")]
        public IActionResult IndexAnh()
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");

            if (authenticatedUser == null)
            {
                // Đối tượng không được lưu trong Session, chuyển hướng đến trang đăng nhập
                return RedirectToAction("Login", "NhaAnPF");
            }
            else
            {
                return View();
            }
        }

        [HttpGet("/MG/An/CheckAnhData")]
        public IActionResult IndexAnhdata(int id)
        {
            var record = _context.TimeRecodePfs.FirstOrDefault(x => x.Id == id);
            if (record == null)
            {
                return NotFound(new { message = "Không tìm thấy dữ liệu!" });
            }

            string? base64Image = null;

            // Ưu tiên lấy từ ImagePath (Ổ chung)
            if (!string.IsNullOrEmpty(record.ImagePath) && System.IO.File.Exists(record.ImagePath))
            {
                try
                {
                    byte[] fileBytes = System.IO.File.ReadAllBytes(record.ImagePath);
                    base64Image = Convert.ToBase64String(fileBytes);
                }
                catch (Exception ex)
                {
                    // Đã sửa: Thực thi ghi nhận lỗi ra Trace hệ thống để xóa sạch cảnh báo khối catch trống
                    // base64Image sẽ giữ là null nếu lỗi phân quyền hoặc file bị xóa để nhảy tiếp xuống logic tiếp theo
                    System.Diagnostics.Trace.WriteLine($"Lỗi truy cập hoặc đọc file ảnh PF: {ex.Message}");
                }
            }

            // Nếu ImagePath trống hoặc lỗi, thử lấy từ ImageData (dữ liệu cũ trong DB) làm phương án dự phòng
            if (string.IsNullOrEmpty(base64Image) && record.ImageData != null)
            {
                base64Image = Convert.ToBase64String(record.ImageData);
            }

            return Json(new
            {
                id = record.Id,
                workerName = record.WorkerName,
                cardId = record.CardId,
                bua = record.Bua,
                date = record.DateTime,
                imageBase64 = base64Image // Trả về ảnh đã đọc từ ổ chung dưới dạng Base64
            });
        }
        #endregion

        #region khách ăn PF

        [HttpGet("/MG/KhachAn")]
        public async Task<IActionResult> KhachAn(string searchString, int page = 1, int pageSize = 20)
        {
            // Kiểm tra nếu Session hết hạn
            if (IsSessionExpired())
            {
                HttpContext.Session.Clear(); // Xóa Session
                return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnMEGA" });
            }

            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnMEGA" });
            }

            // Khởi tạo query cho bảng KhachAnPfs
            var query = _context.KhachAnPfs.AsQueryable();

            // Kiểm tra nếu có chuỗi tìm kiếm
            if (!string.IsNullOrEmpty(searchString))
            {
                // ĐÃ SỬA: Thêm điều kiện x.Truong != null trước khi gọi Contains để triệt tiêu lỗi CS8602
                query = query.Where(x =>
                    (x.MaThe != null && x.MaThe.Contains(searchString)) ||
                    (x.TenThe != null && x.TenThe.Contains(searchString)) ||
                    (x.NhaThau != null && x.NhaThau.Contains(searchString)));
            }

            // Tính tổng số bản ghi sau khi áp dụng tìm kiếm
            var totalItems = await query.CountAsync();

            // Lấy danh sách các mục theo trang hiện tại và kích thước trang
            var items = await query
                .OrderBy(x => x.Id) // Sắp xếp theo Id hoặc thuộc tính khác nếu cần
                .Skip((page - 1) * pageSize) // Bỏ qua các mục trước đó
                .Take(pageSize) // Lấy các mục trong phạm vi trang hiện tại
                .ToListAsync();

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.SearchString = searchString;

            return View(items);
        }


        // Trả danh sách khách ăn dạng JSON cho UI (tìm kiếm / phân trang không reload trang)
        [HttpGet("/MG/khakh/list")]
        public async Task<IActionResult> DanhSachKhachAn(string? searchString, int page = 1, int pageSize = 20)
        {
            if (IsSessionExpired() || HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            var query = _context.KhachAnPfs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(x =>
                    (x.MaThe != null && x.MaThe.Contains(searchString)) ||
                    (x.TenThe != null && x.TenThe.Contains(searchString)) ||
                    (x.NhaThau != null && x.NhaThau.Contains(searchString)));
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new { x.Id, x.MaThe, x.TenThe, x.NhaThau })
                .ToListAsync();

            return Json(new
            {
                ok = true,
                data = new { items, totalItems, page, pageSize },
                message = (string?)null
            });
        }

        [HttpPost("/MG/khakh/add")]
        public async Task<IActionResult> Add([FromBody] KhachAnPf newKhachAn)
        {
            // Kiểm tra nếu Session hết hạn
            if (IsSessionExpired())
            {
                HttpContext.Session.Clear(); // Xóa Session
                return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnMEGA" });

            }

            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnMEGA" });
            }
            if (ModelState.IsValid)
            {
                try
                {
                    var existingKhachAn = await _context.KhachAnPfs
                                                        .FirstOrDefaultAsync(x => x.MaThe == newKhachAn.MaThe);

                    if (existingKhachAn != null)
                    {
                        return BadRequest("Mã thẻ đã tồn tại");
                    }

                    _context.KhachAnPfs.Add(newKhachAn);
                    await _context.SaveChangesAsync();
                    return Ok("Thêm thành công");
                }
                catch (Exception)
                {
                    return StatusCode(500, "Lỗi hệ thống");
                }
            }

            return BadRequest("Dữ liệu không hợp lệ");
        }


        [HttpPost("/MG/khakh/delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Kiểm tra nếu Session hết hạn
            if (IsSessionExpired())
            {
                HttpContext.Session.Clear(); // Xóa Session
                return RedirectToAction("Login", "NhaAnPF", new { area = "NhaAnMEGA" });

            }

            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");
            if (authenticatedUser == null)
            {
                return RedirectToAction("Index", "NhaAnPF", new { area = "NhaAnMEGA" });
            }
            var khachAn = await _context.KhachAnPfs.FindAsync(id);

            if (khachAn == null)
            {
                TempData["ErrorMessage"] = "Khách ăn không tồn tại!";
                return RedirectToAction("KhachAn");
            }

            _context.KhachAnPfs.Remove(khachAn);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Khách ăn đã được xóa thành công!";

            return RedirectToAction("KhachAn");
        }
        [HttpPost("/MG/khakh/update")]
        public IActionResult Update([FromBody] KhachAnModel model)
        {
            var item = _context.KhachAnPfs.FirstOrDefault(x => x.Id == model.Id);
            if (item == null) return NotFound();

            item.TenThe = model.TenThe;
            item.NhaThau = model.NhaThau;
            _context.SaveChanges();

            return Ok();
        }
        public class KhachAnModel
        {
            public int Id { get; set; }
            public string TenThe { get; set; } = string.Empty;
            public string NhaThau { get; set; } = string.Empty;
        }


        #endregion

        #region Check Quên thẻ


        [HttpGet("/MG/An/QuenThe")]
        public IActionResult QuenThe()
        {
            var authenticatedUser = HttpContext.Session.GetString("NhaAnPF");

            if (authenticatedUser == null)
            {
                return RedirectToAction("Login", "NhaAnPF");
            }
            else
            {

                return View();
            }
        }


        public class CheckinoutViewModel
        {
            public int Userid { get; set; }
            public DateTime Checktime { get; set; }
            public string? Checktype { get; set; }
            public int? Verifycode { get; set; }
            public string? Sensorid { get; set; }
            public string? Memoinfo { get; set; }
            public string? WorkCode { get; set; }
            public string? Sn { get; set; }
            public short? UserExtFmt { get; set; }

            // Từ bảng USERINFO
            public string? Name { get; set; }
            public string? Ssn { get; set; }
            public string? BoPhan { get; set; } // Thêm dòng này
        }
        // Xem trước báo cáo quên thẻ ngay trên UI (mặc định là hôm nay)
        [HttpGet("/MG/An/QuenThe/DuLieu")]
        public async Task<IActionResult> DuLieuQuenThe(DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = SoDongMoiTrangQuenThe)
        {
            if (HttpContext.Session.GetString("NhaAnPF") == null)
            {
                return Json(new { ok = false, data = (object?)null, message = "Phiên đăng nhập đã hết hạn" });
            }

            var start = (fromDate ?? DateTime.Today).Date;
            var end = (toDate ?? start).Date.AddDays(1).AddTicks(-1);

            var query = from check in _context.Checkinouts.AsNoTracking()
                        join user in _context.Userinfos.AsNoTracking() on check.Userid equals user.Userid
                        where check.Sensorid == SensorQuenThe
                              && check.Checktime >= start
                              && check.Checktime <= end
                        orderby check.Checktime descending
                        select new
                        {
                            userId = check.Userid,
                            name = user.Name,
                            ssn = user.Ssn,
                            boPhan = user.BoPhan,
                            checkTime = check.Checktime,
                            checkType = check.Checktype
                        };

            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(new
            {
                ok = true,
                data = new { items, totalItems, page, pageSize, fromDate = start, toDate = end },
                message = (string?)null
            });
        }

        [HttpPost("/MG/An/ExportExcel")]
        public IActionResult ExportExcel(DateTime fromDate, DateTime toDate)
        {
            // Nếu cùng ngày, mở rộng khoảng từ 00:00 đến 23:59:59 của ngày đó
            DateTime start = fromDate.Date;
            DateTime end = (fromDate == toDate) ? fromDate.Date.AddDays(1).AddTicks(-1) : toDate.Date.AddDays(1).AddTicks(-1);

            var data = (from check in _context.Checkinouts
                        join user in _context.Userinfos on check.Userid equals user.Userid
                        where check.Sensorid == SensorQuenThe
                              && check.Checktime >= start
                              && check.Checktime <= end
                        orderby check.Checktime descending
                        select new
                        {
                            UserID = check.Userid,
                            Name = user.Name,
                            SSN = user.Ssn,
                            BoPhan = user.BoPhan,
                            CheckTime = check.Checktime,
                            CheckType = check.Checktype,
                            SensorID = check.Sensorid
                        }).ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("CheckinOut");
                worksheet.Cell(1, 1).InsertTable(data);
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    string fileName = $"Checkin_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                    return File(stream.ToArray(),
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                fileName);
                }
            }
        }



        #endregion

    }
}
