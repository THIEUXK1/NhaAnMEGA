using System.Text.Json;

namespace ZkPullerPF
{
    // Cấu hình đọc từ appsettings.json nằm cạnh file exe
    public class CauHinh
    {
        public string ChuoiKetNoi { get; set; } = string.Empty;
        public MayChamCongConfig MayChamCong { get; set; } = new();
        public int SoNgayQuetLui { get; set; } = 2;      // Chỉ xét bản ghi trong N ngày gần nhất cho nhẹ
        public string ThuMucLog { get; set; } = "logs";

        public static CauHinh Doc(string duongDan)
        {
            if (!File.Exists(duongDan))
            {
                throw new FileNotFoundException($"Không tìm thấy file cấu hình: {duongDan}");
            }

            var noiDung = File.ReadAllText(duongDan);
            var cauHinh = JsonSerializer.Deserialize<CauHinh>(noiDung, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (cauHinh == null || string.IsNullOrWhiteSpace(cauHinh.ChuoiKetNoi))
            {
                throw new InvalidOperationException("File cấu hình thiếu ChuoiKetNoi.");
            }

            return cauHinh;
        }
    }

    public class MayChamCongConfig
    {
        public string Ip { get; set; } = string.Empty;
        public int Port { get; set; } = 4370;
        public int MachineNumber { get; set; } = 1;      // Số máy dùng khi gọi SDK
        public string SensorId { get; set; } = string.Empty;   // Ghi vào cột SENSORID của CHECKINOUT
        public string SerialNumber { get; set; } = string.Empty; // Ghi vào cột sn cho giống ZKTime
    }
}
