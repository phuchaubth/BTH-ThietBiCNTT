using System.ComponentModel.DataAnnotations;

namespace ThietBiCNTT.Domain.Common;

/// <summary>Lớp cơ sở cho mọi thực thể: khoá chính + thông tin vết (audit).</summary>
public abstract class BaseEntity
{
    [ScaffoldColumn(false)] public int Id { get; set; }
    [ScaffoldColumn(false)] public DateTime NgayTao { get; set; }
    [ScaffoldColumn(false)] public string? NguoiTao { get; set; }
    [ScaffoldColumn(false)] public DateTime? NgayCapNhat { get; set; }
    [ScaffoldColumn(false)] public string? NguoiCapNhat { get; set; }
}

/// <summary>Lỗi vi phạm quy tắc nghiệp vụ (chuyển trạng thái không hợp lệ...).</summary>
public class DomainException(string message) : Exception(message);
