# Kiểm thử bảo trì định kỳ đầy đủ (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, json, sqlite3, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
k=U("ktv1"); k2=U("ktv2"); ld=U("lanhdao"); a=U("admin")
today=datetime.date.today()
ups=q("select Id from LoaiThietBis where Ma='UPS'")[0][0]; pc=q("select Id from LoaiThietBis where Ma='PC'")[0][0]
bat=q("select Id from VatTus where Ma='BAT-UPS'")[0][0]
check("Hiệu lực" in k.get("/KeHoachBaoTri")[1], "kế hoạch mẫu ở trạng thái Hiệu lực")
# 1. Lập kế hoạch → thẩm định → duyệt
p=k.get("/KeHoachBaoTri/Create")[1]
st,loc,m,_=k.post("/KeHoachBaoTri/Save",{"Ten":"Kiểm tra UPS 6 tháng","LoaiThietBiId":ups,"KieuChuKy":"ThoiGian","ChuKyNgay":"180","NgayTiepTheo":str(today+datetime.timedelta(days=10)),
  "KyThuatVien":"ktv1","NguoiThamDinh":"ktv2","NguoiDuyet":"lanhdao","GioCongDuKien":"1.5","ChiPhiDuKien":"0","vatTu[0].VatTuId":bat,"vatTu[0].SoLuong":"1","guiTrinh":"true"},page=p)
print("lập:", m); kid=loc.rsplit("/",1)[1]
check("thẩm định: ktv2" in m, "gửi trình → chờ thẩm định")
check(q("select DangApDung from KeHoachBaoTris where Id=?",kid)[0][0]==0, "chưa duyệt thì chưa áp dụng")
check("Kế hoạch bảo trì" in k2.get("/ThongBao")[1], "người thẩm định nhận thông báo")
pg=k.get(f"/KeHoachBaoTri/ChiTiet/{kid}")[1]; check('action="/KeHoachBaoTri/ThamDinh/' not in pg, "người lập không thấy nút thẩm định")
m=k.post(f"/KeHoachBaoTri/ThamDinh/{kid}",{"dongY":"true"},page=pg)[2]; check("Chỉ ktv2" in m, "người khác không thẩm định được")
pg=k2.get(f"/KeHoachBaoTri/ChiTiet/{kid}")[1]
m=k2.post(f"/KeHoachBaoTri/ThamDinh/{kid}",{"dongY":"true","yKien":"OK"},page=pg)[2]; check("chuyển xét duyệt" in m, "thẩm định đồng ý")
check(f"/KeHoachBaoTri/ChiTiet/{kid}" in ld.get("/KeHoachBaoTri?choToi=true")[1], "lãnh đạo thấy trong 'chờ tôi duyệt'")
m=ld.post(f"/KeHoachBaoTri/Duyet/{kid}",{"dongY":"true"},page=ld.get(f"/KeHoachBaoTri/ChiTiet/{kid}")[1])[2]; check("có hiệu lực" in m, "lãnh đạo duyệt → hiệu lực")
check(q("select TrangThai,DangApDung from KeHoachBaoTris where Id=?",kid)[0]==(4,1), "trạng thái Hiệu lực, đang áp dụng")
# Sửa chu kỳ kế hoạch đã duyệt → về nháp
pe=k.get(f"/KeHoachBaoTri/Edit/{kid}")[1]
f={"Id":kid,"Ten":"Kiểm tra UPS 6 tháng","LoaiThietBiId":ups,"KieuChuKy":"ThoiGian","ChuKyNgay":"120","NgayTiepTheo":str(today+datetime.timedelta(days=10)),"KyThuatVien":"ktv1","NguoiThamDinh":"","NguoiDuyet":"lanhdao","GioCongDuKien":"1.5","ChiPhiDuKien":"0","vatTu[0].VatTuId":bat,"vatTu[0].SoLuong":"1"}
m=k.post("/KeHoachBaoTri/Save",f,page=pe)[2]; check("trở về Nháp" in m and q("select TrangThai from KeHoachBaoTris where Id=?",kid)[0][0]==1, "đổi chu kỳ → về Nháp, phải duyệt lại")
k.post(f"/KeHoachBaoTri/GuiTrinh/{kid}",{},page=k.get(f"/KeHoachBaoTri/ChiTiet/{kid}")[1])
ld.post(f"/KeHoachBaoTri/Duyet/{kid}",{"dongY":"true"},page=ld.get(f"/KeHoachBaoTri/ChiTiet/{kid}")[1])
# 2. Sinh lệnh theo thời gian (kế hoạch máy in đến kỳ) — job nền có thể đã sinh
k.post("/KeHoachBaoTri/SinhLenh",{},page=k.get("/KeHoachBaoTri")[1])
prn=q("select k.Id from KeHoachBaoTris k where k.Ten like 'Bảo dưỡng máy in%'")[0][0]
lenhPrn=q("select Id,TaiSanId,TrangThai,MoTaSuCo from PhieuSuaChuas where KeHoachBaoTriId=?",prn)
soMayIn=q("select count(*) from TaiSans t join LoaiThietBis l on l.Id=t.LoaiThietBiId where l.Ma='PRN' and t.TrangThai=2")[0][0]
check(len(lenhPrn)==soMayIn and all("đến kỳ" in x[3] for x in lenhPrn), f"sinh lệnh theo lô cho {soMayIn} máy in đang dùng")
check(all(x[2]==2 for x in lenhPrn), "lệnh giao sẵn cho KTV phụ trách (Đã tiếp nhận)")
k.post("/KeHoachBaoTri/SinhLenh",{},page=k.get("/KeHoachBaoTri")[1])
check(len(q("select Id from PhieuSuaChuas where KeHoachBaoTriId=?",prn))==soMayIn, "không sinh trùng khi lệnh còn mở")
# 3. Hoàn thành ghi chỉ số → chỉ số tăng đủ chu kỳ → lệnh theo chỉ số
pid,tsid=lenhPrn[0][0],lenhPrn[0][1]
c0=(q("select ChiSoBoDem from TaiSans where Id=?",tsid)[0][0] or 0)+100
pp=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
m=k.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"bienPhap":"Vệ sinh, thay lô sấy","gioCong":"0.5","chiPhiThueNgoai":"0","chiSoBoDem":str(c0-200)},page=pp)[2]
check("nhỏ hơn chỉ số" in m, "chỉ số lùi bị từ chối")
m=k.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"bienPhap":"Vệ sinh, thay lô sấy","gioCong":"0.5","chiPhiThueNgoai":"0","chiSoBoDem":str(c0)},page=pp)[2]; check("Đã hoàn thành" in m, f"hoàn thành lệnh, ghi chỉ số {c0:,}")
check(q("select ChiSoBoDem from PhieuSuaChuas where Id=?",pid)[0][0]==c0 and q("select ChiSoBoDem from TaiSans where Id=?",tsid)[0][0]==c0, "chỉ số lưu vào lệnh và tài sản")
ma=q("select MaTaiSan from TaiSans where Id=?",tsid)[0][0]
# Cập nhật bộ đếm qua API (khoá có phạm vi taisan.ghi)
pg=a.get("/TichHop")[1]; st,b,l=a.req("/TichHop/TaoKhoa",{"ten":"Bộ đếm máy in","phamVi":["taisan.ghi"],"__RequestVerificationToken":a.token(pg)})
key=re.search(r'id="khoaMoi" value="([^"]+)"', a.get(l)[1]).group(1)
req=urllib.request.Request(BASE+f"/api/v1/taisan/{ma}", data=json.dumps({"chiSoBoDem":c0+19000}).encode(), headers={"Content-Type":"application/json","X-Api-Key":key}, method="PATCH")
urllib.request.urlopen(req)
k.post("/KeHoachBaoTri/SinhLenh",{},page=k.get("/KeHoachBaoTri")[1])
check(len(q("select Id from PhieuSuaChuas where KeHoachBaoTriId=? and TaiSanId=? and Id<>?",prn,tsid,pid))==0, "chưa đủ chu kỳ (19.000 trang) → chưa sinh")
req=urllib.request.Request(BASE+f"/api/v1/taisan/{ma}", data=json.dumps({"chiSoBoDem":c0+20500}).encode(), headers={"Content-Type":"application/json","X-Api-Key":key}, method="PATCH")
urllib.request.urlopen(req)
k.post("/KeHoachBaoTri/SinhLenh",{},page=k.get("/KeHoachBaoTri")[1])
moi=q("select MoTaSuCo from PhieuSuaChuas where KeHoachBaoTriId=? and TaiSanId=? and Id<>?",prn,tsid,pid)
check(len(moi)==1 and "đạt chỉ số" in moi[0][0], f"bộ đếm tăng 20.500 trang → lệnh theo chỉ số")
# 4. Checklist bắt buộc (kế hoạch máy tính) — dời lịch về hôm nay qua lịch
pcplan=q("select Id, NgayTiepTheo from KeHoachBaoTris where Ten like 'Vệ sinh máy tính%'")[0]
nt=datetime.date.fromisoformat(pcplan[1])
lich=k.get(f"/KeHoachBaoTri/Lich?nam={nt.year}&thang={nt.month}")[1]; check(re.search(r'data-id="%d" data-tu="%s"'%(pcplan[0],pcplan[1]), lich) is not None, "lịch tháng hiển thị mốc kế tiếp (kéo thả được)")
m=k.post("/KeHoachBaoTri/DoiLich",{"id":pcplan[0],"tuNgay":pcplan[1],"ngayMoi":str(today)},page=lich)
check('"ok":false' in m[3].replace(" ",""), "dời lịch phải có lý do")
st,loc,m,body=k.post("/KeHoachBaoTri/DoiLich",{"id":pcplan[0],"tuNgay":pcplan[1],"ngayMoi":str(today),"lyDo":"Làm sớm trước đợt kiểm tra"},page=lich)
check('"ok":true' in body.replace(" ",""), "dời lịch về hôm nay")
k.post("/KeHoachBaoTri/SinhLenh",{},page=k.get("/KeHoachBaoTri")[1])
lpc=q("select Id, TaiSanId from PhieuSuaChuas where KeHoachBaoTriId=? order by Id",pcplan[0]); check(len(lpc)>0, f"sinh {len(lpc)} lệnh vệ sinh máy tính")
lp,lts=lpc[0]
pp=k.get(f"/PhieuSuaChua/Details/{lp}")[1]; check("Checklist bảo trì chưa thực hiện" in pp, "lệnh hiển thị yêu cầu checklist")
m=k.post(f"/PhieuSuaChua/HoanThanh/{lp}",{"bienPhap":"Đã vệ sinh","gioCong":"1","chiPhiThueNgoai":"0"},page=pp)[2]; check("Chưa thực hiện checklist" in m, "chưa checklist → không hoàn thành được")
ck=re.search(r'href="/KiemTra/Ghi\?mauId=(\d+)&amp;taiSanId=(\d+)&amp;phieuBaoTriId=(\d+)"|href="/KiemTra/Ghi\?mauId=(\d+)&taiSanId=(\d+)&phieuBaoTriId=(\d+)"', pp)
g=[x for x in ck.groups() if x]; mau=g[0]
pg=k.get(f"/KiemTra/Ghi?mauId={mau}&taiSanId={lts}&phieuBaoTriId={lp}")[1]
mucs=re.findall(r'name="giaTri\[(\d+)\]"', pg); mucs=list(dict.fromkeys(mucs))
vals={f"giaTri[{mucs[0]}]":"true", f"giaTri[{mucs[1]}]":"92", f"giaTri[{mucs[2]}]":"55", f"giaTri[{mucs[3]}]":"true"}
st,loc,m,_=k.post("/KiemTra/Ghi",{"mauId":mau,"taiSanId":lts,"phieuBaoTriId":lp,**vals},page=pg); print("checklist:", m[:140])
check("BẤT THƯỜNG" in m and "Nhiệt độ CPU" in m, "checklist có mục vượt ngưỡng → báo bất thường")
# tạo mới hoặc gắn vào phiếu sửa chữa đang mở của tài sản (bộ kiểm thử khác có thể đã mở trước)
check(q("select count(*) from PhieuSuaChuas where TaiSanId=? and LoaiCongViec=1 and TrangThai not in (6,7,8)",lts)[0][0]>=1 and "phiếu sửa chữa" in m, "mục không đạt tự tạo/gắn phiếu sửa chữa (CM từ PM)")
check(q("select TrangThai from PhieuSuaChuas where Id=?",lp)[0][0]==3, "lệnh bảo trì chuyển Đang xử lý khi làm checklist")
pp=k.get(f"/PhieuSuaChua/Details/{lp}")[1]; check("Đã thực hiện checklist" in pp, "lệnh hiển thị kết quả checklist")
m=k.post(f"/PhieuSuaChua/HoanThanh/{lp}",{"bienPhap":"Đã vệ sinh; tạo phiếu xử lý nhiệt độ","gioCong":"1","chiPhiThueNgoai":"0"},page=pp)[2]; check("Đã hoàn thành" in m, "có checklist → hoàn thành được")
lst=k.get("/KiemTra")[1].split("Kiểm tra gần đây")[0]; check("Checklist bảo trì máy tính" not in lst, "mẫu dùng cho bảo trì không xuất hiện ở danh sách kiểm tra theo chu kỳ")
# 5. Dự trù & gợi ý chu kỳ
dt=k.get("/KeHoachBaoTri/DuTru")[1]; check("Kiểm tra UPS 6 tháng" in dt and "BAT-UPS" in dt, "dự trù có kế hoạch UPS và vật tư ắc quy")
st,b,_=k.req("/KeHoachBaoTri/XuatDuTru"); check(st==200 and b.startswith("PK"), "xuất Excel dự trù")
st,b,_=k.req(f"/KeHoachBaoTri/GoiYChuKy?loaiId={pc}"); check('"chuKy":180' in b, "gợi ý chu kỳ theo khuyến cáo của loại")
cc=q("select t.Id from TaiSans t join KhoaPhongs k on k.Id=t.KhoaPhongId where k.LaKhoaTrongYeu=1 and t.LoaiThietBiId=? limit 1",pc)[0][0]
st,b,_=k.req(f"/KeHoachBaoTri/GoiYChuKy?taiSanId={cc}"); check('"chuKy":90' in b and "tr\\u1ECDng y\\u1EBFu" in b, "rút ngắn chu kỳ cho thiết bị khoa trọng yếu")
check(U("nhanvien").get("/KeHoachBaoTri/Lich")[2].startswith("/Account/AccessDenied"), "nhân viên không xem lịch bảo trì")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
