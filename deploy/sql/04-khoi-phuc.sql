/* ============================================================================
   04 - Khôi phục CSDL từ file sao lưu (diễn tập định kỳ trên máy thử nghiệm!)
   ========================================================================== */
:setvar TenCsdl "ThietBiCNTT"
:setvar FileBak "D:\ThietBiCNTT\backup\ThietBiCNTT_20261121_0130.bak"
USE master;
GO
ALTER DATABASE [$(TenCsdl)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$(TenCsdl)] FROM DISK = N'$(FileBak)' WITH REPLACE, CHECKSUM, STATS = 10;
ALTER DATABASE [$(TenCsdl)] SET MULTI_USER;
GO
