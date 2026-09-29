using System.Reflection;

namespace ZkPullerPF
{
    // Một lượt quẹt đọc được từ bộ nhớ máy chấm công
    public record LuotQuet(string MaNhanVien, DateTime ThoiDiem, int KieuXacThuc, int KieuVaoRa);

    /// <summary>
    /// Đọc log chấm công trực tiếp từ máy qua COM zkemkeeper (chỉ đọc, không xoá log trên máy).
    /// Gọi kiểu late-binding để không cần interop assembly lúc build; máy chạy chỉ cần đã đăng kí SDK 32-bit.
    /// </summary>
    public class DocMayChamCong
    {
        private const string ProgIdSdk = "zkemkeeper.ZKEM";
        private const int SoLanThuKetNoi = 3;        // Máy bận thì thử lại, tránh báo lỗi oan
        private const int GiayChoGiuaHaiLan = 15;    // Chờ đủ lâu để phiên cũ trên máy được giải phóng

        private readonly MayChamCongConfig _cauHinh;
        private readonly GhiLog _log;
        private readonly Type _kieuSdk;
        private readonly object _sdk;

        public DocMayChamCong(MayChamCongConfig cauHinh, GhiLog log)
        {
            _cauHinh = cauHinh;
            _log = log;

            _kieuSdk = Type.GetTypeFromProgID(ProgIdSdk)
                ?? throw new InvalidOperationException(
                    $"Chưa đăng kí SDK {ProgIdSdk} trên máy này. Chạy: regsvr32 C:\\Windows\\SysWOW64\\zkemkeeper.dll");

            _sdk = Activator.CreateInstance(_kieuSdk)
                ?? throw new InvalidOperationException($"Không khởi tạo được COM {ProgIdSdk}");
        }

        public List<LuotQuet> DocLog(DateTime tuThoiDiem)
        {
            var ketQua = new List<LuotQuet>();

            if (!KetNoiCoThuLai())
            {
                throw new InvalidOperationException(
                    $"Không kết nối được máy {_cauHinh.Ip}:{_cauHinh.Port} sau {SoLanThuKetNoi} lần thử");
            }

            try
            {
                // Không gọi EnableDevice(false) để máy vẫn nhận vân tay bình thường trong lúc đọc
                if (!GoiBool("ReadGeneralLogData", _cauHinh.MachineNumber))
                {
                    throw new InvalidOperationException("Không đọc được vùng log chấm công của máy");
                }

                // SSR_GetGeneralLogData(machineNumber, out maNV, out verify, out inOut, out y, out M, out d, out h, out m, out s, ref workCode)
                var thamSo = new object?[] { _cauHinh.MachineNumber, string.Empty, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                var thamChieu = new ParameterModifier(thamSo.Length);
                for (var i = 1; i < thamSo.Length; i++)
                {
                    thamChieu[i] = true;
                }

                while (GoiBoolCoThamChieu("SSR_GetGeneralLogData", thamSo, thamChieu))
                {
                    var thoiDiem = new DateTime(
                        Convert.ToInt32(thamSo[4]), Convert.ToInt32(thamSo[5]), Convert.ToInt32(thamSo[6]),
                        Convert.ToInt32(thamSo[7]), Convert.ToInt32(thamSo[8]), Convert.ToInt32(thamSo[9]));

                    if (thoiDiem >= tuThoiDiem)
                    {
                        ketQua.Add(new LuotQuet(
                            Convert.ToString(thamSo[1])?.Trim() ?? string.Empty,
                            thoiDiem,
                            Convert.ToInt32(thamSo[2]),
                            Convert.ToInt32(thamSo[3])));
                    }

                    // Trả tham số về trạng thái ban đầu cho vòng đọc kế tiếp
                    thamSo[0] = _cauHinh.MachineNumber;
                }

                _log.Ghi($"Đọc được {ketQua.Count} lượt quẹt từ {tuThoiDiem:yyyy-MM-dd HH:mm:ss} trở đi.");
                return ketQua;
            }
            finally
            {
                try
                {
                    _kieuSdk.InvokeMember("Disconnect", BindingFlags.InvokeMethod, null, _sdk, null);
                }
                catch (TargetInvocationException ex)
                {
                    _log.GhiLoi("Ngắt kết nối máy không thành công: " + ex.InnerException?.Message);
                }
            }
        }

        // Máy chỉ cho một số ít phiên đồng thời: thử lại thưa để không làm cạn session của thiết bị
        private bool KetNoiCoThuLai()
        {
            for (var lan = 1; lan <= SoLanThuKetNoi; lan++)
            {
                if (GoiBool("Connect_Net", _cauHinh.Ip, _cauHinh.Port))
                {
                    return true;
                }

                if (lan < SoLanThuKetNoi)
                {
                    _log.Ghi($"Máy đang bận, chờ {GiayChoGiuaHaiLan} giây rồi thử lại (lần {lan}/{SoLanThuKetNoi}).");
                    Thread.Sleep(TimeSpan.FromSeconds(GiayChoGiuaHaiLan));
                }
            }

            return false;
        }

        private bool GoiBool(string tenHam, params object?[] thamSo)
            => Convert.ToBoolean(_kieuSdk.InvokeMember(tenHam, BindingFlags.InvokeMethod, null, _sdk, thamSo));

        private bool GoiBoolCoThamChieu(string tenHam, object?[] thamSo, ParameterModifier thamChieu)
            => Convert.ToBoolean(_kieuSdk.InvokeMember(tenHam, BindingFlags.InvokeMethod, null, _sdk, thamSo,
                new[] { thamChieu }, null, null));
    }
}
