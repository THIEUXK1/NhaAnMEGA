using ClosedXML.Excel;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    /// <summary>
    /// Dựng file Excel cho trang Báo cáo hằng ngày: bảng chi tiết lượt quét
    /// và bảng tổng hợp theo ngày (ngày x bữa).
    /// </summary>
    public static class BaoCaoNgayExcelService
    {
        public const string KieuNoiDungExcel = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private const string TenBuaNgoaiGio = "Ngoài giờ ăn";
        private const string DinhDangThoiGian = "dd/MM/yyyy HH:mm:ss";
        private const string DinhDangNgay = "dd/MM/yyyy";

        /// <summary>Một dòng chi tiết lượt quét đưa vào file Excel.</summary>
        public record DongChiTiet(string? MaNv, string? HoTen, int Id, string? BoPhan,
                                  string? Bua, string? CongQuet, DateTime? ThoiGian);

        /// <summary>Tổng suất của một ngày, tách theo từng bữa.</summary>
        public record DongTongHop(DateTime Ngay, Dictionary<string, int> TheoBua, int Tong);

        // Tên bữa hiển thị: "0" là lượt quẹt ngoài mọi khung giờ bữa
        public static string TenBua(string? bua)
        {
            if (string.IsNullOrWhiteSpace(bua))
            {
                return "N/A";
            }

            return bua == QuetTheService.BuaNgoaiGio ? TenBuaNgoaiGio : bua;
        }

        public static byte[] XuatChiTiet(IEnumerable<DongChiTiet> dsDong, DateTime tuNgay, DateTime denNgay)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Chi tiết");

            VeTieuDe(sheet, 7,
                tuNgay.Date == denNgay.Date
                    ? $"CHI TIẾT LƯỢT QUÉT NGÀY {tuNgay:dd/MM/yyyy}"
                    : $"CHI TIẾT LƯỢT QUÉT TỪ {tuNgay:dd/MM/yyyy} ĐẾN {denNgay:dd/MM/yyyy}");

            var cot = new[] { "Mã NV", "Họ tên", "ID", "Bộ phận", "Bữa", "Cổng quét", "Thời gian" };
            for (var i = 0; i < cot.Length; i++)
            {
                sheet.Cell(2, i + 1).Value = cot[i];
            }
            VeDongTieuDeCot(sheet.Row(2), cot.Length);

            var dong = 3;
            foreach (var x in dsDong)
            {
                sheet.Cell(dong, 1).Value = x.MaNv ?? "";
                sheet.Cell(dong, 2).Value = x.HoTen ?? "";
                sheet.Cell(dong, 3).Value = x.Id;
                sheet.Cell(dong, 4).Value = x.BoPhan ?? "";
                sheet.Cell(dong, 5).Value = TenBua(x.Bua);
                sheet.Cell(dong, 6).Value = x.CongQuet ?? "";

                if (x.ThoiGian.HasValue)
                {
                    sheet.Cell(dong, 7).Value = x.ThoiGian.Value;
                    sheet.Cell(dong, 7).Style.DateFormat.Format = DinhDangThoiGian;
                }

                dong++;
            }

            HoanThien(sheet, dong - 1, cot.Length, 2);
            return LuuThanhByte(workbook);
        }

        public static byte[] XuatTongHop(IReadOnlyList<string> cotBua, IEnumerable<DongTongHop> dsNgay,
                                         DateTime tuNgay, DateTime denNgay)
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Tổng hợp theo ngày");

            var soCot = cotBua.Count + 2;
            VeTieuDe(sheet, soCot, $"TỔNG HỢP SUẤT ĂN TỪ {tuNgay:dd/MM/yyyy} ĐẾN {denNgay:dd/MM/yyyy}");

            sheet.Cell(2, 1).Value = "Ngày";
            for (var i = 0; i < cotBua.Count; i++)
            {
                sheet.Cell(2, i + 2).Value = TenBua(cotBua[i]);
            }
            sheet.Cell(2, soCot).Value = "Tổng suất";
            VeDongTieuDeCot(sheet.Row(2), soCot);

            var dong = 3;
            foreach (var n in dsNgay)
            {
                sheet.Cell(dong, 1).Value = n.Ngay;
                sheet.Cell(dong, 1).Style.DateFormat.Format = DinhDangNgay;

                for (var i = 0; i < cotBua.Count; i++)
                {
                    sheet.Cell(dong, i + 2).Value = n.TheoBua.TryGetValue(cotBua[i], out var soLuong) ? soLuong : 0;
                }

                sheet.Cell(dong, soCot).Value = n.Tong;
                dong++;
            }

            // Dòng tổng cuối bảng
            var dongTong = dong;
            sheet.Cell(dongTong, 1).Value = $"Tổng {dsNgay.Count()} ngày";
            for (var i = 0; i < cotBua.Count; i++)
            {
                sheet.Cell(dongTong, i + 2).Value = dsNgay.Sum(n => n.TheoBua.TryGetValue(cotBua[i], out var sl) ? sl : 0);
            }
            sheet.Cell(dongTong, soCot).Value = dsNgay.Sum(n => n.Tong);
            sheet.Row(dongTong).Style.Font.Bold = true;
            sheet.Row(dongTong).Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");

            HoanThien(sheet, dongTong, soCot, 2);
            return LuuThanhByte(workbook);
        }

        private static void VeTieuDe(IXLWorksheet sheet, int soCot, string tieuDe)
        {
            var o = sheet.Range(1, 1, 1, soCot).Merge();
            o.Value = tieuDe;
            o.Style.Font.Bold = true;
            o.Style.Font.FontSize = 13;
            o.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void VeDongTieuDeCot(IXLRow dong, int soCot)
        {
            var vung = dong.Cells(1, soCot);
            vung.Style.Font.Bold = true;
            vung.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
            vung.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        private static void HoanThien(IXLWorksheet sheet, int dongCuoi, int soCot, int dongTieuDeCot)
        {
            if (dongCuoi >= dongTieuDeCot)
            {
                sheet.Range(dongTieuDeCot, 1, dongCuoi, soCot).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                sheet.Range(dongTieuDeCot, 1, dongCuoi, soCot).Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            sheet.SheetView.FreezeRows(dongTieuDeCot);
            sheet.Columns().AdjustToContents();
        }

        private static byte[] LuuThanhByte(XLWorkbook workbook)
        {
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
