# Quản lý Trang thiết bị CNTT – Bệnh viện

Ứng dụng web quản lý tài sản, cấu hình, sửa chữa/bảo trì (CMMS), điều chuyển và thanh lý trang thiết bị máy tính, CNTT toàn bệnh viện.

**Công nghệ:** ASP.NET Core MVC (.NET 10) · Entity Framework Core · SQL Server · ASP.NET Identity · Bootstrap 5.

## Tài liệu

| Tài liệu | Đường dẫn |
|---|---|
| Đặc tả yêu cầu phần mềm (SRS v1.1, có đối chiếu SpeedMaint) | [docs/SRS.md](docs/SRS.md) |
| Tài liệu thiết kế hệ thống | [docs/ThietKeHeThong.md](docs/ThietKeHeThong.md) |
| Mockup giao diện (mã nguồn các artboard) | [docs/mockup/](docs/mockup/) |
| Quyết định nghiệp vụ & danh mục tham số hệ thống | [docs/QuyetDinhNghiepVu.md](docs/QuyetDinhNghiepVu.md) |

## Kiểm thử

```bash
dotnet test tests/ThietBiCNTT.Tests          # unit test quy tắc nghiệp vụ (SLA, nghiệm thu, điều chuyển, thanh lý)
python3 tests/e2e/e2e.py                      # end-to-end theo vai trò (cần app demo đang chạy, xem đầu file)
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
