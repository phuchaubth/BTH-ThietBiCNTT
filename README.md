# Quản lý Trang thiết bị CNTT – Bệnh viện

Ứng dụng web quản lý tài sản, cấu hình, sửa chữa/bảo trì (CMMS), điều chuyển và thanh lý trang thiết bị máy tính, CNTT toàn bệnh viện.

**Công nghệ:** ASP.NET Core MVC (.NET 10) · Entity Framework Core · SQL Server · ASP.NET Identity · Bootstrap 5.

## Tính năng

**Giai đoạn 1:** tài sản & cấu hình (lịch sử thay đổi, QR), yêu cầu sửa chữa / lệnh công việc (SLA giờ hành chính & 24/7, tạm dừng, Kanban, nghiệm thu), bảo trì định kỳ, điều chuyển duyệt nhiều cấp theo giá trị, thanh lý (tháo ổ cứng), dashboard KPI, phân quyền 5 vai trò, tham số hệ thống, người dùng, nhật ký.

**Giai đoạn 2:**
- Nhập tài sản từ Excel (file mẫu, kiểm tra từng dòng, xem trước) · xuất Excel
- Kiểm kê bằng QR (camera điện thoại / súng mã vạch): khớp, sai vị trí, thừa, thiếu, biên bản
- Thông báo: chuông trong ứng dụng, email (SMTP), Telegram; nhắc trước hạn SLA, leo thang 3 cấp, bản tin hằng ngày
- Kiểm tra thiết bị theo phiếu mẫu (ngưỡng số đo, Đạt/Không đạt), bất thường tự tạo phiếu sửa chữa; ảnh / tệp đính kèm
- Hợp đồng (gắn tài sản), bản quyền phần mềm (cấp phát, chặn vượt số lượng), theo dõi gửi hãng
- Báo cáo SLA / MTTR / MTBF / dừng máy / chi phí / năng suất KTV, xuất Excel nhiều sheet

**Giai đoạn 3:**
- Kho vật tư: nhiều kho, phiếu nhập / xuất / chuyển, thẻ kho, giá bình quân gia quyền, kiểm kê kho có duyệt
- Mua sắm: đề xuất (duyệt Trưởng phòng CNTT → P. Vật tư → Lãnh đạo theo ngưỡng giá trị; tự lập từ vật tư dưới định mức) → đơn mua hàng (in đơn, xác nhận đặt theo ngưỡng, cảnh báo quá hạn giao) → nhận hàng từng phần (vật tư tự nhập kho, thiết bị tự tạo hồ sơ tài sản kèm serial, bảo hành)

## Tài liệu

| Tài liệu | Đường dẫn |
|---|---|
| Đặc tả yêu cầu phần mềm (SRS v1.1, có đối chiếu SpeedMaint) | [docs/SRS.md](docs/SRS.md) |
| Tài liệu thiết kế hệ thống | [docs/ThietKeHeThong.md](docs/ThietKeHeThong.md) |
| Mockup giao diện (mã nguồn các artboard) | [docs/mockup/](docs/mockup/) |
| **Hướng dẫn triển khai trên IIS** (kèm script trong `deploy/`) | [docs/TrienKhaiIIS.md](docs/TrienKhaiIIS.md) |
| Chứng chỉ HTTPS Let's Encrypt tự gia hạn cho `thietbi.bth.org.vn` | [docs/ChungChiHTTPS.md](docs/ChungChiHTTPS.md) |
| Quyết định nghiệp vụ & danh mục tham số hệ thống | [docs/QuyetDinhNghiepVu.md](docs/QuyetDinhNghiepVu.md) |

## Kiểm thử

```bash
dotnet test tests/ThietBiCNTT.Tests          # unit test quy tắc nghiệp vụ (SLA, nghiệm thu, điều chuyển, thanh lý)
python3 tests/e2e/e2e.py                      # end-to-end theo vai trò (cần app demo đang chạy, xem đầu file)
# các kịch bản khác: nhap.py, kiemke.py, kiemtra.py, hopdong.py, kho.py, muasam.py, thongbao.py (cần mock.py)
```

## Cấu trúc

```
src/
  ThietBiCNTT.Domain/          Thực thể, enum, quy tắc nghiệp vụ (chuyển trạng thái)
  ThietBiCNTT.Infrastructure/  EF Core DbContext, Identity, nhật ký, seed, service nghiệp vụ
  ThietBiCNTT.Web/             ASP.NET Core MVC: controller, view, job nền bảo trì định kỳ
```

## Chạy thử nhanh (không cần SQL Server)

```bash
cd src/ThietBiCNTT.Web
ASPNETCORE_ENVIRONMENT=Demo dotnet run --no-launch-profile
```
Dùng SQLite + dữ liệu mẫu. Tài khoản mẫu (mật khẩu `Abc@12345`): `admin`, `ktv1`, `ktv2`, `truongkhoacc`, `lanhdao`, `nhanvien`.

## Chạy với SQL Server

1. Sửa `ConnectionStrings:DefaultConnection` trong `src/ThietBiCNTT.Web/appsettings.json`.
2. Migration `KhoiTao` đã có sẵn; hoặc xuất script SQL cho DBA:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef migrations script --idempotent -p src/ThietBiCNTT.Infrastructure -o khoitao.sql
   ```
3. `dotnet run --project src/ThietBiCNTT.Web`: ứng dụng tự áp migration và tạo tài khoản `admin` (mật khẩu `Abc@12345`, **đổi ngay** khi triển khai).
