# Kiểm thử xác thực 2 lớp, bắt buộc 2FA, tài khoản AD, Zalo OA (mock), số liệu kế toán.
# App demo chạy với: ThongBao__ZaloApiBase=http://127.0.0.1:8097 ThongBao__ZaloOAuthBase=http://127.0.0.1:8097
#   ThongBao__ZaloAccessToken=OLD ThongBao__ZaloRefreshToken=R1 ThongBao__ZaloAppId=1 ThongBao__ZaloSecretKey=SECRET ; mock.py <log> đang chạy.
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sys, time, hmac, hashlib, struct, base64, json, sqlite3, datetime, os
LOG=sys.argv[1] if len(sys.argv) > 1 else "/tmp/mock.log"
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
def totp(key, t=None):
    k=base64.b32decode(key.upper().replace(" ","") + "="*((8 - len(key.replace(" ","")) % 8) % 8))
    c=struct.pack(">Q", int((t or time.time())//30)); h=hmac.new(k, c, hashlib.sha1).digest(); o=h[-1]&15
    return "%06d" % ((struct.unpack(">I", h[o:o+4])[0] & 0x7fffffff) % 1000000)
class U2(U):
    """Đăng nhập không tự xử lý bước 2 lớp."""
    def __init__(s, name, pw="Abc@12345"):
        s.name=name; s.cj=http.cookiejar.CookieJar()
        s.op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(s.cj), NoRedirect())
        r=s.get("/Account/Login"); s.kq=s.post("/Account/Login", {"userName":name,"password":pw,"remember":"false"}, page=r[1])
a=U("admin")
# 1. Bật 2FA cho ktv2
k2=U("ktv2"); p=k2.get("/TaiKhoan/XacThuc2Lop")[1]
key=re.search(r'id="khoa">([^<]+)<', p).group(1); check(len(key.replace(" ",""))>=16 and 'data:image/png;base64' in p, "trang cài đặt: mã QR + khoá thủ công")
m=k2.post("/TaiKhoan/BatXacThuc2Lop",{"ma":"000000"},page=p)[2]; check("không đúng" in m, "mã sai không bật được")
st,body,loc=k2.req("/TaiKhoan/BatXacThuc2Lop",{"ma":totp(key),"__RequestVerificationToken":k2.token(p)})
pg=k2.get(loc)[1]; m=pg; ma_kp=re.findall(r"<code>([^<]+)</code>", pg.split("Mã khôi phục")[1])[:10] if "Mã khôi phục" in pg else []
check("Đã bật" in m and len(ma_kp)==10, f"bật 2FA, nhận 10 mã khôi phục")
check("Mã khôi phục" not in k2.get("/TaiKhoan/XacThuc2Lop")[1].split("Đang bật")[0], "mã khôi phục chỉ hiện một lần")
# Đăng nhập lại: cần mã
x=U2("ktv2"); st,loc,_,_=x.kq; check(loc and loc.startswith("/Account/XacThuc2Lop"), "đăng nhập đúng mật khẩu → bước 2 lớp")
check(x.get("/")[2].startswith("/Account/Login"), "chưa qua bước 2 thì chưa vào được")
p=x.get(loc)[1]; st,body,l2=x.req("/Account/XacThuc2Lop?returnUrl=%2F", {"ma":"123456","remember":"false","__RequestVerificationToken":x.token(p)})
check(st==200 and "Mã không đúng" in html.unescape(body), "mã sai bị từ chối")
time.sleep(1); st,body,l2=x.req("/Account/XacThuc2Lop", {"ma":totp(key),"remember":"false","__RequestVerificationToken":x.token(p)})
check(st==302 and x.get("/")[0]==200, "mã đúng → vào hệ thống")
y=U2("ktv2"); p=y.get(y.kq[1])[1]
st,body,l3=y.req("/Account/XacThuc2Lop", {"ma":ma_kp[0],"laMaKhoiPhuc":"true","remember":"false","__RequestVerificationToken":y.token(p)})
check(st==302 and y.get("/")[0]==200, "đăng nhập bằng mã khôi phục")
z=U2("ktv2"); p=z.get(z.kq[1])[1]
st,body,_=z.req("/Account/XacThuc2Lop", {"ma":ma_kp[0],"laMaKhoiPhuc":"true","remember":"false","__RequestVerificationToken":z.token(p)})
check(st==200, "mã khôi phục đã dùng không dùng lại được")
# 2. Bắt buộc 2FA cho CNTT
a.post("/ThamSo/Luu",{"co[BaoMat.BatBuoc2FAQuanTri]":"true"},page=a.get("/ThamSo")[1])
k1=U("ktv1"); st,_,loc=k1.get("/PhieuSuaChua")
check(loc=="/TaiKhoan/XacThuc2Lop?batBuoc=true", "CNTT chưa bật 2FA → bị chuyển tới trang cài đặt")
check(k1.get("/TaiKhoan/XacThuc2Lop?batBuoc=true")[0]==200, "vẫn vào được trang cài đặt")
check(U("nhanvien").get("/PhieuSuaChua")[0]==200, "nhân viên không bị bắt buộc")
w=U2("ktv2"); p=w.get(w.kq[1])[1]; time.sleep(1); w.req("/Account/XacThuc2Lop", {"ma":totp(key),"remember":"false","__RequestVerificationToken":w.token(p)})
pg=w.get("/TaiKhoan/XacThuc2Lop")[1]
m=w.post("/TaiKhoan/TatXacThuc2Lop",{"ma":totp(key)},page=pg)[2]; check("bắt buộc" in m, "không tắt được 2FA khi đang bắt buộc")
st,_,loc=a.get("/ThamSo"); check(loc=="/TaiKhoan/XacThuc2Lop?batBuoc=true", "Quản trị cũng bị buộc cài 2FA (kể cả trang tham số)")
pa=a.get(loc)[1]; ka=re.search(r'id="khoa">([^<]+)<', pa).group(1); a.req("/TaiKhoan/BatXacThuc2Lop",{"ma":totp(ka),"__RequestVerificationToken":a.token(pa)})
check(a.get("/ThamSo")[0]==200, "sau khi bật 2FA, Quản trị dùng lại bình thường (phiên được làm mới)")
a.post("/ThamSo/Luu",{"co[BaoMat.BatBuoc2FAQuanTri]":"false"},page=a.get("/ThamSo")[1])
uid=re.search(r'/NguoiDung/Edit/([^"]+)"[^>]*>\s*<i class="bi bi-pencil"></i></a></td>', a.get("/NguoiDung?q=ktv2")[1]).group(1)
m=a.post(f"/NguoiDung/DatLai2FA/{uid}",{},page=a.get(f"/NguoiDung/Edit/{uid}")[1])[2]; check("Đã đặt lại" in m, "Quản trị đặt lại 2FA")
v=U2("ktv2"); check(v.kq[1]=="/", "sau khi đặt lại: đăng nhập không cần mã")
# 3. Tài khoản AD (chưa cấu hình máy chủ LDAP trong môi trường thử)
p=a.get("/NguoiDung/Create")[1]; kh=re.search(r'<option value="(\d+)">Khoa Cấp cứu', p).group(1)
m=a.post("/NguoiDung/Save",{"UserName":"bth\\\\aduser".replace("\\\\",""),"HoTen":"Người dùng AD","VaiTro":"NhanVien","KhoaPhongId":kh,"XacThucAD":["true","false"],"DangHoatDong":["true","false"]},page=p)[2]
check("Đã lưu" in m, "tạo tài khoản AD không cần mật khẩu cục bộ")
ad=U2("bthaduser", "matkhau-bat-ky"); st,loc,_,body=ad.kq
check(st==200 and "Chưa cấu hình máy chủ AD" in html.unescape(body), "AD chưa cấu hình → thông báo rõ, không đăng nhập")
ad2=U2("bthaduser", ""); check(ad2.kq[0]==200, "mật khẩu rỗng bị chặn")
# 4. Zalo OA: token cũ hết hạn → tự làm mới → gửi được
a.post("/ThamSo/Luu",{"co[ThongBao.ZaloBat]":"true"},page=a.get("/ThamSo")[1])
k1=U("nhanvien"); p=k1.get("/TaiKhoan")[1]
k1.post("/TaiKhoan/Luu",{"email":"","zaloUserId":"8800123456","nhanZalo":["true","false"],"nhanEmail":["false"],"nhanTelegram":["false"]},page=p)
print("gửi thử:", k1.post("/TaiKhoan/GuiThu",{},page=k1.get("/TaiKhoan")[1])[2])
for _ in range(25):
    log=open(LOG,encoding="utf-8").read() if os.path.exists(LOG) else ""
    if "ZALO 8800123456" in log: break
    time.sleep(2)
check("ZALO-TOKEN-SAI OLD" in log and "ZALO-REFRESH -> NEW1" in log and "ZALO 8800123456" in log, "Zalo: token cũ bị từ chối → làm mới → gửi thành công")
check(os.path.exists("src/ThietBiCNTT.Web/App_Data/zalo-token.dat") and "NEW1" not in open("src/ThietBiCNTT.Web/App_Data/zalo-token.dat").read(), "token mới lưu ra tệp (đã mã hoá)")
# 5. Số liệu kế toán
k=U("ktv1"); DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
TU,DEN="2024-01-01","2026-12-31"
p=k.get(f"/KeToan?tu={TU}&den={DEN}")[1]
n=DB.execute("select count(*) from TaiSans where (NgayMua >= ? and NgayMua <= ?) or (NgayMua is null and NgayTao >= ? and NgayTao < '2027-01-01')", (TU, DEN, TU)).fetchone()[0]
check(f'Ghi tăng tài sản <span class="badge bg-secondary ms-2">{n}</span>' in p, f"ghi tăng = {n} tài sản trong kỳ")
st,csv,_=k.req(f"/KeToan/Csv?loai=tang&tu={TU}&den={DEN}")
check(st==200 and csv.startswith("﻿Mã tài sản,") and len(csv.strip().splitlines())==n+1, "CSV có BOM, tiêu đề, đủ dòng")
st,b,_=k.req("/KeToan/Xuat"); check(st==200 and b.startswith("PK"), "Excel 3 bảng")
pg=a.get("/TichHop")[1]; st,b2,loc=a.req("/TichHop/TaoKhoa",{"ten":"Phần mềm kế toán","phamVi":["baocao.doc"],"__RequestVerificationToken":a.token(pg)})
key_api=re.search(r'id="khoaMoi" value="([^"]+)"', a.get(loc)[1]).group(1)
r=urllib.request.Request(BASE+f"/api/v1/ketoan/tanggiam?tu={TU}&den={DEN}", headers={"X-Api-Key":key_api})
j=json.loads(urllib.request.urlopen(r).read()); check(len(j["ghiTang"])==n and "thucChi" in j, "API kế toán trả JSON ghi tăng/giảm/thực chi")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
an=U.__new__(U); an.cj=http.cookiejar.CookieJar(); an.op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(an.cj), NoRedirect())
st,_,loc=an.req("/Account/DoiMatKhau"); check(st==302 and loc.startswith("/Account/Login"), "đổi mật khẩu yêu cầu đăng nhập")
check(an.req("/Account/Login")[0]==200 and an.req("/Account/AccessDenied")[0]==200, "trang đăng nhập / từ chối vẫn mở ẩn danh")
print("KẾT QUẢ (bổ sung):", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
