// Tiện ích dùng chung cho các màn hình quản lý nhà ăn PF
(function () {
    'use strict';

    const THOI_GIAN_TOAST = 3000;

    // Hiện thông báo nổi góc phải: loai = 'ok' | 'loi' | 'nhac'
    function toast(noiDung, loai) {
        const boc = document.getElementById('qsToast');
        if (!boc) return;

        const el = document.createElement('div');
        el.className = 'qs-toast qs-toast-' + (loai || 'ok');
        el.textContent = noiDung;
        boc.appendChild(el);

        setTimeout(() => el.remove(), THOI_GIAN_TOAST);
    }

    // Khoá nút trong lúc gọi API để tránh bấm trùng
    async function nutBan(nut, congViec) {
        const chuCu = nut.innerHTML;
        nut.disabled = true;
        nut.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang xử lý...';
        try {
            return await congViec();
        } finally {
            nut.disabled = false;
            nut.innerHTML = chuCu;
        }
    }

    // Chặn ký tự HTML khi đổ dữ liệu ra DOM
    function chuanHoa(giaTri) {
        if (giaTri === null || giaTri === undefined) return '';
        return String(giaTri)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    window.QS = { toast, nutBan, chuanHoa };
})();

// Điều khiển menu trái: thu gọn (desktop) và mở/đóng dạng ngăn kéo (màn hình hẹp)
(function () {
    'use strict';

    const KHOA_THU_GON = 'qs-menu-thu-gon';

    const menu = document.getElementById('qsMenu');
    const lopPhu = document.getElementById('qsLopPhu');
    const nutThu = document.getElementById('qsThuMenu');
    const nutMo = document.getElementById('qsMoMenu');
    if (!menu) return;

    // Khôi phục trạng thái thu gọn lần trước
    try {
        if (localStorage.getItem(KHOA_THU_GON) === '1') {
            document.body.classList.add('qs-thu-gon');
        }
    } catch (e) {
        // Trình duyệt chặn localStorage thì bỏ qua, dùng mặc định
    }

    function dongNganKeo() {
        menu.classList.remove('hien');
        if (lopPhu) lopPhu.classList.remove('hien');
    }

    if (nutThu) {
        nutThu.addEventListener('click', function (e) {
            e.preventDefault();
            const thuGon = document.body.classList.toggle('qs-thu-gon');
            try {
                localStorage.setItem(KHOA_THU_GON, thuGon ? '1' : '0');
            } catch (err) {
                // Không lưu được thì vẫn đổi giao diện bình thường
            }
        });
    }

    if (nutMo) {
        nutMo.addEventListener('click', function (e) {
            e.preventDefault();
            menu.classList.add('hien');
            if (lopPhu) lopPhu.classList.add('hien');
        });
    }

    if (lopPhu) lopPhu.addEventListener('click', dongNganKeo);

    // Bấm một mục trong menu ở màn hình hẹp thì đóng ngăn kéo lại
    menu.addEventListener('click', function (e) {
        if (e.target.closest('.qs-menu-item')) dongNganKeo();
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') dongNganKeo();
    });
})();
