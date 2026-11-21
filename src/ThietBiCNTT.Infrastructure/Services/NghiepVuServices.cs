using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Sinh số phiếu dạng {Tiền tố}-{yyMM}-{0001}.</summary>
public class SoPhieuService(AppDbContext db)
{
    public async Task<string> TaoAsync(string tienTo)
    {
        var prefix = $"{tienTo}-{DateTime.Now:yyMM}-";
        IQueryable<string> q = tienTo switch
        {
            "SC" or "BT" => db.PhieuSuaChuas.Select(x => x.SoPhieu),
            "DC" => db.PhieuDieuChuyens.Select(x => x.SoPhieu),
            "TL" => db.DeXuatThanhLys.Select(x => x.SoPhieu),
            _ => throw new ArgumentException("Tiền tố không hợp lệ", nameof(tienTo))
        };
        var last = await q.Where(s => s.StartsWith(prefix)).OrderByDescending(s => s).FirstOrDefaultAsync();
        var n = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        return prefix + n.ToString("D4");
    }
}

public class LichSuService(AppDbContext db)
{
    public void Ghi(int taiSanId, LoaiSuKien loai, string noiDung, string? nguoi) =>
        db.LichSuTaiSans.Add(new LichSuTaiSan
        {
            TaiSanId = taiSanId, LoaiSuKien = loai, NoiDung = noiDung,
            NguoiThucHien = nguoi, ThoiGian = DateTime.Now
        });

    /// <summary>Cập nhật cấu hình, ghi lịch sử từng trường thay đổi.</summary>
    public void CapNhatCauHinh(TaiSan ts, CauHinh moi, string? nguoi)
    {
        if (ts.CauHinh is null)
        {
            moi.TaiSanId = ts.Id; moi.NgayCapNhat = DateTime.Now;
            ts.CauHinh = moi;
            return;
        }
        var thayDoi = ts.CauHinh.SoSanh(moi).ToList();
        if (thayDoi.Count == 0) return;
        foreach (var (truong, cu, m) in thayDoi)
        {
            typeof(CauHinh).GetProperty(truong)!.SetValue(ts.CauHinh, m);
        }
        ts.CauHinh.NgayCapNhat = DateTime.Now;
        Ghi(ts.Id, LoaiSuKien.CauHinh,
            string.Join("; ", thayDoi.Select(t => $"{t.Truong}: '{t.Cu}' → '{t.Moi}'")), nguoi);
    }
}

/// <summary>Sinh lệnh công việc bảo trì định kỳ cho các kế hoạch đến hạn.</summary>
public class BaoTriService(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo)
{
    public async Task<int> SinhLenhDenHanAsync(DateOnly homNay, CancellationToken ct = default)
    {
        var mocSinh = homNay.AddDays(thamSo.SoNguyen(TS.BaoTriSinhTruocNgay));
        var hanNgay = thamSo.SoNguyen(TS.BaoTriHanHoanThanhNgay);
        var keHoachs = await db.KeHoachBaoTris
            .Where(k => k.DangApDung && k.NgayTiepTheo <= mocSinh).ToListAsync(ct);
        var dem = 0;
        foreach (var kh in keHoachs)
        {
            var taiSanIds = kh.TaiSanId is { } id
                ? [id]
                : await db.TaiSans.Where(t => t.LoaiThietBiId == kh.LoaiThietBiId
                                           && t.TrangThai == TrangThaiTaiSan.DangSuDung)
                                  .Select(t => t.Id).ToListAsync(ct);
            foreach (var tsId in taiSanIds)
            {
                // Không sinh trùng nếu còn lệnh bảo trì mở cho cùng kế hoạch + tài sản.
                var daCo = await db.PhieuSuaChuas.AnyAsync(p => p.KeHoachBaoTriId == kh.Id && p.TaiSanId == tsId
                    && p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy, ct);
                if (daCo) continue;
                var ts = await db.TaiSans.FindAsync([tsId], ct);
                var now = DateTime.Now;
                db.PhieuSuaChuas.Add(new PhieuSuaChua
                {
                    SoPhieu = await soPhieu.TaoAsync("BT"),
                    LoaiCongViec = LoaiCongViec.BaoTriDinhKy,
                    TaiSanId = tsId,
                    KhoaPhongId = ts?.KhoaPhongId,
                    MoTaSuCo = $"[Bảo trì định kỳ] {kh.Ten}\n{kh.Checklist}",
                    UuTien = MucDoUuTien.Thap,
                    NgayYeuCau = now,
                    HanXuLy = (kh.NgayTiepTheo > homNay ? kh.NgayTiepTheo : homNay).AddDays(hanNgay).ToDateTime(TimeOnly.MaxValue),
                    KyThuatVien = kh.KyThuatVien,
                    KeHoachBaoTriId = kh.Id,
                    NguoiYeuCau = "Hệ thống (PM)"
                });
                await db.SaveChangesAsync(ct); // lưu từng phiếu để số phiếu kế tiếp không trùng
                dem++;
            }
            kh.DayLichTiep();
            await db.SaveChangesAsync(ct);
        }
        return dem;
    }
}

public record ThongKeKhoa(string Khoa, int SoLuong, decimal GiaTri);
public record DashboardDto(int SoNgayCanhBaoBaoHanh, int SoNgayCanhBaoBaoTri,
    int TongTaiSan, decimal TongNguyenGia, int DangSuDung, int DangSuaChua, int Hong, int ChoThanhLy,
    int PhieuMo, int PhieuQuaHan, int BaoTriDenHan, double? MttrGio, double TyLeDungSla,
    int SapHetBaoHanh, int VatTuDuoiDinhMuc, int TaiSanQuaTuoiTho,
    List<ThongKeKhoa> TheoKhoa, List<(string Nhom, int SoLuong)> TheoNhom,
    List<PhieuSuaChua> PhieuMoiNhat, List<(string Thang, int SoPhieu, decimal ChiPhi)> SuaChuaTheoThang);

public class DashboardService(AppDbContext db, ThamSoService thamSo)
{
    public async Task<DashboardDto> LayAsync()
    {
        var now = DateTime.Now;
        var homNay = DateOnly.FromDateTime(now);
        var ts = db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy);

        var demTrangThai = await ts.GroupBy(t => t.TrangThai).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        int Dem(TrangThaiTaiSan s) => demTrangThai.FirstOrDefault(x => x.Key == s)?.N ?? 0;

        var phieuMo = db.PhieuSuaChuas.Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu
                                               && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy
                                               && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh);
        var tu = now.AddDays(-90);
        var daXong = await db.PhieuSuaChuas
            .Where(p => p.NgayHoanThanh != null && p.NgayHoanThanh >= tu && p.LoaiCongViec == LoaiCongViec.SuaChua)
            .Select(p => new { p.NgayYeuCau, p.NgayHoanThanh, p.HanXuLy }).ToListAsync();

        var loaiMap = await db.LoaiThietBis.ToDictionaryAsync(l => l.Id);
        var dsTuoi = await ts.Select(t => new { t.LoaiThietBiId, t.NgaySuDung, t.NgayMua }).ToListAsync();
        var quaTuoi = dsTuoi.Count(t =>
        {
            var moc = t.NgaySuDung ?? t.NgayMua;
            return moc is not null && loaiMap.TryGetValue(t.LoaiThietBiId, out var l)
                   && moc.Value.AddYears(l.TuoiThoNam) <= homNay;
        });

        var theoKhoa = (await ts.GroupBy(t => t.KhoaPhong!.Ten)
                .Select(g => new { g.Key, N = g.Count(), V = g.Sum(x => x.NguyenGia) }).ToListAsync())
            .Select(x => new ThongKeKhoa(x.Key ?? "(Kho CNTT)", x.N, x.V))
            .OrderByDescending(x => x.SoLuong).Take(10).ToList();
        var theoNhom = (await ts.GroupBy(t => t.LoaiThietBi!.Nhom).Select(g => new { g.Key, N = g.Count() }).ToListAsync())
            .Select(x => (x.Key.ToString(), x.N)).ToList();

        var tu12 = new DateTime(now.Year, now.Month, 1).AddMonths(-11);
        var sc = await db.PhieuSuaChuas.Where(p => p.NgayYeuCau >= tu12)
            .Select(p => new { p.NgayYeuCau, p.ChiPhiThueNgoai, VatTu = p.VatTus.Sum(v => v.SoLuong * v.DonGia) }).ToListAsync();
        var theoThang = Enumerable.Range(0, 12).Select(i => tu12.AddMonths(i)).Select(m =>
        {
            var trong = sc.Where(p => p.NgayYeuCau.Year == m.Year && p.NgayYeuCau.Month == m.Month).ToList();
            return (m.ToString("MM/yy"), trong.Count, trong.Sum(p => p.ChiPhiThueNgoai + p.VatTu));
        }).ToList();

        var ngayBh = thamSo.SoNguyen(TS.CanhBaoBaoHanhNgay);
        var ngayBt = thamSo.SoNguyen(TS.BaoTriCanhBaoNgay);
        return new DashboardDto(ngayBh, ngayBt,
            TongTaiSan: await ts.CountAsync(),
            TongNguyenGia: await ts.SumAsync(t => t.NguyenGia),
            DangSuDung: Dem(TrangThaiTaiSan.DangSuDung),
            DangSuaChua: Dem(TrangThaiTaiSan.DangSuaChua),
            Hong: Dem(TrangThaiTaiSan.Hong),
            ChoThanhLy: Dem(TrangThaiTaiSan.ChoThanhLy),
            PhieuMo: await phieuMo.CountAsync(),
            PhieuQuaHan: await phieuMo.CountAsync(p => p.HanXuLy < now),
            BaoTriDenHan: await db.KeHoachBaoTris.CountAsync(k => k.DangApDung && k.NgayTiepTheo <= homNay.AddDays(ngayBt)),
            MttrGio: daXong.Count == 0 ? null : daXong.Average(p => (p.NgayHoanThanh!.Value - p.NgayYeuCau).TotalHours),
            TyLeDungSla: daXong.Count == 0 ? 100 : 100.0 * daXong.Count(p => p.NgayHoanThanh <= p.HanXuLy) / daXong.Count,
            SapHetBaoHanh: await ts.CountAsync(t => t.HanBaoHanh >= homNay && t.HanBaoHanh <= homNay.AddDays(ngayBh)),
            VatTuDuoiDinhMuc: await db.VatTus.CountAsync(v => v.TonKho <= v.TonToiThieu),
            TaiSanQuaTuoiTho: quaTuoi,
            TheoKhoa: theoKhoa, TheoNhom: theoNhom,
            PhieuMoiNhat: await db.PhieuSuaChuas.Include(p => p.TaiSan).Include(p => p.KhoaPhong)
                .Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy)
                .OrderByDescending(p => p.UuTien).ThenBy(p => p.HanXuLy).Take(8).ToListAsync(),
            SuaChuaTheoThang: theoThang);
    }
}
