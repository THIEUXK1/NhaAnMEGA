namespace ZkPullerPF
{
    // Ghi log ra màn hình và ra file theo ngày, chỉ nối thêm chứ không sửa file cũ
    public class GhiLog
    {
        // Ghi kèm BOM để mở bằng Notepad hay Get-Content trên server không bị vỡ tiếng Việt
        private static readonly System.Text.UTF8Encoding MaHoa = new(true);

        private readonly string _thuMuc;

        public GhiLog(string thuMuc)
        {
            _thuMuc = thuMuc;
            Directory.CreateDirectory(_thuMuc);
        }

        public void Ghi(string noiDung)
        {
            var dong = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {noiDung}";
            Console.WriteLine(dong);

            var duongDan = Path.Combine(_thuMuc, $"zkpuller-{DateTime.Today:yyyyMMdd}.log");
            File.AppendAllText(duongDan, dong + Environment.NewLine, MaHoa);
        }

        public void GhiLoi(string noiDung) => Ghi("LỖI: " + noiDung);
    }
}
