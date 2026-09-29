namespace NhaAnMEGA.Utils;

// Chuỗi kết nối DB NhaAnMEGA dùng chung cho mọi DbContext (đang tạo bằng new ...Context()).
// Web app gán từ cấu hình lúc khởi động (user-secrets / biến môi trường / appsettings);
// tool console không gán thì đọc thẳng biến môi trường ConnectionStrings__NhaAnMEGA.
public static class ChuoiKetNoi
{
    public const string TenKhoa = "NhaAnMEGA";
    private const string BienMoiTruong = "ConnectionStrings__" + TenKhoa;

    private static string? _giaTri;

    public static string GiaTri
    {
        get => _giaTri ??= Environment.GetEnvironmentVariable(BienMoiTruong)
            ?? throw new InvalidOperationException(
                $"Chưa cấu hình ConnectionStrings:{TenKhoa} (user-secrets hoặc biến môi trường {BienMoiTruong}).");
        set => _giaTri = value;
    }
}
