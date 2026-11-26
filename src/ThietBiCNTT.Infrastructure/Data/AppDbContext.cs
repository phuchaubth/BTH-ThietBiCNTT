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
    public DbSet<KeHoachVatTu> KeHoachVatTus => Set<KeHoachVatTu>();
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
    public DbSet<Kho> Khos => Set<Kho>();
    public DbSet<TonKhoVatTu> TonKhoVatTus => Set<TonKhoVatTu>();
    public DbSet<PhieuKho> PhieuKhos => Set<PhieuKho>();
    public DbSet<ChiTietPhieuKho> ChiTietPhieuKhos => Set<ChiTietPhieuKho>();
    public DbSet<TheKho> TheKhos => Set<TheKho>();
    public DbSet<DeXuatMuaSam> DeXuatMuaSams => Set<DeXuatMuaSam>();
    public DbSet<DongDeXuat> DongDeXuats => Set<DongDeXuat>();
    public DbSet<BuocDuyetMuaSam> BuocDuyetMuaSams => Set<BuocDuyetMuaSam>();
    public DbSet<DonMuaHang> DonMuaHangs => Set<DonMuaHang>();
    public DbSet<DongDonHang> DongDonHangs => Set<DongDonHang>();
    public DbSet<PhieuNhanHang> PhieuNhanHangs => Set<PhieuNhanHang>();
    public DbSet<TruongTuyChinh> TruongTuyChinhs => Set<TruongTuyChinh>();
    public DbSet<GiaTriTuyChinh> GiaTriTuyChinhs => Set<GiaTriTuyChinh>();
    public DbSet<BaoCaoMau> BaoCaoMaus => Set<BaoCaoMau>();
    public DbSet<LichGuiBaoCao> LichGuiBaoCaos => Set<LichGuiBaoCao>();
    public DbSet<SoDoMatBang> SoDoMatBangs => Set<SoDoMatBang>();
    public DbSet<DiemSoDo> DiemSoDos => Set<DiemSoDo>();
    public DbSet<MayTramAgent> MayTramAgents => Set<MayTramAgent>();
    public DbSet<KhoaApi> KhoaApis => Set<KhoaApi>();
    public DbSet<NhatKyApi> NhatKyApis => Set<NhatKyApi>();
    public DbSet<Webhook> Webhooks => Set<Webhook>();
    public DbSet<WebhookGui> WebhookGuis => Set<WebhookGui>();
    public DbSet<KhoanMucNganSach> KhoanMucNganSachs => Set<KhoanMucNganSach>();
    public DbSet<DieuChinhNganSach> DieuChinhNganSachs => Set<DieuChinhNganSach>();
    public DbSet<ChiNganSach> ChiNganSachs => Set<ChiNganSach>();
    public DbSet<HopDongTaiSan> HopDongTaiSans => Set<HopDongTaiSan>();
    public DbSet<BanQuyenPhanMem> BanQuyenPhanMems => Set<BanQuyenPhanMem>();
    public DbSet<CapPhatBanQuyen> CapPhatBanQuyens => Set<CapPhatBanQuyen>();
    public DbSet<TinNhanCho> TinNhanChos => Set<TinNhanCho>();
    public DbSet<ChiTietKiemKe> ChiTietKiemKes => Set<ChiTietKiemKe>();
    public DbSet<NgayNghi> NgayNghis => Set<NgayNghi>();
    public DbSet<VanBan> VanBans => Set<VanBan>();
    public DbSet<BuocDuyetVanBan> BuocDuyetVanBans => Set<BuocDuyetVanBan>();
    public DbSet<NhatKyVanBan> NhatKyVanBans => Set<NhatKyVanBan>();
    public DbSet<MauVanBan> MauVanBans => Set<MauVanBan>();

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
        b.Entity<Kho>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<TonKhoVatTu>(e => { e.HasIndex(x => new { x.KhoId, x.VatTuId }).IsUnique(); e.Property(x => x.SoLuong).HasPrecision(18, 2); });
        b.Entity<PhieuKho>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasOne(x => x.Kho).WithMany().HasForeignKey(x => x.KhoId);
            e.HasOne(x => x.KhoDen).WithMany().HasForeignKey(x => x.KhoDenId);
            e.HasMany(x => x.ChiTiets).WithOne(x => x.PhieuKho).HasForeignKey(x => x.PhieuKhoId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ChiTietPhieuKho>(e => { e.Property(x => x.SoLuong).HasPrecision(18, 2); e.Property(x => x.TonSoSach).HasPrecision(18, 2); });
        b.Entity<TheKho>(e =>
        {
            e.HasIndex(x => new { x.KhoId, x.VatTuId, x.ThoiGian });
            e.Property(x => x.Nhap).HasPrecision(18, 2); e.Property(x => x.Xuat).HasPrecision(18, 2); e.Property(x => x.TonSau).HasPrecision(18, 2);
        });
        b.Entity<VatTu>().Property(x => x.TonToiDa).HasPrecision(18, 2);
        b.Entity<DeXuatMuaSam>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasIndex(x => new { x.TrangThai, x.CapHienTai });
            e.HasMany(x => x.Dongs).WithOne(x => x.DeXuatMuaSam).HasForeignKey(x => x.DeXuatMuaSamId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.BuocDuyets).WithOne().HasForeignKey(x => x.DeXuatMuaSamId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<DongDeXuat>(e => { e.Property(x => x.SoLuong).HasPrecision(18, 2); e.Property(x => x.SoLuongDaDat).HasPrecision(18, 2); });
        b.Entity<DonMuaHang>(e =>
        {
            e.HasIndex(x => x.SoPhieu).IsUnique();
            e.HasIndex(x => new { x.TrangThai, x.HanGiao });
            e.Property(x => x.ThueSuat).HasPrecision(5, 2);
            e.HasMany(x => x.Dongs).WithOne(x => x.DonMuaHang).HasForeignKey(x => x.DonMuaHangId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PhieuNhans).WithOne(x => x.DonMuaHang).HasForeignKey(x => x.DonMuaHangId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<KhoanMucNganSach>(e =>
        {
            e.HasIndex(x => new { x.Nam, x.Ma }).IsUnique();
            e.HasMany(x => x.DieuChinhs).WithOne().HasForeignKey(x => x.KhoanMucNganSachId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.KhoanChis).WithOne().HasForeignKey(x => x.KhoanMucNganSachId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<KhoaApi>().HasIndex(x => x.BamKhoa).IsUnique();
        b.Entity<TruongTuyChinh>().HasIndex(x => x.Ma).IsUnique();
        b.Entity<KeHoachBaoTri>(e =>
        {
            e.HasMany(x => x.VatTus).WithOne().HasForeignKey(x => x.KeHoachBaoTriId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.GioCongDuKien).HasPrecision(8, 2);
            e.HasIndex(x => new { x.TrangThai, x.NgayTiepTheo });
        });
        b.Entity<KeHoachVatTu>().Property(x => x.SoLuong).HasPrecision(18, 2);
        b.Entity<VanBan>(e =>
        {
            e.Ignore(x => x.CacBuocLuotNay); e.Ignore(x => x.BuocDangCho); e.Ignore(x => x.NguoiPhoiHopDs);
            e.HasIndex(x => new { x.Nam, x.SoDen }).IsUnique().HasFilter("[SoDen] IS NOT NULL");
            e.HasIndex(x => new { x.Nam, x.SoDi }).IsUnique().HasFilter("[SoDi] IS NOT NULL");
            e.HasIndex(x => new { x.Huong, x.TrangThai });
            e.HasIndex(x => x.NguoiChuTri);
            e.HasOne(x => x.VanBanLienQuan).WithMany().HasForeignKey(x => x.VanBanLienQuanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DeXuatMuaSam).WithMany().HasForeignKey(x => x.DeXuatMuaSamId).OnDelete(DeleteBehavior.SetNull);
            e.HasMany(x => x.BuocDuyets).WithOne().HasForeignKey(x => x.VanBanId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.NhatKys).WithOne().HasForeignKey(x => x.VanBanId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<BuocDuyetVanBan>().HasIndex(x => new { x.NguoiDuyet, x.DongY });
        b.Entity<LanKiemTra>().HasIndex(x => x.PhieuBaoTriId);
        b.Entity<GiaTriTuyChinh>(e =>
        {
            e.HasKey(x => new { x.TaiSanId, x.TruongTuyChinhId });
            e.HasOne(x => x.TaiSan).WithMany(x => x.GiaTriTuyChinhs).HasForeignKey(x => x.TaiSanId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.TruongTuyChinh).WithMany().HasForeignKey(x => x.TruongTuyChinhId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => new { x.TruongTuyChinhId, x.GiaTri });
        });
        b.Entity<SoDoMatBang>().HasMany(x => x.Diems).WithOne(x => x.SoDoMatBang).HasForeignKey(x => x.SoDoMatBangId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<DiemSoDo>(e =>
        {
            e.HasIndex(x => new { x.SoDoMatBangId, x.TaiSanId }).IsUnique(); e.HasIndex(x => x.TaiSanId);
            e.Property(x => x.X).HasPrecision(6, 3); e.Property(x => x.Y).HasPrecision(6, 3);
        });
        b.Entity<MayTramAgent>(e =>
        {
            e.HasIndex(x => x.Hostname); e.HasIndex(x => x.SerialBios); e.HasIndex(x => x.TaiSanId);
            e.Property(x => x.RamGb).HasPrecision(8, 1); e.Property(x => x.OCungTongGb).HasPrecision(10, 1); e.Property(x => x.OCungTrongGb).HasPrecision(10, 1);
        });
        b.Entity<NhatKyApi>().HasIndex(x => x.ThoiGian);
        b.Entity<WebhookGui>(e => { e.HasIndex(x => new { x.TrangThai, x.GuiTu }); e.HasOne(x => x.Webhook).WithMany().HasForeignKey(x => x.WebhookId).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<ChiNganSach>().HasIndex(x => new { x.KhoanMucNganSachId, x.Ngay });
        b.Entity<DongDonHang>(e => { e.Property(x => x.SoLuong).HasPrecision(18, 2); e.Property(x => x.SoLuongDaNhan).HasPrecision(18, 2); });
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
        b.Entity<ThamSoHeThong>().Property(x => x.ToiThieu).HasPrecision(18, 2);
        b.Entity<ThamSoHeThong>().Property(x => x.ToiDa).HasPrecision(18, 2);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var user = currentUser?.UserName ?? "system";
        var logs = new List<(NhatKyHeThong Log, EntityEntry Entry)>();
        var suKien = ThuSuKienWebhook();

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
        suKien.AddRange(_suKienCho); _suKienCho.Clear();
        if (logs.Count > 0 || suKien.Count > 0)
        {
            // Id của bản ghi mới chỉ có sau khi lưu.
            foreach (var (l, e) in logs) l.DoiTuongId ??= e.Property("Id").CurrentValue?.ToString();
            NhatKyHeThongs.AddRange(logs.Select(x => x.Log));
            if (suKien.Count > 0)
            {
                var hooks = await Webhooks.AsNoTracking().Where(w => w.DangHoatDong).ToListAsync(ct);
                foreach (var (ma, duLieu) in suKien)
                    foreach (var w in hooks.Where(w => w.Nhan(ma)))
                        WebhookGuis.Add(new WebhookGui { WebhookId = w.Id, SuKien = ma, ThoiGianTao = now, GuiTu = now,
                            NoiDung = JsonSerializer.Serialize(new { suKien = ma, thoiGian = now, duLieu = duLieu() }, JsonWeb) });
            }
            await base.SaveChangesAsync(ct);
        }
        return result;
    }

    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web)
    { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private readonly List<(string, Func<object>)> _suKienCho = [];

    /// <summary>Phát sự kiện webhook tuỳ ý (gửi ở lần SaveChanges kế tiếp).</summary>
    public void PhatSuKien(string ma, Func<object> duLieu) => _suKienCho.Add((ma, duLieu));

    /// <summary>Nhận diện sự kiện webhook từ thay đổi (dữ liệu dựng sau khi lưu để có Id).</summary>
    private List<(string, Func<object>)> ThuSuKienWebhook()
    {
        var ds = new List<(string, Func<object>)>();
        foreach (var e in ChangeTracker.Entries())
        {
            switch (e.Entity)
            {
                case PhieuSuaChua p when e.State == EntityState.Added:
                    ds.Add((SuKienWebhook.PhieuTao, () => DuLieuPhieu(p)));
                    break;
                case PhieuSuaChua p when e.State == EntityState.Modified && e.Property(nameof(PhieuSuaChua.TrangThai)).IsModified
                                         && p.TrangThai is Domain.Enums.TrangThaiPhieuSuaChua.DaNghiemThu or Domain.Enums.TrangThaiPhieuSuaChua.DaHuy
                                         && !Equals(e.Property(nameof(PhieuSuaChua.TrangThai)).OriginalValue, p.TrangThai):
                    ds.Add((SuKienWebhook.PhieuDong, () => DuLieuPhieu(p)));
                    break;
                case TaiSan t when e.State == EntityState.Modified && e.Property(nameof(TaiSan.TrangThai)).IsModified
                                   && !Equals(e.Property(nameof(TaiSan.TrangThai)).OriginalValue, t.TrangThai):
                    var cu = e.Property(nameof(TaiSan.TrangThai)).OriginalValue?.ToString();
                    ds.Add((SuKienWebhook.TaiSanTrangThai, () => new { t.Id, t.MaTaiSan, t.Ten, t.KhoaPhongId, trangThaiCu = cu, trangThaiMoi = t.TrangThai.ToString() }));
                    if (t.TrangThai == Domain.Enums.TrangThaiTaiSan.DaThanhLy)
                        ds.Add((SuKienWebhook.TaiSanGhiGiam, () => new { t.Id, t.MaTaiSan, t.Ten, t.NguyenGia, t.NgayMua, t.SoHopDong }));
                    break;
                case PhieuNhanHang n when e.State == EntityState.Added:
                    ds.Add((SuKienWebhook.DonHangNhanHang, () => new { n.Id, n.DonMuaHangId, soDonHang = n.DonMuaHang?.SoPhieu, n.NgayNhan, n.GiaTri, n.SoHoaDon, n.PhieuKhoId, taiSanDaTao = n.TaiSanDaTao }));
                    break;
            }
        }
        return ds;
    }

    private static object DuLieuPhieu(PhieuSuaChua p) => new
    {
        p.Id, p.SoPhieu, loai = p.LoaiCongViec.ToString(), p.TaiSanId, p.KhoaPhongId, trangThai = p.TrangThai.ToString(), uuTien = p.UuTien.ToString(),
        moTa = p.MoTaSuCo, p.NgayYeuCau, p.HanXuLy, p.KyThuatVien, p.NgayHoanThanh
    };

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
