using System.Text.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser? currentUser = null)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<KhoaPhong> KhoaPhongs => Set<KhoaPhong>();
    public DbSet<ViTri> ViTris => Set<ViTri>();
    public DbSet<LoaiThietBi> LoaiThietBis => Set<LoaiThietBi>();
    public DbSet<NhaCungCap> NhaCungCaps => Set<NhaCungCap>();
    public DbSet<VatTu> VatTus => Set<VatTu>();
    public DbSet<TaiSan> TaiSans => Set<TaiSan>();
    public DbSet<CauHinh> CauHinhs => Set<CauHinh>();
    public DbSet<LichSuTaiSan> LichSuTaiSans => Set<LichSuTaiSan>();
    public DbSet<PhieuSuaChua> PhieuSuaChuas => Set<PhieuSuaChua>();
    public DbSet<ChiTietVatTuSuaChua> ChiTietVatTuSuaChuas => Set<ChiTietVatTuSuaChua>();
    public DbSet<BinhLuanPhieu> BinhLuanPhieus => Set<BinhLuanPhieu>();
    public DbSet<KeHoachBaoTri> KeHoachBaoTris => Set<KeHoachBaoTri>();
    public DbSet<PhieuDieuChuyen> PhieuDieuChuyens => Set<PhieuDieuChuyen>();
    public DbSet<ChiTietDieuChuyen> ChiTietDieuChuyens => Set<ChiTietDieuChuyen>();
    public DbSet<BuocDuyetDieuChuyen> BuocDuyetDieuChuyens => Set<BuocDuyetDieuChuyen>();
    public DbSet<DeXuatThanhLy> DeXuatThanhLys => Set<DeXuatThanhLy>();
    public DbSet<ChiTietThanhLy> ChiTietThanhLys => Set<ChiTietThanhLy>();
    public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();
    public DbSet<ThamSoHeThong> ThamSoHeThongs => Set<ThamSoHeThong>();
    public DbSet<DotKiemKe> DotKiemKes => Set<DotKiemKe>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<TinNhanCho> TinNhanChos => Set<TinNhanCho>();
    public DbSet<ChiTietKiemKe> ChiTietKiemKes => Set<ChiTietKiemKe>();
    public DbSet<NgayNghi> NgayNghis => Set<NgayNghi>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // SQL Server không cho nhiều đường cascade → mặc định Restrict, chỉ cascade cho chi tiết phiếu.
        // (chỉ áp cho bảng nghiệp vụ; giữ nguyên cascade mặc định của các bảng Identity)
        foreach (var fk in b.Model.GetEntityTypes()
                     .Where(e => e.ClrType.Namespace?.StartsWith("ThietBiCNTT.Domain") == true)
                     .SelectMany(e => e.GetForeignKeys()))
            if (!fk.IsOwnership) fk.DeleteBehavior = DeleteBehavior.Restrict;

        foreach (var p in b.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
                     .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            p.SetPrecision(18); // VNĐ + số lượng

        b.Entity<KhoaPhong>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<LoaiThietBi>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<VatTu>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<VatTu>().Property(x => x.TonKho).HasPrecision(18, 2);
        b.Entity<VatTu>().Property(x => x.TonToiThieu).HasPrecision(18, 2);

        b.Entity<ViTri>().HasOne(x => x.Cha).WithMany(x => x.Con).HasForeignKey(x => x.ChaId);

        b.Entity<TaiSan>(e =>
        {
            e.HasIndex(x => x.MaTaiSan).IsUnique();
            e.HasIndex(x => x.SoSerial);
            e.HasIndex(x => new { x.KhoaPhongId, x.TrangThai });
            e.HasOne(x => x.TaiSanCha).WithMany(x => x.TaiSanCon).HasForeignKey(x => x.TaiSanChaId);
            e.HasOne(x => x.CauHinh).WithOne(x => x.TaiSan).HasForeignKey<CauHinh>(x => x.TaiSanId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CauHinh>().HasKey(x => x.TaiSanId);
        b.Entity<CauHinh>().HasIndex(x => x.DiaChiIP);
        b.Entity<LichSuTaiSan>().HasIndex(x => new { x.TaiSanId, x.ThoiGian });

        b.Entity<PhieuSuaChua>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasIndex(x => new { x.TrangThai, x.HanXuLy });
            e.HasMany(x => x.VatTus).WithOne(x => x.PhieuSuaChua).HasForeignKey(x => x.PhieuSuaChuaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.GioCong).HasPrecision(8, 2);
            e.HasMany(x => x.BinhLuans).WithOne(x => x.PhieuSuaChua).HasForeignKey(x => x.PhieuSuaChuaId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ChiTietVatTuSuaChua>().Property(x => x.SoLuong).HasPrecision(18, 2);

        b.Entity<PhieuDieuChuyen>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasOne(x => x.TuKhoaPhong).WithMany().HasForeignKey(x => x.TuKhoaPhongId);
            e.HasOne(x => x.DenKhoaPhong).WithMany().HasForeignKey(x => x.DenKhoaPhongId);
            e.HasMany(x => x.ChiTiets).WithOne(x => x.PhieuDieuChuyen).HasForeignKey(x => x.PhieuDieuChuyenId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.BuocDuyets).WithOne(x => x.PhieuDieuChuyen).HasForeignKey(x => x.PhieuDieuChuyenId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DenViTri).WithMany().HasForeignKey(x => x.DenViTriId);
            e.HasIndex(x => new { x.TrangThai, x.CapHienTai });
        });
        b.Entity<ChiTietDieuChuyen>().HasIndex(x => x.TaiSanId);
        b.Entity<ChiTietThanhLy>().HasIndex(x => x.SerialOCung);
        b.Entity<DeXuatThanhLy>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasMany(x => x.ChiTiets).WithOne(x => x.DeXuatThanhLy).HasForeignKey(x => x.DeXuatThanhLyId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<NhatKyHeThong>().HasIndex(x => x.ThoiGian);
        b.Entity<ThongBao>().HasIndex(x => new { x.NguoiNhan, x.DaDoc, x.ThoiGian });
        b.Entity<TinNhanCho>().HasIndex(x => new { x.TrangThai, x.GuiTu });
        b.Entity<AppUser>().Property(x => x.TelegramChatId).HasMaxLength(50);
        b.Entity<DotKiemKe>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique();
            e.HasMany(x => x.ChiTiets).WithOne(x => x.DotKiemKe).HasForeignKey(x => x.DotKiemKeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ChiTietKiemKe>(e =>
        {
            e.HasOne(x => x.KhoaSoSach).WithMany().HasForeignKey(x => x.KhoaSoSachId);
            e.HasOne(x => x.KhoaThucTe).WithMany().HasForeignKey(x => x.KhoaThucTeId);
            e.HasIndex(x => new { x.DotKiemKeId, x.TaiSanId });
            e.HasIndex(x => new { x.DotKiemKeId, x.KetQua });
        });
        b.Entity<ThamSoHeThong>().Property(x => x.ToiThieu).HasPrecision(18, 2);
        b.Entity<ThamSoHeThong>().Property(x => x.ToiDa).HasPrecision(18, 2);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var user = currentUser?.UserName ?? "system";
        var logs = new List<(NhatKyHeThong Log, EntityEntry Entry)>();

        foreach (var entry in ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is BaseEntity be)
            {
                if (entry.State == EntityState.Added) { be.NgayTao = now; be.NguoiTao ??= user; }
                else if (entry.State == EntityState.Modified) { be.NgayCapNhat = now; be.NguoiCapNhat = user; }
            }
            if (entry.Entity is BaseEntity && entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
                logs.Add((TaoNhatKy(entry, now, user), entry));
        }

        var result = await base.SaveChangesAsync(ct);
        if (logs.Count > 0)
        {
            // Id của bản ghi mới chỉ có sau khi lưu.
            foreach (var (l, e) in logs) l.DoiTuongId ??= e.Property("Id").CurrentValue?.ToString();
            NhatKyHeThongs.AddRange(logs.Select(x => x.Log));
            await base.SaveChangesAsync(ct);
        }
        return result;
    }

    private static NhatKyHeThong TaoNhatKy(EntityEntry e, DateTime now, string user)
    {
        var changes = e.State == EntityState.Modified
            ? e.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)
                                       && p.Metadata.Name is not (nameof(BaseEntity.NgayCapNhat) or nameof(BaseEntity.NguoiCapNhat)))
                          .ToDictionary(p => p.Metadata.Name, p => new { Cu = p.OriginalValue, Moi = p.CurrentValue })
            : null;
        return new NhatKyHeThong
        {
            ThoiGian = now,
            NguoiDung = user,
            HanhDong = e.State switch { EntityState.Added => "Them", EntityState.Deleted => "Xoa", _ => "Sua" },
            DoiTuong = e.Metadata.ClrType.Name,
            DoiTuongId = e.State == EntityState.Added ? null : e.Property("Id").CurrentValue?.ToString(),
            ChiTiet = changes is null ? null : JsonSerializer.Serialize(changes)
        };
    }
}
