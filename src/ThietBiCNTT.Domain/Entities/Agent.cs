using System.ComponentModel.DataAnnotations;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>
/// Máy trạm có cài agent (FR-CH-08). Mỗi lần agent gửi, bản ghi được cập nhật; nếu đã ghép tài sản thì cấu hình
/// của tài sản được cập nhật (ghi lịch sử từng trường thay đổi). Không kế thừa BaseEntity để không ghi nhật ký thao tác mỗi lần gửi.
/// </summary>
public class MayTramAgent
{
    public int Id { get; set; }
    [StringLength(63)] public string Hostname { get; set; } = "";
    [StringLength(100)] public string? SerialBios { get; set; }
    [StringLength(200)] public string? Mac { get; set; }
    [StringLength(200)] public string? Ip { get; set; }
    [StringLength(30)] public string? MaTaiSanKhaiBao { get; set; }
    public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(150)] public string? Cpu { get; set; }
    public decimal? RamGb { get; set; }
    public decimal? OCungTongGb { get; set; }
    public decimal? OCungTrongGb { get; set; }
    [StringLength(150)] public string? HeDieuHanh { get; set; }
    [StringLength(100)] public string? NguoiDangNhap { get; set; }
    [StringLength(20)] public string? PhienBanAgent { get; set; }
    /// <summary>Danh sách phần mềm, mỗi dòng "Tên|Phiên bản".</summary>
    public string? PhanMem { get; set; }
    public DateTime LanDau { get; set; }
    public DateTime LanCuoi { get; set; }
    [StringLength(45)] public string? IpGui { get; set; }
    public int SoLanGui { get; set; }

    public IEnumerable<string> DsPhanMem => (PhanMem ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool KhongGui(DateTime now, int ngay) => ngay > 0 && now - LanCuoi > TimeSpan.FromDays(ngay);

    /// <summary>Đối chiếu chuẩn cấu hình tối thiểu chạy HIS (FR-CH-09).</summary>
    public List<(string TieuChi, bool Dat, string GiaTri)> DoiChieu(ChuanCauHinh c)
    {
        var kq = new List<(string, bool, string)>();
        if (c.RamGb > 0) kq.Add(($"RAM ≥ {c.RamGb:0.#} GB", RamGb >= c.RamGb, RamGb is null ? "?" : $"{RamGb:0.#} GB"));
        if (c.OCungTrongGb > 0) kq.Add(($"Ổ C còn trống ≥ {c.OCungTrongGb:0} GB", OCungTrongGb >= c.OCungTrongGb, OCungTrongGb is null ? "?" : $"{OCungTrongGb:0.#} GB"));
        if (c.HeDieuHanh.Count > 0)
            kq.Add(($"HĐH: {string.Join(" / ", c.HeDieuHanh)}", HeDieuHanh is not null && c.HeDieuHanh.Any(h => HeDieuHanh.Contains(h, StringComparison.OrdinalIgnoreCase)), HeDieuHanh ?? "?"));
        foreach (var pm in c.PhanMemBatBuoc)
            kq.Add(($"Có phần mềm “{pm}”", DsPhanMem.Any(x => x.Split('|')[0].Contains(pm, StringComparison.OrdinalIgnoreCase)), ""));
        return kq;
    }

    /// <summary>So sánh danh sách phần mềm (theo tên): (được cài thêm, bị gỡ).</summary>
    public static (List<string> Them, List<string> Bo) SoSanhPhanMem(IEnumerable<string> cu, IEnumerable<string> moi)
    {
        static string Ten(string x) => x.Split('|')[0].Trim();
        var a = cu.Select(Ten).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var b = moi.Select(Ten).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return (b.Except(a, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(), a.Except(b, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
    }
}

public record ChuanCauHinh(decimal RamGb, decimal OCungTrongGb, IReadOnlyList<string> HeDieuHanh, IReadOnlyList<string> PhanMemBatBuoc);
