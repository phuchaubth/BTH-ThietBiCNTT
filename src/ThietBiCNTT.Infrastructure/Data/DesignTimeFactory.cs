using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ThietBiCNTT.Infrastructure.Data;

/// <summary>Dùng cho lệnh `dotnet ef` (tạo migration SQL Server) — không cần kết nối CSDL thật.</summary>
public class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=ThietBiCNTT;Trusted_Connection=True;TrustServerCertificate=True",
                x => x.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options);
}
