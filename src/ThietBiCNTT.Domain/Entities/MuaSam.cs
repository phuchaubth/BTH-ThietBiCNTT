using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum LoaiHangMua
{
    [Display(Name = "Vật tư / linh kiện")] VatTu = 1,
    [Display(Name = "Thiết bị (tài sản mới)")] ThietBi
}

public enum CanCuDeXuat
{
    [Display(Name = "Tồn kho dưới định mức")] TonThap = 1,
    [Display(Name = "Phục vụ sửa chữa")] SuaChua,
    [Display(Name = "Thay thế thiết bị cũ/hỏng")] ThayThe,
    [Display(Name = "Trang bị mới")] TrangBiMoi,
    [Display(Name = "Khác")] Khac
}

public enum TrangThaiDeXuat
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Từ chối")] TuChoi,
    [Display(Name = "Đã lập đơn một phần")] DatMotPhan,
    [Display(Name = "Đã lập đơn đủ")] DaDatDu,
    [Display(Name = "Đã huỷ")] DaHuy
}

/// <summary>
/// Đề xuất mua sắm (PR). Duyệt theo cấp: 1 Trưởng phòng CNTT → 2 Phòng Vật tư - TTB (nếu cấu hình người duyệt)
/// → 3 Lãnh đạo (khi tổng dự toán ≥ ngưỡng). Danh sách người duyệt cấp 1, 2 lấy từ tham số.
/// </summary>
public class DeXuatMuaSam : BaseEntity
{
    public const int CapCNTT = 1, CapVatTu = 2, CapLanhDao = 3;

    [Required, StringLength(20), Display(Name = "Số đề xuất")] public string SoPhieu { get; set; } = "";
    [Required, StringLength(300), Display(Name = "Nội dung đề xuất")] public string TieuDe { get; set; } = "";
    [Display(Name = "Căn cứ")] public CanCuDeXuat CanCu { get; set; } = CanCuDeXuat.TonThap;
    [StringLength(1000), Display(Name = "Lý do / thuyết minh")] public string? LyDo { get; set; }
    [Display(Name = "Phiếu sửa chữa liên quan")] public int? PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    [Display(Name = "Khoản mục ngân sách")] public int? KhoanMucNganSachId { get; set; }
    public bool TuDong { get; set; }
    public TrangThaiDeXuat TrangThai { get; set; } = TrangThaiDeXuat.Nhap;
    public int CapHienTai { get; set; }
    public int CapCuoi { get; set; }
    public int LuotGui { get; set; } = 1;
    public List<DongDeXuat> Dongs { get; set; } = [];
    public List<BuocDuyetMuaSam> BuocDuyets { get; set; } = [];

    public decimal TongDuToan => Dongs.Sum(d => d.ThanhTien);
    public bool ChoLapDon => TrangThai is TrangThaiDeXuat.DaDuyet or TrangThaiDeXuat.DatMotPhan;

    public static string TenCap(int cap) => cap switch
    { CapCNTT => "Trưởng phòng CNTT", CapVatTu => "Phòng Vật tư - TTB", CapLanhDao => "Lãnh đạo bệnh viện", _ => "" };

    /// <summary>Các cấp phải duyệt: cấp 2 chỉ khi có người duyệt; cấp 3 khi tổng ≥ ngưỡng.</summary>
    public IEnumerable<int> CacCap() => Enumerable.Range(1, CapCuoi).Where(c => c != CapVatTu || CoCapVatTu);
    public bool CoCapVatTu { get; set; }

    public void KiemTraDong()
    {
        if (Dongs.Count == 0) throw new DomainException("Đề xuất chưa có dòng hàng nào.");
        foreach (var d in Dongs)
        {
            if (d.SoLuong <= 0) throw new DomainException("Số lượng phải lớn hơn 0.");
            if (d.DonGiaDuKien < 0) throw new DomainException("Đơn giá không được âm.");
            if (d.Loai == LoaiHangMua.VatTu && d.VatTuId is null) throw new DomainException("Dòng vật tư phải chọn vật tư trong danh mục.");
            if (d.Loai == LoaiHangMua.ThietBi && (d.LoaiThietBiId is null || string.IsNullOrWhiteSpace(d.MoTa))) throw new DomainException("Dòng thiết bị phải chọn loại thiết bị và ghi mô tả/cấu hình.");
        }
    }

    public void GuiDuyet(decimal nguongLanhDao, bool coCapVatTu)
    {
        if (TrangThai is not (TrangThaiDeXuat.Nhap or TrangThaiDeXuat.TuChoi)) throw new DomainException("Chỉ gửi duyệt đề xuất nháp hoặc bị từ chối.");
        KiemTraDong();
        CoCapVatTu = coCapVatTu;
        CapCuoi = TongDuToan >= nguongLanhDao ? CapLanhDao : coCapVatTu ? CapVatTu : CapCNTT;
        CapHienTai = CacCap().First();
        TrangThai = TrangThaiDeXuat.ChoDuyet;
    }

    public void Duyet(string nguoi, string ten, bool dongY, string? yKien, DateTime now)
    {
        if (TrangThai != TrangThaiDeXuat.ChoDuyet) throw new DomainException("Đề xuất không ở trạng thái chờ duyệt.");
        if (string.Equals(nguoi, NguoiTao, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người lập đề xuất không được tự duyệt.");
        if (BuocDuyets.Any(b => b.DongY && b.LuotGui == LuotGui && string.Equals(b.NguoiDuyet, nguoi, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException("Bạn đã duyệt một cấp của đề xuất này.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Phải ghi lý do khi từ chối.");
        BuocDuyets.Add(new BuocDuyetMuaSam { Cap = CapHienTai, LuotGui = LuotGui, NguoiDuyet = nguoi, TenNguoiDuyet = ten, DongY = dongY, YKien = yKien, ThoiGian = now });
        if (!dongY) { TrangThai = TrangThaiDeXuat.TuChoi; CapHienTai = 0; LuotGui++; return; }
        var tiep = CacCap().Where(c => c > CapHienTai).DefaultIfEmpty(0).First();
        if (tiep == 0) { TrangThai = TrangThaiDeXuat.DaDuyet; CapHienTai = 0; } else CapHienTai = tiep;
    }

    /// <summary>Cập nhật trạng thái theo số lượng đã đưa vào đơn hàng.</summary>
    public void CapNhatTheoDonHang()
    {
        if (!ChoLapDon) return;
        TrangThai = Dongs.All(d => d.SoLuongDaDat >= d.SoLuong) ? TrangThaiDeXuat.DaDatDu
            : Dongs.Any(d => d.SoLuongDaDat > 0) ? TrangThaiDeXuat.DatMotPhan : TrangThaiDeXuat.DaDuyet;
    }
}

public class DongDeXuat
{
    public int Id { get; set; }
    public int DeXuatMuaSamId { get; set; }
    public DeXuatMuaSam? DeXuatMuaSam { get; set; }
    public LoaiHangMua Loai { get; set; } = LoaiHangMua.VatTu;
    public int? VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [StringLength(500)] public string? MoTa { get; set; }
    [StringLength(20)] public string? DonViTinh { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGiaDuKien { get; set; }
    public decimal SoLuongDaDat { get; set; }
    [StringLength(300)] public string? GhiChu { get; set; }
    public decimal ThanhTien => SoLuong * DonGiaDuKien;
    public decimal ConLai => Math.Max(0, SoLuong - SoLuongDaDat);
    public string TenHang => Loai == LoaiHangMua.VatTu ? $"{VatTu?.Ma} – {VatTu?.Ten}" : $"{LoaiThietBi?.Ten}: {MoTa}";
}

public class BuocDuyetMuaSam
{
    public int Id { get; set; }
    public int DeXuatMuaSamId { get; set; }
    public int Cap { get; set; }
    public int LuotGui { get; set; }
    [StringLength(100)] public string NguoiDuyet { get; set; } = "";
    [StringLength(200)] public string? TenNguoiDuyet { get; set; }
    public bool DongY { get; set; }
    [StringLength(1000)] public string? YKien { get; set; }
    public DateTime ThoiGian { get; set; }
}

public enum TrangThaiDonHang
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Đã đặt hàng")] DaDatHang,
    [Display(Name = "Đã nhận một phần")] NhanMotPhan,
    [Display(Name = "Đã nhận đủ")] DaNhanDu,
    [Display(Name = "Đã huỷ")] DaHuy
}

/// <summary>Đơn mua hàng (PO) lập từ các dòng đề xuất đã duyệt.</summary>
public class DonMuaHang : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số đơn hàng")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Nhà cung cấp")] public int NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [Display(Name = "Hợp đồng / căn cứ")] public int? HopDongId { get; set; }
    public HopDong? HopDong { get; set; }
    [StringLength(200), Display(Name = "Căn cứ khác (quyết định, báo giá…)")] public string? CanCu { get; set; }
    [Display(Name = "Hạn giao hàng"), DataType(DataType.Date)] public DateOnly? HanGiao { get; set; }
    [Range(0, 100), Display(Name = "Thuế GTGT (%)")] public decimal ThueSuat { get; set; } = 10;
    [Range(0, 120), Display(Name = "Bảo hành (tháng)")] public int BaoHanhThang { get; set; } = 12;
    [StringLength(1000), Display(Name = "Điều khoản / ghi chú")] public string? GhiChu { get; set; }
    public TrangThaiDonHang TrangThai { get; set; } = TrangThaiDonHang.Nhap;
    public DateTime? NgayDatHang { get; set; }
    [Display(Name = "Khoản mục ngân sách")] public int? KhoanMucNganSachId { get; set; }
    public List<DongDonHang> Dongs { get; set; } = [];
    public List<PhieuNhanHang> PhieuNhans { get; set; } = [];

    public decimal TienHang => Dongs.Sum(d => d.ThanhTien);
    public decimal TienThue => Math.Round(TienHang * ThueSuat / 100, 0);
    public decimal TongTien => TienHang + TienThue;
    public bool TreHan(DateOnly homNay) => TrangThai is TrangThaiDonHang.DaDatHang or TrangThaiDonHang.NhanMotPhan && HanGiao is { } h && h < homNay;

    public void DatHang(DateTime now)
    {
        if (TrangThai != TrangThaiDonHang.Nhap) throw new DomainException("Chỉ đặt hàng đơn nháp.");
        if (Dongs.Count == 0 || Dongs.Any(d => d.SoLuong <= 0 || d.DonGia < 0)) throw new DomainException("Đơn hàng có dòng không hợp lệ.");
        TrangThai = TrangThaiDonHang.DaDatHang; NgayDatHang = now;
    }

    public void CapNhatTheoNhanHang()
    {
        if (TrangThai is TrangThaiDonHang.Nhap or TrangThaiDonHang.DaHuy) return;
        TrangThai = Dongs.All(d => d.SoLuongDaNhan >= d.SoLuong) ? TrangThaiDonHang.DaNhanDu
            : Dongs.Any(d => d.SoLuongDaNhan > 0) ? TrangThaiDonHang.NhanMotPhan : TrangThaiDonHang.DaDatHang;
    }
}

public class DongDonHang
{
    public int Id { get; set; }
    public int DonMuaHangId { get; set; }
    public DonMuaHang? DonMuaHang { get; set; }
    public int? DongDeXuatId { get; set; }
    public DongDeXuat? DongDeXuat { get; set; }
    public LoaiHangMua Loai { get; set; }
    public int? VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [StringLength(500)] public string? MoTa { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal SoLuongDaNhan { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
    public decimal ConLai => Math.Max(0, SoLuong - SoLuongDaNhan);
    public string TenHang => Loai == LoaiHangMua.VatTu ? $"{VatTu?.Ma} – {VatTu?.Ten}" : $"{LoaiThietBi?.Ten}: {MoTa}";
}

/// <summary>Một lần nhận hàng theo đơn: vật tư → phiếu nhập kho; thiết bị → hồ sơ tài sản mới.</summary>
public class PhieuNhanHang
{
    public int Id { get; set; }
    public int DonMuaHangId { get; set; }
    public DonMuaHang? DonMuaHang { get; set; }
    public DateTime NgayNhan { get; set; }
    [StringLength(100)] public string NguoiNhan { get; set; } = "";
    [StringLength(50)] public string? SoHoaDon { get; set; }
    [StringLength(1000)] public string? BienBanNghiemThu { get; set; }
    public int? KhoId { get; set; }
    public int? PhieuKhoId { get; set; }
    public PhieuKho? PhieuKho { get; set; }
    /// <summary>Giá trị nhận (gồm thuế) — ghi nhận thực chi ngân sách.</summary>
    public decimal GiaTri { get; set; }
    [StringLength(2000)] public string? TaiSanDaTao { get; set; }
}
