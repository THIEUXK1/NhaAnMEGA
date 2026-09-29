namespace DongBoSuatAnPF
{
    /// <summary>
    /// Tham số dòng lệnh đã chuẩn hoá: khoảng thời gian cần xét và chế độ chạy.
    /// </summary>
    public sealed class ThamSo
    {
        public const int SoNgayMacDinh = 7;

        public DateTime Tu { get; private set; }
        public DateTime Den { get; private set; }
        public bool ChayDongBo { get; private set; }     // --dong-bo: có ghi xuống Time_RecodePF
        public bool HienChiTiet { get; private set; }    // --chi-tiet: in từng lượt còn thiếu
        public string? Loi { get; private set; }

        public static ThamSo Doc(string[] args)
        {
            var thamSo = new ThamSo
            {
                ChayDongBo = args.Contains("--dong-bo"),
                HienChiTiet = args.Contains("--chi-tiet")
            };

            var soNgay = SoNgayMacDinh;
            DateTime? tu = null;
            DateTime? den = null;

            for (var i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "--ngay" or "-n":
                        if (!int.TryParse(args[i + 1], out soNgay) || soNgay <= 0)
                        {
                            thamSo.Loi = $"--ngay phải là số ngày dương, nhận được '{args[i + 1]}'.";
                            return thamSo;
                        }
                        break;

                    case "--tu":
                        if (!DateTime.TryParse(args[i + 1], out var motMoc))
                        {
                            thamSo.Loi = $"--tu không đọc được ngày '{args[i + 1]}', dùng dạng yyyy-MM-dd.";
                            return thamSo;
                        }
                        tu = motMoc;
                        break;

                    case "--den":
                        if (!DateTime.TryParse(args[i + 1], out var mocCuoi))
                        {
                            thamSo.Loi = $"--den không đọc được ngày '{args[i + 1]}', dùng dạng yyyy-MM-dd.";
                            return thamSo;
                        }
                        // Chỉ ghi ngày thì lấy trọn ngày đó
                        den = mocCuoi.TimeOfDay == TimeSpan.Zero ? mocCuoi.AddDays(1).AddSeconds(-1) : mocCuoi;
                        break;
                }
            }

            thamSo.Den = den ?? DateTime.Now;
            thamSo.Tu = tu ?? DateTime.Today.AddDays(-(soNgay - 1));

            if (thamSo.Tu > thamSo.Den)
            {
                thamSo.Loi = "Khoảng thời gian không hợp lệ: --tu lớn hơn --den.";
            }

            return thamSo;
        }
    }
}
