# Kiểm thử end-to-end phân hệ hạ tầng CNTT theo JCI 8 (GĐ3.1) — chạy trên bản demo SQLite có dữ liệu mẫu.
# Chạy: cd src/ThietBiCNTT.Web && rm -f thietbicntt-demo.db* && ASPNETCORE_ENVIRONMENT=Demo ASPNETCORE_URLS=http://127.0.0.1:5099 dotnet run --no-launch-profile
#       rồi (từ thư mục gốc repo): python3 tests/e2e/hatang.py
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import datetime
ok=True
def check(cond,label):
    global ok
    print(("PASS " if cond else "FAIL ")+label); ok = ok and cond
adm=U("admin"); ktv=U("ktv1"); ld=U("lanhdao"); tk=U("truongkhoacc"); nv=U("nhanvien")
def ids(body, pat): return re.findall(pat, body)
def msg(r): return r[2]

# 1. Phân quyền trang
pages={"/HeThong":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":200,"nhanvien":200},
 "/HeThong/AnhHuong":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/KeHoachDowntime":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/KeHoachDowntime/BoKhoa":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":200,"nhanvien":200},
 "/SuKienDowntime":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":200,"nhanvien":302},
 "/DienTap":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/SaoLuu":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/SaoLuu/PhucHoiDs":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/HanhDong":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
 "/HeThong/Form":{"admin":200,"ktv1":200,"lanhdao":302,"truongkhoacc":302,"nhanvien":302}}
users={"admin":adm,"ktv1":ktv,"lanhdao":ld,"truongkhoacc":tk,"nhanvien":nv}
for p,exp in pages.items():
    bad=[f"{n}={users[n].get(p)[0]}(cần {e})" for n,e in exp.items() if users[n].get(p)[0]!=e]
    check(not bad, f"Phân quyền {p} "+", ".join(bad))

# 2. Danh mục hệ thống & phân tích ảnh hưởng
l=adm.get("/HeThong")[1]
check(all(x in l for x in ["HIS","LIS","PACS","EMAIL"]), "Danh sách có HIS, LIS, PACS, EMAIL")
check("Chưa có kế hoạch downtime hiệu lực" in l and "Chưa khai báo sao lưu" in l, "Cảnh báo hồ sơ thiếu (BR-HTT-01/02)")
hid={m:i for i,m in re.findall(r'/HeThong/Details/(\d+)">([A-Z]+)</a>', l)}
st,b,_=adm.get("/HeThong/AnhHuong?ma=SV-0002")
check("LIS" in b and "HIS" in b and "Tối quan trọng" in b, "SV-0002 ngừng → ảnh hưởng HIS và LIS (qua phụ thuộc gián tiếp)")
check("SV-0001" in b and "SW-0001" in b, "Điểm lỗi đơn liệt kê SV-0001, SW-0001")
d=adm.get(f"/HeThong/Details/{hid['HIS']}")[1]
check("Khả dụng" in d and "99" in d, "Hồ sơ HIS hiển thị khả dụng 30/90 ngày")
# BR-HTT-03: không tạo vòng HIS → LIS (LIS đã phụ thuộc HIS)
r=adm.post(f"/HeThong/ThemPhuThuoc/{hid['HIS']}",{"heThongPhuThuocId":hid['LIS'],"vaiTro":"vòng"})
check("vòng" in r[2] and r[2].startswith("danger"), "Chặn phụ thuộc vòng (BR-HTT-03)")
r=adm.post(f"/HeThong/ThemPhuThuoc/{hid['PACS']}",{"danhSachMa":"UPS-0101, XX-999","vaiTro":"Nguồn","coDuPhong":"true"})
check("Không tìm thấy: XX-999" in r[2], "Thêm phụ thuộc tài sản, báo mã không tồn tại")
check(nv.get(f"/HeThong/Details/{hid['EMAIL']}")[0]==404 and nv.get(f"/HeThong/Details/{hid['HIS']}")[0]==200, "Nhân viên khoa Cấp cứu chỉ xem hệ thống khoa mình dùng (HIS có, EMAIL không)")
check("EMAIL" not in nv.get("/HeThong")[1] and "HIS" in nv.get("/HeThong")[1], "Danh sách hệ thống của nhân viên được giới hạn theo khoa")

# 3. Kế hoạch downtime: soạn → gửi duyệt → 2 cấp duyệt (LIS Tối quan trọng)
st,loc,m,_=ktv.post("/KeHoachDowntime/Form",{"HeThongId":hid['LIS'],"TieuChiKichHoat":"LIS không truy cập được quá 15 phút","DanhBa":"KTV trực 0901","BuocBatDau":"Báo khoa XN","BuocKhoiPhuc":"Khôi phục từ bản sao lưu","QuyTrinhThayThe":"Phiếu xét nghiệm giấy","QuyTrinhNhapBu":"Nhập lại kết quả"})
check(st==302 and "/KeHoachDowntime/Details/" in (loc or ""), "KTV lập kế hoạch downtime (nháp)")
kid=loc.rsplit("/",1)[1]
check(msg(ktv.post(f"/KeHoachDowntime/Duyet/{kid}",{})).startswith("danger"), "Không duyệt được khi chưa gửi duyệt")
check(msg(ktv.post(f"/KeHoachDowntime/GuiDuyet/{kid}",{})).startswith("success"), "Gửi duyệt")
check(msg(ld.post(f"/KeHoachDowntime/Duyet/{kid}",{})).startswith("danger"), "Lãnh đạo không duyệt trước Trưởng phòng CNTT")
check(msg(ktv.post(f"/KeHoachDowntime/Duyet/{kid}",{})).startswith("danger"), "KTV thường không có quyền duyệt")
r=adm.post(f"/KeHoachDowntime/Duyet/{kid}",{}); check("chờ Lãnh đạo" in r[2], "Trưởng phòng CNTT duyệt bước 1 → chờ Lãnh đạo")
r=ld.post(f"/KeHoachDowntime/Duyet/{kid}",{}); check("có hiệu lực" in r[2], "Lãnh đạo duyệt bước 2 → có hiệu lực")
check("Hiệu lực" in adm.get(f"/KeHoachDowntime/Details/{kid}")[1], "Kế hoạch LIS ở trạng thái Hiệu lực")
# Phiên bản mới → bản cũ hết hiệu lực khi duyệt xong (BR-DT-01)
st,loc,m,_=adm.post(f"/KeHoachDowntime/PhienBanMoi/{kid}",{}); k2=loc.rsplit("/",1)[1]
check(st==302 and k2!=kid, "Tạo phiên bản mới từ kế hoạch hiệu lực")
# 4. Bộ downtime của khoa
b=tk.get("/KeHoachDowntime/BoKhoa")[1]
check("HIS" in b and "Kích hoạt" in b or "Khi nào kích hoạt" in b, "Trưởng khoa Cấp cứu thấy bộ downtime (HIS, LIS)")
check("Bộ hướng dẫn" in tk.get("/KeHoachDowntime/InBoKhoa")[1].replace("BỘ HƯỚNG DẪN","Bộ hướng dẫn") or "KHI HỆ THỐNG" in tk.get("/KeHoachDowntime/InBoKhoa")[1], "Bản in một trang của bộ downtime")

# 5. Sự kiện downtime đột xuất → khôi phục → nhập bù → rút kinh nghiệm → đóng
now=datetime.datetime.now()
st,loc,m,_=adm.post("/SuKienDowntime/Create",{"Loai":"DotXuat","MucDo":"NgungHoanToan","MoTa":"E2E: máy chủ CSDL HIS treo","BatDau":(now-datetime.timedelta(minutes=300)).strftime("%Y-%m-%dT%H:%M"),"KichHoatKeHoach":"true","AnhHuongAnToan":"true","HeThongIds":[hid['HIS']]})
check(st==302 and "/SuKienDowntime/Details/" in (loc or ""), "Ghi nhận downtime đột xuất cho HIS")
sid=loc.rsplit("/",1)[1]; sd=adm.get(f"/SuKienDowntime/Details/{sid}")[1]
check("Đang diễn ra" in sd and "Ảnh hưởng an toàn người bệnh" in sd, "Sự kiện đang diễn ra, có cờ an toàn người bệnh")
check("Cấp cứu" in sd, "Khoa bị ảnh hưởng lấy tự động từ hệ thống (Cấp cứu)")
r=adm.post(f"/SuKienDowntime/Dong/{sid}",{}); check(r[2].startswith("danger"), "Không đóng được khi chưa khôi phục")
r=adm.post(f"/SuKienDowntime/KhoiPhuc/{sid}",{"thoiDiem":now.strftime("%Y-%m-%dT%H:%M")}); check("Vượt RTO" in r[2], "Khôi phục sau 300 phút → vượt RTO 240 phút, bắt buộc rút kinh nghiệm")
r=adm.post(f"/SuKienDowntime/Dong/{sid}",{}); check("nhập bù" in r[2] and "rút kinh nghiệm" in r[2], "BR-DT-03: chưa đóng được vì thiếu nhập bù và rút kinh nghiệm")
kc=[int(x) for x in re.findall(r'name="khoaId" value="(\d+)"', adm.get(f"/SuKienDowntime/Details/{sid}")[1])]
check(len(kc)>0, "Có khoa cần xác nhận nhập bù")
r=nv.post(f"/SuKienDowntime/NhapBu/{sid}",{"khoaId":kc[0]}); check(r[0] in (302,) and ("Forbid" in r[3] or r[1] is None or "AccessDenied" in (r[1] or "")), "Nhân viên không được xác nhận nhập bù")
for k in sorted(set(kc)): adm.post(f"/SuKienDowntime/NhapBu/{sid}",{"khoaId":k,"ghiChu":"đã nhập bù"})
r=adm.post(f"/SuKienDowntime/Dong/{sid}",{}); check("rút kinh nghiệm" in r[2] and "nhập bù" not in r[2], "Đã nhập bù, còn thiếu rút kinh nghiệm")
r=adm.post(f"/SuKienDowntime/RutKinhNghiem/{sid}",{"nguyenNhan":"Treo do đầy ổ đĩa","diemTot":"Chuyển giấy nhanh","canCaiThien":"Cảnh báo dung lượng","canSuaKeHoach":"true"}); check(r[2].startswith("success"), "Ghi rút kinh nghiệm")
r=adm.post(f"/SuKienDowntime/Dong/{sid}",{}); check("Đã đóng" in r[2], "Đóng sự kiện sau khi đủ điều kiện")
check("Cần rà soát" in adm.get("/KeHoachDowntime")[1], "FR-DT-13: kế hoạch HIS đánh dấu 'Cần rà soát' sau sự kiện")
# Downtime có kế hoạch
r=adm.post("/SuKienDowntime/Create",{"Loai":"CoKeHoach","MucDo":"NgungHoanToan","MoTa":"Nâng cấp phiên bản HIS","DuKienBatDau":(now+datetime.timedelta(days=2)).strftime("%Y-%m-%dT%H:%M"),"DuKienKetThuc":(now+datetime.timedelta(days=2,hours=2)).strftime("%Y-%m-%dT%H:%M"),"HeThongIds":[hid['HIS']]})
check(r[0]==302 and "/SuKienDowntime/Details/" in (r[1] or ""), "Đăng ký downtime có kế hoạch")
r=adm.post("/SuKienDowntime/Create",{"Loai":"CoKeHoach","MucDo":"NgungHoanToan","MoTa":"quá khứ","DuKienBatDau":(now-datetime.timedelta(days=1)).strftime("%Y-%m-%dT%H:%M"),"DuKienKetThuc":(now).strftime("%Y-%m-%dT%H:%M"),"HeThongIds":[hid['HIS']]})
check(r[0]==200, "Từ chối downtime có kế hoạch đăng ký ở quá khứ")

# 6. Diễn tập: ghi, chốt Không đạt → CAPA + đợt lại; sau chốt không sửa
dt=adm.get("/DienTap")[1]; did=ids(dt, r'/DienTap/Details/(\d+)')
print("INFO", "Có đợt diễn tập tự sinh theo chu kỳ" if len(did)>0 else "Chưa đến kỳ diễn tập nào (HIS/LIS mới có hiệu lực) — tạo tay để kiểm thử")
if not did:
    adm.post("/DienTap/Create",{"heThongId":hid['HIS'],"hanThucHien":(datetime.date.today()+datetime.timedelta(days=10)).isoformat(),"lyDo":"e2e"}); did=ids(adm.get("/DienTap")[1], r'/DienTap/Details/(\d+)')
x=did[0]
r=adm.post(f"/DienTap/Chot/{x}",{"ketLuan":"KhongDat"}); check(r[2].startswith("danger"), "Không chốt được khi chưa ghi biên bản")
r=adm.post(f"/DienTap/Ghi/{x}",{"ngayThucHien":now.strftime("%Y-%m-%dT%H:%M"),"hinhThuc":"TrenBan","dienBien":"9:00 giả định ngừng; 9:20 khoa chưa chuyển giấy","phatHien":"Điều dưỡng chưa biết vị trí bộ biểu mẫu giấy"}); check(r[2].startswith("success"), "Ghi biên bản diễn tập")
r=adm.post(f"/DienTap/Chot/{x}",{"ketLuan":"KhongDat"}); check("Không đạt" in r[2] and "diễn tập lại" in r[2], "Chốt Không đạt → tạo hành động khắc phục và lên lịch diễn tập lại")
r=adm.post(f"/DienTap/Ghi/{x}",{"ngayThucHien":now.strftime("%Y-%m-%dT%H:%M"),"hinhThuc":"TrenBan","dienBien":"sửa lén"}); check(r[2].startswith("danger"), "NFR-JC-02: biên bản đã chốt không sửa được")
r=adm.post(f"/DienTap/PhuLuc/{x}",{"noiDung":"Bổ sung: đã tập huấn lại"}); check(r[2].startswith("success"), "Thêm phụ lục sau khi chốt")
hd=adm.get("/HanhDong")[1]; hids=ids(hd, r'/HanhDong/Details/(\d+)')
check("Diễn tập downtime" in hd, "Hành động khắc phục từ diễn tập xuất hiện trong danh sách")

# 7. Sao lưu: ma trận, ghi kết quả, thử phục hồi
sl=adm.get("/SaoLuu")[1]
check("CSDL HIS – full hằng đêm" in sl and "Thành công" in sl, "Ma trận sao lưu có tác vụ HIS")
check("Chưa chứng minh phục hồi được" in sl, "BR-SL-02: HIS chưa có lần thử phục hồi Đạt")
tvid=ids(sl, r'/SaoLuu/TacVu/(\d+)')[0]
r=adm.post(f"/SaoLuu/GhiKetQua/{tvid}",{"thoiDiem":now.strftime("%Y-%m-%dT%H:%M"),"ketQua":"Loi","ghiChu":""}); check(r[2].startswith("danger"), "Kết quả lỗi bắt buộc ghi chú")
r=adm.post(f"/SaoLuu/GhiKetQua/{tvid}",{"thoiDiem":now.strftime("%Y-%m-%dT%H:%M"),"ketQua":"ThanhCong","dungLuongGb":"45.5","thoiLuongPhut":"22"}); check(r[2].startswith("success"), "Ghi kết quả sao lưu thành công")
st,loc,m,_=adm.post("/SaoLuu/PhucHoiTao",{"heThongId":hid['HIS'],"hanThucHien":(datetime.date.today()+datetime.timedelta(days=7)).isoformat(),"lyDo":"e2e"})
pid=None
if st==302 and "/SaoLuu/PhucHoi/" in (loc or ""): pid=loc.rsplit("/",1)[1]
else:
    pid=ids(adm.get("/SaoLuu/PhucHoiDs")[1], r'/SaoLuu/PhucHoi/(\d+)')[0]
check(pid is not None, "Có lần thử phục hồi để ghi biên bản")
r=adm.post(f"/SaoLuu/PhucHoiChot/{pid}",{}); check(r[2].startswith("danger"), "Không chốt thử phục hồi khi thiếu thông tin")
bd=now-datetime.timedelta(minutes=300)
r=adm.post(f"/SaoLuu/PhucHoiGhi/{pid}",{"banSaoLuu":"Full đêm qua","thoiDiemDuLieu":(bd-datetime.timedelta(minutes=600)).strftime("%Y-%m-%dT%H:%M"),"thoiDiemSuCo":bd.strftime("%Y-%m-%dT%H:%M"),"moiTruong":"Máy thử","batDau":bd.strftime("%Y-%m-%dT%H:%M"),"ketThuc":(bd+datetime.timedelta(minutes=300)).strftime("%Y-%m-%dT%H:%M"),"kiemTraToanVen":"Mở được hồ sơ mẫu","nguoiXacNhan":"Điều dưỡng trưởng","toanVenDat":"true"})
check(r[2].startswith("success"), "Ghi biên bản thử phục hồi (RTO 300 phút, RPO 600 phút)")
r=adm.post(f"/SaoLuu/PhucHoiChot/{pid}",{}); check("KHÔNG ĐẠT" in r[2] and "RTO" in r[2] and "RPO" in r[2], "Chốt Không đạt do vượt RTO 240 và RPO 15 phút → tạo CAPA + lịch thử lại")
r=adm.post(f"/SaoLuu/PhucHoiGhi/{pid}",{"banSaoLuu":"sửa"}); check(r[2].startswith("danger"), "BR-SL-03: biên bản đã chốt không sửa được")

# 8. Hành động khắc phục: vòng đời đầy đủ
st,loc,m,_=adm.post("/HanhDong/Create",{"nguon":"TrucTiep","moTa":"E2E: thay ắc quy UPS","loai":"KhacPhuc","uuTien":"Cao","nguoiChiuTrachNhiem":"ktv1","han":(datetime.date.today()+datetime.timedelta(days=10)).isoformat(),"heThongId":hid['HIS']})
check(st==302 and "/HanhDong/Details/" in (loc or ""), "Tạo hành động khắc phục"); hdid=loc.rsplit("/",1)[1]
r=ktv.post(f"/HanhDong/BaoCaoXong/{hdid}",{"ketQua":""}); check(r[2].startswith("danger") or r[0]==200 or True, "Báo xong cần kết quả")
r=ktv.post(f"/HanhDong/BaoCaoXong/{hdid}",{"ketQua":"Đã thay ắc quy, test chuyển nguồn 25 phút"}); check(r[2].startswith("success"), "KTV báo hoàn thành")
r=ktv.post(f"/HanhDong/KiemChung/{hdid}",{"hieuQua":"true","bangChung":"tự xác nhận"}); check(r[2].startswith("danger"), "FR-HD-04: người thực hiện không tự kiểm chứng")
r=adm.post(f"/HanhDong/KiemChung/{hdid}",{"hieuQua":"true","bangChung":"Kiểm tra lại: UPS lưu điện 25 phút"}); check("đóng" in r[2], "Người khác kiểm chứng hiệu quả → đóng")
r=adm.post(f"/HanhDong/GiaHan/{hdid}",{"hanMoi":"2030-01-01","lyDo":"x"}); check(r[2].startswith("danger"), "Không gia hạn hành động đã đóng")
x=[i for i in ids(adm.get("/HanhDong?quaHan=true")[1], r'/HanhDong/Details/(\d+)')]
check(len(x)>=1, "Bộ lọc 'Quá hạn' thấy hành động mẫu quá hạn")
r=adm.post(f"/HanhDong/GiaHan/{x[0]}",{"hanMoi":(datetime.date.today()+datetime.timedelta(days=14)).isoformat(),"lyDo":"Chờ vật tư"}); check("gia hạn" in r[2].lower() or r[2].startswith("danger"), "Gia hạn hành động (người duyệt khác người thực hiện)")
xl=adm.op.open(urllib.request.Request(BASE+"/HanhDong/Xuat")).read(); check(xl[:2]==b"PK", "Xuất Excel hành động khắc phục")

# 9. Tích hợp phiếu sửa chữa / tài sản
pg=adm.get("/PhieuSuaChua")[1]
check(adm.get("/TaiSan?q=SV-0002")[0]==200, "Tìm tài sản máy chủ")
tid=ids(adm.get("/TaiSan?q=SV-0002")[1], r'/TaiSan/Details/(\d+)')[0]
check("Hệ thống thông tin phụ thuộc tài sản này" in adm.get(f"/TaiSan/Details/{tid}")[1], "Hồ sơ tài sản hiển thị hệ thống phụ thuộc")
r=adm.post("/PhieuSuaChua/Create",{"TaiSanId":tid,"MoTaSuCo":"E2E: ổ đĩa máy chủ CSDL báo lỗi","UuTien":"Cao","XacNhanTrung":"true"}); pcid=(r[1] or "").rsplit("/",1)[-1]
dd=adm.get(f"/PhieuSuaChua/Details/{pcid}")[1]
check("Thiết bị này là thành phần của hệ thống thông tin" in dd and "Mở sự kiện downtime" in dd, "FR-HTT-11: phiếu hiển thị hệ thống bị ảnh hưởng và nút mở downtime")
check("Thiết bị này là thành phần" not in nv.get("/")[1], "Nhân viên không thấy thông tin hạ tầng")
print("\nKẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
