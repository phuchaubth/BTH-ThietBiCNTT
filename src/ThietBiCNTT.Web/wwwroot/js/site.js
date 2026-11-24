// site.js

// PWA: đăng ký service worker (chỉ chạy trên HTTPS hoặc localhost).
if ('serviceWorker' in navigator && window.isSecureContext) {
    window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => { }));
    // Đăng xuất → xoá các trang đã lưu ngoại tuyến (máy dùng chung).
    document.addEventListener('submit', e => {
        if (e.target.action && /\/Account\/Logout/i.test(e.target.action) && navigator.serviceWorker.controller)
            navigator.serviceWorker.controller.postMessage('xoa-cache-trang');
    });
}
