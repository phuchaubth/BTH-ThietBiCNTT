using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký DbContext + Identity + service nghiệp vụ.
    /// "DatabaseProvider": "SqlServer" (mặc định, dùng migrations) hoặc "Sqlite" (chạy thử nhanh, không cần SQL Server).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["DatabaseProvider"] ?? "SqlServer";
        var cs = config.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Thiếu ConnectionStrings:DefaultConnection");
        services.AddDbContext<AppDbContext>(o =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase)) o.UseSqlite(cs);
            else o.UseSqlServer(cs, x => x.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
        });

        services.AddIdentity<AppUser, IdentityRole>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                o.User.RequireUniqueEmail = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddMemoryCache();
        services.AddScoped<ThamSoService>();
        services.AddScoped<SoPhieuService>();
        services.AddScoped<LichSuService>();
        services.AddScoped<BaoTriService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<NghiemThuTuDongService>();
        services.AddScoped<ExcelTaiSanService>();
        services.AddScoped<ThongBaoService>();
        services.AddScoped<TepService>();
        services.AddScoped<KiemTraService>();
        services.AddScoped<BaoCaoService>();
        services.AddScoped<KhoService>();
        services.AddScoped<MuaSamService>();
        services.AddScoped<NganSachService>();
        services.AddScoped<TichHopService>();
        services.AddScoped<WebhookSender>();
        services.AddHttpClient("webhook", c => c.Timeout = TimeSpan.FromSeconds(10));
        services.AddScoped<CanhBaoService>();
        services.AddScoped<HangDoiGuiService>();
        services.AddScoped<IKenhGui, EmailSender>();
        services.AddScoped<IKenhGui, TelegramSender>();
        services.AddHttpClient("telegram", c => c.Timeout = TimeSpan.FromSeconds(20));
        return services;
    }

    public static bool IsSqlite(this AppDbContext db) => db.Database.ProviderName?.Contains("Sqlite") == true;
}
