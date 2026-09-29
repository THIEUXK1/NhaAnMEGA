# CODING STANDARDS

Tóm tắt style + zero-fluff + output clamping + hot reload đã nằm ở [`../rules/core.md`](../rules/core.md).
File này chỉ bổ sung phần chi tiết.

## Naming
- Class/method/property `PascalCase`; private field `_camelCase`; hằng `PascalCase` (`const int SoSuatToiDa`).
- Service đặt tên theo nghiệp vụ + hậu tố `Service` (`QuetTheService`, `BaoCaoNgayExcelService`).
- Action trả JSON cho UI: động từ tiếng Việt không dấu (`ThemKhach`, `XoaKhach`, `LayDanhSach`).
- File JS trùng tên màn hình, kebab-case: `khachan.js`, `baocao-ngay.js`, `check-suat-an.js`.
- Id/class trong DOM đặt theo màn hình để JS bắt đúng: `#formKhachAn`, `.btn-xoa-khach`.

## Design pattern của repo
- **Controller mỏng → Service → Context.** Controller chỉ: đọc input, validate, gọi Service, trả JSON/View.
- **Service** nhận tham số nguyên thuỷ/DTO, trả DTO hoặc kết quả nghiệp vụ — không trả `IActionResult`.
- **Hosted service** cho việc chạy nền (`DongBoZk1Service` đồng bộ ZK1, `XoaAnhDinhKi` dọn ảnh):
  vòng lặp có `CancellationToken`, mỗi vòng bọc try/catch riêng để một lỗi không giết service.
- **Entity DB-First** ở `Models/Zktime/`, `Models/NhaAnMEGA/` là output scaffold — muốn field tính toán
  thì tạo ViewModel riêng, không sửa file scaffold.
- Truy vấn đọc theo `core.md`; thêm: `Include` hoặc projection để tránh N+1, `Skip/Take` khi danh sách
  có thể lớn. SQL thô: [database-safety.md](database-safety.md) mục *Truy vấn an toàn*.

## Xử lý lỗi & dữ liệu rỗng
- Endpoint UI luôn trả đúng hình dạng `{ ok, data, message }`; lỗi nghiệp vụ → `ok:false` + `message`
  tiếng Việt cho người dùng, không ném stack trace ra client.
- Validate sai → HTTP 422 kèm `errors` theo từng field.
- `catch` phải làm một trong: log rõ ràng + trả message, hoặc ném lại. Cấm `catch {}`.
- Không log mật khẩu, connection string, dữ liệu cá nhân nhân viên.
- Danh sách rỗng là trạng thái bình thường: JS render "Không có dữ liệu", không coi là lỗi.

## Frontend
- Mỗi màn hình một file `wwwroot/js/<feature>.js`; View chỉ được truyền **dữ liệu khởi tạo**
  (vd `<script>window.__initKhachAn = @Html.Raw(json)</script>`), không logic.
- `fetch` + `async/await`, luôn `if (!res.ok) ...` trước khi `res.json()`.
- Event delegation trên `document` (`document.addEventListener('click', e => { if (!e.target.closest('.btn-xoa')) return; ... })`)
  để phần DOM render lại vẫn hoạt động.
- Nút gửi phải khoá khi đang chạy (chống double-submit) và mở lại trong `finally`.
- Chỉ cập nhật đúng node thay đổi; không render lại toàn trang.
- Quét thẻ: có phản hồi âm thanh (`wwwroot/Audio`) + hình ảnh ngay, không chờ round-trip xong mới báo.

## Comment
- Tiếng Việt, giải thích **vì sao** chứ không mô tả lại code.
- Giữ mật độ comment như file xung quanh; không thêm header/doc block không được yêu cầu.
