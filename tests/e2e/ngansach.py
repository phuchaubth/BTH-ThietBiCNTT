# Kiểm thử ngân sách: dự toán → duyệt → điều chỉnh → cam kết / thực chi qua mua sắm (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
k=U("ktv1"); a=U("admin"); ld=U("lanhdao")
nam=datetime.date.today().year
idx=k.get("/NganSach")[1]
check(all(m in idx for m in ["TB-MS","VT-LK","SC-TN","PM-BQ"]), "4 khoản mục mẫu đã duyệt")
# 1. Lập dự toán mới, phân bổ quý sai rồi đúng
p=k.get(f"/NganSach/Sua?nam={nam}")[1]
d={"Nam":str(nam),"Ma":"ht-mang","Ten":"Nâng cấp hạ tầng mạng","NhomChi":"MuaSamThietBi","DuToan":"50000000","Quy1":"10000000","Quy2":"10000000"}
st,_,_,body=k.post("/NganSach/Sua",d,page=p); check(st==200 and "phải bằng dự toán" in html.unescape(body), "phân bổ quý sai bị từ chối")
d.update({"Quy3":"15000000","Quy4":"15000000"})
st,loc,m,_=k.post("/NganSach/Sua",d,page=p); print("lưu:", m); km=loc.rsplit("/",1)[1]
check(k.post(f"/NganSach/Duyet/{km}",{},page=k.get(loc)[1])[1].startswith("/Account/AccessDenied"), "KTV không duyệt được dự toán")
m=a.post(f"/NganSach/Duyet/{km}",{},page=a.get(loc)[1])[2]; print("admin duyệt:", m); check("Đã duyệt" in m, "Quản trị duyệt dự toán")
# 2. Điều chỉnh +20tr
m=k.post(f"/NganSach/DieuChinh/{km}",{"giam":"false","soTien":"20000000","lyDo":"Bổ sung switch lõi"},page=k.get(loc)[1])[2]; print("đề nghị:", m)
p=a.get(loc)[1]; dc=re.search(r'name="dieuChinhId" value="(\d+)"', p).group(1)
print("duyệt điều chỉnh:", a.post(f"/NganSach/DuyetDieuChinh/{km}",{"dieuChinhId":dc,"dongY":"true"},page=p)[2])
check("70.000.000" in k.get(loc)[1], "dự toán hiệu lực 70.000.000")
# 3. Đề xuất 3 switch × 25tr = 75tr > 70tr → cảnh báo vượt
p=k.get("/MuaSam/LapDeXuat")[1]
loai={t:i for i,t in re.findall(r'<option value="(\d+)">([^<]+)</option>', p.split('id="ThietBi"')[1])}
sw=[i for t,i in loai.items() if "mạng" in t.lower() or "switch" in t.lower()][0]
check(re.search(rf'<option value="{km}">{nam} · HT-MANG', p) is not None, "khoản mục mới có trong danh sách chọn")
st,loc2,m,_=k.post("/MuaSam/LapDeXuat",{"TieuDe":"Switch lõi","CanCu":"TrangBiMoi","KhoanMucNganSachId":km,"guiDuyet":"true",
  "dong[0].Loai":"ThietBi","dong[0].LoaiThietBiId":sw,"dong[0].MoTa":"Switch L3 48 cổng","dong[0].SoLuong":"3","dong[0].DonGia":"25000000"},page=p)
print("gửi duyệt:", m); dx=loc2.rsplit("/",1)[1]; check("Vượt ngân sách" in m, "cảnh báo vượt ngân sách khi gửi duyệt")
check("Vượt ngân sách" in a.get(loc2)[1], "trang đề xuất hiển thị vượt ngân sách cho người duyệt")
# Bật chặn vượt dự toán → không duyệt được
a.post("/ThamSo/Luu",{"co[NganSach.ChanVuotDuToan]":"true"},page=a.get("/ThamSo")[1])
m=a.post(f"/MuaSam/Duyet/{dx}",{"dongY":"true"},page=a.get(loc2)[1])[2]; print("duyệt khi chặn:", m); check("chặn" in m, "chặn duyệt vượt dự toán")
a.post("/ThamSo/Luu",{"co[NganSach.ChanVuotDuToan]":"false"},page=a.get("/ThamSo")[1])
print("admin duyệt:", a.post(f"/MuaSam/Duyet/{dx}",{"dongY":"true"},page=a.get(loc2)[1])[2])
print("lãnh đạo duyệt:", ld.post(f"/MuaSam/Duyet/{dx}",{"dongY":"true"},page=ld.get(loc2)[1])[2])
c=k.get(loc)[1]; check("75.000.000" in c and "107,1%" in c, "đã duyệt → cam kết 75tr, đã dùng 107,1%")
# 4. Đặt 2 cái giá 24tr (VAT 10%) → cam kết chuyển sang đơn hàng; nhận 1 → thực chi
p=k.get(f"/MuaSam/LapDon?deXuatId={dx}")[1]
i=re.findall(r'name="dong\[(\d+)\].Id" value="(\d+)"', p)[0][1]
ncc=re.search(r'id="NhaCungCapId"[^>]*>\s*<option value="">[^<]*</option>\s*<option value="(\d+)"', p).group(1)
st,loc3,m,_=k.post("/MuaSam/LapDon",{"NhaCungCapId":ncc,"HanGiao":"2026-12-31","ThueSuat":"10","BaoHanhThang":"12","dong[0].Id":i,"dong[0].Chon":"true","dong[0].SoLuong":"2","dong[0].DonGia":"24000000"},page=p)
dh=loc3.rsplit("/",1)[1]; check("HT-MANG" in k.get(loc3)[1], "đơn hàng kế thừa khoản mục từ đề xuất")
ld.post(f"/MuaSam/DatHang/{dh}",{},page=ld.get(loc3)[1])
c=k.get(loc)[1]; check("77.800.000" in c, "cam kết = 25tr (đề xuất còn lại) + 52,8tr (đơn hàng gồm VAT)")
p=k.get(loc3)[1]; n=re.findall(r'name="nhan\[(\d+)\].Id" value="(\d+)"', p)
k.post(f"/MuaSam/NhanHang/{dh}",{"nhan[0].Id":n[0][1],"nhan[0].SoLuong":"1","nhan[0].Serial":"SW-0001"},page=p)
c=k.get(loc)[1]; check("26.400.000" in c and "51.400.000" in c, "nhận 1 → thực chi 26,4tr, cam kết còn 51,4tr")
# 5. Khoản chi ghi tay
vt=re.search(r'href="/NganSach/ChiTiet/(\d+)">VT-LK', k.get("/NganSach")[1]).group(1)
print("chi ngoài năm:", k.post(f"/NganSach/ThemChi/{vt}",{"ngay":f"{nam-1}-12-01","soTien":"1000000","noiDung":"x"},page=k.get(f"/NganSach/ChiTiet/{vt}")[1])[2])
m=k.post(f"/NganSach/ThemChi/{vt}",{"ngay":f"{nam}-11-01","soTien":"4500000","soChungTu":"PC-123","noiDung":"Mua cáp mạng lẻ"},page=k.get(f"/NganSach/ChiTiet/{vt}")[1])[2]
check("Đã ghi" in m and "4.500.000" in k.get(f"/NganSach/ChiTiet/{vt}")[1], "ghi khoản chi tay")
st,b,_=k.req(f"/NganSach/Xuat?nam={nam}"); check(st==200 and b.startswith("PK"), "xuất Excel báo cáo dự toán – thực hiện")
check(U("nhanvien").get("/NganSach")[2].startswith("/Account/AccessDenied"), "nhân viên không vào ngân sách")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
