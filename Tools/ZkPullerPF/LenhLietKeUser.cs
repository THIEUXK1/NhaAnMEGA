using System.Text;

namespace ZkPullerPF
{
    /// <summary>
    /// Điều phối lệnh --liet-ke-user: đọc danh sách user của từng máy và xuất ra file CSV
    /// để đối chiếu với USERINFO. Chỉ đọc, không đụng gì tới máy lẫn DB.
    /// </summary>
    public static class LenhLietKeUser
    {
        public static async Task<int> ChayAsync(
            string chuoiKetNoi, IReadOnlyCollection<int> danhSachSoMay, string thuMucRa, GhiLog log)
        {
            if (danhSachSoMay.Count == 0)
            {
                log.GhiLoi("Thiếu --may: phải chỉ rõ số máy cần liệt kê, ví dụ --may 109");
                return 1;
            }

            var tatCaMay = await DanhSachMay.LayAsync(chuoiKetNoi);
            var (danhSachCanXuLy, khongThay) = DanhSachMay.Loc(tatCaMay, danhSachSoMay);

            foreach (var soMay in khongThay)
            {
                log.GhiLoi($"Không tìm thấy máy số {soMay} trong bảng machines, bỏ qua.");
            }

            if (danhSachCanXuLy.Count == 0)
            {
                log.GhiLoi("Không có máy nào để liệt kê.");
                return 1;
            }

            Directory.CreateDirectory(thuMucRa);
            var doc = new XoaUserTrenMay(log);
            var coLoi = false;

            foreach (var may in danhSachCanXuLy)
            {
                var danhSach = doc.LietKe(may);

                if (danhSach.Count == 0)
                {
                    log.GhiLoi($"Máy {may.MachineNumber} ({may.TenMay}): không đọc được user nào.");
                    coLoi = true;
                    continue;
                }

                var duongDan = Path.Combine(thuMucRa, $"user_may{may.MachineNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                var noiDung = new StringBuilder("Ma,Ten,Quyen,DangBat\n");

                foreach (var user in danhSach)
                {
                    noiDung.Append($"\"{user.Ma}\",\"{user.Ten.Replace("\"", "'")}\",{user.Quyen},{user.DangBat}\n");
                }

                await File.WriteAllTextAsync(duongDan, noiDung.ToString(), Encoding.UTF8);
                log.Ghi($"Máy {may.MachineNumber} ({may.TenMay}): {danhSach.Count} user -> {duongDan}");
            }

            return coLoi ? 1 : 0;
        }
    }
}
