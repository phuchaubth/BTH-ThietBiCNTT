exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, io, openpyxl, datetime
k=U("ktv1")
mas=re.findall(r'Details/\d+">((?:PC|LT)-\d+)</a>', k.get("/TaiSan")[1])[:4]
st,loc,m,_=k.post("/HopDong/Form",{"SoHopDong":"HĐ-15/2026/BTH","Ten":"Bảo trì máy tính 2026","Loai":"BaoTri","HieuLucTu":"2026-01-01","HieuLucDen":(datetime.date.today()+datetime.timedelta(days=30)).isoformat(),"GiaTri":"120000000","PhamVi":"Phản hồi 4 giờ"})
print("tạo HĐ:", m); hid=loc.rsplit("/",1)[1]
print("gắn TS:", k.post(f"/HopDong/GanTaiSan/{hid}",{"danhSachMa":", ".join(mas[:3])+"\nXX-999","capNhatBaoHanh":"true"})[2])
print("danh sách 'sắp hết':", "HĐ-15/2026/BTH" in k.get("/HopDong?tinhTrang=sapHet")[1])
tsid=re.search(rf'/TaiSan/Details/(\d+)">{mas[0]}', k.get(f"/HopDong/Details/{hid}")[1]).group(1)
d=k.get(f"/TaiSan/Details/{tsid}")[1]; print("hồ sơ TS có HĐ:", "HĐ-15/2026/BTH" in d)
st,loc,m,_=k.post("/BanQuyen/Form",{"Ten":"Microsoft Office 2021","NhaSanXuat":"Microsoft","Loai":"VinhVien","SoLuong":"2","HopDongId":hid,"KhoaKichHoat":"ABCDE-12345"}); bid=loc.rsplit("/",1)[1]; print("tạo BQ:", m)
print("cấp 3 máy cho 2 giấy phép:", k.post(f"/BanQuyen/CapPhat/{bid}",{"danhSachMa":",".join(mas[:3])})[2])
ld=U("lanhdao"); b=ld.get(f"/BanQuyen/Details/{bid}")[1]; print("lãnh đạo xem BQ:", "Microsoft Office" in b, "| thấy khoá:", "ABCDE" in b, "| sửa:", ld.get(f"/BanQuyen/Form/{bid}")[2])
print("nhân viên vào HĐ:", U("nhanvien").get("/HopDong")[2])
# Gửi hãng
nv=U("nhanvien"); ts=re.findall(r'/TaiSan/Details/(\d+)">', nv.get("/TaiSan")[1])[0]
st,loc,m,_=nv.post("/PhieuSuaChua/Create",{"TaiSanId":ts,"MoTaSuCo":"Laptop vỡ màn","UuTien":"TrungBinh","XacNhanTrung":"true"}); pid=loc.rsplit("/",1)[1]
k.post(f"/PhieuSuaChua/TiepNhan/{pid}",{"kyThuatVien":"ktv1","hinhThuc":"BaoHanh"})
hen=(datetime.date.today()-datetime.timedelta(days=1)).isoformat()
print("gửi hãng:", k.post(f"/PhieuSuaChua/TamDung/{pid}",{"lyDo":"GuiHang","soPhieuHang":"RMA-7788","ngayHenTra":hen,"nhaCungCapId":"2"})[2])
g=k.get("/PhieuSuaChua/GuiHang")[1]; print("danh sách gửi hãng có RMA + trễ hẹn:", "RMA-7788" in g, 'table-danger' in g)
print("nhận về (tiếp tục):", k.post(f"/PhieuSuaChua/BatDau/{pid}",{})[2][:60])
print("đã nhận về:", "RMA-7788" in k.get("/PhieuSuaChua/GuiHang?daNhan=true")[1])
# Báo cáo
r=ld.get("/BaoCao")[1]; print("báo cáo:", re.findall(r'% đúng SLA</div><div class="value[^"]*">([^<]*)', r), "| MTTR:", re.findall(r'MTTR</div><div class="value">([^<]*)', r))
x=ld.op.open(urllib.request.Request(BASE+"/BaoCao/Xuat")).read(); w=openpyxl.load_workbook(io.BytesIO(x)); print("xlsx:", w.sheetnames)
print("KTV sheet:", [[c.value for c in row] for row in w["KyThuatVien"].iter_rows(min_row=5, max_row=6)])
print("nhân viên vào báo cáo:", U("nhanvien").get("/BaoCao")[2])
