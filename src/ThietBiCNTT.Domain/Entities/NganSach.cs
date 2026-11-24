using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum NhomChiNganSach
{
    [Display(Name = "Mua sắm thiết bị")] MuaSamThietBi = 1,
    [Display(Name = "Vật tư, linh kiện thay thế")] VatTuLinhKien,
    [Display(Name = "Sửa chữa thuê ngoài")] SuaChuaThueNgoai,
    [Display(Name = "Bảo trì theo hợp đồng")] BaoTriHopDong,
    [Display(Name = "Phần mềm, bản quyền")] PhanMem,
    [Display(Name = "Khác")] Khac
}

public enum TrangThaiNganSach
{
    [Display(Name = "Dự thảo")] DuThao = 1,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Đã khoá sổ")] DaKhoa
}

/// <summary>
/// Khoản mục ngân sách CNTT trong một năm. Dự toán được duyệt + điều chỉnh đã duyệt = dự toán hiệu lực.
/// Cam kết (đề xuất đã duyệt chưa đặt, đơn hàng chưa nhận) và thực chi (nhận hàng, chi phí sửa ngoài, chi ghi tay) tính từ chứng từ.
/// </summary>
public class KhoanMucNganSach : BaseEntity
{
    [Required, StringLength(30), Display(Name = "Mã khoản mục")] public string Ma { get; set; } = "";
    [Required, StringLength(250), Display(Name = "Tên khoản mục")] public string Ten { get; set; } = "";
    [Range(2000, 2100), Display(Name = "Năm ngân sách")] public int Nam { get; set; } = DateTime.Today.Year;
    [Display(Name = "Nhóm chi")] public NhomChiNganSach NhomChi { get; set; } = NhomChiNganSach.VatTuLinhKien;
    [Range(0, double.MaxValue), Display(Name = "Dự toán năm (VNĐ)")] public decimal DuToan { get; set; }
    [Display(Name = "Phân bổ quý I")] public decimal? Quy1 { get; set; }
    [Display(Name = "Phân bổ quý II")] public decimal? Quy2 { get; set; }
    [Display(Name = "Phân bổ quý III")] public decimal? Quy3 { get; set; }
    [Display(Name = "Phân bổ quý IV")] public decimal? Quy4 { get; set; }
    [Display(Name = "Tự tính chi phí sửa chữa thuê ngoài")] public bool GomChiPhiSuaNgoai { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú / căn cứ")] public string? GhiChu { get; set; }
    public TrangThaiNganSach TrangThai { get; set; } = TrangThaiNganSach.DuThao;
    [StringLength(100)] public string? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    public List<DieuChinhNganSach> DieuChinhs { get; set; } = [];
    public List<ChiNganSach> KhoanChis { get; set; } = [];

    public decimal TongDieuChinh => DieuChinhs.Where(d => d.DaDuyet).Sum(d => d.SoTien);
    public decimal DuToanHieuLuc => DuToan + TongDieuChinh;
    public bool DungDuoc => TrangThai == TrangThaiNganSach.DaDuyet;

    public void KiemTraPhanBo()
    {
        if (DuToan < 0) throw new DomainException("Dự toán không được âm.");
        decimal?[] q = [Quy1, Quy2, Quy3, Quy4];
        if (q.Any(x => x is not null) && q.Sum(x => x ?? 0) != DuToan)
            throw new DomainException($"Tổng phân bổ 4 quý ({q.Sum(x => x ?? 0):N0}) phải bằng dự toán năm ({DuToan:N0}).");
    }

    public void Duyet(string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiNganSach.DuThao) throw new DomainException("Chỉ duyệt khoản mục đang dự thảo.");
        if (string.Equals(nguoi, NguoiTao, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người lập không tự duyệt dự toán.");
        KiemTraPhanBo();
        TrangThai = TrangThaiNganSach.DaDuyet; NguoiDuyet = nguoi; NgayDuyet = now;
    }

    /// <summary>Đề nghị điều chỉnh (tăng/giảm) — chờ duyệt.</summary>
    public DieuChinhNganSach DeNghiDieuChinh(decimal soTien, string lyDo, string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiNganSach.DaDuyet) throw new DomainException("Chỉ điều chỉnh khoản mục đã duyệt, chưa khoá sổ.");
        if (soTien == 0) throw new DomainException("Số tiền điều chỉnh phải khác 0.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Ghi lý do điều chỉnh.");
        if (DuToanHieuLuc + soTien < 0) throw new DomainException("Dự toán sau điều chỉnh không được âm.");
        var d = new DieuChinhNganSach { SoTien = soTien, LyDo = lyDo.Trim(), NguoiDeNghi = nguoi, NgayDeNghi = now };
        DieuChinhs.Add(d);
        return d;
    }

    public void DuyetDieuChinh(DieuChinhNganSach d, bool dongY, string nguoi, DateTime now)
    {
        if (d.DaDuyet || d.TuChoi) throw new DomainException("Điều chỉnh đã được xử lý.");
        if (string.Equals(nguoi, d.NguoiDeNghi, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người đề nghị không tự duyệt điều chỉnh.");
        if (dongY && DuToanHieuLuc + d.SoTien < 0) throw new DomainException("Dự toán sau điều chỉnh không được âm.");
        d.DaDuyet = dongY; d.TuChoi = !dongY; d.NguoiDuyet = nguoi; d.NgayDuyet = now;
    }

    public void KhoaSo()
    {
        if (TrangThai != TrangThaiNganSach.DaDuyet) throw new DomainException("Chỉ khoá sổ khoản mục đã duyệt.");
        TrangThai = TrangThaiNganSach.DaKhoa;
    }
}

public class DieuChinhNganSach
{
    public int Id { get; set; }
    public int KhoanMucNganSachId { get; set; }
    public decimal SoTien { get; set; }
    [StringLength(500)] public string LyDo { get; set; } = "";
    [StringLength(100)] public string NguoiDeNghi { get; set; } = "";
    public DateTime NgayDeNghi { get; set; }
    public bool DaDuyet { get; set; }
    public bool TuChoi { get; set; }
    [StringLength(100)] public string? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
}

/// <summary>Khoản chi ghi tay (chi ngoài luồng mua sắm: phí bảo trì hợp đồng, gia hạn bản quyền…).</summary>
public class ChiNganSach
{
    public int Id { get; set; }
    public int KhoanMucNganSachId { get; set; }
    public DateOnly Ngay { get; set; }
    public decimal SoTien { get; set; }
    [StringLength(50)] public string? SoChungTu { get; set; }
    [StringLength(500)] public string NoiDung { get; set; } = "";
    public int? HopDongId { get; set; }
    public int? PhieuSuaChuaId { get; set; }
    [StringLength(100)] public string NguoiNhap { get; set; } = "";
    public DateTime ThoiGian { get; set; }
}

/// <summary>Tình hình sử dụng một khoản mục (tính tại thời điểm xem).</summary>
public record SuDungNganSach(KhoanMucNganSach KhoanMuc, decimal CamKetDeXuat, decimal CamKetDonHang, decimal ThucChiNhanHang, decimal ThucChiSuaNgoai, decimal ThucChiKhac)
{
    public decimal DuToan => KhoanMuc.DuToanHieuLuc;
    public decimal CamKet => CamKetDeXuat + CamKetDonHang;
    public decimal ThucChi => ThucChiNhanHang + ThucChiSuaNgoai + ThucChiKhac;
    public decimal ConLai => DuToan - CamKet - ThucChi;
    /// <summary>% đã dùng (cam kết + thực chi) / dự toán.</summary>
    public decimal TyLe => DuToan <= 0 ? (CamKet + ThucChi > 0 ? 999 : 0) : Math.Round((CamKet + ThucChi) * 100 / DuToan, 1);
}
