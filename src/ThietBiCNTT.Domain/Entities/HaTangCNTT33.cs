using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

// =====================================================================================
// GĐ3.3 – Quản lý thay đổi (TD): HCT.01.00 (lựa chọn, triển khai, đánh giá hệ thống mới), FMS.10.00 (PCRA),
// rà soát quyền truy cập (TC, MOI.01.01), theo dõi lỗ hổng, lưu giữ dữ liệu… (docs/JCI8-HaTangCNTT.md mục 4.5, 4.7)
// =====================================================================================

#region Quản lý thay đổi (FR-TD-01..10)

public enum LoaiThayDoi
{
    [Display(Name = "Chuẩn (lặp lại, rủi ro thấp)")] Chuan = 1,
    [Display(Name = "Thường")] Thuong,
    [Display(Name = "Khẩn cấp")] KhanCap,
    [Display(Name = "Triển khai hệ thống mới")] TrienKhaiMoi
}

public enum TrangThaiThayDoi
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Từ chối")] TuChoi,
    [Display(Name = "Đang thực hiện")] DangThucHien,
    [Display(Name = "Hoàn thành")] HoanThanh,
    [Display(Name = "Đã quay lui")] DaQuayLui,
    [Display(Name = "Thất bại")] ThatBai
}

public enum BuocDuyetThayDoi
{
    [Display(Name = "Trưởng phòng CNTT")] Cntt = 1,
    [Display(Name = "Chủ sở hữu nghiệp vụ")] ChuSoHuu,
    [Display(Name = "Lãnh đạo")] LanhDao
}

public enum NhomPcra
{
    [Display(Name = "Chất lượng không khí")] KhongKhi = 1,
    [Display(Name = "Kiểm soát nhiễm khuẩn")] NhiemKhuan,
    [Display(Name = "Tiện ích (điện, nước, khí, mạng)")] TienIch,
    [Display(Name = "Tiếng ồn")] TiengOn,
    [Display(Name = "Rung")] Rung,
    [Display(Name = "Vật liệu và chất thải nguy hại")] ChatThai,
    [Display(Name = "Phòng cháy")] PhongChay,
    [Display(Name = "An ninh")] AnNinh,
    [Display(Name = "Quy trình khẩn cấp")] KhanCap,
    [Display(Name = "Nguy cơ khác")] Khac
}

/// <summary>Phiếu đề nghị thay đổi (FR-TD-01..09).</summary>
public class PhieuThayDoi : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số phiếu")] public string Ma { get; set; } = "";
    [Display(Name = "Loại thay đổi")] public LoaiThayDoi Loai { get; set; } = LoaiThayDoi.Thuong;
    [Display(Name = "Trạng thái")] public TrangThaiThayDoi TrangThai { get; set; } = TrangThaiThayDoi.Nhap;
    [Required, StringLength(300), Display(Name = "Tên thay đổi")] public string Ten { get; set; } = "";
    [StringLength(3000), Display(Name = "Mô tả")] public string? MoTa { get; set; }
    [StringLength(2000), Display(Name = "Lý do")] public string? LyDo { get; set; }
    [Display(Name = "Hệ thống chính")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Tài sản tác động")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Display(Name = "Mức rủi ro")] public MucRuiRo MucRui { get; set; } = MucRuiRo.Thap;
    [Display(Name = "Thay đổi lớn")] public bool LaThayDoiLon { get; set; }
    [StringLength(3000), Display(Name = "Kế hoạch thực hiện")] public string? KeHoachThucHien { get; set; }
    [StringLength(3000), Display(Name = "Kế hoạch kiểm thử")] public string? KeHoachKiemThu { get; set; }
    [StringLength(3000), Display(Name = "Phương án quay lui")] public string? PhuongAnQuayLui { get; set; }
    [Display(Name = "Cần downtime")] public bool CanDowntime { get; set; }
    [Display(Name = "Đề xuất bắt đầu")] public DateTime? DeXuatBatDau { get; set; }
    [Display(Name = "Đề xuất kết thúc")] public DateTime? DeXuatKetThuc { get; set; }
    [Display(Name = "Đổi hệ thống sao lưu")] public bool LaHeThongSaoLuu { get; set; }
    [Display(Name = "Công cụ hỗ trợ quyết định lâm sàng / AI")] public bool LaCongCuHoTroQuyetDinh { get; set; }
    [StringLength(100)] public string? NguoiDeXuat { get; set; }
    [StringLength(100), Display(Name = "Người thực hiện")] public string? NguoiThucHien { get; set; }

    // Phê duyệt
    [StringLength(100)] public string? NguoiDuyetCntt { get; set; }
    public DateTime? NgayDuyetCntt { get; set; }
    [StringLength(100)] public string? NguoiDuyetChuSoHuu { get; set; }
    public DateTime? NgayDuyetChuSoHuu { get; set; }
    [StringLength(100)] public string? NguoiDuyetLanhDao { get; set; }
    public DateTime? NgayDuyetLanhDao { get; set; }
    [StringLength(1000)] public string? LyDoTuChoi { get; set; }
    public DateTime? NgayGuiDuyet { get; set; }

    // Khẩn cấp (BR-TD-02)
    public DateTime? HanDuyetBoSung { get; set; }
    public bool DaLeoThang { get; set; }

    // Thực hiện và kết quả (FR-TD-05)
    public DateTime? BatDauThucTe { get; set; }
    public DateTime? KetThucThucTe { get; set; }
    [StringLength(3000), Display(Name = "Kết quả / vấn đề phát sinh")] public string? KetQua { get; set; }
    [StringLength(2000), Display(Name = "Kiểm thử sau thay đổi")] public string? KiemThuSau { get; set; }
    [Display(Name = "Thành công có vấn đề")] public bool CoVanDe { get; set; }
    [StringLength(100)] public string? NguoiXacNhanNghiepVu { get; set; }
    public DateTime? NgayXacNhanNghiepVu { get; set; }
    [StringLength(50), Display(Name = "Phiên bản mới của hệ thống")] public string? PhienBanMoi { get; set; }
    public int? SuKienDowntimeId { get; set; }
    public SuKienDowntime? SuKienDowntime { get; set; }
    public int? HeThongMoiId { get; set; }
    public int? LanThuPhucHoiId { get; set; }

    // PCRA (FR-TD-08)
    [Display(Name = "Ảnh hưởng chăm sóc người bệnh")] public bool AnhHuongChamSoc { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp bảo đảm an toàn người bệnh")] public string? BienPhapAnToan { get; set; }
    [StringLength(100), Display(Name = "Người phụ trách tại khoa")] public string? NguoiPhuTrachKhoa { get; set; }
    [StringLength(200), Display(Name = "Nhà thầu")] public string? NhaThau { get; set; }
    [StringLength(2000), Display(Name = "Giám sát tuân thủ nhà thầu")] public string? GiamSatNhaThau { get; set; }

    public List<PhieuThayDoiHeThong> HeThongs { get; set; } = [];
    public List<MucPcra> MucPcras { get; set; } = [];
    public List<MucKiemTraThayDoi> Checklist { get; set; } = [];
    public DanhGiaSauThayDoi? DanhGiaSau { get; set; }

    public bool LaKhanCap => Loai == LoaiThayDoi.KhanCap;
    public bool DaKetThuc => TrangThai is TrangThaiThayDoi.HoanThanh or TrangThaiThayDoi.DaQuayLui or TrangThaiThayDoi.ThatBai or TrangThaiThayDoi.TuChoi;

    /// <summary>FR-TD-02: các bước duyệt cần có, theo thứ tự. <paramref name="toiQuanTrong"/>: có HTTT mức Tối quan trọng bị tác động.</summary>
    public List<BuocDuyetThayDoi> BuocCanDuyet(bool toiQuanTrong) => Loai switch
    {
        LoaiThayDoi.Chuan => [],
        LoaiThayDoi.KhanCap => [BuocDuyetThayDoi.Cntt],
        LoaiThayDoi.TrienKhaiMoi => [BuocDuyetThayDoi.Cntt, BuocDuyetThayDoi.LanhDao],
        _ => toiQuanTrong ? [BuocDuyetThayDoi.Cntt, BuocDuyetThayDoi.ChuSoHuu] : [BuocDuyetThayDoi.Cntt]
    };

    public bool DaDuyetBuoc(BuocDuyetThayDoi b) => b switch
    {
        BuocDuyetThayDoi.Cntt => NguoiDuyetCntt is not null,
        BuocDuyetThayDoi.ChuSoHuu => NguoiDuyetChuSoHuu is not null,
        _ => NguoiDuyetLanhDao is not null
    };

    public BuocDuyetThayDoi? BuocTiepTheo(bool toiQuanTrong) => BuocCanDuyet(toiQuanTrong).Cast<BuocDuyetThayDoi?>().FirstOrDefault(b => !DaDuyetBuoc(b!.Value));

    /// <summary>Điều kiện còn thiếu trước khi gửi duyệt / bắt đầu (BR-TD-03, FR-TD-06, FR-TD-08).</summary>
    public List<string> ThieuDieuKien()
    {
        var l = new List<string>();
        if (Loai != LoaiThayDoi.Chuan && string.IsNullOrWhiteSpace(PhuongAnQuayLui)) l.Add("chưa có phương án quay lui");
        if (Loai != LoaiThayDoi.Chuan && string.IsNullOrWhiteSpace(KeHoachThucHien)) l.Add("chưa có kế hoạch thực hiện");
        if (CanDowntime && (DeXuatBatDau is null || DeXuatKetThuc is null)) l.Add("chưa có khung giờ đề xuất cho downtime");
        if (AnhHuongChamSoc)
        {
            var thieu = Enum.GetValues<NhomPcra>().Count(n => !MucPcras.Any(m => m.Nhom == n && m.DaXong));
            if (thieu > 0) l.Add($"đánh giá rủi ro chăm sóc (PCRA) còn {thieu} mục chưa ghi");
            if (string.IsNullOrWhiteSpace(BienPhapAnToan)) l.Add("chưa ghi biện pháp bảo đảm an toàn người bệnh");
            if (string.IsNullOrWhiteSpace(NguoiPhuTrachKhoa)) l.Add("chưa chỉ định người phụ trách tại khoa");
        }
        if (Loai == LoaiThayDoi.TrienKhaiMoi)
        {
            var chua = Checklist.Count(m => !m.Dat);
            if (Checklist.Count == 0) l.Add("chưa tạo checklist đánh giá trước triển khai");
            else if (chua > 0) l.Add($"checklist trước triển khai còn {chua} mục chưa đạt");
        }
        return l;
    }

    public void GuiDuyet(string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiThayDoi.Nhap) throw new DomainException("Chỉ gửi duyệt phiếu ở trạng thái Nháp.");
        if (Loai == LoaiThayDoi.Chuan) throw new DomainException("Thay đổi Chuẩn không cần duyệt — bắt đầu thực hiện trực tiếp.");
        var l = ThieuDieuKien();
        if (l.Count > 0) throw new DomainException("Chưa gửi duyệt được: " + string.Join("; ", l) + ".");
        TrangThai = TrangThaiThayDoi.ChoDuyet; NgayGuiDuyet = now; LyDoTuChoi = null;
    }

    /// <summary>Duyệt một bước (đúng thứ tự). Trả về true khi đủ cấp.</summary>
    public bool Duyet(BuocDuyetThayDoi buoc, string nguoi, bool toiQuanTrong, DateTime now)
    {
        if (TrangThai != TrangThaiThayDoi.ChoDuyet && !(LaKhanCap && TrangThai == TrangThaiThayDoi.DangThucHien)) throw new DomainException("Phiếu không ở trạng thái chờ duyệt.");
        var tiep = BuocTiepTheo(toiQuanTrong) ?? throw new DomainException("Phiếu đã duyệt đủ cấp.");
        if (tiep != buoc) throw new DomainException($"Bước duyệt tiếp theo là: {tiep.Ten()}.");
        switch (buoc)
        {
            case BuocDuyetThayDoi.Cntt: NguoiDuyetCntt = nguoi; NgayDuyetCntt = now; break;
            case BuocDuyetThayDoi.ChuSoHuu: NguoiDuyetChuSoHuu = nguoi; NgayDuyetChuSoHuu = now; break;
            default: NguoiDuyetLanhDao = nguoi; NgayDuyetLanhDao = now; break;
        }
        var du = BuocTiepTheo(toiQuanTrong) is null;
        if (du && TrangThai == TrangThaiThayDoi.ChoDuyet) TrangThai = TrangThaiThayDoi.DaDuyet;
        return du;
    }

    public void TuChoi(string nguoi, string lyDo)
    {
        if (TrangThai != TrangThaiThayDoi.ChoDuyet) throw new DomainException("Phiếu không ở trạng thái chờ duyệt.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do từ chối.");
        TrangThai = TrangThaiThayDoi.TuChoi; LyDoTuChoi = $"{nguoi}: {lyDo.Trim()}";
    }

    /// <summary>BR-TD-01: Thường / Triển khai mới phải duyệt đủ cấp. Chuẩn và Khẩn cấp bắt đầu được ngay (Khẩn cấp duyệt bổ sung trong 2 ngày làm việc).</summary>
    public void BatDau(string nguoi, DateTime now)
    {
        switch (Loai)
        {
            case LoaiThayDoi.Chuan:
                if (TrangThai != TrangThaiThayDoi.Nhap) throw new DomainException("Phiếu không ở trạng thái Nháp.");
                break;
            case LoaiThayDoi.KhanCap:
                if (TrangThai is not (TrangThaiThayDoi.Nhap or TrangThaiThayDoi.ChoDuyet)) throw new DomainException("Phiếu khẩn cấp phải ở trạng thái Nháp hoặc Chờ duyệt.");
                if (string.IsNullOrWhiteSpace(LyDo)) throw new DomainException("Thay đổi khẩn cấp cần ghi lý do khẩn cấp.");
                if (string.IsNullOrWhiteSpace(PhuongAnQuayLui)) throw new DomainException("Chưa có phương án quay lui.");
                HanDuyetBoSung = CongNgayLam(now, 2);
                break;
            default:
                if (TrangThai != TrangThaiThayDoi.DaDuyet) throw new DomainException("Chưa duyệt đủ cấp — không được bắt đầu thực hiện.");
                break;
        }
        var l = ThieuDieuKien();
        if (l.Count > 0 && Loai != LoaiThayDoi.KhanCap) throw new DomainException("Chưa bắt đầu được: " + string.Join("; ", l) + ".");
        TrangThai = TrangThaiThayDoi.DangThucHien; BatDauThucTe = now; NguoiThucHien ??= nguoi;
    }

    /// <summary>FR-TD-05: ghi kết quả. Thay đổi lớn thành công cần người dùng nghiệp vụ xác nhận.</summary>
    public void GhiKetQua(TrangThaiThayDoi ketQua, string ketQuaMoTa, string? kiemThuSau, bool coVanDe, string? nguoiXacNhanNghiepVu, string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiThayDoi.DangThucHien) throw new DomainException("Phiếu không ở trạng thái Đang thực hiện.");
        if (ketQua is not (TrangThaiThayDoi.HoanThanh or TrangThaiThayDoi.DaQuayLui or TrangThaiThayDoi.ThatBai)) throw new DomainException("Kết quả không hợp lệ.");
        if (string.IsNullOrWhiteSpace(ketQuaMoTa)) throw new DomainException("Nhập mô tả kết quả.");
        if (ketQua == TrangThaiThayDoi.HoanThanh)
        {
            if (string.IsNullOrWhiteSpace(kiemThuSau)) throw new DomainException("Thay đổi thành công cần ghi kết quả kiểm thử sau thay đổi.");
            if (LaThayDoiLon && string.IsNullOrWhiteSpace(nguoiXacNhanNghiepVu)) throw new DomainException("Thay đổi lớn cần người dùng nghiệp vụ xác nhận.");
            if (LaKhanCap && NguoiDuyetCntt is null) throw new DomainException("Thay đổi khẩn cấp cần được Trưởng phòng CNTT duyệt bổ sung trước khi đóng.");
        }
        TrangThai = ketQua; KetQua = ketQuaMoTa.Trim(); KiemThuSau = kiemThuSau?.Trim(); CoVanDe = ketQua == TrangThaiThayDoi.HoanThanh && coVanDe;
        KetThucThucTe = now;
        if (!string.IsNullOrWhiteSpace(nguoiXacNhanNghiepVu)) { NguoiXacNhanNghiepVu = nguoiXacNhanNghiepVu.Trim(); NgayXacNhanNghiepVu = now; }
    }

    /// <summary>Gợi ý tạo hành động khắc phục khi thất bại, quay lui hoặc thành công có vấn đề.</summary>
    public bool NenTaoHanhDong => TrangThai is TrangThaiThayDoi.DaQuayLui or TrangThaiThayDoi.ThatBai || (TrangThai == TrangThaiThayDoi.HoanThanh && CoVanDe);

    /// <summary>BR-TD-02: khẩn cấp chưa duyệt bổ sung quá hạn.</summary>
    public bool QuaHanDuyetBoSung(DateTime now) => LaKhanCap && NguoiDuyetCntt is null && HanDuyetBoSung is { } h && h < now && TrangThai is TrangThaiThayDoi.DangThucHien or TrangThaiThayDoi.HoanThanh or TrangThaiThayDoi.ThatBai or TrangThaiThayDoi.DaQuayLui;

    /// <summary>Hạn sau N ngày làm việc (bỏ thứ bảy, chủ nhật).</summary>
    public static DateTime CongNgayLam(DateTime tu, int soNgay)
    {
        var d = tu;
        while (soNgay > 0) { d = d.AddDays(1); if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) soNgay--; }
        return d;
    }

    /// <summary>Hai khung giờ có giao nhau không (FR-TD-04).</summary>
    public static bool GiaoNhau(DateTime a1, DateTime a2, DateTime b1, DateTime b2) => a1 < b2 && b1 < a2;

    public static readonly string[] MucChecklistMacDinh =
    [
        "Yêu cầu bảo mật (mã hoá, xác thực, nhật ký)", "Phân quyền người dùng theo vai trò", "Sao lưu và khôi phục", "Tích hợp với hệ thống hiện có",
        "Đào tạo người dùng", "Kế hoạch downtime cho hệ thống mới", "Đánh giá của chủ sở hữu nghiệp vụ", "Đánh giá an toàn thông tin"
    ];

    public static readonly string[] MucChecklistAi =
    [
        "Cơ sở chuyên môn của công cụ (bằng chứng, hướng dẫn lâm sàng)", "Người chịu trách nhiệm giám sát công cụ", "Tiêu chí theo dõi hiệu quả và an toàn"
    ];

    public void TaoChecklistMacDinh()
    {
        foreach (var t in MucChecklistMacDinh) Checklist.Add(new MucKiemTraThayDoi { Ten = t });
        if (LaCongCuHoTroQuyetDinh) foreach (var t in MucChecklistAi) Checklist.Add(new MucKiemTraThayDoi { Ten = t });
    }
}

public class PhieuThayDoiHeThong
{
    public int PhieuThayDoiId { get; set; }
    public PhieuThayDoi? PhieuThayDoi { get; set; }
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
}

/// <summary>Một mục PCRA (FMS.10.00 ME1). Không áp dụng phải kèm lý do.</summary>
public class MucPcra
{
    public int Id { get; set; }
    public int PhieuThayDoiId { get; set; }
    public NhomPcra Nhom { get; set; }
    public bool ApDung { get; set; } = true;
    [StringLength(1000)] public string? NoiDung { get; set; }
    public bool DaXong => !string.IsNullOrWhiteSpace(NoiDung);
}

/// <summary>Checklist đánh giá trước triển khai hệ thống mới (FR-TD-06).</summary>
public class MucKiemTraThayDoi
{
    public int Id { get; set; }
    public int PhieuThayDoiId { get; set; }
    [StringLength(300)] public string Ten { get; set; } = "";
    public bool Dat { get; set; }
    [StringLength(1000)] public string? GhiChu { get; set; }
    [StringLength(100)] public string? NguoiXacNhan { get; set; }
}

/// <summary>FR-TD-09: đánh giá sau triển khai (HCT.01.00 ME4).</summary>
public class DanhGiaSauThayDoi
{
    public int Id { get; set; }
    public int PhieuThayDoiId { get; set; }
    public DateOnly Han { get; set; }
    public bool DaNhac { get; set; }
    [StringLength(1000)] public string? DeDung { get; set; }
    [StringLength(1000)] public string? DungMucDich { get; set; }
    [StringLength(1000)] public string? TichHop { get; set; }
    [StringLength(1000)] public string? AnToanNguoiBenh { get; set; }
    [StringLength(1000)] public string? TacDongNhanVien { get; set; }
    [StringLength(1500)] public string? KetLuan { get; set; }
    public DateTime? NgayDanhGia { get; set; }
    [StringLength(100)] public string? NguoiDanhGia { get; set; }
    public bool DaDanhGia => NgayDanhGia is not null;

    public void Ghi(string? deDung, string? dungMucDich, string? tichHop, string? anToan, string? tacDong, string ketLuan, string nguoi, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(ketLuan)) throw new DomainException("Nhập kết luận đánh giá.");
        if (string.IsNullOrWhiteSpace(anToan)) throw new DomainException("Nhập đánh giá tác động tới an toàn người bệnh / sai sót.");
        DeDung = deDung?.Trim(); DungMucDich = dungMucDich?.Trim(); TichHop = tichHop?.Trim(); AnToanNguoiBenh = anToan.Trim(); TacDongNhanVien = tacDong?.Trim();
        KetLuan = ketLuan.Trim(); NgayDanhGia = now; NguoiDanhGia = nguoi;
    }
}

/// <summary>BR-TD-04: danh mục thay đổi Chuẩn — Trưởng phòng CNTT duyệt, có hướng dẫn thực hiện.</summary>
public class ThayDoiChuanMau : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên thay đổi chuẩn")] public string Ten { get; set; } = "";
    [Required, StringLength(3000), Display(Name = "Hướng dẫn thực hiện")] public string HuongDan { get; set; } = "";
    [StringLength(1000), Display(Name = "Kiểm thử sau thay đổi")] public string? KiemThu { get; set; }
    [Display(Name = "Đang dùng")] public bool HoatDong { get; set; } = true;
    [StringLength(100)] public string? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public bool DaDuyet => NguoiDuyet is not null;
}

/// <summary>FR-TD-10: dự án xây dựng, cải tạo, phá dỡ có thể ảnh hưởng hạ tầng CNTT (FMS.10.00 Intent).</summary>
public class DuAnXayDung : BaseEntity
{
    [Required, StringLength(300), Display(Name = "Tên dự án")] public string Ten { get; set; } = "";
    [StringLength(300), Display(Name = "Vị trí")] public string? ViTri { get; set; }
    [Display(Name = "Từ ngày")] public DateOnly? TuNgay { get; set; }
    [Display(Name = "Đến ngày")] public DateOnly? DenNgay { get; set; }
    [StringLength(2000), Display(Name = "Hạ tầng CNTT có thể bị ảnh hưởng")] public string? HaTangAnhHuong { get; set; }
    [StringLength(200), Display(Name = "Người của Phòng CNTT tham gia đánh giá")] public string? NguoiThamGiaCntt { get; set; }
    [Display(Name = "Ngày đánh giá rủi ro")] public DateOnly? NgayDanhGia { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp bảo vệ hạ tầng")] public string? BienPhapBaoVe { get; set; }
    [StringLength(200), Display(Name = "Nhà thầu")] public string? NhaThau { get; set; }
    [StringLength(2000), Display(Name = "Kết quả giám sát")] public string? KetQuaGiamSat { get; set; }
    [Display(Name = "Phiếu thay đổi (di dời thiết bị)")] public int? PhieuThayDoiId { get; set; }
    public PhieuThayDoi? PhieuThayDoi { get; set; }
    [Display(Name = "Đã hoàn thành")] public bool DaHoanThanh { get; set; }

    public bool ThieuThongTin => NguoiThamGiaCntt is null || NgayDanhGia is null || string.IsNullOrWhiteSpace(BienPhapBaoVe);
}

#endregion

#region Rà soát quyền truy cập (FR-TC-01..07, MOI.01.01)

public enum TrangThaiRaSoat
{
    [Display(Name = "Chờ nhập danh sách")] ChoNhapDanhSach = 1,
    [Display(Name = "Đang xác nhận")] DangXacNhan,
    [Display(Name = "Chờ thực hiện")] ChoThucHien,
    [Display(Name = "Đã đóng")] DaDong
}

public enum QuyetDinhTaiKhoan
{
    [Display(Name = "Chưa xác nhận")] ChuaXacNhan = 0,
    [Display(Name = "Giữ")] Giu = 1,
    [Display(Name = "Thu hồi")] ThuHoi,
    [Display(Name = "Đổi quyền")] DoiQuyen
}

/// <summary>Một đợt rà soát quyền truy cập của một hệ thống (FR-TC-01, 03, 05).</summary>
public class DotRaSoatQuyen : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số đợt")] public string Ma { get; set; } = "";
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Trạng thái")] public TrangThaiRaSoat TrangThai { get; set; } = TrangThaiRaSoat.ChoNhapDanhSach;
    [Display(Name = "Hạn hoàn thành")] public DateOnly Han { get; set; }
    [StringLength(300), Display(Name = "Lý do mở")] public string? LyDo { get; set; }
    /// <summary>Số ngày không đăng nhập bị đánh dấu — chốt tại thời điểm nhập (FR-TC-04).</summary>
    public int SoNgayKhongDangNhap { get; set; } = 90;
    public bool DaNhacQuaHan { get; set; }
    public DateTime? NgayNhapDanhSach { get; set; }
    [StringLength(100)] public string? NguoiNhap { get; set; }
    public DateTime? NgayDong { get; set; }
    [StringLength(100)] public string? NguoiDong { get; set; }
    /// <summary>Biên bản rà soát (ảnh chụp lúc đóng đợt).</summary>
    public string? BienBan { get; set; }
    public List<TaiKhoanRaSoat> TaiKhoans { get; set; } = [];

    public int SoChuaXacNhan => TaiKhoans.Count(t => t.QuyetDinh == QuyetDinhTaiKhoan.ChuaXacNhan);
    public int SoCanThucHien => TaiKhoans.Count(t => t.QuyetDinh is QuyetDinhTaiKhoan.ThuHoi or QuyetDinhTaiKhoan.DoiQuyen);
    public int SoChuaThucHien => TaiKhoans.Count(t => t.QuyetDinh is QuyetDinhTaiKhoan.ThuHoi or QuyetDinhTaiKhoan.DoiQuyen && !t.DaThucHien);

    public void NhapDanhSach(IEnumerable<TaiKhoanRaSoat> ds, string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiRaSoat.ChoNhapDanhSach) throw new DomainException("Đợt đã có danh sách tài khoản.");
        var l = ds.ToList();
        if (l.Count == 0) throw new DomainException("Danh sách tài khoản trống.");
        foreach (var t in l) { t.PhatHien(SoNgayKhongDangNhap, now); TaiKhoans.Add(t); }
        TrangThai = TrangThaiRaSoat.DangXacNhan; NgayNhapDanhSach = now; NguoiNhap = nguoi;
    }

    public void ChuyenThucHien()
    {
        if (TrangThai != TrangThaiRaSoat.DangXacNhan) throw new DomainException("Đợt không ở trạng thái Đang xác nhận.");
        if (SoChuaXacNhan > 0) throw new DomainException($"Còn {SoChuaXacNhan} tài khoản chưa được xác nhận (Giữ / Thu hồi / Đổi quyền).");
        TrangThai = TrangThaiRaSoat.ChoThucHien;
    }

    /// <summary>FR-TC-05: đóng đợt khi mọi yêu cầu thu hồi / đổi quyền đã được cán bộ CNTT thực hiện.</summary>
    public void Dong(string nguoi, DateTime now)
    {
        if (TrangThai == TrangThaiRaSoat.DaDong) throw new DomainException("Đợt đã đóng.");
        if (TrangThai == TrangThaiRaSoat.ChoNhapDanhSach) throw new DomainException("Chưa nhập danh sách tài khoản.");
        if (SoChuaXacNhan > 0) throw new DomainException($"Còn {SoChuaXacNhan} tài khoản chưa được xác nhận.");
        if (SoChuaThucHien > 0) throw new DomainException($"Còn {SoChuaThucHien} yêu cầu thu hồi / đổi quyền chưa được thực hiện trên hệ thống.");
        TrangThai = TrangThaiRaSoat.DaDong; NgayDong = now; NguoiDong = nguoi;
        var nguoiXn = string.Join(", ", TaiKhoans.Select(t => t.NguoiXacNhan).Where(n => !string.IsNullOrEmpty(n)).Distinct());
        BienBan = $"Biên bản rà soát quyền truy cập {Ma} – hệ thống {HeThong?.Ma}\nNgày đóng: {now:dd/MM/yyyy HH:mm} · Người đóng: {nguoi}\n" +
                  $"Số tài khoản: {TaiKhoans.Count} · Giữ: {TaiKhoans.Count(t => t.QuyetDinh == QuyetDinhTaiKhoan.Giu)} · Thu hồi: {TaiKhoans.Count(t => t.QuyetDinh == QuyetDinhTaiKhoan.ThuHoi)} · Đổi quyền: {TaiKhoans.Count(t => t.QuyetDinh == QuyetDinhTaiKhoan.DoiQuyen)}\n" +
                  $"Tài khoản bị đánh dấu tự động: {TaiKhoans.Count(t => t.CoDauHieu)} · Người xác nhận: {nguoiXn}";
    }

    /// <summary>Hạn rà soát theo mức trọng yếu (tháng).</summary>
    public static int ChuKyThang(MucTrongYeu m, int thangToiQuanTrong, int thangCao) => m switch { MucTrongYeu.ToiQuanTrong => thangToiQuanTrong, MucTrongYeu.Cao => thangCao, _ => 0 };
}

public class TaiKhoanRaSoat
{
    public int Id { get; set; }
    public int DotRaSoatQuyenId { get; set; }
    public DotRaSoatQuyen? DotRaSoatQuyen { get; set; }
    [Required, StringLength(100)] public string TenDangNhap { get; set; } = "";
    [StringLength(200)] public string? HoTen { get; set; }
    [StringLength(200)] public string? KhoaText { get; set; }
    public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(200)] public string? NhomQuyen { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    [StringLength(50)] public string? TrangThaiTk { get; set; }
    // FR-TC-04
    public bool KhongDangNhapLau { get; set; }
    public bool DungChung { get; set; }
    public bool QuyenQuanTri { get; set; }
    public bool KhongThuocKhoa { get; set; }
    // FR-TC-03
    public QuyetDinhTaiKhoan QuyetDinh { get; set; }
    [StringLength(200)] public string? QuyenMoi { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
    [StringLength(100)] public string? NguoiXacNhan { get; set; }
    public DateTime? NgayXacNhan { get; set; }
    // FR-TC-05
    public bool DaThucHien { get; set; }
    [StringLength(100)] public string? NguoiThucHien { get; set; }
    public DateTime? NgayThucHien { get; set; }

    public bool CoDauHieu => KhongDangNhapLau || DungChung || QuyenQuanTri || KhongThuocKhoa;
    public List<string> DauHieu()
    {
        var l = new List<string>();
        if (KhongDangNhapLau) l.Add(LanDangNhapCuoi is null ? "chưa từng đăng nhập" : "lâu không đăng nhập");
        if (DungChung) l.Add("tài khoản dùng chung / không gắn người");
        if (QuyenQuanTri) l.Add("quyền quản trị");
        if (KhongThuocKhoa) l.Add("không thuộc khoa nào");
        return l;
    }

    static readonly string[] MauDungChung = ["chung", "shared", "dungchung", "test", "demo", "guest", "temp", "tam"];
    static readonly string[] MauQuanTri = ["admin", "quản trị", "quan tri", "administrator", "root", "dba", "sa ", "superuser", "super user"];

    /// <summary>FR-TC-04: đánh dấu tự động.</summary>
    public void PhatHien(int soNgay, DateTime now)
    {
        KhongDangNhapLau = soNgay > 0 && (LanDangNhapCuoi is null || LanDangNhapCuoi < now.AddDays(-soNgay));
        var u = TenDangNhap.ToLowerInvariant();
        DungChung = string.IsNullOrWhiteSpace(HoTen) || MauDungChung.Any(m => u.Contains(m));
        var q = (NhomQuyen ?? "").ToLowerInvariant() + " ";
        QuyenQuanTri = MauQuanTri.Any(m => q.Contains(m)) || u is "sa" or "root" or "admin" or "administrator";
        KhongThuocKhoa = KhoaPhongId is null;
    }

    public void XacNhan(QuyetDinhTaiKhoan qd, string? quyenMoi, string? ghiChu, string nguoi, DateTime now)
    {
        if (DotRaSoatQuyen is { TrangThai: not TrangThaiRaSoat.DangXacNhan }) throw new DomainException("Đợt không còn ở bước xác nhận.");
        if (qd == QuyetDinhTaiKhoan.ChuaXacNhan) throw new DomainException("Chọn Giữ, Thu hồi hoặc Đổi quyền.");
        if (qd == QuyetDinhTaiKhoan.DoiQuyen && string.IsNullOrWhiteSpace(quyenMoi)) throw new DomainException($"Tài khoản {TenDangNhap}: ghi quyền mới khi đổi quyền.");
        if (qd == QuyetDinhTaiKhoan.Giu && (QuyenQuanTri || DungChung) && string.IsNullOrWhiteSpace(ghiChu)) throw new DomainException($"Tài khoản {TenDangNhap} (quản trị / dùng chung): ghi lý do giữ.");
        QuyetDinh = qd; QuyenMoi = qd == QuyetDinhTaiKhoan.DoiQuyen ? quyenMoi!.Trim() : null; GhiChu = ghiChu?.Trim(); NguoiXacNhan = nguoi; NgayXacNhan = now;
        DaThucHien = false; NguoiThucHien = null; NgayThucHien = null;
    }

    public void DaThucHienTrenHeThong(string nguoi, DateTime now)
    {
        if (QuyetDinh is not (QuyetDinhTaiKhoan.ThuHoi or QuyetDinhTaiKhoan.DoiQuyen)) throw new DomainException("Tài khoản này không có yêu cầu thu hồi / đổi quyền.");
        if (DotRaSoatQuyen is { TrangThai: not TrangThaiRaSoat.ChoThucHien }) throw new DomainException("Đợt chưa chuyển sang bước thực hiện.");
        DaThucHien = true; NguoiThucHien = nguoi; NgayThucHien = now;
    }
}

public enum LoaiNghiViec
{
    [Display(Name = "Nghỉ việc")] NghiViec = 1,
    [Display(Name = "Chuyển khoa")] ChuyenKhoa
}

/// <summary>FR-TC-06: thông báo nghỉ việc / chuyển khoa → việc thu hồi quyền.</summary>
public class ThongBaoNghiViec : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Họ tên")] public string HoTen { get; set; } = "";
    [StringLength(100), Display(Name = "Tên đăng nhập")] public string? TenDangNhap { get; set; }
    [Display(Name = "Loại")] public LoaiNghiViec Loai { get; set; } = LoaiNghiViec.NghiViec;
    [Display(Name = "Khoa/phòng")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Ngày hiệu lực")] public DateOnly NgayHieuLuc { get; set; }
    [StringLength(200), Display(Name = "Khoa chuyển đến")] public string? KhoaMoi { get; set; }
    [Display(Name = "Hạn thu hồi")] public DateTime Han { get; set; }
    public bool DaLeoThang { get; set; }
    public List<ViecThuHoiQuyen> Viecs { get; set; } = [];
    public bool Xong => Viecs.Count > 0 && Viecs.All(v => v.DaThucHien);
}

public class ViecThuHoiQuyen
{
    public int Id { get; set; }
    public int ThongBaoNghiViecId { get; set; }
    public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [StringLength(300)] public string NoiDung { get; set; } = "";
    public bool DaThucHien { get; set; }
    [StringLength(100)] public string? NguoiThucHien { get; set; }
    public DateTime? NgayThucHien { get; set; }
}

public enum NguonViPham
{
    [Display(Name = "Nhật ký hệ thống")] NhatKyHeThong = 1,
    [Display(Name = "Báo cáo của khoa")] BaoCaoKhoa,
    [Display(Name = "Kết quả rà soát quyền")] KetQuaRaSoat
}

public enum TrangThaiViPham
{
    [Display(Name = "Mới")] Moi = 1,
    [Display(Name = "Đang xử lý")] DangXuLy,
    [Display(Name = "Đã đóng")] DaDong
}

/// <summary>FR-TC-07 (MOI.01.01 ME6): giám sát tuân thủ truy cập, hành động khi có vi phạm.</summary>
public class ViPhamTruyCap : BaseEntity
{
    [Display(Name = "Nguồn phát hiện")] public NguonViPham Nguon { get; set; } = NguonViPham.BaoCaoKhoa;
    [Required, StringLength(2000), Display(Name = "Mô tả vi phạm")] public string MoTa { get; set; } = "";
    [Display(Name = "Hệ thống")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Khoa/phòng liên quan")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Ngày phát hiện")] public DateOnly NgayPhatHien { get; set; }
    [Display(Name = "Nghiêm trọng")] public bool NghiemTrong { get; set; }
    [Display(Name = "Trạng thái")] public TrangThaiViPham TrangThai { get; set; } = TrangThaiViPham.Moi;
    [StringLength(100), Display(Name = "Người xử lý")] public string? NguoiXuLy { get; set; }
    [StringLength(2000), Display(Name = "Hành động đã thực hiện")] public string? HanhDong { get; set; }
    public DateTime? NgayDong { get; set; }
    public int? SuCoAnNinhId { get; set; }

    public void GhiHanhDong(string nguoi, string hanhDong, bool dong, DateTime now)
    {
        if (TrangThai == TrangThaiViPham.DaDong) throw new DomainException("Vi phạm đã đóng.");
        if (string.IsNullOrWhiteSpace(hanhDong)) throw new DomainException("Nhập hành động đã thực hiện.");
        NguoiXuLy = nguoi; HanhDong = hanhDong.Trim();
        if (dong) { TrangThai = TrangThaiViPham.DaDong; NgayDong = now; } else TrangThai = TrangThaiViPham.DangXuLy;
    }
}

#endregion
