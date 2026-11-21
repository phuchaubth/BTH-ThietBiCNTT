/* ============================================================================
   03 - (Tuỳ chọn, khuyến nghị) Hạ quyền tài khoản ứng dụng sau khi cài đặt xong
   Khi đó đặt "Database:AutoMigrate": false trong appsettings.Production.json;
   mỗi lần cập nhật phiên bản, DBA chạy file migration-idempotent.sql trong gói cài đặt.
   ========================================================================== */
:setvar TenCsdl     "ThietBiCNTT"
:setvar DanhTinhApp "IIS AppPool\ThietBiCNTT"
USE [$(TenCsdl)];
GO
ALTER ROLE db_owner DROP MEMBER [$(DanhTinhApp)];
PRINT N'Đã bỏ db_owner. Tài khoản ứng dụng còn: db_datareader, db_datawriter, EXECUTE.';
