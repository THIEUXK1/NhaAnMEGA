// Màn hình báo cáo / xuất Excel quên thẻ (/PF/An/QuenThe)
(function () {
    'use strict';

    const SO_NUT_TRANG_TOI_DA = 7;

    const form = document.getElementById('formXuatQuenThe');
    const oTuNgay = document.getElementById('oTuNgay');
    const oDenNgay = document.getElementById('oDenNgay');
    const bang = document.getElementById('bangQuenThe');
    const thanBang = bang.querySelector('tbody');
    const tongSo = document.getElementById('tongQuenThe');
    const trangThai = document.getElementById('trangThaiQuenThe');
    const phanTrang = document.getElementById('phanTrangQuenThe');
    const tieuDeBaoCao = document.getElementById('tieuDeBaoCao');

    let trangHienTai = 1;
    let tongBanGhi = 0;
    let soDongMoiTrang = 50;

    function dinhDangNgay(d) {
        const buDay = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${buDay(d.getMonth() + 1)}-${buDay(d.getDate())}`;
    }

    function datTrangThai(noiDung, dangTai) {
        if (!noiDung) {
            trangThai.style.display = 'none';
            return;
        }
        trangThai.style.display = 'block';
        trangThai.innerHTML = dangTai
            ? `<i class="fa-solid fa-spinner fa-spin"></i> ${noiDung}`
            : `<i class="fa-regular fa-folder-open"></i> ${noiDung}`;
    }

    function khoangNgayHopLe() {
        if (!oTuNgay.value || !oDenNgay.value) {
            QS.toast('Vui lòng chọn đủ khoảng ngày', 'nhac');
            return false;
        }
        if (oTuNgay.value > oDenNgay.value) {
            QS.toast('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc', 'nhac');
            return false;
        }
        return true;
    }

    function veDong(bg) {
        const thoiGian = bg.checkTime ? new Date(bg.checkTime).toLocaleString('vi-VN') : '';
        return `
            <tr>
                <td><span class="qs-the-ma">${QS.chuanHoa(bg.userId)}</span></td>
                <td style="font-weight:600;">${QS.chuanHoa(bg.name) || 'N/A'}</td>
                <td>${QS.chuanHoa(bg.ssn) || 'N/A'}</td>
                <td>${QS.chuanHoa(bg.boPhan) || 'N/A'}</td>
                <td>${QS.chuanHoa(thoiGian)}</td>
            </tr>`;
    }

    function vePhanTrang() {
        const tongTrang = Math.max(1, Math.ceil(tongBanGhi / soDongMoiTrang));
        phanTrang.innerHTML = '';
        if (tongTrang <= 1) return;

        const them = (nhan, trang, dangChon, khoa) => {
            const nut = document.createElement('button');
            nut.type = 'button';
            nut.innerHTML = nhan;
            if (dangChon) nut.classList.add('active');
            nut.disabled = !!khoa;
            if (!khoa) nut.addEventListener('click', () => taiBaoCao(trang));
            phanTrang.appendChild(nut);
        };

        them('<i class="fa-solid fa-angle-left"></i>', trangHienTai - 1, false, trangHienTai <= 1);

        // Chỉ hiện cửa sổ trang quanh trang hiện tại cho gọn
        let batDau = Math.max(1, trangHienTai - Math.floor(SO_NUT_TRANG_TOI_DA / 2));
        const ketThuc = Math.min(tongTrang, batDau + SO_NUT_TRANG_TOI_DA - 1);
        batDau = Math.max(1, ketThuc - SO_NUT_TRANG_TOI_DA + 1);

        for (let i = batDau; i <= ketThuc; i++) them(String(i), i, i === trangHienTai, false);

        them('<i class="fa-solid fa-angle-right"></i>', trangHienTai + 1, false, trangHienTai >= tongTrang);
    }

    function capNhatTieuDe() {
        const homNay = dinhDangNgay(new Date());
        if (oTuNgay.value === homNay && oDenNgay.value === homNay) {
            tieuDeBaoCao.textContent = 'Báo cáo hôm nay';
        } else if (oTuNgay.value === oDenNgay.value) {
            tieuDeBaoCao.textContent = `Báo cáo ngày ${oTuNgay.value}`;
        } else {
            tieuDeBaoCao.textContent = `Báo cáo từ ${oTuNgay.value} đến ${oDenNgay.value}`;
        }
    }

    async function taiBaoCao(trang) {
        if (!khoangNgayHopLe()) return;

        trangHienTai = trang || 1;
        thanBang.innerHTML = '';
        phanTrang.innerHTML = '';
        capNhatTieuDe();
        datTrangThai('Đang tải báo cáo...', true);

        const thamSo = new URLSearchParams({
            fromDate: oTuNgay.value,
            toDate: oDenNgay.value,
            page: trangHienTai
        });

        try {
            const res = await fetch(`/PF/An/QuenThe/DuLieu?${thamSo}`);
            if (!res.ok) throw new Error('Không tải được báo cáo');

            const ketQua = await res.json();
            if (!ketQua.ok) throw new Error(ketQua.message || 'Không tải được báo cáo');

            tongBanGhi = ketQua.data.totalItems;
            soDongMoiTrang = ketQua.data.pageSize;
            tongSo.textContent = tongBanGhi;

            if (ketQua.data.items.length === 0) {
                datTrangThai('Không có lượt quên thẻ nào trong khoảng ngày này', false);
                return;
            }

            thanBang.innerHTML = ketQua.data.items.map(veDong).join('');
            datTrangThai('', false);
            vePhanTrang();
        } catch (err) {
            tongSo.textContent = '0';
            datTrangThai(err.message, false);
            QS.toast(err.message, 'loi');
        }
    }

    // Mặc định lấy dữ liệu trong ngày hôm nay
    const homNay = new Date();
    oTuNgay.value = dinhDangNgay(homNay);
    oDenNgay.value = dinhDangNgay(homNay);

    document.querySelectorAll('.btn-nhanh').forEach(nut => {
        nut.addEventListener('click', function () {
            const soNgay = parseInt(nut.dataset.soNgay, 10);
            const den = new Date();
            const tu = new Date();
            tu.setDate(den.getDate() - soNgay);

            oTuNgay.value = dinhDangNgay(tu);
            oDenNgay.value = dinhDangNgay(den);
            taiBaoCao(1);
        });
    });

    document.getElementById('btnXemBaoCao').addEventListener('click', () => taiBaoCao(1));

    form.addEventListener('submit', async function (e) {
        e.preventDefault();
        if (!khoangNgayHopLe()) return;

        const nutXuat = form.querySelector('button[type=submit]');

        await QS.nutBan(nutXuat, async () => {
            try {
                const res = await fetch('/PF/An/ExportExcel', {
                    method: 'POST',
                    body: new URLSearchParams(new FormData(form))
                });
                if (!res.ok) throw new Error('Không xuất được dữ liệu');

                const blob = await res.blob();
                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = `QuenThe_${oTuNgay.value}_${oDenNgay.value}.xlsx`;
                document.body.appendChild(link);
                link.click();
                link.remove();
                URL.revokeObjectURL(url);

                QS.toast('Đã xuất file Excel', 'ok');
            } catch (err) {
                QS.toast(err.message, 'loi');
            }
        });
    });

    taiBaoCao(1);
})();
