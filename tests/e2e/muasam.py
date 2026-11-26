# Kiểm thử mua sắm: đề xuất → duyệt nhiều cấp → đơn hàng → nhận hàng (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
k=U("ktv1"); a=U("admin"); ld=U("lanhdao")
# 1. Tự lập đề xuất từ tồn thấp (SSD-256 4/5, TRONG-HP 1/3)
st,loc,m,_=k.post("/MuaSam/TuDong",{},page=k.get("/MuaSam")[1]); print("tự động:", m)
if "Không có vật tư" in m:
    # Bản tin hằng ngày (sau 07:30) có thể đã tự lập đề xuất nháp trước — dùng đề xuất đó
    loc=re.search(r'href="(/MuaSam/DeXuat/\d+)">[^<]+</a>\s*<span class="badge[^"]*"[^>]*>tự động', k.get("/MuaSam")[1]).group(1)
auto=k.get(loc)[1]
check("TRONG-HP" in auto and "RAM-D4-8G" not in auto, "đề xuất tự động gồm vật tư dưới định mức")
st,loc2,m,_=k.post("/MuaSam/TuDong",{},page=k.get("/MuaSam")[1]); print("chạy lại:", m)
check("Không có vật tư" in m, "chạy lại không trùng (đã tính số đang mua)")
# 2. Lập đề xuất nhỏ (dưới ngưỡng 50tr) và lớn
p=k.get("/MuaSam/LapDeXuat")[1]
vt={m_:i for i,m_ in re.findall(r'<option value="(\d+)" data-gia="\d+" data-ton="[^"]*" data-dang="[^"]*">([A-Z0-9-]+) –', p)}
loai={t:i for i,t in re.findall(r'<option value="(\d+)">([^<]+)</option>', p.split('id="ThietBi"')[1])}
pc=[i for t,i in loai.items() if "để bàn" in t.lower()][0]
st,loc,m,_=k.post("/MuaSam/LapDeXuat",{"TieuDe":"Thay PC phòng Kế hoạch","CanCu":"ThayThe","guiDuyet":"true",
  "dong[0].Loai":"VatTu","dong[0].VatTuId":vt["RAM-D4-8G"],"dong[0].SoLuong":"5","dong[0].DonGia":"450000",
  "dong[1].Loai":"ThietBi","dong[1].LoaiThietBiId":pc,"dong[1].MoTa":"PC Core i5-14400, 16GB, SSD 512GB","dong[1].SoLuong":"2","dong[1].DonGia":"18000000"},page=p)
print("đề xuất nhỏ:", m); dx1=loc.rsplit("/",1)[1]
d=k.get(loc)[1]; check("Trưởng phòng CNTT" in d and "Lãnh đạo bệnh viện" not in d.split("Quy trình duyệt")[1][:200], "dưới ngưỡng: chỉ cấp Trưởng phòng CNTT")
print("KTV tự duyệt:", k.post(f"/MuaSam/Duyet/{dx1}",{"dongY":"true"},page=d)[2])
print("lãnh đạo duyệt sai cấp:", ld.post(f"/MuaSam/Duyet/{dx1}",{"dongY":"true"},page=ld.get(f"/MuaSam/DeXuat/{dx1}")[1])[2])
m=a.post(f"/MuaSam/Duyet/{dx1}",{"dongY":"true","yKien":"Đồng ý"},page=a.get(f"/MuaSam/DeXuat/{dx1}")[1])[2]; print("admin duyệt:", m)
check("hoàn tất" in m, "đề xuất nhỏ duyệt xong 1 cấp")
p=k.get("/MuaSam/LapDeXuat")[1]
st,loc,m,_=k.post("/MuaSam/LapDeXuat",{"TieuDe":"Trang bị phòng máy đào tạo","CanCu":"TrangBiMoi","guiDuyet":"true",
  "dong[0].Loai":"ThietBi","dong[0].LoaiThietBiId":pc,"dong[0].MoTa":"PC đào tạo","dong[0].SoLuong":"3","dong[0].DonGia":"20000000"},page=p)
dx2=loc.rsplit("/",1)[1]
m=a.post(f"/MuaSam/Duyet/{dx2}",{"dongY":"true"},page=a.get(f"/MuaSam/DeXuat/{dx2}")[1])[2]; print("admin duyệt lớn:", m)
check("Lãnh đạo" in m, "≥ ngưỡng: chuyển Lãnh đạo")
cho=ld.get("/MuaSam?choToi=true")[1]; check(f"/MuaSam/DeXuat/{dx2}" in cho, "lãnh đạo thấy trong 'Chờ tôi duyệt'")
print("từ chối thiếu lý do:", ld.post(f"/MuaSam/Duyet/{dx2}",{"dongY":"false"},page=cho)[2])
print("lãnh đạo duyệt:", ld.post(f"/MuaSam/Duyet/{dx2}",{"dongY":"true"},page=cho)[2])
tb=k.get("/ThongBao")[1]; check("đã được duyệt" in tb, "người lập nhận thông báo kết quả duyệt")
# 3. Lập đơn hàng từ đề xuất nhỏ: RAM 5 + PC 1 (đặt một phần)
p=k.get(f"/MuaSam/LapDon?deXuatId={dx1}")[1]
ids=re.findall(r'name="dong\[(\d+)\].Id" value="(\d+)"', p)
ncc=re.search(r'id="NhaCungCapId"[^>]*>\s*<option value="">[^<]*</option>\s*<option value="(\d+)"', p).group(1)
data={"NhaCungCapId":ncc,"CanCu":"Báo giá 15/11","HanGiao":"2026-12-10","ThueSuat":"10","BaoHanhThang":"36"}
for idx,i in ids: data[f"dong[{idx}].Id"]=i; data[f"dong[{idx}].Chon"]="true"; data[f"dong[{idx}].SoLuong"]="5" if idx=="0" else "1"; data[f"dong[{idx}].DonGia"]="440000" if idx=="0" else "17500000"
st,loc,m,_=k.post("/MuaSam/LapDon",data,page=p); print("lập đơn:", m); dh=loc.rsplit("/",1)[1]
d=k.get(f"/MuaSam/DeXuat/{dx1}")[1]; check("Đã lập đơn một phần" in d, "đề xuất → đã lập đơn một phần")
over=dict(data); over[f"dong[1].SoLuong"]="5"
m=k.post("/MuaSam/LapDon",over,page=p)[2]; print("đặt vượt số đề xuất:", m); check("RAM-D4-8G" in m and "chỉ còn 0" in m, "chặn đặt vượt số lượng đề xuất")
print("KTV xác nhận đặt hàng:", k.post(f"/MuaSam/DatHang/{dh}",{},page=k.get(f"/MuaSam/DonHang/{dh}")[1])[2])
m=a.post(f"/MuaSam/DatHang/{dh}",{},page=a.get(f"/MuaSam/DonHang/{dh}")[1])[2]; print("admin đặt hàng:", m); check("Đã xác nhận" in m, "dưới ngưỡng: Trưởng phòng CNTT xác nhận đặt")
# 4. Nhận hàng một phần: RAM 3 + PC 1 kèm serial
p=k.get(f"/MuaSam/DonHang/{dh}")[1]
n=re.findall(r'name="nhan\[(\d+)\].Id" value="(\d+)"', p)
kho=re.search(r'name="khoId" id="khoId"[^>]*>\s*<option[^>]*value="(\d+)"', p).group(1)
st,loc,m,_=k.post(f"/MuaSam/NhanHang/{dh}",{"khoId":kho,"soHoaDon":"HĐ 0000881","bienBan":"BBNT 01",f"nhan[0].Id":n[0][1],"nhan[0].SoLuong":"3",
  "nhan[1].Id":n[1][1],"nhan[1].SoLuong":"1","nhan[1].Serial":"SN-PC-0001"},page=p); print("nhận lần 1:", m)
check("Tài sản mới" in m and "phiếu nhập kho" in m, "nhận hàng: tạo phiếu nhập + tài sản")
p=k.get(f"/MuaSam/DonHang/{dh}")[1]; check("Đã nhận một phần" in p, "đơn → đã nhận một phần")
n=re.findall(r'name="nhan\[(\d+)\].Id" value="(\d+)"', p)
print("nhận vượt:", k.post(f"/MuaSam/NhanHang/{dh}",{"khoId":kho,"nhan[0].Id":n[0][1],"nhan[0].SoLuong":"9"},page=p)[2])
m=k.post(f"/MuaSam/NhanHang/{dh}",{"khoId":kho,"nhan[0].Id":n[0][1],"nhan[0].SoLuong":"2"},page=p)[2]; print("nhận lần 2:", m)
p=k.get(f"/MuaSam/DonHang/{dh}")[1]; check("Đã nhận đủ" in p, "đơn → đã nhận đủ")
print("in đơn:", k.get(f"/MuaSam/InDon/{dh}")[0])
# 5. Đơn lớn: chỉ Lãnh đạo xác nhận; huỷ đơn trả lại số lượng
p=k.get(f"/MuaSam/LapDon?deXuatId={dx2}")[1]
i=re.findall(r'name="dong\[(\d+)\].Id" value="(\d+)"', p)[0][1]
st,loc,m,_=k.post("/MuaSam/LapDon",{"NhaCungCapId":ncc,"HanGiao":"2026-11-01","ThueSuat":"10","BaoHanhThang":"24","dong[0].Id":i,"dong[0].Chon":"true","dong[0].SoLuong":"3","dong[0].DonGia":"20000000"},page=p)
dh2=loc.rsplit("/",1)[1]
print("admin đặt đơn lớn:", a.post(f"/MuaSam/DatHang/{dh2}",{},page=a.get(f"/MuaSam/DonHang/{dh2}")[1])[2])
m=ld.post(f"/MuaSam/DatHang/{dh2}",{},page=ld.get(f"/MuaSam/DonHang/{dh2}")[1])[2]; print("lãnh đạo đặt:", m); check("Đã xác nhận" in m, "≥ ngưỡng: Lãnh đạo xác nhận đặt")
check(f"/MuaSam/DonHang/{dh2}" in k.get("/MuaSam/DonHangs?treHan=true")[1], "đơn quá hạn giao lọc được")
m=k.post(f"/MuaSam/HuyDon/{dh2}",{},page=k.get(f"/MuaSam/DonHang/{dh2}")[1])[2]; print("huỷ đơn:", m)
check("Đã duyệt" in k.get(f"/MuaSam/DeXuat/{dx2}")[1], "huỷ đơn → đề xuất trở lại 'Đã duyệt'")
print("nhân viên vào mua sắm:", U("nhanvien").get("/MuaSam")[2])
db=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
ram=db.execute("select TonKho, DonGia from VatTus where Ma='RAM-D4-8G'").fetchone()
ts=db.execute("select MaTaiSan, SoSerial, TrangThai, NguyenGia, HanBaoHanh from TaiSans where GhiChu like 'Nhận theo đơn hàng%'").fetchall()
print("RAM tồn/giá BQ:", ram, "| tài sản mới:", ts)
check(float(ram[0])==17 and len(ts)==1 and ts[0][1]=="SN-PC-0001" and ts[0][2]==1 and float(ts[0][3])==19250000, "tồn 12+5=17, tài sản Trong kho, nguyên giá gồm VAT 19.250.000")
lech=db.execute("select v.Ma from VatTus v left join TonKhoVatTus t on t.VatTuId=v.Id group by v.Id having abs(v.TonKho - coalesce(sum(t.SoLuong),0)) > 0.001").fetchall()
check(not lech, "bất biến tổng tồn = Σ tồn kho")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
