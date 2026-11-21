using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>
/// Yêu cầu sửa chữa + Lệnh công việc (Work Request / Work Order).
/// Luồng: Mới → Đã tiếp nhận → Đang xử lý ⇄ (Chờ linh kiện | Gửi hãng) → Hoàn thành → Đã nghiệm thu.
/// </summary>
public class PhieuSuaChua : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số phiếu")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Loại công việc")] public LoaiCongViec LoaiCongViec { get; set; } = LoaiCongViec.SuaChua;
    [Display(Name = "Tài sản")] public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Display(Name = "Khoa/phòng yêu cầu")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(100), Display(Name = "Người báo")] public string? NguoiYeuCau { get; set; }
    [StringLength(20), Display(Name = "SĐT liên hệ")] public string? DienThoaiLienHe { get; set; }
    [Required, StringLength(2000), Display(Name = "Mô tả sự cố")] public string MoTaSuCo { get; set; } = "";
    [Display(Name = "Mức độ ưu tiên")] public MucDoUuTien UuTien { get; set; } = MucDoUuTien.TrungBinh;
    [Display(Name = "Trạng thái")] public TrangThaiPhieuSuaChua TrangThai { get; set; } = TrangThaiPhieuSuaChua.Moi;

    [Display(Name = "Thời điểm báo")] public DateTime NgayYeuCau { get; set; }
    [Display(Name = "Hạn xử lý (SLA)")] public DateTime HanXuLy { get; set; }
    [Display(Name = "Thời điểm tiếp nhận")] public DateTime? NgayTiepNhan { get; set; }
    [Display(Name = "Bắt đầu xử lý")] public DateTime? NgayBatDau { get; set; }
    [Display(Name = "Thời điểm hoàn thành")] public DateTime? NgayHoanThanh { get; set; }
    [Display(Name = "Thời điểm nghiệm thu")] public DateTime? NgayNghiemThu { get; set; }

    [StringLength(100), Display(Name = "Kỹ thuật viên")] public string? KyThuatVien { get; set; }
    [Display(Name = "Hình thức xử lý")] public HinhThucXuLy? HinhThucXuLy { get; set; }
    [Display(Name = "Đơn vị bảo hành / thuê ngoài")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [StringLength(1000), Display(Name = "Nguyên nhân")] public string? NguyenNhan { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp khắc phục")] public string? BienPhapKhacPhuc { get; set; }
    [Display(Name = "Giờ công")] public decimal GioCong { get; set; }
    [Display(Name = "Chi phí thuê ngoài (VNĐ)")] public decimal ChiPhiThueNgoai { get; set; }
    [StringLength(100), Display(Name = "Người nghiệm thu")] public string? NguoiNghiemThu { get; set; }
    [Range(1, 5), Display(Name = "Đánh giá (1-5)")] public int? DanhGia { get; set; }
    [StringLength(1000), Display(Name = "Ý kiến người dùng")] public string? YKienNguoiDung { get; set; }

    [Display(Name = "Kế hoạch bảo trì")] public int? KeHoachBaoTriId { get; set; }
    public KeHoachBaoTri? KeHoachBaoTri { get; set; }
    public ICollection<ChiTietVatTuSuaChua> VatTus { get; set; } = [];
    public ICollection<BinhLuanPhieu> BinhLuans { get; set; } = [];

    public decimal ChiPhiVatTu => VatTus.Sum(v => v.ThanhTien);
    public decimal TongChiPhi => ChiPhiVatTu + ChiPhiThueNgoai;
    public bool DaDong => TrangThai is TrangThaiPhieuSuaChua.DaNghiemThu or TrangThaiPhieuSuaChua.DaHuy;
    public bool QuaHan(DateTime now) => !DaDong && TrangThai != TrangThaiPhieuSuaChua.HoanThanh && now > HanXuLy;

    /// <summary>Thời gian ngừng máy (giờ) = từ lúc báo đến lúc hoàn thành.</summary>
    public double? ThoiGianDungMayGio => NgayHoanThanh is null ? null : (NgayHoanThanh.Value - NgayYeuCau).TotalHours;

    /// <summary>Tính hạn SLA theo mức ưu tiên và khoa trọng yếu.</summary>
    public static TimeSpan TinhSla(MucDoUuTien uuTien, bool khoaTrongYeu)
    {
        var gio = uuTien switch
        {
            MucDoUuTien.KhanCap => 2,
            MucDoUuTien.Cao => 8,
            MucDoUuTien.TrungBinh => 24,
            _ => 72
        };
        return TimeSpan.FromHours(khoaTrongYeu ? Math.Max(1, gio / 2) : gio);
    }

    private void YeuCau(params TrangThaiPhieuSuaChua[] hopLe)
    {
        if (!hopLe.Contains(TrangThai))
            throw new DomainException($"Không thể thực hiện thao tác khi phiếu đang ở trạng thái '{TrangThai}'.");
    }

    public void TiepNhan(string kyThuatVien, HinhThucXuLy hinhThuc, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.Moi, TrangThaiPhieuSuaChua.DaTiepNhan);
        if (string.IsNullOrWhiteSpace(kyThuatVien)) throw new DomainException("Phải chọn kỹ thuật viên.");
        KyThuatVien = kyThuatVien;
        HinhThucXuLy = hinhThuc;
        NgayTiepNhan ??= now;
        TrangThai = TrangThaiPhieuSuaChua.DaTiepNhan;
    }

    public void BatDauXuLy(DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang);
        NgayBatDau ??= now;
        TrangThai = TrangThaiPhieuSuaChua.DangXuLy;
    }

    public void TamDung(TrangThaiPhieuSuaChua lyDo)
    {
        if (lyDo is not (TrangThaiPhieuSuaChua.ChoLinhKien or TrangThaiPhieuSuaChua.GuiHang))
            throw new DomainException("Lý do tạm dừng không hợp lệ.");
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy);
        TrangThai = lyDo;
    }

    public void HoanThanh(string? nguyenNhan, string? bienPhap, decimal gioCong, decimal chiPhiThueNgoai, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.GuiHang);
        if (string.IsNullOrWhiteSpace(bienPhap)) throw new DomainException("Phải nhập biện pháp khắc phục.");
        NguyenNhan = nguyenNhan;
        BienPhapKhacPhuc = bienPhap;
        GioCong = gioCong;
        ChiPhiThueNgoai = chiPhiThueNgoai;
        NgayBatDau ??= now;
        NgayHoanThanh = now;
        TrangThai = TrangThaiPhieuSuaChua.HoanThanh;
    }

    public void NghiemThu(string nguoiNghiemThu, int? danhGia, string? yKien, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.HoanThanh);
        NguoiNghiemThu = nguoiNghiemThu;
        DanhGia = danhGia;
        YKienNguoiDung = yKien;
        NgayNghiemThu = now;
        TrangThai = TrangThaiPhieuSuaChua.DaNghiemThu;
    }

    /// <summary>Người dùng không đồng ý nghiệm thu → trả lại cho KTV.</summary>
    public void TraLai(string yKien)
    {
        YeuCau(TrangThaiPhieuSuaChua.HoanThanh);
        YKienNguoiDung = yKien;
        NgayHoanThanh = null;
        TrangThai = TrangThaiPhieuSuaChua.DangXuLy;
    }

    public void Huy(string lyDo)
    {
        if (DaDong || TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Phiếu đã hoàn thành/đóng, không thể huỷ.");
        YKienNguoiDung = lyDo;
        TrangThai = TrangThaiPhieuSuaChua.DaHuy;
    }
}

public class ChiTietVatTuSuaChua
{
    public int Id { get; set; }
    public int PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

/// <summary>Kế hoạch bảo trì phòng ngừa (PM) theo chu kỳ ngày, áp cho một tài sản hoặc cả loại thiết bị.</summary>
public class KeHoachBaoTri : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên kế hoạch")] public string Ten { get; set; } = "";
    [Display(Name = "Áp dụng cho loại thiết bị")] public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [Display(Name = "Áp dụng cho tài sản")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Range(1, 3650), Display(Name = "Chu kỳ (ngày)")] public int ChuKyNgay { get; set; } = 180;
    [Display(Name = "Ngày thực hiện tiếp theo"), DataType(DataType.Date)] public DateOnly NgayTiepTheo { get; set; }
    [StringLength(4000), Display(Name = "Checklist (mỗi dòng 1 mục)")] public string? Checklist { get; set; }
    [StringLength(100), Display(Name = "KTV phụ trách")] public string? KyThuatVien { get; set; }
    [Display(Name = "Đang áp dụng")] public bool DangApDung { get; set; } = true;

    public bool DenHan(DateOnly homNay) => DangApDung && NgayTiepTheo <= homNay;
    public void DayLichTiep() => NgayTiepTheo = NgayTiepTheo.AddDays(ChuKyNgay);
}

/// <summary>Trao đổi / bình luận giữa người báo và KTV ngay trong phiếu.</summary>
public class BinhLuanPhieu
{
    public long Id { get; set; }
    public int PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    public DateTime ThoiGian { get; set; }
    [StringLength(100)] public string NguoiViet { get; set; } = "";
    [Required, StringLength(2000)] public string NoiDung { get; set; } = "";
    /// <summary>Bình luận do hệ thống sinh khi đổi trạng thái.</summary>
    public bool LaHeThong { get; set; }
}
