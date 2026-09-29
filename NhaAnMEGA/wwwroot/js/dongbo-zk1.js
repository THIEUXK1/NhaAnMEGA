// Hiển thị lượt quẹt vân tay máy ZK1 vừa đồng bộ lên màn hình quét, giống hệt một lượt quét mã vạch
(function () {
    const GIAY_HOI_LAI = 2000;      // Chu kì hỏi server, khớp với chu kì đồng bộ chạy ngầm
    const SO_HOP_TOI_DA = 20;       // Số hộp thông báo giữ lại trên màn hình
    const GIAY_TU_AN = 30000;       // Thời gian tự ẩn một hộp thông báo

    const ID_CONG_ZK1 = "1";        // Lượt vân tay ZK1 chỉ thuộc về cổng số 1

    let mocIdDaHienThi = null;      // null = chưa lấy mốc lần đầu

    // Chỉ hiển thị lượt vân tay khi màn hình đang chọn đúng cổng máy chấm ZK1
    function dangChonCongZk1() {
        const gate = document.getElementById("gate");
        return !!gate && gate.value === ID_CONG_ZK1;
    }

    // Tên cổng đang chọn trên dropdown, dùng khi server không trả kèm tên cổng
    function tenCongDangChon() {
        const gate = document.getElementById("gate");
        if (!gate || gate.selectedIndex < 0) return "";
        const chon = gate.options[gate.selectedIndex];
        return chon && gate.value && gate.value !== "0" ? chon.textContent.trim() : "";
    }

    /// Vẽ một lượt quét thành công lên màn hình: ô Tên, ô Số lượt, đếm "OK +n", hộp xanh và tiếng chuông
    window.hienThiQuetThanhCong = function (workerName, dailyScans, nguon, gateName) {
        const oTen = document.getElementById("name");
        const oSoLuot = document.getElementById("dailyScans");
        if (oTen) oTen.value = workerName || "";
        if (oSoLuot) oSoLuot.value = dailyScans || 0;

        const statusMessage = document.getElementById("statusMessage");
        if (statusMessage) {
            const currentMessage = statusMessage.innerHTML;
            let count = currentMessage.split('+')[1] ? parseInt(currentMessage.split('+')[1]) : 0;
            count++;
            statusMessage.innerHTML = `<span style="font-size:50px;font-weight:bold;">OK +${count}</span>`;
            statusMessage.style.display = "flex";
        }

        const container = document.getElementById("userSuccessMessageContainer");
        if (container) {
            const newBox = document.createElement("div");
            newBox.className = "success-box";

            // Lượt từ máy vân tay tô màu xanh dương để phân biệt với quét mã vạch tại cổng
            const laVanTay = nguon === "zk1";
            newBox.style.cssText = laVanTay
                ? "background:linear-gradient(135deg,#cfe2ff,#8ec5ff); border:2px solid #0d6efd; border-radius:16px; padding:20px; margin-bottom:10px; font-weight:bold; text-align:center; box-shadow:0 4px 15px rgba(0,0,0,0.2);"
                : "background:linear-gradient(135deg,#c2f0c2,#85e085); border:2px solid #009900; border-radius:16px; padding:20px; margin-bottom:10px; font-weight:bold; text-align:center; box-shadow:0 4px 15px rgba(0,0,0,0.2);";
            // Kèm tên cổng ghi nhận lượt quẹt để người trực biết lượt này thuộc cổng nào
            const tenCong = gateName || tenCongDangChon();
            const phanCong = tenCong ? ` <span style="font-size:26px;">(${tenCong})</span>` : "";
            newBox.innerHTML = laVanTay
                ? `<span style="font-size: 30px; display: block; padding: 10px;">🖐️ <strong>${workerName}</strong> quét vân tay thành công!${phanCong}</span>`
                : `<span style="font-size: 30px; display: block; padding: 10px;">✅ <strong>${workerName}</strong> quét thành công!${phanCong}</span>`;

            container.prepend(newBox);
            container.style.display = "flex";
            container.style.flexDirection = "column";
            container.style.alignItems = "center";

            if (container.children.length > SO_HOP_TOI_DA) {
                container.removeChild(container.lastChild);
            }

            setTimeout(() => {
                if (newBox && newBox.parentElement) {
                    newBox.remove();
                }
                if (container.children.length === 0) {
                    container.style.display = "none";
                }
            }, GIAY_TU_AN);
        }

        const doneSound = document.getElementById("doneSound");
        if (doneSound) {
            doneSound.currentTime = 0;
            doneSound.play().catch(() => { });
        }
    };

    // Hỏi server các lượt quẹt ZK1 mới hơn mốc đang giữ rồi hiển thị lần lượt
    async function kiemTraQuetZk1Moi() {
        // Đang đứng ở cổng khác thì không hiện người quẹt vân tay; đặt lại mốc để lúc quay lại không dội lượt cũ
        if (!dangChonCongZk1()) {
            mocIdDaHienThi = null;
            return;
        }

        try {
            const url = mocIdDaHienThi === null ? "QuetZk1Moi" : `QuetZk1Moi?tuId=${mocIdDaHienThi}`;
            const res = await fetch(url);
            if (!res.ok) return;

            const data = await res.json();
            if (!data.ok) return;

            // Lần đầu chỉ ghi nhận mốc, tránh dội lại toàn bộ lượt quẹt cũ trong ngày khi vừa mở màn hình
            if (mocIdDaHienThi === null) {
                mocIdDaHienThi = data.lastId || 0;
                return;
            }

            if (!data.items || data.items.length === 0) return;

            data.items.forEach(item => {
                window.hienThiQuetThanhCong(item.workerName, item.dailyScans, "zk1", item.gateName);
            });

            mocIdDaHienThi = data.lastId;

            // Cập nhật lại các ô thống kê tổng suất giống luồng quét mã vạch
            if (typeof fetchTotalMeals === "function") {
                fetchTotalMeals();
            }
            const gate = document.getElementById("gate");
            if (gate && gate.value && typeof updateGateMealCount === "function") {
                updateGateMealCount(gate.value);
            }
        } catch (e) {
            console.error("Lỗi lấy lượt quẹt ZK1: ", e);
        }
    }

    // Đồng bộ bù: gọi server ghi nhận lại các lượt quẹt máy ZK1 mà ZKTime đẩy về muộn
    async function chayDongBoBu(event) {
        event.preventDefault();

        const oTu = document.getElementById("dongBoBuTu");
        const oDen = document.getElementById("dongBoBuDen");
        const oKetQua = document.getElementById("dongBoBuKetQua");
        const nut = document.getElementById("btnChayDongBoBu");

        if (!oTu.value || !oDen.value) {
            oKetQua.style.color = "#b02a37";
            oKetQua.textContent = "Vui lòng chọn đủ mốc bắt đầu và kết thúc!";
            return;
        }

        nut.disabled = true;
        oKetQua.style.color = "#0a3622";
        oKetQua.textContent = "Đang đồng bộ bù, vui lòng đợi...";

        try {
            const formData = new FormData();
            formData.append("tu", oTu.value);
            formData.append("den", oDen.value);

            const res = await fetch("DongBoZk1Bu", { method: "POST", body: formData });
            if (!res.ok) throw new Error("Máy chủ trả về lỗi " + res.status);

            const data = await res.json();
            oKetQua.style.color = data.ok ? "#0a3622" : "#b02a37";
            oKetQua.textContent = data.message || "Không có phản hồi từ máy chủ.";

            // Có suất ghi mới thì cập nhật lại các ô thống kê ngay, không reload trang
            if (data.ok && data.data && data.data.thanhCong > 0) {
                if (typeof fetchTotalMeals === "function") fetchTotalMeals();
                const gate = document.getElementById("gate");
                if (gate && gate.value && typeof updateGateMealCount === "function") {
                    updateGateMealCount(gate.value);
                }
            }
        } catch (e) {
            oKetQua.style.color = "#b02a37";
            oKetQua.textContent = "Lỗi đồng bộ bù: " + e.message;
        } finally {
            nut.disabled = false;
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        kiemTraQuetZk1Moi();
        setInterval(kiemTraQuetZk1Moi, GIAY_HOI_LAI);

        const nutMo = document.getElementById("btnMoDongBoBu");
        const khungBu = document.getElementById("dongBoBuForm");
        if (nutMo && khungBu) {
            nutMo.addEventListener("click", (event) => {
                event.preventDefault();
                khungBu.style.display = khungBu.style.display === "none" ? "block" : "none";
            });
        }

        const nutChay = document.getElementById("btnChayDongBoBu");
        if (nutChay) {
            nutChay.addEventListener("click", chayDongBoBu);
        }
    });
})();
