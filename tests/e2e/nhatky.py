# Kiểm thử nhật ký hệ thống: đăng nhập, xem thông tin nhạy cảm, xuất dữ liệu, chống sửa (cần app demo đang chạy)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, json
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
def moi(hd, nguoi=None):
    return q("select NguoiDung, DiaChiIp, ChiTiet, DoiTuong, DoiTuongId from NhatKyHeThongs where HanhDong=? "+("and NguoiDung=? " if nguoi else "")+"order by Id desc limit 1", *([hd,nguoi] if nguoi else [hd]))
# Đăng nhập lỗi / thành công
x=U.__new__(U); x.name="ktv1"; x.cj=http.cookiejar.CookieJar(); x.op=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(x.cj), NoRedirect())
r=x.get("/Account/Login"); x.post("/Account/Login",{"userName":"ktv1","password":"sai-mat-khau"},page=r[1])
l=moi("DangNhapLoi","ktv1"); check(l and l[0][1]=="127.0.0.1" and "Sai mật khẩu" in l[0][2], "ghi đăng nhập lỗi kèm IP, lý do")
r=x.get("/Account/Login"); x.post("/Account/Login",{"userName":"khongtontai","password":"x"},page=r[1])
check("không tồn tại" in (moi("DangNhapLoi","khongtontai") or [("","","")])[0][2], "ghi đăng nhập với tên không tồn tại")
a=U("admin"); k=U("ktv1")
check(moi("DangNhap","ktv1") and moi("DangNhap","ktv1")[0][2]=="Mật khẩu", "ghi đăng nhập thành công")
# Xuất dữ liệu
n0=q("select count(*) from NhatKyHeThongs where HanhDong='XuatDuLieu'")[0][0]
st,b,_=k.req("/BaoCaoChuan/Xem?ma=BC01&dang=xlsx")
l=moi("XuatDuLieu","ktv1"); check(st==200 and l and "BaoCaoChuan/Xem" in l[0][2] and "BC01_" in l[0][2] and l[0][3]=="BaoCaoChuan", "ghi nhật ký xuất Excel (đường dẫn, tên tệp)")
k.req("/BaoCaoChuan/Xem?ma=BC02&dang=pdf"); check(q("select count(*) from NhatKyHeThongs where HanhDong='XuatDuLieu'")[0][0]==n0+2, "ghi nhật ký xuất PDF")
tep=q("select Id from TepDinhKems limit 1")
if tep: k.req(f"/Tep/Xem/{tep[0][0]}")
check(q("select count(*) from NhatKyHeThongs where HanhDong='XuatDuLieu'")[0][0]==n0+2, "xem tệp đính kèm không tính là xuất dữ liệu")
# Thông tin nhạy cảm: khoá kích hoạt bản quyền
bq=q("select Id from BanQuyenPhanMems where KhoaKichHoat is not null limit 1")
if not bq:
    k.post("/BanQuyen/Form",{"Id":"0","Ten":"Windows Server 2022 Standard","SoLuong":"2","Loai":"2","KhoaKichHoat":"ABCDE-12345-SECRET"},page=k.get("/BanQuyen/Form")[1])
    bq=q("select Id from BanQuyenPhanMems where KhoaKichHoat is not null limit 1")
bid=bq[0][0]
khoa=q("select KhoaKichHoat from BanQuyenPhanMems where Id=?",bid)[0][0]
pd=k.get(f"/BanQuyen/Details/{bid}")[1]; check(khoa not in pd and "Hiện khoá" in pd, "trang chi tiết không chứa khoá kích hoạt")
pf=k.get(f"/BanQuyen/Form/{bid}")[1]; check(khoa not in pf and "giữ nguyên" in pf, "form sửa không lộ khoá")
d={"__RequestVerificationToken":k.token(pd)}
st,b,_=k.req(f"/BanQuyen/XemKhoa/{bid}",d); check(st==200 and json.loads(b)["khoa"]==khoa, "bấm Hiện khoá trả về khoá")
l=moi("XemNhayCam","ktv1"); check(l and l[0][3]=="BanQuyenPhanMem" and l[0][4]==str(bid), "ghi nhật ký xem thông tin nhạy cảm")
check(U("nhanvien").req(f"/BanQuyen/XemKhoa/{bid}",{"__RequestVerificationToken":"x"})[0] in (302,400,403), "nhân viên không xem được khoá")
# Lưu form để trống khoá → giữ nguyên; đổi khoá → nhật ký che giá trị
ten=q("select Ten, SoLuong, Loai from BanQuyenPhanMems where Id=?",bid)[0]
loai=["","VinhVien","ThueBao","TheoSoLuong","MaNguonMo"]
k.post("/BanQuyen/Form",{"Id":bid,"Ten":ten[0],"SoLuong":ten[1],"Loai":str(ten[2]),"KhoaKichHoat":""},page=pf)
check(q("select KhoaKichHoat from BanQuyenPhanMems where Id=?",bid)[0][0]==khoa, "để trống khoá khi sửa → giữ nguyên")
k.post("/BanQuyen/Form",{"Id":bid,"Ten":ten[0],"SoLuong":ten[1],"Loai":str(ten[2]),"KhoaKichHoat":"NEW-KEY-99999"},page=pf)
l=q("select ChiTiet from NhatKyHeThongs where DoiTuong='BanQuyenPhanMem' and HanhDong='Sua' and DoiTuongId=? order by Id desc limit 1",str(bid))
check(q("select KhoaKichHoat from BanQuyenPhanMems where Id=?",bid)[0][0]=="NEW-KEY-99999" and l and "NEW-KEY" not in l[0][0] and "***" in l[0][0], "đổi khoá: nhật ký che giá trị (***)")
# Màn hình nhật ký
w=a.get("/NhatKy?hanhDong=DangNhapLoi")[1]; check("Đăng nhập lỗi" in w and "sai" not in "" and "127.0.0.1" in w, "lọc nhật ký theo thao tác, hiện IP")
check("lượt đăng nhập lỗi hôm nay" in a.get("/NhatKy")[1], "cảnh báo số lượt đăng nhập lỗi trong ngày")
st,b,_=a.req("/NhatKy/Xuat?hanhDong=XemNhayCam"); check(st==200 and b.startswith("PK") and moi("XuatDuLieu","admin")[0][3]=="NhatKy", "xuất nhật ký ra Excel (và ghi nhật ký việc xuất)")
check(k.get("/NhatKy")[2].startswith("/Account/AccessDenied"), "chỉ Quản trị xem nhật ký")
# Đăng xuất
po=k.get("/")[1]; k.req("/Account/Logout",{"__RequestVerificationToken":k.token(po)})
check(moi("DangXuat","ktv1") is not None and len(moi("DangXuat","ktv1"))==1, "ghi đăng xuất")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
