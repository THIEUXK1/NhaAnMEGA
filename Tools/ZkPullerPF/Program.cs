using ZkPullerPF;

// Tool kéo log chấm công của máy ZK1 (10.0.198.12) về bảng CHECKINOUT.
// Chạy một lượt rồi thoát: đặt lịch Task Scheduler gọi lại theo chu kì, giống các sync service bên 10.0.60.52.
// Tham số tuỳ chọn:
//   --ngay <so ngay quet lui>  chạy bù cho nhiều ngày trước
//   --chi-doc                  chỉ đọc máy và in ra, không ghi gì xuống DB
//
// Lệnh xoá hẳn một người dùng khỏi bộ nhớ máy (không kéo log):
//   --xoa-user <badge> --may <so may, cach nhau dau phay> [--that-su]
//   Thiếu --that-su thì chỉ kiểm tra xem máy nào còn badge đó, không xoá gì.

var thuMucGoc = AppContext.BaseDirectory;
var cauHinh = CauHinh.Doc(Path.Combine(thuMucGoc, "appsettings.json"));

var thuMucLog = Path.IsPathRooted(cauHinh.ThuMucLog)
    ? cauHinh.ThuMucLog
    : Path.Combine(thuMucGoc, cauHinh.ThuMucLog);
var log = new GhiLog(thuMucLog);

var soNgay = cauHinh.SoNgayQuetLui;
var chiDoc = args.Contains("--chi-doc");
var badgeCanXoa = string.Empty;
var danhSachSoMay = new List<int>();
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--ngay" or "-n" && int.TryParse(args[i + 1], out var soNgayThamSo) && soNgayThamSo > 0)
    {
        soNgay = soNgayThamSo;
    }

    if (args[i] == "--xoa-user")
    {
        badgeCanXoa = args[i + 1].Trim();
    }

    if (args[i] == "--may")
    {
        danhSachSoMay = args[i + 1]
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => int.TryParse(x, out _))
            .Select(int.Parse)
            .Distinct()
            .ToList();
    }
}

// Chỉ cho một tiến trình nối máy tại một thời điểm: máy chấm công chỉ chịu được vài phiên đồng thời.
// Dùng Semaphore chứ không dùng Mutex: Mutex gắn với thread đang giữ, mà sau await thì
// thread có thể đổi nên ReleaseMutex sẽ ném "unsynchronized block of code".
using var khoaChay = new Semaphore(1, 1, @"Global\ZkPullerPF_109");
if (!khoaChay.WaitOne(TimeSpan.FromSeconds(30)))
{
    log.Ghi("Một lượt kéo log khác đang chạy, bỏ qua lượt này.");
    return 0;
}

try
{
    // Lệnh liệt kê user trên máy: chỉ đọc, xuất CSV để đối chiếu với USERINFO
    if (args.Contains("--liet-ke-user"))
    {
        return await LenhLietKeUser.ChayAsync(
            cauHinh.ChuoiKetNoi, danhSachSoMay, Path.Combine(thuMucLog, "danh-sach-user"), log);
    }

    // Lệnh xoá user chạy độc lập, không kéo log
    if (!string.IsNullOrWhiteSpace(badgeCanXoa))
    {
        return await LenhXoaUser.ChayAsync(
            cauHinh.ChuoiKetNoi, badgeCanXoa, danhSachSoMay, args.Contains("--that-su"), log);
    }

    var tuThoiDiem = DateTime.Today.AddDays(-(soNgay - 1));
    log.Ghi($"Bắt đầu kéo log máy {cauHinh.MayChamCong.Ip} (SENSORID {cauHinh.MayChamCong.SensorId}), quét lùi {soNgay} ngày.");

    var docMay = new DocMayChamCong(cauHinh.MayChamCong, log);
    var danhSach = docMay.DocLog(tuThoiDiem);

    if (danhSach.Count == 0)
    {
        log.Ghi("Không có lượt quẹt nào trong khoảng cần xét.");
        return 0;
    }

    if (chiDoc)
    {
        foreach (var luot in danhSach.OrderBy(x => x.ThoiDiem))
        {
            log.Ghi($"[chỉ đọc] {luot.ThoiDiem:yyyy-MM-dd HH:mm:ss}  badge {luot.MaNhanVien}");
        }

        log.Ghi($"Chế độ chỉ đọc: {danhSach.Count} lượt, không ghi xuống DB.");
        return 0;
    }

    var ghi = new GhiCheckinout(cauHinh.ChuoiKetNoi, cauHinh.MayChamCong, log);
    var ketQua = await ghi.GhiAsync(danhSach);

    log.Ghi($"Xong: chèn mới {ketQua.DaChen}, đã có sẵn {ketQua.DaCoSan}, không thấy nhân sự {ketQua.KhongThayNhanSu}.");
    return 0;
}
catch (Exception ex)
{
    log.GhiLoi(ex.Message);
    return 1;
}
finally
{
    khoaChay.Release();
}
