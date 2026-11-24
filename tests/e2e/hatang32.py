# E2E GĐ3.2 (HCT.01.00-05, an ninh mạng, rủi ro, JCI) — chạy trên demo DB mới (xem hatang.py).
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
ok=True
def check(cond,label):
    global ok
    print(("PASS " if cond else "FAIL ")+label); ok = ok and cond
adm=U("admin"); ktv=U("ktv1"); ld=U("lanhdao"); nv=U("nhanvien")
def pg(u,p): return u.get(p)[1]
def first(pat,body): m=re.search(pat,body); return m.group(1) if m else None

# Phân quyền
check(adm.get("/AnNinh")[0]==200 and ktv.get("/AnNinh")[0]!=200 and nv.get("/AnNinh")[0]!=200, "Sự cố an ninh: chỉ nhóm ATTT/lãnh đạo xem danh sách")
# 1. Người dùng báo sự cố an ninh
r=nv.post("/AnNinh/Bao",{"moTa":"Nhận email lạ yêu cầu cung cấp mật khẩu","loai":"Phishing"},page=pg(nv,"/AnNinh/Bao"))
check(r[0]==302,"Nhân viên báo sự cố ATTT")
l=pg(adm,"/AnNinh"); id_=first(r'/AnNinh/Details/(\d+)',l)
check("AN-" in l and id_ is not None,"Sự cố hiện ở danh sách ATTT")
d=pg(adm,f"/AnNinh/Details/{id_}")
# Phân loại mức độ, ghi bước, đóng
r=adm.post(f"/AnNinh/PhanMuc/{id_}",{"mucDo":"ThongThuong","loai":"Phishing","lienQuan":"khong"},page=d)
check("success" in r[2],"Phân loại sự cố: "+r[2])
r=adm.post(f"/AnNinh/Dong/{id_}",{},page=d); check("danger" in r[2],"Chưa rút kinh nghiệm → không đóng được: "+r[2])
adm.post(f"/AnNinh/RutKinhNghiem/{id_}",{"nguyenNhan":"Thiếu nhận biết","baiHoc":"Tăng đào tạo"},page=d)
for b in ("PhatHien","NganChan"): adm.post(f"/AnNinh/GhiBuoc/{id_}",{"buoc":b,"noiDung":"Đã xử lý","hoanThanh":"true"},page=d)
d=pg(adm,f"/AnNinh/Details/{id_}")
r=adm.post(f"/AnNinh/Dong/{id_}",{},page=d); check("success" in r[2],"Đóng sự cố sau rút kinh nghiệm: "+r[2])
# Nhân viên không xem được chi tiết sự cố
check(nv.get(f"/AnNinh/Details/{id_}")[0]!=200,"Nhân viên không xem chi tiết sự cố ATTT")

# 2. Rủi ro
l=pg(adm,"/RuiRo"); check("RR-" in l,"Sổ rủi ro có dữ liệu mẫu")
rid=first(r'/RuiRo/Details/(\d+)',l); d=pg(adm,f"/RuiRo/Details/{rid}")
r=adm.post(f"/RuiRo/ChamLai/{rid}",{"khaNang":"2","hauQua":"4","lyDo":""},page=d); check("danger" in r[2] or r[0]==302,"Chấm lại điểm không lý do: "+r[2])
r=adm.post(f"/RuiRo/ChamLai/{rid}",{"khaNang":"2","hauQua":"4","lyDo":"Đã thay ắc quy"},page=d); check("success" in r[2],"Chấm lại điểm có lý do: "+r[2])
r=adm.post("/RuiRo/Form",{"MoTa":"Thiếu điều hòa phòng máy chủ","Nhom":"DienDieuHoa","KhaNang":"3","HauQua":"4","NguoiChiuTrachNhiem":"ktv1"},page=pg(adm,"/RuiRo/Form"))
check(r[0]==302,"Tạo rủi ro mới: "+r[2])

# 3. Đợt đánh giá rủi ro hằng năm
nam=str(__import__('datetime').date.today().year)
r=adm.post("/DotRuiRo/Tao",{"nam":nam},page=pg(adm,"/DotRuiRo"))
l=pg(adm,"/DotRuiRo"); did=first(r'/DotRuiRo/Details/(\d+)',l); check(did is not None,"Tạo đợt đánh giá rủi ro năm "+nam)
d=pg(adm,f"/DotRuiRo/Details/{did}")
r=adm.post(f"/DotRuiRo/Chot/{did}",{},page=d); check("danger" in r[2],"Chưa chấm lại hết → không chốt: "+r[2])
for rid2 in sorted(set(re.findall(r'name="ruiRoId" value="(\d+)"',d))): adm.post(f"/DotRuiRo/ChamLai/{did}",{"ruiRoId":rid2,"khaNang":"2","hauQua":"3","lyDo":"Rà soát"},page=d)
d=pg(adm,f"/DotRuiRo/Details/{did}")
r=adm.post(f"/DotRuiRo/Chot/{did}",{},page=d); check("success" in r[2],"Chốt đợt đánh giá rủi ro: "+r[2])
d=pg(adm,f"/DotRuiRo/Details/{did}")

# 4. Tài liệu: chính sách nháp → gửi duyệt → duyệt 2 cấp
l=pg(adm,"/TaiLieu"); check("Chính sách an ninh mạng" in l,"Tài liệu mẫu hiển thị")
tid=[i for i in re.findall(r'/TaiLieu/Details/(\d+)',l)]
nhap=None
for i in tid:
    if "Nháp" in pg(adm,f"/TaiLieu/Details/{i}"): nhap=i
check(nhap is not None,"Có tài liệu nháp")
d=pg(adm,f"/TaiLieu/Details/{nhap}")
r=adm.post(f"/TaiLieu/GuiDuyet/{nhap}",{},page=d); print("   gửi duyệt:",r[2])
r=adm.post(f"/TaiLieu/Duyet/{nhap}",{},page=pg(adm,f"/TaiLieu/Details/{nhap}")); check("success" in r[2],"Trưởng phòng CNTT duyệt cấp 1: "+r[2])
r=ld.post(f"/TaiLieu/Duyet/{nhap}",{},page=pg(ld,f"/TaiLieu/Details/{nhap}")); check("success" in r[2],"Lãnh đạo duyệt cấp 2: "+r[2])
check("Hiệu lực" in pg(adm,f"/TaiLieu/Details/{nhap}"),"Tài liệu chuyển Hiệu lực")

# 5. Đào tạo, JCI, báo cáo lãnh đạo
check("Downtime" in pg(adm,"/DaoTao") or "downtime" in pg(adm,"/DaoTao"),"Đào tạo có dữ liệu mẫu")
check(adm.get("/DaoTao/XuatNhanVien")[0] in (200,302),"Xuất Excel đào tạo")
t=pg(adm,"/Jci/TieuChuan"); check("HCT.01.04" in t and "FMS.08.01" in t,"Danh mục tiêu chuẩn JCI")
check("HCT.01.05" in t,"Có HCT.01.05")
check(adm.get("/Jci/ChiSo")[0]==200,"Chỉ số KPI JCI")
jid=first(r'/HeThong/Details/(\d+)',pg(adm,"/HeThong")); tr=adm.get(f"/Jci/Tracer/{jid}"); check(tr[0]==200 and "Tracer" in pg(adm,f"/HeThong/Details/{jid}"),"Tracer theo hệ thống")
q=(__import__('datetime').date.today().month-1)//3+1
r=adm.post("/BaoCaoLanhDao/Tao",{"nam":nam,"quy":str(q)},page=pg(adm,"/BaoCaoLanhDao"))
l=pg(adm,"/BaoCaoLanhDao"); bid=first(r'/BaoCaoLanhDao/Details/(\d+)',l); check(bid is not None,"Tạo báo cáo quý cho lãnh đạo")
if bid:
    d=pg(adm,f"/BaoCaoLanhDao/Details/{bid}")
    r=adm.post(f"/BaoCaoLanhDao/Gui/{bid}",{"nhanXet":"Tình hình ổn định"},page=d); check("success" in r[2],"Gửi báo cáo: "+r[2])
    r=ld.post(f"/BaoCaoLanhDao/XacNhan/{bid}",{"yKien":"Đã xem"},page=pg(ld,f"/BaoCaoLanhDao/Details/{bid}")); check("success" in r[2],"Lãnh đạo xác nhận đã xem: "+r[2])
    check(adm.get(f"/BaoCaoLanhDao/In/{bid}")[0]==200,"In báo cáo")

# 6. Trang hệ thống telehealth/AI
h=pg(adm,"/HeThong"); check("TELE" in h and "AI-NKH" in h,"Hệ thống telehealth và AI hiển thị")
print("\nKẾT QUẢ:", "ALL OK" if ok else "CÓ LỖI"); sys.exit(0 if ok else 1)
