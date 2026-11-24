using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Hệ thống bị ảnh hưởng khi một tài sản ngừng (FR-HTT-07/11).</summary>
public record AnhHuongTaiSan(List<HeThongThongTin> HeThongs, List<KhoaPhong> Khoas, bool CoToiQuanTrong);

/// <summary>GĐ3.1 – nghiệp vụ dùng chung của phân hệ hạ tầng CNTT (docs/JCI8-HaTangCNTT.md).</summary>
public class HaTangService(AppDbContext db, ThamSoService thamSo)
{
    public async Task<List<CanhPhuThuoc>> CanhAsync(CancellationToken ct = default) =>
        await db.PhuThuocHeThongs.AsNoTracking()
            .Where(p => p.HeThong!.TrangThai != TrangThaiHeThong.NgungSuDung)
            .Select(p => new CanhPhuThuoc(p.HeThongId, p.TaiSanId, p.HeThongPhuThuocId, p.CoDuPhong)).ToListAsync(ct);

    /// <summary>FR-HTT-07: từ một tài sản → hệ thống và khoa bị ảnh hưởng.</summary>
    public async Task<AnhHuongTaiSan> AnhHuongCuaTaiSanAsync(int taiSanId, CancellationToken ct = default)
    {
        var ids = PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(taiSanId, await CanhAsync(ct));
        return await AnhHuongAsync(ids, ct);
    }

    public async Task<AnhHuongTaiSan> AnhHuongAsync(IEnumerable<int> heThongIds, CancellationToken ct = default)
    {
        var ids = heThongIds.ToList();
        if (ids.Count == 0) return new([], [], false);
        var hts = await db.HeThongThongTins.AsNoTracking().Where(h => ids.Contains(h.Id) && h.TrangThai != TrangThaiHeThong.NgungSuDung)
            .OrderBy(h => h.MucTrongYeu).ThenBy(h => h.Ten).ToListAsync(ct);
        var khoas = await db.HeThongKhoaPhongs.AsNoTracking().Where(k => ids.Contains(k.HeThongId)).Select(k => k.KhoaPhong!).Distinct()
            .OrderBy(k => k.Ten).ToListAsync(ct);
        return new(hts, khoas, hts.Any(h => h.MucTrongYeu == MucTrongYeu.ToiQuanTrong));
    }

    /// <summary>Kế hoạch downtime đang hiệu lực của hệ thống.</summary>
    public Task<KeHoachDowntime?> KeHoachHieuLucAsync(int heThongId) =>
        db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == heThongId && k.TrangThai == TrangThaiKeHoach.HieuLuc)
            .OrderByDescending(k => k.PhienBan).FirstOrDefaultAsync();

    /// <summary>Ghi nhận các hệ thống/khoa bị ảnh hưởng vào sự kiện + chốt RTO nhỏ nhất và mức trọng yếu cao nhất.</summary>
    public async Task GanHeThongAsync(SuKienDowntime s, IEnumerable<int> heThongIds, IEnumerable<int>? khoaIds = null, IEnumerable<int>? khoaKichHoatGiay = null)
    {
        var ids = heThongIds.Distinct().ToList();
        if (ids.Count == 0) throw new Domain.Common.DomainException("Chọn ít nhất một hệ thống bị ảnh hưởng.");
        var hts = await db.HeThongThongTins.Where(h => ids.Contains(h.Id)).ToListAsync();
        foreach (var h in hts) s.HeThongs.Add(new SuKienDowntimeHeThong { HeThongId = h.Id });
        s.RtoPhut = hts.Min(thamSo.Rto);
        s.MucTrongYeuCaoNhat = hts.Min(h => h.MucTrongYeu);
        var khoa = khoaIds?.ToList() ?? await db.HeThongKhoaPhongs.Where(k => ids.Contains(k.HeThongId)).Select(k => k.KhoaPhongId).Distinct().ToListAsync();
        var giay = (khoaKichHoatGiay ?? []).ToHashSet();
        foreach (var k in khoa.Distinct()) s.Khoas.Add(new SuKienDowntimeKhoa { KhoaPhongId = k, KichHoatGiay = s.KichHoatKeHoach && (khoaKichHoatGiay is null || giay.Contains(k)) });
    }

    /// <summary>Mốc sao lưu của một tác vụ trong khoảng thời gian (FR-SL-04, FR-SL-08).</summary>
    public async Task<List<(DateTime Moc, TrangThaiMocSaoLuu TrangThai, KetQuaSaoLuuLan? KetQua)>> MocSaoLuuAsync(TacVuSaoLuu tv, DateTime tu, DateTime den, DateTime now)
    {
        var buoc = TimeSpan.FromHours(Math.Max(1, tv.ChuKyGio));
        var kq = await db.KetQuaSaoLuuLans.AsNoTracking().Where(k => k.TacVuSaoLuuId == tv.Id && k.ThoiDiem >= tu - buoc && k.ThoiDiem < den + buoc).ToListAsync();
        return DoiChieuSaoLuu.Moc(tv, kq, tu, den, now, TimeSpan.FromHours(thamSo.SoNguyen(TS.JciSaoLuuChoGio)));
    }

    /// <summary>
    /// FR-DT-10, FR-SL-05: tự sinh đợt diễn tập và lần thử phục hồi theo chu kỳ của mức trọng yếu (bảng 4.1.3).
    /// Mốc tính từ lần đã chốt gần nhất (chưa có → từ ngày kế hoạch hiệu lực / ngày khai báo hệ thống). Tạo trước hạn N ngày.
    /// </summary>
    public async Task<int> SinhLichAsync(DateOnly homNay, CancellationToken ct = default)
    {
        var dem = 0;
        var hts = await db.HeThongThongTins.Where(h => h.TrangThai == TrangThaiHeThong.VanHanh).ToListAsync(ct);
        var nhacDt = thamSo.SoNguyen(TS.JciNhacDienTapTruocNgay);
        var nhacPh = thamSo.SoNguyen(TS.JciNhacPhucHoiTruocNgay);
        foreach (var h in hts)
        {
            var mt = thamSo.MucTieu(h.MucTrongYeu);
            if (mt.ChuKyDienTapThang > 0 && !await db.DotDienTaps.AnyAsync(d => d.HeThongId == h.Id && !d.DaChot, ct))
            {
                var kh = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == h.Id && k.TrangThai == TrangThaiKeHoach.HieuLuc).OrderByDescending(k => k.PhienBan).FirstOrDefaultAsync(ct);
                var cuoi = await db.DotDienTaps.Where(d => d.HeThongId == h.Id && d.DaChot).OrderByDescending(d => d.NgayChot).Select(d => d.NgayThucHien ?? d.NgayChot).FirstOrDefaultAsync(ct);
                var goc = cuoi is { } c ? DateOnly.FromDateTime(c) : kh?.NgayHieuLuc ?? DateOnly.FromDateTime(h.NgayTao);
                var han = goc.AddMonths(mt.ChuKyDienTapThang);
                if (han.AddDays(-nhacDt) <= homNay)
                {
                    db.DotDienTaps.Add(new DotDienTap { HeThongId = h.Id, KeHoachDowntimeId = kh?.Id, HanThucHien = han, NguoiTao = "system",
                        LyDo = cuoi is null ? $"Diễn tập định kỳ {mt.ChuKyDienTapThang} tháng (lần đầu)" : $"Diễn tập định kỳ {mt.ChuKyDienTapThang} tháng" });
                    dem++;
                }
            }
            if (mt.ChuKyThuPhucHoiThang > 0 && !await db.LanThuPhucHois.AnyAsync(d => d.HeThongId == h.Id && !d.DaChot, ct))
            {
                var cuoi = await db.LanThuPhucHois.Where(d => d.HeThongId == h.Id && d.DaChot).OrderByDescending(d => d.NgayChot).Select(d => d.KetThuc ?? d.NgayChot).FirstOrDefaultAsync(ct);
                var goc = cuoi is { } c ? DateOnly.FromDateTime(c) : DateOnly.FromDateTime(h.NgayTao);
                var han = goc.AddMonths(mt.ChuKyThuPhucHoiThang);
                if (han.AddDays(-nhacPh) <= homNay)
                {
                    db.LanThuPhucHois.Add(new LanThuPhucHoi { HeThongId = h.Id, HanThucHien = han, NguoiTao = "system",
                        LyDo = $"Thử phục hồi định kỳ {mt.ChuKyThuPhucHoiThang} tháng" + (cuoi is null ? " (lần đầu)" : "") });
                    dem++;
                }
            }
        }
        if (dem > 0) await db.SaveChangesAsync(ct);
        return dem;
    }
}

/// <summary>Mục 5: hành động khắc phục dùng chung.</summary>
public class HanhDongService(AppDbContext db, SoPhieuService soPhieu, ThongBaoService tb, ThamSoService thamSo)
{
    public async Task<HanhDongKhacPhuc> TaoAsync(NguonHanhDong nguon, int? nguonId, string moTa, int? heThongId, string nguoiChiuTrachNhiem,
        DateOnly? han, LoaiHanhDong loai, MucDoUuTien uuTien, string nguoiTao)
    {
        if (string.IsNullOrWhiteSpace(moTa)) throw new Domain.Common.DomainException("Nhập nội dung hành động.");
        if (string.IsNullOrWhiteSpace(nguoiChiuTrachNhiem)) throw new Domain.Common.DomainException("Chọn người chịu trách nhiệm.");
        var h = new HanhDongKhacPhuc
        {
            Nguon = nguon, NguonId = nguonId, MoTa = moTa.Trim(), HeThongId = heThongId, NguoiChiuTrachNhiem = nguoiChiuTrachNhiem.Trim(),
            Han = han ?? DateOnly.FromDateTime(DateTime.Today).AddDays(thamSo.SoNguyen(TS.JciHanhDongMacDinhNgay)), Loai = loai, UuTien = uuTien, NguoiTao = nguoiTao
        };
        db.HanhDongKhacPhucs.Add(h);
        await db.LuuMoiAsync(soPhieu, "HD", so => h.Ma = so);
        await tb.GuiAsync([h.NguoiChiuTrachNhiem], LoaiThongBao.HanhDong, $"Bạn được giao hành động {h.Ma} (hạn {h.Han:dd/MM/yyyy})",
            $"[{h.Nguon.TenHienThi()}] {h.MoTa}", $"/HanhDong/Details/{h.Id}", boQua: nguoiTao);
        await db.SaveChangesAsync();
        return h;
    }

    public static string LinkNguon(NguonHanhDong n, int? id) => (n, id) switch
    {
        (NguonHanhDong.SuKienDowntime, { } i) => $"/SuKienDowntime/Details/{i}",
        (NguonHanhDong.DienTap, { } i) => $"/DienTap/Details/{i}",
        (NguonHanhDong.ThuPhucHoi, { } i) => $"/SaoLuu/PhucHoi/{i}",
        (NguonHanhDong.KiemTra, { } i) => $"/KiemTra/Lan/{i}",
        (NguonHanhDong.SaoLuu, { } i) => $"/SaoLuu/TacVu/{i}",
        _ => ""
    };
}

/// <summary>
/// Chạy nền mỗi 5 phút: sinh lịch diễn tập/thử phục hồi, thông báo downtime có kế hoạch,
/// cảnh báo sao lưu thiếu/lỗi (FR-SL-04), nhắc & leo thang diễn tập/thử phục hồi quá hạn (BR-DT-06), hành động khắc phục (FR-HD-03).
/// </summary>
public class HaTangCanhBaoService(AppDbContext db, HaTangService haTang, ThongBaoService tb, ThamSoService thamSo, UserManager<AppUser> users,
    SoPhieuService soPhieu, LichSuService lichSu)
{
    public async Task ChayAsync(DateTime now, CancellationToken ct = default)
    {
        var homNay = DateOnly.FromDateTime(now);
        await haTang.SinhLichAsync(homNay, ct);
        await DowntimeCoKeHoachAsync(now, ct);
        await DienTapPhucHoiAsync(homNay, ct);
        await SaoLuuAsync(now, ct);
        await HanhDongAsync(homNay, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<List<string>> PhuTrachAsync(HeThongThongTin? h) =>
        h is null ? await tb.CanBoCNTTAsync()
        : new[] { h.PhuTrachChinh, h.PhuTrachDuPhong }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!).ToList() is { Count: > 0 } l ? l : await tb.CanBoCNTTAsync();

    private List<string> NguoiNhanCap2() => thamSo.Chuoi(TS.LeoThangCap2NguoiNhan).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    private async Task<List<string>> LanhDaoAsync() => (await users.GetUsersInRoleAsync(VaiTro.LanhDao)).Where(u => u.DangHoatDong).Select(u => u.UserName!).ToList();

    /// <summary>FR-DT-05: thông báo khoa trước N giờ và nhắc lại trước 1 giờ.</summary>
    private async Task DowntimeCoKeHoachAsync(DateTime now, CancellationToken ct)
    {
        var truoc = thamSo.SoNguyen(TS.JciThongBaoTruocGio);
        var ds = await db.SuKienDowntimes.Include(s => s.HeThongs).ThenInclude(h => h.HeThong).Include(s => s.Khoas)
            .Where(s => s.TrangThai == TrangThaiSuKien.DaLenLich && s.DuKienBatDau != null && (!s.DaThongBaoTruoc || !s.DaNhacTruoc1Gio)).ToListAsync(ct);
        foreach (var s in ds)
        {
            var bd = s.DuKienBatDau!.Value;
            var nhac1h = !s.DaNhacTruoc1Gio && bd - now <= TimeSpan.FromHours(1) && bd > now;
            var thongBao = !s.DaThongBaoTruoc && bd - now <= TimeSpan.FromHours(truoc);
            if (!thongBao && !nhac1h) continue;
            var nhan = new List<string>();
            foreach (var k in s.Khoas) nhan.AddRange(await tb.TruongKhoaAsync(k.KhoaPhongId));
            nhan.AddRange(await tb.CanBoCNTTAsync());
            var ten = string.Join(", ", s.HeThongs.Select(h => h.HeThong?.Ten));
            await tb.GuiAsync(nhan, LoaiThongBao.Downtime, $"{(nhac1h ? "[Còn 1 giờ] " : "")}Downtime có kế hoạch {ten}: {bd:HH:mm dd/MM} – {s.DuKienKetThuc:HH:mm dd/MM}",
                $"{s.MoTa}\nKhoa chuẩn bị quy trình thay thế theo bộ downtime của khoa.", $"/SuKienDowntime/Details/{s.Id}", MucDoThongBao.CanhBao);
            s.DaThongBaoTruoc = true;
            if (nhac1h) s.DaNhacTruoc1Gio = true;
        }
    }

    /// <summary>FR-DT-10, FR-SL-05, BR-DT-06: nhắc khi đợt được tạo, leo thang khi quá hạn 0/7/30 ngày.</summary>
    private async Task DienTapPhucHoiAsync(DateOnly homNay, CancellationToken ct)
    {
        var c2 = thamSo.SoNguyen(TS.JciLeoThangDienTap2Ngay); var c3 = thamSo.SoNguyen(TS.JciLeoThangDienTap3Ngay);
        int Cap(DateOnly han) { var tre = homNay.DayNumber - han.DayNumber; return tre <= 0 ? 0 : c3 > 0 && tre >= c3 ? 3 : c2 > 0 && tre >= c2 ? 2 : 1; }

        foreach (var d in await db.DotDienTaps.Include(x => x.HeThong).Where(x => !x.DaChot).ToListAsync(ct))
        {
            if (!d.DaNhacTruoc)
            {
                d.DaNhacTruoc = true;
                await tb.GuiAsync(await PhuTrachAsync(d.HeThong), LoaiThongBao.DienTap, $"Đến kỳ diễn tập downtime {d.HeThong?.Ten} (hạn {d.HanThucHien:dd/MM/yyyy})", d.LyDo, $"/DienTap/Details/{d.Id}");
            }
            var cap = Cap(d.HanThucHien);
            if (cap <= d.CapNhacQuaHan) continue;
            var nhan = await PhuTrachAsync(d.HeThong);
            if (cap >= 2) nhan.AddRange(NguoiNhanCap2());
            if (cap >= 3) nhan.AddRange(await LanhDaoAsync());
            d.CapNhacQuaHan = cap;
            await tb.GuiAsync(nhan, LoaiThongBao.DienTap, $"[Quá hạn{(cap > 1 ? $" – cấp {cap}" : "")}] Diễn tập downtime {d.HeThong?.Ten} (hạn {d.HanThucHien:dd/MM/yyyy})",
                "Kế hoạch downtime chưa được diễn tập đúng hạn.", $"/DienTap/Details/{d.Id}", cap >= 2 ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao);
        }
        foreach (var l in await db.LanThuPhucHois.Include(x => x.HeThong).Where(x => !x.DaChot).ToListAsync(ct))
        {
            if (!l.DaNhacTruoc)
            {
                l.DaNhacTruoc = true;
                await tb.GuiAsync(await PhuTrachAsync(l.HeThong), LoaiThongBao.DienTap, $"Đến kỳ thử phục hồi {l.HeThong?.Ten} (hạn {l.HanThucHien:dd/MM/yyyy})", l.LyDo, $"/SaoLuu/PhucHoi/{l.Id}");
            }
            var cap = Cap(l.HanThucHien);
            if (cap <= l.CapNhacQuaHan) continue;
            var nhan = await PhuTrachAsync(l.HeThong);
            if (cap >= 2) nhan.AddRange(NguoiNhanCap2());
            if (cap >= 3) nhan.AddRange(await LanhDaoAsync());
            l.CapNhacQuaHan = cap;
            await tb.GuiAsync(nhan, LoaiThongBao.DienTap, $"[Quá hạn{(cap > 1 ? $" – cấp {cap}" : "")}] Thử phục hồi {l.HeThong?.Ten} (hạn {l.HanThucHien:dd/MM/yyyy})",
                "Chưa chứng minh được khả năng phục hồi dữ liệu đúng chu kỳ.", $"/SaoLuu/PhucHoi/{l.Id}", cap >= 2 ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao);
        }
    }

    /// <summary>FR-SL-04: mốc sao lưu lỗi/không rõ → cảnh báo; lỗi liên tiếp ≥ N với hệ thống Tối quan trọng → tự tạo phiếu sửa chữa ưu tiên Cao.</summary>
    private async Task SaoLuuAsync(DateTime now, CancellationToken ct)
    {
        var nguong = thamSo.SoNguyen(TS.JciSaoLuuLoiTaoPhieu);
        var ds = await db.TacVuSaoLuus.Include(t => t.HeThong).Include(t => t.TaiSan).ThenInclude(t => t!.KhoaPhong)
            .Where(t => t.DangApDung && t.HeThong!.TrangThai == TrangThaiHeThong.VanHanh).ToListAsync(ct);
        foreach (var tv in ds)
        {
            var tu = now.AddHours(-Math.Max(24 * 7, tv.ChuKyGio * (nguong + 2)));
            var moc = await haTang.MocSaoLuuAsync(tv, tu, now, now);
            var loi = DoiChieuSaoLuu.LoiLienTiep(moc);
            if (loi == 0) { tv.SoLanLoiDaCanhBao = 0; continue; }
            if (loi <= tv.SoLanLoiDaCanhBao) continue;
            tv.SoLanLoiDaCanhBao = loi;
            var cuoi = moc.Last(m => m.TrangThai != TrangThaiMocSaoLuu.ChuaDenHan);
            await tb.GuiAsync(await PhuTrachAsync(tv.HeThong), LoaiThongBao.SaoLuu,
                $"Sao lưu {(cuoi.TrangThai == TrangThaiMocSaoLuu.Loi ? "lỗi" : "không rõ kết quả")}: {tv.HeThong?.Ten} – {tv.Ten} ({cuoi.Moc:HH:mm dd/MM})",
                loi > 1 ? $"{loi} lần liên tiếp không có bản sao lưu thành công." : cuoi.KetQua?.GhiChu, $"/SaoLuu/TacVu/{tv.Id}",
                tv.HeThong?.LaTrongYeu == true ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao, guiNhomCNTT: tv.HeThong?.LaTrongYeu == true);
            if (nguong > 0 && loi == nguong && tv.HeThong?.MucTrongYeu == MucTrongYeu.ToiQuanTrong && tv.TaiSan is { } ts)
            {
                var p = new PhieuSuaChua
                {
                    TaiSanId = ts.Id, KhoaPhongId = ts.KhoaPhongId, NguoiYeuCau = "system", NguoiTao = "system",
                    MoTaSuCo = $"[Sao lưu lỗi {loi} lần liên tiếp] {tv.HeThong.Ten} – {tv.Ten} ({tv.DoiTuong})",
                    UuTien = MucDoUuTien.Cao, NgayYeuCau = now, HanXuLy = thamSo.TinhHanSla(now, MucDoUuTien.Cao, ts.KhoaPhong?.LaKhoaTrongYeu == true)
                };
                db.PhieuSuaChuas.Add(p);
                await db.LuuMoiAsync(soPhieu, "SC", so => p.SoPhieu = so, ct);
                lichSu.Ghi(ts.Id, LoaiSuKien.SuaChua, $"Sao lưu lỗi liên tiếp → tự tạo {p.SoPhieu}", "system");
                await tb.PhieuMoiAsync(p, ts.KhoaPhong?.Ten, $"{ts.MaTaiSan} {ts.Ten}");
            }
        }
    }

    /// <summary>FR-HD-03: nhắc trước hạn, leo thang khi quá hạn (cấp 1 ngay, cấp 2 sau 7 ngày, cấp 3 sau 30 ngày).</summary>
    private async Task HanhDongAsync(DateOnly homNay, CancellationToken ct)
    {
        var truoc = thamSo.SoNguyen(TS.JciHanhDongNhacTruocNgay);
        var c2 = thamSo.SoNguyen(TS.JciLeoThangDienTap2Ngay); var c3 = thamSo.SoNguyen(TS.JciLeoThangDienTap3Ngay);
        var ds = await db.HanhDongKhacPhucs.Where(h => h.TrangThai == TrangThaiHanhDong.Mo || h.TrangThai == TrangThaiHanhDong.DangLam).ToListAsync(ct);
        foreach (var h in ds)
        {
            var tre = homNay.DayNumber - h.Han.DayNumber;
            if (tre < 0)
            {
                if (truoc > 0 && !h.DaNhacTruocHan && -tre <= truoc)
                {
                    h.DaNhacTruocHan = true;
                    await tb.GuiAsync([h.NguoiChiuTrachNhiem], LoaiThongBao.HanhDong, $"Hành động {h.Ma} sắp đến hạn ({h.Han:dd/MM/yyyy})", h.MoTa, $"/HanhDong/Details/{h.Id}", MucDoThongBao.CanhBao);
                }
                continue;
            }
            if (tre == 0) continue;
            var cap = c3 > 0 && tre >= c3 ? 3 : c2 > 0 && tre >= c2 ? 2 : 1;
            if (cap <= h.CapLeoThang) continue;
            var nhan = new List<string> { h.NguoiChiuTrachNhiem };
            if (cap >= 2) nhan.AddRange(NguoiNhanCap2());
            if (cap >= 3) nhan.AddRange(await LanhDaoAsync());
            h.CapLeoThang = cap;
            await tb.GuiAsync(nhan, LoaiThongBao.HanhDong, $"[Quá hạn{(cap > 1 ? $" – cấp {cap}" : "")}] Hành động {h.Ma} (hạn {h.Han:dd/MM/yyyy})",
                $"{h.MoTa}\nPhụ trách: {h.NguoiChiuTrachNhiem}", $"/HanhDong/Details/{h.Id}", cap >= 2 ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao);
        }
    }
}

/// <summary>Dòng bản tin hằng ngày của phân hệ hạ tầng CNTT.</summary>
public static class HaTangTongHop
{
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var db = (AppDbContext)sp.GetService(typeof(AppDbContext))!;
        var d = DateOnly.FromDateTime(now);
        var dong = new List<string>();
        var dangNgung = await db.SuKienDowntimes.CountAsync(s => s.TrangThai == TrangThaiSuKien.DangDienRa, ct);
        if (dangNgung > 0) dong.Add($"• {dangNgung} sự kiện downtime đang diễn ra");
        var choDong = await db.SuKienDowntimes.CountAsync(s => s.TrangThai == TrangThaiSuKien.DaKhoiPhuc, ct);
        if (choDong > 0) dong.Add($"• {choDong} sự kiện downtime đã khôi phục, chờ nhập bù / rút kinh nghiệm để đóng");
        var dt = await db.DotDienTaps.CountAsync(x => !x.DaChot && x.HanThucHien < d, ct);
        if (dt > 0) dong.Add($"• {dt} đợt diễn tập downtime quá hạn");
        var ph = await db.LanThuPhucHois.CountAsync(x => !x.DaChot && x.HanThucHien < d, ct);
        if (ph > 0) dong.Add($"• {ph} lần thử phục hồi quá hạn");
        var rs = await db.KeHoachDowntimes.CountAsync(k => k.TrangThai == TrangThaiKeHoach.HieuLuc && k.NgayRaSoat <= d, ct);
        if (rs > 0) dong.Add($"• {rs} kế hoạch downtime đến hạn rà soát");
        var sl = await db.TacVuSaoLuus.CountAsync(t => t.DangApDung && t.SoLanLoiDaCanhBao > 0, ct);
        if (sl > 0) dong.Add($"• {sl} tác vụ sao lưu đang lỗi / không rõ kết quả");
        var hd = await db.HanhDongKhacPhucs.CountAsync(h => (h.TrangThai == TrangThaiHanhDong.Mo || h.TrangThai == TrangThaiHanhDong.DangLam) && h.Han < d, ct);
        if (hd > 0) dong.Add($"• {hd} hành động khắc phục quá hạn");
        // BR-HTT-02: hệ thống trọng yếu thiếu kế hoạch downtime hiệu lực / chưa khai báo sao lưu
        var thieu = await db.HeThongThongTins.Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao)
            && (!db.KeHoachDowntimes.Any(k => k.HeThongId == h.Id && k.TrangThai == TrangThaiKeHoach.HieuLuc) || !db.TacVuSaoLuus.Any(t => t.HeThongId == h.Id && t.DangApDung)))
            .Select(h => h.Ma).ToListAsync(ct);
        if (thieu.Count > 0) dong.Add($"• Hệ thống trọng yếu thiếu kế hoạch downtime / sao lưu: {string.Join(", ", thieu)}");
        return dong;
    }
}
