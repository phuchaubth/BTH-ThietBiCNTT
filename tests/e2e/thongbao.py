# Kiểm thử thông báo: chạy mock.py <log> trước, app demo với ThongBao__TelegramBotToken=TEST123 ThongBao__TelegramApiBase=http://127.0.0.1:8099; rồi python3 tests/e2e/thongbao.py <log>
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, time, sys
LOG=sys.argv[1]
def log(): return open(LOG, encoding="utf-8").read().splitlines() if __import__("os").path.exists(LOG) else []
a=U("admin")
nhom_tb="Th%C3%B4ng%20b%C3%A1o"
p=a.get("/ThamSo?nhom="+nhom_tb)[1]
print("lưu tham số:", a.post("/ThamSo/Luu",{"nhom":"Thông báo","co[ThongBao.EmailBat]":["false","true"],"giaTri[ThongBao.SmtpHost]":"127.0.0.1","giaTri[ThongBao.SmtpPort]":"2525",
  "co[ThongBao.SmtpBaoMat]":"false","giaTri[ThongBao.SmtpUser]":"","giaTri[ThongBao.EmailGui]":"thietbi@bth.org.vn","co[ThongBao.TelegramBat]":["false","true"],
  "giaTri[ThongBao.TelegramNhomCNTT]":"-100999","giaTri[ThongBao.GioTongHop]":"23:59","giaTri[LeoThang.Cap2SauPhut]":"60","giaTri[LeoThang.Cap2NguoiNhan]":"admin","giaTri[LeoThang.Cap3SauPhut]":"240"},page=p)[2])
for u,em,chat in [("ktv1","ktv1@bth.org.vn","111"),("ktv2","ktv2@bth.org.vn","222"),("nhanvien","nv@bth.org.vn","333"),("truongkhoacc","tk@bth.org.vn","444")]:
    x=U(u); print(u, x.post("/TaiKhoan/Luu",{"email":em,"dienThoai":"","telegramChatId":chat,"nhanEmail":["true","false"],"nhanTelegram":["true","false"]})[2])
print("gửi thử HangDoi email:", a.post("/HangDoi/GuiThu",{"kenh":"Email","diaChi":"test@bth.org.vn"})[2])
print("gửi thử HangDoi telegram:", a.post("/HangDoi/GuiThu",{"kenh":"Telegram","diaChi":"555"})[2])
k1=U("ktv1"); print("ktv1 gửi thử:", k1.post("/TaiKhoan/GuiThu",{})[2])
n0=len(log())
nv=U("nhanvien")
ts=re.findall(r'/TaiSan/Details/(\d+)">', nv.get("/TaiSan")[1])[-1]
st,loc,m,_=nv.post("/PhieuSuaChua/Create",{"TaiSanId":ts,"MoTaSuCo":"E2E thông báo: máy không lên nguồn","UuTien":"Cao","XacNhanTrung":"true"})
pid=loc.rsplit("/",1)[1]; print("báo hỏng:", m)
k=U("ktv1"); print("giao ktv2:", k.post(f"/PhieuSuaChua/TiepNhan/{pid}",{"kyThuatVien":"ktv2","hinhThuc":"TuSua"})[2])
k2=U("ktv2"); print("hoàn thành:", k2.post(f"/PhieuSuaChua/HoanThanh/{pid}",{"bienPhap":"Thay nguồn","gioCong":"1","chiPhiThueNgoai":"0","nhomNguyenNhan":"NguonDien"})[2])
print("trả lại:", nv.post(f"/PhieuSuaChua/TraLai/{pid}",{"yKien":"Vẫn chưa lên"})[2])
print("bình luận:", nv.post(f"/PhieuSuaChua/BinhLuan/{pid}",{"noiDung":"Anh xem lại giúp em"})[1])
print("chờ hàng đợi gửi (≤35s)…"); time.sleep(35)
moi=log()[n0:]
for l in moi: print("  ", l)
tb=k2.get("/ThongBao")[1]
print("ktv2 chuông:", re.findall(r'id="demTb"[^>]*>([^<]*)', tb), "| có 'Bạn được giao':", "Bạn được giao" in tb, "| 'bị trả lại':", "bị trả lại" in tb)
nvb=nv.get("/ThongBao")[1]; print("nhanvien nhận 'mời nghiệm thu':", "mời nghiệm thu" in nvb)
tkb=U("truongkhoacc").get("/ThongBao")[1]; print("trưởng khoa CC nhận leo thang/tổng hợp?:", "Leo thang" in tkb)
ad=a.get("/ThongBao")[1]; print("admin nhận leo thang cấp 2:", "Leo thang cấp 2" in ad, "| tổng hợp ngày:", "Tổng hợp ngày" in ad)
hd=a.get("/HangDoi")[1]; print("hàng đợi:", re.findall(r'(Chờ gửi|Đã gửi|Lỗi) <span class="badge bg-light text-dark">(\d+)', hd))
