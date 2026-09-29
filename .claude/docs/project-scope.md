# PROJECT SCOPE

**Một câu:** ứng dụng nội bộ quản lý suất ăn nhà ăn PF — quét thẻ nhân viên tại cổng, ghi nhận
khách ăn, lưu ảnh quét, đồng bộ dữ liệu máy chấm công ZK và xuất báo cáo Excel.

## In scope
- Đăng nhập & phiên làm việc (Session + cookie ghi nhớ).
- Quét thẻ, chống quét trùng, phản hồi âm thanh/hình ảnh tức thì.
- Khách ăn, quên thẻ, blacklist, kiểm tra suất ăn.
- Ảnh quét & dọn ảnh định kỳ.
- Đồng bộ ZK1 (`DongBoZk1Service`) và tool rời trong `Tools/`.
- Báo cáo ngày + xuất Excel (ClosedXML).

## Out of scope
- Tính lương, chấm công (thuộc hệ thống Zktime), nhân sự.
- Cổng thanh toán, app mobile, tích hợp dịch vụ ngoài mạng nội bộ.
- Đa ngôn ngữ, đa tenant, public internet.

## Do's (riêng của phạm vi này — quy tắc kỹ thuật xem [`../rules/core.md`](../rules/core.md))
- Tối ưu cho vận hành thực tế tại cổng: nhanh, một tay thao tác, phản hồi tức thì khi quét thẻ.
- UI và message nghiệp vụ bằng tiếng Việt.
- Tái sử dụng model/context sẵn có; ưu tiên sửa nhỏ đúng chỗ.
- Bám tech-stack đã chốt trong [dotnet-architecture.md](dotnet-architecture.md).

## Don'ts
- Không refactor diện rộng ngoài yêu cầu; không "dọn dẹp" file không liên quan tới task.
- Không đổi/chồng thêm framework, không thêm package chưa duyệt.
- Không đổi schema DB ngoài quy trình ở [database-safety.md](database-safety.md).
- Không đổi cơ chế auth Session khi chưa có quyết định.
- Không xoá/di chuyển view, route, endpoint đang dùng mà chưa xác nhận.

## Vùng xám — phải hỏi trước
- Bất kỳ thao tác nào ghi vào DB **Zktime** ngoài các bảng suất ăn của ứng dụng.
- Xoá dữ liệu / ảnh quét hàng loạt, kể cả khi "để dọn dẹp".
- Đổi route `/PF/...`, đổi `SetApplicationName` của DataProtection (làm mất toàn bộ cookie ghi nhớ).
- Thêm endpoint không yêu cầu đăng nhập.
- Đổi ngưỡng thời gian giữ ảnh / chu kỳ đồng bộ ZK1.
