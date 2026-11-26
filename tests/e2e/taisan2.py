# Kiểm thử trường tuỳ chỉnh, mã thông minh, hồ sơ vận hành (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, io, uuid, sqlite3, datetime, openpyxl, warnings
warnings.filterwarnings("ignore")
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
k=U("ktv1"); a=U("admin"); nam=datetime.date.today().year
pc=DB.execute("select Id from LoaiThietBis where Ma='PC'").fetchone()[0]; ups=DB.execute("select Id from LoaiThietBis where Ma='UPS'").fetchone()[0]
cc=DB.execute("select Id from KhoaPhongs where Ma='CC'").fetchone()[0]
check(all(x in k.get("/TruongTuyChinh")[1] for x in ["SoPhong","CongSuatVA","KieuUPS"]), "danh sách trường bổ sung mẫu")
# 1. Thêm trường bắt buộc cho PC
p=k.get("/TruongTuyChinh/Sua")[1]
st,loc,m,body=k.post("/TruongTuyChinh/Sua",{"Ten":"Phân loại bảo mật","Ma":"PhanLoaiBaoMat","Kieu":"DanhSach","LuaChon":"","LoaiThietBiId":pc,"ThuTu":"5","BatBuoc":["true","false"],"DangDung":["true","false"]},page=p)
check(st==200 and "ít nhất một lựa chọn" in html.unescape(body), "kiểu danh sách phải có lựa chọn")
m=k.post("/TruongTuyChinh/Sua",{"Ten":"Phân loại bảo mật","Ma":"PhanLoaiBaoMat","Kieu":"DanhSach","LuaChon":"Thường; Nhạy cảm","LoaiThietBiId":pc,"ThuTu":"5","BatBuoc":["true","false"],"DangDung":["true","false"]},page=p)[2]
check("Đã lưu" in m, "tạo trường bắt buộc cho loại PC")
st,_,_,body=k.post("/TruongTuyChinh/Sua",{"Ten":"Trùng","Ma":"SoPhong","Kieu":"VanBan","DangDung":["true","false"]},page=p); check(st==200 and "đã tồn tại" in html.unescape(body), "mã trường trùng bị chặn")

ids={m_:i_ for i_,m_ in DB.execute("select Id, Ma from TruongTuyChinhs")}
# 2. Mã thông minh
st,b,_=k.req(f"/TaiSan/XemTruocMa?loai={pc}&khoa={cc}"); prev=__import__("json").loads(b)
check(prev["ma"]==f"PC-CC-{nam}-00001", f"xem trước mã: {prev}")
p=k.get("/TaiSan/Create")[1]
def tao(extra):
    d={"TaiSan.MaTaiSan":"","TaiSan.Ten":"PC thử mã","TaiSan.LoaiThietBiId":pc,"TaiSan.KhoaPhongId":cc,"TaiSan.TrangThai":"DangSuDung","TaiSan.NguonVon":"NganSach","TaiSan.NguyenGia":"15000000"}
    d.update(extra); return k.post("/TaiSan/Save", d, page=p)
st,loc,m,body=tao({}); check(st==200 and "Phân loại bảo mật" in html.unescape(body) and "bắt buộc" in html.unescape(body), "thiếu trường bắt buộc → báo lỗi")
st,loc,m,_=tao({f"TuyChinh[{ids['PhanLoaiBaoMat']}]":"Nhạy cảm", f"TuyChinh[{ids['SoPhong']}]":"P.101"}); print("tạo:", m)
check(f"PC-CC-{nam}-00001" in m, "mã tự sinh theo mẫu")
id1=loc.rsplit("/",1)[1]
st,loc2,m,_=tao({f"TuyChinh[{ids['PhanLoaiBaoMat']}]":"Thường"}); check(f"PC-CC-{nam}-00002" in m, "mã tiếp theo tăng dần")
d=k.get(f"/TaiSan/Details/{id1}")[1]
check("Phân loại bảo mật" in d and "Nhạy cảm" in d and "P.101" in d, "hồ sơ hiển thị thông tin bổ sung")
check("Hồ sơ vận hành" in d and "MTBF" in d and "Tổng chi phí sở hữu" in d, "hồ sơ vận hành trên trang tài sản")
# Sửa giá trị → lịch sử
pe=k.get(f"/TaiSan/Edit/{id1}")[1]
form={n:v for n,v in re.findall(r'name="(TaiSan\.[A-Za-z]+)"[^>]*value="([^"]*)"', pe)}
form.update({"TaiSan.Id":id1,"TaiSan.LoaiThietBiId":pc,"TaiSan.KhoaPhongId":cc,"TaiSan.TrangThai":"DangSuDung","TaiSan.NguonVon":"NganSach",
  f"TuyChinh[{ids['PhanLoaiBaoMat']}]":"Thường", f"TuyChinh[{ids['SoPhong']}]":"P.102"})
m=k.post("/TaiSan/Save", form, page=pe)[2]
dd=k.get(f"/TaiSan/Details/{id1}")[1]
check("Số phòng: 'P.101' → 'P.102'" in dd and "'Nhạy cảm' → 'Thường'" in dd, "đổi giá trị ghi vào dòng thời gian (cũ → mới)")
# UPS: số, danh sách
st,loc,m,body=k.post("/TaiSan/Save",{"TaiSan.MaTaiSan":"","TaiSan.Ten":"UPS 1500VA","TaiSan.LoaiThietBiId":ups,"TaiSan.TrangThai":"TrongKho","TaiSan.NguonVon":"NganSach",
  f"TuyChinh[{ids['CongSuatVA']}]":"abc"},page=p); check(st==200 and "không phải số" in html.unescape(body), "số sai bị từ chối")
st,loc,m,_=k.post("/TaiSan/Save",{"TaiSan.MaTaiSan":"","TaiSan.Ten":"UPS 1500VA","TaiSan.LoaiThietBiId":ups,"TaiSan.TrangThai":"TrongKho","TaiSan.NguonVon":"NganSach",
  f"TuyChinh[{ids['CongSuatVA']}]":"1.500", f"TuyChinh[{ids['KieuUPS']}]":"online"},page=p)
check(f"UPS-KHO-{nam}-00001" in m, f"UPS ở kho → mã UPS-KHO: {m[:80]}")
v=DB.execute("select g.GiaTri from GiaTriTuyChinhs g join TruongTuyChinhs t on t.Id=g.TruongTuyChinhId where t.Ma in ('CongSuatVA','KieuUPS') order by t.Ma").fetchall()
check([x[0] for x in v]==["1500","Online"], f"lưu chuẩn hoá: {v}")
# 3. Lọc & xuất
lst=k.get(f"/TaiSan?tt={ids['PhanLoaiBaoMat']}&ttv=Th%C6%B0%E1%BB%9Dng")[1]; check(f"PC-CC-{nam}-00001" in lst and f"PC-CC-{nam}-00002" in lst, "lọc theo thông tin bổ sung")
r=k.op.open(urllib.request.Request(BASE+f"/TaiSan/Export?tt={ids['CongSuatVA']}")); wb=openpyxl.load_workbook(io.BytesIO(r.read())); ws=wb.active
hdr=[c.value for c in ws[4]]; col=hdr.index("Công suất (VA) [CongSuatVA]")+1
check(ws.cell(5,col).value==1500 and ws.cell(5,1).value==f"UPS-KHO-{nam}-00001", "xuất Excel có cột bổ sung (kiểu số)")
# 4. Nhập Excel: mã trống → tự sinh; cột bổ sung
mau=k.op.open(urllib.request.Request(BASE+"/TaiSan/MauNhap")).read(); wb=openpyxl.load_workbook(io.BytesIO(mau)); ws=wb["TaiSan"]
hdr=[c.value for c in ws[1]]; cB=hdr.index("Phân loại bảo mật (*) [PhanLoaiBaoMat]")+1; cP=hdr.index("Số phòng [SoPhong]")+1
check(cB>26, "file mẫu có cột trường bổ sung (đánh dấu bắt buộc)")
ws.delete_rows(2); row=["","PC nhập Excel","PC","","","","CC"]+[""]*19; ws.append(row); ws.cell(2,cB).value="Nhạy cảm"; ws.cell(2,cP).value="P.303"
ws.append(["","PC thiếu trường","PC"]+[""]*23)
buf=io.BytesIO(); wb.save(buf); data=buf.getvalue()
def upload():
    page=k.get("/TaiSan/Nhap")[1]; tok=k.token(page); b="----"+uuid.uuid4().hex
    parts=[f'--{b}\r\nContent-Disposition: form-data; name="__RequestVerificationToken"\r\n\r\n{tok}\r\n'.encode(),
           f'--{b}\r\nContent-Disposition: form-data; name="file"; filename="n.xlsx"\r\nContent-Type: application/octet-stream\r\n\r\n'.encode()+data+b"\r\n", f"--{b}--\r\n".encode()]
    req=urllib.request.Request(BASE+"/TaiSan/Nhap", data=b"".join(parts), headers={"Content-Type":"multipart/form-data; boundary="+b})
    return html.unescape(k.op.open(req).read().decode())
pv=upload(); check("Mã sẽ tự sinh" in pv and "Phân loại bảo mật' là trường bắt buộc" in pv, "xem trước: báo tự sinh mã & thiếu trường bắt buộc")
ws.delete_rows(3); buf=io.BytesIO(); wb.save(buf); data=buf.getvalue(); pv=upload()
mf=re.search(r'name="maFile" value="([^"]+)"', pv).group(1)
st,loc,m,_=k.post("/TaiSan/XacNhanNhap",{"maFile":mf},page=pv); print("nhập:", m)
r=DB.execute("select t.MaTaiSan from TaiSans t where t.Ten='PC nhập Excel'").fetchone()
check(r and r[0]==f"PC-CC-{nam}-00003", f"nhập Excel sinh mã {r}")
check(DB.execute("select count(*) from GiaTriTuyChinhs g join TaiSans t on t.Id=g.TaiSanId where t.Ten='PC nhập Excel'").fetchone()[0]==2, "nhập Excel ghi trường bổ sung")
# 5. Báo cáo tuỳ biến theo trường bổ sung
p=k.get("/BaoCaoTuyBien?chay=true&Nguon=TaiSan&Truong=MaTaiSan&Truong=tt_PhanLoaiBaoMat&NhomTheo=tt_PhanLoaiBaoMat")[1]
check(re.search(r"<td[^>]*>Nhạy cảm</td>\s*<td[^>]*>1</td>", p) and re.search(r"<td[^>]*>Thường</td>\s*<td[^>]*>2</td>", p), "báo cáo tuỳ biến nhóm theo trường bổ sung (Thường 2, Nhạy cảm 1)")
# 6. Mẫu mã không hợp lệ bị chặn
m=a.post("/ThamSo/Luu",{"giaTri[TaiSan.MauMa]":"{LOAI}-{NAM}"},page=a.get("/ThamSo")[1])[2]; check("STT" in m, "mẫu mã thiếu {STT} bị từ chối")
a.post("/ThamSo/Luu",{"giaTri[TaiSan.MauMa]":"TB-{LOAI}-{STT:3}"},page=a.get("/ThamSo")[1])
st,b,_=k.req(f"/TaiSan/XemTruocMa?loai={pc}"); check('"TB-PC-001"' in b, "đổi mẫu mã có hiệu lực ngay")
a.post("/ThamSo/Luu",{"giaTri[TaiSan.MauMa]":"{LOAI}-{KHOA}-{NAM}-{STT:5}"},page=a.get("/ThamSo")[1])
check(U("nhanvien").get("/TruongTuyChinh")[2].startswith("/Account/AccessDenied"), "nhân viên không quản lý trường bổ sung")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
