# DECISION LOG

Lịch sử quyết định. 5 dòng gần nhất sống ở [00-context-memory.md](00-context-memory.md);
quá 5 thì đẩy xuống đây. Mỗi dòng: **quyết định — lý do — hệ quả**, ngày tuyệt đối.

| Ngày | Quyết định | Lý do | Hệ quả |
|---|---|---|---|
| 2026-09-09 | Auth bằng Session, route mặc định `/PF/Login`, không dùng Identity/JWT | Ứng dụng nội bộ, không cần quản lý user phức tạp | Đổi sang Identity/JWT phải có quyết định mới |
| 2026-09-09 | Tách JS từng màn hình ra `wwwroot/js/<feature>.js` | `<script>` inline trong View trộn UI với nghiệp vụ, khó sửa | Màn hình mới bắt buộc có file JS riêng |
| 2026-09-09 | Xuất Excel bằng ClosedXML thay vì Interop | Server không cài Office | Báo cáo mới dùng chung `BaoCaoNgayExcelService` làm mẫu |
