/* ============================================================================
   02 - Sao lưu CSDL hằng ngày (dùng được cả SQL Server Express — không cần SQL Agent)
   Gọi từ Windows Task Scheduler, VD 01:30 hằng ngày:
     Program:   sqlcmd
     Arguments: -S localhost -E -b -i "D:\ThietBiCNTT\sql\02-sao-luu.sql" -o "D:\ThietBiCNTT\logs\saoluu.log"
   (Sửa tên CSDL / thư mục sao lưu ở 2 dòng :setvar bên dưới.)
   Giữ lại N ngày gần nhất bằng tác vụ dọn file (xem tài liệu mục 6.2).
   Phiên bản có SQL Agent: tạo Job chạy cùng câu lệnh, kèm Maintenance Plan "Cleanup".
   ========================================================================== */
:setvar TenCsdl "ThietBiCNTT"
:setvar ThuMuc  "D:\ThietBiCNTT\backup"
SET NOCOUNT ON;
DECLARE @file nvarchar(400) = N'$(ThuMuc)\$(TenCsdl)_' + FORMAT(SYSDATETIME(), 'yyyyMMdd_HHmm') + N'.bak';
BACKUP DATABASE [$(TenCsdl)] TO DISK = @file
    WITH COMPRESSION, CHECKSUM, INIT, STATS = 25,
         NAME = N'$(TenCsdl) - sao lưu đầy đủ hằng ngày';
RESTORE VERIFYONLY FROM DISK = @file WITH CHECKSUM;
PRINT N'Đã sao lưu và kiểm tra: ' + @file;
/* Express không hỗ trợ COMPRESSION → nếu báo lỗi, xoá "COMPRESSION," ở trên. */
