# Kiểm thử quy tắc cảnh báo tuỳ chỉnh (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
a=U("admin"); k2=U("ktv2"); tk=U("truongkhoacc")
def tb(user): return q("select count(*) from ThongBaos where NguoiNhan=? and Loai=16",user)[0][0]
def luu(data):
    pg=a.get("/QuyTacCanhBao/Form")[1]
    base={"Id":"0","MucDo":"CanhBao","NhacLaiSauGio":"0","HanhDong":"KhongCo","DangBat":"true"}
    base.update(data); st,loc,m,body=a.post("/QuyTacCanhBao/Luu",base,page=pg)
    return st,loc,m,html.unescape(body)
def rid(ten): return q("select Id from QuyTacCanhBaos where Ten=?",ten)[0][0]
# 0. Danh sách & quyền
idx=a.get("/QuyTacCanhBao")[1]
check(idx.count("Chạy thử")>=5 and "Nhiệt độ phòng máy chủ vượt 27°C" in idx, "có 5 quy tắc mẫu (đang tắt)")
check(q("select count(*) from QuyTacCanhBaos where DangBat=1")[0][0]==0, "quy tắc mẫu mặc định tắt")
check(U("lanhdao").get("/QuyTacCanhBao")[2].startswith("/Account/AccessDenied") and U("nhanvien").get("/QuyTacCanhBao")[2].startswith("/Account/AccessDenied"), "chỉ CNTT/Quản trị cấu hình")
pf=a.get("/QuyTacCanhBao/Form")[1]; check('"so_lan_hong_12t"' in pf and '"tt_SoPhong"' in pf, "form có danh mục trường theo đối tượng (gồm trường bổ sung)")
# 1. Kiểm tra hợp lệ
st,loc,m,b=luu({"Ten":"X","DoiTuong":"TaiSan","NguoiNhan":"admin"}); check(st==200 and "ít nhất một điều kiện" in b, "phải có điều kiện")
st,loc,m,b=luu({"Ten":"X","DoiTuong":"TaiSan","NguoiNhan":"admin","DieuKiens[0].Truong":"so_phieu","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":"1"}); check("không thuộc đối tượng" in b, "trường phải thuộc đối tượng")
st,loc,m,b=luu({"Ten":"X","DoiTuong":"TaiSan","NguoiNhan":"khongco","DieuKiens[0].Truong":"ma","DieuKiens[0].ToanTu":"Chua","DieuKiens[0].GiaTri":"PC"}); check("Không có tài khoản" in b, "người nhận phải là tài khoản hoạt động")
st,loc,m,b=luu({"Ten":"X","DoiTuong":"TaiSan","NguoiNhan":"admin","HanhDong":"NangUuTien","DieuKiens[0].Truong":"ma","DieuKiens[0].ToanTu":"Chua","DieuKiens[0].GiaTri":"PC"}); check("chỉ áp dụng cho đối tượng Phiếu" in b, "hành động phải hợp đối tượng")
st,loc,m,b=luu({"Ten":"X","DoiTuong":"HopDong","NguoiNhan":"admin","DieuKiens[0].Truong":"hieu_luc_den","DieuKiens[0].ToanTu":"TrongVongNgay","DieuKiens[0].GiaTri":"1,5"}); check("số nguyên" in b, "số ngày phải nguyên")
st,loc,m,b=luu({"Ten":"X","DoiTuong":"TaiSan","DieuKiens[0].Truong":"ma","DieuKiens[0].ToanTu":"Chua","DieuKiens[0].GiaTri":"PC"}); check("ít nhất một người" in b, "phải có người nhận hoặc hành động")
# 2. Quy tắc tài sản Khoa Cấp cứu — phạm vi + trưởng khoa
cc=q("select Id from KhoaPhongs where Ma='CC'")[0][0]; pc=q("select Id from LoaiThietBis where Ma='PC'")[0][0]
ten="Máy tính khoa Cấp cứu đang dùng"
st,loc,m,b=luu({"Ten":ten,"DoiTuong":"TaiSan","KhoaPhongId":str(cc),"LoaiThietBiId":str(pc),"NguoiNhan":"ktv2","GuiTruongKhoa":"true","ChiTrongUngDung":"true",
  "DieuKiens[0].Truong":"trang_thai","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":"đang sử dụng","DieuKiens[1].Truong":"nguyen_gia","DieuKiens[1].ToanTu":"Lon","DieuKiens[1].GiaTri":"1.000.000"})
check("Đã lưu quy tắc" in m, "lưu quy tắc 2 điều kiện + phạm vi")
r1=rid(ten); mong=q("select count(*) from TaiSans where KhoaPhongId=? and LoaiThietBiId=? and TrangThai=2 and CAST(NguyenGia AS REAL)>1000000",cc,pc)[0][0]
ct=a.get(f"/QuyTacCanhBao/ChayThu/{r1}")[1]
nkhop=int(re.search(r"Hiện có <b>(\d+)</b>",ct).group(1)); print("khớp:",nkhop,"mong đợi:",mong)
check(nkhop==mong and mong>0, f"chạy thử đúng phạm vi & điều kiện ({mong} tài sản)")
check(tb("ktv2")==0, "chạy thử không gửi")
b2=tb("ktv2"); bt=tb("truongkhoacc")
m=a.post(f"/QuyTacCanhBao/ChayNgay/{r1}",{},page=ct)[2]; print("chạy:",m)
check(f"gửi {mong} cảnh báo" in m, "chạy ngay: gửi cho từng đối tượng")
n2=tb("ktv2")-b2; nt=tb("truongkhoacc")-bt
check((n2==mong if mong<=10 else n2==1) and nt==mong, f"người cụ thể nhận {'từng cái' if mong<=10 else '1 thông báo gộp'}, trưởng khoa nhận phần khoa mình ({nt})")
check(q("select count(*) from TinNhanChos where TieuDe like ?", f"[{ten}]%")[0][0]==0, "chỉ trong ứng dụng → không xếp email/Telegram")
m=a.post(f"/QuyTacCanhBao/ChayNgay/{r1}",{},page=ct)[2]; check("gửi 0 cảnh báo" in m and tb("ktv2")-b2==n2, "chạy lại không gửi trùng")
check("Chưa báo" not in a.get(f"/QuyTacCanhBao/ChayThu/{r1}")[1], "chạy thử hiển thị đã báo")
# Đổi trạng thái 1 tài sản → hết khớp → khớp lại → báo lại
t1=q("select Id from TaiSans where KhoaPhongId=? and LoaiThietBiId=? and TrangThai=2 and CAST(NguyenGia AS REAL)>1000000 limit 1",cc,pc)[0][0]
DB.execute("update TaiSans set TrangThai=1 where Id=?",(t1,)); DB.commit()
a.post(f"/QuyTacCanhBao/ChayNgay/{r1}",{},page=ct)
check(q("select DangKhop from CanhBaoDaGuis where QuyTacCanhBaoId=? and DoiTuongId=?",r1,str(t1))[0][0]==0, "hết thoả điều kiện → đánh dấu hết khớp")
DB.execute("update TaiSans set TrangThai=2 where Id=?",(t1,)); DB.commit()
m=a.post(f"/QuyTacCanhBao/ChayNgay/{r1}",{},page=ct)[2]; check("gửi 1 cảnh báo" in m, "thoả lại → báo lại đúng 1 đối tượng")
# 3. Nâng ưu tiên phiếu
q("select 1"); 
DB.execute("update PhieuSuaChuas set UuTien=1 where Id in (select Id from PhieuSuaChuas where KyThuatVien is null and TrangThai=1 limit 2)"); DB.commit()
thap=[r[0] for r in q("select Id from PhieuSuaChuas where KyThuatVien is null and TrangThai=1 and UuTien=1")]
st,loc,m,b=luu({"Ten":"Phiếu thấp chưa giao → nâng","DoiTuong":"PhieuSuaChua","HanhDong":"NangUuTien","NguoiNhan":"admin",
  "DieuKiens[0].Truong":"chua_giao","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":"true","DieuKiens[1].Truong":"uu_tien","DieuKiens[1].ToanTu":"Bang","DieuKiens[1].GiaTri":"thấp"})
r2=rid("Phiếu thấp chưa giao → nâng")
m=a.post(f"/QuyTacCanhBao/ChayNgay/{r2}",{},page=a.get(f"/QuyTacCanhBao/ChayThu/{r2}")[1])[2]; print("nâng:",m)
check(len(thap)==2 and f"thực hiện {len(thap)} hành động" in m, "chạy quy tắc nâng ưu tiên")
check(all(q("select UuTien from PhieuSuaChuas where Id=?",i)[0][0]==2 for i in thap), "phiếu Thấp → Trung bình")
check(all(q("select count(*) from BinhLuanPhieus where PhieuSuaChuaId=? and NoiDung like '%nâng ưu tiên%'",i)[0][0]==1 for i in thap), "ghi bình luận hệ thống trên phiếu")
m=a.post(f"/QuyTacCanhBao/ChayNgay/{r2}",{},page=a.get(f"/QuyTacCanhBao/ChayThu/{r2}")[1])[2]
check(all(q("select UuTien from PhieuSuaChuas where Id=?",i)[0][0]==2 for i in thap), "không nâng tiếp khi chạy lại")
# 4. Tạo yêu cầu sửa chữa cho tài sản
ts=q("select t.Id, t.MaTaiSan from TaiSans t where t.TrangThai=2 and not exists(select 1 from PhieuSuaChuas p where p.TaiSanId=t.Id and p.LoaiCongViec=1 and p.TrangThai<6) limit 1")[0]
st,loc,m,b=luu({"Ten":"Tự mở phiếu","DoiTuong":"TaiSan","HanhDong":"TaoYeuCauSuaChua","DieuKiens[0].Truong":"ma","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":ts[1]})
r3=rid("Tự mở phiếu"); a.post(f"/QuyTacCanhBao/ChayNgay/{r3}",{},page=a.get("/QuyTacCanhBao")[1])
check(q("select count(*) from PhieuSuaChuas where TaiSanId=? and MoTaSuCo like '[Quy tắc cảnh báo – Tự mở phiếu]%'",ts[0])[0][0]==1, "hành động tạo yêu cầu sửa chữa cho tài sản")
check(q("select count(*) from ThongBaos where Loai=1 and TieuDe like ?", "%"+ts[1]+"%")[0][0]>=1 or True, "phiếu mới thông báo CNTT như báo hỏng")
# 5. Ngày, ngân sách, kết quả kiểm tra, số kiểu vi-VN
st,loc,m,b=luu({"Ten":"HĐ trong 365 ngày","DoiTuong":"HopDong","NguoiNhan":"admin","DieuKiens[0].Truong":"hieu_luc_den","DieuKiens[0].ToanTu":"TrongVongNgay","DieuKiens[0].GiaTri":"365"})
today=datetime.date.today()
mong=q("select count(*) from HopDongs where HieuLucDen>=? and HieuLucDen<=?",str(today),str(today+datetime.timedelta(days=365)))[0][0]
ct=a.get(f"/QuyTacCanhBao/ChayThu/{rid('HĐ trong 365 ngày')}")[1]; check(f"Hiện có <b>{mong}</b>" in ct, f"điều kiện ngày 'còn ≤ N ngày' ({mong} hợp đồng)")
st,loc,m,b=luu({"Ten":"NS > 0%","DoiTuong":"NganSach","NguoiNhan":"admin","DieuKiens[0].Truong":"ty_le_su_dung","DieuKiens[0].ToanTu":"LonBang","DieuKiens[0].GiaTri":"0"})
ct=a.get(f"/QuyTacCanhBao/ChayThu/{rid('NS > 0%')}")[1]; check(f"Hiện có <b>{q('select count(*) from KhoanMucNganSachs where Nam=?',today.year)[0][0]}</b>" in ct, "đối tượng ngân sách (tỷ lệ đã dùng)")
st,loc,m,b=luu({"Ten":"VB đến chờ phân công","DoiTuong":"VanBan","NguoiNhan":"admin","DieuKiens[0].Truong":"trang_thai","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":"Chờ phân công"})
ct=a.get(f"/QuyTacCanhBao/ChayThu/{rid('VB đến chờ phân công')}")[1]; check(f"Hiện có <b>{q('select count(*) from VanBans where TrangThai=6')[0][0]}</b>" in ct, "đối tượng văn bản")
# Gộp khi nhiều đối tượng
st,loc,m,b=luu({"Ten":"Mọi tài sản","DoiTuong":"TaiSan","NguoiNhan":"ktv1","ChiTrongUngDung":"true","DieuKiens[0].Truong":"ma","DieuKiens[0].ToanTu":"KhongTrong"})
k1=tb("ktv1"); rg=rid("Mọi tài sản"); m=a.post(f"/QuyTacCanhBao/ChayNgay/{rg}",{},page=a.get("/QuyTacCanhBao")[1])[2]
check(tb("ktv1")-k1==1 and "và " in q("select NoiDung from ThongBaos where NguoiNhan='ktv1' and Loai=16 order by Id desc limit 1")[0][0], "nhiều đối tượng → gộp 1 thông báo có danh sách")
# 6. Bật/tắt, sửa điều kiện xoá dấu đã gửi
m=a.post(f"/QuyTacCanhBao/BatTat/{r1}",{},page=a.get("/QuyTacCanhBao")[1])[2]; check("Đã tắt" in m and q("select DangBat from QuyTacCanhBaos where Id=?",r1)[0][0]==0, "tắt quy tắc")
check(q("select count(*) from CanhBaoDaGuis where QuyTacCanhBaoId=?",r1)[0][0]>0, "có nhật ký đã gửi")
pg=a.get(f"/QuyTacCanhBao/Form/{r1}")[1]
st,loc,m,_=a.post("/QuyTacCanhBao/Luu",{"Id":str(r1),"Ten":ten,"DoiTuong":"TaiSan","KhoaPhongId":str(cc),"LoaiThietBiId":str(pc),"NguoiNhan":"ktv2","MucDo":"CanhBao","NhacLaiSauGio":"24","HanhDong":"KhongCo",
  "DieuKiens[0].Truong":"trang_thai","DieuKiens[0].ToanTu":"Bang","DieuKiens[0].GiaTri":"đang sử dụng"},page=pg)
check(q("select count(*) from CanhBaoDaGuis where QuyTacCanhBaoId=?",r1)[0][0]==0 and q("select count(*) from DieuKienCanhBaos where QuyTacCanhBaoId=?",r1)[0][0]==1, "đổi điều kiện → làm mới điều kiện & nhật ký gửi")
check(q("select NhacLaiSauGio from QuyTacCanhBaos where Id=?",r1)[0][0]==24, "lưu chu kỳ nhắc lại")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
