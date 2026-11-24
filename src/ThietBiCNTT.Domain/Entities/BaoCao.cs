using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum NguonBaoCao
{
    [Display(Name = "Tài sản")] TaiSan = 1,
    [Display(Name = "Phiếu sửa chữa / bảo trì")] PhieuSuaChua
}

/// <summary>Mẫu báo cáo tuỳ biến (FR-BC-12): trường hiển thị, bộ lọc, nhóm — lưu dạng JSON.</summary>
public class BaoCaoMau : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên mẫu báo cáo")] public string Ten { get; set; } = "";
    public NguonBaoCao Nguon { get; set; }
    public string CauHinh { get; set; } = "{}";
    [Display(Name = "Chia sẻ cho mọi cán bộ quản lý")] public bool ChiaSe { get; set; }
}

public enum LoaiBaoCaoDinhKy
{
    [Display(Name = "KPI vận hành (SLA, MTTR, chi phí)")] KpiVanHanh = 1,
    [Display(Name = "Tổng chi phí sở hữu (TCO)")] Tco,
    [Display(Name = "Ngân sách dự toán – thực hiện")] NganSach,
    [Display(Name = "Mẫu báo cáo tuỳ biến")] TuyBien
}

public enum ChuKyGui
{
    [Display(Name = "Hằng tuần (thứ Hai, số liệu tuần trước)")] Tuan = 1,
    [Display(Name = "Hằng tháng (ngày 1, số liệu tháng trước)")] Thang
}

/// <summary>Lịch tự gửi báo cáo qua email (FR-BC-13).</summary>
public class LichGuiBaoCao : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên lịch gửi")] public string Ten { get; set; } = "";
    [Display(Name = "Báo cáo")] public LoaiBaoCaoDinhKy Loai { get; set; } = LoaiBaoCaoDinhKy.KpiVanHanh;
    [Display(Name = "Mẫu tuỳ biến")] public int? BaoCaoMauId { get; set; }
    public BaoCaoMau? BaoCaoMau { get; set; }
    [Display(Name = "Chu kỳ")] public ChuKyGui ChuKy { get; set; } = ChuKyGui.Thang;
    /// <summary>Tên đăng nhập hoặc địa chỉ email, cách nhau dấu phẩy.</summary>
    [Required, StringLength(1000), Display(Name = "Người nhận (tên đăng nhập hoặc email)")] public string NguoiNhan { get; set; } = "";
    [Display(Name = "Đang bật")] public bool DangBat { get; set; } = true;
    [StringLength(20)] public string? KyDaGui { get; set; }
    public DateTime? LanGuiCuoi { get; set; }
    [StringLength(300)] public string? TepCuoi { get; set; }
    [StringLength(500)] public string? KetQuaCuoi { get; set; }

    /// <summary>Kỳ cần gửi tại thời điểm <paramref name="now"/> (null nếu chưa đến ngày gửi) và khoảng số liệu [Tu, Den).</summary>
    public static (string Ky, DateTime Tu, DateTime Den)? KyDenHan(ChuKyGui ck, DateTime now)
    {
        var homNay = now.Date;
        if (ck == ChuKyGui.Thang)
        {
            if (homNay.Day != 1) return null;
            var tu = homNay.AddMonths(-1);
            return (tu.ToString("yyyy-MM", CultureInfo.InvariantCulture), tu, homNay);
        }
        if (homNay.DayOfWeek != DayOfWeek.Monday) return null;
        var t = homNay.AddDays(-7);
        return ($"{ISOWeek.GetYear(t)}-W{ISOWeek.GetWeekOfYear(t):00}", t, homNay);
    }

    /// <summary>Kỳ gần nhất đã kết thúc (dùng cho "Gửi ngay").</summary>
    public static (string Ky, DateTime Tu, DateTime Den) KyGanNhat(ChuKyGui ck, DateTime now)
    {
        var d = now.Date;
        if (ck == ChuKyGui.Thang) { var den = new DateTime(d.Year, d.Month, 1); var tu = den.AddMonths(-1); return (tu.ToString("yyyy-MM", CultureInfo.InvariantCulture), tu, den); }
        var thu2 = d.AddDays(-(((int)d.DayOfWeek + 6) % 7)); var t = thu2.AddDays(-7);
        return ($"{ISOWeek.GetYear(t)}-W{ISOWeek.GetWeekOfYear(t):00}", t, thu2);
    }

    public IReadOnlyList<string> DsNguoiNhan => NguoiNhan.Split([',', ';', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void KiemTra()
    {
        if (DsNguoiNhan.Count == 0) throw new DomainException("Nhập ít nhất một người nhận.");
        if (Loai == LoaiBaoCaoDinhKy.TuyBien && BaoCaoMauId is null) throw new DomainException("Chọn mẫu báo cáo tuỳ biến.");
    }
}
