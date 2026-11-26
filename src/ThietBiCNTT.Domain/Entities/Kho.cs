using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum LoaiKho
{
    [Display(Name = "Kho chính Phòng CNTT")] Chinh = 1,
    [Display(Name = "Kho phụ / tại toà nhà")] Phu,
    [Display(Name = "Kho thiết bị chờ thanh lý")] ChoThanhLy
}

public class Kho : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã kho")] public string Ma { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Tên kho")] public string Ten { get; set; } = "";
    [Display(Name = "Loại kho")] public LoaiKho Loai { get; set; } = LoaiKho.Chinh;
    [StringLength(100), Display(Name = "Thủ kho (tên đăng nhập)")] public string? ThuKho { get; set; }
    [StringLength(300), Display(Name = "Vị trí")] public string? ViTri { get; set; }
    [Display(Name = "Đang hoạt động")] public bool DangHoatDong { get; set; } = true;
}

/// <summary>Tồn kho theo (kho, vật tư).</summary>
public class TonKhoVatTu
{
    public int Id { get; set; }
    public int KhoId { get; set; }
    public Kho? Kho { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public decimal SoLuong { get; set; }
}

public enum LoaiPhieuKho
{
    [Display(Name = "Nhập kho")] Nhap = 1,
    [Display(Name = "Xuất kho")] Xuat,
    [Display(Name = "Chuyển kho")] Chuyen,
    [Display(Name = "Điều chỉnh (kiểm kê kho)")] DieuChinh
}

public enum LyDoPhieuKho
{
    [Display(Name = "Nhập mua mới")] NhapMua = 1,
    [Display(Name = "Nhập hàng hãng trả / bảo hành")] NhapHangTra,
    [Display(Name = "Nhập thu hồi (tháo máy, còn dùng được)")] NhapThuHoi,
    [Display(Name = "Nhập khác")] NhapKhac,
    [Display(Name = "Xuất cho phiếu sửa chữa / bảo trì")] XuatSuaChua = 11,
    [Display(Name = "Xuất cấp cho khoa/phòng")] XuatCapKhoa,
    [Display(Name = "Xuất huỷ (hỏng, hết hạn)")] XuatHuy,
    [Display(Name = "Xuất khác")] XuatKhac,
    [Display(Name = "Chuyển kho")] ChuyenKho = 21,
    [Display(Name = "Điều chỉnh theo kiểm kê")] KiemKe = 31
}

public enum TrangThaiPhieuKho
{
    [Display(Name = "Chờ duyệt")] ChoDuyet = 1,
    [Display(Name = "Đã ghi sổ")] DaGhiSo,
    [Display(Name = "Đã huỷ")] DaHuy
}

/// <summary>Phiếu kho: nhập / xuất / chuyển / điều chỉnh. Ghi sổ → cập nhật tồn theo kho và thẻ kho.</summary>
public class PhieuKho : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số phiếu")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Loại phiếu")] public LoaiPhieuKho Loai { get; set; }
    [Display(Name = "Lý do")] public LyDoPhieuKho LyDo { get; set; }
    [Display(Name = "Kho")] public int KhoId { get; set; }
    public Kho? Kho { get; set; }
    [Display(Name = "Kho nhận")] public int? KhoDenId { get; set; }
    public Kho? KhoDen { get; set; }
    [Display(Name = "Khoa/phòng nhận")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Phiếu sửa chữa")] public int? PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    /// <summary>Linh kiện thu hồi từ tài sản trong đề xuất thanh lý (FR-TL-10).</summary>
    [Display(Name = "Đề xuất thanh lý")] public int? DeXuatThanhLyId { get; set; }
    [Display(Name = "Nhà cung cấp")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [StringLength(50), Display(Name = "Số hoá đơn / chứng từ")] public string? SoChungTu { get; set; }
    [StringLength(100), Display(Name = "Người giao / nhận")] public string? NguoiGiaoNhan { get; set; }
    [StringLength(1000), Display(Name = "Diễn giải")] public string? DienGiai { get; set; }
    public TrangThaiPhieuKho TrangThai { get; set; } = TrangThaiPhieuKho.ChoDuyet;
    public DateTime? NgayGhiSo { get; set; }
    [StringLength(100)] public string? NguoiDuyet { get; set; }
    public List<ChiTietPhieuKho> ChiTiets { get; set; } = [];

    public decimal TongTien => ChiTiets.Sum(c => c.ThanhTien);

    public static LoaiPhieuKho LoaiCua(LyDoPhieuKho l) => (int)l switch
    {
        < 11 => LoaiPhieuKho.Nhap, < 21 => LoaiPhieuKho.Xuat, < 31 => LoaiPhieuKho.Chuyen, _ => LoaiPhieuKho.DieuChinh
    };

    public void KiemTraHopLe()
    {
        if (LoaiCua(LyDo) != Loai) throw new DomainException("Lý do không phù hợp loại phiếu.");
        if (ChiTiets.Count == 0) throw new DomainException("Phiếu chưa có dòng vật tư nào.");
        if (Loai != LoaiPhieuKho.DieuChinh && ChiTiets.Any(c => c.SoLuong <= 0)) throw new DomainException("Số lượng phải lớn hơn 0.");
        if (Loai == LoaiPhieuKho.DieuChinh && ChiTiets.Any(c => c.SoLuong == 0)) throw new DomainException("Dòng điều chỉnh không có chênh lệch.");
        if (ChiTiets.Any(c => c.DonGia < 0)) throw new DomainException("Đơn giá không được âm.");
        if (ChiTiets.GroupBy(c => c.VatTuId).Any(g => g.Count() > 1)) throw new DomainException("Mỗi vật tư chỉ một dòng trên phiếu.");
        if (Loai == LoaiPhieuKho.Chuyen && (KhoDenId is null || KhoDenId == KhoId)) throw new DomainException("Chọn kho nhận khác kho xuất.");
        if (LyDo == LyDoPhieuKho.XuatCapKhoa && KhoaPhongId is null) throw new DomainException("Chọn khoa/phòng nhận.");
    }
}

public class ChiTietPhieuKho
{
    public long Id { get; set; }
    public int PhieuKhoId { get; set; }
    public PhieuKho? PhieuKho { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    /// <summary>Nhập/xuất/chuyển: dương. Điều chỉnh: dương = tăng, âm = giảm.</summary>
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    /// <summary>Kiểm kê kho: tồn sổ sách tại thời điểm lập phiếu (để in biên bản).</summary>
    public decimal? TonSoSach { get; set; }
    [StringLength(200)] public string? GhiChu { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

/// <summary>Thẻ kho: mỗi biến động tồn của một vật tư tại một kho.</summary>
public class TheKho
{
    public long Id { get; set; }
    public int KhoId { get; set; }
    public Kho? Kho { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public int PhieuKhoId { get; set; }
    public PhieuKho? PhieuKho { get; set; }
    public DateTime ThoiGian { get; set; }
    public decimal Nhap { get; set; }
    public decimal Xuat { get; set; }
    public decimal TonSau { get; set; }
    public decimal DonGia { get; set; }
    [StringLength(300)] public string? DienGiai { get; set; }
}
