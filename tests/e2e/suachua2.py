# Kiểm thử phiếu sửa chữa mở rộng: nguyên nhân, giờ công theo KTV, chi phí nhân công, thiết bị mượn tạm (cần app demo đang chạy)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, datetime
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
a=U("admin"); k=U("ktv1"); k2=U("ktv2")
a.post("/ThamSo/Luu",{"giaTri[SuaChua.DonGiaGioCong]":"80000"},page=a.get("/ThamSo")[1])
now=datetime.datetime.now()
pid,khoa,tsid=q("select Id, KhoaPhongId, TaiSanId from PhieuSuaChuas where TrangThai=1 and LoaiCongViec=1 and ThietBiTamId is null order by Id desc limit 1")[0]
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
check('action="/PhieuSuaChua/GhiGioCong/' not in pg, "phiếu mới chưa tiếp nhận: chưa ghi giờ công")
k.post(f"/PhieuSuaChua/TiepNhan/{pid}",{"kyThuatVien":"ktv1","hinhThuc":"TuSua"},page=pg)
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
k.post(f"/PhieuSuaChua/BatDau/{pid}",{},page=pg); pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
# Thiết bị mượn tạm
if not q("select 1 from TaiSans where TrangThai=1 and Id<>?",tsid):
    # dữ liệu mẫu không có máy dự phòng → đưa một máy (không có phiếu mở) về kho CNTT
    cntt=q("select Id from KhoaPhongs where Ma='CNTT'")[0][0]
    DB.execute("update TaiSans set TrangThai=1, KhoaPhongId=? where Id=(select Id from TaiSans t where TrangThai=2 and Id<>? and not exists(select 1 from PhieuSuaChuas p where p.TaiSanId=t.Id and p.TrangThai<6) limit 1)",(cntt,tsid)); DB.commit()
tam=q("select Id, MaTaiSan, KhoaPhongId from TaiSans where TrangThai=1 and Id<>? limit 1",tsid)[0]
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]; check(f'value="{tam[0]}"' in pg, "form cho mượn liệt kê thiết bị trong kho")
dang=q("select Id from TaiSans where TrangThai=2 and Id<>? limit 1",tsid)[0][0]
m=k.post(f"/PhieuSuaChua/ChoMuonTam/{pid}",{"thietBiTamId":dang},page=pg)[2]; check("Trong kho" in m, "chỉ cho mượn thiết bị đang trong kho")
m=k.post(f"/PhieuSuaChua/ChoMuonTam/{pid}",{"thietBiTamId":tam[0]},page=pg)[2]; print("mượn:", m)
r=q("select TrangThai, KhoaPhongId from TaiSans where Id=?",tam[0])[0]
check("Đã cho mượn" in m and r==(2,khoa), "cho mượn: thiết bị → đang sử dụng tại khoa báo hỏng")
check(q("select count(*) from LichSuTaiSans where TaiSanId=? and NoiDung like 'Cho mượn tạm%'",tam[0])[0][0]==1, "ghi lịch sử thiết bị mượn")
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
check("Thiết bị cho mượn tạm" in pg and tam[1] in pg, "trang phiếu hiện thiết bị đang mượn")
m=k.post(f"/PhieuSuaChua/Huy/{pid}",{"lyDo":"x"},page=pg)[2]; check("Thu hồi thiết bị" in m, "không huỷ phiếu khi còn thiết bị mượn")
# Giờ công theo KTV
f=lambda d: d.strftime("%Y-%m-%dT%H:%M")
b1=now-datetime.timedelta(hours=3); e1=now-datetime.timedelta(hours=1, minutes=30)
m=k.post(f"/PhieuSuaChua/GhiGioCong/{pid}",{"batDau":f(b1),"ketThuc":f(e1),"ghiChu":"Kiểm tra nguồn"},page=pg)[2]; check("1,5" in m or "1.5" in m, f"ghi giờ công của tôi ({m})")
m=k.post(f"/PhieuSuaChua/GhiGioCong/{pid}",{"nguoi":"ktv1","batDau":f(b1+datetime.timedelta(minutes=30)),"ketThuc":f(e1+datetime.timedelta(minutes=30))},page=pg)[2]; check("trùng" in m, "chống chồng lấn giờ công cùng người")
m=k.post(f"/PhieuSuaChua/GhiGioCong/{pid}",{"nguoi":"ktv2","batDau":f(b1),"ketThuc":f(b1+datetime.timedelta(hours=1))},page=pg)[2]; check("ktv2" in m and "2,5" in m, "ghi hộ KTV khác, tổng 2,5 giờ")
m=k.post(f"/PhieuSuaChua/GhiGioCong/{pid}",{"batDau":f(now+datetime.timedelta(hours=1)),"ketThuc":f(now+datetime.timedelta(hours=2))},page=pg)[2]; check("tương lai" in m, "không ghi giờ tương lai")
check(float(q("select GioCong from PhieuSuaChuas where Id=?",pid)[0][0])==2.5, "giờ công phiếu = tổng nhật ký")
# Hoàn thành: bắt buộc nhóm nguyên nhân
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]; check('name="nhomNguyenNhan"' in pg and "readonly" in pg, "form hoàn thành: chọn nguyên nhân, giờ công tự tính")
m=k.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"bienPhap":"Thay nguồn","gioCong":"0","chiPhiThueNgoai":"0"},page=pg)[2]; check("nhóm nguyên nhân" in m, "bắt buộc chọn nhóm nguyên nhân")
m=k.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"bienPhap":"Thay nguồn","gioCong":"0","chiPhiThueNgoai":"0","nhomNguyenNhan":"NguonDien","hanhDongKhacPhuc":"ThayLinhKien","nguyenNhan":"Nguồn 450W cháy"},page=pg)[2]; print("hoàn thành:", m)
r=q("select TrangThai, NhomNguyenNhan, HanhDongKhacPhuc, GioCong, ChiPhiNhanCong from PhieuSuaChuas where Id=?",pid)[0]
check(r[0]==6 and r[1]==4 and r[2]==2 and float(r[3])==2.5 and float(r[4])==200000, "lưu nguyên nhân, hành động; chi phí nhân công = 2,5 × 80.000")
check("thu hồi thiết bị cho mượn" in m, "nhắc thu hồi thiết bị mượn khi hoàn thành")
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
check("Nguồn điện" in pg and "nhân công 200.000" in pg and "Máy đã sửa xong" in pg, "trang phiếu: nguyên nhân, chi phí nhân công, cảnh báo thu hồi")
m=k.post(f"/PhieuSuaChua/GhiGioCong/{pid}",{"batDau":f(b1-datetime.timedelta(hours=1)),"ketThuc":f(b1)},page=pg)[2]; check("không ghi thêm" in m, "đã hoàn thành: khoá giờ công")
m=k.post(f"/PhieuSuaChua/ThuHoiTam/{pid}",{},page=pg)[2]
check("Đã thu hồi" in m and q("select TrangThai, KhoaPhongId from TaiSans where Id=?",tam[0])[0]==(1,tam[2]), "thu hồi: thiết bị về kho, khoa gốc")
# BC07, KPI chi phí gồm nhân công
w=a.get(f"/BaoCaoChuan/Xem?ma=BC07&tu={(now-datetime.timedelta(days=400)).date()}&den={now.date()}")[1]
check("Nguồn điện" in w and "Tổng cộng" in w and "Thay linh kiện" in w, "BC07 thống kê nguyên nhân sự cố")
w=a.get(f"/BaoCaoChuan/Xem?ma=BC03&tu={now.date()-datetime.timedelta(days=400)}&den={now.date()}")[1]; check("Nhân công nội bộ" in w, "BC03 có cột chi phí nhân công")
a.post("/ThamSo/Luu",{"giaTri[SuaChua.DonGiaGioCong]":"0"},page=a.get("/ThamSo")[1])
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
