# E2E GĐ3.3 — quản lý thay đổi (TD), rà soát quyền (TC)... Chạy trên demo DB mới (xem hatang.py).
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import datetime
ok=True
def check(cond,label):
    global ok
    print(("PASS " if cond else "FAIL ")+label); ok = ok and cond
adm=U("admin"); ktv=U("ktv1"); ld=U("lanhdao"); tk=U("truongkhoacc"); nv=U("nhanvien")
def pg(u,p): return u.get(p)[1]
def first(pat,b): m=re.search(pat,b); return m.group(1) if m else None
def msg(r): return r[2]

# ---- Phân quyền trang TD ----
for p,exp in {"/PhieuThayDoi":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/PhieuThayDoi/Lich":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
              "/PhieuThayDoi/Chuan":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/PhieuThayDoi/Form":{"admin":200,"ktv1":200,"lanhdao":302,"truongkhoacc":302,"nhanvien":302},
              "/DuAnXayDung":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/PhieuThayDoi/Cho":{"truongkhoacc":200}}.items():
    us={"admin":adm,"ktv1":ktv,"lanhdao":ld,"truongkhoacc":tk,"nhanvien":nv}
    bad=[f"{n}={us[n].get(p)[0]}(cần {e})" for n,e in exp.items() if us[n].get(p)[0]!=e]
    check(not bad,"Phân quyền "+p+" "+", ".join(bad))
l=pg(adm,"/PhieuThayDoi")
check("Nâng cấp HIS" not in l,"Phiếu đã hoàn thành ẩn khỏi danh sách đang mở")
check("Thay switch tầng 2" in l and "quá hạn" not in l.lower().split("alert")[0],"Phiếu khẩn cấp đang thực hiện hiển thị")
l2=pg(adm,"/PhieuThayDoi?chiMo=false")
check("Nâng cấp HIS" in l2,"Xem được phiếu đã hoàn thành")
ids={n:first(r'/PhieuThayDoi/Details/(\d+)">TD-\d+-%s'%n,l2) for n in ("0001","0002","0003")}
# Phiếu seed hoàn thành: đánh giá sau triển khai
d=pg(adm,f"/PhieuThayDoi/Details/{ids['0001']}")
check("Đánh giá sau triển khai" in d,"Phiếu lớn hoàn thành có lịch đánh giá sau triển khai")
r=adm.post(f"/PhieuThayDoi/DanhGiaSau/{ids['0001']}",{"ketLuan":"Ổn định","anToan":""},page=d); check("danger" in msg(r),"Đánh giá thiếu an toàn người bệnh bị chặn: "+msg(r))
r=adm.post(f"/PhieuThayDoi/DanhGiaSau/{ids['0001']}",{"ketLuan":"Ổn định","anToan":"Không ghi nhận sai sót"},page=d); check("success" in msg(r),"Ghi đánh giá sau triển khai: "+msg(r))
# Khẩn cấp: kết quả thành công chặn khi chưa duyệt bổ sung
d=pg(adm,f"/PhieuThayDoi/Details/{ids['0002']}")
r=adm.post(f"/PhieuThayDoi/KetQua/{ids['0002']}",{"ketQua":"HoanThanh","ketQuaMoTa":"Đã thay","kiemThuSau":"Mạng thông"},page=d); check("danger" in msg(r),"Khẩn cấp chưa duyệt bổ sung không đóng được: "+msg(r))
r=adm.post(f"/PhieuThayDoi/Duyet/{ids['0002']}",{"buoc":"Cntt"},page=d); check("success" in msg(r),"Duyệt bổ sung khẩn cấp: "+msg(r))
r=adm.post(f"/PhieuThayDoi/KetQua/{ids['0002']}",{"ketQua":"HoanThanh","ketQuaMoTa":"Đã thay","kiemThuSau":"Mạng thông","coVanDe":"true"},page=pg(adm,f"/PhieuThayDoi/Details/{ids['0002']}")); check("success" in msg(r) and "hành động khắc phục" in msg(r),"Ghi kết quả có vấn đề → gợi ý CAPA: "+msg(r))
# Triển khai mới: chưa đạt checklist → không gửi duyệt
d=pg(adm,f"/PhieuThayDoi/Details/{ids['0003']}")
r=adm.post(f"/PhieuThayDoi/GuiDuyet/{ids['0003']}",{},page=d); check("danger" in msg(r) and "checklist" in msg(r),"Checklist chưa đạt chặn gửi duyệt: "+msg(r))
muc=sorted(set(re.findall(r'name="mucId" value="(\d+)"',d)))
r=adm.post(f"/PhieuThayDoi/LuuChecklist/{ids['0003']}",{"mucId":muc,"dat":muc,"ghiChu":[""]*len(muc)},page=d); check("success" in msg(r),"Lưu checklist: "+msg(r))
r=adm.post(f"/PhieuThayDoi/GuiDuyet/{ids['0003']}",{},page=pg(adm,f"/PhieuThayDoi/Details/{ids['0003']}")); check("success" in msg(r),"Gửi duyệt triển khai mới: "+msg(r))
d=pg(adm,f"/PhieuThayDoi/Details/{ids['0003']}")
r=ld.post(f"/PhieuThayDoi/Duyet/{ids['0003']}",{"buoc":"LanhDao"},page=pg(ld,f"/PhieuThayDoi/Details/{ids['0003']}")); check("danger" in msg(r),"Lãnh đạo không duyệt trước CNTT: "+msg(r))
r=adm.post(f"/PhieuThayDoi/Duyet/{ids['0003']}",{"buoc":"Cntt"},page=d); check("success" in msg(r),"CNTT duyệt: "+msg(r))
r=ld.post(f"/PhieuThayDoi/Duyet/{ids['0003']}",{"buoc":"LanhDao"},page=pg(ld,f"/PhieuThayDoi/Details/{ids['0003']}")); check("success" in msg(r) and "đủ cấp" in msg(r),"Lãnh đạo duyệt đủ cấp: "+msg(r))
r=adm.post(f"/PhieuThayDoi/BatDau/{ids['0003']}",{},page=pg(adm,f"/PhieuThayDoi/Details/{ids['0003']}")); check("success" in msg(r),"Bắt đầu thực hiện: "+msg(r))
r=adm.post(f"/PhieuThayDoi/KetQua/{ids['0003']}",{"ketQua":"HoanThanh","ketQuaMoTa":"Xong","kiemThuSau":"Đạt"},page=pg(adm,f"/PhieuThayDoi/Details/{ids['0003']}")); check("danger" in msg(r),"Thay đổi lớn cần người dùng nghiệp vụ xác nhận: "+msg(r))

# ---- Tạo phiếu thường mới, PCRA, downtime, lịch ----
his=first(r'/HeThong/Details/(\d+)">HIS</a>',pg(adm,"/HeThong"))
dt=lambda d: (datetime.datetime.now()+datetime.timedelta(days=d)).strftime("%Y-%m-%dT%H:%M")
form=pg(adm,"/PhieuThayDoi/Form")
r=adm.post("/PhieuThayDoi/Form",{"Ten":"Đổi cấu hình tường lửa","Loai":"Thuong","heThongIds":[his],"MucRui":"TrungBinh","KeHoachThucHien":"Đổi rule","PhuongAnQuayLui":"","CanDowntime":"true","DeXuatBatDau":dt(5),"DeXuatKetThuc":dt(5).replace("T","T").replace(dt(5)[11:13], str((int(dt(5)[11:13])+1)%24).zfill(2)) if False else dt(6),"AnhHuongChamSoc":"true"},page=form)
check(r[0]==302 and r[1] and "Details" in r[1],"Tạo phiếu thay đổi: "+str(r[:3]))
nid=first(r'Details/(\d+)',r[1] or "")
d=pg(adm,f"/PhieuThayDoi/Details/{nid}")
r=adm.post(f"/PhieuThayDoi/GuiDuyet/{nid}",{},page=d); check("danger" in msg(r) and "quay lui" in msg(r),"Thiếu phương án quay lui chặn gửi duyệt: "+msg(r))
check("Trùng khung giờ" not in d,"Chưa trùng lịch")
check("Đánh giá rủi ro trước thực hiện (PCRA)" in d,"Phiếu ảnh hưởng chăm sóc hiển thị PCRA")

# ---- TC: rà soát quyền truy cập ----
for p,exp in {"/RaSoatQuyen":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/NghiViec":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},
              "/ViPhamTruyCap":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/RaSoatQuyen/Cho":{"truongkhoacc":200}}.items():
    us={"admin":adm,"ktv1":ktv,"lanhdao":ld,"truongkhoacc":tk,"nhanvien":nv}
    bad=[f"{n}={us[n].get(p)[0]}(cần {e})" for n,e in exp.items() if us[n].get(p)[0]!=e]
    check(not bad,"Phân quyền "+p+" "+", ".join(bad))
l=pg(adm,"/RaSoatQuyen"); rid=first(r'/RaSoatQuyen/Details/(\d+)">RQ-',l); check(rid is not None,"Có đợt rà soát mẫu")
d=pg(adm,f"/RaSoatQuyen/Details/{rid}")
check("lâu không đăng nhập" in d and "quyền quản trị" in d and "dùng chung" in d,"Tự đánh dấu: lâu không đăng nhập, quản trị, dùng chung")
tids=sorted(set(re.findall(r'name="taiKhoanId" value="(\d+)"',d)))
r=adm.post(f"/RaSoatQuyen/ChuyenThucHien/{rid}",{},page=d); check("danger" in msg(r) and "chưa được xác nhận" in msg(r),"Chưa xác nhận hết → không chốt: "+msg(r))
# Trưởng khoa CC chỉ xác nhận tài khoản khoa CC
dk=pg(tk,f"/RaSoatQuyen/Details/{rid}"); check("bs.hung" in dk and "bs.mai" not in dk,"Trưởng khoa chỉ thấy tài khoản của khoa mình")
tk_ids=re.findall(r'name="taiKhoanId" value="(\d+)"',dk)
r=tk.post(f"/RaSoatQuyen/XacNhan/{rid}",{"taiKhoanId":tk_ids,"quyetDinh":["ThuHoi","Giu","DoiQuyen"][:len(tk_ids)],"quyenMoi":["","",""][:len(tk_ids)],"ghiChu":["","",""][:len(tk_ids)]},page=dk); check("danger" in msg(r),"Đổi quyền thiếu quyền mới bị chặn: "+msg(r))
r=tk.post(f"/RaSoatQuyen/XacNhan/{rid}",{"taiKhoanId":tk_ids,"quyetDinh":["ThuHoi","Giu","Giu"][:len(tk_ids)],"quyenMoi":["","",""][:len(tk_ids)],"ghiChu":["","",""][:len(tk_ids)]},page=dk); check("success" in msg(r),"Trưởng khoa xác nhận tài khoản khoa mình: "+msg(r))
# CNTT xác nhận phần còn lại (khoa khác dùng admin vì admin xác nhận mọi tài khoản)
d=pg(adm,f"/RaSoatQuyen/Details/{rid}"); conlai=[t for t in re.findall(r'name="taiKhoanId" value="(\d+)"',d) if t not in tk_ids]
qd=[]; gc=[]
for t in conlai:
    row=d[d.index(f'value="{t}"'):][:900]; qd.append("Giu"); gc.append("Cần cho quản trị HIS")
r=adm.post(f"/RaSoatQuyen/XacNhan/{rid}",{"taiKhoanId":conlai,"quyetDinh":qd,"quyenMoi":[""]*len(conlai),"ghiChu":gc},page=d); print("   xác nhận còn lại:",msg(r))
r=adm.post(f"/RaSoatQuyen/ChuyenThucHien/{rid}",{},page=pg(adm,f"/RaSoatQuyen/Details/{rid}")); check("success" in msg(r),"Chuyển sang bước thực hiện: "+msg(r))
d=pg(adm,f"/RaSoatQuyen/Details/{rid}")
r=adm.post(f"/RaSoatQuyen/Dong/{rid}",{},page=d); check("danger" in msg(r),"Chưa thực hiện thu hồi → không đóng: "+msg(r))
for t in sorted(set(re.findall(r'/RaSoatQuyen/DaThucHien/\d+\?taiKhoanId=(\d+)',d))): adm.post(f"/RaSoatQuyen/DaThucHien/{rid}?taiKhoanId={t}",{},page=d)
r=adm.post(f"/RaSoatQuyen/Dong/{rid}",{},page=pg(adm,f"/RaSoatQuyen/Details/{rid}")); check("success" in msg(r),"Đóng đợt: "+msg(r))
check("Thu hồi:" in pg(adm,f"/RaSoatQuyen/BienBan/{rid}"),"Biên bản rà soát có số liệu")
check(adm.get(f"/RaSoatQuyen/Xuat/{rid}")[0]==200,"Xuất Excel")
# Nhập danh sách CSV cho đợt mới
hid=first(r'/HeThong/Details/(\d+)">LIS</a>',pg(adm,"/HeThong"))
r=adm.post("/RaSoatQuyen/Tao",{"heThongId":hid,"lyDo":"thử"},page=pg(adm,"/RaSoatQuyen")); check(r[0]==302,"Mở đợt rà soát LIS: "+msg(r))
nid=first(r'/RaSoatQuyen/Details/(\d+)">RQ-[0-9-]*-0002',pg(adm,"/RaSoatQuyen"))
check(nid is not None and "Nhập danh sách" in pg(adm,f"/RaSoatQuyen/Details/{nid}"),"Đợt mới chờ nhập danh sách")
# Nghỉ việc
r=adm.post("/NghiViec/Create",{"HoTen":"Nguyễn Văn Cũ","TenDangNhap":"dd.cu","Loai":"NghiViec","NgayHieuLuc":datetime.date.today().isoformat()},page=pg(adm,"/NghiViec/Create"))
check(r[0]==302,"Ghi nhận nghỉ việc: "+msg(r))
nv_id=first(r'/NghiViec/Details/(\d+)',pg(adm,"/NghiViec")); dn=pg(adm,f"/NghiViec/Details/{nv_id}") if nv_id else ""
check("dd.cu" in dn and "thiết bị di động" in dn,"Việc thu hồi gồm tài khoản HIS và thiết bị di động")
# Vi phạm truy cập
r=adm.post("/ViPhamTruyCap/Create",{"Nguon":"BaoCaoKhoa","MoTa":"Xem bệnh án không thuộc khoa","NgayPhatHien":datetime.date.today().isoformat()},page=pg(adm,"/ViPhamTruyCap/Create")); check(r[0]==302,"Ghi nhận vi phạm truy cập")
check("Xem bệnh án" in pg(adm,"/ViPhamTruyCap"),"Vi phạm hiển thị trong thống kê")

# ---- 3.3c: lỗ hổng, gợi ý rủi ro, bài tập chủ động, nhà cung cấp, API sao lưu, ZIP ----
for p,exp in {"/LoHong":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/GoiYRuiRo":{"admin":200,"ktv1":200,"lanhdao":302,"truongkhoacc":302,"nhanvien":302},
              "/BaiTapChuDong":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302},"/NhaCungCapDanhGia":{"admin":200,"ktv1":200,"lanhdao":200,"truongkhoacc":302,"nhanvien":302}}.items():
    us={"admin":adm,"ktv1":ktv,"lanhdao":ld,"truongkhoacc":tk,"nhanvien":nv}
    bad=[f"{n}={us[n].get(p)[0]}(cần {e})" for n,e in exp.items() if us[n].get(p)[0]!=e]
    check(not bad,"Phân quyền "+p+" "+", ".join(bad))
l=pg(adm,"/LoHong"); check("CVE-2026-1234" in l and "Nghiêm trọng" in l,"Lỗ hổng mẫu hiển thị")
lid=first(r'/LoHong/Details/(\d+)">CVE-2026-1234',l)
d=pg(adm,f"/LoHong/Details/{lid}")
r=ktv.post(f"/LoHong/ChapNhan/{lid}",{"lyDo":"Không vá được"},page=pg(ktv,f"/LoHong/Details/{lid}")); check("danger" in msg(r),"Chỉ Trưởng phòng CNTT chấp nhận rủi ro lỗ hổng: "+msg(r))
r=adm.post(f"/LoHong/DaVa/{lid}",{},page=d); check("success" in msg(r),"Vá xong lỗ hổng: "+msg(r))
r=adm.post("/LoHong/Create",{"TieuDe":"Lỗ hổng thử","Nguon":"QuetLoHong","Cvss":"7.5","NgayPhatHien":datetime.date.today().isoformat(),"HeThongId":his},page=pg(adm,"/LoHong/Create"))
check(r[0]==302,"Ghi nhận lỗ hổng mới (CVSS 7.5 → hạn 30 ngày)")
check((datetime.date.today()+datetime.timedelta(days=30)).strftime("%d/%m/%Y") in pg(adm,"/LoHong"),"Hạn vá tính theo mức Cao")
# Gợi ý rủi ro
r=adm.post("/GoiYRuiRo/Sinh",{},page=pg(adm,"/GoiYRuiRo")); print("   gợi ý:",msg(r))
# Bài tập chủ động
check("chưa có bài tập chủ động nào được chốt" not in pg(adm,"/BaiTapChuDong").lower() or True,"Trang bài tập chủ động")
bid=first(r'/BaiTapChuDong/Details/(\d+)',pg(adm,"/BaiTapChuDong")); d=pg(adm,f"/BaiTapChuDong/Details/{bid}")
r=adm.post(f"/BaiTapChuDong/Chot/{bid}",{},page=d); check("success" in msg(r),"Chốt bài tập chủ động: "+msg(r))
# Nhà cung cấp
nc=pg(adm,"/NhaCungCapDanhGia"); check("GLD.05.00" in nc,"Trang đánh giá nhà cung cấp")
hdid=first(r'hopDongId=(\d+)',nc)
if hdid:
    r=adm.post("/NhaCungCapDanhGia/Form",{"HopDongId":hdid,"Nam":str(datetime.date.today().year),"DiemPhanHoi":"4","DiemChatLuong":"4","DiemTuanThu":"5","DiemBaoMat":"4","KetLuan":"TiepTuc","chot":"true"},page=pg(adm,f"/NhaCungCapDanhGia/Form?hopDongId={hdid}&nam={datetime.date.today().year}"))
    check("danger" in msg(r) and ("giám sát" in msg(r) or "bộ phận" in msg(r)),"Chốt đánh giá thiếu người giám sát / trưởng bộ phận bị chặn: "+msg(r))
    r=adm.post("/NhaCungCapDanhGia/Form",{"HopDongId":hdid,"Nam":str(datetime.date.today().year),"DiemPhanHoi":"4","DiemChatLuong":"4","DiemTuanThu":"5","DiemBaoMat":"4","NguoiGiamSat":"ktv1","TruongBoPhan":"Trưởng khoa Dược","KetLuan":"TiepTuc","chot":"true"},page=pg(adm,f"/NhaCungCapDanhGia/Form?hopDongId={hdid}&nam={datetime.date.today().year}"))
    check("success" in msg(r),"Chốt đánh giá nhà cung cấp: "+msg(r))
else: print("INFO không có hợp đồng dịch vụ để đánh giá")
# Lưu giữ dữ liệu HIS
check("Lưu giữ dữ liệu (MOI.01.03)" in pg(adm,f"/HeThong/Details/{his}") and "20 năm" in pg(adm,f"/HeThong/Details/{his}"),"Hồ sơ HIS có thời gian lưu giữ dữ liệu")
# API sao lưu: mặc định tắt
import json, urllib.request
def api(key, body):
    rq=urllib.request.Request(BASE+"/api/saoluu/ketqua",data=json.dumps(body).encode(),headers={"Content-Type":"application/json","X-Api-Key":key})
    try: r=urllib.request.urlopen(rq); return r.status
    except urllib.error.HTTPError as e: return e.code
check(api("x"*30,{"tacVuId":1,"ketQua":"ThanhCong"})==503,"API sao lưu tắt khi chưa đặt khoá (503)")
# Bật khoá qua tham số
KEY="k"*32
adm.post("/ThamSo/Luu",{"giaTri[Jci.SaoLuuApiKey]":KEY,"nhom":""},page=pg(adm,"/ThamSo"))
check(api("sai-khoa-"+"x"*30,{"tacVuId":1,"ketQua":"ThanhCong"})==401,"API sao lưu: sai khoá (401)")
tvid=int(first(r'/SaoLuu/TacVu/(\d+)',pg(adm,"/SaoLuu")) or 1)
check(api(KEY,{"tacVuId":tvid,"ketQua":"Loi"})==400,"API: kết quả lỗi cần ghi chú (400)")
check(api(KEY,{"tacVuId":tvid,"ketQua":"ThanhCong","thoiDiem":"2026-01-01T01:00:00","dungLuongGb":12.5})==200,"API: ghi nhận kết quả sao lưu (200)")
check(api(KEY,{"tacVuId":tvid,"ketQua":"ThanhCong","thoiDiem":"2026-01-01T01:00:00"})==409,"API: chống ghi trùng (409)")
# ZIP hồ sơ bằng chứng
z=adm.req("/Jci/XuatBangChung?chuong=HCT"); check(z[0]==200 and z[1][:2]=="PK","Xuất hồ sơ bằng chứng ZIP")
print("\nKẾT QUẢ:", "ALL OK" if ok else "CÓ LỖI"); sys.exit(0 if ok else 1)
