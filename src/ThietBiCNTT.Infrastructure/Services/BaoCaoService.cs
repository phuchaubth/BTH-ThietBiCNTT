using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

public record DongSla(string Nhom, int SoPhieu, int DaXong, int DungHan, double? MttrGio, decimal ChiPhi, double DungMayGio)
{
    public double TyLeDungHan => DaXong == 0 ? 0 : 100.0 * DungHan / DaXong;
}
public record DongKtv(string KyThuatVien, int SoPhieu, int DaXong, int DungHan, double? MttrGio, decimal GioCong, double? DanhGiaTb, int BaoTri)
{
    public double TyLeDungHan => DaXong == 0 ? 0 : 100.0 * DungHan / DaXong;
}
public record DongThietBi(int TaiSanId, string MaTaiSan, string Ten, string? Khoa, string? Loai, int SoLanHong, double DungMayGio, decimal ChiPhi, double? MtbfNgay, decimal NguyenGia);
public record DongChiPhi(string Nhom, int SoPhieu, decimal VatTu, decimal ThueNgoai)
{
    public decimal Tong => VatTu + ThueNgoai;
}
public record BaoCaoKetQua(DateTime Tu, DateTime Den, string PhamVi,
    DongSla Tong, List<DongSla> TheoThang, List<DongSla> TheoKhoa, List<DongSla> TheoUuTien,
    List<DongKtv> TheoKtv, List<DongThietBi> ThietBiHongNhieu, List<DongChiPhi> ChiPhiTheoKhoa, List<DongChiPhi> ChiPhiTheoLoai,
    List<DongSla> DungMayKhoaTrongYeu);

/// <summary>Báo cáo vận hành: SLA, MTTR, MTBF, thời gian dừng máy, chi phí, năng suất KTV.</summary>
public class BaoCaoService(AppDbContext db)
{
    private sealed record P(int Id, int TaiSanId, string MaTs, string TenTs, string? Khoa, bool TrongYeu, string? Loai, MucDoUuTien UuTien, LoaiCongViec LoaiCv,
        string? Ktv, DateTime NgayYeuCau, DateTime? NgayHoanThanh, DateTime HanXuLy, long TamDung, decimal GioCong, decimal ThueNgoai, decimal VatTu,
        int? DanhGia, bool Huy, DateOnly? NgaySuDung, decimal NguyenGia)
    {
        public bool Xong => NgayHoanThanh is not null;
        public bool DungHan => NgayHoanThanh <= HanXuLy;
        public double? XuLyGio => NgayHoanThanh is { } h ? Math.Max(0, (h - NgayYeuCau).TotalHours - TamDung / 3600.0) : null;
        public double DungMayGio(DateTime den) => Math.Max(0, ((NgayHoanThanh ?? den) - NgayYeuCau).TotalHours);
        public decimal ChiPhi => VatTu + ThueNgoai;
    }

    public async Task<BaoCaoKetQua> LapAsync(DateTime tu, DateTime den, int? khoaId)
    {
        var denCuoi = den.Date.AddDays(1);
        var q = db.PhieuSuaChuas.AsNoTracking().Where(p => p.NgayYeuCau >= tu.Date && p.NgayYeuCau < denCuoi && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy);
        if (khoaId is not null) q = q.Where(p => p.KhoaPhongId == khoaId);
        var ds = (await q.Select(p => new
            {
                p.Id, p.TaiSanId, p.TaiSan!.MaTaiSan, TenTs = p.TaiSan.Ten, Khoa = p.KhoaPhong!.Ten, TrongYeu = (bool?)p.KhoaPhong.LaKhoaTrongYeu, Loai = p.TaiSan.LoaiThietBi!.Ten,
                p.UuTien, p.LoaiCongViec, p.KyThuatVien, p.NgayYeuCau, p.NgayHoanThanh, p.HanXuLy, p.TongTamDungGiay, p.GioCong, p.ChiPhiThueNgoai,
                VatTu = p.VatTus.Sum(v => (decimal?)(v.SoLuong * v.DonGia)) ?? 0, p.DanhGia, p.TaiSan.NgaySuDung, p.TaiSan.NguyenGia, p.TaiSan.NgayMua
            }).ToListAsync())
            .Select(x => new P(x.Id, x.TaiSanId, x.MaTaiSan, x.TenTs, x.Khoa, x.TrongYeu == true, x.Loai, x.UuTien, x.LoaiCongViec, x.KyThuatVien, x.NgayYeuCau,
                x.NgayHoanThanh, x.HanXuLy, x.TongTamDungGiay, x.GioCong, x.ChiPhiThueNgoai, x.VatTu, x.DanhGia, false, x.NgaySuDung ?? x.NgayMua, x.NguyenGia)).ToList();
        var now = DateTime.Now < denCuoi ? DateTime.Now : denCuoi;
        var sc = ds.Where(p => p.LoaiCv == LoaiCongViec.SuaChua).ToList();

        DongSla Gop(string nhom, IEnumerable<P> g)
        {
            var l = g.ToList(); var xong = l.Where(p => p.Xong).ToList();
            return new DongSla(nhom, l.Count, xong.Count, xong.Count(p => p.DungHan), xong.Count == 0 ? null : xong.Average(p => p.XuLyGio!.Value),
                l.Sum(p => p.ChiPhi), l.Sum(p => p.DungMayGio(now)));
        }

        var thang = new List<DongSla>();
        for (var m = new DateTime(tu.Year, tu.Month, 1); m < denCuoi; m = m.AddMonths(1))
            thang.Add(Gop(m.ToString("MM/yyyy"), sc.Where(p => p.NgayYeuCau.Year == m.Year && p.NgayYeuCau.Month == m.Month)));

        var ktv = ds.Where(p => p.Ktv is not null).GroupBy(p => p.Ktv!).Select(g =>
        {
            var x = g.Where(p => p.Xong && p.LoaiCv == LoaiCongViec.SuaChua).ToList();
            var dg = g.Where(p => p.DanhGia is not null).ToList();
            return new DongKtv(g.Key, g.Count(), x.Count, x.Count(p => p.DungHan), x.Count == 0 ? null : x.Average(p => p.XuLyGio!.Value),
                g.Sum(p => p.GioCong), dg.Count == 0 ? null : dg.Average(p => p.DanhGia!.Value), g.Count(p => p.LoaiCv == LoaiCongViec.BaoTriDinhKy));
        }).OrderByDescending(k => k.SoPhieu).ToList();

        var soNgayKy = Math.Max(1, (denCuoi - tu.Date).TotalDays);
        var thietBi = sc.GroupBy(p => p.TaiSanId).Select(g =>
        {
            var f = g.First();
            // MTBF ≈ thời gian quan sát trong kỳ (từ khi đưa vào sử dụng nếu muộn hơn) / số lần hỏng
            var batDau = f.NgaySuDung is { } sd && sd.ToDateTime(TimeOnly.MinValue) > tu.Date ? sd.ToDateTime(TimeOnly.MinValue) : tu.Date;
            var quanSat = Math.Max(1, (denCuoi - batDau).TotalDays);
            return new DongThietBi(g.Key, f.MaTs, f.TenTs, f.Khoa, f.Loai, g.Count(), g.Sum(p => p.DungMayGio(now)), g.Sum(p => p.ChiPhi),
                Math.Round(quanSat / g.Count(), 1), f.NguyenGia);
        }).OrderByDescending(t => t.SoLanHong).ThenByDescending(t => t.DungMayGio).Take(20).ToList();

        string phamVi = khoaId is null ? "Toàn bệnh viện" : (await db.KhoaPhongs.FindAsync(khoaId))?.Ten ?? "";
        return new BaoCaoKetQua(tu.Date, den.Date, phamVi, Gop("Tổng", sc), thang,
            sc.GroupBy(p => p.Khoa ?? "Kho CNTT").Select(g => Gop(g.Key, g)).OrderByDescending(x => x.SoPhieu).ToList(),
            sc.GroupBy(p => p.UuTien).OrderByDescending(g => g.Key).Select(g => Gop(g.Key.TenHienThi(), g)).ToList(),
            ktv, thietBi,
            ds.GroupBy(p => p.Khoa ?? "Kho CNTT").Select(g => new DongChiPhi(g.Key, g.Count(), g.Sum(p => p.VatTu), g.Sum(p => p.ThueNgoai))).OrderByDescending(x => x.Tong).ToList(),
            ds.GroupBy(p => p.Loai ?? "—").Select(g => new DongChiPhi(g.Key, g.Count(), g.Sum(p => p.VatTu), g.Sum(p => p.ThueNgoai))).OrderByDescending(x => x.Tong).ToList(),
            sc.Where(p => p.TrongYeu).GroupBy(p => p.Khoa!).Select(g => Gop(g.Key, g)).OrderByDescending(x => x.DungMayGio).ToList());
    }
}

/// <summary>Dòng bản tin hằng ngày: hợp đồng, bản quyền, gửi hãng.</summary>
public static class HopDongTongHop
{
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var db = (AppDbContext)sp.GetService(typeof(AppDbContext))!;
        var ts = (ThamSoService)sp.GetService(typeof(ThamSoService))!;
        var d = DateOnly.FromDateTime(now); var n = ts.SoNguyen(TS.HopDongCanhBaoNgay); var moc = d.AddDays(n);
        var kq = new List<string>();
        var hd = await db.HopDongs.Where(h => h.HieuLucDen >= d && h.HieuLucDen <= moc).Select(h => h.SoHopDong).ToListAsync(ct);
        if (hd.Count > 0) kq.Add($"• Hợp đồng sắp hết hạn ({n} ngày): {string.Join(", ", hd)}");
        var bq = await db.BanQuyenPhanMems.Where(b => b.NgayHetHan >= d && b.NgayHetHan <= moc).Select(b => b.Ten).ToListAsync(ct);
        if (bq.Count > 0) kq.Add($"• Bản quyền sắp hết hạn: {string.Join(", ", bq)}");
        var vuot = await db.BanQuyenPhanMems.Where(b => b.Loai != LoaiGiayPhep.MienPhi && b.CapPhats.Count > b.SoLuong).Select(b => b.Ten).ToListAsync(ct);
        if (vuot.Count > 0) kq.Add($"• Bản quyền dùng vượt số lượng: {string.Join(", ", vuot)}");
        var gh = await db.PhieuSuaChuas.Where(p => p.TrangThai == TrangThaiPhieuSuaChua.GuiHang && p.NgayHenTra < d).Select(p => p.SoPhieu).ToListAsync(ct);
        if (gh.Count > 0) kq.Add($"• Gửi hãng quá ngày hẹn trả: {string.Join(", ", gh)}");
        return kq;
    }
}
