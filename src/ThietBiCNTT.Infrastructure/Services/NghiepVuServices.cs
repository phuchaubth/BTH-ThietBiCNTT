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
            "NK" or "XK" or "CK" or "DC-K" => db.PhieuKhos.Select(x => x.SoPhieu),
            "DX" => db.DeXuatMuaSams.Select(x => x.SoPhieu),
            "DH" => db.DonMuaHangs.Select(x => x.SoPhieu),
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
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];

    /// <summary>Tài sản thuộc phạm vi kế hoạch (đang sử dụng).</summary>
    public IQueryable<TaiSan> PhamVi(KeHoachBaoTri kh) =>
        kh.TaiSanId is { } id ? db.TaiSans.Where(t => t.Id == id && t.TrangThai != TrangThaiTaiSan.DaThanhLy)
            : db.TaiSans.Where(t => t.LoaiThietBiId == kh.LoaiThietBiId && t.TrangThai == TrangThaiTaiSan.DangSuDung
                                    && (kh.KhoaPhongId == null || t.KhoaPhongId == kh.KhoaPhongId));

    /// <summary>
    /// Sinh lệnh bảo trì (job nền mỗi vài giờ, hoặc bấm tay). Chỉ kế hoạch Hiệu lực.
    /// • Thời gian: đến NgayTiepTheo (trừ số ngày sinh trước) → lệnh cho mọi tài sản phạm vi, rồi dời lịch một chu kỳ.
    ///   Kết hợp: bỏ qua tài sản vừa bảo trì (theo chỉ số) trong nửa chu kỳ gần đây.
    /// • Chỉ số: tài sản có bộ đếm tăng ≥ chu kỳ kể từ lần bảo trì hoàn thành gần nhất → lệnh riêng.
    ///   Chưa từng bảo trì theo kế hoạch → lần đầu theo NgayTiepTheo.
    /// </summary>
    public async Task<int> SinhLenhDenHanAsync(DateOnly homNay, CancellationToken ct = default)
    {
        var mocSinh = homNay.AddDays(thamSo.SoNguyen(TS.BaoTriSinhTruocNgay));
        var hanNgay = thamSo.SoNguyen(TS.BaoTriHanHoanThanhNgay);
        foreach (var het in await db.KeHoachBaoTris.Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc && k.NgayKetThuc < homNay).ToListAsync(ct))
            het.KiemTraHetHan(homNay);
        await db.SaveChangesAsync(ct);
        var keHoachs = await db.KeHoachBaoTris.Include(k => k.VatTus).ThenInclude(v => v.VatTu)
            .Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync(ct);
        var dem = 0;
        foreach (var kh in keHoachs)
        {
            var dsTs = await PhamVi(kh).ToListAsync(ct);
            if (dsTs.Count == 0) { if (kh.TheoThoiGian && kh.NgayTiepTheo <= mocSinh) { kh.DayLichTiep(); await db.SaveChangesAsync(ct); } continue; }
            var ids = dsTs.Select(t => t.Id).ToList();
            // Lệnh đã Hoàn thành (chờ khoa nghiệm thu) coi như đã làm xong — không chặn lệnh kỳ sau
            var dangMo = (await db.PhieuSuaChuas.Where(p => p.KeHoachBaoTriId == kh.Id && ids.Contains(p.TaiSanId) && !Dong.Contains(p.TrangThai) && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh)
                .Select(p => p.TaiSanId).ToListAsync(ct)).ToHashSet();
            var lanCuoi = (await db.PhieuSuaChuas.Where(p => p.KeHoachBaoTriId == kh.Id && ids.Contains(p.TaiSanId) && p.NgayHoanThanh != null && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy)
                    .Select(p => new { p.TaiSanId, p.NgayHoanThanh, p.ChiSoBoDem }).ToListAsync(ct))
                .GroupBy(x => x.TaiSanId).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.NgayHoanThanh).First());

            var denKy = kh.NgayTiepTheo <= mocSinh;
            foreach (var ts in dsTs.Where(t => !dangMo.Contains(t.Id)))
            {
                var cuoi = lanCuoi.GetValueOrDefault(ts.Id);
                string? lyDo = null;
                if (kh.TheoChiSo && kh.DenHanChiSo(ts.ChiSoBoDem, cuoi?.ChiSoBoDem))
                    lyDo = $"đạt chỉ số: {ts.ChiSoBoDem:N0} {kh.DonViChiSo} (lần trước {cuoi!.ChiSoBoDem:N0}, chu kỳ {kh.ChuKyChiSo:N0})";
                else if (denKy && kh.KieuChuKy == KieuChuKy.ChiSo && cuoi is null)
                    lyDo = "lần bảo trì đầu tiên theo kế hoạch";
                else if (denKy && kh.TheoThoiGian)
                {
                    // Kết hợp: tài sản vừa bảo trì (do đạt chỉ số) trong nửa chu kỳ → chờ kỳ sau
                    if (kh.KieuChuKy == KieuChuKy.KetHop && cuoi?.NgayHoanThanh is { } ht && ht > homNay.ToDateTime(TimeOnly.MinValue).AddDays(-kh.ChuKyNgay / 2.0)) continue;
                    lyDo = $"đến kỳ {kh.NgayTiepTheo:dd/MM/yyyy}";
                }
                if (lyDo is null) continue;
                await TaoLenhAsync(kh, ts, lyDo, homNay, hanNgay, ct);
                dem++;
            }
            if (denKy && kh.TheoThoiGian) kh.DayLichTiep();
            await db.SaveChangesAsync(ct);
        }
        return dem;
    }

    private async Task TaoLenhAsync(KeHoachBaoTri kh, TaiSan ts, string lyDo, DateOnly homNay, int hanNgay, CancellationToken ct)
    {
        var now = DateTime.Now;
        var vt = kh.VatTus.Count == 0 ? "" : "\nVật tư dự kiến: " + string.Join(", ", kh.VatTus.Select(v => $"{v.VatTu?.Ten} × {v.SoLuong:0.##} {v.VatTu?.DonViTinh}"));
        var phieu = new PhieuSuaChua
        {
            LoaiCongViec = LoaiCongViec.BaoTriDinhKy, TaiSanId = ts.Id, KhoaPhongId = ts.KhoaPhongId,
            MoTaSuCo = $"[Bảo trì định kỳ] {kh.Ten} — {lyDo}" + (string.IsNullOrWhiteSpace(kh.Checklist) ? "" : "\n" + kh.Checklist) + vt
                       + (kh.MauKiemTraId is null ? "" : "\nThực hiện checklist trên hệ thống trước khi hoàn thành."),
            UuTien = MucDoUuTien.Thap, NgayYeuCau = now,
            HanXuLy = (kh.NgayTiepTheo > homNay && kh.TheoThoiGian ? kh.NgayTiepTheo : homNay).AddDays(hanNgay).ToDateTime(TimeOnly.MaxValue),
            KyThuatVien = kh.KyThuatVien, KeHoachBaoTriId = kh.Id, NguoiYeuCau = "Hệ thống (PM)"
        };
        if (phieu.KyThuatVien is not null) phieu.TiepNhan(phieu.KyThuatVien, HinhThucXuLy.TuSua, now); // KTV phụ trách của kế hoạch nhận lệnh ngay
        db.PhieuSuaChuas.Add(phieu);
        await db.LuuMoiAsync(soPhieu, "BT", so => phieu.SoPhieu = so, ct);
        var nhan = phieu.KyThuatVien is not null ? [phieu.KyThuatVien] : await tb.CanBoCNTTAsync();
        await tb.GuiAsync(nhan, LoaiThongBao.BaoTri, $"Lệnh bảo trì {phieu.SoPhieu}: {kh.Ten}",
            $"Thiết bị {ts.MaTaiSan} · {lyDo} · hạn {phieu.HanXuLy:dd/MM}", $"/PhieuSuaChua/Details/{phieu.Id}");
    }

    /// <summary>Dự trù theo kỳ (FR-PM-12): số lượt, giờ công, vật tư, chi phí của mỗi kế hoạch Hiệu lực trong [tu, den].</summary>
    public async Task<List<DuTruKeHoach>> DuTruAsync(DateOnly tu, DateOnly den)
    {
        var kq = new List<DuTruKeHoach>();
        foreach (var kh in await db.KeHoachBaoTris.AsNoTracking().Include(k => k.VatTus).ThenInclude(v => v.VatTu).Include(k => k.LoaiThietBi).Include(k => k.TaiSan)
                     .Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync())
        {
            var soTs = await PhamVi(kh).CountAsync();
            var moc = kh.CacMoc(tu, den).Count();
            var luot = moc * soTs;
            kq.Add(new DuTruKeHoach(kh, soTs, moc, luot, luot * kh.GioCongDuKien, luot * kh.ChiPhiDuKien,
                kh.VatTus.Select(v => new DuTruVatTu(v.VatTu!, v.SoLuong * luot, v.SoLuong * luot * v.VatTu!.DonGia)).ToList()));
        }
        return kq;
    }
}

public record DuTruVatTu(VatTu VatTu, decimal SoLuong, decimal ThanhTien);
public record DuTruKeHoach(KeHoachBaoTri KeHoach, int SoTaiSan, int SoMoc, int SoLuot, decimal GioCong, decimal ChiPhiThueNgoai, List<DuTruVatTu> VatTu)
{
    public decimal ChiPhiVatTu => VatTu.Sum(v => v.ThanhTien);
    public decimal TongChiPhi => ChiPhiVatTu + ChiPhiThueNgoai;
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
            .Select(p => new { p.NgayYeuCau, ChiPhiThueNgoai = p.ChiPhiThueNgoai + p.ChiPhiNhanCong, VatTu = p.VatTus.Sum(v => v.SoLuong * v.DonGia) }).ToListAsync();
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
