exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, json, io, openpyxl
k=U("ktv1")
st,loc,m,_=k.post("/KiemKe/Create",{"Ten":"Kiểm kê E2E","ToKiemKe":"KTV An"}); print("mở đợt:", m)
did=loc.rsplit("/",1)[1]
_,loc2,m2,_=k.post("/KiemKe/Create",{"Ten":"Trùng phạm vi"}); print("mở đợt trùng:", m2)
# lấy 2 tài sản ở 2 khoa khác nhau
lst=k.get("/TaiSan?page=1")[1]
rows=re.findall(r'/TaiSan/Details/(\d+)">([A-Z]+-\d+)</a></td>\s*<td>[^<]*<div[^>]*>[^<]*</div></td>\s*<td class="small">([^<]*)</td>', lst)
khoas=dict(re.findall(r'<option value="(\d+)">([^<]+)</option>', k.get(f"/KiemKe/Quet/{did}")[1]))
ten2id={v:kk for kk,v in khoas.items()}
a=rows[0]; b=next(r for r in rows if r[2]!=a[2] and r[2] in ten2id)
qpage=k.get(f"/KiemKe/Quet/{did}")[1]
def quet(ma, khoa, page=qpage, user=k, tt=""):
    st,b,_=user.req("/KiemKe/QuetMa",{"__RequestVerificationToken":user.token(page),"DotId":did,"Ma":ma,"KhoaThucTeId":khoa,"TinhTrang":tt,"GhiChu":""})
    return json.loads(b)
r=quet(a[1], ten2id.get(a[2],"")); print("1 khớp:", r["ketQuaTen"], r["ma"])
r=quet(f"https://thietbi.bth.org.vn/qr/{b[1]}", ten2id[a[2]] if a[2] in ten2id else ""); print("2 QR-URL sai vị trí:", r["ketQuaTen"], "| sổ:", r["khoaSoSach"], "| thực tế:", r["khoaThucTe"])
r=quet(a[1].lower(), ten2id.get(a[2],"")); print("3 quét lại:", "đã kiểm" if r["daKiem"] else r)
r=quet("XYZ-999", ten2id.get(a[2],""), tt="Hong"); print("4 mã lạ:", r["ketQuaTen"], r["ten"])
# Trưởng khoa CC không quét đợt toàn viện
tk=U("truongkhoacc"); print("TK vào trang quét đợt toàn viện:", tk.get(f"/KiemKe/Quet/{did}")[2])
d=k.get(f"/KiemKe/Details/{did}")[1]
print("thống kê:", re.findall(r'(Khớp|Sai vị trí|Thừa \(ngoài sổ\)|Chưa kiểm) <span class="badge bg-light text-dark">(\d+)', d))
print("khoá sổ:", k.post(f"/KiemKe/KhoaSo/{did}",{})[2])
print("quét sau khoá:", quet(a[1], ten2id.get(a[2],""))["loi"])
x=k.op.open(urllib.request.Request(BASE+f"/KiemKe/Xuat/{did}")).read(); w=openpyxl.load_workbook(io.BytesIO(x))
print("xlsx:", w.sheetnames, "dòng:", w["KiemKe"].max_row-4, "| tổng hợp:", [(r[0].value,r[1].value) for r in w["TongHop"].iter_rows(min_row=2)])
print("biên bản:", k.get(f"/KiemKe/InBienBan/{did}")[0])
# Đợt riêng khoa CC cho trưởng khoa
cc=next(kk for kk,v in khoas.items() if v=="Khoa Cấp cứu")
_,loc,m,_=k.post("/KiemKe/Create",{"Ten":"Kiểm kê CC","KhoaPhongId":cc}); d2=loc.rsplit("/",1)[1]; print("đợt CC:", m)
p2=tk.get(f"/KiemKe/Quet/{d2}")[1]; print("TK CC mở trang quét:", "Html5Qrcode" in p2)
cc_ma=re.findall(r'/TaiSan/Details/\d+">([A-Z]+-\d+)', tk.get("/TaiSan")[1])[0]
did_save=did; did=d2
r=quet(cc_ma, cc, page=p2, user=tk); print("TK quét tài sản khoa mình:", r["ketQuaTen"])
r=quet(cc_ma, "", page=p2, user=tk); print("quét ở khoa khác phạm vi:", r.get("loi"))
print("nhân viên vào kiểm kê:", U("nhanvien").get("/KiemKe")[2])
