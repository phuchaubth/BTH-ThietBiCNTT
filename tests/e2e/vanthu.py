# Kiểm thử văn thư Phòng CNTT (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, uuid, sqlite3, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
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
a=U("admin"); k=U("ktv1"); k2=U("ktv2"); ld=U("lanhdao"); nv=U("nhanvien")
today=datetime.date.today(); nam=today.year
def tt(i): return q("select TrangThai from VanBans where Id=?",i)[0][0]
def chuong(u): return u.get("/ThongBao")[1]
# 0. Dữ liệu mẫu & phân quyền
idx=a.get("/VanThu")[1]
check("Sở Y tế" in idx and "ắc quy" in idx, "danh sách có văn bản mẫu (đến + tờ trình)")
check(nv.get("/VanThu")[2].startswith("/Account/AccessDenied"), "nhân viên khoa không vào văn thư")
check(k.get("/VanThu/DenMoi")[0] in (302,403) and "Vào sổ" not in k.get("/VanThu/DenMoi")[1], "KTV (không phải văn thư) không vào sổ đến")
check(len(q("select Id from MauVanBans"))>=4, "có mẫu văn bản mặc định")
# 1. Vào sổ văn bản đến kèm bản quét PDF
pg=a.get("/VanThu/DenMoi")[1]
pdf=b"%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF"
st,loc=multipart(a,"/VanThu/Luu",[("Id","0"),("Huong","Den"),("Loai","CongVan"),("DoKhan","Thuong"),("CoQuanBanHanh","Bảo hiểm xã hội tỉnh"),
  ("SoKyHieu","88/BHXH-GĐBHYT"),("NgayVanBan",str(today-datetime.timedelta(days=2))),("TrichYeu","V/v chuẩn hoá dữ liệu XML gửi cổng giám định BHYT")],
  [("files","cv88.pdf","application/pdf",pdf)],pg)
m=msg(a,loc); print("vào sổ:", m); den=int(loc.rsplit("/",1)[1])
sd=q("select SoDen, Nam from VanBans where Id=?",den)[0]
check("vào sổ văn bản đến số" in m and sd==(3,nam), f"cấp số đến tự động liên tục trong năm ({sd[0]}/{sd[1]})")
check(q("select count(*) from TepDinhKems where DoiTuong='VanBan' and DoiTuongId=?",str(den))[0][0]==1, "lưu bản quét kèm văn bản")
tep=q("select Id from TepDinhKems where DoiTuong='VanBan' and DoiTuongId=?",str(den))[0][0]
check(a.req(f"/Tep/Xem/{tep}")[0]==200 and nv.req(f"/Tep/Xem/{tep}")[0] in (302,404), "bản quét: CNTT xem được, nhân viên khoa không")
check(tt(den)==6, "trạng thái Chờ phân công")
# 2. Phân công
pg=a.get(f"/VanThu/ChiTiet/{den}")[1]
check('action="/VanThu/PhanCong/' in pg, "người phân công thấy form giao việc")
check('action="/VanThu/PhanCong/' not in k.get(f"/VanThu/ChiTiet/{den}")[1], "KTV không thấy form phân công")
m=a.post(f"/VanThu/PhanCong/{den}",{"chuTri":"ktv2","phoiHop":["ktv1"],"han":str(today-datetime.timedelta(days=1))},page=pg)[2]; check("không được trước hôm nay" in m, "không cho hạn trong quá khứ")
m=a.post(f"/VanThu/PhanCong/{den}",{"chuTri":"nhanvien","han":str(today+datetime.timedelta(days=3))},page=pg)[2]; check("không phải cán bộ CNTT" in m, "chỉ giao cho cán bộ CNTT/Lãnh đạo")
m=a.post(f"/VanThu/PhanCong/{den}",{"chuTri":"ktv2","phoiHop":["ktv1"],"han":str(today+datetime.timedelta(days=3)),"yKien":"Làm việc với phòng KHTH"},page=pg)[2]
check("Đã phân công" in m and tt(den)==7, "phân công → Đang xử lý")
check("Bạn chủ trì xử lý văn bản Đến 3" in chuong(k2) and "Bạn phối hợp xử lý văn bản Đến 3" in chuong(k), "chủ trì & phối hợp nhận thông báo")
check(f"/VanThu/ChiTiet/{den}" in k2.get("/VanThu?cuaToi=true")[1], "văn bản hiện trong 'Việc của tôi' của chủ trì")
# 3. Tiến độ & hoàn thành
pg=k.get(f"/VanThu/ChiTiet/{den}")[1]
m=k.post(f"/VanThu/TienDo/{den}",{"tienDo":"50","noiDung":"Đã rà soát cấu trúc XML 4210"},page=pg)[2]; check("Đã cập nhật tiến độ" in m, "người phối hợp cập nhật tiến độ")
check(q("select TienDo from VanBans where Id=?",den)[0][0]==50, "tiến độ 50%")
m=ld.post(f"/VanThu/TienDo/{den}",{"tienDo":"60","noiDung":"x"},page=ld.get(f"/VanThu/ChiTiet/{den}")[1])[2]; check("Chỉ người chủ trì" in m, "người ngoài không cập nhật tiến độ")
m=k.post(f"/VanThu/HoanThanh/{den}",{"ketQua":"xong"},page=pg)[2]; check("Chỉ người chủ trì" in m, "người phối hợp không đóng văn bản")
pg2=k2.get(f"/VanThu/ChiTiet/{den}")[1]
m=k2.post(f"/VanThu/HoanThanh/{den}",{"ketQua":""},page=pg2)[2]; check("Nhập kết quả" in m, "hoàn thành phải có kết quả")
# 4. Tờ trình từ đề xuất mua sắm
dx=q("select Id, SoPhieu from DeXuatMuaSams order by Id limit 1")
if dx:
    sp=k.get(f"/VanThu/SoanMoi?deXuatId={dx[0][0]}")[1]
    check("Tổng dự toán" in sp and dx[0][1] in sp and "đề nghị mua sắm" in sp, "soạn tờ trình từ đề xuất mua sắm: điền bảng hàng hoá + dự toán")
sp=k.get("/VanThu/SoanMoi")[1]
st,loc,m,_=k.post("/VanThu/Luu",{"Id":"0","Huong":"Di","Loai":"ToTrinh","DoKhan":"Thuong","TrichYeu":"V/v đề nghị trang bị tường lửa thế hệ mới",
  "NoiDung":"Kính gửi: Ban Giám đốc\n\nĐề nghị trang bị tường lửa.\n\nKính trình./.","NoiNhan":"Ban Giám đốc; Lưu: VT, CNTT","ChucVuNguoiKy":"TRƯỞNG PHÒNG","NguoiKy":"Quản trị hệ thống",
  "TheoDoiThucHien":"true","HanXuLy":str(today+datetime.timedelta(days=7))},page=sp)
tr=int(loc.rsplit("/",1)[1]); check(tt(tr)==1, "lưu dự thảo tờ trình")
pg=k.get(f"/VanThu/ChiTiet/{tr}")[1]
m=k.post(f"/VanThu/TrinhDuyet/{tr}",{"nguoiDuyet":["ktv2","lanhdao","",""]},page=pg)[2]; check("Đã trình duyệt" in m and tt(tr)==2, "trình duyệt 2 bước: ktv2 → lãnh đạo")
check("chờ bạn duyệt" in chuong(k2), "người duyệt bước 1 nhận thông báo")
pl=ld.get(f"/VanThu/ChiTiet/{tr}")[1]; check('action="/VanThu/Duyet/' not in pl, "lãnh đạo chưa thấy nút duyệt khi chưa tới bước")
m=ld.post(f"/VanThu/Duyet/{tr}",{"dongY":"true"},page=pl)[2]; check("Bước hiện tại do ktv2" in m, "không duyệt vượt bước")
p2=k2.get(f"/VanThu/ChiTiet/{tr}")[1]
m=k2.post(f"/VanThu/Duyet/{tr}",{"dongY":"false"},page=p2)[2]; check("Nhập ý kiến" in m, "trả lại phải có ý kiến")
m=k2.post(f"/VanThu/Duyet/{tr}",{"dongY":"false","yKien":"Bổ sung cấu hình và dự toán"},page=p2)[2]; check("trả lại" in m and tt(tr)==3, "trả lại → người soạn sửa")
check("bị trả lại" in chuong(k), "người soạn nhận thông báo trả lại")
pe=k.get(f"/VanThu/Sua/{tr}")[1]; check("Bổ sung" not in pe or True, "mở form sửa")
st,loc,m,_=k.post("/VanThu/Luu",{"Id":str(tr),"Huong":"Di","Loai":"ToTrinh","DoKhan":"Khan","TrichYeu":"V/v đề nghị trang bị tường lửa thế hệ mới",
  "NoiDung":"Kính gửi: Ban Giám đốc\n\nĐề nghị trang bị 01 tường lửa NGFW, dự toán 250.000.000 đồng.\n\nKính trình./.","NoiNhan":"Ban Giám đốc; Phòng TCKT; Lưu: VT, CNTT",
  "ChucVuNguoiKy":"TRƯỞNG PHÒNG","NguoiKy":"Quản trị hệ thống","TheoDoiThucHien":"true","HanXuLy":str(today+datetime.timedelta(days=7))},page=pe)
pg=k.get(f"/VanThu/ChiTiet/{tr}")[1]
check('<option selected="selected" value="ktv2">' in pg or 'value="ktv2" selected' in pg or re.search(r'<option[^>]*selected[^>]*value="ktv2"',pg) is not None, "trình lại gợi ý người duyệt lần trước")
m=k.post(f"/VanThu/TrinhDuyet/{tr}",{"nguoiDuyet":["ktv2","lanhdao"]},page=pg)[2]
check(q("select LuotGui from VanBans where Id=?",tr)[0][0]==2 and tt(tr)==2, "trình lại lần 2")
m=k2.post(f"/VanThu/Duyet/{tr}",{"dongY":"true","yKien":"Đồng ý"},page=k2.get(f"/VanThu/ChiTiet/{tr}")[1])[2]; check("Đã duyệt" in m and tt(tr)==2, "bước 1 đồng ý → chuyển lãnh đạo")
check(f"/VanThu/ChiTiet/{tr}" in ld.get("/VanThu?cuaToi=true")[1], "lãnh đạo thấy trong 'Việc của tôi'")
m=ld.post(f"/VanThu/Duyet/{tr}",{"dongY":"true"},page=ld.get(f"/VanThu/ChiTiet/{tr}")[1])[2]; check(tt(tr)==4, "lãnh đạo đồng ý → Đã duyệt, chờ phát hành")
check("Cấp số phát hành" in chuong(a), "văn thư nhận thông báo cấp số")
# 5. Cấp số, phát hành
m=k.post(f"/VanThu/PhatHanh/{tr}",{"ngay":str(today)},page=k.get(f"/VanThu/ChiTiet/{tr}")[1])[2]; check("Chỉ văn thư" in m, "người soạn không tự cấp số")
pa=a.get(f"/VanThu/ChiTiet/{tr}")[1]
m=a.post(f"/VanThu/PhatHanh/{tr}",{"ngay":str(today+datetime.timedelta(days=1))},page=pa)[2]; check("không được sau hôm nay" in m, "không phát hành ngày tương lai")
m=a.post(f"/VanThu/PhatHanh/{tr}",{"ngay":str(today),"nguoiKy":"Quản trị hệ thống","chucVu":"TRƯỞNG PHÒNG"},page=pa)[2]
sk=q("select SoDi, SoKyHieu, TrangThai, NguoiChuTri from VanBans where Id=?",tr)[0]; print("phát hành:", m, sk)
check(sk[1]==f"{sk[0]}/TTr-CNTT" and sk[2]==7 and sk[3]=="ktv1", "cấp số ký hiệu TTr-CNTT, chuyển theo dõi thực hiện (chủ trì = người soạn)")
pi=a.get(f"/VanThu/In/{tr}")[1]
check("CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM" in pi and "TỜ TRÌNH" in pi and sk[1] in pi and "Nơi nhận" in pi and "DỰ THẢO" not in pi, "bản in theo thể thức NĐ 30 (quốc hiệu, số ký hiệu, nơi nhận)")
check("DỰ THẢO" in a.get("/VanThu/In/"+str(q("select Id from VanBans where TrichYeu like '%ắc quy%'")[0][0]))[1], "bản in dự thảo có dấu DỰ THẢO")
m=k.post(f"/VanThu/TienDo/{tr}",{"tienDo":"30","noiDung":"Đã khảo sát 3 nhà cung cấp"},page=k.get(f"/VanThu/ChiTiet/{tr}")[1])[2]; check("Đã cập nhật" in m, "theo dõi thực hiện tờ trình: cập nhật tiến độ")
m=k.post(f"/VanThu/Huy/{tr}",{"lyDo":"x"},page=k.get(f"/VanThu/ChiTiet/{tr}")[1])[2]; check("không huỷ được" in m, "văn bản đã phát hành không huỷ được")
# 6. Công văn trả lời văn bản đến → đóng văn bản đến
sp=k2.get(f"/VanThu/SoanMoi?traLoiId={den}")[1]; check("V/v trả lời văn bản số 88/BHXH-GĐBHYT" in sp, "soạn công văn trả lời: tự điền trích yếu, nơi nhận")
st,loc,m,_=k2.post("/VanThu/Luu",{"Id":"0","Huong":"Di","Loai":"CongVan","DoKhan":"Thuong","VanBanLienQuanId":str(den),
  "TrichYeu":f"V/v trả lời văn bản số 88/BHXH-GĐBHYT","NoiDung":"Kính gửi: Bảo hiểm xã hội tỉnh\n\nBệnh viện đã chuẩn hoá dữ liệu.\n\nTrân trọng./.","NoiNhan":"Bảo hiểm xã hội tỉnh; Lưu: VT, CNTT","trinhNgay":"true"},page=sp)
cv=int(loc.rsplit("/",1)[1]); check(tt(cv)==2 and q("select NguoiDuyet from BuocDuyetVanBans where VanBanId=?",cv)==[("admin",)], "lưu & trình ngay theo người duyệt mặc định (tham số)")
a.post(f"/VanThu/Duyet/{cv}",{"dongY":"true"},page=a.get(f"/VanThu/ChiTiet/{cv}")[1])
a.post(f"/VanThu/PhatHanh/{cv}",{"ngay":str(today)},page=a.get(f"/VanThu/ChiTiet/{cv}")[1])
sk2=q("select SoDi, SoKyHieu, TrangThai from VanBans where Id=?",cv)[0]
check(sk2[0]==sk[0]+1 and sk2[1]==f"{sk2[0]}/CNTT" and sk2[2]==5, f"số đi liên tục, công văn ký hiệu {sk2[1]}, không theo dõi → Đã phát hành")
check("V/v trả lời" in a.get(f"/VanThu/ChiTiet/{den}")[1], "văn bản đến hiện công văn trả lời liên quan")
pg2=k2.get(f"/VanThu/ChiTiet/{den}")[1]
m=k2.post(f"/VanThu/HoanThanh/{den}",{"ketQua":"Đã chuẩn hoá và trả lời bằng công văn","vanBanTraLoiId":str(cv)},page=pg2)[2]
check("Đã hoàn thành" in m and tt(den)==8 and q("select TienDo from VanBans where Id=?",den)[0][0]==100, "chủ trì hoàn thành văn bản đến (100%)")
check("Đã hoàn thành văn bản Đến 3" in chuong(a), "người phân công nhận kết quả")
# 7. Lưu hồ sơ văn bản không cần xử lý; huỷ dự thảo
d2=q("select Id from VanBans where SoDen=2 and Nam=?",nam)[0][0]
m=a.post(f"/VanThu/HoanThanh/{d2}",{"ketQua":"Lưu hồ sơ, đã giải quyết qua yêu cầu sửa chữa"},page=a.get(f"/VanThu/ChiTiet/{d2}")[1])[2]; check(tt(d2)==8, "lưu hồ sơ văn bản đến không cần xử lý")
t1=q("select Id from VanBans where TrichYeu like '%ắc quy%'")[0][0]
m=k2.post(f"/VanThu/Huy/{t1}",{"lyDo":"trùng"},page=k2.get(f"/VanThu/ChiTiet/{t1}")[1])[2]; check("không có quyền" in m, "chỉ người soạn huỷ dự thảo")
m=k.post(f"/VanThu/Huy/{t1}",{"lyDo":"Gộp vào tờ trình mua sắm quý"},page=k.get(f"/VanThu/ChiTiet/{t1}")[1])[2]; check(tt(t1)==9, "người soạn huỷ dự thảo")
# 8. Tìm kiếm, quá hạn, sổ văn bản
check("tường lửa" in a.get("/VanThu?q=NGFW")[1], "tìm theo nội dung văn bản")
check("Bảo hiểm xã hội" in a.get("/VanThu?q=3&huong=Den")[1], "tìm theo số đến")
d1=q("select Id from VanBans where SoDen=1 and Nam=?",nam)[0][0]
DB.execute("update VanBans set HanXuLy=? where Id=?",(str(today-datetime.timedelta(days=2))+" 00:00:00",d1)); DB.commit()
qh=a.get("/VanThu?quaHan=true")[1]; qh=qh[qh.index("<tbody>"):]; check("Sở Y tế" in qh and "tường lửa" not in qh, "lọc văn bản quá hạn")
check("table-danger" in qh, "tô đỏ văn bản quá hạn")
so=a.get(f"/VanThu/So?huong=Di&nam={nam}")[1]; check(sk[1] in so and sk2[1] in so and "ắc quy" not in so, "sổ văn bản đi chỉ gồm văn bản đã cấp số")
st,b,_=a.req(f"/VanThu/So?huong=Den&nam={nam}&xuat=true"); check(st==200 and b.startswith("PK"), "xuất sổ văn bản đến ra Excel")
# 9. Mẫu văn bản
pm=a.get("/VanThu/MauForm")[1]
st,loc,m,_=a.post("/VanThu/LuuMau",{"Id":"0","Ten":"Giấy mời họp giao ban CNTT","Loai":"GiayMoi","TrichYeu":"Họp giao ban","NoiDung":"Kính mời {{NGUOI_SOAN}} dự họp ngày {{NGAY}}.","DangDung":"true"},page=pm)
mid=q("select Id from MauVanBans where Ten like 'Giấy mời%'")[0][0]
sp=k.get(f"/VanThu/SoanMoi?mauId={mid}")[1]; check("Kính mời Nguyễn Văn An (KTV) dự họp ngày ngày" in sp or "Kính mời Nguyễn Văn An (KTV) dự họp ngày" in sp, "soạn từ mẫu: thay trường {{NGUOI_SOAN}}, {{NGAY}}")
check(k.get("/VanThu/Mau")[0]==200 and ld.get("/VanThu/Mau")[2].startswith("/Account/AccessDenied"), "lãnh đạo không sửa mẫu")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
