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
    public DbSet<MauKiemTra> MauKiemTras => Set<MauKiemTra>();
    public DbSet<MucKiemTra> MucKiemTras => Set<MucKiemTra>();
    public DbSet<LanKiemTra> LanKiemTras => Set<LanKiemTra>();
    public DbSet<KetQuaKiemTra> KetQuaKiemTras => Set<KetQuaKiemTra>();
    public DbSet<TepDinhKem> TepDinhKems => Set<TepDinhKem>();
    public DbSet<HopDong> HopDongs => Set<HopDong>();
    public DbSet<HopDongTaiSan> HopDongTaiSans => Set<HopDongTaiSan>();
    public DbSet<BanQuyenPhanMem> BanQuyenPhanMems => Set<BanQuyenPhanMem>();
    public DbSet<CapPhatBanQuyen> CapPhatBanQuyens => Set<CapPhatBanQuyen>();
    public DbSet<TinNhanCho> TinNhanChos => Set<TinNhanCho>();
    public DbSet<ChiTietKiemKe> ChiTietKiemKes => Set<ChiTietKiemKe>();
    public DbSet<NgayNghi> NgayNghis => Set<NgayNghi>();
    // GĐ3.1 – Hạ tầng CNTT theo JCI 8
    public DbSet<HeThongThongTin> HeThongThongTins => Set<HeThongThongTin>();
    public DbSet<HeThongKhoaPhong> HeThongKhoaPhongs => Set<HeThongKhoaPhong>();
    public DbSet<PhuThuocHeThong> PhuThuocHeThongs => Set<PhuThuocHeThong>();
    public DbSet<KeHoachDowntime> KeHoachDowntimes => Set<KeHoachDowntime>();
    public DbSet<SuKienDowntime> SuKienDowntimes => Set<SuKienDowntime>();
    public DbSet<SuKienDowntimeHeThong> SuKienDowntimeHeThongs => Set<SuKienDowntimeHeThong>();
    public DbSet<SuKienDowntimeKhoa> SuKienDowntimeKhoas => Set<SuKienDowntimeKhoa>();
    public DbSet<NhatKyDowntime> NhatKyDowntimes => Set<NhatKyDowntime>();
    public DbSet<DotDienTap> DotDienTaps => Set<DotDienTap>();
    public DbSet<TacVuSaoLuu> TacVuSaoLuus => Set<TacVuSaoLuu>();
    public DbSet<KetQuaSaoLuuLan> KetQuaSaoLuuLans => Set<KetQuaSaoLuuLan>();
    public DbSet<LanThuPhucHoi> LanThuPhucHois => Set<LanThuPhucHoi>();
    public DbSet<HanhDongKhacPhuc> HanhDongKhacPhucs => Set<HanhDongKhacPhuc>();
    // GĐ3.2
    public DbSet<TaiLieuQuyDinh> TaiLieuQuyDinhs => Set<TaiLieuQuyDinh>();
    public DbSet<NguoiGiamSatCntt> NguoiGiamSatCntts => Set<NguoiGiamSatCntt>();
    public DbSet<KenhTruyenDuLieu> KenhTruyenDuLieus => Set<KenhTruyenDuLieu>();
    public DbSet<BuocVongDoi> BuocVongDois => Set<BuocVongDoi>();
    public DbSet<SuCoAnNinh> SuCoAnNinhs => Set<SuCoAnNinh>();
    public DbSet<BuocUngPho> BuocUngPhos => Set<BuocUngPho>();
    public DbSet<RuiRo> RuiRos => Set<RuiRo>();
    public DbSet<LichSuDiemRuiRo> LichSuDiemRuiRos => Set<LichSuDiemRuiRo>();
    public DbSet<DotDanhGiaRuiRo> DotDanhGiaRuiRos => Set<DotDanhGiaRuiRo>();
    public DbSet<BuoiDaoTao> BuoiDaoTaos => Set<BuoiDaoTao>();
    public DbSet<ThamDuDaoTao> ThamDuDaoTaos => Set<ThamDuDaoTao>();
    public DbSet<TieuChuanJci> TieuChuanJcis => Set<TieuChuanJci>();
    public DbSet<DanhGiaTieuChuanJci> DanhGiaTieuChuanJcis => Set<DanhGiaTieuChuanJci>();
    public DbSet<BaoCaoLanhDao> BaoCaoLanhDaos => Set<BaoCaoLanhDao>();
    public DbSet<PhieuThayDoi> PhieuThayDois => Set<PhieuThayDoi>();
    public DbSet<ThayDoiChuanMau> ThayDoiChuanMaus => Set<ThayDoiChuanMau>();
    public DbSet<DuAnXayDung> DuAnXayDungs => Set<DuAnXayDung>();
    public DbSet<DanhGiaSauThayDoi> DanhGiaSauThayDois => Set<DanhGiaSauThayDoi>();

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
        b.Entity<MauKiemTra>().HasMany(x => x.Mucs).WithOne(x => x.MauKiemTra).HasForeignKey(x => x.MauKiemTraId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MucKiemTra>(e => { e.Property(x => x.NguongMin).HasPrecision(18, 3); e.Property(x => x.NguongMax).HasPrecision(18, 3); });
        b.Entity<LanKiemTra>(e =>
        {
            e.HasMany(x => x.KetQuas).WithOne(x => x.LanKiemTra).HasForeignKey(x => x.LanKiemTraId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.MauKiemTraId, x.TaiSanId, x.ThoiGian });
        });
        b.Entity<KetQuaKiemTra>().Property(x => x.GiaTriSo).HasPrecision(18, 3);
        b.Entity<TepDinhKem>().HasIndex(x => new { x.DoiTuong, x.DoiTuongId });
        b.Entity<HopDong>(e => { e.HasIndex(x => x.SoHopDong); e.HasIndex(x => x.HieuLucDen); });
        b.Entity<HopDongTaiSan>(e =>
        {
            e.HasKey(x => new { x.HopDongId, x.TaiSanId });
            e.HasOne(x => x.HopDong).WithMany(x => x.TaiSans).HasForeignKey(x => x.HopDongId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TaiSan).WithMany().HasForeignKey(x => x.TaiSanId);
        });
        b.Entity<BanQuyenPhanMem>().HasMany(x => x.CapPhats).WithOne(x => x.BanQuyenPhanMem).HasForeignKey(x => x.BanQuyenPhanMemId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<CapPhatBanQuyen>().HasIndex(x => new { x.BanQuyenPhanMemId, x.TaiSanId }).IsUnique();
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
        // GĐ3.1 – Hạ tầng CNTT theo JCI 8
        b.Entity<HeThongThongTin>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique();
            e.HasOne(x => x.ChuSoHuuKhoaPhong).WithMany().HasForeignKey(x => x.ChuSoHuuKhoaPhongId);
            e.HasMany(x => x.KhoaPhongs).WithOne(x => x.HeThong).HasForeignKey(x => x.HeThongId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PhuThuocs).WithOne(x => x.HeThong).HasForeignKey(x => x.HeThongId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<HeThongKhoaPhong>().HasKey(x => new { x.HeThongId, x.KhoaPhongId });
        b.Entity<PhuThuocHeThong>(e =>
        {
            e.HasOne(x => x.HeThongPhuThuoc).WithMany().HasForeignKey(x => x.HeThongPhuThuocId);
            e.HasIndex(x => x.TaiSanId);
            e.HasIndex(x => x.HeThongPhuThuocId);
        });
        b.Entity<KeHoachDowntime>().HasIndex(x => new { x.HeThongId, x.PhienBan }).IsUnique();
        b.Entity<SuKienDowntime>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique();
            e.HasIndex(x => new { x.TrangThai, x.BatDau });
            e.HasMany(x => x.HeThongs).WithOne(x => x.SuKienDowntime).HasForeignKey(x => x.SuKienDowntimeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Khoas).WithOne(x => x.SuKienDowntime).HasForeignKey(x => x.SuKienDowntimeId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.NhatKys).WithOne().HasForeignKey(x => x.SuKienDowntimeId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<SuKienDowntimeHeThong>().HasKey(x => new { x.SuKienDowntimeId, x.HeThongId });
        b.Entity<SuKienDowntimeKhoa>().HasKey(x => new { x.SuKienDowntimeId, x.KhoaPhongId });
        b.Entity<DotDienTap>().HasIndex(x => new { x.HeThongId, x.DaChot });
        b.Entity<TacVuSaoLuu>().HasMany(x => x.KetQuas).WithOne(x => x.TacVuSaoLuu).HasForeignKey(x => x.TacVuSaoLuuId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<KetQuaSaoLuuLan>(e => { e.HasIndex(x => new { x.TacVuSaoLuuId, x.ThoiDiem }); e.Property(x => x.DungLuongGb).HasPrecision(18, 2); });
        b.Entity<LanThuPhucHoi>().HasIndex(x => new { x.HeThongId, x.DaChot });
        b.Entity<HanhDongKhacPhuc>(e => { e.HasIndex(x => x.Ma).IsUnique(); e.HasIndex(x => new { x.TrangThai, x.Han }); e.HasIndex(x => new { x.Nguon, x.NguonId }); });
        // GĐ3.2
        b.Entity<TaiLieuQuyDinh>(e => { e.HasIndex(x => x.Ma).IsUnique(); e.HasIndex(x => new { x.Loai, x.TrangThai }); });
        b.Entity<BuocVongDoi>().HasIndex(x => new { x.HeThongId, x.Buoc });
        b.Entity<SuCoAnNinh>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique(); e.HasIndex(x => new { x.TrangThai, x.MucDo });
            e.HasMany(x => x.Buocs).WithOne().HasForeignKey(x => x.SuCoAnNinhId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<RuiRo>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique(); e.HasIndex(x => new { x.TrangThai, x.Nhom });
            e.HasMany(x => x.LichSus).WithOne().HasForeignKey(x => x.RuiRoId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<DotDanhGiaRuiRo>().HasIndex(x => x.Nam);
        b.Entity<BuoiDaoTao>(e => { e.HasIndex(x => x.Ngay); e.HasMany(x => x.ThamDus).WithOne(x => x.BuoiDaoTao).HasForeignKey(x => x.BuoiDaoTaoId).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<TieuChuanJci>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<BaoCaoLanhDao>().HasIndex(x => x.Ky);
        b.Entity<PhieuThayDoi>(e =>
        {
            e.HasIndex(x => x.Ma).IsUnique(); e.HasIndex(x => x.TrangThai);
            e.HasMany(x => x.HeThongs).WithOne(x => x.PhieuThayDoi).HasForeignKey(x => x.PhieuThayDoiId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.MucPcras).WithOne().HasForeignKey(x => x.PhieuThayDoiId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Checklist).WithOne().HasForeignKey(x => x.PhieuThayDoiId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.DanhGiaSau).WithOne().HasForeignKey<DanhGiaSauThayDoi>(x => x.PhieuThayDoiId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<PhieuThayDoiHeThong>().HasKey(x => new { x.PhieuThayDoiId, x.HeThongId });
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
