using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

public enum TrangThaiKiemKe
{
    [Display(Name = "Đang kiểm kê")] DangKiemKe = 1,
    [Display(Name = "Đã khoá sổ")] DaKhoaSo
}

public enum KetQuaKiemKe
{
    [Display(Name = "Chưa kiểm")] ChuaKiem = 1,
    [Display(Name = "Khớp")] Khop,
    [Display(Name = "Sai vị trí")] SaiViTri,
    [Display(Name = "Thừa (ngoài sổ)")] Thua,
    [Display(Name = "Thiếu (không tìm thấy)")] Thieu
}

/// <summary>Đợt kiểm kê: chụp sổ sách lúc mở đợt, quét QR đối chiếu thực tế, khoá sổ → chưa kiểm = thiếu.</summary>
public class DotKiemKe : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã đợt")] public string Ma { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Tên đợt kiểm kê")] public string Ten { get; set; } = "";
    /// <summary>Phạm vi: null = toàn bệnh viện; có giá trị = một khoa/phòng.</summary>
    [Display(Name = "Phạm vi khoa/phòng")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Ngày bắt đầu")] public DateTime NgayBatDau { get; set; }
    [Display(Name = "Ngày khoá sổ")] public DateTime? NgayKhoaSo { get; set; }
    [Display(Name = "Trạng thái")] public TrangThaiKiemKe TrangThai { get; set; } = TrangThaiKiemKe.DangKiemKe;
    [StringLength(500), Display(Name = "Thành phần tổ kiểm kê")] public string? ToKiemKe { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }
    public ICollection<ChiTietKiemKe> ChiTiets { get; set; } = [];

    public bool DaKhoa => TrangThai == TrangThaiKiemKe.DaKhoaSo;

    /// <summary>Chụp sổ sách: mỗi tài sản trong phạm vi một dòng "Chưa kiểm".</summary>
    public void ChupSoSach(IEnumerable<TaiSan> taiSans)
    {
        if (ChiTiets.Count > 0) throw new DomainException("Đợt đã có dữ liệu sổ sách.");
        foreach (var t in taiSans)
            ChiTiets.Add(new ChiTietKiemKe { TaiSanId = t.Id, TaiSan = t, MaQuet = t.MaTaiSan, KhoaSoSachId = t.KhoaPhongId, KetQua = KetQuaKiemKe.ChuaKiem });
    }

    /// <summary>
    /// Ghi nhận một lần quét. <paramref name="taiSan"/> = tài sản tra được theo mã (null nếu mã không tồn tại).
    /// Trả về dòng chi tiết và cờ "đã kiểm trước đó".
    /// </summary>
    public (ChiTietKiemKe ChiTiet, bool DaKiemTruoc) GhiNhan(string ma, TaiSan? taiSan, int? khoaThucTeId,
        TrangThaiTaiSan? tinhTrang, string nguoi, DateTime now, string? ghiChu = null)
    {
        if (DaKhoa) throw new DomainException("Đợt kiểm kê đã khoá sổ.");
        ma = (ma ?? "").Trim().ToUpperInvariant();
        if (ma.Length == 0) throw new DomainException("Chưa có mã tài sản.");
        if (KhoaPhongId is not null && khoaThucTeId != KhoaPhongId)
            throw new DomainException("Đợt này chỉ kiểm kê tại khoa/phòng đã chọn.");
        var ct = taiSan is not null
            ? ChiTiets.FirstOrDefault(c => c.TaiSanId == taiSan.Id)
            : ChiTiets.FirstOrDefault(c => c.TaiSanId == null && c.MaQuet == ma);
        if (ct is not null && ct.KetQua != KetQuaKiemKe.ChuaKiem) return (ct, true);
        if (ct is null)
        {
            // Không có trong sổ sách của phạm vi (tài sản khoa khác / thêm sau khi mở đợt / mã lạ)
            ct = new ChiTietKiemKe { TaiSanId = taiSan?.Id, TaiSan = taiSan, MaQuet = taiSan?.MaTaiSan ?? ma, KhoaSoSachId = taiSan?.KhoaPhongId, KetQua = KetQuaKiemKe.Thua };
            ChiTiets.Add(ct);
        }
        else ct.KetQua = ct.KhoaSoSachId == khoaThucTeId ? KetQuaKiemKe.Khop : KetQuaKiemKe.SaiViTri;
        ct.KhoaThucTeId = khoaThucTeId; ct.TinhTrangThucTe = tinhTrang; ct.NguoiKiem = nguoi; ct.ThoiGianKiem = now; ct.GhiChu = ghiChu;
        return (ct, false);
    }

    public void KhoaSo(DateTime now)
    {
        if (DaKhoa) throw new DomainException("Đợt đã khoá sổ.");
        foreach (var c in ChiTiets.Where(c => c.KetQua == KetQuaKiemKe.ChuaKiem)) c.KetQua = KetQuaKiemKe.Thieu;
        NgayKhoaSo = now;
        TrangThai = TrangThaiKiemKe.DaKhoaSo;
    }

    public Dictionary<KetQuaKiemKe, int> ThongKe() =>
        Enum.GetValues<KetQuaKiemKe>().ToDictionary(k => k, k => ChiTiets.Count(c => c.KetQua == k));
}

public class ChiTietKiemKe
{
    public long Id { get; set; }
    public int DotKiemKeId { get; set; }
    public DotKiemKe? DotKiemKe { get; set; }
    public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(100)] public string MaQuet { get; set; } = "";
    /// <summary>Khoa theo sổ sách tại thời điểm mở đợt.</summary>
    public int? KhoaSoSachId { get; set; }
    public KhoaPhong? KhoaSoSach { get; set; }
    /// <summary>Khoa nơi thực tế tìm thấy.</summary>
    public int? KhoaThucTeId { get; set; }
    public KhoaPhong? KhoaThucTe { get; set; }
    public KetQuaKiemKe KetQua { get; set; }
    public TrangThaiTaiSan? TinhTrangThucTe { get; set; }
    [StringLength(100)] public string? NguoiKiem { get; set; }
    public DateTime? ThoiGianKiem { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
}
