using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>Sơ đồ mặt bằng (FR-VT-05): ảnh mặt bằng tầng/khoa, trên đó đặt biểu tượng tài sản.</summary>
public class SoDoMatBang : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên sơ đồ")] public string Ten { get; set; } = "";
    [StringLength(500), Display(Name = "Mô tả")] public string? MoTa { get; set; }
    /// <summary>Khoa quản lý: người của khoa được xem; trống = sơ đồ chung (mọi người xem).</summary>
    [Display(Name = "Khoa/phòng")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Vị trí (tầng/toà)")] public int? ViTriId { get; set; }
    public ViTri? ViTri { get; set; }
    public long? TepAnhId { get; set; }
    public int ThuTu { get; set; }
    public List<DiemSoDo> Diems { get; set; } = [];
}

/// <summary>Vị trí một tài sản trên sơ đồ, toạ độ theo % chiều rộng/cao ảnh (không phụ thuộc kích thước hiển thị).</summary>
public class DiemSoDo
{
    public int Id { get; set; }
    public int SoDoMatBangId { get; set; }
    public SoDoMatBang? SoDoMatBang { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    public decimal X { get; set; }
    public decimal Y { get; set; }

    public static (decimal X, decimal Y) KiemTraToaDo(decimal x, decimal y)
    {
        if (x < 0 || x > 100 || y < 0 || y > 100) throw new DomainException("Toạ độ phải nằm trong ảnh sơ đồ (0–100%).");
        return (Math.Round(x, 3), Math.Round(y, 3));
    }

    public static string Icon(NhomThietBi? n) => n switch
    {
        NhomThietBi.MayTinhDeBan => "bi-pc-display", NhomThietBi.MayTinhXachTay => "bi-laptop", NhomThietBi.MayChu => "bi-hdd-rack",
        NhomThietBi.MayIn => "bi-printer", NhomThietBi.ThietBiMang => "bi-router", NhomThietBi.ManHinh => "bi-display", NhomThietBi.Ups => "bi-battery-charging",
        NhomThietBi.Camera => "bi-camera-video", NhomThietBi.NgoaiVi => "bi-keyboard", _ => "bi-cpu"
    };
}
