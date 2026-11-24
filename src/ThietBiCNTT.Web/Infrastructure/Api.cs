using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Xác thực khoá API: header <c>X-Api-Key: tbk_…</c> hoặc <c>Authorization: Bearer tbk_…</c>.</summary>
public class ApiKeyHandler(IOptionsMonitor<AuthenticationSchemeOptions> o, ILoggerFactory l, UrlEncoder e, TichHopService th, AppDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(o, l, e)
{
    public new const string Scheme = "ApiKey";
    public const string ClaimPhamVi = "scope";

    public static string? DocKhoa(HttpRequest r)
    {
        if (r.Headers.TryGetValue("X-Api-Key", out var k) && !string.IsNullOrWhiteSpace(k)) return k.ToString().Trim();
        var a = r.Headers.Authorization.ToString();
        return a.StartsWith("Bearer " + TichHopService.TienToKhoa, StringComparison.Ordinal) ? a[7..].Trim() : null;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var goc = DocKhoa(Request);
        if (goc is null) return AuthenticateResult.NoResult();
        var k = await th.TimAsync(goc);
        if (k is null) return Loi("Khoá API không hợp lệ.");
        Context.Items["KhoaApi"] = k;
        if (!k.CoHieuLuc(DateOnly.FromDateTime(DateTime.Today))) return Loi("Khoá API đã thu hồi hoặc hết hạn.");
        if (!k.IpHopLe(Context.Connection.RemoteIpAddress)) return Loi("IP không được phép dùng khoá này.");
        var now = DateTime.Now;
        if (k.LanCuoi is null || now - k.LanCuoi > TimeSpan.FromMinutes(1))
        {
            // Cập nhật trực tiếp (không qua nhật ký thao tác) để không làm đầy nhật ký hệ thống.
            var ip = Context.Connection.RemoteIpAddress?.ToString();
            await db.KhoaApis.Where(x => x.Id == k.Id).ExecuteUpdateAsync(u => u.SetProperty(x => x.LanCuoi, now).SetProperty(x => x.IpCuoi, ip));
        }
        var id = new ClaimsIdentity(Scheme);
        id.AddClaim(new Claim(ClaimTypes.Name, "api:" + k.Ten));
        id.AddClaim(new Claim("khoaApiId", k.Id.ToString()));
        foreach (var p in k.DsPhamVi) id.AddClaim(new Claim(ClaimPhamVi, p));
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(id), Scheme));
    }

    private AuthenticateResult Loi(string loi) { Context.Items["ApiLoi"] = loi; return AuthenticateResult.Fail(loi); }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        return Response.WriteAsJsonAsync(new { loi = Context.Items["ApiLoi"] as string ?? "Thiếu khoá API (header X-Api-Key)." });
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        return Response.WriteAsJsonAsync(new { loi = "Khoá API không có phạm vi quyền cho thao tác này." });
    }

    public static void DangKyChinhSach(AuthorizationOptions o)
    {
        foreach (var (ma, _) in PhamViApi.TatCa)
            o.AddPolicy("api:" + ma, p => p.AddAuthenticationSchemes(Scheme).RequireAuthenticatedUser().RequireClaim(ClaimPhamVi, ma));
    }
}

/// <summary>Lớp gốc của API /api/v1: chỉ xác thực bằng khoá API, không dùng cookie đăng nhập.</summary>
[ApiController, Authorize(AuthenticationSchemes = ApiKeyHandler.Scheme), Produces("application/json")]
public abstract class ApiV1Controller : ControllerBase
{
    protected static object Trang<T>(int page, int pageSize, int total, IEnumerable<T> items) => new { page, pageSize, total, items };
    protected ObjectResult Loi(int ma, string loi) => StatusCode(ma, new { loi });
}

/// <summary>Áp "phải đăng nhập" (cookie) cho mọi controller giao diện; API dùng khoá riêng.</summary>
public class DangNhapMacDinhConvention : IControllerModelConvention
{
    public void Apply(ControllerModel c)
    {
        if (!typeof(ApiV1Controller).IsAssignableFrom(c.ControllerType)) c.Filters.Add(new AuthorizeFilter());
    }
}

/// <summary>Bật/tắt API, giới hạn tần suất theo khoá, ghi nhật ký gọi API.</summary>
public class ApiMiddleware(RequestDelegate next, IServiceScopeFactory scopes, ILogger<ApiMiddleware> log)
{
    private static readonly ConcurrentDictionary<string, (long Phut, int Dem)> Dem = new();

    public async Task InvokeAsync(HttpContext ctx, ThamSoService thamSo)
    {
        if (!ctx.Request.Path.StartsWithSegments("/api/v1")) { await next(ctx); return; }
        if (!thamSo.BatTat(TS.ApiBat)) { ctx.Response.StatusCode = 503; await ctx.Response.WriteAsJsonAsync(new { loi = "API đang tắt (tham số Api.Bat)." }); return; }
        var goc = ApiKeyHandler.DocKhoa(ctx.Request);
        var khoaDem = goc is null ? "ip:" + ctx.Connection.RemoteIpAddress : TichHopService.Bam(goc)[..16];
        var phut = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMinute;
        var v = Dem.AddOrUpdate(khoaDem, (phut, 1), (_, x) => x.Phut == phut ? (phut, x.Dem + 1) : (phut, 1));
        if (Dem.Count > 10_000) foreach (var x in Dem.Where(x => x.Value.Phut < phut).Select(x => x.Key).ToList()) Dem.TryRemove(x, out _);
        var sw = Stopwatch.StartNew();
        if (v.Dem > thamSo.SoNguyen(TS.ApiGioiHanPhut))
        {
            ctx.Response.StatusCode = 429;
            ctx.Response.Headers.RetryAfter = "60";
            await ctx.Response.WriteAsJsonAsync(new { loi = "Vượt giới hạn số lời gọi mỗi phút." });
        }
        else await next(ctx);
        try
        {
            using var scope = scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var k = ctx.Items["KhoaApi"] as KhoaApi;
            db.NhatKyApis.Add(new NhatKyApi
            {
                KhoaApiId = k?.Id, TenKhoa = k?.Ten, ThoiGian = DateTime.Now, Ip = ctx.Connection.RemoteIpAddress?.ToString(),
                Method = ctx.Request.Method, Path = (ctx.Request.Path + ctx.Request.QueryString).ToString() is var p && p.Length > 300 ? p[..300] : p,
                MaHttp = ctx.Response.StatusCode, ThoiGianMs = (int)sw.ElapsedMilliseconds
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex) { log.LogWarning(ex, "Không ghi được nhật ký API"); }
    }
}
