# Kiểm thử end-to-end trên bản demo (SQLite, dữ liệu mẫu mới).
# Chạy: cd src/ThietBiCNTT.Web && rm -f thietbicntt-demo.db* && ASPNETCORE_ENVIRONMENT=Demo ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --no-launch-profile
#       rồi: python3 tests/e2e/e2e.py
import urllib.request, urllib.parse, http.cookiejar, re, sys, html
BASE="http://127.0.0.1:5099"
def norm(l):
    return l[len(BASE):] if l and l.startswith(BASE) else l
class U:
    def __init__(s, name):
        s.name=name; s.cj=http.cookiejar.CookieJar()
        s.op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(s.cj), NoRedirect())
        r=s.get("/Account/Login"); s.post("/Account/Login", {"userName":name,"password":"Abc@12345"}, page=r[1])
    def req(s, path, data=None):
        try:
            r=s.op.open(urllib.request.Request(BASE+path, data=urllib.parse.urlencode(data, doseq=True).encode() if data is not None else None))
            return r.status, r.read().decode(), norm(r.headers.get("Location"))
        except urllib.error.HTTPError as e:
            return e.code, e.read().decode(errors="ignore"), norm(e.headers.get("Location"))
    def get(s, path):
        st,b,l=s.req(path); return st, html.unescape(b), l
    def token(s, page): 
        m=re.search(r'name="__RequestVerificationToken" type="hidden" value="([^"]+)"', page); return m.group(1) if m else ""
    def post(s, path, data, page=None):
        page = page or s.get("/")[1]
        d=dict(data); d["__RequestVerificationToken"]=s.token(page)
        st, body, loc = s.req(path, d)
        msg=""
        if st==302 and loc and not loc.startswith("/Account"):
            b2=s.get(loc)[1]
            m=re.search(r'alert-(success|danger)[^>]*>(.*?)<button', b2, re.S)
            msg = (m.group(1)+": "+html.unescape(re.sub(r"<[^>]+>","",m.group(2))).strip()) if m else ""
        return st, loc, msg, body
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(s,*a,**k): return None
ok=True
def check(cond, label):
    global ok
    print(("PASS " if cond else "FAIL ")+label)
    ok = ok and cond

# 1. Các trang theo vai trò
pages=["/","/TaiSan","/TaiSan/Details/1","/TaiSan/Create","/PhieuSuaChua","/PhieuSuaChua/Kanban","/PhieuSuaChua/Create","/PhieuSuaChua/Details/1","/PhieuSuaChua/InPhieu/1",
       "/KeHoachBaoTri","/KeHoachBaoTri/Create","/VatTu","/DieuChuyen","/DieuChuyen/Create","/ThanhLy","/ThanhLy/Create","/ThanhLy/OCung","/KhoaPhong","/LoaiThietBi","/NhaCungCap","/ThamSo","/NhatKy","/TaiSan/InTem?ids=1","/TaiSan/Export"]
expect={ # 200 = được vào; 302 = bị chặn (AccessDenied); 404 = ngoài phạm vi khoa
 "admin":   {p:200 for p in pages},
 "ktv1":    {**{p:200 for p in pages}, "/ThamSo":302, "/NhatKy":302},
 "lanhdao": {**{p:200 for p in pages}, "/TaiSan/Create":302,"/KeHoachBaoTri/Create":302,"/VatTu":302,"/ThanhLy/Create":302,"/ThanhLy/OCung":302,"/KhoaPhong":302,"/LoaiThietBi":302,"/NhaCungCap":302,"/ThamSo":302,"/NhatKy":302},
 "nhanvien":{**{p:200 for p in pages}, "/TaiSan/Create":302,"/KeHoachBaoTri":302,"/KeHoachBaoTri/Create":302,"/VatTu":302,"/ThanhLy":302,"/ThanhLy/Create":302,"/ThanhLy/OCung":302,"/KhoaPhong":302,"/LoaiThietBi":302,"/NhaCungCap":302,"/ThamSo":302,"/NhatKy":302},
}
users={n:U(n) for n in ["admin","ktv1","ktv2","lanhdao","nhanvien","truongkhoacc"]}
# nhanvien thuộc Cấp cứu: tìm 1 tài sản ngoài khoa để kiểm tra phạm vi
for n,exp in expect.items():
    bad=[]
    for p in pages:
        st=users[n].get(p)[0]
        e=exp[p]
        if n=="nhanvien" and p in ("/TaiSan/Details/1","/PhieuSuaChua/Details/1","/PhieuSuaChua/InPhieu/1"): continue
        if st!=e: bad.append(f"{p}={st}(cần {e})")
    check(not bad, f"Phân quyền trang [{n}] " + ", ".join(bad))

# tìm tài sản thuộc Cấp cứu từ danh sách của nhanvien
body=users["nhanvien"].get("/TaiSan")[1]
ids=re.findall(r'/TaiSan/Details/(\d+)', body)
check(len(ids)>0, f"Nhân viên CC thấy {len(ids)} tài sản của khoa mình")
all_ids=set(re.findall(r'/TaiSan/Details/(\d+)', users["admin"].get("/TaiSan?page=1")[1]))
ngoai=[i for i in all_ids if i not in ids]
if ngoai: check(users["nhanvien"].get(f"/TaiSan/Details/{ngoai[0]}")[0]==404, "Nhân viên không xem được tài sản khoa khác")
ts=ids[0]
d=users["nhanvien"].get("/")[1]
check("Khoa/phòng của bạn" in d, "Dashboard nhân viên chỉ hiện số liệu khoa")

# 2. Luồng sửa chữa
nv=users["nhanvien"]
st,loc,msg,_=nv.post("/PhieuSuaChua/Create",{"TaiSanId":ts,"MoTaSuCo":"E2E: không vào được HIS","UuTien":"KhanCap","NguoiYeuCau":"ĐD Hà"})
if loc is None or "Details" not in loc:  # có thể vướng cảnh báo trùng
    st,loc,msg,_=nv.post("/PhieuSuaChua/Create",{"TaiSanId":ts,"MoTaSuCo":"E2E: không vào được HIS","UuTien":"KhanCap","XacNhanTrung":"true"})
check(loc and "Details" in loc, f"Nhân viên báo hỏng → {msg}")
pid=loc.rsplit("/",1)[1]
st,loc2,msg2,body=nv.post("/PhieuSuaChua/Create",{"TaiSanId":ts,"MoTaSuCo":"E2E lần 2","UuTien":"Cao"})
check(st==200 and "đang có yêu cầu chưa xử lý xong" in body, "Báo trùng thiết bị → hiện cảnh báo trùng, chưa tạo phiếu")
k=users["ktv1"]
_,_,m,_=k.post(f"/PhieuSuaChua/TiepNhan/{pid}",{"kyThuatVien":"ktv1","hinhThuc":"TuSua"}); check(m.startswith("success"), "KTV tiếp nhận: "+m)
_,_,m,_=nv.post(f"/PhieuSuaChua/TiepNhan/{pid}",{"kyThuatVien":"ktv1","hinhThuc":"TuSua"}); check(not m.startswith("success"), "Nhân viên không được tiếp nhận")
_,_,m,_=k.post(f"/PhieuSuaChua/TamDung/{pid}",{"lyDo":"ChoLinhKien"}); check("tạm dừng" in m, "Chờ linh kiện → "+m)
b=k.get(f"/PhieuSuaChua/Details/{pid}")[1]; check("tạm dừng SLA" in b, "Chi tiết hiển thị 'tạm dừng SLA'")
_,_,m,_=k.post(f"/PhieuSuaChua/BatDau/{pid}",{}); check("Hạn SLA mới" in m, "Tiếp tục xử lý → "+m)
_,_,m,_=k.post(f"/PhieuSuaChua/ThemVatTu/{pid}",{"vatTuId":"1","soLuong":"1"}); check(m.startswith("success"), "Xuất kho vật tư: "+m)
_,_,m,_=k.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"nguyenNhan":"Lỗi driver","bienPhap":"Cài lại driver","gioCong":"0.5","chiPhiThueNgoai":"0"}); check(m.startswith("success"), "Hoàn thành: "+m)
_,_,m,_=k.post(f"/PhieuSuaChua/NghiemThu/{pid}",{"danhGia":"5"}); check("Chỉ người báo hỏng" in m, "KTV không tự nghiệm thu: "+m)
_,_,m,_=users["ktv2"].post(f"/PhieuSuaChua/NghiemThu/{pid}",{"danhGia":"5"}); check(not m.startswith("success"), "KTV khác không nghiệm thu được")
_,_,m,_=nv.post(f"/PhieuSuaChua/NghiemThu/{pid}",{"danhGia":"5","yKien":"Tốt"}); check(m.startswith("success"), "Người báo nghiệm thu: "+m)

# 3. Điều chuyển (CC → Nội) — người lập: nhanvien; cấp 1: truongkhoacc; cấp 2: ktv1
khoa_noi = re.search(r'<option value="(\d+)">Khoa Nội tổng hợp</option>', nv.get("/DieuChuyen/Create")[1]).group(1)
page=nv.get("/DieuChuyen/Create")[1]
cands=re.findall(r'name="taiSanIds" value="(\d+)" data-gia="(\d+)"', page)
cands=[c for c in cands if c[0]!=ts]
tuKhoa=re.search(r'name="TuKhoaPhongId" value="(\d+)"', page)
tuKhoa = tuKhoa.group(1) if tuKhoa else re.search(r'<option selected="selected" value="(\d+)"', page).group(1)
small=[c for c in cands if int(c[1])<30_000_000][:1]
st,loc,m,body=nv.post("/DieuChuyen/Create",{"TuKhoaPhongId":tuKhoa,"DenKhoaPhongId":khoa_noi,"LyDo":"E2E điều chuyển","taiSanIds":[small[0][0]],"guiDuyet":"true"},page=page)
check(loc and "Details" in loc, "Lập phiếu điều chuyển: "+m+(re.sub(r"<[^>]+>"," ",body)[:0] if loc else re.findall(r'validation-summary-errors.*?</div>',body,re.S)[:1].__str__()))
did=loc.rsplit("/",1)[1]
_,_,m,_=nv.post(f"/DieuChuyen/Duyet/{did}",{"dongY":"true"}); check(not m.startswith("success"), "Người lập không tự duyệt: "+m)
_,_,m,_=k.post(f"/DieuChuyen/Duyet/{did}",{"dongY":"true"}); check(not m.startswith("success"), "KTV không duyệt thay cấp Trưởng khoa: "+m)
_,_,m,_=users["truongkhoacc"].post(f"/DieuChuyen/Duyet/{did}",{"dongY":"true"}); check("Chờ: Phòng CNTT" in m, "Trưởng khoa duyệt cấp 1: "+m)
_,_,m,_=k.post(f"/DieuChuyen/Duyet/{did}",{"dongY":"true"}); check("cấp cuối" in m, "CNTT duyệt cấp 2 (dưới ngưỡng, 2 cấp): "+m)
_,_,m,_=k.post(f"/DieuChuyen/BanGiao/{did}",{}); check(m.startswith("success"), "Bàn giao: "+m)
check(users["admin"].get(f"/DieuChuyen/InBienBan/{did}")[0]==200, "In biên bản bàn giao")

# 4. Thanh lý (ktv1 lập, có PC)
a=users["admin"]
page=k.get("/ThanhLy/Create")[1]
pcs=re.findall(r'name="taiSanIds" value="(\d+)" id="t\d+" />\s*</td>\s*<td><label for="t\d+" class="fw-semibold">PC-', page)
st,loc,m,_=k.post("/ThanhLy/Create",{"LyDo":"E2E hỏng không sửa được","taiSanIds":[pcs[0]]},page=page)
check(loc and "Details" in loc, "Lập đề xuất thanh lý: "+m); tid=loc.rsplit("/",1)[1]
_,_,m,_=k.post(f"/ThanhLy/GuiHoiDong/{tid}",{}); check(m.startswith("success"), "Gửi hội đồng")
_,_,m,_=a.post(f"/ThanhLy/DanhGia/{tid}",{"hoiDong":"PGĐ, TP CNTT","ketLuan":"Đồng ý","dongY":"true"}); check(m.startswith("success"), "Hội đồng đánh giá")
_,_,m,_=k.post(f"/ThanhLy/PheDuyet/{tid}",{"soQuyetDinh":"1/QĐ","ngayQuyetDinh":"2026-11-21","hinhThuc":"BanTrucTiep"}); check(_ is None or True, "")
st,_,_,_=k.post(f"/ThanhLy/PheDuyet/{tid}",{"soQuyetDinh":"1/QĐ","ngayQuyetDinh":"2026-11-21","hinhThuc":"BanTrucTiep"}); check(st==302, "KTV không ghi nhận quyết định (chuyển AccessDenied)")
_,_,m,_=users["lanhdao"].post(f"/ThanhLy/PheDuyet/{tid}",{"soQuyetDinh":"215/QĐ-BV","ngayQuyetDinh":"2026-11-21","hinhThuc":"BanTrucTiep"}); check(m.startswith("success"), "Lãnh đạo ghi nhận quyết định: "+m)
_,_,m,_=k.post(f"/ThanhLy/HoanTat/{tid}",{"giaTriThuHoi":"100000"}); check("Chưa ghi nhận tháo ổ cứng" in m, "Chặn hoàn tất khi chưa tháo ổ: "+m)
ct=re.search(r'name="chiTietId" value="(\d+)"', k.get(f"/ThanhLy/Details/{tid}")[1]).group(1)
_,_,m,_=k.post(f"/ThanhLy/ThaoOCung/{tid}",{"chiTietId":ct,"serial":"WD-E2E-001","noiLuu":"Tủ 2 kho CNTT"}); check(m.startswith("success"), "Ghi nhận tháo ổ: "+m)
_,_,m,_=k.post(f"/ThanhLy/HoanTat/{tid}",{"giaTriThuHoi":"100000"}); check(m.startswith("success"), "Hoàn tất & ghi giảm: "+m)
check("WD-E2E-001" in k.get("/ThanhLy/OCung")[1], "Ổ cứng xuất hiện trong danh sách thu hồi")
_,_,m,_=k.post("/ThanhLy/HuyOCung",{"chiTietIds":[ct],"soBienBan":"BB-01"}); check(m.startswith("success"), "Huỷ ổ cứng: "+m)
check(users["admin"].get(f"/ThanhLy/InBienBan/{tid}")[0]==200, "In biên bản kiểm định")

# 5. Tham số tác động ngay
_,_,m,_=a.post("/ThamSo/Luu",{"nhom":"Điều chuyển","giaTri[DieuChuyen.NguongDuyetLanhDao]":"1000"}); check(m.startswith("success"), "Hạ ngưỡng duyệt Lãnh đạo xuống 1.000đ")
page=nv.get("/DieuChuyen/Create")[1]
c2=[c for c in re.findall(r'name="taiSanIds" value="(\d+)" data-gia="(\d+)"', page) if c[0] not in (ts, small[0][0])]
st,loc,m,_=nv.post("/DieuChuyen/Create",{"TuKhoaPhongId":tuKhoa,"DenKhoaPhongId":khoa_noi,"LyDo":"E2E 3 cấp","taiSanIds":[c2[0][0]],"guiDuyet":"true"},page=page)
check("3 cấp" in m, "Phiếu mới cần 3 cấp theo tham số mới: "+m)
a.post("/ThamSo/KhoiPhuc",{"ma":"DieuChuyen.NguongDuyetLanhDao","nhom":"Điều chuyển"})
print("ALL OK" if ok else "SOME FAILED")
