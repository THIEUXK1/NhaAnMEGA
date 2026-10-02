// Quản lý camera màn hình quét thẻ nhà ăn MEGA: xin quyền truy cập, chọn thiết bị và phát luồng hình
(function () {
    const ID_VIDEO = "cameraFeed";
    const ID_SELECT = "cameraSelect";
    const ID_BUTTON = "btnKetNoiCamera";
    const ID_HINT = "cameraHint";

    let currentStream = null;

    const video = () => document.getElementById(ID_VIDEO);
    const selectCam = () => document.getElementById(ID_SELECT);
    const btnCam = () => document.getElementById(ID_BUTTON);
    const hintBox = () => document.getElementById(ID_HINT);

    // Hiện/ẩn khung hướng dẫn xử lý khi trình duyệt chặn quyền camera
    function hienHuongDan(noiDungHtml) {
        const box = hintBox();
        if (!box) return;

        if (!noiDungHtml) {
            box.style.display = "none";
            box.innerHTML = "";
            return;
        }

        box.innerHTML = noiDungHtml;
        box.style.display = "block";
    }

    // Cập nhật trạng thái hiển thị trên nút kết nối
    function datTrangThaiNut(noiDung, mauNen, choPhepBam) {
        const btn = btnCam();
        if (!btn) return;
        btn.textContent = noiDung;
        btn.style.backgroundColor = mauNen;
        btn.disabled = !choPhepBam;
    }

    // Bật luồng hình theo thiết bị được chọn
    async function startCamera(deviceId) {
        if (currentStream) {
            currentStream.getTracks().forEach(track => track.stop());
            currentStream = null;
        }

        const constraints = {
            video: {
                deviceId: deviceId ? { ideal: deviceId } : true,
                width: { ideal: 640 },
                height: { ideal: 480 }
            }
        };

        let stream;
        try {
            stream = await navigator.mediaDevices.getUserMedia(constraints);
        } catch (innerError) {
            console.warn("Ràng buộc camera ưu tiên thất bại, kích hoạt luồng bảo vệ mặc định:", innerError);
            stream = await navigator.mediaDevices.getUserMedia({ video: true });
        }

        currentStream = stream;
        const el = video();
        if (el) {
            el.srcObject = stream;
        }
    }

    // Nạp danh sách camera vào ô chọn, ưu tiên giữ nguyên thiết bị đang dùng
    async function napDanhSachCamera() {
        const devices = await navigator.mediaDevices.enumerateDevices();
        const videoDevices = devices.filter(device => device.kind === "videoinput");
        const box = selectCam();
        if (!box) return videoDevices;

        const dangChon = box.value;
        box.innerHTML = "";
        videoDevices.forEach((device, index) => {
            const option = document.createElement("option");
            option.value = device.deviceId;
            option.textContent = device.label || `Camera ${index + 1}`;
            box.appendChild(option);
        });

        if (dangChon && videoDevices.some(d => d.deviceId === dangChon)) {
            box.value = dangChon;
        }
        return videoDevices;
    }

    // Luồng chính: xin quyền trình duyệt rồi bật camera đầu tiên
    async function ketNoiCamera() {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            datTrangThaiNut("Không dùng được camera", "#dc3545", false);
            hienHuongDan(window.isSecureContext
                ? "<b>Trình duyệt không hỗ trợ camera.</b><br>Hãy dùng Chrome hoặc Edge bản mới."
                : `<b>Trang đang chạy qua HTTP nên trình duyệt khoá camera.</b><br>
                   Cách xử lý: mở <code>chrome://flags/#unsafely-treat-insecure-origin-as-secure</code>,
                   thêm <code>${window.location.origin}</code> vào danh sách, chọn <b>Enabled</b> rồi khởi động lại trình duyệt.
                   Hoặc truy cập bằng <b>https</b>.`);
            return;
        }

        datTrangThaiNut("Đang kết nối camera...", "#6c757d", false);
        hienHuongDan(null);

        try {
            // getUserMedia là bước làm trình duyệt hiện hộp thoại xin quyền truy cập camera
            await startCamera(null);

            // Chỉ sau khi có quyền thì enumerateDevices mới trả về tên thiết bị thật
            const videoDevices = await napDanhSachCamera();
            const box = selectCam();
            if (box && box.value) {
                await startCamera(box.value);
            }

            datTrangThaiNut(`Camera đã bật (${videoDevices.length} thiết bị)`, "#28a745", true);
            hienHuongDan(null);
        } catch (e) {
            console.error("Không thể kết nối phần cứng camera: ", e);

            if (e && (e.name === "NotAllowedError" || e.name === "SecurityError")) {
                // Chrome đã chặn quyền cho website này nên không hiện lại hộp thoại, phải mở lại quyền thủ công
                datTrangThaiNut("Bị chặn quyền camera", "#dc3545", true);
                hienHuongDan(`<b>Trình duyệt đã CHẶN quyền camera cho trang này</b> nên không hiện hộp thoại hỏi nữa.<br>
                    Mở lại: bấm biểu tượng <b>🔒 / 📷</b> bên trái thanh địa chỉ →
                    mục <b>Camera</b> chọn <b>Cho phép (Allow)</b> → tải lại trang (Ctrl+F5).<br>
                    Hoặc vào <code>chrome://settings/content/camera</code> và xoá
                    <code>${window.location.origin}</code> khỏi danh sách chặn.`);
            } else if (e && (e.name === "NotFoundError" || e.name === "OverconstrainedError" || e.name === "DevicesNotFoundError")) {
                datTrangThaiNut("Không tìm thấy camera", "#dc3545", true);
                hienHuongDan("<b>Máy không nhận được camera nào.</b><br>Kiểm tra webcam đã cắm và không bị ứng dụng khác chiếm, rồi bấm lại nút.");
            } else if (e && (e.name === "NotReadableError" || e.name === "TrackStartError")) {
                datTrangThaiNut("Camera đang bị chiếm", "#dc3545", true);
                hienHuongDan("<b>Camera đang bị phần mềm khác sử dụng</b> (Zoom, Teams, tab khác...).<br>Đóng ứng dụng đó rồi bấm lại nút.");
            } else {
                datTrangThaiNut("Lỗi camera - bấm để thử lại", "#dc3545", true);
                hienHuongDan(`<b>Lỗi camera:</b> ${e && e.name ? e.name : "không xác định"}. Bấm nút để thử lại.`);
            }
        }
    }

    document.addEventListener("DOMContentLoaded", async function () {
        const btn = btnCam();
        if (btn) {
            btn.addEventListener("click", function (event) {
                event.preventDefault();
                ketNoiCamera();
            });
        }

        const box = selectCam();
        if (box) {
            box.addEventListener("change", function () {
                startCamera(this.value).catch(e => console.error("Lỗi đổi camera: ", e));
            });
        }

        // Nếu máy đã cấp quyền từ trước thì tự bật lại, không bắt người dùng bấm mỗi lần mở màn hình
        try {
            if (navigator.permissions && navigator.permissions.query) {
                const trangThai = await navigator.permissions.query({ name: "camera" });

                // Người dùng mở lại quyền ngay trên thanh địa chỉ thì tự kết nối, không cần tải lại trang
                trangThai.onchange = function () {
                    if (this.state === "granted") {
                        ketNoiCamera();
                    }
                };

                if (trangThai.state === "granted") {
                    ketNoiCamera();
                    return;
                }

                if (trangThai.state === "denied") {
                    datTrangThaiNut("Bị chặn quyền camera", "#dc3545", true);
                    hienHuongDan(`<b>Trình duyệt đã CHẶN quyền camera cho trang này.</b><br>
                        Bấm biểu tượng <b>🔒 / 📷</b> bên trái thanh địa chỉ → mục <b>Camera</b> chọn <b>Cho phép (Allow)</b>.`);
                    return;
                }
            }
        } catch (e) {
            // Một số trình duyệt không hỗ trợ truy vấn quyền camera, bỏ qua và chờ người dùng bấm nút
            console.warn("Không kiểm tra được quyền camera: ", e);
        }

        datTrangThaiNut("📷 Kết nối camera", "#007bff", true);
    });
})();
