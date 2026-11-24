using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Khoá API (tạo, xác thực) và hàng đợi webhook.</summary>
public class TichHopService(AppDbContext db)
{
    public const string TienToKhoa = "tbk_";

    public static string Bam(string khoa) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(khoa))).ToLowerInvariant();

    /// <summary>Tạo khoá mới. Trả về khoá gốc (chỉ hiển thị một lần).</summary>
    public async Task<(KhoaApi Khoa, string KhoaGoc)> TaoKhoaAsync(string ten, IEnumerable<string> phamVi, string? ip, DateOnly? hetHan)
    {
        var pv = phamVi.Where(p => PhamViApi.TatCa.Any(x => x.Ma == p)).Distinct().ToList();
        if (string.IsNullOrWhiteSpace(ten)) throw new DomainException("Nhập tên hệ thống / mục đích.");
        if (pv.Count == 0) throw new DomainException("Chọn ít nhất một phạm vi quyền.");
        var goc = TienToKhoa + Convert.ToBase64String(RandomNumberGenerator.GetBytes(30)).Replace('+', 'A').Replace('/', 'B').TrimEnd('=');
        var k = new KhoaApi { Ten = ten.Trim(), PhamVi = string.Join(",", pv), IpChoPhep = NullNeuRong(KhoaApi.KiemTraDanhSachIp(ip)), HetHan = hetHan,
            TienTo = goc[..12], BamKhoa = Bam(goc) };
        db.KhoaApis.Add(k);
        await db.SaveChangesAsync();
        return (k, goc);
    }

    private static string? NullNeuRong(string s) => string.IsNullOrWhiteSpace(s) ? null : s;

    public Task<KhoaApi?> TimAsync(string khoaGoc)
    {
        var bam = Bam(khoaGoc.Trim());
        return db.KhoaApis.FirstOrDefaultAsync(k => k.BamKhoa == bam);
    }

    /// <summary>Xoá nhật ký API và webhook đã gửi cũ hơn số ngày cấu hình.</summary>
    public async Task<int> DonDepAsync(int ngay, CancellationToken ct = default)
    {
        var moc = DateTime.Now.AddDays(-ngay);
        return await db.NhatKyApis.Where(x => x.ThoiGian < moc).ExecuteDeleteAsync(ct)
             + await db.WebhookGuis.Where(x => x.TrangThai != TrangThaiGuiWebhook.Cho && x.ThoiGianTao < moc).ExecuteDeleteAsync(ct);
    }

    public static string TaoBiMat() => Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    /// <summary>Chữ ký gửi kèm header X-TBCNTT-Signature: sha256=HEX(HMAC-SHA256(bí mật, thân JSON)).</summary>
    public static string Ky(string biMat, string than) =>
        "sha256=" + Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(biMat), Encoding.UTF8.GetBytes(than))).ToLowerInvariant();
}

/// <summary>Gửi các webhook đến hạn (chạy nền 30 giây/lần). Lỗi → thử lại luỹ thừa, tối đa 8 lần.</summary>
public class WebhookSender(AppDbContext db, IHttpClientFactory http, ILogger<WebhookSender> log)
{
    public async Task<int> XuLyAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var ds = await db.WebhookGuis.Include(x => x.Webhook).Where(x => x.TrangThai == TrangThaiGuiWebhook.Cho && x.GuiTu <= now)
            .OrderBy(x => x.Id).Take(50).ToListAsync(ct);
        foreach (var g in ds) await GuiAsync(g, ct);
        if (ds.Count > 0) await db.SaveChangesAsync(ct);
        return ds.Count;
    }

    public async Task GuiAsync(WebhookGui g, CancellationToken ct)
    {
        var now = DateTime.Now;
        if (g.Webhook is null || !g.Webhook.DangHoatDong) { g.GhiLoi("Webhook đã tắt", null, now); g.TrangThai = TrangThaiGuiWebhook.Loi; return; }
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, g.Webhook.Url) { Content = new StringContent(g.NoiDung, Encoding.UTF8, "application/json") };
            req.Headers.Add("X-TBCNTT-Event", g.SuKien);
            req.Headers.Add("X-TBCNTT-Delivery", g.Id.ToString());
            req.Headers.Add("X-TBCNTT-Signature", TichHopService.Ky(g.Webhook.BiMat, g.NoiDung));
            req.Headers.UserAgent.Add(new ProductInfoHeaderValue("ThietBiCNTT-Webhook", "1.0"));
            using var res = await http.CreateClient("webhook").SendAsync(req, ct);
            if (res.IsSuccessStatusCode) g.GhiThanhCong((int)res.StatusCode, now);
            else g.GhiLoi($"HTTP {(int)res.StatusCode} {res.ReasonPhrase}", (int)res.StatusCode, now);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException or UriFormatException)
        {
            g.GhiLoi(ex.Message, null, now);
            log.LogWarning("Webhook {Id} → {Url} lỗi: {Loi}", g.Id, g.Webhook.Url, ex.Message);
        }
    }
}
