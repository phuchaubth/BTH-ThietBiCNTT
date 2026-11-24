using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

public class PhanMemAgent { public string Ten { get; set; } = ""; public string? PhienBan { get; set; } }

/// <summary>Dữ liệu agent gửi lên (JSON).</summary>
public class CauHinhAgent
{
    public string Hostname { get; set; } = "";
    public string? SerialBios { get; set; }
    /// <summary>Mã tài sản khai báo trên máy (registry HKLM\SOFTWARE\BTH\ThietBi\MaTaiSan) — ưu tiên khi ghép.</summary>
    public string? MaTaiSan { get; set; }
    public List<string> Mac { get; set; } = [];
    public List<string> Ip { get; set; } = [];
    public string? Cpu { get; set; }
    public decimal? RamGb { get; set; }
    public decimal? OCungTongGb { get; set; }
    public decimal? OCungTrongGb { get; set; }
    /// <summary>Mô tả ổ đĩa vật lý, VD "SSD Samsung 512GB".</summary>
    public string? OCung { get; set; }
    public string? HeDieuHanh { get; set; }
    public string? NguoiDangNhap { get; set; }
    public string? PhienBanAgent { get; set; }
    public List<PhanMemAgent> PhanMem { get; set; } = [];
}

public record KetQuaAgent(int MayId, string? MaTaiSan, string CachGhep, List<string> ThayDoi, List<string> PhanMemThem, List<string> PhanMemBo);

/// <summary>Nhận cấu hình từ agent: ghép tài sản, cập nhật cấu hình + lịch sử, thông báo & webhook khi thay đổi.</summary>
public class AgentService(AppDbContext db, LichSuService lichSu, ThongBaoService tb, ThamSoService thamSo)
{
    private static string? Cat(string? s, int n) => string.IsNullOrWhiteSpace(s) ? null : (s.Trim().Length > n ? s.Trim()[..n] : s.Trim());
    public static string ChuanMac(string m) => new string(m.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant() is { Length: 12 } h
        ? string.Join("-", Enumerable.Range(0, 6).Select(i => h.Substring(i * 2, 2))) : m.Trim().ToUpperInvariant();

    public ChuanCauHinh Chuan()
    {
        static List<string> Ds(string s) => s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        return new(thamSo.SoThuc(TS.AgentChuanRam), thamSo.SoThuc(TS.AgentChuanOCungTrong), Ds(thamSo.Chuoi(TS.AgentChuanHdh)), Ds(thamSo.Chuoi(TS.AgentChuanPhanMem)));
    }

    public async Task<KetQuaAgent> NhanAsync(CauHinhAgent d, string? ipGui, string nguoi)
    {
        var host = Cat(d.Hostname, 63)?.ToUpperInvariant() ?? throw new DomainException("Thiếu hostname.");
        var serial = Cat(d.SerialBios, 100);
        if (serial is not null && (serial.Length < 4 || serial.Trim('0', ' ', '.').Length == 0 || serial.Contains("To be filled", StringComparison.OrdinalIgnoreCase)
                                   || serial.Equals("Default string", StringComparison.OrdinalIgnoreCase) || serial.Equals("System Serial Number", StringComparison.OrdinalIgnoreCase)))
            serial = null; // serial BIOS rác của máy lắp ráp — không dùng để ghép
        var macs = d.Mac.Where(m => !string.IsNullOrWhiteSpace(m)).Select(ChuanMac).Distinct().Take(8).ToList();
        var now = DateTime.Now;

        var may = (serial is not null ? await db.MayTramAgents.FirstOrDefaultAsync(m => m.SerialBios == serial) : null)
                  ?? await db.MayTramAgents.FirstOrDefaultAsync(m => m.Hostname == host);
        if (may is null) { may = new MayTramAgent { Hostname = host, LanDau = now }; db.MayTramAgents.Add(may); }
        var phanMemCu = may.DsPhanMem.ToList();
        may.Hostname = host; may.SerialBios = serial; may.Mac = Cat(string.Join(", ", macs), 200); may.Ip = Cat(string.Join(", ", d.Ip.Where(i => !string.IsNullOrWhiteSpace(i)).Take(8)), 200);
        may.MaTaiSanKhaiBao = Cat(d.MaTaiSan, 30); may.Cpu = Cat(d.Cpu, 150); may.RamGb = d.RamGb; may.OCungTongGb = d.OCungTongGb; may.OCungTrongGb = d.OCungTrongGb;
        may.HeDieuHanh = Cat(d.HeDieuHanh, 150); may.NguoiDangNhap = Cat(d.NguoiDangNhap, 100); may.PhienBanAgent = Cat(d.PhienBanAgent, 20);
        var pmMoi = d.PhanMem.Where(p => !string.IsNullOrWhiteSpace(p.Ten)).Select(p => $"{p.Ten.Trim().Replace("|", "/")}|{p.PhienBan?.Trim()}")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(1000).ToList();
        may.PhanMem = string.Join("\n", pmMoi);
        may.LanCuoi = now; may.IpGui = ipGui; may.SoLanGui++;

        // Ghép tài sản: đã ghép → giữ; mã khai báo → serial → (tuỳ chọn) hostname / MAC.
        var cachGhep = "đã ghép";
        TaiSan? ts = may.TaiSanId is { } id ? await db.TaiSans.Include(t => t.CauHinh).FirstOrDefaultAsync(t => t.Id == id) : null;
        if (ts is null && may.MaTaiSanKhaiBao is { } ma) { ts = await TimAsync(t => t.MaTaiSan == ma); cachGhep = "mã tài sản"; }
        if (ts is null && serial is not null) { ts = await TimAsync(t => t.SoSerial == serial); cachGhep = "serial"; }
        if (ts is null && thamSo.BatTat(TS.AgentTuGhepHostname))
        {
            ts = await TimAsync(t => t.CauHinh != null && t.CauHinh.Hostname != null && t.CauHinh.Hostname.ToUpper() == host); cachGhep = "hostname";
            var macCo = macs.Concat(macs.Select(m => m.Replace('-', ':'))).ToList(); // CSDL có thể lưu dạng AA:BB:… hoặc AA-BB-…
            if (ts is null && macs.Count > 0) { ts = await TimAsync(t => t.CauHinh != null && t.CauHinh.DiaChiMAC != null && macCo.Contains(t.CauHinh.DiaChiMAC.ToUpper())); cachGhep = "MAC"; }
        }
        if (ts is not null && may.TaiSanId != ts.Id && await db.MayTramAgents.AnyAsync(m => m.TaiSanId == ts.Id && m.Id != may.Id)) ts = null; // đã ghép máy khác
        var thayDoi = new List<string>();
        List<string> them = [], bo = [];
        if (phanMemCu.Count > 0) (them, bo) = MayTramAgent.SoSanhPhanMem(phanMemCu, pmMoi);
        if (ts is null) { may.TaiSanId = null; await db.SaveChangesAsync(); return new(may.Id, null, "chưa ghép", thayDoi, them, bo); }
        if (ts.DaThanhLy) { await db.SaveChangesAsync(); return new(may.Id, ts.MaTaiSan, "tài sản đã thanh lý — không cập nhật", thayDoi, them, bo); }
        may.TaiSanId = ts.Id; may.TaiSan = ts;

        var cu = ts.CauHinh;
        var moi = new CauHinh
        {
            CPU = may.Cpu ?? cu?.CPU, RAM = may.RamGb is { } r ? $"{r:0.#} GB" : cu?.RAM,
            OCung = Cat(d.OCung, 150) ?? (may.OCungTongGb is { } o ? $"{o:0} GB" : cu?.OCung),
            HeDieuHanh = Cat(may.HeDieuHanh, 100) ?? cu?.HeDieuHanh, BanQuyen = cu?.BanQuyen,
            DiaChiIP = Cat(d.Ip.FirstOrDefault(), 45) ?? cu?.DiaChiIP, DiaChiMAC = macs.FirstOrDefault() ?? cu?.DiaChiMAC, Hostname = host, PhanMem = cu?.PhanMem
        };
        if (cu is not null && !thamSo.BatTat(TS.AgentGhiDoiIp) && cu.DiaChiIP != moi.DiaChiIP) cu.DiaChiIP = moi.DiaChiIP; // đổi IP (DHCP): cập nhật im lặng
        if (cu is not null) thayDoi.AddRange(cu.SoSanh(moi).Select(x => $"{x.Truong}: '{x.Cu}' → '{x.Moi}'"));
        lichSu.CapNhatCauHinh(ts, moi, nguoi);
        if (them.Count + bo.Count > 0)
            lichSu.Ghi(ts.Id, LoaiSuKien.CauHinh, "Phần mềm" + (them.Count > 0 ? $" — cài thêm: {string.Join(", ", them)}" : "") + (bo.Count > 0 ? $" — gỡ bỏ: {string.Join(", ", bo)}" : ""), nguoi);
        if (cu is not null && (thayDoi.Count > 0 || them.Count + bo.Count > 0))
        {
            var noiDung = string.Join("\n", thayDoi.Concat(them.Select(x => "+ " + x)).Concat(bo.Select(x => "− " + x)).Take(30));
            await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.CauHinh, $"Cấu hình {ts.MaTaiSan} ({host}) thay đổi", noiDung, $"/TaiSan/Details/{ts.Id}",
                thayDoi.Count > 0 ? MucDoThongBao.CanhBao : MucDoThongBao.ThongTin);
            var tsId = ts.Id; var tsMa = ts.MaTaiSan; var td = thayDoi.ToList();
            db.PhatSuKien(SuKienWebhook.CauHinhThayDoi, () => new { taiSanId = tsId, maTaiSan = tsMa, hostname = host, thayDoi = td, phanMemThem = them, phanMemBo = bo });
        }
        await db.SaveChangesAsync();
        return new(may.Id, ts.MaTaiSan, cachGhep, thayDoi, them, bo);
    }

    private Task<TaiSan?> TimAsync(System.Linq.Expressions.Expression<Func<TaiSan, bool>> dk) =>
        db.TaiSans.Include(t => t.CauHinh).Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy).Where(dk).OrderBy(t => t.Id).FirstOrDefaultAsync();

    /// <summary>Ghép thủ công máy với tài sản.</summary>
    public async Task GhepAsync(int mayId, string maTaiSan, string nguoi)
    {
        var may = await db.MayTramAgents.FindAsync(mayId) ?? throw new DomainException("Không tìm thấy máy.");
        var ts = await db.TaiSans.Include(t => t.CauHinh).FirstOrDefaultAsync(t => t.MaTaiSan == maTaiSan.Trim()) ?? throw new DomainException($"Không có tài sản {maTaiSan}.");
        if (ts.DaThanhLy) throw new DomainException("Tài sản đã thanh lý.");
        if (await db.MayTramAgents.AnyAsync(m => m.TaiSanId == ts.Id && m.Id != mayId)) throw new DomainException($"{ts.MaTaiSan} đã ghép với máy khác — bỏ ghép máy đó trước.");
        may.TaiSanId = ts.Id;
        lichSu.CapNhatCauHinh(ts, new CauHinh
        {
            CPU = may.Cpu ?? ts.CauHinh?.CPU, RAM = may.RamGb is { } r ? $"{r:0.#} GB" : ts.CauHinh?.RAM, OCung = ts.CauHinh?.OCung ?? (may.OCungTongGb is { } o ? $"{o:0} GB" : null),
            HeDieuHanh = Cat(may.HeDieuHanh, 100) ?? ts.CauHinh?.HeDieuHanh, BanQuyen = ts.CauHinh?.BanQuyen, DiaChiIP = may.Ip?.Split(',')[0].Trim() ?? ts.CauHinh?.DiaChiIP,
            DiaChiMAC = may.Mac?.Split(',')[0].Trim() ?? ts.CauHinh?.DiaChiMAC, Hostname = may.Hostname, PhanMem = ts.CauHinh?.PhanMem
        }, nguoi);
        lichSu.Ghi(ts.Id, LoaiSuKien.CauHinh, $"Ghép với máy trạm agent {may.Hostname}", nguoi);
        await db.SaveChangesAsync();
    }
}

/// <summary>Dòng bản tin: máy lâu không gửi cấu hình, máy chưa ghép, máy không đạt chuẩn HIS.</summary>
public static class AgentTongHop
{
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var db = (AppDbContext)sp.GetService(typeof(AppDbContext))!;
        var ts = (ThamSoService)sp.GetService(typeof(ThamSoService))!;
        var svc = (AgentService)sp.GetService(typeof(AgentService))!;
        var ds = await db.MayTramAgents.AsNoTracking().ToListAsync(ct);
        if (ds.Count == 0) return [];
        var kq = new List<string>();
        var ngay = ts.SoNguyen(TS.AgentKhongGuiNgay);
        var im = ds.Count(m => m.TaiSanId != null && m.KhongGui(now, ngay));
        if (im > 0) kq.Add($"• {im} máy trạm không gửi cấu hình quá {ngay} ngày");
        var chua = ds.Count(m => m.TaiSanId == null);
        if (chua > 0) kq.Add($"• {chua} máy trạm agent chưa ghép với tài sản");
        var chuan = svc.Chuan();
        var khongDat = ds.Count(m => m.DoiChieu(chuan).Any(x => !x.Dat));
        if (khongDat > 0) kq.Add($"• {khongDat} máy trạm không đạt chuẩn cấu hình HIS");
        return kq;
    }
}
