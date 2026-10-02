// Màn hình tra cứu ảnh suất ăn (/MG/An/CheckAnh)
(function () {
    'use strict';

    const oId = document.getElementById('oIdLuotQuet');
    const nutTraCuu = document.getElementById('btnTraCuu');
    const khungKetQua = document.getElementById('ketQuaTraCuu');

    function veDongThongTin(nhan, giaTri, icon) {
        return `
            <div style="display:flex; gap:12px; padding:11px 14px; border-bottom:1px solid #f0f2f6;">
                <span style="width:130px; color:var(--qs-chu-mo); font-weight:600;">
                    <i class="fa-solid ${icon}"></i> ${nhan}
                </span>
                <span style="font-weight:600;">${QS.chuanHoa(giaTri) || 'N/A'}</span>
            </div>`;
    }

    function veKetQua(data) {
        const anh = data.imageBase64
            ? `<img src="data:image/jpeg;base64,${data.imageBase64}" alt="Ảnh quét thẻ"
                    style="width:100%; max-width:460px; border-radius:10px; border:1px solid var(--qs-vien); box-shadow:var(--qs-bong);" />`
            : `<div class="qs-bao qs-bao-nhac" style="margin:0;">
                   <i class="fa-solid fa-triangle-exclamation"></i> Không tìm thấy ảnh trên server
               </div>`;

        const ngay = data.date ? new Date(data.date).toLocaleString('vi-VN') : null;

        khungKetQua.innerHTML = `
            <div class="qs-the-tieu-de"><i class="fa-solid fa-circle-info" style="color:var(--qs-xanh)"></i> Thông tin lượt quét</div>
            ${veDongThongTin('ID', data.id, 'fa-hashtag')}
            ${veDongThongTin('Nhân viên', data.workerName, 'fa-user')}
            ${veDongThongTin('Mã thẻ', data.cardId, 'fa-id-card')}
            ${veDongThongTin('Bữa ăn', data.bua, 'fa-bowl-food')}
            ${veDongThongTin('Thời gian', ngay, 'fa-clock')}
            <div style="margin-top:16px; text-align:center;">${anh}</div>`;
    }

    async function traCuu() {
        const id = oId.value.trim();
        if (!id) {
            QS.toast('Vui lòng nhập ID lượt quét', 'nhac');
            oId.focus();
            return;
        }

        khungKetQua.innerHTML = '<div class="qs-rong"><i class="fa-solid fa-spinner fa-spin"></i> Đang tải dữ liệu...</div>';

        try {
            const res = await fetch(`/MG/An/CheckAnhData?id=${encodeURIComponent(id)}`);
            if (!res.ok) throw new Error('Không tìm thấy dữ liệu với ID này');

            const data = await res.json();
            if (data.message) throw new Error(data.message);

            veKetQua(data);
        } catch (err) {
            khungKetQua.innerHTML = `<div class="qs-bao qs-bao-loi" style="margin:0;">
                    <i class="fa-solid fa-circle-exclamation"></i> ${QS.chuanHoa(err.message)}
                </div>`;
        }
    }

    nutTraCuu.addEventListener('click', traCuu);

    oId.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter') return;
        e.preventDefault();
        traCuu();
    });

    // Mở từ báo cáo hằng ngày (/MG/An/CheckAnh?id=123): điền sẵn ID và tra luôn
    const idTuUrl = new URLSearchParams(location.search).get('id');
    if (idTuUrl) {
        oId.value = idTuUrl;
        traCuu();
    }

    oId.focus();
})();
