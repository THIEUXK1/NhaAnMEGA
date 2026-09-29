namespace ZkPullerPF
{
    /// <summary>
    /// Điều phối lệnh --xoa-user: lấy danh sách máy từ DB, chạy lần lượt từng máy, tổng kết.
    /// Không đụng tới dữ liệu trong DB, chỉ tác động bộ nhớ máy chấm công.
    /// </summary>
    public static class LenhXoaUser
    {
        public static async Task<int> ChayAsync(
            string chuoiKetNoi, string badge, IReadOnlyCollection<int> danhSachSoMay, bool thatSu, GhiLog log)
        {
            if (danhSachSoMay.Count == 0)
            {
                log.GhiLoi("Thiếu --may: phải chỉ rõ số máy cần xử lý, ví dụ --may 4,5,6,108,109,110,111,112");
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
                log.GhiLoi("Không có máy nào để xử lý.");
                return 1;
            }

            log.Ghi(thatSu
                ? $"XOÁ THẬT badge {badge} trên {danhSachCanXuLy.Count} máy."
                : $"Chế độ kiểm tra: dò badge {badge} trên {danhSachCanXuLy.Count} máy, không xoá gì.");

            var thongKe = new Dictionary<KetQuaXoa, int>();
            var xoa = new XoaUserTrenMay(log);

            foreach (var may in danhSachCanXuLy)
            {
                KetQuaXoa ketQua;
                try
                {
                    ketQua = xoa.XuLyMot(may, badge, thatSu);
                }
                catch (Exception ex)
                {
                    log.GhiLoi($"Máy {may.MachineNumber} ({may.TenMay}): lỗi - {ex.Message}");
                    ketQua = KetQuaXoa.LoiKhiXoa;
                }

                thongKe[ketQua] = thongKe.GetValueOrDefault(ketQua) + 1;
            }

            log.Ghi("Tổng kết: " + string.Join(", ", thongKe.Select(x => $"{x.Key} = {x.Value}")));

            // Còn máy chưa kết nối được hoặc xoá lỗi thì trả mã lỗi để biết mà chạy lại
            var conViecChuaXong = thongKe.GetValueOrDefault(KetQuaXoa.KhongKetNoiDuoc)
                                  + thongKe.GetValueOrDefault(KetQuaXoa.LoiKhiXoa);

            if (conViecChuaXong > 0)
            {
                log.GhiLoi($"Còn {conViecChuaXong} máy chưa xử lý xong, cần chạy lại.");
                return 1;
            }

            return 0;
        }
    }
}
