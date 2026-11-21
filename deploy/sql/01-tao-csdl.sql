/* ============================================================================
   01 - Tạo CSDL và tài khoản cho ứng dụng Quản lý TTB CNTT
   Chạy bằng SSMS / sqlcmd với quyền sysadmin:
       sqlcmd -S localhost -E -i 01-tao-csdl.sql
   Sửa 3 biến bên dưới trước khi chạy.
   ========================================================================== */
-- DanhTinhApp:
--   Cách A : SQL cùng máy web           → "IIS AppPool\ThietBiCNTT" (danh tính app pool)
--   Cách A': SQL khác máy, cùng domain  → "BENHVIEN\WEBSRV01$" (tài khoản máy web) hoặc tài khoản dịch vụ domain
-- QuyenCaoKhiCai:
--   1 = cấp db_owner để ứng dụng tự áp migration (Database:AutoMigrate = true)
--   0 = chỉ đọc/ghi + EXECUTE; DBA tự chạy migration-idempotent.sql
:setvar TenCsdl "ThietBiCNTT"
:setvar DanhTinhApp "IIS AppPool\ThietBiCNTT"
:setvar QuyenCaoKhiCai "1"

USE master;
GO
IF DB_ID(N'$(TenCsdl)') IS NULL
BEGIN
    CREATE DATABASE [$(TenCsdl)] COLLATE Vietnamese_CI_AS;
    ALTER DATABASE [$(TenCsdl)] SET RECOVERY FULL;          -- cho phép sao lưu log, khôi phục đến thời điểm
    ALTER DATABASE [$(TenCsdl)] SET READ_COMMITTED_SNAPSHOT ON;
    PRINT N'Đã tạo CSDL $(TenCsdl)';
END
GO

/* ---- Cách A / A': Windows Authentication ---- */
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'$(DanhTinhApp)')
    CREATE LOGIN [$(DanhTinhApp)] FROM WINDOWS WITH DEFAULT_DATABASE = [$(TenCsdl)];
GO

/* ---- Cách B: SQL Authentication (bỏ chú thích nếu không dùng Windows auth) ----
IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'thietbi_app')
    CREATE LOGIN [thietbi_app] WITH PASSWORD = N'<MAT_KHAU_MANH>', CHECK_POLICY = ON, DEFAULT_DATABASE = [$(TenCsdl)];
GO
-- và thay $(DanhTinhApp) = "thietbi_app" ở phần dưới
*/

USE [$(TenCsdl)];
GO
IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(DanhTinhApp)')
    CREATE USER [$(DanhTinhApp)] FOR LOGIN [$(DanhTinhApp)];
GO
ALTER ROLE db_datareader ADD MEMBER [$(DanhTinhApp)];
ALTER ROLE db_datawriter ADD MEMBER [$(DanhTinhApp)];
GRANT EXECUTE TO [$(DanhTinhApp)];
GO
IF '$(QuyenCaoKhiCai)' = '1'
BEGIN
    ALTER ROLE db_owner ADD MEMBER [$(DanhTinhApp)];
    PRINT N'Đã cấp db_owner để ứng dụng tự áp migration. Sau khi chạy ổn định, có thể chạy 03-ha-quyen.sql.';
END
GO
PRINT N'Hoàn tất. Danh tính ứng dụng: $(DanhTinhApp)';
