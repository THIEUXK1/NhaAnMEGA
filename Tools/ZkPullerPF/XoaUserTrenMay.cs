using System.Reflection;

namespace ZkPullerPF
{
    // Một người dùng đang nằm trong bộ nhớ máy
    public record UserTrenMay(string Ma, string Ten, int Quyen, bool DangBat);

    // Kết quả xử lý một máy
    public enum KetQuaXoa
    {
        KhongKetNoiDuoc,
        KhongCoUser,
        DaTimThay,      // chế độ kiểm tra: có user nhưng chưa xoá
        DaXoa,
        LoiKhiXoa
    }

    /// <summary>
    /// Xoá hẳn một người dùng (theo badge number) khỏi bộ nhớ máy chấm công qua COM zkemkeeper.
    /// Mặc định chỉ kiểm tra; muốn xoá thật phải truyền thatSu = true.
    /// </summary>
    public class XoaUserTrenMay
    {
        private const string ProgIdSdk = "zkemkeeper.ZKEM";
        private const int SoLanThuKetNoi = 3;
        private const int GiayChoGiuaHaiLan = 15;
        private const int XoaTatCaDuLieu = 12;   // backup number 12 = xoá cả user lẫn vân tay/khuôn mặt

        private readonly GhiLog _log;
        private readonly Type _kieuSdk;
        private readonly object _sdk;

        // Mã đúng như máy đang lưu (có thể là "0007"), dùng để gọi xoá cho khớp
        private string _maThucTeTrenMay = string.Empty;

        public XoaUserTrenMay(GhiLog log)
        {
            _log = log;

            _kieuSdk = Type.GetTypeFromProgID(ProgIdSdk)
                ?? throw new InvalidOperationException(
                    $"Chưa đăng kí SDK {ProgIdSdk} trên máy này. Chạy: regsvr32 C:\\Windows\\SysWOW64\\zkemkeeper.dll");

            _sdk = Activator.CreateInstance(_kieuSdk)
                ?? throw new InvalidOperationException($"Không khởi tạo được COM {ProgIdSdk}");
        }

        public KetQuaXoa XuLyMot(MayChamCong may, string badge, bool thatSu)
        {
            var ten = $"máy {may.MachineNumber} ({may.TenMay}) {may.Ip}:{may.Port}";

            if (!KetNoiCoThuLai(may))
            {
                _log.GhiLoi($"{ten}: không kết nối được sau {SoLanThuKetNoi} lần thử.");
                return KetQuaXoa.KhongKetNoiDuoc;
            }

            try
            {
                _maThucTeTrenMay = badge;

                if (!CoUser(may.MachineNumber, badge, out var tenUser))
                {
                    _log.Ghi($"{ten}: không có badge {badge} trong bộ nhớ máy.");
                    return KetQuaXoa.KhongCoUser;
                }

                if (!thatSu)
                {
                    _log.Ghi($"{ten}: CÓ badge {badge} (tên trên máy: \"{tenUser}\") — chế độ kiểm tra, chưa xoá.");
                    return KetQuaXoa.DaTimThay;
                }

                // Khoá máy trong lúc sửa bộ nhớ để tránh có người quẹt xen vào giữa chừng
                GoiBoolBoQuaLoi("EnableDevice", may.MachineNumber, false);

                try
                {
                    if (!GoiBool("SSR_DeleteEnrollData", may.MachineNumber, _maThucTeTrenMay, XoaTatCaDuLieu))
                    {
                        _log.GhiLoi($"{ten}: SSR_DeleteEnrollData trả về false, chưa xoá được badge {badge}.");
                        return KetQuaXoa.LoiKhiXoa;
                    }

                    GoiBoolBoQuaLoi("RefreshData", may.MachineNumber);

                    // Đọc lại để chắc chắn máy đã xoá thật, không tin mỗi giá trị trả về
                    if (CoUser(may.MachineNumber, badge, out _))
                    {
                        _log.GhiLoi($"{ten}: đã gọi xoá nhưng badge {badge} vẫn còn trên máy.");
                        return KetQuaXoa.LoiKhiXoa;
                    }

                    _log.Ghi($"{ten}: ĐÃ XOÁ badge {badge} (tên trên máy: \"{tenUser}\").");
                    return KetQuaXoa.DaXoa;
                }
                finally
                {
                    GoiBoolBoQuaLoi("EnableDevice", may.MachineNumber, true);
                }
            }
            finally
            {
                try
                {
                    _kieuSdk.InvokeMember("Disconnect", BindingFlags.InvokeMethod, null, _sdk, null);
                }
                catch (TargetInvocationException ex)
                {
                    _log.GhiLoi($"{ten}: ngắt kết nối không thành công - " + ex.InnerException?.Message);
                }
            }
        }

        // SSR_GetUserInfo(machineNumber, enrollNumber, out name, out password, out privilege, out enabled)
        // Vài dòng máy chỉ trả đúng khi đã nạp vùng user, nên tra hụt thì duyệt lại cả danh sách.
        private bool CoUser(int soMay, string badge, out string tenUser)
        {
            var thamSo = new object?[] { soMay, badge, string.Empty, string.Empty, 0, true };
            var thamChieu = new ParameterModifier(thamSo.Length);
            for (var i = 2; i < thamSo.Length; i++)
            {
                thamChieu[i] = true;
            }

            var co = Convert.ToBoolean(_kieuSdk.InvokeMember("SSR_GetUserInfo", BindingFlags.InvokeMethod, null, _sdk,
                thamSo, new[] { thamChieu }, null, null));

            if (co)
            {
                tenUser = Convert.ToString(thamSo[2])?.Trim() ?? string.Empty;
                return true;
            }

            return DuyetTimUser(soMay, badge, out tenUser);
        }

        /// <summary>
        /// Duyệt toàn bộ vùng user của máy để tìm badge, đồng thời cho biết máy đang giữ bao nhiêu người.
        /// </summary>
        private bool DuyetTimUser(int soMay, string badge, out string tenUser)
        {
            tenUser = string.Empty;
            var danhSach = DocToanBoUser(soMay);

            if (danhSach.Count == 0)
            {
                return false;
            }

            var trung = danhSach.FirstOrDefault(x => SoSanhMa(x.Ma, badge));
            if (trung is not null)
            {
                tenUser = trung.Ten;
                _maThucTeTrenMay = trung.Ma;
            }

            _log.Ghi($"Máy {soMay}: duyệt {danhSach.Count} user, {(trung is not null ? $"CÓ badge {badge}" : $"không có badge {badge}")}.");
            return trung is not null;
        }

        /// <summary>
        /// Đọc toàn bộ danh sách người dùng đang nằm trong bộ nhớ máy (mã, tên, quyền, trạng thái).
        /// Máy phải đang ở trạng thái đã kết nối.
        /// </summary>
        public List<UserTrenMay> DocToanBoUser(int soMay)
        {
            var ketQua = new List<UserTrenMay>();

            if (!GoiBool("ReadAllUserID", soMay))
            {
                _log.Ghi($"Máy {soMay}: không nạp được vùng user để duyệt.");
                return ketQua;
            }

            // SSR_GetAllUserInfo(machineNumber, out enrollNumber, out name, out password, out privilege, out enabled)
            var thamSo = new object?[] { soMay, string.Empty, string.Empty, string.Empty, 0, true };
            var thamChieu = new ParameterModifier(thamSo.Length);
            for (var i = 1; i < thamSo.Length; i++)
            {
                thamChieu[i] = true;
            }

            while (Convert.ToBoolean(_kieuSdk.InvokeMember("SSR_GetAllUserInfo", BindingFlags.InvokeMethod, null, _sdk,
                       thamSo, new[] { thamChieu }, null, null)))
            {
                ketQua.Add(new UserTrenMay(
                    Convert.ToString(thamSo[1])?.Trim() ?? string.Empty,
                    Convert.ToString(thamSo[2])?.Trim() ?? string.Empty,
                    Convert.ToInt32(thamSo[4]),
                    Convert.ToBoolean(thamSo[5])));

                thamSo[0] = soMay;
            }

            return ketQua;
        }

        /// <summary>
        /// Kết nối máy, đọc danh sách user rồi ngắt. Dùng cho lệnh liệt kê.
        /// </summary>
        public List<UserTrenMay> LietKe(MayChamCong may)
        {
            if (!KetNoiCoThuLai(may))
            {
                _log.GhiLoi($"máy {may.MachineNumber} ({may.TenMay}): không kết nối được.");
                return new List<UserTrenMay>();
            }

            try
            {
                return DocToanBoUser(may.MachineNumber);
            }
            finally
            {
                try
                {
                    _kieuSdk.InvokeMember("Disconnect", BindingFlags.InvokeMethod, null, _sdk, null);
                }
                catch (TargetInvocationException ex)
                {
                    _log.GhiLoi($"máy {may.MachineNumber}: ngắt kết nối không thành công - " + ex.InnerException?.Message);
                }
            }
        }

        // Máy có thể lưu mã dạng "0007" hoặc có khoảng trắng: so cả dạng chuỗi lẫn dạng số
        private static bool SoSanhMa(string maTrenMay, string badge)
            => string.Equals(maTrenMay, badge, StringComparison.OrdinalIgnoreCase)
               || (long.TryParse(maTrenMay, out var a) && long.TryParse(badge, out var b) && a == b);

        private bool KetNoiCoThuLai(MayChamCong may)
        {
            for (var lan = 1; lan <= SoLanThuKetNoi; lan++)
            {
                if (GoiBool("Connect_Net", may.Ip, may.Port))
                {
                    return true;
                }

                if (lan < SoLanThuKetNoi)
                {
                    _log.Ghi($"Máy {may.MachineNumber} đang bận, chờ {GiayChoGiuaHaiLan} giây rồi thử lại (lần {lan}/{SoLanThuKetNoi}).");
                    Thread.Sleep(TimeSpan.FromSeconds(GiayChoGiuaHaiLan));
                }
            }

            return false;
        }

        private bool GoiBool(string tenHam, params object?[] thamSo)
            => Convert.ToBoolean(_kieuSdk.InvokeMember(tenHam, BindingFlags.InvokeMethod, null, _sdk, thamSo));

        // Vài dòng máy không hỗ trợ EnableDevice/RefreshData: thiếu nó vẫn xoá được nên chỉ ghi log
        private void GoiBoolBoQuaLoi(string tenHam, params object?[] thamSo)
        {
            try
            {
                GoiBool(tenHam, thamSo);
            }
            catch (TargetInvocationException ex)
            {
                _log.Ghi($"Bỏ qua {tenHam}: {ex.InnerException?.Message}");
            }
        }
    }
}
