using System.ComponentModel.DataAnnotations;
using System.Net;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>Phạm vi quyền của khoá API (FR-HT-17).</summary>
public static class PhamViApi
{
    public const string TaiSanDoc = "taisan.doc";
    public const string TaiSanGhi = "taisan.ghi";
    public const string CauHinhGhi = "cauhinh.ghi";
    public const string YeuCauTao = "yeucau.tao";
    public const string CongViecDoc = "congviec.doc";
    public const string KhoDoc = "kho.doc";
    public const string BaoCaoDoc = "baocao.doc";

    public static readonly (string Ma, string Ten)[] TatCa =
    [
        (TaiSanDoc, "Đọc tài sản, danh mục"), (TaiSanGhi, "Cập nhật tài sản (người dùng, vị trí, chỉ số)"),
        (CauHinhGhi, "Ghi cấu hình (agent)"), (YeuCauTao, "Tạo yêu cầu sửa chữa"),
        (CongViecDoc, "Đọc phiếu sửa chữa / bảo trì"), (KhoDoc, "Đọc tồn kho, đơn mua hàng"), (BaoCaoDoc, "Đọc báo cáo KPI")
    ];
}

/// <summary>Khoá API cho hệ thống ngoài / agent. Chỉ lưu băm SHA-256; khoá gốc hiển thị một lần khi tạo.</summary>
public class KhoaApi : BaseEntity
{
    [Required, StringLength(150), Display(Name = "Tên hệ thống / mục đích")] public string Ten { get; set; } = "";
    [StringLength(16)] public string TienTo { get; set; } = "";
    [StringLength(64)] public string BamKhoa { get; set; } = "";
    [StringLength(300), Display(Name = "Phạm vi quyền")] public string PhamVi { get; set; } = "";
    [StringLength(500), Display(Name = "IP được phép (để trống = mọi IP trong LAN)")] public string? IpChoPhep { get; set; }
    [Display(Name = "Hết hạn"), DataType(DataType.Date)] public DateOnly? HetHan { get; set; }
    public bool DangHoatDong { get; set; } = true;
    public DateTime? LanCuoi { get; set; }
    [StringLength(45)] public string? IpCuoi { get; set; }

    public IReadOnlyList<string> DsPhamVi => PhamVi.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool CoHieuLuc(DateOnly homNay) => DangHoatDong && (HetHan is null || HetHan >= homNay);

    /// <summary>Kiểm tra IP theo danh sách: địa chỉ đơn hoặc CIDR IPv4 (VD 10.10.0.0/16), cách nhau dấu phẩy.</summary>
    public bool IpHopLe(IPAddress? ip)
    {
        if (string.IsNullOrWhiteSpace(IpChoPhep)) return true;
        if (ip is null) return false;
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        foreach (var muc in IpChoPhep.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var phan = muc.Split('/');
            if (!IPAddress.TryParse(phan[0], out var goc)) continue;
            if (phan.Length == 1) { if (goc.Equals(ip)) return true; continue; }
            if (!int.TryParse(phan[1], out var bit) || goc.AddressFamily != ip.AddressFamily) continue;
            var a = goc.GetAddressBytes(); var b = ip.GetAddressBytes();
            var ok = true;
            for (var i = 0; i < a.Length && bit > 0; i++, bit -= 8)
            {
                var mask = bit >= 8 ? 0xFF : (byte)(0xFF << (8 - bit));
                if ((a[i] & mask) != (b[i] & mask)) { ok = false; break; }
            }
            if (ok) return true;
        }
        return false;
    }

    public static string KiemTraDanhSachIp(string? ds)
    {
        foreach (var muc in (ds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var p = muc.Split('/');
            if (!IPAddress.TryParse(p[0], out var ip) || p.Length > 2
                || (p.Length == 2 && (!int.TryParse(p[1], out var n) || n < 0 || n > (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128))))
                throw new DomainException($"IP/CIDR '{muc}' không hợp lệ.");
        }
        return string.Join(", ", (ds ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}

public class NhatKyApi
{
    public long Id { get; set; }
    public int? KhoaApiId { get; set; }
    [StringLength(150)] public string? TenKhoa { get; set; }
    public DateTime ThoiGian { get; set; }
    [StringLength(45)] public string? Ip { get; set; }
    [StringLength(10)] public string Method { get; set; } = "";
    [StringLength(300)] public string Path { get; set; } = "";
    public int MaHttp { get; set; }
    public int ThoiGianMs { get; set; }
}

/// <summary>Sự kiện gửi qua webhook (IF-10).</summary>
public static class SuKienWebhook
{
    public const string PhieuTao = "phieu.tao";
    public const string PhieuDong = "phieu.dong";
    public const string TaiSanTrangThai = "taisan.trangthai";
    public const string TaiSanGhiGiam = "taisan.ghigiam";
    public const string DonHangNhanHang = "donhang.nhanhang";
    public const string CauHinhThayDoi = "cauhinh.thaydoi";

    public static readonly (string Ma, string Ten)[] TatCa =
    [
        (PhieuTao, "Tạo yêu cầu sửa chữa / lệnh bảo trì"), (PhieuDong, "Đóng phiếu (nghiệm thu / huỷ)"),
        (TaiSanTrangThai, "Tài sản đổi trạng thái"), (TaiSanGhiGiam, "Ghi giảm tài sản (thanh lý)"),
        (DonHangNhanHang, "Nhận hàng theo đơn mua"), (CauHinhThayDoi, "Agent phát hiện thay đổi cấu hình")
    ];
}

public class Webhook : BaseEntity
{
    [Required, StringLength(150), Display(Name = "Tên")] public string Ten { get; set; } = "";
    [Required, StringLength(500), Url, Display(Name = "URL nhận (POST JSON)")] public string Url { get; set; } = "";
    [StringLength(300), Display(Name = "Sự kiện")] public string SuKien { get; set; } = "";
    /// <summary>Bí mật dùng ký HMAC-SHA256 (bên nhận cần cùng giá trị để xác minh).</summary>
    [StringLength(100)] public string BiMat { get; set; } = "";
    public bool DangHoatDong { get; set; } = true;
    public bool Nhan(string suKien) => DangHoatDong && SuKien.Split(',', StringSplitOptions.TrimEntries).Contains(suKien);
}

public enum TrangThaiGuiWebhook { [Display(Name = "Chờ gửi")] Cho = 1, [Display(Name = "Đã gửi")] DaGui, [Display(Name = "Lỗi")] Loi }

public class WebhookGui
{
    public const int SoLanToiDa = 8;
    public long Id { get; set; }
    public int WebhookId { get; set; }
    public Webhook? Webhook { get; set; }
    [StringLength(50)] public string SuKien { get; set; } = "";
    public string NoiDung { get; set; } = "";
    public TrangThaiGuiWebhook TrangThai { get; set; } = TrangThaiGuiWebhook.Cho;
    public int SoLan { get; set; }
    public DateTime ThoiGianTao { get; set; }
    public DateTime GuiTu { get; set; }
    public DateTime? GuiLuc { get; set; }
    public int? MaHttp { get; set; }
    [StringLength(500)] public string? LoiCuoi { get; set; }

    /// <summary>Ghi nhận một lần gửi lỗi: thử lại sau 1, 2, 4, 8… phút; quá số lần → Lỗi.</summary>
    public void GhiLoi(string loi, int? maHttp, DateTime now)
    {
        SoLan++; MaHttp = maHttp; LoiCuoi = loi.Length > 500 ? loi[..500] : loi;
        if (SoLan >= SoLanToiDa) TrangThai = TrangThaiGuiWebhook.Loi;
        else GuiTu = now.AddMinutes(Math.Pow(2, SoLan - 1));
    }

    public void GhiThanhCong(int maHttp, DateTime now) { SoLan++; MaHttp = maHttp; TrangThai = TrangThaiGuiWebhook.DaGui; GuiLuc = now; LoiCuoi = null; }
}
