// Màn hình quản lý khách ăn (/NhaAnMEGA/NhaAnPF/KhachAn)
(function () {
    'use strict';

    const DO_TRE_TIM = 350;          // ms chờ sau khi gõ mới gọi API
    const SO_NUT_TRANG_TOI_DA = 7;
    const MAU_CHU_CAI = ['#0061f2', '#1aa053', '#f0a500', '#e03131', '#7b4bd6', '#0d9488'];
    const SO_DONG_TAT_CA = 100000;   // chọn "Tất cả" thì lấy trong một trang duy nhất

    const bang = document.getElementById('bangKhachAn');
    const thanBang = bang.querySelector('tbody');
    const oTim = document.getElementById('oTimKhachAn');
    const chonSoDong = document.getElementById('chonSoDong');
    const tongSo = document.getElementById('tongKhachAn');
    const trangThai = document.getElementById('trangThaiKhachAn');
    const phanTrang = document.getElementById('phanTrangKhachAn');
    const theThemMoi = document.getElementById('theThemMoi');
    const formThem = document.getElementById('formThemKhachAn');

    let trangHienTai = 1;
    let tongBanGhi = 0;
    let hanTim = null;

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

    // Màu chữ cái đầu cố định theo tên để nhìn quen mắt giữa các lần tải
    function mauTheoTen(ten) {
        let tong = 0;
        for (let i = 0; i < ten.length; i++) tong += ten.charCodeAt(i);
        return MAU_CHU_CAI[tong % MAU_CHU_CAI.length];
    }

    // Khoá riêng cho nút chỉ có icon: đổi icon thành vòng quay, giữ nguyên bề ngang
    async function nutBanIcon(nut, congViec) {
        const icon = nut.querySelector('i');
        const lopCu = icon ? icon.className : '';
        nut.disabled = true;
        if (icon) icon.className = 'fa-solid fa-spinner fa-spin';
        try {
            return await congViec();
        } finally {
            nut.disabled = false;
            if (icon) icon.className = lopCu;
        }
    }

    function veDong(kh) {
        const ma = QS.chuanHoa(kh.maThe);
        const ten = QS.chuanHoa(kh.tenThe);
        const nhaThau = QS.chuanHoa(kh.nhaThau);
        const tenGoc = (kh.tenThe || '').trim();
        const chuCai = QS.chuanHoa(tenGoc.charAt(0) || '?');

        return `
            <tr data-id="${kh.id}">
                <td><span class="qs-the-ma">${ma}</span></td>
                <td>
                    <div class="ka-ten">
                        <span class="ka-chu-cai" style="background:${mauTheoTen(tenGoc)}">${chuCai}</span>
                        <input type="text" class="qs-o ka-o-sua o-ten" value="${ten}" />
                    </div>
                </td>
                <td><input type="text" class="qs-o ka-o-sua o-nhathau" value="${nhaThau}" /></td>
                <td class="ka-thao-tac">
                    <button type="button" class="ka-nut-icon ka-nut-luu btn-luu" title="Lưu thay đổi" aria-label="Lưu">
                        <i class="fa-solid fa-floppy-disk"></i>
                    </button>
                    <button type="button" class="ka-nut-icon ka-nut-xoa btn-xoa" data-ten="${ten}" title="Xóa khách ăn" aria-label="Xóa">
                        <i class="fa-solid fa-trash"></i>
                    </button>
                </td>
            </tr>`;
    }

    // Ô chọn trả 0 nghĩa là "Tất cả" -> quy về một trang thật lớn
    function soDongMoiTrang() {
        const soDong = parseInt(chonSoDong.value, 10);
        return soDong > 0 ? soDong : SO_DONG_TAT_CA;
    }

    function vePhanTrang() {
        const soDong = soDongMoiTrang();
        const tongTrang = Math.max(1, Math.ceil(tongBanGhi / soDong));
        phanTrang.innerHTML = '';
        if (tongTrang <= 1) return;

        const them = (nhan, trang, vaoHoatDong, khoa) => {
            const nut = document.createElement('button');
            nut.type = 'button';
            nut.innerHTML = nhan;
            if (vaoHoatDong) nut.classList.add('active');
            nut.disabled = !!khoa;
            if (!khoa) nut.addEventListener('click', () => taiDanhSach(trang));
            phanTrang.appendChild(nut);
        };

        them('<i class="fa-solid fa-angle-left"></i>', trangHienTai - 1, false, trangHienTai <= 1);

        // Chỉ hiện cửa sổ trang quanh trang hiện tại cho gọn
        let batDau = Math.max(1, trangHienTai - Math.floor(SO_NUT_TRANG_TOI_DA / 2));
        let ketThuc = Math.min(tongTrang, batDau + SO_NUT_TRANG_TOI_DA - 1);
        batDau = Math.max(1, ketThuc - SO_NUT_TRANG_TOI_DA + 1);

        for (let i = batDau; i <= ketThuc; i++) them(String(i), i, i === trangHienTai, false);

        them('<i class="fa-solid fa-angle-right"></i>', trangHienTai + 1, false, trangHienTai >= tongTrang);
    }

    async function taiDanhSach(trang) {
        trangHienTai = trang || 1;
        thanBang.innerHTML = '';
        phanTrang.innerHTML = '';
        datTrangThai('Đang tải danh sách...', true);

        const thamSo = new URLSearchParams({
            searchString: oTim.value.trim(),
            page: trangHienTai,
            pageSize: soDongMoiTrang()
        });

        try {
            const res = await fetch(`/Pf/khakh/list?${thamSo}`);
            if (!res.ok) throw new Error('Không tải được dữ liệu');

            const ketQua = await res.json();
            if (!ketQua.ok) throw new Error(ketQua.message || 'Không tải được dữ liệu');

            tongBanGhi = ketQua.data.totalItems;
            tongSo.textContent = tongBanGhi;

            if (ketQua.data.items.length === 0) {
                datTrangThai(oTim.value.trim() ? 'Không tìm thấy khách ăn phù hợp' : 'Chưa có khách ăn nào', false);
                return;
            }

            thanBang.innerHTML = ketQua.data.items.map(veDong).join('');
            datTrangThai('', false);
            vePhanTrang();
        } catch (err) {
            datTrangThai(err.message, false);
            QS.toast(err.message, 'loi');
        }
    }

    // ----- Tìm kiếm / số dòng -----
    oTim.addEventListener('input', function () {
        clearTimeout(hanTim);
        hanTim = setTimeout(() => taiDanhSach(1), DO_TRE_TIM);
    });

    chonSoDong.addEventListener('change', () => taiDanhSach(1));

    // ----- Thêm mới -----
    function dongFormThem() {
        formThem.reset();
        theThemMoi.classList.remove('hien');
    }

    document.getElementById('btnHienFormThem').addEventListener('click', function () {
        const dangMo = theThemMoi.classList.toggle('hien');
        if (dangMo) document.getElementById('oMaThe').focus();
    });

    document.getElementById('btnHuyThem').addEventListener('click', dongFormThem);
    document.getElementById('btnDongThem').addEventListener('click', dongFormThem);

    formThem.addEventListener('submit', async function (e) {
        e.preventDefault();
        const nutThem = formThem.querySelector('button[type=submit]');

        await QS.nutBan(nutThem, async () => {
            const duLieu = {
                MaThe: document.getElementById('oMaThe').value.trim(),
                TenThe: document.getElementById('oTenThe').value.trim(),
                NhaThau: document.getElementById('oNhaThau').value.trim()
            };

            try {
                const res = await fetch('/Pf/khakh/add', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(duLieu)
                });

                if (!res.ok) throw new Error(await res.text() || 'Thêm khách ăn thất bại');

                // Giữ form mở để nhập tiếp nhiều thẻ liên tiếp
                formThem.reset();
                document.getElementById('oMaThe').focus();
                QS.toast('Đã thêm khách ăn', 'ok');
                await taiDanhSach(1);
            } catch (err) {
                QS.toast(err.message, 'loi');
            }
        });
    });

    // ----- Sửa / xóa trên từng dòng -----
    // Đánh dấu dòng có thay đổi chưa lưu để nút Lưu nổi lên
    thanBang.addEventListener('input', function (e) {
        const o = e.target.closest('.ka-o-sua');
        if (o) o.closest('tr').classList.add('ka-doi');
    });

    thanBang.addEventListener('click', async function (e) {
        const dong = e.target.closest('tr');
        if (!dong) return;
        const id = dong.dataset.id;

        const nutLuu = e.target.closest('.btn-luu');
        if (nutLuu) {
            await nutBanIcon(nutLuu, async () => {
                try {
                    const res = await fetch('/Pf/khakh/update', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({
                            id: parseInt(id, 10),
                            tenThe: dong.querySelector('.o-ten').value.trim(),
                            nhaThau: dong.querySelector('.o-nhathau').value.trim()
                        })
                    });
                    if (!res.ok) throw new Error('Cập nhật thất bại');
                    dong.classList.remove('ka-doi');
                    QS.toast('Đã cập nhật khách ăn', 'ok');
                } catch (err) {
                    QS.toast(err.message, 'loi');
                }
            });
            return;
        }

        const nutXoa = e.target.closest('.btn-xoa');
        if (!nutXoa) return;

        if (!confirm(`Xác nhận xóa khách ăn ${nutXoa.dataset.ten || ''}?`)) return;

        await nutBanIcon(nutXoa, async () => {
            try {
                const res = await fetch(`/Pf/khakh/delete/${id}`, { method: 'POST' });
                if (!res.ok) throw new Error('Xóa thất bại');

                dong.remove();
                tongBanGhi = Math.max(0, tongBanGhi - 1);
                tongSo.textContent = tongBanGhi;
                if (thanBang.children.length === 0) await taiDanhSach(trangHienTai);
                QS.toast('Đã xóa khách ăn', 'ok');
            } catch (err) {
                QS.toast(err.message, 'loi');
            }
        });
    });

    taiDanhSach(1);
})();
