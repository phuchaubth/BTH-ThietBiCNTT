// Service worker — Quản lý TTB CNTT.
// • Tệp tĩnh (thư viện, CSS, JS, icon): cache-first.
// • Trang: luôn lấy từ mạng; mất mạng → trang ngoại tuyến. Riêng màn hình quét kiểm kê được lưu lại để mở được khi Wi-Fi chập chờn (FR-KK-07).
// • Không lưu dữ liệu nghiệp vụ khác vào cache (máy ở khoa thường dùng chung).
const PHIEN_BAN = 'ttb-v1';
const TINH = 'tinh-' + PHIEN_BAN, TRANG = 'trang-' + PHIEN_BAN;
const NAP_TRUOC = ['/offline.html', '/lib/bootstrap/dist/css/bootstrap.min.css', '/lib/bootstrap-icons/bootstrap-icons.min.css',
  '/lib/bootstrap/dist/js/bootstrap.bundle.min.js', '/lib/jquery/dist/jquery.min.js', '/lib/html5-qrcode/html5-qrcode.min.js', '/icons/icon-192.png'];

self.addEventListener('install', e => {
  e.waitUntil(caches.open(TINH).then(c => Promise.all(NAP_TRUOC.map(u => c.add(u).catch(() => null)))).then(() => self.skipWaiting()));
});
self.addEventListener('activate', e => {
  e.waitUntil(caches.keys().then(ks => Promise.all(ks.filter(k => !k.endsWith(PHIEN_BAN)).map(k => caches.delete(k)))).then(() => self.clients.claim()));
});

const laTinh = p => /^\/(lib|css|js|icons)\//.test(p) || p === '/offline.html' || p === '/favicon.ico';
const laTrangQuet = p => /^\/KiemKe\/Quet\/\d+$/i.test(p);

self.addEventListener('fetch', e => {
  const r = e.request, u = new URL(r.url);
  if (r.method !== 'GET' || u.origin !== location.origin || u.pathname.startsWith('/api/')) return;
  if (laTinh(u.pathname)) {
    e.respondWith(caches.match(r, { ignoreSearch: true }).then(c => c || fetch(r).then(res => {
      if (res.ok) { const sao = res.clone(); caches.open(TINH).then(x => x.put(r, sao)); }
      return res;
    })));
    return;
  }
  if (r.mode === 'navigate') {
    e.respondWith(fetch(r).then(res => {
      if (laTrangQuet(u.pathname) && res.ok && !res.redirected) { const sao = res.clone(); caches.open(TRANG).then(x => x.put(u.pathname, sao)); }
      return res;
    }).catch(async () => (laTrangQuet(u.pathname) && await caches.match(u.pathname)) || caches.match('/offline.html')));
  }
});

// Đăng xuất: xoá trang đã lưu
self.addEventListener('message', e => { if (e.data === 'xoa-cache-trang') caches.delete(TRANG); });
