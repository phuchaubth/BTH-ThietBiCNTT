# Kiểm thử vòng đời: điều chuyển ra ngoài BV, gợi ý thanh lý, thu hồi linh kiện (cần app demo đang chạy)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
k=U("ktv1"); k2=U("ktv2"); tk=U("truongkhoacc"); ld=U("lanhdao"); nv=U("nhanvien")
today=datetime.date.today()
cc=q("select Id from KhoaPhongs where Ma='CC'")[0][0]
ts=q("""select t.Id, t.MaTaiSan from TaiSans t join LoaiThietBis l on l.Id=t.LoaiThietBiId where t.KhoaPhongId=? and t.TrangThai=2 and l.CoLuuTruDuLieu=1
  and not exists(select 1 from PhieuSuaChuas p where p.TaiSanId=t.Id and p.TrangThai not in (6,7,8))
  and not exists(select 1 from ChiTietDieuChuyens c join PhieuDieuChuyens d on d.Id=c.PhieuDieuChuyenId where c.TaiSanId=t.Id and d.TrangThai in (1,2,3)) limit 1""",cc)
if not ts:
    # trạng thái do các bộ kiểm thử trước để lại: chọn máy không nằm trong phiếu điều chuyển mở, đóng phiếu sửa chữa của nó
    ts=q("""select t.Id, t.MaTaiSan from TaiSans t join LoaiThietBis l on l.Id=t.LoaiThietBiId where t.KhoaPhongId=? and l.CoLuuTruDuLieu=1 and t.TrangThai not in (5,6)
      and not exists(select 1 from ChiTietDieuChuyens c join PhieuDieuChuyens d on d.Id=c.PhieuDieuChuyenId where c.TaiSanId=t.Id and d.TrangThai in (1,2,3)) limit 1""",cc)
    DB.execute("update PhieuSuaChuas set TrangThai=8 where TaiSanId=? and TrangThai not in (6,7,8)",(ts[0][0],))
    DB.execute("update TaiSans set TrangThai=2 where Id=?",(ts[0][0],)); DB.commit()
tsid,ma=ts[0]
# 1. Điều chuyển ra ngoài
pg=k2.get(f"/DieuChuyen/Create?tuKhoa={cc}")[1]
check('name="RaNgoai"' in pg, "CNTT có tuỳ chọn điều chuyển ra ngoài")
check('name="RaNgoai"' not in tk.get("/DieuChuyen/Create")[1], "trưởng khoa không lập phiếu ra ngoài")
st,loc,m,b=k2.post("/DieuChuyen/Create",{"TuKhoaPhongId":cc,"RaNgoai":"true","LyDo":"Hỗ trợ tuyến dưới","taiSanIds":tsid,"guiDuyet":"true"},page=pg)
check(st==200 and "Nhập đơn vị nhận" in html.unescape(b), "ra ngoài phải nhập đơn vị nhận")
st,loc,m,b=k2.post("/DieuChuyen/Create",{"TuKhoaPhongId":cc,"RaNgoai":"true","DonViNhanNgoai":"Trung tâm Y tế huyện Hàm Thuận Bắc","LyDo":"Hỗ trợ tuyến dưới theo chỉ đạo Sở","taiSanIds":tsid,"guiDuyet":"true"},page=pg)
print("lập:", m, re.findall(r'text-danger[^>]*>\s*<ul>(.*?)</ul>', html.unescape(b), re.S)[:1]); did=int(loc.rsplit("/",1)[1])
r=q("select RaNgoai, DenKhoaPhongId, CapCuoi, CapHienTai from PhieuDieuChuyens where Id=?",did)[0]
check(r[0]==1 and r[1] is None and r[2]==3 and "3 cấp" in m, "phiếu ra ngoài luôn duyệt 3 cấp (đến Lãnh đạo) dù giá trị nhỏ")
for u in (tk,k,ld):
    m=u.post(f"/DieuChuyen/Duyet/{did}",{"dongY":"true"},page=u.get(f"/DieuChuyen/Details/{did}")[1])[2]; print("duyệt:", m)
check(q("select TrangThai from PhieuDieuChuyens where Id=?",did)[0][0]==3, "duyệt đủ 3 cấp")
pd=k.get(f"/DieuChuyen/Details/{did}")[1]
check("Ra ngoài BV · ghi giảm" in pd and "Hàm Thuận Bắc" in pd and 'name="soQuyetDinh"' in pd, "trang phiếu: đơn vị nhận, form quyết định")
m=k.post(f"/DieuChuyen/BanGiao/{did}",{"soQuyetDinh":"","ngayQuyetDinh":""},page=pd)[2]; check("số và ngày quyết định" in m, "bàn giao ra ngoài phải có quyết định")
m=k.post(f"/DieuChuyen/BanGiao/{did}",{"soQuyetDinh":"125/QĐ-BV","ngayQuyetDinh":str(today)},page=pd)[2]; check("xoá dữ liệu" in m, "thiết bị có dữ liệu: phải xác nhận xoá dữ liệu")
m=k.post(f"/DieuChuyen/BanGiao/{did}",{"soQuyetDinh":"125/QĐ-BV","ngayQuyetDinh":str(today),"daXoaDuLieu":"true"},page=pd)[2]; print("bàn giao:", m)
r=q("select TrangThai, KhoaPhongId, NguoiSuDung from TaiSans where Id=?",tsid)[0]
check(r[0]==6 and r[1] is None and "Hàm Thuận Bắc" in r[2], "tài sản ghi giảm (đã chuyển đi), không còn thuộc khoa")
check(q("select count(*) from LichSuTaiSans where TaiSanId=? and NoiDung like '%chuyển ra ngoài bệnh viện%125/QĐ-BV%'",tsid)[0][0]==1, "lịch sử ghi rõ đơn vị nhận và quyết định")
kt=k.get(f"/KeToan?tu={today}&den={today}")[1]
check(ma in kt and "Điều chuyển ra ngoài: Trung tâm Y tế huyện Hàm Thuận Bắc" in kt, "số liệu kế toán: ghi giảm có tài sản chuyển ra ngoài")
check("ngoài bệnh viện" in k.get("/DieuChuyen")[1], "danh sách điều chuyển hiện nơi đến ngoài BV")
# 2. Gợi ý thanh lý
hong=q("select Id, MaTaiSan from TaiSans where TrangThai=4 limit 1")
pc=k.get("/ThanhLy/Create")[1]
check(hong and re.search(r'class="form-check-input goi-y" name="taiSanIds" value="%d"'%hong[0][0], pc) is not None and "Gợi ý" in pc, "thiết bị hỏng được gợi ý thanh lý (đánh dấu, lý do)")
first=re.search(r'name="taiSanIds" value="(\d+)"', pc).group(1)
check('goi-y" name="taiSanIds" value="%s"'%first in pc, "gợi ý xếp đầu danh sách")
# 3. Thu hồi linh kiện
st,loc,m,_=k.post("/ThanhLy/Create",{"LyDo":"Hỏng không kinh tế để sửa","taiSanIds":hong[0][0]},page=pc)
tid=int(loc.rsplit("/",1)[1]); ctid=q("select Id from ChiTietThanhLys where DeXuatThanhLyId=?",tid)[0][0]
ram=q("select Id, TonKho from VatTus where Ma='RAM-D4-8G'")[0]; kho=q("select Id from Khos limit 1")[0][0]
pt=k.get(f"/ThanhLy/Details/{tid}")[1]
check('action="/ThanhLy/ThuHoiLinhKien/' not in pt, "nháp: chưa thu hồi linh kiện")
k.post(f"/ThanhLy/GuiHoiDong/{tid}",{},page=pt); pt=k.get(f"/ThanhLy/Details/{tid}")[1]
check('action="/ThanhLy/ThuHoiLinhKien/' in pt, "chờ hội đồng: có form thu hồi linh kiện")
m=k.post(f"/ThanhLy/ThuHoiLinhKien/{tid}",{"chiTietId":ctid,"vatTuId":ram[0],"khoId":kho,"soLuong":"2","donGia":"200000","ghiChu":"Test OK"},page=pt)[2]; print("thu hồi:", m)
check("Đã nhập kho 2" in m and float(q("select TonKho from VatTus where Id=?",ram[0])[0][0])==float(ram[1])+2, "nhập kho 2 thanh RAM thu hồi, tồn tăng")
pk=q("select SoPhieu, LyDo, TrangThai from PhieuKhos where DeXuatThanhLyId=?",tid)
check(len(pk)==1 and pk[0][1]==3 and pk[0][2]==2, "phiếu nhập 'thu hồi' đã ghi sổ, gắn đề xuất thanh lý")
check(q("select count(*) from LichSuTaiSans where TaiSanId=? and NoiDung like 'Tháo thu hồi%'",hong[0][0])[0][0]==1, "lịch sử tài sản ghi tháo linh kiện")
m=k.post(f"/ThanhLy/ThuHoiLinhKien/{tid}",{"chiTietId":ctid,"vatTuId":ram[0],"khoId":kho,"soLuong":"1.5","donGia":"0"},page=pt)[2]; check("số nguyên" in m, "số lượng phải nguyên")
check(pk[0][0] in k.get(f"/ThanhLy/Details/{tid}")[1], "đề xuất hiện danh sách linh kiện đã thu hồi")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
