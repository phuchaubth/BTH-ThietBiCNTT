using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum KieuTruong
{
    [Display(Name = "Văn bản")] VanBan = 1,
    [Display(Name = "Số")] So,
    [Display(Name = "Ngày")] Ngay,
    [Display(Name = "Danh sách chọn")] DanhSach,
    [Display(Name = "Có / Không")] CoKhong
}

/// <summary>Trường thông tin bổ sung do Quản trị định nghĩa (FR-TS-15), áp dụng cho mọi tài sản hoặc một loại thiết bị.</summary>
public class TruongTuyChinh : BaseEntity
{
    [Required, StringLength(40), RegularExpression("^[A-Za-z][A-Za-z0-9_]*$", ErrorMessage = "Mã chỉ gồm chữ không dấu, số, gạch dưới; bắt đầu bằng chữ."), Display(Name = "Mã trường")]
    public string Ma { get; set; } = "";
    [Required, StringLength(150), Display(Name = "Tên hiển thị")] public string Ten { get; set; } = "";
    [Display(Name = "Kiểu dữ liệu")] public KieuTruong Kieu { get; set; } = KieuTruong.VanBan;
    [StringLength(1000), Display(Name = "Các lựa chọn (cách nhau dấu ;)")] public string? LuaChon { get; set; }
    [Display(Name = "Bắt buộc")] public bool BatBuoc { get; set; }
    [Display(Name = "Thứ tự")] public int ThuTu { get; set; }
    [Display(Name = "Áp dụng cho loại thiết bị")] public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [StringLength(300), Display(Name = "Gợi ý nhập")] public string? MoTa { get; set; }
    [Display(Name = "Đang dùng")] public bool DangDung { get; set; } = true;

    public IReadOnlyList<string> DsLuaChon => (LuaChon ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool ApDung(int loaiThietBiId) => DangDung && (LoaiThietBiId is null || LoaiThietBiId == loaiThietBiId);

    private static readonly string[] DangNgay = ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy"];

    /// <summary>Kiểm tra và chuẩn hoá giá trị nhập để lưu (số: dạng invariant; ngày: yyyy-MM-dd; có/không: true/false). Rỗng → null.</summary>
    public string? ChuanHoa(string? v)
    {
        v = v?.Trim();
        if (string.IsNullOrEmpty(v))
        {
            if (BatBuoc) throw new DomainException($"'{Ten}' là trường bắt buộc.");
            return null;
        }
        switch (Kieu)
        {
            case KieuTruong.So:
                var s = v.Replace(" ", "");
                // Chấp nhận kiểu vi-VN (1.500 / 1.234,5) và kiểu máy (1500 / 1234.5):
                // dấu chấm theo nhóm 3 chữ số = phân cách nghìn; dấu phẩy = thập phân.
                if (s.Contains(',') && s.Contains('.')) s = s.Replace(".", "").Replace(',', '.');
                else if (s.Count(c => c == ',') == 1) s = s.Replace(',', '.');
                else if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^-?\d{1,3}(\.\d{3})+$")) s = s.Replace(".", "");
                if (!decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var so)) throw new DomainException($"'{Ten}': '{v}' không phải số.");
                return so.ToString("0.##########", CultureInfo.InvariantCulture);
            case KieuTruong.Ngay:
                if (!DateOnly.TryParseExact(v, DangNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)) throw new DomainException($"'{Ten}': '{v}' không phải ngày (dd/MM/yyyy).");
                return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            case KieuTruong.DanhSach:
                var chon = DsLuaChon.FirstOrDefault(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase))
                           ?? throw new DomainException($"'{Ten}': '{v}' không thuộc danh sách ({string.Join(", ", DsLuaChon)}).");
                return chon;
            case KieuTruong.CoKhong:
                return v.ToLowerInvariant() switch
                {
                    "true" or "1" or "có" or "co" or "x" or "yes" => "true",
                    "false" or "0" or "không" or "khong" or "no" => "false",
                    _ => throw new DomainException($"'{Ten}': nhập Có hoặc Không.")
                };
            default:
                if (v.Length > 500) throw new DomainException($"'{Ten}': tối đa 500 ký tự.");
                return v;
        }
    }

    /// <summary>Giá trị hiển thị cho người dùng.</summary>
    public string HienThi(string? luu) => luu is null ? "" : Kieu switch
    {
        KieuTruong.So => decimal.TryParse(luu, NumberStyles.Number, CultureInfo.InvariantCulture, out var s) ? s.ToString("#,0.##", new CultureInfo("vi-VN")) : luu,
        KieuTruong.Ngay => DateOnly.TryParseExact(luu, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("dd/MM/yyyy") : luu,
        KieuTruong.CoKhong => luu == "true" ? "Có" : "Không",
        _ => luu
    };

    /// <summary>Giá trị có kiểu (số, ngày, bool) — dùng cho Excel / báo cáo.</summary>
    public object? GiaTriCoKieu(string? luu) => luu is null ? null : Kieu switch
    {
        KieuTruong.So => decimal.TryParse(luu, NumberStyles.Number, CultureInfo.InvariantCulture, out var s) ? s : null,
        KieuTruong.Ngay => DateOnly.TryParseExact(luu, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null,
        KieuTruong.CoKhong => luu == "true" ? "Có" : "Không",
        _ => luu
    };

    public void KiemTraDinhNghia()
    {
        if (Kieu == KieuTruong.DanhSach && DsLuaChon.Count == 0) throw new DomainException("Trường kiểu danh sách cần ít nhất một lựa chọn.");
    }
}

public class GiaTriTuyChinh
{
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    public int TruongTuyChinhId { get; set; }
    public TruongTuyChinh? TruongTuyChinh { get; set; }
    [StringLength(500)] public string GiaTri { get; set; } = "";
}
