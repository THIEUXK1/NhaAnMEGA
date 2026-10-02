// Màn hình quản lý danh sách đen (/MG/BlackList)
(function () {
    'use strict';

    const DUONG_DAN_TRANG = '/MG/BlackList';

    const oTim = document.getElementById('oTimBlacklist');
    const bang = document.getElementById('bangBlacklist');
    const khongCoDuLieu = document.getElementById('khongCoBlacklist');
    const tongSo = document.getElementById('tongBlacklist');
    const formLuu = document.getElementById('formBlacklist');
    const formImport = document.querySelector('form[action="/MG/BlackList/Import"]');

    // Nạp lại phần thân bảng từ server mà không tải lại trang
    async function napLaiBang() {
        const res = await fetch(DUONG_DAN_TRANG, { headers: { 'X-Requested-With': 'fetch' } });
        if (!res.ok) throw new Error('Không tải được danh sách');

        const html = await res.text();
        const trangMoi = new DOMParser().parseFromString(html, 'text/html');
        const thanBangMoi = trangMoi.querySelector('#bangBlacklist tbody');
        if (!thanBangMoi) throw new Error('Dữ liệu trả về không hợp lệ');

        bang.querySelector('tbody').innerHTML = thanBangMoi.innerHTML;
        capNhatTrangThaiBang();
        locDanhSach();
    }

    function capNhatTrangThaiBang() {
        const soDong = bang.querySelectorAll('tbody tr').length;
        tongSo.textContent = soDong;
        khongCoDuLieu.style.display = soDong === 0 ? 'block' : 'none';
    }

    function locDanhSach() {
        const tuKhoa = (oTim.value || '').trim().toLowerCase();
        let hienThi = 0;

        bang.querySelectorAll('tbody tr').forEach(dong => {
            const khop = !tuKhoa || (dong.dataset.tim || '').includes(tuKhoa);
            dong.style.display = khop ? '' : 'none';
            if (khop) hienThi++;
        });

        tongSo.textContent = hienThi;
        khongCoDuLieu.style.display = hienThi === 0 ? 'block' : 'none';
    }

    function moModal(id, ma, ten) {
        document.getElementById('oId').value = id || 0;
        document.getElementById('oMaNhanVien').value = ma || '';
        document.getElementById('oTenNhanVien').value = ten || '';
        document.getElementById('tieuDeModal').textContent = id ? 'Cập nhật blacklist' : 'Thêm mới blacklist';
        $('#modalBlacklist').modal('show');
        setTimeout(() => document.getElementById('oMaNhanVien').focus(), 300);
    }

    oTim.addEventListener('input', locDanhSach);

    document.getElementById('btnThemMoi').addEventListener('click', () => moModal(0, '', ''));

    // Sửa / xóa dùng uỷ quyền sự kiện vì thân bảng được nạp lại động
    bang.addEventListener('click', async function (e) {
        const nutSua = e.target.closest('.btn-sua');
        if (nutSua) {
            moModal(nutSua.dataset.id, nutSua.dataset.ma, nutSua.dataset.ten);
            return;
        }

        const nutXoa = e.target.closest('.btn-xoa');
        if (!nutXoa) return;

        if (!confirm(`Xác nhận xóa nhân viên ${nutXoa.dataset.ten || ''} khỏi danh sách đen?`)) return;

        await QS.nutBan(nutXoa, async () => {
            try {
                const res = await fetch(`/MG/BlackList/Remove/${nutXoa.dataset.id}`, { method: 'POST' });
                if (!res.ok) throw new Error('Xóa thất bại');

                nutXoa.closest('tr').remove();
                capNhatTrangThaiBang();
                QS.toast('Đã xóa khỏi danh sách đen', 'ok');
            } catch (err) {
                QS.toast('Không xóa được: ' + err.message, 'loi');
            }
        });
    });

    formLuu.addEventListener('submit', async function (e) {
        e.preventDefault();
        const nutLuu = formLuu.querySelector('button[type=submit]');

        await QS.nutBan(nutLuu, async () => {
            try {
                const res = await fetch('/MG/BlackList/Save', {
                    method: 'POST',
                    body: new URLSearchParams(new FormData(formLuu))
                });
                if (!res.ok) throw new Error('Lưu thất bại');

                $('#modalBlacklist').modal('hide');
                await napLaiBang();
                QS.toast('Đã lưu thông tin nhân viên', 'ok');
            } catch (err) {
                QS.toast('Không lưu được: ' + err.message, 'loi');
            }
        });
    });

    formImport.addEventListener('submit', async function (e) {
        e.preventDefault();
        const nutImport = formImport.querySelector('button[type=submit]');

        await QS.nutBan(nutImport, async () => {
            try {
                const res = await fetch('/MG/BlackList/Import', {
                    method: 'POST',
                    body: new FormData(formImport)
                });
                if (!res.ok) throw new Error('Import thất bại');

                formImport.reset();
                await napLaiBang();
                QS.toast('Đã nhập dữ liệu từ file Excel', 'ok');
            } catch (err) {
                QS.toast('Không import được: ' + err.message, 'loi');
            }
        });
    });

    capNhatTrangThaiBang();
})();
