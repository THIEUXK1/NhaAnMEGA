using DongBoSuatAnPF;
using NhaAnMEGA.Areas.NhaAnMEGA.Service;
using NhaAnMEGA.Context;

// Tool đối soát và đồng bộ bù suất ăn máy chấm công ZK1 (CHECKINOUT SENSORID 109 -> Time_RecodePF).
// Chạy độc lập với web app, dùng lại đúng nghiệp vụ của DongBoZk1Service nên không lệch luật chống trùng.
//
//   --kiem-tra                    (mặc định) chỉ đối soát, không ghi gì
//   --dong-bo                     ghi bù các lượt còn thiếu xuống Time_RecodePF
//   --ngay <n>                    xét n ngày gần nhất tính cả hôm nay (mặc định 7)
//   --tu <yyyy-MM-dd> --den <...> xét đúng khoảng chỉ định
//   --chi-tiet                    in từng lượt còn thiếu
//
// Mã thoát: 0 = khớp hết, 2 = còn lượt chưa đồng bộ (dùng cho cảnh báo lịch chạy), 1 = lỗi.

const int MaThoatConThieu = 2;
const int MaThoatLoi = 1;
const int SoLuotInToiDa = 50;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var thamSo = ThamSo.Doc(args);
if (thamSo.Loi is not null)
{
    Console.Error.WriteLine(thamSo.Loi);
    return MaThoatLoi;
}

try
{
    using var context = new DBZktimePFContext();

    Console.WriteLine($"Khoảng xét: {thamSo.Tu:yyyy-MM-dd HH:mm:ss} → {thamSo.Den:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"Chế độ: {(thamSo.ChayDongBo ? "ĐỒNG BỘ BÙ (có ghi Time_RecodePF)" : "chỉ kiểm tra")}");
    Console.WriteLine();

    var truoc = await KiemTraDongBo.ChayAsync(context, thamSo.Tu, thamSo.Den);
    InBangDoiSoat(truoc);

    if (!thamSo.ChayDongBo)
    {
        if (thamSo.HienChiTiet)
        {
            InChiTietThieu(truoc);
        }

        if (truoc.TongThieuThatSu > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"Còn {truoc.TongThieuThatSu} lượt chưa đồng bộ. Chạy lại kèm --dong-bo để ghi bù.");
            return MaThoatConThieu;
        }

        Console.WriteLine();
        Console.WriteLine("Đã đồng bộ đủ, không có lượt nào bị bỏ sót.");
        return 0;
    }

    if (truoc.TongThieuThatSu == 0)
    {
        Console.WriteLine();
        Console.WriteLine("Không có lượt nào cần ghi bù.");
        return 0;
    }

    if (thamSo.HienChiTiet)
    {
        InChiTietThieu(truoc);
    }

    Console.WriteLine();
    Console.WriteLine($"Đang ghi bù {truoc.TongThieuThatSu} lượt...");

    var ketQua = await DongBoZk1Service.DongBoKhoangAsync(context, thamSo.Tu, thamSo.Den);

    Console.WriteLine($"Tổng lượt quẹt xét: {ketQua.TongLuot}");
    Console.WriteLine($"  Ghi mới suất ăn : {ketQua.ThanhCong}");
    Console.WriteLine($"  Đã có sẵn       : {ketQua.DaCoSan}");
    Console.WriteLine($"  Bỏ do trùng     : {ketQua.BoQuaTrung}");
    Console.WriteLine($"  Ngoài giờ ăn    : {ketQua.NgoaiGioAn}");
    Console.WriteLine($"  Lỗi nghiệp vụ   : {ketQua.LoiNghiepVu} (danh sách đen hoặc không tìm thấy nhân sự)");

    // Đối soát lại sau khi ghi để biết chắc còn sót hay không
    var sau = await KiemTraDongBo.ChayAsync(context, thamSo.Tu, thamSo.Den);
    Console.WriteLine();
    Console.WriteLine("Đối soát lại sau khi ghi bù:");
    InBangDoiSoat(sau);

    if (sau.TongThieuThatSu > 0)
    {
        if (thamSo.HienChiTiet)
        {
            InChiTietThieu(sau);
        }

        Console.WriteLine();
        Console.WriteLine($"Vẫn còn {sau.TongThieuThatSu} lượt không ghi được (xem mục lỗi nghiệp vụ ở trên).");
        return MaThoatConThieu;
    }

    Console.WriteLine();
    Console.WriteLine("Đã đồng bộ đủ.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Lỗi: {ex.Message}");
    return MaThoatLoi;
}

static void InBangDoiSoat(KetQuaKiemTra ketQua)
{
    if (ketQua.TheoNgay.Count == 0)
    {
        Console.WriteLine("Không có lượt quẹt máy ZK1 nào trong khoảng này.");
        return;
    }

    Console.WriteLine($"{"Ngày",-12}{"Lượt ZK1",10}{"Đã có suất",12}{"Bỏ do trùng",13}{"Chưa đồng bộ",14}");
    foreach (var dong in ketQua.TheoNgay)
    {
        Console.WriteLine(dong.Ngay.ToString("yyyy-MM-dd").PadRight(12)
            + $"{dong.TongLuot,10}{dong.DaGhi,12}{dong.ThieuDoTrung,13}{dong.ThieuThatSu,14}");
    }

    Console.WriteLine(new string('-', 61));
    Console.WriteLine($"{"Tổng",-12}{ketQua.TongLuot,10}{ketQua.TongDaGhi,12}{ketQua.TongThieuDoTrung,13}{ketQua.TongThieuThatSu,14}");

    if (ketQua.LuotKhongCoNhanSu > 0)
    {
        Console.WriteLine($"Bỏ qua {ketQua.LuotKhongCoNhanSu} lượt có hồ sơ USERINFO không gắn mã thẻ (SSN rỗng).");
    }
}

static void InChiTietThieu(KetQuaKiemTra ketQua)
{
    if (ketQua.DanhSachThieu.Count == 0)
    {
        return;
    }

    Console.WriteLine();
    Console.WriteLine("Các lượt chưa đồng bộ:");
    foreach (var luot in ketQua.DanhSachThieu.Take(SoLuotInToiDa))
    {
        Console.WriteLine($"  {luot.ThoiDiem:yyyy-MM-dd HH:mm:ss}  {luot.MaThe,-12}{luot.TenNhanVien}");
    }

    if (ketQua.DanhSachThieu.Count > SoLuotInToiDa)
    {
        Console.WriteLine($"  ... còn {ketQua.DanhSachThieu.Count - SoLuotInToiDa} lượt nữa.");
    }
}
