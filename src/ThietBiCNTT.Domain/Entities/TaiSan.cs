using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

public class TaiSan : BaseEntity
{
    [Required, StringLength(30), Display(Name = "Mã tài sản")] public string MaTaiSan { get; set; } = "";
    [Required, StringLength(250), Display(Name = "Tên tài sản")] public string Ten { get; set; } = "";
    [Display(Name = "Loại thiết bị")] public int LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [StringLength(100), Display(Name = "Hãng sản xuất")] public string? Hang { get; set; }
    [StringLength(100)] public string? Model { get; set; }
    [StringLength(100), Display(Name = "Số serial")] public string? SoSerial { get; set; }

    [Display(Name = "Nguồn vốn")] public NguonVon NguonVon { get; set; } = NguonVon.NganSach;
    [Display(Name = "Nguyên giá (VNĐ)"), DisplayFormat(DataFormatString = "{0:N0}")] public decimal NguyenGia { get; set; }
    [Display(Name = "Ngày mua"), DataType(DataType.Date)] public DateOnly? NgayMua { get; set; }
    [Display(Name = "Ngày đưa vào sử dụng"), DataType(DataType.Date)] public DateOnly? NgaySuDung { get; set; }
    [Display(Name = "Nhà cung cấp")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [Display(Name = "Hạn bảo hành"), DataType(DataType.Date)] public DateOnly? HanBaoHanh { get; set; }
    [StringLength(50), Display(Name = "Số hợp đồng / hoá đơn")] public string? SoHopDong { get; set; }

    [Display(Name = "Khoa/phòng sử dụng")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Vị trí")] public int? ViTriId { get; set; }
    public ViTri? ViTri { get; set; }
    [StringLength(100), Display(Name = "Người sử dụng")] public string? NguoiSuDung { get; set; }

    /// <summary>Thiết bị cha (vd: tủ rack chứa switch, máy chủ chứa ổ cứng).</summary>
    [Display(Name = "Thuộc thiết bị")] public int? TaiSanChaId { get; set; }
    public TaiSan? TaiSanCha { get; set; }
    public ICollection<TaiSan> TaiSanCon { get; set; } = [];

    [Display(Name = "Trạng thái")] public TrangThaiTaiSan TrangThai { get; set; } = TrangThaiTaiSan.DangSuDung;
    [Display(Name = "Bộ đếm (số trang in...)")] public long? ChiSoBoDem { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }

    public CauHinh? CauHinh { get; set; }
    public ICollection<LichSuTaiSan> LichSu { get; set; } = [];
    public ICollection<PhieuSuaChua> PhieuSuaChuas { get; set; } = [];

    public bool ConBaoHanh(DateOnly homNay) => HanBaoHanh.HasValue && HanBaoHanh.Value >= homNay;

    public int? TuoiNam(DateOnly homNay)
    {
        var moc = NgaySuDung ?? NgayMua;
        return moc is null ? null : (int)((homNay.DayNumber - moc.Value.DayNumber) / 365.25);
    }

    public bool DaThanhLy => TrangThai == TrangThaiTaiSan.DaThanhLy;
}

/// <summary>Cấu hình hiện tại của thiết bị (quan hệ 1-1 với TaiSan).</summary>
public class CauHinh
{
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(150)] public string? CPU { get; set; }
    [StringLength(50)] public string? RAM { get; set; }
    [StringLength(150), Display(Name = "Ổ cứng")] public string? OCung { get; set; }
    [StringLength(100), Display(Name = "Hệ điều hành")] public string? HeDieuHanh { get; set; }
    [StringLength(100), Display(Name = "Bản quyền HĐH / Office")] public string? BanQuyen { get; set; }
    [StringLength(45), Display(Name = "Địa chỉ IP")] public string? DiaChiIP { get; set; }
    [StringLength(17), Display(Name = "Địa chỉ MAC")] public string? DiaChiMAC { get; set; }
    [StringLength(63)] public string? Hostname { get; set; }
    [StringLength(500), Display(Name = "Phần mềm cài đặt")] public string? PhanMem { get; set; }
    public DateTime? NgayCapNhat { get; set; }

    public static readonly string[] CacTruong =
        [nameof(CPU), nameof(RAM), nameof(OCung), nameof(HeDieuHanh), nameof(BanQuyen),
         nameof(DiaChiIP), nameof(DiaChiMAC), nameof(Hostname), nameof(PhanMem)];

    /// <summary>So sánh với cấu hình mới, trả về danh sách (trường, cũ, mới) đã thay đổi.</summary>
    public IEnumerable<(string Truong, string? Cu, string? Moi)> SoSanh(CauHinh moi)
    {
        foreach (var t in CacTruong)
        {
            var p = typeof(CauHinh).GetProperty(t)!;
            var cu = (string?)p.GetValue(this);
            var m = (string?)p.GetValue(moi);
            if (!string.Equals(cu?.Trim(), m?.Trim(), StringComparison.Ordinal))
                yield return (t, cu, m);
        }
    }
}

/// <summary>Dòng thời gian: mọi sự kiện xảy ra với tài sản.</summary>
public class LichSuTaiSan
{
    public long Id { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    public DateTime ThoiGian { get; set; }
    public LoaiSuKien LoaiSuKien { get; set; }
    [StringLength(1000)] public string NoiDung { get; set; } = "";
    [StringLength(100)] public string? NguoiThucHien { get; set; }
}
