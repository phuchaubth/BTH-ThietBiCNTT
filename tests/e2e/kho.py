# Kiểm thử kho vật tư (cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3, os
k=U("ktv1"); a=U("admin")
idx=k.get("/Kho")[1]
def ton_tong(ma):
    m=re.search(rf'<td class="fw-semibold">{ma}</td>.*?<td class="text-end fw-semibold">([^<]*)</td>', idx, re.S); return m.group(1) if m else None
print("tồn SSD-256 sau chuyển dữ liệu cũ:", ton_tong("SSD-256"))
vt={m:i for i,m in re.findall(r'<option value="(\d+)" data-gia="\d+" data-dvt="[^"]*">([A-Z0-9-]+) –', k.get("/Kho/Lap?loai=Nhap")[1])}
p=k.get("/Kho/Lap?loai=Nhap")[1]; kho_chinh=re.search(r'id="kho"[^>]*>\s*<option selected="selected" value="(\d+)"', p).group(1)
st,loc,m,_=k.post("/Kho/Lap",{"LyDo":"NhapMua","KhoId":kho_chinh,"SoChungTu":"HĐ 0012345","dong[0].VatTuId":vt["SSD-256"],"dong[0].SoLuong":"10","dong[0].DonGia":"750000"},page=p)
print("nhập 10 SSD @750k:", m)
tk=k.get(f"/Kho/TheKho?vatTuId={vt['SSD-256']}")[1]; print("   giá BQ:", re.findall(r'đơn giá bình quân ([^ ]*) đ', tk), "| tổng tồn:", re.findall(r'Tổng tồn ([^ ]*)', tk))
print("tạo kho phụ:", a.post("/KhoDanhMuc/Save",{"Ma":"KHO-B","Ten":"Kho toà nhà B","Loai":"Phu","DangHoatDong":["true","false"]})[2])
p=k.get("/Kho/Lap?loai=Chuyen")[1]; kho_b=re.search(r'<option value="(\d+)">Kho toà nhà B', p).group(1)
print("chuyển 4 SSD sang B:", k.post("/Kho/Lap",{"LyDo":"ChuyenKho","KhoId":kho_chinh,"KhoDenId":kho_b,"dong[0].VatTuId":vt["SSD-256"],"dong[0].SoLuong":"4"},page=p)[2])
p=k.get("/Kho/Lap?loai=Xuat")[1]
print("xuất 100 SSD từ B (vượt tồn):", k.post("/Kho/Lap",{"LyDo":"XuatHuy","KhoId":kho_b,"dong[0].VatTuId":vt["SSD-256"],"dong[0].SoLuong":"100"},page=p)[2])
print("xuất cấp khoa thiếu khoa:", k.post("/Kho/Lap",{"LyDo":"XuatCapKhoa","KhoId":kho_b,"dong[0].VatTuId":vt["SSD-256"],"dong[0].SoLuong":"1"},page=p)[2])
idx=k.get("/Kho")[1]; print("tổng tồn SSD sau (=4+10+cũ trong 2 kho):", ton_tong("SSD-256"))
# Kiểm kê kho chính: MS-USB thực tế 10
kk=k.get(f"/Kho/KiemKe?khoId={kho_chinh}")[1]
st,loc,m,_=k.post("/Kho/KiemKe",{"khoId":kho_chinh,f"thucTe[{vt['MS-USB']}]":"10","dienGiai":"Kiểm kê quý IV"},page=kk); print("kiểm kê:", m); pid=loc.rsplit("/",1)[1]
print("KTV duyệt:", k.post(f"/Kho/Duyet/{pid}",{"dongY":"true"})[1])
print("admin duyệt:", a.post(f"/Kho/Duyet/{pid}",{"dongY":"true"})[2])
idx=k.get("/Kho")[1]; print("MS-USB sau kiểm kê:", ton_tong("MS-USB"))
print("in phiếu:", k.get(f"/Kho/InPhieu/{pid}")[0], "| nhân viên vào kho:", U("nhanvien").get("/Kho")[2])
db=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
lech=db.execute("select v.Ma, v.TonKho, coalesce(sum(t.SoLuong),0) s from VatTus v left join TonKhoVatTus t on t.VatTuId=v.Id group by v.Id having abs(v.TonKho - s) > 0.001").fetchall()
the=db.execute("select count(*) from TheKhos").fetchone()[0]
print("bất biến tổng tồn = Σ tồn kho:", "OK" if not lech else lech, "| dòng thẻ kho:", the)
