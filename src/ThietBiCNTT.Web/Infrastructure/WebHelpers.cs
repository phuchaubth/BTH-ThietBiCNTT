using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

public class CurrentUser(IHttpContextAccessor http) : ICurrentUser
{
    public string? UserName => http.HttpContext?.User?.Identity?.Name;
}

public static class EnumExtensions
{
    public static string Ten(this Enum value)
    {
        var m = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return m?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }

    public static SelectList ToSelectList<T>(T? selected = null) where T : struct, Enum =>
        new(Enum.GetValues<T>().Select(v => new { Value = v, Text = v.Ten() }), "Value", "Text", selected);
}

/// <summary>Màu badge Bootstrap theo trạng thái.</summary>
public static class Badge
{
    public static string Mau(Enum e) => e switch
    {
        TrangThaiTaiSan.DangSuDung => "success",
        TrangThaiTaiSan.TrongKho => "secondary",
        TrangThaiTaiSan.DangSuaChua => "warning",
        TrangThaiTaiSan.Hong => "danger",
        TrangThaiTaiSan.ChoThanhLy => "dark",
        TrangThaiTaiSan.DaThanhLy => "light text-dark border",
        TrangThaiPhieuSuaChua.Moi => "primary",
        TrangThaiPhieuSuaChua.DaTiepNhan => "info",
        TrangThaiPhieuSuaChua.DangXuLy => "warning",
        TrangThaiPhieuSuaChua.ChoLinhKien or TrangThaiPhieuSuaChua.GuiHang => "secondary",
        TrangThaiPhieuSuaChua.HoanThanh => "success",
        TrangThaiPhieuSuaChua.DaNghiemThu => "success-subtle text-success-emphasis border border-success",
        TrangThaiPhieuSuaChua.DaHuy => "light text-muted border",
        MucDoUuTien.KhanCap => "danger",
        MucDoUuTien.Cao => "warning",
        MucDoUuTien.TrungBinh => "info",
        MucDoUuTien.Thap => "secondary",
        TrangThaiDieuChuyen.ChoDuyet or TrangThaiThanhLy.ChoHoiDong => "warning",
        TrangThaiDieuChuyen.DaDuyet or TrangThaiThanhLy.DaDanhGia or TrangThaiThanhLy.DaPheDuyet => "info",
        TrangThaiDieuChuyen.DaBanGiao or TrangThaiThanhLy.DaThanhLy => "success",
        TrangThaiDieuChuyen.TuChoi or TrangThaiThanhLy.TuChoi => "danger",
        _ => "secondary"
    };

    public static string Html(Enum e) => $"<span class=\"badge bg-{Mau(e)}\">{System.Net.WebUtility.HtmlEncode(e.Ten())}</span>";
}

public static class UserExtensions
{
    public static bool LaCNTT(this ClaimsPrincipal u) => u.IsInRole(VaiTro.QuanTri) || u.IsInRole(VaiTro.CanBoCNTT);
    public static bool LaDuyet(this ClaimsPrincipal u) => u.IsInRole(VaiTro.QuanTri) || u.IsInRole(VaiTro.LanhDao);
}

/// <summary>Chạy nền: mỗi 6 giờ sinh lệnh bảo trì định kỳ cho kế hoạch đến hạn.</summary>
public class BaoTriBackgroundService(IServiceScopeFactory scopes, ILogger<BaoTriBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), ct);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<BaoTriService>()
                    .SinhLenhDenHanAsync(DateOnly.FromDateTime(DateTime.Today), ct);
                if (n > 0) log.LogInformation("Đã sinh {N} lệnh bảo trì định kỳ", n);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Lỗi sinh lệnh bảo trì định kỳ");
            }
            await Task.Delay(TimeSpan.FromHours(6), ct);
        }
    }
}

public record PagedList<T>(List<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

/// <summary>Thêm claim KhoaPhongId, HoTen để giới hạn phạm vi dữ liệu theo khoa.</summary>
public class AppClaimsFactory(Microsoft.AspNetCore.Identity.UserManager<AppUser> um,
    Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole> rm,
    Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions> opt)
    : Microsoft.AspNetCore.Identity.UserClaimsPrincipalFactory<AppUser, Microsoft.AspNetCore.Identity.IdentityRole>(um, rm, opt)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var id = await base.GenerateClaimsAsync(user);
        id.AddClaim(new Claim("HoTen", user.HoTen));
        if (user.KhoaPhongId is { } k) id.AddClaim(new Claim("KhoaPhongId", k.ToString()));
        return id;
    }
}

public static class PhamVi
{
    public static int? KhoaPhongId(this ClaimsPrincipal u) =>
        int.TryParse(u.FindFirst("KhoaPhongId")?.Value, out var k) ? k : null;

    public static string HoTen(this ClaimsPrincipal u) => u.FindFirst("HoTen")?.Value ?? u.Identity?.Name ?? "";

    /// <summary>Người dùng chỉ được xem dữ liệu khoa mình (trừ CNTT, Lãnh đạo).</summary>
    public static bool XemToanVien(this ClaimsPrincipal u) => u.LaCNTT() || u.IsInRole(VaiTro.LanhDao);
}
