exec(open('tests/e2e/e2e.py').read().split("ok=True")[0])
import re, io, openpyxl, uuid, datetime
import tempfile; S=tempfile.gettempdir()
k=U("ktv1")
mau=k.op.open(urllib.request.Request(BASE+"/TaiSan/MauNhap")).read()
wb=openpyxl.load_workbook(io.BytesIO(mau)); ws=wb["TaiSan"]
print("sheets:", wb.sheetnames, "| dv:", len(ws.data_validations.dataValidation))
ws.delete_rows(2)
rows=[
 ["PC-1001","Máy tính HP 280 G9","PC","HP","280 G9","SNX1001","KB","Phòng khám 5","Ngân sách nhà nước",14500000,"10/01/2025","15/01/2025","10/01/2028","","HĐ 01/2025","Đang sử dụng","i5-13400","16GB","SSD 512GB","Windows 11 Pro","OEM","10.10.2.101","AA:BB:CC:00:10:01","KB-PK05","HIS",""],
 ["PC-1002","Máy tính HP 280 G9","pc","HP","280 G9","SNX1002","","","Tài trợ",datetime.datetime(2025,1,10),datetime.datetime(2025,1,10),None,None,"","","","","","","","","","","","",""],
 ["PC-1001","Trùng mã trong file","PC"]+[""]*23,
 ["PC-1003","Sai loại","XYZ","","","","ZZ","","Abc","mười triệu","32/13/2025"]+[""]*10+["999.1.1.1"]+[""]*4,
 ["PC-0001","Cập nhật máy cũ","PC","Dell","7010","","","","",16000000,"","","","","","","","32GB DDR4"]+[""]*8,
]
for r in rows: ws.append(r)
# PC-1002 nguyên giá đặt nhầm datetime -> sửa thành số kiểu chuỗi "12.500.000"
ws.cell(row=3, column=10).value="12.500.000"
buf=io.BytesIO(); wb.save(buf); data=buf.getvalue()
open(S+"/nhap_test.xlsx","wb").write(data)
def upload(cap):
    page=k.get("/TaiSan/Nhap")[1]; tok=k.token(page)
    b="----"+uuid.uuid4().hex; parts=[]
    for n,v in [("__RequestVerificationToken",tok)]+([("capNhatDaCo","true")] if cap else []):
        parts.append(f'--{b}\r\nContent-Disposition: form-data; name="{n}"\r\n\r\n{v}\r\n'.encode())
    parts.append(f'--{b}\r\nContent-Disposition: form-data; name="file"; filename="nhap.xlsx"\r\nContent-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet\r\n\r\n'.encode()+data+b"\r\n")
    parts.append(f"--{b}--\r\n".encode())
    req=urllib.request.Request(BASE+"/TaiSan/Nhap", data=b"".join(parts), headers={"Content-Type":"multipart/form-data; boundary="+b})
    return html.unescape(k.op.open(req).read().decode())
body=upload(True)
for m in re.finditer(r'<tr class="([^"]*)">\s*<td>(\d+)</td><td class="fw-semibold">([^<]*)</td>.*?<td class="small">\s*(.*?)</td>\s*</tr>', body, re.S):
    print(m.group(2), m.group(3), "|", re.sub(r"\s+"," ",re.sub(r"<[^>]+>"," ",m.group(4))).strip())
print("Có nút xác nhận:", "Xác nhận nhập" in body)
