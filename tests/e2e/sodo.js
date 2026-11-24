// Kiểm thử sơ đồ mặt bằng (Playwright; cần app demo đang chạy — xem e2e.py). Chạy: NO_PROXY=127.0.0.1 node tests/e2e/sodo.js
const { chromium } = require('playwright');
const path = require('path');
const BASE = 'http://127.0.0.1:5099';
let ok = true; const check = (c, m) => { console.log((c ? 'PASS ' : 'FAIL ') + m); ok = ok && c; };
async function dangNhap(b, u, vp = { width: 1360, height: 900 }) {
  const ctx = await b.newContext({ viewport: vp }); const p = await ctx.newPage(); await p.goto(BASE + '/Account/Login');
  await p.fill('[name=userName]', u); await p.fill('[name=password]', 'Abc@12345');
  await Promise.all([p.waitForNavigation(), p.press('[name=password]', 'Enter')]); return p;
}
async function taoSoDo(p, ten, khoa) {
  await p.goto(BASE + '/SoDo/Sua'); await p.fill('[name=Ten]', ten);
  if (khoa) await p.selectOption('[name=KhoaPhongId]', { label: khoa });
  await p.setInputFiles('#anh', path.join(__dirname, 'matbang-mau.png'));
  await Promise.all([p.waitForNavigation(), p.click('form button.btn-primary')]);
  return p.url().match(/\/SoDo\/Xem\/(\d+)/)?.[1];
}
(async () => {
  const b = await chromium.launch();
  const k = await dangNhap(b, 'ktv1');
  await k.goto(BASE + '/SoDo/Sua'); await k.fill('[name=Ten]', 'Thiếu ảnh'); await k.click('form button.btn-primary');
  check((await k.content()).includes('Chọn ảnh sơ đồ'), 'bắt buộc có ảnh khi tạo');
  await k.goto(BASE + '/SoDo/Sua'); await k.fill('[name=Ten]', 'Sai định dạng');
  await k.setInputFiles('#anh', { name: 'gia.png', mimeType: 'image/png', buffer: Buffer.from('khong phai anh') });
  await k.click('form button.btn-primary');
  check((await k.content()).includes('không đúng định dạng'), 'chặn tệp giả ảnh (kiểm tra magic bytes)');
  const id = await taoSoDo(k, 'Tầng 1 – Cấp cứu', 'Khoa Cấp cứu'); check(!!id, 'tạo sơ đồ khoa Cấp cứu #' + id);
  const idKhac = await taoSoDo(k, 'Tầng 3 – Khám bệnh', 'Khoa Khám bệnh');
  // Đặt thiết bị
  await k.goto(BASE + `/SoDo/Xem/${id}`);
  await k.click('#btnSua');
  const img = await k.locator('#anhSoDo').boundingBox();
  const dat = async (ma, fx, fy) => { await k.click(`#listChua [data-ma="${ma}"]`); await k.mouse.click(img.x + img.width * fx, img.y + img.height * fy); };
  const chua = await k.locator('#listChua [data-ma]').evaluateAll(xs => xs.map(x => x.dataset.ma));
  check(chua.includes('PC-0001') && chua.every(m => m), `danh sách chưa đặt chỉ gồm tài sản khoa (${chua.length})`);
  await dat('PC-0001', 0.15, 0.2); await dat('PC-0002', 0.6, 0.2); await dat('PC-0003', 0.85, 0.85);
  check(await k.locator('.diem').count() === 3, 'đặt 3 thiết bị');
  // Kéo PC-0001 sang Quầy tiếp đón
  const d1 = k.locator('.diem[data-id]').first(); const bb = await d1.boundingBox();
  await k.mouse.move(bb.x + bb.width / 2, bb.y + bb.height / 2); await k.mouse.down();
  await k.mouse.move(img.x + img.width * 0.2, img.y + img.height * 0.8, { steps: 8 }); await k.mouse.up();
  // Gỡ PC-0003
  await k.locator('.diem').nth(2).locator('.xoa').click();
  check(await k.locator('.diem').count() === 2, 'gỡ một thiết bị');
  await Promise.all([k.waitForNavigation(), k.click('#btnLuu')]);
  const diem = await (await k.request.get(BASE + `/SoDo/Diem/${id}`)).json();
  const p1 = diem.find(d => d.ma === 'PC-0001');
  check(diem.length === 2 && p1 && Math.abs(p1.x - 20) < 1.5 && Math.abs(p1.y - 80) < 1.5, `lưu vị trí sau khi kéo: PC-0001 (${p1?.x}, ${p1?.y})`);
  const p2 = diem.find(d => d.ma === 'PC-0002');
  console.log('   PC-0002:', p2.trangThai, 'phiếu mở', p2.phieuMo);
  check(await k.locator(`.diem[data-id="${p2.taiSanId}"]`).evaluate(e => e.className).then(c => c.includes('bg-' + p2.mauTrangThai)), 'màu theo trạng thái tài sản');
  const coPhieu = diem.filter(d => d.phieuMo > 0);
  check(coPhieu.every(d => true) && (await k.locator('.diem.coPhieu').count()) === coPhieu.length, `thiết bị có phiếu mở nhấp nháy (${coPhieu.length})`);
  await k.locator('.diem').first().click();
  check((await k.locator('#thongTin').innerText()).includes('Hồ sơ'), 'chạm biểu tượng → thông tin + liên kết');
  await k.fill('#loc', 'PC-0002'); check(await k.locator('.diem.mo').count() === 1, 'tìm trên sơ đồ làm mờ thiết bị khác');
  // Toạ độ ngoài ảnh bị chặn
  const r = await k.evaluate(async id => { const fd = new FormData(); fd.append('__RequestVerificationToken', document.querySelector('input[name=__RequestVerificationToken]').value);
    fd.append('diem[0].TaiSanId', '1'); fd.append('diem[0].X', '150'); fd.append('diem[0].Y', '10');
    return (await fetch('/SoDo/LuuDiem/' + id, { method: 'POST', body: fd })).json(); }, id);
  check(r.ok === false && r.loi.includes('0–100'), 'toạ độ ngoài ảnh bị từ chối');
  // Hồ sơ tài sản có liên kết sơ đồ
  await k.goto(BASE + `/TaiSan/Details/${p1.taiSanId}`);
  check(await k.locator(`a[href*="/SoDo/Xem/${id}"]`).count() === 1, 'hồ sơ tài sản có nút xem trên sơ đồ');
  // Phạm vi khoa
  const nv = await dangNhap(b, 'nhanvien', { width: 412, height: 900 });
  await nv.goto(BASE + '/SoDo'); const ds = await nv.content();
  check(ds.includes('Tầng 1 – Cấp cứu') && !ds.includes('Tầng 3 – Khám bệnh'), 'nhân viên chỉ thấy sơ đồ của khoa mình');
  check((await nv.goto(BASE + `/SoDo/Xem/${idKhac}`)).status() === 404, 'mở sơ đồ khoa khác → 404');
  await nv.goto(BASE + `/SoDo/Xem/${id}`);
  check(await nv.locator('#btnSua').count() === 0 && await nv.locator('.diem').count() === 2, 'nhân viên xem được, không sửa được');
  const anhKhac = await k.goto(BASE + `/SoDo/Xem/${idKhac}`).then(() => k.locator('#anhSoDo').getAttribute('src'));
  check((await nv.goto(BASE + anhKhac)).status() === 404, 'không xem được ảnh sơ đồ khoa khác');
  await nv.goto(BASE + `/SoDo/Xem/${id}`); await nv.screenshot({ path: process.env.ANH || '/dev/null' }).catch(() => { });
  await b.close();
  console.log('KẾT QUẢ:', ok ? 'TẤT CẢ ĐẠT' : 'CÓ LỖI');
})();
