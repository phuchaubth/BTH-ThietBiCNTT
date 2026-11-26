using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum DoiTuongCanhBao
{
    [Display(Name = "Tài sản")] TaiSan = 1,
    [Display(Name = "Phiếu sửa chữa / bảo trì")] PhieuSuaChua,
    [Display(Name = "Kế hoạch bảo trì")] KeHoachBaoTri,
    [Display(Name = "Kết quả kiểm tra (từng mục)")] KetQuaKiemTra,
    [Display(Name = "Vật tư kho")] VatTu,
    [Display(Name = "Hợp đồng")] HopDong,
    [Display(Name = "Bản quyền phần mềm")] BanQuyen,
    [Display(Name = "Khoản mục ngân sách")] NganSach,
    [Display(Name = "Đề xuất mua sắm")] DeXuatMuaSam,
    [Display(Name = "Văn bản")] VanBan
}

public enum ToanTuCanhBao
{
    [Display(Name = ">")] Lon = 1,
    [Display(Name = "≥")] LonBang,
    [Display(Name = "<")] Nho,
    [Display(Name = "≤")] NhoBang,
    [Display(Name = "=")] Bang,
    [Display(Name = "≠")] Khac,
    [Display(Name = "chứa")] Chua,
    [Display(Name = "không chứa")] KhongChua,
    [Display(Name = "trống")] Trong,
    [Display(Name = "không trống")] KhongTrong,
    [Display(Name = "còn ≤ N ngày tới mốc")] TrongVongNgay,
    [Display(Name = "đã quá mốc ≥ N ngày")] QuaNgay
}

public enum HanhDongCanhBao
{
    [Display(Name = "Chỉ thông báo")] KhongCo = 1,
    [Display(Name = "Tạo yêu cầu sửa chữa cho tài sản (nếu chưa có phiếu mở)")] TaoYeuCauSuaChua,
    [Display(Name = "Nâng mức ưu tiên phiếu lên một bậc")] NangUuTien
}

/// <summary>
/// Quy tắc cảnh báo tuỳ chỉnh (FR-HT-15): Đối tượng → Điều kiện (AND) → Phạm vi → Người nhận → Kênh → Nhắc lại → Hành động.
/// Không cần sửa mã: trường điều kiện lấy từ danh mục trường của từng đối tượng.
/// </summary>
public class QuyTacCanhBao : BaseEntity
{
    [Required(ErrorMessage = "Nhập tên quy tắc."), StringLength(200), Display(Name = "Tên quy tắc")] public string Ten { get; set; } = "";
    [StringLength(1000), Display(Name = "Mô tả / nội dung gửi kèm")] public string? MoTa { get; set; }
    [Display(Name = "Đối tượng")] public DoiTuongCanhBao DoiTuong { get; set; } = DoiTuongCanhBao.TaiSan;
    [Display(Name = "Khoa/phòng")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Loại thiết bị")] public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [Display(Name = "Ưu tiên từ mức")] public Enums.MucDoUuTien? UuTienTu { get; set; }
    [StringLength(500), Display(Name = "Người nhận cụ thể")] public string? NguoiNhan { get; set; }
    [StringLength(200), Display(Name = "Vai trò nhận")] public string? VaiTroNhan { get; set; }
    [Display(Name = "Người phụ trách đối tượng")] public bool GuiNguoiPhuTrach { get; set; }
    [Display(Name = "Trưởng khoa của đối tượng")] public bool GuiTruongKhoa { get; set; }
    [Display(Name = "Mức độ")] public MucDoThongBao MucDo { get; set; } = MucDoThongBao.CanhBao;
    [Display(Name = "Chỉ trong ứng dụng (không email/Telegram/Zalo)")] public bool ChiTrongUngDung { get; set; }
    [Range(0, 8760), Display(Name = "Nhắc lại sau (giờ, 0 = một lần)")] public int NhacLaiSauGio { get; set; }
    [Display(Name = "Hành động tự động")] public HanhDongCanhBao HanhDong { get; set; } = HanhDongCanhBao.KhongCo;
    [Display(Name = "Đang bật")] public bool DangBat { get; set; } = true;
    public DateTime? LanChayCuoi { get; set; }
    public int SoLanGui { get; set; }
    [StringLength(500)] public string? LoiCuoi { get; set; }
    public List<DieuKienCanhBao> DieuKiens { get; set; } = [];

    public IEnumerable<string> DsNguoiNhan => Tach(NguoiNhan);
    public IEnumerable<string> DsVaiTro => Tach(VaiTroNhan);
    private static IEnumerable<string> Tach(string? s) => (s ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void KiemTra()
    {
        if (DieuKiens.Count == 0) throw new DomainException("Quy tắc phải có ít nhất một điều kiện.");
        if (DieuKiens.Count > 5) throw new DomainException("Tối đa 5 điều kiện.");
        foreach (var d in DieuKiens) d.KiemTra();
        if (!DsNguoiNhan.Any() && !DsVaiTro.Any() && !GuiNguoiPhuTrach && !GuiTruongKhoa && HanhDong == HanhDongCanhBao.KhongCo)
            throw new DomainException("Chọn ít nhất một người/vai trò nhận hoặc một hành động tự động.");
        if (HanhDong == HanhDongCanhBao.NangUuTien && DoiTuong != DoiTuongCanhBao.PhieuSuaChua)
            throw new DomainException("Nâng ưu tiên chỉ áp dụng cho đối tượng Phiếu sửa chữa.");
        if (HanhDong == HanhDongCanhBao.TaoYeuCauSuaChua && DoiTuong is not (DoiTuongCanhBao.TaiSan or DoiTuongCanhBao.KetQuaKiemTra))
            throw new DomainException("Tạo yêu cầu sửa chữa chỉ áp dụng cho Tài sản hoặc Kết quả kiểm tra.");
    }

    /// <summary>Đối tượng có thoả mọi điều kiện (AND) không.</summary>
    public bool Khop(IReadOnlyDictionary<string, object?> giaTri, DateOnly homNay) =>
        DieuKiens.All(d => d.Khop(giaTri.TryGetValue(d.Truong, out var v) ? v : null, homNay));

    public string MoTaDieuKien(Func<string, string> tenTruong) =>
        string.Join(" VÀ ", DieuKiens.Select(d => d.MoTa(tenTruong(d.Truong))));
}

public class DieuKienCanhBao
{
    public int Id { get; set; }
    public int QuyTacCanhBaoId { get; set; }
    [Required, StringLength(60)] public string Truong { get; set; } = "";
    public ToanTuCanhBao ToanTu { get; set; } = ToanTuCanhBao.Lon;
    [StringLength(200)] public string? GiaTri { get; set; }

    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <summary>Đọc số chấp nhận cả "27,5" và "1.500" (vi-VN).</summary>
    public static decimal? So(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        s = s.Trim().Replace(" ", "");
        if (System.Text.RegularExpressions.Regex.IsMatch(s, @"^-?\d{1,3}(\.\d{3})+$")) s = s.Replace(".", "");
        s = s.Replace(',', '.');
        return decimal.TryParse(s, NumberStyles.Number, Inv, out var v) ? v : null;
    }

    public void KiemTra()
    {
        if (string.IsNullOrWhiteSpace(Truong)) throw new DomainException("Chọn trường cho điều kiện.");
        if (ToanTu is ToanTuCanhBao.Trong or ToanTuCanhBao.KhongTrong) return;
        if (string.IsNullOrWhiteSpace(GiaTri)) throw new DomainException("Nhập giá trị so sánh cho điều kiện.");
        if (ToanTu is ToanTuCanhBao.TrongVongNgay or ToanTuCanhBao.QuaNgay && (So(GiaTri) is not { } n || n < 0 || n != Math.Floor(n)))
            throw new DomainException("Số ngày phải là số nguyên không âm.");
    }

    public string MoTa(string tenTruong) => ToanTu switch
    {
        ToanTuCanhBao.Trong or ToanTuCanhBao.KhongTrong => $"{tenTruong} {Ten(ToanTu)}",
        ToanTuCanhBao.TrongVongNgay => $"{tenTruong} còn ≤ {GiaTri} ngày",
        ToanTuCanhBao.QuaNgay => $"{tenTruong} đã quá ≥ {GiaTri} ngày",
        _ => $"{tenTruong} {Ten(ToanTu)} {GiaTri}"
    };

    private static string Ten(ToanTuCanhBao t) => typeof(ToanTuCanhBao).GetMember(t.ToString())[0]
        .GetCustomAttributes(typeof(DisplayAttribute), false).OfType<DisplayAttribute>().First().Name!;

    public bool Khop(object? v, DateOnly homNay)
    {
        if (v is DateTime dt) v = DateOnly.FromDateTime(dt);
        if (v is string sv && string.IsNullOrWhiteSpace(sv)) v = null;
        switch (ToanTu)
        {
            case ToanTuCanhBao.Trong: return v is null;
            case ToanTuCanhBao.KhongTrong: return v is not null;
        }
        if (v is null) return false;
        if (ToanTu is ToanTuCanhBao.TrongVongNgay or ToanTuCanhBao.QuaNgay)
        {
            if (v is not DateOnly d || So(GiaTri) is not { } n) return false;
            var cach = d.DayNumber - homNay.DayNumber;
            return ToanTu == ToanTuCanhBao.TrongVongNgay ? cach >= 0 && cach <= n : -cach >= n;
        }
        if (v is bool b)
        {
            var g = (GiaTri ?? "").Trim().ToLowerInvariant();
            var mong = g is "true" or "1" or "có" or "co" or "đúng" or "x";
            return ToanTu switch { ToanTuCanhBao.Bang => b == mong, ToanTuCanhBao.Khac => b != mong, _ => false };
        }
        if (v is DateOnly ngay)
        {
            if (!DateOnly.TryParseExact(GiaTri?.Trim(), ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"], Inv, DateTimeStyles.None, out var g)) return false;
            return SoSanh(ngay.CompareTo(g));
        }
        if (v is decimal or int or long or double)
        {
            var so = Convert.ToDecimal(v, Inv);
            if (ToanTu is ToanTuCanhBao.Chua or ToanTuCanhBao.KhongChua) return ChuoiKhop(so.ToString(Inv));
            return So(GiaTri) is { } g && SoSanh(so.CompareTo(g));
        }
        var s = v.ToString() ?? "";
        // Chuỗi là số (trường tuỳ chỉnh) → so sánh số khi toán tử là so sánh
        if (ToanTu is ToanTuCanhBao.Lon or ToanTuCanhBao.LonBang or ToanTuCanhBao.Nho or ToanTuCanhBao.NhoBang)
            return So(s) is { } a && So(GiaTri) is { } g2 && SoSanh(a.CompareTo(g2));
        return ChuoiKhop(s);
    }

    private bool ChuoiKhop(string s)
    {
        var g = (GiaTri ?? "").Trim();
        return ToanTu switch
        {
            ToanTuCanhBao.Bang => string.Equals(s.Trim(), g, StringComparison.OrdinalIgnoreCase),
            ToanTuCanhBao.Khac => !string.Equals(s.Trim(), g, StringComparison.OrdinalIgnoreCase),
            ToanTuCanhBao.Chua => s.Contains(g, StringComparison.OrdinalIgnoreCase),
            ToanTuCanhBao.KhongChua => !s.Contains(g, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private bool SoSanh(int c) => ToanTu switch
    {
        ToanTuCanhBao.Lon => c > 0, ToanTuCanhBao.LonBang => c >= 0, ToanTuCanhBao.Nho => c < 0, ToanTuCanhBao.NhoBang => c <= 0,
        ToanTuCanhBao.Bang => c == 0, ToanTuCanhBao.Khac => c != 0, _ => false
    };
}

/// <summary>Đã cảnh báo đối tượng nào theo quy tắc nào — chống gửi lặp, hỗ trợ nhắc lại.</summary>
public class CanhBaoDaGui
{
    public long Id { get; set; }
    public int QuyTacCanhBaoId { get; set; }
    [StringLength(50)] public string DoiTuongId { get; set; } = "";
    public DateTime LanDau { get; set; }
    public DateTime LanCuoi { get; set; }
    public int SoLan { get; set; }
    /// <summary>Lần đánh giá gần nhất đối tượng còn thoả điều kiện. Hết thoả → false; thoả lại → gửi lại ngay.</summary>
    public bool DangKhop { get; set; }
}
