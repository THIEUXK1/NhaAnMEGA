using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NhaAnMEGA.Areas.NhaAnMEGA.Service
{
    public class XoaAnhDinhKi : BackgroundService
    {
        private readonly ILogger<XoaAnhDinhKi> _logger;

        // Cùng thư mục lưu ảnh quét với màn hình quét (cấu hình AnhQuet:ThuMuc)
        private readonly string _folderPath = global::NhaAnMEGA.Utils.ThuMucAnhQuet.GiaTri;

        public XoaAnhDinhKi(ILogger<XoaAnhDinhKi> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("XoaAnhDinhKi Service chạy ngầm nhà ăn PF đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.Now;

                    // Thiết lập mốc thời gian chạy là 16h00 chiều ngày hôm nay
                    var nextRunTime = new DateTime(now.Year, now.Month, now.Day, 16, 0, 0);

                    // Nếu thời gian hiện tại đã vượt quá 16h00 hôm nay, hẹn giờ chạy vào 16h00 chiều ngày mai
                    if (now >= nextRunTime)
                    {
                        nextRunTime = nextRunTime.AddDays(1);
                    }

                    var delayTime = nextRunTime - now;
                    _logger.LogInformation($"Service PF sẽ tạm nghỉ trong {delayTime.TotalHours:F2} giờ. Thời gian quét ảnh tiếp theo: {nextRunTime:dd/MM/yyyy HH:mm:ss}");

                    // Chờ đợi đến đúng thời điểm 16h00 chiều
                    await Task.Delay(delayTime, stoppingToken);

                    // Đến giờ hẹn, thực thi hành động quét và xóa ảnh cũ
                    _logger.LogInformation("Bắt đầu thực hiện tiến trình quét xóa ảnh định kì cho nhà ăn PF...");
                    DeleteOldImages();
                }
                catch (TaskCanceledException)
                {
                    // Luồng bị hủy bỏ khi ứng dụng tắt, không cần xử lý lỗi ở đây
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Lỗi xảy ra trong vòng lặp chạy ngầm PF: {ex.Message}");
                    // Nếu có lỗi hệ thống, tạm dừng 1 phút trước khi thử lại để tránh loop vô hạn làm treo CPU
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private void DeleteOldImages()
        {
            try
            {
                // Kiểm tra sự tồn tại của thư mục lưu ảnh mạng nội bộ nhà ăn PF
                if (!Directory.Exists(_folderPath))
                {
                    _logger.LogWarning($"Thư mục không tồn tại hoặc tài khoản App Pool không có quyền truy cập UNC mạng: {_folderPath}");
                    return;
                }

                // Thiết lập mốc thời gian: Lùi lại 2 tháng so với thời điểm hiện tại
                var thresholdDate = DateTime.Now.AddMonths(-2);
                var directoryInfo = new DirectoryInfo(_folderPath);
                int deletedCount = 0;

                // Quét toàn bộ danh sách các file có đuôi mở rộng là .jpg trong thư mục
                foreach (var file in directoryInfo.GetFiles("*.jpg"))
                {
                    // So sánh thời gian ghi file cuối cùng (LastWriteTime) với mốc 2 tháng trước
                    if (file.LastWriteTime < thresholdDate)
                    {
                        try
                        {
                            file.Delete();
                            deletedCount++;
                        }
                        catch (Exception ex)
                        {
                            // Nếu có file đang bị ứng dụng khác khóa hoặc lỗi riêng lẻ, log lỗi file đó lại 
                            // và tiếp tục vòng lặp để xóa các file tiếp theo, không làm dừng tiến trình chung
                            _logger.LogError($"Không thể xóa file ảnh {file.Name}: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation($"Hoàn thành tiến trình xóa file ảnh cũ hằng ngày cho nhà ăn PF. Tổng số file đã xóa thành công: {deletedCount} ảnh (Đã xóa các file lưu trước ngày {thresholdDate:dd/MM/yyyy}).");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi nghiêm trọng khi thực hiện tiến trình xóa file PF: {ex.Message}");
            }
        }
    }
}