using Microsoft.Data.SqlClient;

namespace ZkPullerPF
{
    // Một đầu đọc lấy từ bảng machines của ZKTime
    public record MayChamCong(int MachineNumber, string TenMay, string Ip, int Port);

    /// <summary>
    /// Đọc danh sách đầu đọc từ bảng machines để không phải khai IP tay cho từng máy.
    /// machines.MachineNumber chính là giá trị ghi vào CHECKINOUT.SENSORID.
    /// </summary>
    public static class DanhSachMay
    {
        private const string SqlLay = @"
SELECT MachineNumber, MachineAlias, IP, Port
FROM machines
WHERE IP IS NOT NULL AND LTRIM(RTRIM(IP)) <> ''
ORDER BY MachineNumber";

        public static async Task<List<MayChamCong>> LayAsync(string chuoiKetNoi, CancellationToken token = default)
        {
            var ketQua = new List<MayChamCong>();

            await using var ketNoi = new SqlConnection(chuoiKetNoi);
            await ketNoi.OpenAsync(token);

            await using var lenh = new SqlCommand(SqlLay, ketNoi);
            await using var doc = await lenh.ExecuteReaderAsync(token);

            while (await doc.ReadAsync(token))
            {
                ketQua.Add(new MayChamCong(
                    Convert.ToInt32(doc["MachineNumber"]),
                    Convert.ToString(doc["MachineAlias"])?.Trim() ?? string.Empty,
                    Convert.ToString(doc["IP"])!.Trim(),
                    Convert.ToInt32(doc["Port"])));
            }

            return ketQua;
        }

        /// <summary>
        /// Lọc theo danh sách MachineNumber người dùng truyền vào; trả luôn những số không tìm thấy máy.
        /// </summary>
        public static (List<MayChamCong> Tim, List<int> KhongThay) Loc(
            IReadOnlyCollection<MayChamCong> tatCa, IReadOnlyCollection<int> danhSachSoMay)
        {
            var tim = new List<MayChamCong>();
            var khongThay = new List<int>();

            foreach (var soMay in danhSachSoMay)
            {
                var may = tatCa.FirstOrDefault(x => x.MachineNumber == soMay);
                if (may is null)
                {
                    khongThay.Add(soMay);
                }
                else
                {
                    tim.Add(may);
                }
            }

            return (tim, khongThay);
        }
    }
}
