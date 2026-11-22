exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, uuid, struct, zlib
def png():
    raw=b"\x00\xff\x00\x00"; ihdr=struct.pack(">IIBBBBB",1,1,8,2,0,0,0)
    ch=lambda t,d: struct.pack(">I",len(d))+t+d+struct.pack(">I",zlib.crc32(t+d)&0xffffffff)
    return b"\x89PNG\r\n\x1a\n"+ch(b"IHDR",ihdr)+ch(b"IDAT",zlib.compress(raw))+ch(b"IEND",b"")
def multipart(user, path, fields, files, page):
    b="----"+uuid.uuid4().hex; parts=[]
    for n,v in [("__RequestVerificationToken",user.token(page))]+fields:
        parts.append(f'--{b}\r\nContent-Disposition: form-data; name="{n}"\r\n\r\n{v}\r\n'.encode())
    for n,fn,ct,data in files:
        parts.append(f'--{b}\r\nContent-Disposition: form-data; name="{n}"; filename="{fn}"\r\nContent-Type: {ct}\r\n\r\n'.encode()+data+b"\r\n")
    parts.append(f"--{b}--\r\n".encode())
    try: r=user.op.open(urllib.request.Request(BASE+path, data=b"".join(parts), headers={"Content-Type":"multipart/form-data; boundary="+b})); return r.status, norm(r.headers.get("Location"))
    except urllib.error.HTTPError as e: return e.code, norm(e.headers.get("Location"))
def msg(u, loc):
    b=u.get(loc)[1]; m=re.search(r'alert-(success|danger)[^>]*>(.*?)<button', b, re.S)
    return (m.group(1)+": "+re.sub(r"\s+"," ",re.sub(r"<[^>]+>","",m.group(2))).strip()) if m else ""
k=U("ktv1")
idx=k.get("/KiemTra")[1]
viec=re.findall(r'/KiemTra/Ghi\?mauId=(\d+)(?:&amp;taiSanId=(\d+))?', idx)
print("việc đến hạn:", len(viec))
maus=dict(re.findall(r'<td class="fw-semibold">([^<]+)<div', k.get("/KiemTra/Mau")[1])) if False else None
mau_page=k.get("/KiemTra/Mau")[1]
ids=re.findall(r'/KiemTra/MauForm/(\d+)', mau_page)
def mau_id(ten): 
    i=mau_page.index(ten); return re.search(r'/KiemTra/MauForm/(\d+)', mau_page[i:]).group(1)
phong=mau_id("phòng máy chủ"); ups=mau_id("UPS hằng tuần"); pc_cc=mau_id("đầu ca")
g=k.get(f"/KiemTra/Ghi?mauId={phong}")[1]
mucs=re.findall(r'name="giaTri\[(\d+)\]"', g); mucs=list(dict.fromkeys(mucs))
vals=["30","50","true","true","true","true",""]
st,loc=multipart(k, "/KiemTra/Ghi", [("mauId",phong)]+[(f"giaTri[{m}]",v) for m,v in zip(mucs,vals)]+[("ghiChu","Điều hoà kêu to")], [("anh","phong.png","image/png",png())], g)
print("1 phòng máy chủ 30°C:", msg(k, loc)); lan1=loc
print("   có ảnh:", "/Tep/Xem/" in k.get(lan1)[1])
st,loc=multipart(k, "/KiemTra/Ghi", [("mauId",phong)]+[(f"giaTri[{m}]","") for m in mucs], [], g); print("2 thiếu mục bắt buộc:", msg(k, loc)[:70])
g=k.get(f"/KiemTra/Ghi?mauId={ups}")[1]
tsups=re.findall(r'<option value="(\d+)">(UPS-[^<]+)', g)[0]
mucs=list(dict.fromkeys(re.findall(r'name="giaTri\[(\d+)\]"', g)))
st,loc=multipart(k, "/KiemTra/Ghi", [("mauId",ups),("taiSanId",tsups[0])]+[(f"giaTri[{m}]",v) for m,v in zip(mucs,["5","40","true"])], [], g)
m1=msg(k,loc); print("3 UPS lưu điện 5 phút:", m1[:110]); p1=re.search(r'/PhieuSuaChua/Details/(\d+)', k.get(loc)[1])
st,loc=multipart(k, "/KiemTra/Ghi", [("mauId",ups),("taiSanId",tsups[0])]+[(f"giaTri[{m}]",v) for m,v in zip(mucs,["4","90","false"])], [], g)
p2=re.search(r'/PhieuSuaChua/Details/(\d+)', k.get(loc)[1]); print("4 lần 2 gắn cùng phiếu:", p1.group(1)==p2.group(1), "| phiếu", p1.group(1))
ph=k.get(f"/PhieuSuaChua/Details/{p1.group(1)}")[1]; print("   phiếu có mô tả kiểm tra + bình luận lần 2:", "Kiểm tra bất thường" in ph, "Bất thường: Thời gian lưu điện" in ph)
nv=U("nhanvien")
print("5 nhân viên CC mở phiếu đầu ca:", nv.get(f"/KiemTra/Ghi?mauId={pc_cc}")[0], "| phiếu UPS:", nv.get(f"/KiemTra/Ghi?mauId={ups}")[2] or nv.get(f"/KiemTra/Ghi?mauId={ups}")[0])
g=nv.get(f"/KiemTra/Ghi?mauId={pc_cc}")[1]; tspc=re.findall(r'<option value="(\d+)">', g)[0]; mucs=list(dict.fromkeys(re.findall(r'name="giaTri\[(\d+)\]"', g)))
st,loc=multipart(nv, "/KiemTra/Ghi", [("mauId",pc_cc),("taiSanId",tspc)]+[(f"giaTri[{m}]","true") for m in mucs], [], g); print("   nhân viên ghi đạt:", msg(nv, loc))
# tệp: ảnh giả, ảnh thật trên phiếu, quyền xem
d=k.get(f"/PhieuSuaChua/Details/{p1.group(1)}")[1]
st,loc=multipart(k, "/Tep/TaiLen", [("doiTuong","PhieuSuaChua"),("doiTuongId",p1.group(1)),("quayLai",f"/PhieuSuaChua/Details/{p1.group(1)}")], [("files","gia.png","image/png",b"<script>alert(1)</script>")], d)
print("6 ảnh giả mạo:", msg(k, loc))
st,loc=multipart(k, "/Tep/TaiLen", [("doiTuong","PhieuSuaChua"),("doiTuongId",p1.group(1)),("quayLai",f"/PhieuSuaChua/Details/{p1.group(1)}")], [("files","that.png","image/png",png())], d)
print("7 ảnh thật:", msg(k, loc))
tid=re.findall(r'/Tep/Xem/(\d+)', k.get(f"/PhieuSuaChua/Details/{p1.group(1)}")[1])[0]
r=k.op.open(urllib.request.Request(BASE+f"/Tep/Xem/{tid}")); print("   xem tệp:", r.status, r.headers["Content-Type"], r.headers.get("X-Content-Type-Options"))
khoa_phieu = re.search(r'· (Khoa[^<·]*|Phòng[^<·]*)</div>', d)
print("   nhân viên CC xem tệp phiếu khoa khác:", nv.req(f"/Tep/Xem/{tid}")[0], "(phiếu thuộc", (khoa_phieu.group(1) if khoa_phieu else "?").strip(), ")")
print("8 báo hỏng kèm ảnh:", end=" ")
cp=nv.get("/PhieuSuaChua/Create")[1]; ts_nv=re.findall(r'<option value="(\d+)">', cp)[1]
st,loc=multipart(nv, "/PhieuSuaChua/Create", [("TaiSanId",ts_nv),("MoTaSuCo","Màn hình nháy"),("UuTien","TrungBinh"),("XacNhanTrung","true")], [("anh","loi.png","image/png",png())], cp)
print(msg(nv, loc)[:60], "| có ảnh:", "/Tep/Xem/" in nv.get(loc)[1])
