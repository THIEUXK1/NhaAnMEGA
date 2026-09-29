using Microsoft.Data.SqlClient;

namespace ZkPullerPF
{
    // Kết quả một vòng ghi xuống CHECKINOUT
    public class KetQuaGhi
    {
        public int DaChen { get; set; }
        public int DaCoSan { get; set; }
        public int KhongThayNhanSu { get; set; }
    }

    /// <summary>
    /// Ghi lượt quẹt đọc từ máy sang bảng CHECKINOUT, đúng định dạng phần mềm ZKTime vẫn ghi.
    /// Chống trùng bằng khoá tự nhiên USERID + CHECKTIME + SENSORID nên chạy lại bao nhiêu lần cũng an toàn.
    /// </summary>
    public class GhiCheckinout
    {
        private const string SqlChen = @"
DECLARE @uid int = (SELECT TOP 1 USERID FROM USERINFO WHERE BADGENUMBER = @badge);
IF @uid IS NULL
    SELECT -1;
-- Khoá chính của CHECKINOUT là USERID + CHECKTIME (không có SENSORID), nên phải kiểm tra đúng cặp đó
ELSE IF EXISTS (SELECT 1 FROM CHECKINOUT WHERE USERID = @uid AND CHECKTIME = @gio)
    SELECT 0;
ELSE
BEGIN
    INSERT INTO CHECKINOUT (USERID, CHECKTIME, CHECKTYPE, VERIFYCODE, SENSORID, WorkCode, sn, UserExtFmt)
    VALUES (@uid, @gio, 'I', @verify, @sensor, '0', @sn, 1);
    SELECT 1;
END";

        private readonly string _chuoiKetNoi;
        private readonly MayChamCongConfig _cauHinhMay;
        private readonly GhiLog _log;

        public GhiCheckinout(string chuoiKetNoi, MayChamCongConfig cauHinhMay, GhiLog log)
        {
            _chuoiKetNoi = chuoiKetNoi;
            _cauHinhMay = cauHinhMay;
            _log = log;
        }

        public async Task<KetQuaGhi> GhiAsync(IEnumerable<LuotQuet> danhSach, CancellationToken token = default)
        {
            var ketQua = new KetQuaGhi();

            await using var ketNoi = new SqlConnection(_chuoiKetNoi);
            await ketNoi.OpenAsync(token);

            foreach (var luot in danhSach.OrderBy(x => x.ThoiDiem))
            {
                await using var lenh = new SqlCommand(SqlChen, ketNoi);
                lenh.Parameters.AddWithValue("@badge", luot.MaNhanVien);
                lenh.Parameters.AddWithValue("@gio", luot.ThoiDiem);
                lenh.Parameters.AddWithValue("@sensor", _cauHinhMay.SensorId);
                lenh.Parameters.AddWithValue("@sn", _cauHinhMay.SerialNumber);
                lenh.Parameters.AddWithValue("@verify", luot.KieuXacThuc);

                var ma = Convert.ToInt32(await lenh.ExecuteScalarAsync(token));

                switch (ma)
                {
                    case 1:
                        ketQua.DaChen++;
                        _log.Ghi($"Chèn {luot.ThoiDiem:yyyy-MM-dd HH:mm:ss} badge {luot.MaNhanVien}");
                        break;
                    case 0:
                        ketQua.DaCoSan++;
                        break;
                    default:
                        ketQua.KhongThayNhanSu++;
                        _log.Ghi($"Bỏ qua {luot.ThoiDiem:yyyy-MM-dd HH:mm:ss}: badge {luot.MaNhanVien} không có trong USERINFO");
                        break;
                }
            }

            return ketQua;
        }
    }
}
