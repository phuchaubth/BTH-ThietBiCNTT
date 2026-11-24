// Kiểm thử PWA + giao diện điện thoại + kiểm kê ngoại tuyến (Playwright; cần app demo đang chạy — xem e2e.py)
// Chạy: NO_PROXY=127.0.0.1 node tests/e2e/pwa.js
const { chromium, devices } = require('playwright');
const BASE = 'http://127.0.0.1:5099';
let ok = true; const check = (c, m) => { console.log((c ? 'PASS ' : 'FAIL ') + m); ok = ok && c; };
async function dangNhap(ctx, u) {
  const p = await ctx.newPage(); await p.goto(BASE + '/Account/Login');
  await p.fill('[name=userName]', u); await p.fill('[name=password]', 'Abc@12345');
  await Promise.all([p.waitForNavigation(), p.press('[name=password]', 'Enter')]); return p;
}
(async () => {
  const b = await chromium.launch();
  // 1. Tệp PWA
  const req = await b.newContext().then(c => c.request);
  const mf = await req.get(BASE + '/manifest.webmanifest');
  check(mf.ok() && (await mf.json()).start_url === '/m', 'manifest: start_url /m');
  check((mf.headers()['content-type'] || '').includes('manifest+json'), 'manifest content-type: ' + mf.headers()['content-type']);
  for (const u of ['/sw.js', '/offline.html', '/icons/icon-192.png', '/icons/icon-maskable-512.png'])
    check((await req.get(BASE + u)).ok(), 'tải ' + u);

  // 2. Giao diện điện thoại theo vai trò
  const dt = devices['Pixel 7'];
  for (const [u, dau, coMuc, khongMuc] of [['nhanvien', 'Báo hỏng', 'Yêu cầu của tôi', 'Kho'], ['ktv1', 'Việc của tôi', 'Kiểm tra', 'Ngân sách'], ['lanhdao', 'Tổng quan', 'Chờ duyệt', 'Quét QR']]) {
    const ctx = await b.newContext(dt); const p = await dangNhap(ctx, u);
    await p.goto(BASE + '/m');
    const o = await p.locator('.o-didong').first().innerText();
    const menu = await p.locator('.menu-didong a').allInnerTexts();
    check(o.includes(dau), `${u}: ô đầu tiên "${dau}"`);
    check(menu.some(x => x.includes(coMuc)) && !menu.some(x => x.includes(khongMuc)) && menu.length <= 5, `${u}: menu dưới [${menu.map(x => x.trim()).join(' | ')}]`);
    check(await p.locator('.menu-didong').isVisible(), `${u}: thanh menu dưới hiển thị trên điện thoại`);
    if (u === 'ktv1') {
      await p.goto(BASE + '/m/Quet'); await p.fill('#maTay', 'pc-0001'); await Promise.all([p.waitForNavigation(), p.press('#maTay', 'Enter')]);
      check(/\/TaiSan\/Details\/\d+/.test(p.url()), 'quét/nhập mã → mở hồ sơ tài sản');
      await p.goto(BASE + '/m/Quet'); await p.check('#baoHong'); await p.fill('#maTay', 'PC-0001'); await Promise.all([p.waitForNavigation(), p.press('#maTay', 'Enter')]);
      check(/\/PhieuSuaChua\/Create\?taiSanId=\d+/.test(p.url()), 'quét để báo hỏng → form báo hỏng chọn sẵn thiết bị');
    }
    if (u === 'lanhdao') { await p.goto(BASE + '/m/ChoDuyet'); check(await p.locator('h1').innerText().then(t => t.includes('Chờ tôi xử lý')), 'trang chờ tôi xử lý'); }
    await ctx.close();
  }
  const ctxPc = await b.newContext({ viewport: { width: 1360, height: 900 } }); const pc = await dangNhap(ctxPc, 'ktv1');
  check(!(await pc.locator('.menu-didong').isVisible()), 'máy tính: không hiện thanh menu dưới'); await ctxPc.close();

  // 3. Kiểm kê ngoại tuyến
  const ctx = await b.newContext(dt); const p = await dangNhap(ctx, 'ktv1');
  await p.goto(BASE + '/KiemKe/Create'); await p.fill('[name=Ten]', 'Kiểm kê ngoại tuyến'); await Promise.all([p.waitForNavigation(), p.locator('form button[type=submit], form .btn-primary').last().click()]);
  const dot = p.url().match(/\/KiemKe\/Details\/(\d+)/)?.[1]; check(!!dot, 'mở đợt kiểm kê ' + dot);
  await p.goto(BASE + `/KiemKe/Quet/${dot}`);
  await p.evaluate(() => navigator.serviceWorker.ready); await p.reload(); // để service worker kiểm soát & lưu trang quét
  check(await p.evaluate(() => !!navigator.serviceWorker.controller), 'service worker đang kiểm soát trang');
  await p.fill('#maTay', 'PC-0002'); await p.press('#maTay', 'Enter'); await p.waitForTimeout(800);
  check((await p.locator('#ketqua').innerText()).includes('PC-0002'), 'quét khi có mạng');
  await ctx.setOffline(true);
  const lucOffline = Date.now();
  for (const ma of ['PC-0003', 'PC-0004']) { await p.fill('#maTay', ma); await p.press('#maTay', 'Enter'); await p.waitForTimeout(3200); }
  check((await p.locator('#ketqua').innerText()).includes('lưu tạm'), 'mất mạng → lưu tạm');
  check(await p.locator('#cho b').innerText() === '2', 'đếm 2 lượt chờ đồng bộ');
  await p.reload(); // tải lại khi mất mạng → trang quét lấy từ cache
  check((await p.locator('#maTay').count()) === 1 && await p.locator('#cho b').innerText() === '2', 'mất mạng vẫn mở lại được trang quét, hàng đợi còn nguyên');
  await p.goto(BASE + '/TaiSan').catch(() => { }); check((await p.content()).includes('Không kết nối được máy chủ'), 'trang khác khi mất mạng → trang ngoại tuyến');
  await p.goto(BASE + `/KiemKe/Quet/${dot}`);
  await ctx.setOffline(false);
  await p.evaluate(() => window.dispatchEvent(new Event('online')));
  await p.waitForFunction(() => document.querySelector('#cho').classList.contains('d-none'), null, { timeout: 25000 }).catch(() => { });
  check(await p.locator('#cho').evaluate(e => e.classList.contains('d-none')), 'có mạng → tự đồng bộ hết hàng đợi');
  await p.goto(BASE + `/KiemKe/Details/${dot}`);
  const html = await p.content();
  check(['PC-0002', 'PC-0003', 'PC-0004'].every(m => html.includes(m)), 'kết quả quét ngoại tuyến đã ghi vào đợt');
  console.log('offline lúc', new Date(lucOffline).toLocaleTimeString('vi-VN'));
  await ctx.close(); await b.close();
  console.log('KẾT QUẢ:', ok ? 'TẤT CẢ ĐẠT' : 'CÓ LỖI');
})();
