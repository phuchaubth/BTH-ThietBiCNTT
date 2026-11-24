using Microsoft.AspNetCore.Identity;
using ThietBiCNTT.Infrastructure;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Web.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using ThietBiCNTT.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);
// Chạy từ mã nguồn (dotnet run) ở môi trường khác Development (VD: Demo): dùng file tĩnh từ thư mục build.
// Bản publish (IIS/Production) không cần dòng này.
if (!builder.Environment.IsProduction())
    builder.WebHost.UseStaticWebAssets();

// Giữ nguyên chữ tiếng Việt có dấu trong HTML (mặc định Razor mã hoá thành &#x...;)
builder.Services.Configure<Microsoft.Extensions.WebEncoders.WebEncoderOptions>(o =>
    o.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(System.Text.Unicode.UnicodeRanges.All));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IUserClaimsPrincipalFactory<AppUser>, AppClaimsFactory>();
// Khoá tài khoản / đổi quyền / đặt lại mật khẩu → phiên đang mở bị đăng xuất trong vòng 1 phút.
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));
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
    // Mặc định mọi trang giao diện đều yêu cầu đăng nhập; API /api/v1 xác thực bằng khoá API.
    o.Conventions.Add(new DangNhapMacDinhConvention());
    // Đọc số trong form/query theo InvariantCulture (dấu chấm thập phân) dù hiển thị theo vi-VN.
    var i = o.ValueProviderFactories.IndexOf(o.ValueProviderFactories.OfType<Microsoft.AspNetCore.Mvc.ModelBinding.FormValueProviderFactory>().Single());
    o.ValueProviderFactories[i] = new InvariantValueProviderFactory();
});
// Hiển thị số/ngày theo tiếng Việt: 14.200.000 · 21/11/2026
var vi = new System.Globalization.CultureInfo("vi-VN");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = vi;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = vi;
builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, ApiKeyHandler>(ApiKeyHandler.Scheme, null);
builder.Services.AddAuthorization(ApiKeyHandler.DangKyChinhSach);
// Tài liệu OpenAPI cho API mở (chỉ các endpoint /api/v1).
builder.Services.AddOpenApi("v1", o =>
{
    o.ShouldInclude = d => d.RelativePath?.StartsWith("api/v1") == true;
    o.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info = new() { Title = "API Quản lý TTB CNTT", Version = "v1",
            Description = "Xác thực: header X-Api-Key: tbk_… (Quản trị → Tích hợp → Khoá API). Mỗi khoá có phạm vi quyền và giới hạn IP; mọi lời gọi được ghi nhật ký." };
        doc.Components ??= new();
        doc.Components.SecuritySchemes ??= new Dictionary<string, Microsoft.OpenApi.IOpenApiSecurityScheme>();
        doc.Components.SecuritySchemes["ApiKey"] = new Microsoft.OpenApi.OpenApiSecurityScheme
        { Type = Microsoft.OpenApi.SecuritySchemeType.ApiKey, In = Microsoft.OpenApi.ParameterLocation.Header, Name = "X-Api-Key" };
        doc.Security = [new Microsoft.OpenApi.OpenApiSecurityRequirement { [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("ApiKey", doc)] = [] }];
        return Task.CompletedTask;
    });
});
builder.Services.AddHostedService<BaoTriBackgroundService>();
builder.Services.AddHostedService<ThongBaoBackgroundService>();

// Lưu khoá mã hoá cookie ra thư mục cố định: IIS recycle app pool không làm người dùng bị đăng xuất.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
        .SetApplicationName("ThietBiCNTT");

CanhBaoService.TongHopBoSung.Add(KiemTraTongHop.DongAsync);
CanhBaoService.TongHopBoSung.Add(HopDongTongHop.DongAsync);
CanhBaoService.TongHopBoSung.Add(MuaSamTongHop.DongAsync);
CanhBaoService.TongHopBoSung.Add(NganSachTongHop.DongAsync);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var db = sp.GetRequiredService<AppDbContext>();
    if (db.IsSqlite()) await db.Database.EnsureCreatedAsync();
    else if (app.Configuration.GetValue("Database:AutoMigrate", true)) await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db, sp.GetRequiredService<UserManager<AppUser>>(),
        sp.GetRequiredService<RoleManager<IdentityRole>>(),
        duLieuMau: app.Configuration.GetValue("SeedSampleData", false),
        matKhauAdmin: app.Configuration["Seed:AdminPassword"]);
    // Địa chỉ truy cập (link trong mã QR): lấy từ cấu hình nếu tham số chưa được đặt trên giao diện.
    if (app.Configuration["App:DiaChiTruyCap"] is { Length: > 0 } diaChi
        && await db.ThamSoHeThongs.FindAsync(TS.DiaChiTruyCap) is { GiaTri: "" } ts)
    {
        ts.GiaTri = diaChi.TrimEnd('/');
        await db.SaveChangesAsync();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseMiddleware<ApiMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapOpenApi("/api/openapi/{documentName}.json").RequireAuthorization(p => p.RequireRole(VaiTro.QuanTri, VaiTro.CanBoCNTT));

// Link in trên tem QR: /qr/{maTaiSan} → mở hồ sơ thiết bị.
app.MapControllerRoute("qr", "qr/{ma}", new { controller = "TaiSan", action = "Qr" });
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();

app.Run();
