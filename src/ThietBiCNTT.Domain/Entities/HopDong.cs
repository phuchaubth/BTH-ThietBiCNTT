using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum LoaiHopDong
{
    [Display(Name = "Mua sắm")] MuaSam = 1,
    [Display(Name = "Bảo trì / bảo dưỡng")] BaoTri,
    [Display(Name = "Thuê ngoài dịch vụ")] ThueNgoai,
    [Display(Name = "Bản quyền phần mềm")] BanQuyen,
    [Display(Name = "Khác")] Khac
}

public class HopDong : BaseEntity
{
    [Required, StringLength(50), Display(Name = "Số hợp đồng")] public string SoHopDong { get; set; } = "";
    [Required, StringLength(300), Display(Name = "Tên / nội dung hợp đồng")] public string Ten { get; set; } = "";
    [Display(Name = "Loại hợp đồng")] public LoaiHopDong Loai { get; set; } = LoaiHopDong.BaoTri;
    [Display(Name = "Nhà cung cấp")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [Display(Name = "Ngày ký"), DataType(DataType.Date)] public DateOnly? NgayKy { get; set; }
    [Display(Name = "Hiệu lực từ"), DataType(DataType.Date)] public DateOnly? HieuLucTu { get; set; }
    [Display(Name = "Hiệu lực đến"), DataType(DataType.Date)] public DateOnly? HieuLucDen { get; set; }
    [Display(Name = "Giá trị (VNĐ)")] public decimal GiaTri { get; set; }
    [StringLength(2000), Display(Name = "Phạm vi / điều khoản chính (SLA của nhà thầu…)")] public string? PhamVi { get; set; }
    [StringLength(100), Display(Name = "Cán bộ phụ trách")] public string? NguoiPhuTrach { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }
    // GĐ3.3 – GLD.05.00 (hợp đồng dịch vụ CNTT)
    [StringLength(1500), Display(Name = "Chỉ tiêu chất lượng, hiệu năng nhà thầu phải báo cáo")] public string? ChiTieuNhaThau { get; set; }
    [StringLength(300), Display(Name = "Tần suất và cách báo cáo")] public string? TanSuatBaoCao { get; set; }
    [StringLength(1000), Display(Name = "Cách xử lý khi nhà thầu không đạt chỉ tiêu")] public string? CachXuLyKhongDat { get; set; }
    [StringLength(1000), Display(Name = "Kế hoạch giữ liên tục dịch vụ khi gia hạn / chấm dứt")] public string? KeHoachLienTuc { get; set; }
    public ICollection<HopDongTaiSan> TaiSans { get; set; } = [];

    public bool LaDichVuCntt => Loai is LoaiHopDong.BaoTri or LoaiHopDong.ThueNgoai;
    /// <summary>GLD.05.00 ME2, ME4, ME5, ME6: mục còn thiếu.</summary>
    public List<string> ThieuGld05()
    {
        var l = new List<string>();
        if (string.IsNullOrWhiteSpace(PhamVi)) l.Add("phạm vi dịch vụ bằng văn bản");
        if (string.IsNullOrWhiteSpace(ChiTieuNhaThau)) l.Add("chỉ tiêu nhà thầu phải báo cáo");
        if (string.IsNullOrWhiteSpace(TanSuatBaoCao)) l.Add("tần suất và cách báo cáo");
        if (string.IsNullOrWhiteSpace(CachXuLyKhongDat)) l.Add("cách xử lý khi không đạt");
        if (string.IsNullOrWhiteSpace(KeHoachLienTuc)) l.Add("kế hoạch giữ liên tục dịch vụ");
        return l;
    }

    public bool ConHieuLuc(DateOnly d) => (HieuLucTu is null || HieuLucTu <= d) && (HieuLucDen is null || HieuLucDen >= d);
    public bool SapHetHan(DateOnly d, int soNgay) => HieuLucDen is { } den && den >= d && den <= d.AddDays(soNgay);
    public bool DaHetHan(DateOnly d) => HieuLucDen is { } den && den < d;
}

public class HopDongTaiSan
{
    public int HopDongId { get; set; }
    public HopDong? HopDong { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
}

public enum LoaiGiayPhep
{
    [Display(Name = "OEM (theo máy)")] OEM = 1,
    [Display(Name = "Vĩnh viễn")] VinhVien,
    [Display(Name = "Thuê bao (có hạn)")] ThueBao,
    [Display(Name = "Volume / số lượng lớn")] Volume,
    [Display(Name = "Miễn phí / mã nguồn mở")] MienPhi
}

/// <summary>Bản quyền phần mềm: số lượng giấy phép, hạn dùng, cấp phát cho máy.</summary>
public class BanQuyenPhanMem : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Phần mềm")] public string Ten { get; set; } = "";
    [StringLength(100), Display(Name = "Hãng")] public string? NhaSanXuat { get; set; }
    [Display(Name = "Loại giấy phép")] public LoaiGiayPhep Loai { get; set; } = LoaiGiayPhep.VinhVien;
    [Range(0, 100000), Display(Name = "Số lượng giấy phép")] public int SoLuong { get; set; } = 1;
    [Display(Name = "Ngày mua"), DataType(DataType.Date)] public DateOnly? NgayMua { get; set; }
    [Display(Name = "Ngày hết hạn"), DataType(DataType.Date)] public DateOnly? NgayHetHan { get; set; }
    /// <summary>Khoá kích hoạt — chỉ hiển thị cho Phòng CNTT.</summary>
    [StringLength(500), Display(Name = "Khoá kích hoạt / tài khoản quản trị")] public string? KhoaKichHoat { get; set; }
    [Display(Name = "Hợp đồng")] public int? HopDongId { get; set; }
    public HopDong? HopDong { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }
    public ICollection<CapPhatBanQuyen> CapPhats { get; set; } = [];

    public int DaDung => CapPhats.Count;
    public int ConLai => SoLuong - DaDung;
    public bool SapHetHan(DateOnly d, int soNgay) => NgayHetHan is { } h && h >= d && h <= d.AddDays(soNgay);
    public bool DaHetHan(DateOnly d) => NgayHetHan is { } h && h < d;

    public CapPhatBanQuyen CapPhat(TaiSan ts, string nguoi, DateTime now)
    {
        if (ts.DaThanhLy) throw new DomainException($"{ts.MaTaiSan} đã thanh lý.");
        if (CapPhats.Any(c => c.TaiSanId == ts.Id)) throw new DomainException($"{ts.MaTaiSan} đã được cấp bản quyền này.");
        if (Loai != LoaiGiayPhep.MienPhi && ConLai <= 0) throw new DomainException($"Hết giấy phép ({DaDung}/{SoLuong}) — cần mua thêm hoặc thu hồi từ máy khác.");
        var c = new CapPhatBanQuyen { TaiSanId = ts.Id, TaiSan = ts, NguoiCap = nguoi, NgayCap = now };
        CapPhats.Add(c);
        return c;
    }
}

public class CapPhatBanQuyen
{
    public int Id { get; set; }
    public int BanQuyenPhanMemId { get; set; }
    public BanQuyenPhanMem? BanQuyenPhanMem { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    public DateTime NgayCap { get; set; }
    [StringLength(100)] public string? NguoiCap { get; set; }
}
