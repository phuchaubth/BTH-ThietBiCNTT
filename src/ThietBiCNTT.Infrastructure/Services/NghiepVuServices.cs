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
            "KK" => db.DotKiemKes.Select(x => x.Ma),
            "DT" => db.SuKienDowntimes.Select(x => x.Ma),
            "HD" => db.HanhDongKhacPhucs.Select(x => x.Ma),
            "VB" => db.TaiLieuQuyDinhs.Select(x => x.Ma),
            "AN" => db.SuCoAnNinhs.Select(x => x.Ma),
            "RR" => db.RuiRos.Select(x => x.Ma),
            "TD" => db.PhieuThayDois.Select(x => x.Ma),
            "RQ" => db.DotRaSoatQuyens.Select(x => x.Ma),
            _ => throw new ArgumentException("Tiền tố không hợp lệ", nameof(tienTo))
        };
        var last = await q.Where(s => s.StartsWith(prefix)).OrderByDescending(s => s).FirstOrDefaultAsync();
        var n = last is null ? 1 : int.Parse(last[prefix.Length..]) + 1;
        return prefix + n.ToString("D4");
    }
}

public static class LuuSoPhieu
{
    /// <summary>
    /// Lưu bản ghi mới có số phiếu duy nhất: nếu hai người tạo cùng lúc và trùng số (vi phạm unique index),
    /// sinh lại số và thử lại (tối đa 5 lần).
    /// </summary>
    public static async Task LuuMoiAsync(this AppDbContext db, SoPhieuService soPhieu, string tienTo, Action<string> ganSo, CancellationToken ct = default)
    {
        for (var lan = 1; ; lan++)
        {
            ganSo(await soPhieu.TaoAsync(tienTo));
            try { await db.SaveChangesAsync(ct); return; }
            catch (DbUpdateException) when (lan < 5) { await Task.Delay(Random.Shared.Next(20, 120), ct); }
        }
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
            if (ts.Id != 0) Ghi(ts.Id, LoaiSuKien.CauHinh, "Khai báo cấu hình: " + string.Join("; ",
                CauHinh.CacTruong.Select(f => (f, v: (string?)typeof(CauHinh).GetProperty(f)!.GetValue(moi))).Where(x => !string.IsNullOrWhiteSpace(x.v)).Select(x => $"{x.f}={x.v}")), nguoi);
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
public class BaoTriService(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb)
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
                var phieu = new PhieuSuaChua
                {
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
                };
                db.PhieuSuaChuas.Add(phieu);
                await db.LuuMoiAsync(soPhieu, "BT", so => phieu.SoPhieu = so, ct);
                if (phieu.KyThuatVien is not null)
                    await tb.GuiAsync([phieu.KyThuatVien], LoaiThongBao.BaoTri, $"Lệnh bảo trì {phieu.SoPhieu}: {kh.Ten}",
                        $"Thiết bị {ts?.MaTaiSan} · hạn {phieu.HanXuLy:dd/MM}", $"/PhieuSuaChua/Details/{phieu.Id}");
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
    int PhieuMo, int PhieuQuaHan, int BaoTriDenHan, double? MttrGio, double TyLeDungSla, bool LaToanVien,
    int SapHetBaoHanh, int VatTuDuoiDinhMuc, int TaiSanQuaTuoiTho,
    List<ThongKeKhoa> TheoKhoa, List<(string Nhom, int SoLuong)> TheoNhom,
    List<PhieuSuaChua> PhieuMoiNhat, List<(string Thang, int SoPhieu, decimal ChiPhi)> SuaChuaTheoThang);

public class DashboardService(AppDbContext db, ThamSoService thamSo)
{
    /// <param name="khoaId">null = toàn viện; có giá trị = chỉ số liệu của khoa đó (vai trò khoa).</param>
    public async Task<DashboardDto> LayAsync(int? khoaId = null)
    {
        var now = DateTime.Now;
        var homNay = DateOnly.FromDateTime(now);
        var ts = db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && (khoaId == null || t.KhoaPhongId == khoaId));
        var phamViPhieu = db.PhieuSuaChuas.Where(p => khoaId == null || p.KhoaPhongId == khoaId);

        var demTrangThai = await ts.GroupBy(t => t.TrangThai).Select(g => new { g.Key, N = g.Count() }).ToListAsync();
        int Dem(TrangThaiTaiSan s) => demTrangThai.FirstOrDefault(x => x.Key == s)?.N ?? 0;

        var phieuMo = phamViPhieu.Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu
                                               && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy
                                               && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh);
        var tu = now.AddDays(-90);
        var daXong = await phamViPhieu
            .Where(p => p.NgayHoanThanh != null && p.NgayHoanThanh >= tu && p.LoaiCongViec == LoaiCongViec.SuaChua)
            .Select(p => new { p.NgayYeuCau, p.NgayHoanThanh, p.HanXuLy, p.TongTamDungGiay }).ToListAsync();

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
        var sc = await phamViPhieu.Where(p => p.NgayYeuCau >= tu12)
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
            PhieuQuaHan: await phieuMo.CountAsync(p => p.HanXuLy < now && p.BatDauTamDung == null),
            BaoTriDenHan: await db.KeHoachBaoTris.CountAsync(k => k.DangApDung && k.NgayTiepTheo <= homNay.AddDays(ngayBt)),
            MttrGio: daXong.Count == 0 ? null : daXong.Average(p => Math.Max(0, (p.NgayHoanThanh!.Value - p.NgayYeuCau).TotalHours - p.TongTamDungGiay / 3600.0)),
            TyLeDungSla: daXong.Count == 0 ? 100 : 100.0 * daXong.Count(p => p.NgayHoanThanh <= p.HanXuLy) / daXong.Count,
            LaToanVien: khoaId == null,
            SapHetBaoHanh: await ts.CountAsync(t => t.HanBaoHanh >= homNay && t.HanBaoHanh <= homNay.AddDays(ngayBh)),
            VatTuDuoiDinhMuc: await db.VatTus.CountAsync(v => v.TonKho <= v.TonToiThieu),
            TaiSanQuaTuoiTho: quaTuoi,
            TheoKhoa: theoKhoa, TheoNhom: theoNhom,
            PhieuMoiNhat: await phamViPhieu.Include(p => p.TaiSan).Include(p => p.KhoaPhong)
                .Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy)
                .OrderByDescending(p => p.UuTien).ThenBy(p => p.HanXuLy).Take(8).ToListAsync(),
            SuaChuaTheoThang: theoThang);
    }
}

/// <summary>QĐ-01: tự động nghiệm thu phiếu đã hoàn thành quá N ngày (tham số NghiemThu.TuDongSauNgay).</summary>
public class NghiemThuTuDongService(AppDbContext db, ThamSoService thamSo)
{
    public async Task<int> ChayAsync(DateTime now, CancellationToken ct = default)
    {
        var soNgay = thamSo.SoNguyen(TS.NghiemThuTuDongSauNgay);
        if (soNgay <= 0) return 0;
        var moc = now.AddDays(-soNgay);
        var ds = await db.PhieuSuaChuas.Where(p => p.TrangThai == TrangThaiPhieuSuaChua.HoanThanh && p.NgayHoanThanh <= moc).ToListAsync(ct);
        foreach (var p in ds)
            if (p.TuDongNghiemThu(soNgay, now))
                db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = p.Id, ThoiGian = now, NguoiViet = "system", LaHeThong = true,
                    NoiDung = $"Hoàn thành → Đã nghiệm thu (tự động sau {soNgay} ngày không phản hồi)" });
        await db.SaveChangesAsync(ct);
        return ds.Count;
    }
}
