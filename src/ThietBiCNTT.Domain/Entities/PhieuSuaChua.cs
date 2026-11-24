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
    /// <summary>QĐ-04: đang tạm dừng SLA (chờ linh kiện / gửi hãng) từ thời điểm này.</summary>
    public DateTime? BatDauTamDung { get; set; }
    /// <summary>Tổng thời gian đã tạm dừng (giây) — trừ khỏi thời gian xử lý khi tính MTTR.</summary>
    public long TongTamDungGiay { get; set; }
    // Theo dõi gửi hãng / bảo hành
    [StringLength(50), Display(Name = "Số phiếu hãng / RMA")] public string? SoPhieuHang { get; set; }
    [Display(Name = "Ngày gửi hãng")] public DateTime? NgayGuiHang { get; set; }
    [Display(Name = "Ngày hãng hẹn trả"), DataType(DataType.Date)] public DateOnly? NgayHenTra { get; set; }
    [Display(Name = "Ngày nhận về")] public DateTime? NgayNhanVe { get; set; }
    public bool TreHenTra(DateOnly homNay) => TrangThai == TrangThaiPhieuSuaChua.GuiHang && NgayHenTra is { } h && h < homNay;

    /// <summary>Đã gửi nhắc "sắp đến hạn" cho KTV.</summary>
    public bool DaNhacTruocHan { get; set; }
    /// <summary>Cấp leo thang đã thông báo khi quá hạn (0 = chưa; 1 = KTV & CNTT; 2 = trưởng phòng; 3 = lãnh đạo).</summary>
    public int CapLeoThang { get; set; }

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
    public bool DangTamDung => BatDauTamDung is not null;
    public bool QuaHan(DateTime now) => !DaDong && !DangTamDung && TrangThai != TrangThaiPhieuSuaChua.HoanThanh && now > HanXuLy;
    public bool HoanThanhDungHan => NgayHoanThanh is not null && NgayHoanThanh <= HanXuLy;

    /// <summary>Thời gian ngừng máy (giờ) = từ lúc báo đến lúc hoàn thành.</summary>
    public double? ThoiGianDungMayGio => NgayHoanThanh is null ? null : (NgayHoanThanh.Value - NgayYeuCau).TotalHours;
    /// <summary>Thời gian xử lý hiệu dụng (giờ) = dừng máy trừ thời gian tạm dừng chờ linh kiện/hãng.</summary>
    public double? ThoiGianXuLyGio => ThoiGianDungMayGio is { } g ? Math.Max(0, g - TongTamDungGiay / 3600.0) : null;

    /// <summary>Hàm dời hạn khi hết tạm dừng: (hạn cũ, lúc bắt đầu tạm dừng, lúc tiếp tục) → hạn mới.</summary>
    public delegate DateTime DoiHanSla(DateTime hanCu, DateTime batDauTamDung, DateTime tiepTuc);

    private void KetThucTamDung(DateTime now, DoiHanSla? doiHan)
    {
        if (BatDauTamDung is not { } s) return;
        TongTamDungGiay += (long)Math.Max(0, (now - s).TotalSeconds);
        if (doiHan is not null) HanXuLy = doiHan(HanXuLy, s, now);
        BatDauTamDung = null;
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

    public void BatDauXuLy(DateTime now, DoiHanSla? doiHan = null)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang);
        if (TrangThai == TrangThaiPhieuSuaChua.GuiHang) NgayNhanVe = now;
        KetThucTamDung(now, doiHan);
        NgayBatDau ??= now;
        TrangThai = TrangThaiPhieuSuaChua.DangXuLy;
    }

    /// <param name="dungSla">QĐ-04: true = dừng đồng hồ SLA trong thời gian chờ (tham số Sla.TamDungKhiCho).</param>
    public void TamDung(TrangThaiPhieuSuaChua lyDo, DateTime now, bool dungSla = true)
    {
        if (lyDo is not (TrangThaiPhieuSuaChua.ChoLinhKien or TrangThaiPhieuSuaChua.GuiHang))
            throw new DomainException("Lý do tạm dừng không hợp lệ.");
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang);
        if (TrangThai == lyDo) return;
        NgayBatDau ??= now;
        if (dungSla) BatDauTamDung ??= now;
        if (lyDo == TrangThaiPhieuSuaChua.GuiHang) { NgayGuiHang = now; NgayNhanVe = null; }
        TrangThai = lyDo;
    }

    public void HoanThanh(string? nguyenNhan, string? bienPhap, decimal gioCong, decimal chiPhiThueNgoai, DateTime now, DoiHanSla? doiHan = null)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.GuiHang, TrangThaiPhieuSuaChua.ChoLinhKien);
        if (string.IsNullOrWhiteSpace(bienPhap)) throw new DomainException("Phải nhập biện pháp khắc phục.");
        if (gioCong < 0 || chiPhiThueNgoai < 0) throw new DomainException("Giờ công / chi phí không được âm.");
        if (TrangThai == TrangThaiPhieuSuaChua.GuiHang) NgayNhanVe = now;
        KetThucTamDung(now, doiHan);
        NguyenNhan = nguyenNhan;
        BienPhapKhacPhuc = bienPhap;
        GioCong = gioCong;
        ChiPhiThueNgoai = chiPhiThueNgoai;
        NgayBatDau ??= now;
        NgayHoanThanh = now;
        TrangThai = TrangThaiPhieuSuaChua.HoanThanh;
    }

    /// <summary>
    /// QĐ-01: chỉ người báo hỏng hoặc Trưởng khoa của khoa sở hữu được nghiệm thu; KTV xử lý phiếu không tự nghiệm thu.
    /// </summary>
    public bool DuocNghiemThu(string? userName, bool laTruongKhoa, int? khoaCuaUser) =>
        !string.IsNullOrEmpty(userName) && !string.Equals(userName, KyThuatVien, StringComparison.OrdinalIgnoreCase)
        && (string.Equals(userName, NguoiTao, StringComparison.OrdinalIgnoreCase) || (laTruongKhoa && khoaCuaUser is not null && khoaCuaUser == KhoaPhongId));

    public void NghiemThu(string nguoiNghiemThu, int? danhGia, string? yKien, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.HoanThanh);
        if (danhGia is < 1 or > 5) throw new DomainException("Đánh giá từ 1 đến 5.");
        NguoiNghiemThu = nguoiNghiemThu;
        DanhGia = danhGia;
        YKienNguoiDung = yKien;
        NgayNghiemThu = now;
        TrangThai = TrangThaiPhieuSuaChua.DaNghiemThu;
    }

    /// <summary>QĐ-01: phiếu hoàn thành quá N ngày không phản hồi → hệ thống tự nghiệm thu.</summary>
    public bool TuDongNghiemThu(int soNgay, DateTime now)
    {
        if (soNgay <= 0 || TrangThai != TrangThaiPhieuSuaChua.HoanThanh || NgayHoanThanh is null || NgayHoanThanh.Value.AddDays(soNgay) > now) return false;
        NguoiNghiemThu = "Hệ thống (tự động sau " + soNgay + " ngày)";
        NgayNghiemThu = now;
        TrangThai = TrangThaiPhieuSuaChua.DaNghiemThu;
        return true;
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
        BatDauTamDung = null;
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
    /// <summary>Kho xuất & phiếu xuất kho tương ứng (truy vết FR-KHO-09).</summary>
    public int? KhoId { get; set; }
    public int? PhieuKhoId { get; set; }
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
