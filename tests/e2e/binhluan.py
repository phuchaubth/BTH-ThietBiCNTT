# Kiểm thử bình luận: @nhắc tên, nội bộ CNTT (cần app demo đang chạy)
exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, sqlite3
ok=True
def check(c, label):
    global ok; print(("PASS " if c else "FAIL ")+label); ok = ok and c
DB=sqlite3.connect("src/ThietBiCNTT.Web/thietbicntt-demo.db")
def q(sql,*a): return DB.execute(sql,a).fetchall()
k=U("ktv1"); k2=U("ktv2"); nv=U("nhanvien"); tk=U("truongkhoacc")
cc=q("select Id from KhoaPhongs where Ma='CC'")[0][0]
pid,so=q("select Id, SoPhieu from PhieuSuaChuas where NguoiTao='nhanvien' and KhoaPhongId=? and TrangThai<6 order by Id desc limit 1",cc)[0]
def gui(u, path, data, page):
    d=dict(data); d["__RequestVerificationToken"]=u.token(page)
    st,b,loc=u.req(path,d); return u.get(loc)[1]
def tb(u, chua): return q("select count(*) from ThongBaos where NguoiNhan=? and TieuDe like ?",u,f"%{chua}%")[0][0]
n2=tb("ktv2",f"nhắc bạn trong {so}"); ntk=tb("truongkhoacc",f"nhắc bạn trong {so}")
pg=k.get(f"/PhieuSuaChua/Details/{pid}")[1]
check('name="noiBo"' in pg, "CNTT thấy tuỳ chọn bình luận nội bộ")
pg=gui(k,f"/PhieuSuaChua/BinhLuan/{pid}",{"noiDung":"Nhờ @ktv2 kiểm tra nguồn, báo @truongkhoacc. @khongco"},pg)
check("Đã gửi và nhắc ktv2, truongkhoacc" in pg and "Không nhắc được: khongco" in pg, "nhắc tên hợp lệ / báo tên không tồn tại")
check(tb("ktv2",f"nhắc bạn trong {so}")==n2+1 and tb("truongkhoacc",f"nhắc bạn trong {so}")==ntk+1, "người được nhắc nhận thông báo 'nhắc bạn'")
check('<span class="text-primary fw-semibold">@ktv2</span>' in pg, "tô sáng @tên trong bình luận")
# nội bộ
pg=gui(k,f"/PhieuSuaChua/BinhLuan/{pid}",{"noiDung":"Nội bộ: nghi do người dùng tự tháo RAM, @nhanvien @ktv2","noiBo":"true"},pg)
check("Nội bộ CNTT" in pg and "Không nhắc được: nhanvien" in pg, "bình luận nội bộ: không nhắc được người ngoài CNTT")
check(q("select NoiBo from BinhLuanPhieus where PhieuSuaChuaId=? order by Id desc limit 1",pid)[0][0]==1, "lưu cờ nội bộ")
pn=nv.get(f"/PhieuSuaChua/Details/{pid}")[1]; pt=tk.get(f"/PhieuSuaChua/Details/{pid}")[1]
check("tự tháo RAM" not in pn and "tự tháo RAM" not in pt and "Nhờ" in pn, "người báo / trưởng khoa không thấy bình luận nội bộ, vẫn thấy bình luận thường")
check(q("select count(*) from ThongBaos where NguoiNhan='nhanvien' and NoiDung like '%tự tháo RAM%'")[0][0]==0, "người báo không nhận thông báo bình luận nội bộ")
check('name="noiBo"' not in pn, "người báo không có tuỳ chọn nội bộ")
# người báo gửi noiBo=true bị bỏ qua
st,loc,m,_=nv.post(f"/PhieuSuaChua/BinhLuan/{pid}",{"noiDung":"Máy vẫn chưa lên, @ktv1 xem giúp","noiBo":"true"},page=pn)
check(q("select NoiBo from BinhLuanPhieus where PhieuSuaChuaId=? order by Id desc limit 1",pid)[0][0]==0, "người ngoài CNTT không tạo được bình luận nội bộ")
check(tb("ktv1",f"nhắc bạn trong {so}")>=1, "người báo nhắc được KTV")
# XSS
st,loc,m,_=k.post(f"/PhieuSuaChua/BinhLuan/{pid}",{"noiDung":"<script>alert(1)</script> @ktv2"},page=pg)
pg=k.req(loc)[1]; check("<script>alert(1)</script>" not in pg and "&lt;script&gt;" in pg, "nội dung bình luận được mã hoá HTML")
print("KẾT QUẢ:", "TẤT CẢ ĐẠT" if ok else "CÓ LỖI")
