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
        Domain.Entities.TrangThaiDeXuat.ChoDuyet => "warning text-dark",
        Domain.Entities.TrangThaiDeXuat.DaDuyet or Domain.Entities.TrangThaiDeXuat.DatMotPhan => "info",
        Domain.Entities.TrangThaiDeXuat.DaDatDu => "success",
        Domain.Entities.TrangThaiDeXuat.TuChoi => "danger",
        Domain.Entities.TrangThaiDeXuat.DaHuy or Domain.Entities.TrangThaiDonHang.DaHuy => "light text-muted border",
        Domain.Entities.TrangThaiDonHang.DaDatHang => "primary",
        Domain.Entities.TrangThaiDonHang.NhanMotPhan => "info",
        Domain.Entities.TrangThaiDonHang.DaNhanDu => "success",
        Domain.Entities.TrangThaiNganSach.DuThao => "warning text-dark",
        Domain.Entities.TrangThaiNganSach.DaDuyet => "success",
        Domain.Entities.TrangThaiNganSach.DaKhoa => "dark",
        _ => "secondary"
    };

    public static string Html(Enum e) => $"<span class=\"badge bg-{Mau(e)}\">{System.Net.WebUtility.HtmlEncode(e.Ten())}</span>";
}

public static class UserExtensions
{
    public static bool LaCNTT(this ClaimsPrincipal u) => u.IsInRole(VaiTro.QuanTri) || u.IsInRole(VaiTro.CanBoCNTT);
    public static bool LaDuyet(this ClaimsPrincipal u) => u.IsInRole(VaiTro.QuanTri) || u.IsInRole(VaiTro.LanhDao);
}

/// <summary>Chạy nền: theo chu kỳ tham số BaoTri.ChuKyQuetGio, sinh lệnh bảo trì định kỳ cho kế hoạch đến hạn.</summary>
public class BaoTriBackgroundService(IServiceScopeFactory scopes, ILogger<BaoTriBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(20), ct);
        var chuKy = 6;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                var n = await scope.ServiceProvider.GetRequiredService<BaoTriService>()
                    .SinhLenhDenHanAsync(DateOnly.FromDateTime(DateTime.Today), ct);
                if (n > 0) log.LogInformation("Đã sinh {N} lệnh bảo trì định kỳ", n);
                var nt = await scope.ServiceProvider.GetRequiredService<NghiemThuTuDongService>().ChayAsync(DateTime.Now, ct);
                if (nt > 0) log.LogInformation("Tự động nghiệm thu {N} phiếu", nt);
                chuKy = scope.ServiceProvider.GetRequiredService<ThamSoService>().SoNguyen(TS.BaoTriChuKyQuetGio);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Lỗi sinh lệnh bảo trì định kỳ");
            }
            await Task.Delay(TimeSpan.FromHours(Math.Clamp(chuKy, 1, 24)), ct);
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
        if (user.TwoFactorEnabled) id.AddClaim(new Claim("2fa", "1"));
        if (user.XacThucAD) id.AddClaim(new Claim("ad", "1"));
        return id;
    }
}

/// <summary>
/// Tham số BaoMat.BatBuoc2FAQuanTri: tài khoản Quản trị / Cán bộ CNTT chưa bật xác thực 2 lớp bị chuyển tới trang cài đặt
/// trước khi dùng các chức năng khác.
/// </summary>
public class BatBuoc2FAFilter(ThamSoService ts) : Microsoft.AspNetCore.Mvc.Filters.IActionFilter
{
    public void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext c)
    {
        var u = c.HttpContext.User;
        if (u.Identity?.IsAuthenticated != true || u.Identity.AuthenticationType != Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme) return;
        if (u.HasClaim("2fa", "1") || !u.LaCNTT() || !ts.BatTat(TS.BatBuoc2FA)) return;
        var ctl = c.RouteData.Values["controller"]?.ToString();
        var act = c.RouteData.Values["action"]?.ToString() ?? "";
        if (ctl == "Account" || (ctl == "TaiKhoan" && act.Contains("XacThuc2Lop"))) return;
        c.Result = new Microsoft.AspNetCore.Mvc.RedirectResult("/TaiKhoan/XacThuc2Lop?batBuoc=true");
    }
    public void OnActionExecuted(Microsoft.AspNetCore.Mvc.Filters.ActionExecutedContext c) { }
}

public static class PhamVi
{
    public static int? KhoaPhongId(this ClaimsPrincipal u) =>
        int.TryParse(u.FindFirst("KhoaPhongId")?.Value, out var k) ? k : null;

    public static string HoTen(this ClaimsPrincipal u) => u.FindFirst("HoTen")?.Value ?? u.Identity?.Name ?? "";

    /// <summary>Người dùng chỉ được xem dữ liệu khoa mình (trừ CNTT, Lãnh đạo).</summary>
    public static bool XemToanVien(this ClaimsPrincipal u) => u.LaCNTT() || u.IsInRole(VaiTro.LanhDao);
}

/// <summary>QĐ-05: Nhân viên/Trưởng khoa tự đăng xuất sau N phút không thao tác; CNTT/KTV/Lãnh đạo giữ phiên M giờ.</summary>
public static class PhienDangNhap
{
    public static bool LaTaiKhoanKhoa(ClaimsPrincipal u) => !(u.LaCNTT() || u.IsInRole(VaiTro.LanhDao));
    public static bool LaTaiKhoanKhoa(IEnumerable<string> roles) =>
        !roles.Any(r => r is VaiTro.QuanTri or VaiTro.CanBoCNTT or VaiTro.LanhDao);

    public static TimeSpan GioiHan(ClaimsPrincipal u, ThamSoService ts) => LaTaiKhoanKhoa(u)
        ? TimeSpan.FromMinutes(ts.SoNguyen(TS.PhienKhoaPhut))
        : TimeSpan.FromHours(ts.SoNguyen(TS.PhienCNTTGio));
}

/// <summary>
/// Hiển thị theo văn hoá vi-VN (14.200.000 đ, dd/MM/yyyy) nhưng đọc dữ liệu form/query theo InvariantCulture,
/// vì input type=number của trình duyệt luôn gửi dấu chấm thập phân ("0.5").
/// </summary>
public class InvariantValueProviderFactory : Microsoft.AspNetCore.Mvc.ModelBinding.IValueProviderFactory
{
    public async Task CreateValueProviderAsync(Microsoft.AspNetCore.Mvc.ModelBinding.ValueProviderFactoryContext context)
    {
        var req = context.ActionContext.HttpContext.Request;
        if (req.HasFormContentType)
        {
            var form = await req.ReadFormAsync();
            context.ValueProviders.Add(new Microsoft.AspNetCore.Mvc.ModelBinding.FormValueProvider(
                Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Form, form, System.Globalization.CultureInfo.InvariantCulture));
        }
    }
}

/// <summary>Chạy nền: gửi hàng đợi email/Telegram mỗi 30 giây; cảnh báo SLA, leo thang, tổng hợp ngày mỗi 5 phút.</summary>
public class ThongBaoBackgroundService(IServiceScopeFactory scopes, ILogger<ThongBaoBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), ct);
        var lanCanhBao = DateTime.MinValue;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope();
                if (DateTime.Now - lanCanhBao >= TimeSpan.FromMinutes(5))
                {
                    await scope.ServiceProvider.GetRequiredService<CanhBaoService>().ChayAsync(DateTime.Now, ct);
                    await scope.ServiceProvider.GetRequiredService<BaoCaoDinhKyService>().ChayAsync(DateTime.Now, ct);
                    await scope.ServiceProvider.GetRequiredService<TichHopService>().DonDepAsync(
                        scope.ServiceProvider.GetRequiredService<ThamSoService>().SoNguyen(TS.ApiLuuNhatKyNgay), ct);
                    lanCanhBao = DateTime.Now;
                }
                await scope.ServiceProvider.GetRequiredService<HangDoiGuiService>().XuLyAsync(ct);
                await scope.ServiceProvider.GetRequiredService<WebhookSender>().XuLyAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) { log.LogError(ex, "Lỗi job thông báo"); }
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
        }
    }
}

/// <summary>Chuông thông báo trên thanh trên cùng.</summary>
public class ChuongThongBaoViewComponent(AppDbContext db) : Microsoft.AspNetCore.Mvc.ViewComponent
{
    public async Task<Microsoft.AspNetCore.Mvc.IViewComponentResult> InvokeAsync()
    {
        var u = UserClaimsPrincipal.Identity?.Name;
        var q = db.ThongBaos.Where(t => t.NguoiNhan == u);
        ViewBag.ChuaDoc = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(q.Where(t => !t.DaDoc));
        return View(await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(q.OrderByDescending(t => t.Id).Take(6)));
    }
}
