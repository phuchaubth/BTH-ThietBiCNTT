# Kiểm thử API mở + khoá API + webhook (cần app demo đang chạy với NO_PROXY=127.0.0.1 — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, json, hmac, hashlib, threading, time
from http.server import BaseHTTPRequestHandler, HTTPServer
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
nhan=[]
class H(BaseHTTPRequestHandler):
    def do_POST(s):
        b=s.rfile.read(int(s.headers.get("Content-Length",0))); nhan.append((dict(s.headers), b))
        s.send_response(200); s.end_headers(); s.wfile.write(b"ok")
    def log_message(s,*a): pass
srv=HTTPServer(("127.0.0.1",8098),H); threading.Thread(target=srv.serve_forever,daemon=True).start()
def api(path, key=None, method="GET", body=None):
    h={"Content-Type":"application/json"}
    if key: h["X-Api-Key"]=key
    r=urllib.request.Request(BASE+path, data=json.dumps(body).encode() if body is not None else None, headers=h, method=method)
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read() or b"null")
    except urllib.error.HTTPError as e:
        t=e.read(); return e.code, (json.loads(t) if t.strip().startswith(b"{") else t.decode(errors="ignore"))
a=U("admin")
# TaoKhoa chuyển hướng về Index và hiển thị khoá một lần → đọc từ chính lần GET sau redirect
def tao_khoa(ten, pv, ip=""):
    p=a.get("/TichHop")[1]
    d={"ten":ten,"phamVi":pv,"ipChoPhep":ip,"__RequestVerificationToken":a.token(p)}
    st,body,loc=a.req("/TichHop/TaoKhoa", d)
    b=a.get(loc)[1]; m=re.search(r'id="khoaMoi" value="([^"]+)"', b)
    return m.group(1) if m else re.sub(r"<[^>]+>","",b)[:300]
k1=tao_khoa("HIS – báo hỏng", ["taisan.doc","yeucau.tao","congviec.doc"])
k2=tao_khoa("Chỉ đọc, giới hạn IP", ["taisan.doc"], "10.99.0.0/16")
k3=tao_khoa("Kho chỉ đọc", ["kho.doc","baocao.doc"])
check(k1.startswith("tbk_") and len(k1)>30, "tạo khoá, hiển thị một lần")
pg=a.get("/TichHop")[1]; check('id="khoaMoi"' not in pg and k1 not in pg and k1[:12] in pg, "tải lại trang chỉ còn tiền tố khoá")
st,b=api("/api/v1/taisan"); check(st==401, f"không có khoá → 401 ({b})")
st,b=api("/api/v1/taisan", "tbk_saisaisai"); check(st==401 and "không hợp lệ" in b["loi"], "khoá sai → 401")
st,b=api("/api/v1/taisan", k2); check(st==401 and "IP" in b["loi"], "khoá giới hạn IP → 401 từ 127.0.0.1")
# Cookie đăng nhập không dùng được cho API
st,body,_=a.req("/api/v1/taisan"); check(st==401, "cookie đăng nhập không thay khoá API")
st,b=api("/api/v1/taisan?pageSize=5&trangThai=DangSuDung", k1); check(st==200 and b["total"]>0 and len(b["items"])==5 and b["items"][0]["trangThai"]=="DangSuDung", f"liệt kê tài sản: total={b.get('total')}")
ma=b["items"][0]["maTaiSan"]
st,b=api(f"/api/v1/taisan/{ma}", k1); check(st==200 and b["maTaiSan"]==ma, "chi tiết tài sản theo mã")
st,b=api("/api/v1/taisan?trangThai=XYZ", k1); check(st==400, "tham số sai → 400")
st,b=api(f"/api/v1/taisan/{ma}", k1, "PATCH", {"chiSoBoDem":12345}); check(st==403, "thiếu phạm vi taisan.ghi → 403")
st,b=api("/api/v1/tonkho", k1); check(st==403, "thiếu phạm vi kho.doc → 403")
st,b=api("/api/v1/tonkho", k3); check(st==200 and len(b)>0, "tồn kho với khoá kho.doc")
st,b=api("/api/v1/baocao/kpi", k3); check(st==200 and "tong" in b, f"KPI: {b.get('tong') if isinstance(b,dict) else b}")
# Webhook
p=a.get("/TichHop/SuaWebhook")[1]
st,loc,m,_=a.post("/TichHop/SuaWebhook",{"Ten":"HIS nhận sự kiện","Url":"http://127.0.0.1:8098/hook","suKien":["phieu.tao","taisan.trangthai"],"DangHoatDong":["true","false"]},page=p)
bimat=re.search(r"Bí mật ký HMAC: ([0-9a-f]+)", m).group(1); print("webhook:", m[:60])
wid=re.search(r'/TichHop/ThuGui/(\d+)', a.get("/TichHop/Webhooks")[1]).group(1)
m=a.post(f"/TichHop/ThuGui/{wid}",{},page=a.get("/TichHop/Webhooks")[1])[2]; check("thành công" in m and nhan and nhan[-1][0].get("X-TBCNTT-Event")=="ping", "gửi thử ping")
st,b=api("/api/v1/yeucau", k1, "POST", {"maTaiSan":ma,"moTa":"Màn hình không lên (HIS báo)","uuTien":"Cao","nguoiYeuCau":"Điều dưỡng A"})
if st==409: st,b=api("/api/v1/yeucau", k1, "POST", {"maTaiSan":ma,"moTa":"Màn hình không lên (HIS báo)","uuTien":"Cao","boQuaTrung":True})
check(st==201 and b["soPhieu"].startswith("SC-"), f"tạo yêu cầu qua API: {b.get('soPhieu') if isinstance(b,dict) else b}")
so=b["soPhieu"]
st,b2=api("/api/v1/yeucau", k1, "POST", {"maTaiSan":ma,"moTa":"lặp","uuTien":"Cao"}); check(st==409 and so in b2["phieuDangMo"], "chống trùng → 409")
st,b2=api(f"/api/v1/phieusuachua/{so}", k1); check(st==200 and b2["trangThai"]=="Moi", "đọc lại phiếu")
check(so in a.get("/PhieuSuaChua")[1], "phiếu hiện trong giao diện")
nk=a.get("/NhatKy")[1]; check("api:HIS" in nk, "nhật ký thao tác ghi người tạo là khoá API")
for i in range(45):
    if any(h.get("X-TBCNTT-Event")=="phieu.tao" for h,_ in nhan): break
    time.sleep(1)
ev=[(h,b) for h,b in nhan if h.get("X-TBCNTT-Event")=="phieu.tao"]
check(len(ev)==1, "webhook phieu.tao được gửi (job nền)")
if ev:
    h,b=ev[0]; sig="sha256="+hmac.new(bimat.encode(), b, hashlib.sha256).hexdigest()
    check(h.get("X-TBCNTT-Signature")==sig, "chữ ký HMAC khớp")
    d=json.loads(b); check(d["suKien"]=="phieu.tao" and d["duLieu"]["soPhieu"]==so, "nội dung sự kiện đúng phiếu")
# Giới hạn tần suất
a.post("/ThamSo/Luu",{"giaTri[Api.GioiHanMoiPhut]":"5"},page=a.get("/ThamSo")[1])
codes=[api("/api/v1/khoaphong", k1)[0] for _ in range(8)]
check(429 in codes, f"vượt giới hạn → 429 {codes}")
a.post("/ThamSo/Luu",{"giaTri[Api.GioiHanMoiPhut]":"120"},page=a.get("/ThamSo")[1])
# Thu hồi
kid=re.findall(r'/TichHop/ThuHoi/(\d+)', a.get("/TichHop")[1])
a.post(f"/TichHop/ThuHoi/{kid[-1]}",{},page=a.get("/TichHop")[1])   # khoá tạo đầu tiên (HIS) nằm cuối danh sách
time.sleep(61)  # chờ qua cửa sổ giới hạn tần suất
st,b=api("/api/v1/khoaphong", k1); check(st==401 and "thu hồi" in b["loi"], "khoá đã thu hồi → 401")
nk=a.get("/TichHop/NhatKy?loi=true")[1]; check("/api/v1/taisan" in nk and "401" in nk, "nhật ký gọi API có lỗi")
st,doc,_=a.req("/api/openapi/v1.json"); check(st==200 and "/api/v1/yeucau" in doc and "X-Api-Key" in doc, "tài liệu OpenAPI")
check(U("nhanvien").req("/api/openapi/v1.json")[0] in (302,403), "nhân viên không xem tài liệu API")
check(a.get("/TichHop/TaiLieu")[0]==200, "trang tài liệu API (Swagger UI)")
anon=U.__new__(U); anon.cj=http.cookiejar.CookieJar(); anon.op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(anon.cj), NoRedirect())
st,_,loc=anon.req("/TaiSan"); check(st==302 and "/Account/Login" in loc, "trang giao diện vẫn bắt đăng nhập")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
