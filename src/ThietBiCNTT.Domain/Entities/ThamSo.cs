using System.ComponentModel.DataAnnotations;

namespace ThietBiCNTT.Domain.Entities;

public enum KieuThamSo
{
    [Display(Name = "Số nguyên")] SoNguyen = 1,
    [Display(Name = "Số thực")] SoThuc,
    [Display(Name = "Văn bản")] VanBan,
    [Display(Name = "Giờ (HH:mm)")] Gio,
    [Display(Name = "Bật/Tắt")] BatTat,
    /// <summary>Danh sách thứ trong tuần: 2..7 = Thứ 2..Thứ 7, 8 = Chủ nhật.</summary>
    [Display(Name = "Các thứ trong tuần")] ThuTrongTuan
}

/// <summary>Tham số hệ thống — Quản trị chỉnh trên màn hình, không cần sửa mã/khởi động lại.</summary>
public class ThamSoHeThong
{
    [Key, StringLength(100)] public string Ma { get; set; } = "";
    [StringLength(100)] public string Nhom { get; set; } = "";
    [StringLength(200)] public string Ten { get; set; } = "";
    [StringLength(1000)] public string? MoTa { get; set; }
    public KieuThamSo Kieu { get; set; }
    [StringLength(1000)] public string GiaTri { get; set; } = "";
    [StringLength(1000)] public string GiaTriMacDinh { get; set; } = "";
    [StringLength(30)] public string? DonVi { get; set; }
    public decimal? ToiThieu { get; set; }
    public decimal? ToiDa { get; set; }
    public int ThuTu { get; set; }
    public DateTime? NgayCapNhat { get; set; }
    [StringLength(100)] public string? NguoiCapNhat { get; set; }
}

/// <summary>Ngày nghỉ lễ — không tính vào SLA giờ hành chính.</summary>
public class NgayNghi
{
    [Key] public DateOnly Ngay { get; set; }
    [Required, StringLength(200)] public string Ten { get; set; } = "";
}

/// <summary>Tham số SLA đã đọc sẵn, truyền vào bộ tính hạn.</summary>
public record SlaThamSo(
    decimal GioKhanCap, decimal GioCao, decimal GioTrungBinh, decimal GioThap,
    decimal HeSoKhoaTrongYeu, bool KhoaTrongYeu24h,
    TimeOnly GioBatDau, TimeOnly GioKetThuc, IReadOnlySet<DayOfWeek> NgayLamViec,
    IReadOnlySet<DateOnly> NgayNghi)
{
    public static readonly SlaThamSo MacDinh = new(2, 8, 24, 72, 0.5m, true, new(7, 0), new(17, 0),
        new HashSet<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
        new HashSet<DateOnly>());
}
