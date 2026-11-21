using Microsoft.AspNetCore.Identity;
using ThietBiCNTT.Infrastructure;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using ThietBiCNTT.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<AppUser>, AppClaimsFactory>();
builder.Services.ConfigureApplicationCookie(o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/AccessDenied";
    // QĐ-05: thời gian phiên theo vai trò, đọc từ tham số hệ thống (BaoMat.*)
    o.ExpireTimeSpan = TimeSpan.FromHours(24);
    o.SlidingExpiration = false;
    var kiemTraGoc = o.Events.OnValidatePrincipal;
    o.Events.OnValidatePrincipal = async ctx =>
    {
        await kiemTraGoc(ctx);
        if (ctx.Principal?.Identity?.IsAuthenticated != true) return;
        var ts = ctx.HttpContext.RequestServices.GetRequiredService<ThamSoService>();
        var gioiHan = PhienDangNhap.GioiHan(ctx.Principal, ts);
        var now = DateTimeOffset.UtcNow;
        var hoatDongCuoi = ctx.Properties.Items.TryGetValue("hd", out var s) && DateTimeOffset.TryParse(s, out var d) ? d : ctx.Properties.IssuedUtc ?? now;
        if (now - hoatDongCuoi > gioiHan)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        if (now - hoatDongCuoi > TimeSpan.FromMinutes(1))
        {
            ctx.Properties.Items["hd"] = now.ToString("o");
            ctx.Properties.ExpiresUtc = now + gioiHan;
            ctx.ShouldRenew = true;
        }
    };
});
builder.Services.AddControllersWithViews(o =>
{
    // Mặc định mọi trang đều yêu cầu đăng nhập.
    o.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter());
});
builder.Services.AddHostedService<BaoTriBackgroundService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    if (db.IsSqlite()) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, sp.GetRequiredService<UserManager<AppUser>>(),
        sp.GetRequiredService<RoleManager<IdentityRole>>(),
        duLieuMau: app.Configuration.GetValue("SeedSampleData", false));
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();

// Link in trên tem QR: /qr/{maTaiSan} → mở hồ sơ thiết bị.
app.MapControllerRoute("qr", "qr/{ma}", new { controller = "TaiSan", action = "Qr" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();
