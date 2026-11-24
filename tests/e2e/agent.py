# Kiểm thử agent thu thập cấu hình (mô phỏng JSON agent gửi; cần app demo đang chạy — xem e2e.py)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, json, sqlite3
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql, *a): return DB.execute(sql, a).fetchall()
def api(path, key, body):
    r=urllib.request.Request(BASE+path, data=json.dumps(body).encode(), headers={"Content-Type":"application/json","X-Api-Key":key}, method="POST")
    try:
        with urllib.request.urlopen(r) as x: return x.status, json.loads(x.read())
    except urllib.error.HTTPError as e: return e.code, e.read().decode(errors="ignore")
a=U("admin"); k=U("ktv1")
def tao_khoa(ten, pv):
    p=a.get("/TichHop")[1]; st,b,loc=a.req("/TichHop/TaoKhoa", {"ten":ten,"phamVi":pv,"__RequestVerificationToken":a.token(p)})
    return re.search(r'id="khoaMoi" value="([^"]+)"', a.get(loc)[1]).group(1)
key=tao_khoa("Agent máy trạm", ["cauhinh.ghi"]); key2=tao_khoa("Chỉ đọc", ["taisan.doc"])
PM=[{"ten":"Google Chrome","phienBan":"131.0"},{"ten":"HIS Client","phienBan":"5.2"},{"ten":"UltraViewer","phienBan":"6.6"}]
def goi(host, **kw):
    d={"hostname":host,"mac":[],"ip":["10.10.2.15"],"cpu":"Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz","ramGb":8,"oCungTongGb":237.9,"oCungTrongGb":80.2,
       "oCung":"SSD KINGSTON SA400 240GB","heDieuHanh":"Microsoft Windows 10 Pro (10.0.19045)","nguoiDangNhap":"BTH\\dieuduong1","phienBanAgent":"1.0","phanMem":PM}
    d.update(kw); return api("/api/v1/agent/cauhinh", key, d)
st,b=api("/api/v1/agent/cauhinh", key2, {"hostname":"X"}); check(st==403, "khoá không có phạm vi cauhinh.ghi → 403")
# 1. Ghép theo mã tài sản khai báo
st,b=goi("CC-PC01", maTaiSan="PC-0001"); print("PC-0001:", b)
check(st==200 and b["maTaiSan"]=="PC-0001" and b["cachGhep"]=="mã tài sản", "ghép theo mã tài sản khai báo")
check(any("RAM" in x for x in b["thayDoi"]) or any("CPU" in x for x in b["thayDoi"]), "phát hiện khác biệt với cấu hình khai báo tay")
st,b=goi("CC-PC01", maTaiSan="PC-0001"); check(st==200 and b["cachGhep"]=="đã ghép" and b["thayDoi"]==[] and b["phanMemThem"]==[], "gửi lại không đổi → không ghi gì")
n0=q("select count(*) from LichSuTaiSans l join TaiSans t on t.Id=l.TaiSanId where t.MaTaiSan='PC-0001' and l.LoaiSuKien=3")[0][0]
st,b=goi("CC-PC01", maTaiSan="PC-0001", ip=["10.10.9.99"]); check(b["thayDoi"]==[], "chỉ đổi IP (DHCP) → không ghi lịch sử")
check(q("select c.DiaChiIP from CauHinhs c join TaiSans t on t.Id=c.TaiSanId where t.MaTaiSan='PC-0001'")[0][0]=="10.10.9.99", "IP vẫn được cập nhật")
pm2=[x for x in PM if x["ten"]!="UltraViewer"]+[{"ten":"AnyDesk","phienBan":"8.1"}]
st,b=goi("CC-PC01", maTaiSan="PC-0001", ramGb=16, phanMem=pm2, ip=["10.10.9.99"]); print("nâng RAM + đổi phần mềm:", b)
check(any(x.startswith("RAM") and "16 GB" in x for x in b["thayDoi"]) and b["phanMemThem"]==["AnyDesk"] and b["phanMemBo"]==["UltraViewer"], "phát hiện nâng RAM, cài/gỡ phần mềm")
n1=q("select count(*) from LichSuTaiSans l join TaiSans t on t.Id=l.TaiSanId where t.MaTaiSan='PC-0001' and l.LoaiSuKien=3")[0][0]
check(n1-n0==2, f"ghi 2 dòng lịch sử cấu hình (thông số + phần mềm): {n1-n0}")
tb=k.get("/ThongBao")[1]; check("Cấu hình PC-0001 (CC-PC01) thay đổi" in tb, "CNTT nhận thông báo thay đổi cấu hình")
# 2. Ghép theo serial BIOS, hostname, MAC
ser=q("select SoSerial from TaiSans where MaTaiSan='PC-0003'")[0][0]
st,b=goi("MAY-MOI-01", serialBios=ser); check(b["maTaiSan"]=="PC-0003" and b["cachGhep"]=="serial", "ghép theo serial BIOS")
st,b=goi("san-pc05"); check(b["maTaiSan"]=="PC-0005" and b["cachGhep"]=="hostname", "ghép theo hostname (không phân biệt hoa thường)")
mac=q("select c.DiaChiMAC from CauHinhs c join TaiSans t on t.Id=c.TaiSanId where t.MaTaiSan='PC-0006'")[0][0]
st,b=goi("HOST-LA", mac=[mac.replace(":","").lower()]); check(b["maTaiSan"]=="PC-0006" and b["cachGhep"]=="MAC", "ghép theo MAC (chuẩn hoá định dạng)")
st,b=goi("ABC", serialBios="To be filled by O.E.M."); check(b["maTaiSan"] is None, "serial rác của máy lắp ráp không dùng để ghép")
# 3. Máy lạ → chưa ghép → ghép tay
st,b=goi("LAB-XYZ", ramGb=4, heDieuHanh="Microsoft Windows 7 Professional (6.1.7601)", oCungTrongGb=5)
check(st==200 and b["maTaiSan"] is None and b["cachGhep"]=="chưa ghép", "máy lạ → chưa ghép")
idx=k.get("/Agent")[1]; check("LAB-XYZ" in idx and "Chưa ghép" in idx, "danh sách agent hiển thị máy chưa ghép")
hang=re.findall(r'asp-route-id|href="/Agent/ChiTiet/\d+">([^<]+)</a>', k.get("/Agent?loc=khongdat")[1])
check([h for h in hang if h]==["LAB-XYZ"], f"lọc máy không đạt chuẩn HIS: {hang}")
mid=b["mayId"]
p=k.get(f"/Agent/ChiTiet/{mid}")[1]; check("RAM ≥ 8 GB" in p and "bi-x-circle-fill" in p, "trang chi tiết đối chiếu chuẩn HIS")
print("ghép trùng:", k.post(f"/Agent/Ghep/{mid}",{"maTaiSan":"PC-0001"},page=p)[2])
m=k.post(f"/Agent/Ghep/{mid}",{"maTaiSan":"PC-0004"},page=p)[2]; check("Đã ghép" in m, "ghép tay với tài sản")
check(q("select c.Hostname from CauHinhs c join TaiSans t on t.Id=c.TaiSanId where t.MaTaiSan='PC-0004'")[0][0]=="LAB-XYZ", "ghép tay cập nhật cấu hình tài sản")
st,b=goi("LAB-XYZ", ramGb=4); check(b["maTaiSan"]=="PC-0004" and b["cachGhep"]=="đã ghép", "lần gửi sau giữ nguyên ghép")
st,s,_=k.req("/Agent/TaiScript?ten=ThuThapCauHinh.ps1"); check(st==200 and "/api/v1/agent/cauhinh" in s, "tải script agent")
check(k.req("/Agent/TaiScript?ten=../appsettings.json")[0]==404, "không tải được tệp ngoài danh sách")
check(U("nhanvien").get("/Agent")[2].startswith("/Account/AccessDenied"), "nhân viên không vào trang agent")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
