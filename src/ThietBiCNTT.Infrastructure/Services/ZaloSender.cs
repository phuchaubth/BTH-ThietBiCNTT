using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThietBiCNTT.Domain.Entities;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>
/// Lưu access/refresh token Zalo OA. Access token hết hạn sau khoảng 25 giờ; refresh token đổi mỗi lần làm mới,
/// nên cặp token mới được lưu (mã hoá bằng Data Protection) ra tệp <c>zalo-token.dat</c> trong thư mục khoá / App_Data.
/// Giá trị ban đầu lấy từ appsettings: ThongBao:ZaloAccessToken, ThongBao:ZaloRefreshToken.
/// </summary>
public class ZaloTokenStore(IConfiguration cfg, IDataProtectionProvider dp, IHostEnvironment env, ILogger<ZaloTokenStore> log)
{
    private readonly SemaphoreSlim _khoa = new(1, 1);
    private readonly IDataProtector _p = dp.CreateProtector("ThietBiCNTT.Zalo");
    private string? _access, _refresh;
    private bool _daDoc;

    private string TepLuu => Path.Combine(cfg["DataProtection:KeysPath"] is { Length: > 0 } k ? k : Path.Combine(env.ContentRootPath, "App_Data"), "zalo-token.dat");

    private void Doc()
    {
        if (_daDoc) return;
        _daDoc = true;
        _access = cfg["ThongBao:ZaloAccessToken"]; _refresh = cfg["ThongBao:ZaloRefreshToken"];
        try
        {
            if (!File.Exists(TepLuu)) return;
            var t = JsonSerializer.Deserialize<string[]>(_p.Unprotect(File.ReadAllText(TepLuu)));
            if (t is [var a, var r]) { _access = a; _refresh = r; }
        }
        catch (Exception ex) when (ex is IOException or System.Security.Cryptography.CryptographicException or JsonException)
        { log.LogWarning(ex, "Không đọc được {Tep} — dùng token trong appsettings", TepLuu); }
    }

    public string? Access { get { lock (this) { Doc(); return _access; } } }

    /// <summary>Làm mới access token bằng refresh token (một luồng tại một thời điểm). Trả về token mới.</summary>
    public async Task<string> LamMoiAsync(HttpClient http, string tokenCu, CancellationToken ct)
    {
        await _khoa.WaitAsync(ct);
        try
        {
            lock (this) Doc();
            if (_access != tokenCu && !string.IsNullOrEmpty(_access)) return _access!; // luồng khác vừa làm mới
            var appId = cfg["ThongBao:ZaloAppId"]; var secret = cfg["ThongBao:ZaloSecretKey"];
            if (string.IsNullOrWhiteSpace(_refresh) || string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Access token Zalo hết hạn và chưa cấu hình ThongBao:ZaloRefreshToken / ZaloAppId / ZaloSecretKey để tự làm mới.");
            var goc = (cfg["ThongBao:ZaloOAuthBase"] ?? "https://oauth.zaloapp.com").TrimEnd('/');
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{goc}/v4/oa/access_token")
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["refresh_token"] = _refresh!, ["app_id"] = appId!, ["grant_type"] = "refresh_token" }) };
            req.Headers.Add("secret_key", secret);
            using var res = await http.SendAsync(req, ct);
            var j = await res.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (!j.TryGetProperty("access_token", out var a) || !j.TryGetProperty("refresh_token", out var r))
                throw new InvalidOperationException("Làm mới token Zalo thất bại: " + j.GetRawText()[..Math.Min(300, j.GetRawText().Length)]);
            lock (this) { _access = a.GetString(); _refresh = r.GetString(); }
            Directory.CreateDirectory(Path.GetDirectoryName(TepLuu)!);
            await File.WriteAllTextAsync(TepLuu, _p.Protect(JsonSerializer.Serialize(new[] { _access, _refresh })), ct);
            log.LogInformation("Đã làm mới access token Zalo OA");
            return _access!;
        }
        finally { _khoa.Release(); }
    }
}

/// <summary>Gửi tin tư vấn (CS) Zalo OA tới người dùng đã quan tâm OA. Lỗi token hết hạn → tự làm mới rồi gửi lại.</summary>
public class ZaloSender(IHttpClientFactory http, IConfiguration cfg, ZaloTokenStore tokens) : IKenhGui
{
    public KenhGui Kenh => KenhGui.Zalo;
    private static readonly int[] LoiToken = [-216, -124, -220];

    public async Task GuiAsync(TinNhanCho tin, CancellationToken ct)
    {
        var c = http.CreateClient("zalo");
        var token = tokens.Access;
        if (string.IsNullOrWhiteSpace(token)) token = await tokens.LamMoiAsync(c, "", ct);
        var (ma, tb) = await GoiAsync(c, token, tin, ct);
        if (LoiToken.Contains(ma)) (ma, tb) = await GoiAsync(c, await tokens.LamMoiAsync(c, token, ct), tin, ct);
        if (ma != 0) throw new InvalidOperationException($"Zalo OA lỗi {ma}: {tb}");
    }

    private async Task<(int Ma, string? ThongBao)> GoiAsync(HttpClient c, string token, TinNhanCho tin, CancellationToken ct)
    {
        var goc = (cfg["ThongBao:ZaloApiBase"] ?? "https://openapi.zalo.me").TrimEnd('/');
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{goc}/v3.0/oa/message/cs")
        { Content = JsonContent.Create(new { recipient = new { user_id = tin.DiaChi }, message = new { text = tin.NoiDung } }) };
        req.Headers.Add("access_token", token);
        using var res = await c.SendAsync(req, ct);
        var j = await res.Content.ReadFromJsonAsync<JsonElement>(ct);
        return (j.TryGetProperty("error", out var e) ? e.GetInt32() : (res.IsSuccessStatusCode ? 0 : (int)res.StatusCode),
                j.TryGetProperty("message", out var m) ? m.GetString() : null);
    }
}
