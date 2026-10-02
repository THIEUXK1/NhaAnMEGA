namespace NhaAnMEGA.Utils;

// Thư mục lưu ảnh quét nhà ăn, dùng chung cho màn hình quét và dịch vụ xoá ảnh định kì.
// Web app gán từ cấu hình AnhQuet:ThuMuc (appsettings / biến môi trường AnhQuet__ThuMuc);
// không cấu hình thì lưu ngay trên máy chủ tại <ContentRoot>/App_Data/AnhQuet.
public static class ThuMucAnhQuet
{
    public const string TenKhoa = "AnhQuet:ThuMuc";
    private const string ThuMucMacDinh = "App_Data/AnhQuet";

    private static string? _giaTri;

    public static string GiaTri
    {
        get => _giaTri ??= Path.Combine(AppContext.BaseDirectory, ThuMucMacDinh);
        set => _giaTri = value;
    }

    public static string TuCauHinh(string? cauHinh, string contentRoot) =>
        string.IsNullOrWhiteSpace(cauHinh)
            ? Path.Combine(contentRoot, ThuMucMacDinh)
            : Path.GetFullPath(cauHinh, contentRoot);
}
