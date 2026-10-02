// Màn hình báo cáo suất ăn hằng ngày (/MG/An/BaoCaoNgay)
(function () {
    'use strict';

    const DO_TRE_TIM = 350;
    const SO_NUT_TRANG_TOI_DA = 7;
    const BUA_NGOAI_GIO = '0';   // Lượt quẹt ngoài mọi khung giờ bữa, server ghi Bua = "0"
    const SO_COT_CHI_TIET = 7;   // Số cột bảng chi tiết (chưa tính cột xoá), dùng cho colspan của dòng mở rộng

    const oTuNgay = document.getElementById('oTuNgay');
    const oDenNgay = document.getElementById('oDenNgay');
    const oTim = document.getElementById('oTimBaoCao');
    const bang = document.getElementById('bangBaoCao');
    const thanBang = bang.querySelector('tbody');
    const tongSo = document.getElementById('tongBaoCao');
    const trangThai = document.getElementById('trangThaiBaoCao');
    const phanTrang = document.getElementById('phanTrangBaoCao');
    const theThongKe = document.getElementById('theThongKe');
    const tieuDe = document.getElementById('tieuDeBaoCao');
    const dieuHuongNgay = document.getElementById('dieuHuongNgay');
    const oSoDong = document.getElementById('oSoDong');
    const nhanSoDong = document.getElementById('nhanSoDong');
    const oGopNgay = document.getElementById('oGopNgay');
    const bocBangChiTiet = document.getElementById('bocBangChiTiet');
    const bocBangGop = document.getElementById('bocBangGop');
    const bangGop = document.getElementById('bangGopNgay');

    // Mỗi "trang" là 1 ngày: ngày đang xem nằm trong khoảng [oTuNgay, oDenNgay]
    let ngayDangXem = '';
    let trangHienTai = 1;
    let tongBanGhi = 0;
    let soDongMoiTrang = 0; // 0 = xem tất cả trong 1 trang
    let hanTim = null;
    let duocXoa = false;    // Server báo tài khoản hiện tại có quyền xoá lượt quét
    const nutXoaTest = document.getElementById('btnXoaTest');

    // Ẩn/hiện tiêu đề cột Xoá; tự tạo nếu view chưa có (view cũ còn cache khi chưa restart)
    function datCotXoa(hien) {
        let cotXoa = document.getElementById('cotXoa');
        if (!cotXoa) {
            if (!hien) return;
            cotXoa = document.createElement('th');
            cotXoa.id = 'cotXoa';
            cotXoa.style.width = '70px';
            cotXoa.textContent = 'Xoá';
            bang.querySelector('thead tr').appendChild(cotXoa);
        }
        cotXoa.hidden = !hien;
    }

    // Bộ lọc dạng tích (nhiều lựa chọn): nút thả xuống + danh sách checkbox
    function taoLocTich(boc, khiDoi) {
        const nhanMacDinh = boc.dataset.nhan || 'Tất cả';
        const tienTo = boc.dataset.tienTo || '';
        const daChon = new Set();
        let danhSach = [];

        boc.innerHTML = `
            <button type="button" class="qs-o qs-loc-nut">
                <span class="qs-loc-ten"></span>
                <i class="fa-solid fa-angle-down"></i>
            </button>
            <div class="qs-loc-bang" hidden></div>`;

        const nut = boc.querySelector('.qs-loc-nut');
        const ten = boc.querySelector('.qs-loc-ten');
        const bangChon = boc.querySelector('.qs-loc-bang');

        function capNhatNhan() {
            if (daChon.size === 0) {
                ten.textContent = nhanMacDinh;
                nut.classList.remove('dang-loc');
                return;
            }
            const ds = [...daChon];
            ten.textContent = ds.length <= 2
                ? ds.map(v => tienTo + v).join(', ')
                : `Đã chọn ${ds.length} mục`;
            nut.classList.add('dang-loc');
        }

        function ve() {
            bangChon.innerHTML = '';

            const nutBoTich = document.createElement('button');
            nutBoTich.type = 'button';
            nutBoTich.className = 'qs-loc-xoa';
            nutBoTich.textContent = 'Bỏ chọn tất cả';
            nutBoTich.addEventListener('click', function () {
                if (daChon.size === 0) return;
                daChon.clear();
                ve();
                capNhatNhan();
                khiDoi();
            });
            bangChon.appendChild(nutBoTich);

            if (danhSach.length === 0) {
                const rong = document.createElement('div');
                rong.className = 'qs-loc-rong';
                rong.textContent = 'Không có dữ liệu';
                bangChon.appendChild(rong);
                return;
            }

            danhSach.forEach(giaTri => {
                const dong = document.createElement('label');
                dong.className = 'qs-loc-dong';

                const o = document.createElement('input');
                o.type = 'checkbox';
                o.value = giaTri;
                o.checked = daChon.has(giaTri);
                o.addEventListener('change', function () {
                    if (o.checked) daChon.add(giaTri); else daChon.delete(giaTri);
                    capNhatNhan();
                    khiDoi();
                });

                const chu = document.createElement('span');
                chu.textContent = tienTo + giaTri;

                dong.appendChild(o);
                dong.appendChild(chu);
                bangChon.appendChild(dong);
            });
        }

        nut.addEventListener('click', function (e) {
            e.stopPropagation();
            const dangMo = !bangChon.hidden;
            document.querySelectorAll('.qs-loc-bang').forEach(b => b.hidden = true);
            bangChon.hidden = dangMo;
        });

        bangChon.addEventListener('click', e => e.stopPropagation());
        document.addEventListener('click', () => { bangChon.hidden = true; });

        capNhatNhan();
        ve();

        return {
            // Danh sách tuỳ chọn lấy từ dữ liệu thật, cập nhật mỗi lần tải báo cáo
            datDanhSach(ds) {
                const gop = [...new Set([...(ds || []), ...daChon])].sort();
                if (gop.join('|') === danhSach.join('|')) return;
                danhSach = gop;
                ve();
            },
            layGiaTri() {
                return [...daChon].join(',');
            },
            // Bỏ hết tích, không tự tải lại (bên gọi tự quyết định khi nào tải)
            boChon() {
                if (daChon.size === 0) return;
                daChon.clear();
                ve();
                capNhatNhan();
            }
        };
    }

    const locBua = taoLocTich(document.getElementById('locBua'), () => taiBaoCao(1));
    const locCong = taoLocTich(document.getElementById('locCong'), () => taiBaoCao(1));

    function dinhDangNgay(d) {
        const bu = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${bu(d.getMonth() + 1)}-${bu(d.getDate())}`;
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

    // Cộng/trừ số ngày cho chuỗi yyyy-MM-dd
    function congNgay(chuoiNgay, soNgay) {
        const [nam, thang, ngay] = chuoiNgay.split('-').map(Number);
        const d = new Date(nam, thang - 1, ngay);
        d.setDate(d.getDate() + soNgay);
        return dinhDangNgay(d);
    }

    function ngayHienThi(chuoiNgay) {
        const [nam, thang, ngay] = chuoiNgay.split('-').map(Number);
        return new Date(nam, thang - 1, ngay).toLocaleDateString('vi-VN', {
            weekday: 'short', day: '2-digit', month: '2-digit', year: 'numeric'
        });
    }

    // Ghim ngày đang xem vào trong khoảng đang chọn
    function chuanHoaNgayDangXem() {
        if (!ngayDangXem || ngayDangXem > oDenNgay.value) ngayDangXem = oDenNgay.value;
        if (ngayDangXem < oTuNgay.value) ngayDangXem = oTuNgay.value;
    }

    function capNhatTieuDe() {
        const homNay = dinhDangNgay(new Date());
        tieuDe.textContent = ngayDangXem === homNay
            ? 'Chi tiết lượt quét hôm nay'
            : `Chi tiết lượt quét ngày ${ngayHienThi(ngayDangXem)}`;
    }

    // Thanh chuyển ngày: lùi/tiến 1 ngày trong khoảng đã chọn
    function veDieuHuongNgay() {
        dieuHuongNgay.innerHTML = '';

        // Khoảng chỉ có đúng 1 ngày (vd hôm nay) thì không cần thanh chuyển ngày
        if (oTuNgay.value === oDenNgay.value) {
            dieuHuongNgay.hidden = true;
            return;
        }
        dieuHuongNgay.hidden = false;

        const them = (nhan, ngayDich, khoa, tieuDeNut) => {
            const nut = document.createElement('button');
            nut.type = 'button';
            nut.innerHTML = nhan;
            nut.disabled = khoa;
            if (tieuDeNut) nut.title = tieuDeNut;
            if (!khoa) nut.addEventListener('click', () => doiNgay(ngayDich));
            dieuHuongNgay.appendChild(nut);
        };

        const ngayTruoc = congNgay(ngayDangXem, -1);
        const ngaySau = congNgay(ngayDangXem, 1);

        them('<i class="fa-solid fa-angles-left"></i>', oTuNgay.value,
            ngayDangXem === oTuNgay.value, 'Ngày đầu khoảng');
        them('<i class="fa-solid fa-angle-left"></i> Ngày trước', ngayTruoc,
            ngayTruoc < oTuNgay.value, 'Lùi 1 ngày');

        const nhan = document.createElement('span');
        nhan.className = 'qs-ngay-dang-xem';
        nhan.textContent = ngayHienThi(ngayDangXem);
        dieuHuongNgay.appendChild(nhan);

        them('Ngày sau <i class="fa-solid fa-angle-right"></i>', ngaySau,
            ngaySau > oDenNgay.value, 'Tiến 1 ngày');
        them('<i class="fa-solid fa-angles-right"></i>', oDenNgay.value,
            ngayDangXem === oDenNgay.value, 'Ngày cuối khoảng');
    }

    function doiNgay(ngayMoi) {
        ngayDangXem = ngayMoi;
        taiBaoCao(1);
    }

    // Tên hiển thị của bữa: "0" là lượt quẹt ngoài giờ ăn
    function tenBua(bua) {
        return bua === BUA_NGOAI_GIO ? 'Ngoài giờ ăn' : 'Bữa ' + bua;
    }

    function veThongKe(tongTheoBua) {
        // Lượt quẹt ngoài khung giờ bữa (Bữa "0") vẫn hiện nhưng không cộng vào tổng suất ăn
        const tong = tongTheoBua.reduce((s, x) => s + (x.bua === BUA_NGOAI_GIO ? 0 : x.soLuong), 0);

        const o = (nhan, giaTri, icon, mau) => `
            <div class="qs-tk">
                <span class="qs-tk-icon"><i class="fa-solid ${icon}"></i></span>
                <span>
                    <span class="qs-tk-nhan">${QS.chuanHoa(nhan)}</span>
                    <div class="qs-tk-so" style="color:${mau};">${giaTri}</div>
                </span>
            </div>`;

        theThongKe.innerHTML = o('Tổng suất ăn', tong, 'fa-utensils', 'var(--qs-xanh)')
            + tongTheoBua.map(x => x.bua === BUA_NGOAI_GIO
                ? o(tenBua(x.bua), x.soLuong, 'fa-clock', 'var(--qs-cam, #f59e0b)')
                : o(tenBua(x.bua), x.soLuong, 'fa-bowl-food', 'var(--qs-luc)')).join('');
    }

    function veDong(bg) {
        const thoiGian = bg.mealtime ? new Date(bg.mealtime).toLocaleString('vi-VN') : '';
        return `
            <tr>
                <td><span class="qs-the-ma">${QS.chuanHoa(bg.workerId)}</span></td>
                <td style="font-weight:600;">${QS.chuanHoa(bg.workerName) || 'N/A'}</td>
                <td>
                    <button type="button" class="qs-the-ma qs-the-ma-lien-ket btn-mo-rong"
                            data-id="${QS.chuanHoa(bg.id)}"
                            title="Bấm để xem ảnh đã lưu của lượt quét này">${QS.chuanHoa(bg.id)}</button>
                </td>
                <td>${QS.chuanHoa(bg.depId) || 'N/A'}</td>
                <td>${bg.bua ? QS.chuanHoa(tenBua(bg.bua)) : 'N/A'}</td>
                <td>${QS.chuanHoa(bg.gName) || 'N/A'}</td>
                <td>${QS.chuanHoa(thoiGian)}</td>
                ${duocXoa ? `<td>${bg.laMaTest ? `
                    <button type="button" class="qs-nut qs-nut-do qs-nut-nho btn-xoa-luot"
                            data-id="${QS.chuanHoa(bg.id)}" data-ten="${QS.chuanHoa(bg.workerName)}"
                            title="Xoá lượt quét này"><i class="fa-solid fa-trash"></i></button>` : ''}
                </td>` : ''}
            </tr>`;
    }

    // ----- Mở rộng dòng: bấm ID để xem ảnh đã lưu ngay trong bảng -----

    function veChiTietLuotQuet(d) {
        const anh = d.imageBase64
            ? `<div class="qs-mo-rong-anh">
                   <img src="data:image/jpeg;base64,${d.imageBase64}" alt="Ảnh quét thẻ" loading="lazy" />
               </div>`
            : `<div class="qs-bao qs-bao-nhac">
                   <i class="fa-solid fa-triangle-exclamation"></i> Không tìm thấy ảnh trên server
               </div>`;

        const dong = (nhan, giaTri, icon) => `
            <div class="qs-mo-rong-dong">
                <span><i class="fa-solid ${icon}"></i> ${nhan}</span>
                <span>${QS.chuanHoa(giaTri) || 'N/A'}</span>
            </div>`;

        const ngay = d.date ? new Date(d.date).toLocaleString('vi-VN') : null;

        return `
            <div class="qs-mo-rong">
                ${anh}
                <div class="qs-mo-rong-tin">
                    ${dong('ID', d.id, 'fa-hashtag')}
                    ${dong('Nhân viên', d.workerName, 'fa-user')}
                    ${dong('Mã thẻ', d.cardId, 'fa-id-card')}
                    ${dong('Bữa ăn', d.bua ? tenBua(String(d.bua)) : null, 'fa-bowl-food')}
                    ${dong('Thời gian', ngay, 'fa-clock')}
                </div>
            </div>`;
    }

    async function moRongDong(nut) {
        const dongCha = nut.closest('tr');
        const dangMo = dongCha.nextElementSibling;

        // Bấm lại vào ID đang mở thì thu gọn
        if (dangMo && dangMo.classList.contains('qs-dong-mo-rong')) {
            dangMo.remove();
            nut.classList.remove('dang-mo');
            return;
        }

        // Mỗi lần chỉ mở 1 dòng cho gọn bảng
        thanBang.querySelectorAll('.qs-dong-mo-rong').forEach(tr => tr.remove());
        thanBang.querySelectorAll('.btn-mo-rong.dang-mo').forEach(b => b.classList.remove('dang-mo'));
        nut.classList.add('dang-mo');

        const dongChiTiet = document.createElement('tr');
        dongChiTiet.className = 'qs-dong-mo-rong';
        dongChiTiet.innerHTML = `<td colspan="${SO_COT_CHI_TIET + (duocXoa ? 1 : 0)}">
                <div class="qs-rong"><i class="fa-solid fa-spinner fa-spin"></i> Đang tải ảnh lượt quét...</div>
            </td>`;
        dongCha.insertAdjacentElement('afterend', dongChiTiet);

        const o = dongChiTiet.firstElementChild;
        try {
            const res = await fetch(`/MG/An/CheckAnhData?id=${encodeURIComponent(nut.dataset.id)}`);
            if (!res.ok) throw new Error('Không tìm thấy dữ liệu của lượt quét này');

            const d = await res.json();
            if (d.message) throw new Error(d.message);

            o.innerHTML = veChiTietLuotQuet(d);
        } catch (err) {
            o.innerHTML = `<div class="qs-bao qs-bao-loi">
                    <i class="fa-solid fa-circle-exclamation"></i> ${QS.chuanHoa(err.message)}
                </div>`;
        }
    }

    // Xoá 1 lượt quét (nút chỉ có khi server cho phép, server vẫn kiểm quyền lại)
    async function xoaLuotQuet(nut) {
        if (!confirm(`Xác nhận xoá lượt quét ID ${nut.dataset.id} của ${nut.dataset.ten || 'N/A'}? Không thể khôi phục.`)) return;

        await QS.nutBan(nut, async () => {
            try {
                const res = await fetch(`/MG/An/BaoCaoNgay/Xoa/${encodeURIComponent(nut.dataset.id)}`, { method: 'POST' });
                const ketQua = await res.json();
                if (!ketQua.ok) throw new Error(ketQua.message || 'Xoá thất bại');

                QS.toast(ketQua.message, 'ok');
                taiBaoCao(trangHienTai);   // tải lại để tổng số, thống kê theo bữa khớp dữ liệu
            } catch (err) {
                QS.toast('Không xoá được: ' + err.message, 'loi');
            }
        });
    }

    thanBang.addEventListener('click', function (e) {
        const nutXoa = e.target.closest('.btn-xoa-luot');
        if (nutXoa) {
            e.preventDefault();
            xoaLuotQuet(nutXoa);
            return;
        }

        const nut = e.target.closest('.btn-mo-rong');
        if (!nut) return;
        e.preventDefault();
        moRongDong(nut);
    });

    function vePhanTrang() {
        phanTrang.innerHTML = '';
        if (!soDongMoiTrang) return; // đang xem tất cả thì không cần phân trang

        const tongTrang = Math.max(1, Math.ceil(tongBanGhi / soDongMoiTrang));
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

        let batDau = Math.max(1, trangHienTai - Math.floor(SO_NUT_TRANG_TOI_DA / 2));
        const ketThuc = Math.min(tongTrang, batDau + SO_NUT_TRANG_TOI_DA - 1);
        batDau = Math.max(1, ketThuc - SO_NUT_TRANG_TOI_DA + 1);

        for (let i = batDau; i <= ketThuc; i++) them(String(i), i, i === trangHienTai, false);

        them('<i class="fa-solid fa-angle-right"></i>', trangHienTai + 1, false, trangHienTai >= tongTrang);
    }

    // Bảng tổng hợp: mỗi dòng 1 ngày, cột theo bữa + cột tổng, dòng cuối là tổng cả khoảng
    function veBangGop(d) {
        const dau = bangGop.querySelector('thead');
        const than = bangGop.querySelector('tbody');
        const chan = bangGop.querySelector('tfoot');

        dau.innerHTML = `
            <tr>
                <th>Ngày</th>
                ${d.cotBua.map(b => `<th style="width:110px;">${QS.chuanHoa(tenBua(b))}</th>`).join('')}
                <th style="width:120px;">Tổng suất</th>
            </tr>`;

        than.innerHTML = d.theoNgay.map(n => `
            <tr>
                <td>
                    <button type="button" class="qs-the-ma qs-the-ma-lien-ket btn-xem-ngay"
                            data-ngay="${n.ngay}" title="Xem chi tiết ngày này">${QS.chuanHoa(ngayHienThi(n.ngay))}</button>
                </td>
                ${d.cotBua.map(b => `<td>${n.theoBua[b] || 0}</td>`).join('')}
                <td class="qs-cot-tong">${n.tong}</td>
            </tr>`).join('');

        const tongCot = b => d.theoNgay.reduce((s, n) => s + (n.theoBua[b] || 0), 0);
        chan.innerHTML = `
            <tr>
                <td>Tổng ${d.soNgay} ngày</td>
                ${d.cotBua.map(b => `<td>${tongCot(b)}</td>`).join('')}
                <td class="qs-cot-tong">${d.tongChung}</td>
            </tr>`;

        than.querySelectorAll('.btn-xem-ngay').forEach(nut => {
            nut.addEventListener('click', function () {
                oGopNgay.checked = false;
                ngayDangXem = nut.dataset.ngay;
                taiBaoCao(1);
            });
        });
    }

    // Chế độ gộp: lấy tổng theo từng ngày cho cả khoảng, không phân trang theo ngày
    async function taiGopNgay() {
        thanBang.innerHTML = '';
        phanTrang.innerHTML = '';
        dieuHuongNgay.hidden = true;
        dieuHuongNgay.innerHTML = '';
        tieuDe.textContent = `Tổng hợp theo ngày (${ngayHienThi(oTuNgay.value)} - ${ngayHienThi(oDenNgay.value)})`;
        datTrangThai('Đang tải báo cáo...', true);

        const thamSo = new URLSearchParams({
            fromDate: oTuNgay.value,
            toDate: oDenNgay.value,
            bua: locBua.layGiaTri(),
            cong: locCong.layGiaTri(),
            tuKhoa: oTim.value.trim()
        });

        try {
            const res = await fetch(`/MG/An/BaoCaoNgay/GopNgay?${thamSo}`);
            if (!res.ok) throw new Error('Không tải được báo cáo');

            const ketQua = await res.json();
            if (!ketQua.ok) throw new Error(ketQua.message || 'Không tải được báo cáo');

            const d = ketQua.data;
            tongSo.textContent = d.tongChung;
            veThongKe(d.cotBua.map(b => ({
                bua: b,
                soLuong: d.theoNgay.reduce((s, n) => s + (n.theoBua[b] || 0), 0)
            })));

            if (d.theoNgay.length === 0) {
                bangGop.querySelector('thead').innerHTML = '';
                bangGop.querySelector('tbody').innerHTML = '';
                bangGop.querySelector('tfoot').innerHTML = '';
                datTrangThai('Không có suất ăn nào khớp điều kiện lọc', false);
                return;
            }

            veBangGop(d);
            datTrangThai('', false);
        } catch (err) {
            tongSo.textContent = '0';
            theThongKe.innerHTML = '';
            datTrangThai(err.message, false);
            QS.toast(err.message, 'loi');
        }
    }

    // Đổi giữa bảng chi tiết theo ngày và bảng tổng hợp theo ngày
    function capNhatCheDo() {
        const gop = oGopNgay.checked;
        bocBangChiTiet.hidden = gop;
        bocBangGop.hidden = !gop;
        nhanSoDong.hidden = gop;
        oSoDong.hidden = gop;
    }

    async function taiBaoCao(trang) {
        if (!oTuNgay.value || !oDenNgay.value) {
            QS.toast('Vui lòng chọn đủ khoảng ngày', 'nhac');
            return;
        }
        if (oTuNgay.value > oDenNgay.value) {
            QS.toast('Ngày bắt đầu phải nhỏ hơn hoặc bằng ngày kết thúc', 'nhac');
            return;
        }

        capNhatCheDo();
        if (oGopNgay.checked) {
            await taiGopNgay();
            return;
        }

        trangHienTai = trang || 1;
        chuanHoaNgayDangXem();
        thanBang.innerHTML = '';
        phanTrang.innerHTML = '';
        capNhatTieuDe();
        veDieuHuongNgay();
        datTrangThai('Đang tải báo cáo...', true);

        const thamSo = new URLSearchParams({
            fromDate: ngayDangXem,
            toDate: ngayDangXem,
            bua: locBua.layGiaTri(),
            cong: locCong.layGiaTri(),
            tuKhoa: oTim.value.trim(),
            page: trangHienTai,
            pageSize: oSoDong.value,
            pageSize: oSoDong.value
        });

        try {
            const res = await fetch(`/MG/An/BaoCaoNgay/DuLieu?${thamSo}`);
            if (!res.ok) throw new Error('Không tải được báo cáo');

            const ketQua = await res.json();
            if (!ketQua.ok) throw new Error(ketQua.message || 'Không tải được báo cáo');

            tongBanGhi = ketQua.data.totalItems;
            soDongMoiTrang = ketQua.data.pageSize;
            duocXoa = ketQua.data.duocXoa === true;
            datCotXoa(duocXoa);
            if (nutXoaTest) nutXoaTest.hidden = !duocXoa;
            tongSo.textContent = tongBanGhi;

            // Xem tất cả nhưng chạm trần server thì báo để người dùng chọn số dòng/trang
            if (!soDongMoiTrang && tongBanGhi > ketQua.data.items.length) {
                QS.toast(`Chỉ hiển thị ${ketQua.data.gioiHanTatCa} dòng đầu, hãy lọc bớt hoặc chọn số dòng/trang`, 'nhac');
            }

            veThongKe(ketQua.data.tongTheoBua);
            locBua.datDanhSach(ketQua.data.dsBua);
            locCong.datDanhSach(ketQua.data.dsCong);

            if (ketQua.data.items.length === 0) {
                datTrangThai('Không có suất ăn nào khớp điều kiện lọc', false);
                return;
            }

            thanBang.innerHTML = ketQua.data.items.map(veDong).join('');
            datTrangThai('', false);
            vePhanTrang();
        } catch (err) {
            tongSo.textContent = '0';
            theThongKe.innerHTML = '';
            datTrangThai(err.message, false);
            QS.toast(err.message, 'loi');
        }
    }

    // Mặc định xem báo cáo hôm nay
    const homNay = new Date();
    oTuNgay.value = dinhDangNgay(homNay);
    oDenNgay.value = dinhDangNgay(homNay);
    ngayDangXem = dinhDangNgay(homNay);

    document.querySelectorAll('.btn-nhanh').forEach(nut => {
        nut.addEventListener('click', function () {
            const soNgay = parseInt(nut.dataset.soNgay, 10);
            const den = new Date();
            const tu = new Date();

            if (soNgay === 1) {
                // Hôm qua: cả ngày hôm trước
                tu.setDate(tu.getDate() - 1);
                den.setDate(den.getDate() - 1);
            } else {
                tu.setDate(den.getDate() - soNgay);
            }

            oTuNgay.value = dinhDangNgay(tu);
            oDenNgay.value = dinhDangNgay(den);
            ngayDangXem = oDenNgay.value; // mở ở ngày mới nhất của khoảng
            taiBaoCao(1);
        });
    });

    document.getElementById('btnXemBaoCao').addEventListener('click', () => taiBaoCao(1));

    // Tải file Excel bằng fetch + blob để giữ nguyên trang, không điều hướng
    async function xuatExcel() {
        if (!oTuNgay.value || !oDenNgay.value || oTuNgay.value > oDenNgay.value) {
            QS.toast('Vui lòng chọn khoảng ngày hợp lệ', 'nhac');
            return;
        }

        const thamSo = new URLSearchParams({
            fromDate: oTuNgay.value,
            toDate: oDenNgay.value,
            bua: locBua.layGiaTri(),
            cong: locCong.layGiaTri(),
            tuKhoa: oTim.value.trim(),
            gop: oGopNgay.checked
        });

        nutXuat.disabled = true;
        const nhanCu = nutXuat.innerHTML;
        nutXuat.innerHTML = '<i class="fa-solid fa-spinner fa-spin"></i> Đang xuất...';

        try {
            const res = await fetch(`/MG/An/BaoCaoNgay/Excel?${thamSo}`);
            if (!res.ok) throw new Error('Không xuất được file Excel');

            // Server trả JSON khi phiên đăng nhập hết hạn
            if ((res.headers.get('content-type') || '').includes('application/json')) {
                const loi = await res.json();
                throw new Error(loi.message || 'Không xuất được file Excel');
            }

            const blob = await res.blob();
            const ten = (res.headers.get('content-disposition') || '').match(/filename="?([^";]+)"?/);
            const duongDan = URL.createObjectURL(blob);
            const lienKet = document.createElement('a');
            lienKet.href = duongDan;
            lienKet.download = ten ? decodeURIComponent(ten[1]) : 'BaoCaoSuatAn.xlsx';
            document.body.appendChild(lienKet);
            lienKet.click();
            lienKet.remove();
            URL.revokeObjectURL(duongDan);

            QS.toast('Đã xuất file Excel', 'ok');
        } catch (err) {
            QS.toast(err.message, 'loi');
        } finally {
            nutXuat.disabled = false;
            nutXuat.innerHTML = nhanCu;
        }
    }

    const nutXuat = document.getElementById('btnXuatExcel');
    nutXuat.addEventListener('click', xuatExcel);

    // Xoá nhanh mọi lượt quét mã test trong khoảng ngày đang chọn
    async function xoaNhanhTest() {
        if (!oTuNgay.value || !oDenNgay.value || oTuNgay.value > oDenNgay.value) {
            QS.toast('Vui lòng chọn khoảng ngày hợp lệ', 'nhac');
            return;
        }
        if (!confirm(`Xoá TẤT CẢ lượt quét của mã test 111…999 từ ${ngayHienThi(oTuNgay.value)} đến ${ngayHienThi(oDenNgay.value)}? Không thể khôi phục.`)) return;

        await QS.nutBan(nutXoaTest, async () => {
            try {
                const thamSo = new URLSearchParams({ fromDate: oTuNgay.value, toDate: oDenNgay.value });
                const res = await fetch(`/MG/An/BaoCaoNgay/XoaTest?${thamSo}`, { method: 'POST' });
                const ketQua = await res.json();
                if (!ketQua.ok) throw new Error(ketQua.message || 'Xoá thất bại');

                QS.toast(ketQua.message, 'ok');
                taiBaoCao(1);
            } catch (err) {
                QS.toast('Không xoá được: ' + err.message, 'loi');
            }
        });
    }

    if (nutXoaTest) nutXoaTest.addEventListener('click', xoaNhanhTest);

    // Bỏ lọc: đưa mọi bộ lọc về mặc định (hôm nay, không tích bữa/cổng, không từ khoá)
    document.getElementById('btnBoLoc').addEventListener('click', function () {
        const ngayHomNay = dinhDangNgay(new Date());
        oTuNgay.value = ngayHomNay;
        oDenNgay.value = ngayHomNay;
        ngayDangXem = ngayHomNay;
        oTim.value = '';
        clearTimeout(hanTim);
        locBua.boChon();
        locCong.boChon();
        oGopNgay.checked = false;
        oSoDong.value = '0';
        taiBaoCao(1);
        QS.toast('Đã bỏ lọc, xem lại báo cáo hôm nay', 'ok');
    });

    oSoDong.addEventListener('change', () => taiBaoCao(1));

    oGopNgay.addEventListener('change', () => taiBaoCao(1));

    oTim.addEventListener('input', function () {
        clearTimeout(hanTim);
        hanTim = setTimeout(() => taiBaoCao(1), DO_TRE_TIM);
    });

    [oTuNgay, oDenNgay].forEach(o => o.addEventListener('change', () => {
        ngayDangXem = oDenNgay.value;
        taiBaoCao(1);
    }));

    taiBaoCao(1);
})();
