# Kiểm thử TCO, báo cáo tuỳ biến, gửi báo cáo định kỳ (cần app demo đang chạy; gửi email cần mock.py SMTP 2525 — xem thongbao.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, io, sqlite3, datetime, openpyxl, warnings, time
warnings.filterwarnings("ignore")
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
k=U("ktv1"); a=U("admin"); ld=U("lanhdao")
def xlsx(u, path):
    st,_,_=u.req(path)
    r=u.op.open(urllib.request.Request(BASE+path)); return openpyxl.load_workbook(io.BytesIO(r.read()), data_only=False)
# 1. TCO: hợp đồng bảo trì 30tr gắn 3 máy → mỗi máy 10tr
mas=[r[0] for r in DB.execute("select MaTaiSan from TaiSans where MaTaiSan like 'PC-%' and TrangThai<>6 order by MaTaiSan limit 3")]
st,loc,m,_=k.post("/HopDong/Form",{"SoHopDong":"HĐ-BT-TCO","Ten":"Bảo trì PC (TCO)","Loai":"BaoTri","HieuLucTu":"2026-01-01","HieuLucDen":"2026-12-31","GiaTri":"30000000"})
hid=loc.rsplit("/",1)[1]; k.post(f"/HopDong/GanTaiSan/{hid}",{"danhSachMa":", ".join(mas)})
p=k.get("/Tco?nhom=TaiSan")[1]
row=re.search(rf'href="/qr/{mas[0]}">{mas[0]}</a>\s*</td>(.*?)</tr>', p, re.S).group(1)
cells=[re.sub(r"<[^>]+>","",c).strip() for c in re.findall(r"<td[^>]*>(.*?)</td>", row, re.S)]
print("TCO", mas[0], cells)
check(cells[4]=="10.000.000", "hợp đồng bảo trì phân bổ đều: 30tr / 3 máy = 10tr")
ng=DB.execute("select NguyenGia from TaiSans where MaTaiSan=?", (mas[0],)).fetchone()[0]
tong=sum(int(cells[i].replace(".","") or 0) for i in (1,2,3,4,5))
check(int(cells[6].replace(".",""))==tong, f"TCO = nguyên giá + sửa + bảo trì + HĐ + bản quyền ({tong:,})")
for n in ("Loai","Model","Khoa"): check(k.get(f"/Tco?nhom={n}")[0]==200, f"TCO nhóm theo {n}")
wb=xlsx(k,"/Tco/Xuat"); check(wb.sheetnames==["TheoLoai","TheoModel","TheoTaiSan"], f"TCO xuất Excel: {wb.sheetnames}")
s1=sum((r[8] or 0) for r in wb["TheoLoai"].iter_rows(min_row=5, values_only=True) if r[0]); s2=sum((r[10] or 0) for r in wb["TheoTaiSan"].iter_rows(min_row=5, values_only=True) if r[0])
check(abs(s1-s2)<1, f"tổng TCO theo loại = theo tài sản ({s1:,.0f})")
# 2. Báo cáo tuỳ biến: nhóm phiếu theo khoa, cộng chi phí
q="/BaoCaoTuyBien?chay=true&Nguon=PhieuSuaChua&Truong=SoPhieu&Truong=Khoa&Truong=TongChiPhi&NhomTheo=Khoa"
p=k.get(q)[1]
hang=re.findall(r'<tbody>(.*?)</tbody>', p, re.S)[0]
nhom=[[re.sub(r"<[^>]+>","",c).strip() for c in re.findall(r"<td[^>]*>(.*?)</td>", r, re.S)] for r in re.findall(r"<tr>(.*?)</tr>", hang, re.S)]
print("nhóm theo khoa:", nhom[:3])
tongPhieu=DB.execute("select count(*) from PhieuSuaChuas").fetchone()[0]
check(sum(int(r[1]) for r in nhom)==tongPhieu and len(nhom[0])==3, f"nhóm: tổng số phiếu = {tongPhieu}, cột [Khoa, Số phiếu, Tổng chi phí]")
check(int(nhom[0][1])>=int(nhom[-1][1]), "nhóm mặc định sắp giảm dần theo số lượng")
p=k.get("/BaoCaoTuyBien?chay=true&Nguon=TaiSan&Truong=MaTaiSan&Truong=Khoa&Truong=NguyenGia&TrangThai=Hong&SapXep=NguyenGia&Giam=true")[1]
soHong=DB.execute("select count(*) from TaiSans where TrangThai=4").fetchone()[0]
check(f"{soHong} bản ghi" in p and "Hỏng" in p, f"lọc tài sản hỏng: {soHong}")
check("Chọn ít nhất một trường" in k.get("/BaoCaoTuyBien?chay=true&Nguon=TaiSan")[1], "báo lỗi khi không chọn trường")
# Lưu mẫu, chia sẻ, lãnh đạo thấy
pg=k.get(q)[1]
st,loc,m,_=k.post("/BaoCaoTuyBien/LuuMau",{"ten":"Chi phí sửa chữa theo khoa","chiaSe":"true","Nguon":"PhieuSuaChua","Truong":["SoPhieu","Khoa","TongChiPhi"],"NhomTheo":"Khoa","TheoKyGui":"true"},page=pg)
check("Đã lưu mẫu" in m, "lưu mẫu báo cáo"); mid=re.search(r"mauId=(\d+)", loc).group(1)
p=ld.get(f"/BaoCaoTuyBien?mauId={mid}")[1]; check("Chi phí sửa chữa theo khoa" in p and "<tbody>" in p, "lãnh đạo mở mẫu được chia sẻ, tự chạy")
check(ld.post(f"/BaoCaoTuyBien/XoaMau/{mid}",{},page=p)[0]==404, "người khác không xoá được mẫu")
wb=xlsx(k,"/BaoCaoTuyBien/Xuat?Nguon=TaiSan&Truong=MaTaiSan&Truong=NguyenGia&ten=DS%20tai%20san"); ws=wb.active
check(ws.cell(4,1).value=="Mã tài sản" and ws.max_row>5, "xuất Excel báo cáo tuỳ biến")
# 3. Lịch gửi định kỳ (email qua mock SMTP nếu đang chạy)
a.post("/ThamSo/Luu",{"co[ThongBao.EmailBat]":"true","giaTri[ThongBao.SmtpHost]":"127.0.0.1","giaTri[ThongBao.SmtpPort]":"2525","co[ThongBao.SmtpBaoMat]":"false","giaTri[ThongBao.EmailGui]":"thietbi@bth.local"},page=a.get("/ThamSo")[1])
p=ld.get("/LichGuiBaoCao/Sua")[1]
st,loc,m,_=ld.post("/LichGuiBaoCao/Sua",{"Ten":"KPI tháng – BGĐ","Loai":"KpiVanHanh","ChuKy":"Thang","NguoiNhan":"lanhdao, giamdoc@bth.local","DangBat":["true","false"]},page=p)
check("Đã lưu lịch" in m, "lãnh đạo tạo lịch gửi KPI hằng tháng")
st,loc,m,_=ld.post("/LichGuiBaoCao/Sua",{"Ten":"Thiếu mẫu","Loai":"TuyBien","ChuKy":"Tuan","NguoiNhan":"x@y.z","DangBat":["true","false"]},page=p)
check(st==200, "tuỳ biến mà không chọn mẫu → báo lỗi")
st,loc,m,_=k.post("/LichGuiBaoCao/Sua",{"Ten":"Chi phí theo khoa tuần","Loai":"TuyBien","BaoCaoMauId":mid,"ChuKy":"Tuan","NguoiNhan":"ktv1","DangBat":["true","false"]},page=k.get("/LichGuiBaoCao/Sua")[1])
idx=ld.get("/LichGuiBaoCao")[1]
ids=re.findall(r'/LichGuiBaoCao/GuiNgay/(\d+)', idx)
for i in ids:
    m=ld.post(f"/LichGuiBaoCao/GuiNgay/{i}",{},page=idx)[2]; print("gửi ngay:", m)
check(all("Đã xếp hàng gửi" in ld.get("/LichGuiBaoCao")[1] for _ in [0]) and "Lỗi" not in ld.get("/LichGuiBaoCao")[1], "gửi ngay cả 2 lịch thành công")
tin=DB.execute("select DiaChi, TenTepDinhKem, TepDinhKem from TinNhanChos where TenTepDinhKem is not null").fetchall()
print("email kèm tệp:", [(t[0], t[1]) for t in tin])
check(any(t[0]=="giamdoc@bth.local" and t[1].startswith("KPI_") for t in tin), "email KPI kèm tệp xlsx tới địa chỉ ngoài")
check(any(t[1].startswith("Chi_phi_sua_chua") for t in tin), "email báo cáo tuỳ biến (tên tệp không dấu)")
st,b,_=ld.req(f"/LichGuiBaoCao/Tep/{ids[0]}"); check(st==200 and b.startswith("PK"), "tải tệp báo cáo đã gửi")
tb=ld.get("/ThongBao")[1]; check("[Báo cáo định kỳ] KPI tháng" in tb, "người nhận (tài khoản) có thông báo trong ứng dụng")
check(U("nhanvien").get("/Tco")[2].startswith("/Account/AccessDenied"), "nhân viên không xem TCO")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
