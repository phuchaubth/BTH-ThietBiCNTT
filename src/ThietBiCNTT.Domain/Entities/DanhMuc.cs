using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

public class KhoaPhong : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã khoa/phòng")] public string Ma { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Tên khoa/phòng")] public string Ten { get; set; } = "";
    [StringLength(100), Display(Name = "Trưởng khoa/phòng")] public string? TruongKhoa { get; set; }
    [StringLength(20), Display(Name = "Điện thoại")] public string? DienThoai { get; set; }
    /// <summary>Khoa trọng yếu (Cấp cứu, ICU, Phòng mổ...) được áp SLA ngắn hơn.</summary>
    [Display(Name = "Khoa trọng yếu (SLA ưu tiên)")] public bool LaKhoaTrongYeu { get; set; }
    [Display(Name = "Đang hoạt động")] public bool DangHoatDong { get; set; } = true;
}

/// <summary>Cây vị trí: Toà nhà → Tầng → Phòng.</summary>
public class ViTri : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên vị trí")] public string Ten { get; set; } = "";
    [Display(Name = "Cấp")] public LoaiViTri Loai { get; set; }
    [Display(Name = "Thuộc vị trí")] public int? ChaId { get; set; }
    public ViTri? Cha { get; set; }
    public ICollection<ViTri> Con { get; set; } = [];
    [Display(Name = "Khoa/phòng quản lý")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
}

public class LoaiThietBi : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã loại")] public string Ma { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Tên loại")] public string Ten { get; set; } = "";
    [Display(Name = "Nhóm")] public NhomThietBi Nhom { get; set; }
    /// <summary>Loại có ổ lưu trữ → bắt buộc xoá dữ liệu trước khi thanh lý.</summary>
    [Display(Name = "Có lưu trữ dữ liệu")] public bool CoLuuTruDuLieu { get; set; }
    [Range(1, 50), Display(Name = "Thời gian sử dụng khuyến nghị (năm)")] public int TuoiThoNam { get; set; } = 5;
    /// <summary>Chu kỳ bảo trì khuyến cáo của nhà sản xuất (FR-PM-15) — mặc định khi lập kế hoạch.</summary>
    [Range(1, 3650), Display(Name = "Chu kỳ bảo trì khuyến cáo (ngày)")] public int? ChuKyBaoTriNgay { get; set; }
}

public class NhaCungCap : BaseEntity
{
    [Required, StringLength(250), Display(Name = "Tên nhà cung cấp")] public string Ten { get; set; } = "";
    [StringLength(20), Display(Name = "Mã số thuế")] public string? MaSoThue { get; set; }
    [StringLength(500), Display(Name = "Địa chỉ")] public string? DiaChi { get; set; }
    [StringLength(20), Display(Name = "Điện thoại")] public string? DienThoai { get; set; }
    [EmailAddress, StringLength(100)] public string? Email { get; set; }
    [StringLength(100), Display(Name = "Người liên hệ")] public string? NguoiLienHe { get; set; }
    [Display(Name = "Là hãng/đơn vị bảo hành")] public bool LaDonViBaoHanh { get; set; }
}

public class VatTu : BaseEntity
{
    [Required, StringLength(30), Display(Name = "Mã vật tư")] public string Ma { get; set; } = "";
    [Required, StringLength(250), Display(Name = "Tên vật tư / linh kiện")] public string Ten { get; set; } = "";
    [Required, StringLength(20), Display(Name = "Đơn vị tính")] public string DonViTinh { get; set; } = "Cái";
    [StringLength(100), Display(Name = "Nhóm")] public string? Nhom { get; set; }
    [StringLength(300), Display(Name = "Quy cách / model tương thích")] public string? QuyCach { get; set; }
    /// <summary>Tổng tồn mọi kho (được cập nhật khi ghi sổ phiếu kho — không sửa tay).</summary>
    [ScaffoldColumn(false), Display(Name = "Tồn kho")] public decimal TonKho { get; set; }
    [Display(Name = "Tồn tối thiểu")] public decimal TonToiThieu { get; set; }
    [Display(Name = "Tồn tối đa (gợi ý số lượng mua)")] public decimal TonToiDa { get; set; }
    /// <summary>Giá bình quân gia quyền — cập nhật khi nhập kho.</summary>
    [Display(Name = "Đơn giá (bình quân)")] public decimal DonGia { get; set; }

    /// <summary>Nhập kho: cập nhật giá bình quân gia quyền theo tổng tồn.</summary>
    public void CapNhatGiaBinhQuan(decimal soLuongNhap, decimal donGiaNhap)
    {
        if (soLuongNhap <= 0 || donGiaNhap <= 0) return;
        var tonTruoc = Math.Max(0, TonKho);
        DonGia = Math.Round((tonTruoc * DonGia + soLuongNhap * donGiaNhap) / (tonTruoc + soLuongNhap), 0);
    }

    public bool DuoiTonToiThieu => TonKho <= TonToiThieu;
}

/// <summary>Nhật ký thao tác hệ thống (audit log).</summary>
public class NhatKyHeThong
{
    public long Id { get; set; }
    public DateTime ThoiGian { get; set; }
    [StringLength(100)] public string? NguoiDung { get; set; }
    [StringLength(20)] public string HanhDong { get; set; } = "";
    [StringLength(100)] public string DoiTuong { get; set; } = "";
    [StringLength(50)] public string? DoiTuongId { get; set; }
    public string? ChiTiet { get; set; }
    [StringLength(45)] public string? DiaChiIp { get; set; }
}
