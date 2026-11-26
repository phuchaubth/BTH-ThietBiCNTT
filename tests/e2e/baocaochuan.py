# Kiểm thử báo cáo chuẩn PDF/Excel + đề nghị thay thế (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, subprocess, tempfile, os
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
a=U("admin"); ld=U("lanhdao")
def raw(u, path):
    try: r=u.op.open(BASE+path); return r.status, r.read(), r.headers.get("Content-Type","")
    except urllib.error.HTTPError as e: return e.code, b"", ""
def pdftext(b):
    with tempfile.NamedTemporaryFile(suffix=".pdf", delete=False) as f: f.write(b); p=f.name
    try: return subprocess.run(["pdftotext","-layout",p,"-"],capture_output=True,text=True).stdout
    finally: os.unlink(p)
idx=a.get("/BaoCaoChuan")[1]
check(all(m in idx for m in ["BC01","BC02","BC03","BC04","BC05","BC06"]), "danh mục 6 báo cáo chuẩn")
check(U("nhanvien").get("/BaoCaoChuan")[2].startswith("/Account/AccessDenied"), "nhân viên khoa không xem báo cáo chuẩn")
for ma in ["BC01","BC02","BC03","BC04","BC05","BC06"]:
    st,b,ct=raw(a, f"/BaoCaoChuan/Xem?ma={ma}&dang=pdf&tu=2026-01-01&den=2026-12-31")
    t=pdftext(b) if b.startswith(b"%PDF-") else ""
    check(st==200 and "application/pdf" in ct and "CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM" in t and "Độc lập - Tự do - Hạnh phúc" in t and "GIÁM ĐỐC" in t and f"{ma} · in lúc" in t,
          f"{ma}: PDF có quốc hiệu, tiêu ngữ, ô ký, số trang (tiếng Việt có dấu)")
    st2,b2,_=raw(a, f"/BaoCaoChuan/Xem?ma={ma}&dang=xlsx"); check(st2==200 and b2[:2]==b"PK", f"{ma}: Excel")
nts=q("select count(*) from TaiSans where TrangThai<>6")[0][0]
w=a.get("/BaoCaoChuan/Xem?ma=BC01")[1]; check(f"{nts} tài sản" in w, f"BC01 đếm đủ {nts} tài sản (trừ đã thanh lý)")
st,b,_=raw(a,"/BaoCaoChuan/Xem?ma=BC01&dang=pdf"); t=pdftext(b)
check("DANH SÁCH TÀI SẢN CÔNG NGHỆ THÔNG TIN" in t and "trang 1/" in t, "BC01 PDF: tiêu đề in hoa, phân trang")
cc=q("select Id from KhoaPhongs where Ma='CC'")[0][0]; ncc=q("select count(*) from TaiSans where TrangThai<>6 and KhoaPhongId=?",cc)[0][0]
check(f"{ncc} tài sản" in a.get(f"/BaoCaoChuan/Xem?ma=BC01&khoaId={cc}")[1], "lọc theo khoa")
kp=q("select count(*) from KhoanMucNganSachs where Nam=strftime('%Y','now')")[0][0]
w=a.get("/BaoCaoChuan/Xem?ma=BC05")[1]; check(w.count("<tr>")-1>=kp and "Tổng cộng" in w, "BC05 ngân sách có đủ khoản mục và dòng tổng")
check(ld.get("/BaoCaoChuan/Xem?ma=BC02")[0]==200, "lãnh đạo xem được báo cáo chuẩn")
check(a.req("/BaoCaoChuan/Xem?ma=BC99")[0]==404, "mã báo cáo không tồn tại → 404")
# Đề nghị thay thế
tsid,ma=q("select t.Id, t.MaTaiSan from TaiSans t join LoaiThietBis l on l.Id=t.LoaiThietBiId where l.Ma='PC' and t.TrangThai=2 and t.NgaySuDung>date('now','-2 year') and t.Id not in (select TaiSanId from MayTramAgents where TaiSanId is not null) limit 1")[0]
DB.execute("update CauHinhs set HeDieuHanh='Windows 7 Professional', RAM='4GB DDR3' where TaiSanId=?",(tsid,)); DB.commit()
tt=a.get("/BaoCaoChuan/ThayThe")[1]; tt=tt[tt.index("<tbody>"):]
row=re.search(re.escape(ma)+r".*?</tr>", tt, re.S)
check(row is not None and "HĐH hết hỗ trợ (Windows 7 Professional)" in row.group(0) and "RAM 4 GB dưới chuẩn 8 GB" in row.group(0), "máy Windows 7, RAM 4GB → vào danh sách thay thế với 2 lý do")
w=a.get("/BaoCaoChuan/Xem?ma=BC04")[1]; check(ma in w and "Windows 7 Professional" in w, "BC04 có thiết bị và lý do")
check('action="/BaoCaoChuan/LapDeXuat"' not in ld.get("/BaoCaoChuan/ThayThe")[1] or 'name="taiSanId"' not in ld.get("/BaoCaoChuan/ThayThe")[1], "lãnh đạo xem, không lập đề xuất")
ids=[tsid]+[r[0] for r in q("select t.Id from TaiSans t where t.TrangThai=4 limit 1")]
nDx=q("select count(*) from DeXuatMuaSams")[0][0]
st,loc,m,_=a.post("/BaoCaoChuan/LapDeXuat",{"taiSanId":ids},page=tt); print("lập đề xuất:", m, loc)
dx=q("select Id, CanCu, TrangThai, LyDo from DeXuatMuaSams order by Id desc limit 1")[0]
dongs=q("select Loai, SoLuong, GhiChu from DongDeXuats where DeXuatMuaSamId=?",dx[0])
check(q("select count(*) from DeXuatMuaSams")[0][0]==nDx+1 and dx[1]==3 and dx[2]==1 and ma in dx[3], "lập đề xuất mua sắm nháp căn cứ 'Thay thế' kèm lý do")
check(sum(float(d[1]) for d in dongs)==len(ids) and all(d[0]==2 for d in dongs) and any(ma in (d[2] or "") for d in dongs), "dòng thiết bị gom theo loại, ghi chú mã tài sản thay")
check(loc and loc.startswith("/MuaSam/DeXuat/"), "chuyển sang đề xuất để sửa đơn giá, gửi duyệt")
m=a.post("/BaoCaoChuan/LapDeXuat",{},page=tt)[2]; check("Chọn ít nhất" in m, "phải chọn tài sản")
# Lịch gửi định kỳ kèm PDF chuẩn
p=ld.get("/LichGuiBaoCao/Sua")[1]
st,loc,m,_=ld.post("/LichGuiBaoCao/Sua",{"Ten":"BC02 tháng – BGĐ","Loai":"ChuanBC02","ChuKy":"Thang","NguoiNhan":"lanhdao","DangBat":["true","false"]},page=p)
lid=q("select Id from LichGuiBaoCaos where Ten='BC02 tháng – BGĐ'")[0][0]
m=ld.post(f"/LichGuiBaoCao/GuiNgay/{lid}",{},page=ld.get("/LichGuiBaoCao")[1])[2]; print("gửi:", m)
kq=q("select TepCuoi, KetQuaCuoi from LichGuiBaoCaos where Id=?",lid)[0]
check(kq[0] and kq[0].endswith(".pdf") and "BC02_" in (kq[1] or ""), "lịch gửi định kỳ dựng PDF chuẩn BC02")
st,b,ct=raw(ld, f"/LichGuiBaoCao/Tep/{lid}"); check(st==200 and b.startswith(b"%PDF-") and "pdf" in ct, "tải lại tệp PDF đã gửi")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
