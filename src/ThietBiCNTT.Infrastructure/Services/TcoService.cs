using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Một dòng TCO (theo tài sản hoặc theo nhóm).</summary>
public record DongTco(string Nhom, string? MaTaiSan, string? Loai, string? Model, string? Khoa, int SoTaiSan, decimal NguyenGia,
    decimal SuaChua, decimal BaoTri, decimal HopDong, decimal BanQuyen, int SoLanHong, double NamSuDung)
{
    public decimal VanHanh => SuaChua + BaoTri + HopDong + BanQuyen;
    public decimal Tong => NguyenGia + VanHanh;
    /// <summary>Chi phí sở hữu bình quân mỗi năm (mỗi tài sản).</summary>
    public decimal MoiNam => NamSuDung <= 0 || SoTaiSan == 0 ? 0 : Math.Round(Tong / SoTaiSan / (decimal)NamSuDung, 0);
    /// <summary>% chi phí sửa chữa so với nguyên giá.</summary>
    public decimal TyLeSua => NguyenGia <= 0 ? 0 : Math.Round(SuaChua * 100 / NguyenGia, 1);
}

public enum NhomTco { TaiSan = 1, Loai, Model, Khoa }

/// <summary>
/// Tổng chi phí sở hữu (FR-BC-06) = nguyên giá + sửa chữa (vật tư + thuê ngoài) + bảo trì định kỳ + hợp đồng bảo trì/thuê ngoài
/// phân bổ đều cho tài sản gắn hợp đồng + bản quyền phần mềm (giá hợp đồng bản quyền ÷ số giấy phép × số giấy phép cấp cho tài sản).
/// </summary>
public class TcoService(AppDbContext db)
{
    public async Task<List<DongTco>> LapAsync(NhomTco nhom, int? khoaId = null, int? loaiId = null, bool gomThanhLy = false, DateOnly? homNay = null)
    {
        var d = homNay ?? DateOnly.FromDateTime(DateTime.Today);
        var q = db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).AsQueryable();
        if (!gomThanhLy) q = q.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy);
        if (khoaId is not null) q = q.Where(t => t.KhoaPhongId == khoaId);
        if (loaiId is not null) q = q.Where(t => t.LoaiThietBiId == loaiId);
        var ts = await q.ToListAsync();
        var ids = ts.Select(t => t.Id).ToHashSet();

        var phieu = await db.PhieuSuaChuas.AsNoTracking().Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaHuy).Include(p => p.VatTus)
            .Select(p => new { p.TaiSanId, p.LoaiCongViec, p.ChiPhiThueNgoai, VatTu = p.VatTus.Select(v => v.SoLuong * v.DonGia).ToList() }).ToListAsync();
        var sua = phieu.Where(p => ids.Contains(p.TaiSanId)).GroupBy(p => p.TaiSanId).ToDictionary(g => g.Key, g => (
            Sua: g.Where(p => p.LoaiCongViec == LoaiCongViec.SuaChua).Sum(p => p.ChiPhiThueNgoai + p.VatTu.Sum()),
            BaoTri: g.Where(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy).Sum(p => p.ChiPhiThueNgoai + p.VatTu.Sum()),
            Hong: g.Count(p => p.LoaiCongViec == LoaiCongViec.SuaChua)));

        // Hợp đồng bảo trì / thuê ngoài gắn tài sản: chia đều cho số tài sản của hợp đồng
        var hd = await db.HopDongTaiSans.AsNoTracking().Where(x => x.HopDong!.Loai == LoaiHopDong.BaoTri || x.HopDong.Loai == LoaiHopDong.ThueNgoai)
            .Select(x => new { x.HopDongId, x.TaiSanId, x.HopDong!.GiaTri }).ToListAsync();
        var soTsHd = hd.GroupBy(x => x.HopDongId).ToDictionary(g => g.Key, g => g.Count());
        var phanBoHd = hd.Where(x => ids.Contains(x.TaiSanId)).GroupBy(x => x.TaiSanId).ToDictionary(g => g.Key, g => g.Sum(x => x.GiaTri / soTsHd[x.HopDongId]));

        // Bản quyền: giá trị hợp đồng của bản quyền ÷ số giấy phép, nhân số giấy phép cấp cho tài sản
        var bq = await db.CapPhatBanQuyens.AsNoTracking().Where(c => c.BanQuyenPhanMem!.HopDongId != null)
            .Select(c => new { c.TaiSanId, c.BanQuyenPhanMem!.SoLuong, GiaTri = c.BanQuyenPhanMem.HopDong!.GiaTri,
                SoBanQuyenCungHd = db.BanQuyenPhanMems.Count(b => b.HopDongId == c.BanQuyenPhanMem.HopDongId) }).ToListAsync();
        var phanBoBq = bq.Where(x => ids.Contains(x.TaiSanId) && x.SoLuong > 0).GroupBy(x => x.TaiSanId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.GiaTri / Math.Max(1, x.SoBanQuyenCungHd) / x.SoLuong));

        double Nam(TaiSan t) => (t.NgaySuDung ?? t.NgayMua) is { } bd ? Math.Max(1.0 / 12, (d.DayNumber - bd.DayNumber) / 365.25) : 1;
        var dong = ts.Select(t =>
        {
            var s = sua.GetValueOrDefault(t.Id);
            return new DongTco(t.MaTaiSan, t.MaTaiSan, t.LoaiThietBi?.Ten, ModelCua(t), t.KhoaPhong?.Ten, 1, t.NguyenGia, s.Sua, s.BaoTri,
                Math.Round(phanBoHd.GetValueOrDefault(t.Id), 0), Math.Round(phanBoBq.GetValueOrDefault(t.Id), 0), s.Hong, Math.Round(Nam(t), 2));
        }).ToList();
        if (nhom == NhomTco.TaiSan) return dong.OrderByDescending(x => x.Tong).ToList();
        Func<DongTco, string> key = nhom switch { NhomTco.Loai => x => x.Loai ?? "(chưa phân loại)", NhomTco.Model => x => x.Model ?? "(không rõ model)", _ => x => x.Khoa ?? "Kho CNTT" };
        return dong.GroupBy(key).Select(g => new DongTco(g.Key, null, nhom == NhomTco.Model ? g.First().Loai : null, null, null, g.Count(), g.Sum(x => x.NguyenGia),
                g.Sum(x => x.SuaChua), g.Sum(x => x.BaoTri), g.Sum(x => x.HopDong), g.Sum(x => x.BanQuyen), g.Sum(x => x.SoLanHong), Math.Round(g.Average(x => x.NamSuDung), 2)))
            .OrderByDescending(x => x.Tong).ToList();
    }

    public static string? ModelCua(TaiSan t) => string.IsNullOrWhiteSpace(t.Hang) && string.IsNullOrWhiteSpace(t.Model) ? null : $"{t.Hang} {t.Model}".Trim();
}
