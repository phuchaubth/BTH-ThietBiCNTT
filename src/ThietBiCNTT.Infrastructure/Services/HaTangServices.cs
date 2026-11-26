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
        var buoc = TimeSpan.FromMinutes(Math.Max(5, tv.ChuKyPhut));
        // Không tính các mốc trước khi tác vụ tồn tại (ngày khai báo hoặc kết quả đầu tiên đã ghi) — tránh cảnh báo "không rõ" giả.
        var dauTien = await db.KetQuaSaoLuuLans.AsNoTracking().Where(k => k.TacVuSaoLuuId == tv.Id).MinAsync(k => (DateTime?)k.ThoiDiem);
        var batDau = dauTien is { } d && d < tv.NgayTao ? d : tv.NgayTao;
        if (tu < batDau) tu = batDau;
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
        // HCT.01.05 ME1: chương trình ứng phó tấn công mạng phải được thử ít nhất mỗi năm
        var thang = thamSo.SoNguyen(TS.JciThuUngPhoMangThang);
        foreach (var chuongTrinh in await db.TaiLieuQuyDinhs.Where(t => t.Loai == LoaiTaiLieu.ChuongTrinhUngPhoMang && t.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync(ct))
        {
            if (await db.DotDienTaps.AnyAsync(d => d.LoaiThuNghiem == LoaiThuNghiem.UngPhoTanCong && !d.DaChot, ct)) continue;
            var cuoi = await db.DotDienTaps.Where(d => d.LoaiThuNghiem == LoaiThuNghiem.UngPhoTanCong && d.DaChot).OrderByDescending(d => d.NgayChot).Select(d => d.NgayThucHien ?? d.NgayChot).FirstOrDefaultAsync(ct);
            var han = (cuoi is { } c ? DateOnly.FromDateTime(c) : chuongTrinh.NgayHieuLuc ?? homNay).AddMonths(thang);
            if (han.AddDays(-nhacDt) > homNay) continue;
            db.DotDienTaps.Add(new DotDienTap { LoaiThuNghiem = LoaiThuNghiem.UngPhoTanCong, TaiLieuId = chuongTrinh.Id, HanThucHien = han, NguoiTao = "system", LyDo = $"Thử chương trình ứng phó tấn công mạng định kỳ {thang} tháng" });
            dem++;
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
        (NguonHanhDong.SuCoAnNinh, { } i) => $"/AnNinh/Details/{i}",
        (NguonHanhDong.RuiRo, { } i) => $"/RuiRo/Details/{i}",
        (NguonHanhDong.BaoCaoLanhDao, { } i) => $"/BaoCaoLanhDao/Details/{i}",
        (NguonHanhDong.PhieuThayDoi, { } i) => $"/PhieuThayDoi/Details/{i}",
        _ => ""
    };
}

/// <summary>
/// Chạy nền mỗi 5 phút: sinh lịch diễn tập/thử phục hồi, thông báo downtime có kế hoạch,
/// cảnh báo sao lưu thiếu/lỗi (FR-SL-04), nhắc & leo thang diễn tập/thử phục hồi quá hạn (BR-DT-06), hành động khắc phục (FR-HD-03).
/// </summary>
public class HaTangCanhBaoService(AppDbContext db, HaTangService haTang, ThongBaoService tb, ThamSoService thamSo, UserManager<AppUser> users,
    SoPhieuService soPhieu, LichSuService lichSu, RaSoatQuyenService raSoatQuyen, GoiYRuiRoService goiY)
{
    public async Task ChayAsync(DateTime now, CancellationToken ct = default)
    {
        var homNay = DateOnly.FromDateTime(now);
        await haTang.SinhLichAsync(homNay, ct);
        await DowntimeCoKeHoachAsync(now, ct);
        await DienTapPhucHoiAsync(homNay, ct);
        await RaSoatKeHoachAsync(homNay, ct);
        await SaoLuuAsync(now, ct);
        await HanhDongAsync(homNay, ct);
        await GD32Async(now, homNay, ct);
        await GD33Async(now, homNay, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Chống thông báo lặp: chỉ gửi nếu trong <paramref name="ngay"/> ngày qua chưa có thông báo cùng tiêu đề.</summary>
    private async Task<bool> ChuaThongBaoAsync(string tieuDe, int ngay, CancellationToken ct)
    {
        var tu = DateTime.Now.AddDays(-ngay);
        return !await db.ThongBaos.AnyAsync(t => t.TieuDe == tieuDe && t.ThoiGian >= tu, ct) && !db.ThongBaos.Local.Any(t => t.TieuDe == tieuDe && t.ThoiGian >= tu);
    }

    /// <summary>GĐ3.3: thay đổi khẩn cấp quá hạn duyệt bổ sung (BR-TD-02), đánh giá sau triển khai đến hạn (HCT.01.00 ME4).</summary>
    private async Task GD33Async(DateTime now, DateOnly homNay, CancellationToken ct)
    {
        // FR-TC-01: sinh đợt rà soát quyền khi đến chu kỳ; nhắc đợt quá hạn
        await raSoatQuyen.SinhDotAsync(ct);
        await goiY.SinhAsync(homNay, ct);
        await goiY.NhacAsync(now, homNay, ct);
        await db.SaveChangesAsync(ct);
        var cnttQ = await tb.CanBoCNTTAsync();
        foreach (var d in await db.DotRaSoatQuyens.Include(x => x.HeThong).Where(x => x.TrangThai != TrangThaiRaSoat.DaDong && !x.DaNhacQuaHan && x.Han < homNay).ToListAsync(ct))
        {
            d.DaNhacQuaHan = true;
            await tb.GuiAsync(cnttQ, LoaiThongBao.RaSoatQuyen, $"[Quá hạn] Rà soát quyền {d.HeThong?.Ma} ({d.Ma})", $"Hạn {d.Han:dd/MM/yyyy}.", $"/RaSoatQuyen/Details/{d.Id}", MucDoThongBao.CanhBao);
        }
        // FR-TC-06: việc thu hồi quá hạn
        foreach (var n in await db.ThongBaoNghiViecs.Include(x => x.Viecs).Where(x => !x.DaLeoThang && x.Han < now).ToListAsync(ct))
        {
            if (n.Xong) continue;
            n.DaLeoThang = true;
            await tb.GuiAsync(cnttQ.Concat(thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).Distinct(), LoaiThongBao.RaSoatQuyen, $"[Quá hạn] Thu hồi quyền của {n.HoTen}", $"Hạn {n.Han:dd/MM/yyyy HH:mm}, còn {n.Viecs.Count(v => !v.DaThucHien)} việc.", $"/NghiViec/Details/{n.Id}", MucDoThongBao.KhanCap);
        }
        var duyet = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        foreach (var p in await db.PhieuThayDois.Where(x => x.Loai == LoaiThayDoi.KhanCap && x.NguoiDuyetCntt == null && !x.DaLeoThang && x.HanDuyetBoSung != null && x.HanDuyetBoSung < now &&
            (x.TrangThai == TrangThaiThayDoi.DangThucHien || x.TrangThai == TrangThaiThayDoi.HoanThanh || x.TrangThai == TrangThaiThayDoi.ThatBai || x.TrangThai == TrangThaiThayDoi.DaQuayLui)).ToListAsync(ct))
        {
            p.DaLeoThang = true;
            await tb.GuiAsync(duyet, LoaiThongBao.ThayDoi, $"[Quá hạn] Thay đổi khẩn cấp {p.Ma} chưa duyệt bổ sung", $"Hạn {p.HanDuyetBoSung:dd/MM/yyyy}. BR-TD-02: leo thang Trưởng phòng CNTT.", $"/PhieuThayDoi/Details/{p.Id}", MucDoThongBao.KhanCap);
        }
        foreach (var p in await db.PhieuThayDois.Include(x => x.DanhGiaSau).Where(x => x.DanhGiaSau != null && x.DanhGiaSau.NgayDanhGia == null && !x.DanhGiaSau.DaNhac && x.DanhGiaSau.Han <= homNay).ToListAsync(ct))
        {
            p.DanhGiaSau!.DaNhac = true;
            await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.ThayDoi, $"Đến hạn đánh giá sau triển khai: {p.Ten} ({p.Ma})", "HCT.01.00 ME4: đánh giá tính dễ dùng, đúng mục đích, an toàn người bệnh.", $"/PhieuThayDoi/Details/{p.Id}", MucDoThongBao.CanhBao);
        }
    }

    /// <summary>GĐ3.2: nhắc rà soát tài liệu, xem xét công cụ AI, thử nguồn dự phòng, báo cáo cơ quan chức năng, báo cáo quý, rủi ro.</summary>
    private async Task GD32Async(DateTime now, DateOnly homNay, CancellationToken ct)
    {
        var cntt = await tb.CanBoCNTTAsync();
        var duyet = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        var attt = thamSo.Chuoi(TS.JciNhomAnToanThongTin).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        // 1. Tài liệu đến hạn rà soát (MOI.02.01, FMS.01.01 ME2)
        foreach (var t in await db.TaiLieuQuyDinhs.Where(x => x.TrangThai == TrangThaiKeHoach.HieuLuc && !x.DaNhacRaSoat).ToListAsync(ct))
        {
            if (!t.DenHanRaSoat(homNay)) continue;
            t.DaNhacRaSoat = true;
            await tb.GuiAsync(duyet.Concat(cntt).Distinct(), LoaiThongBao.HeThong, $"Đến hạn rà soát {t.Loai.TenHienThi()}: {t.Ten} v{t.PhienBan}", $"Hạn rà soát {t.NgayRaSoat:dd/MM/yyyy}.", $"/TaiLieu/Details/{t.Id}", MucDoThongBao.CanhBao);
        }
        // 2. Công cụ AI đến hạn xem xét (HCT.01.03)
        foreach (var h in await db.HeThongThongTins.Where(x => x.LaCongCuAi && x.TrangThai == TrangThaiHeThong.VanHanh && x.NgayXemXetAi != null && x.NgayXemXetAi <= homNay).ToListAsync(ct))
        {
            var td = $"Đến hạn xem xét công cụ AI {h.Ma}";
            if (await ChuaThongBaoAsync(td, 7, ct)) await tb.GuiAsync(await PhuTrachAsync(h), LoaiThongBao.HeThong, td, "Giám sát mức sử dụng, sai lệch, cập nhật và khía cạnh đạo đức; ghi bước Giám sát / Cải tiến.", $"/HeThong/Details/{h.Id}", MucDoThongBao.CanhBao);
        }
        // 3. Thử nguồn điện dự phòng quá hạn (FMS.08.01 ME3): nhắc phòng thực hiện, quá 7 ngày thông báo thêm Phòng CNTT
        var thang = thamSo.SoNguyen(TS.JciThuNguonDuPhongThang);
        foreach (var m in await db.MauKiemTras.Include(x => x.KhoaPhong).Where(x => x.LaThuNguonDuPhong && x.DangApDung).ToListAsync(ct))
        {
            var cuoi = await db.LanKiemTras.Where(l => l.MauKiemTraId == m.Id).MaxAsync(l => (DateTime?)l.ThoiGian, ct);
            var han = (cuoi ?? m.NgayTao).AddHours(Math.Min(m.ChuKyGio, thang * 31 * 24));
            if (han >= now) continue;
            var td = $"Quá hạn thử nguồn điện dự phòng: {m.Ten}";
            if (!await ChuaThongBaoAsync(td, 7, ct)) continue;
            var nhan = new List<string?>(await tb.TruongKhoaAsync(m.KhoaPhongId));
            if (now - han > TimeSpan.FromDays(7) || m.KhoaPhongId is null) nhan.AddRange(cntt);
            await tb.GuiAsync(nhan, LoaiThongBao.KiemTraBatThuong, td, $"Hạn thử {han:dd/MM/yyyy}. FMS.08.01 ME3: thử nguồn thay thế ít nhất mỗi quý và lưu kết quả.", "/TienIch", MucDoThongBao.CanhBao);
        }
        // 4. Sự cố an ninh mạng: sắp đến / quá hạn báo cáo cơ quan chức năng (FR-AN-04)
        var gio = thamSo.SoNguyen(TS.JciHanBaoCaoSuCoGio);
        if (gio > 0)
            foreach (var s in await db.SuCoAnNinhs.Where(x => x.TrangThai != TrangThaiSuCoAn.DaDong && !x.DaBaoCaoCoQuan && !x.DaNhacBaoCao && x.LyDoKhongBaoCao == null).ToListAsync(ct))
                if (s.HanBaoCao(gio) is { } hn && hn - now <= TimeSpan.FromHours(Math.Min(6, gio)))
                {
                    s.DaNhacBaoCao = true;
                    await tb.GuiAsync(attt.Concat(duyet).Distinct(), LoaiThongBao.AnNinhMang, $"{(hn < now ? "[Quá hạn] " : "")}Báo cáo cơ quan chức năng sự cố {s.Ma}", $"Hạn {hn:HH:mm dd/MM/yyyy}.", $"/AnNinh/Details/{s.Id}", MucDoThongBao.KhanCap);
                }
        // 5. Báo cáo quý cho Lãnh đạo (GLD.04.01 ME2)
        var nhacNgay = thamSo.SoNguyen(TS.JciBaoCaoQuyNhacNgay);
        if (nhacNgay > 0)
        {
            var quyTruoc = BaoCaoLanhDao.TenKy(homNay.AddMonths(-3)); var (_, hetQuy) = BaoCaoLanhDao.KhoangQuy(int.Parse(quyTruoc[..4]), int.Parse(quyTruoc[^1..]));
            if (homNay > hetQuy.AddDays(nhacNgay) && !await db.BaoCaoLanhDaos.AnyAsync(b => b.Ky == quyTruoc && b.NgayGui != null, ct))
            {
                var td = $"Chưa gửi báo cáo hạ tầng CNTT quý {quyTruoc} cho Lãnh đạo";
                if (await ChuaThongBaoAsync(td, 7, ct)) await tb.GuiAsync(duyet.Concat(cntt).Distinct(), LoaiThongBao.BaoCaoLanhDao, td, "GLD.04.01 ME2: báo cáo bằng văn bản ít nhất hằng quý.", "/BaoCaoLanhDao", MucDoThongBao.CanhBao);
            }
            var cho = await db.BaoCaoLanhDaos.Where(b => b.NgayGui != null && b.NgayXem == null && b.NgayGui < now.AddDays(-nhacNgay)).ToListAsync(ct);
            foreach (var b in cho) if (await ChuaThongBaoAsync($"Báo cáo quý {b.Ky} chờ Lãnh đạo xác nhận đã xem", 7, ct)) { b.SoLanNhac++; await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.BaoCaoLanhDao, $"Báo cáo quý {b.Ky} chờ Lãnh đạo xác nhận đã xem", null, $"/BaoCaoLanhDao/Details/{b.Id}", MucDoThongBao.CanhBao); }
        }
        // 6. Rủi ro: quá hạn xem xét lại; BR-RR-04 từ 01/11 chưa có đợt đánh giá năm nay
        foreach (var r in await db.RuiRos.Where(x => (x.TrangThai == TrangThaiRuiRo.Mo || x.TrangThai == TrangThaiRuiRo.DangXuLy || x.TrangThai == TrangThaiRuiRo.DaKiemSoat) && x.NgayXemXetLai != null && x.NgayXemXetLai < homNay).ToListAsync(ct))
        {
            var td = $"Rủi ro {r.Ma} quá hạn xem xét lại";
            if (await ChuaThongBaoAsync(td, 14, ct)) await tb.GuiAsync([r.NguoiChiuTrachNhiem], LoaiThongBao.RuiRo, td, r.MoTa.Length > 100 ? r.MoTa[..100] + "…" : r.MoTa, $"/RuiRo/Details/{r.Id}", MucDoThongBao.CanhBao);
        }
        if (homNay.Month >= 11 && !await db.DotDanhGiaRuiRos.AnyAsync(d => d.Nam == homNay.Year && d.DaChot, ct))
        {
            var td = $"Chưa hoàn tất đánh giá rủi ro CNTT năm {homNay.Year}";
            if (await ChuaThongBaoAsync(td, 14, ct)) await tb.GuiAsync(duyet.Concat(cntt).Distinct(), LoaiThongBao.RuiRo, td, "BR-RR-04 · FMS.02.00: cần chốt trước 31/12.", "/DotRuiRo", MucDoThongBao.CanhBao);
        }
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

    /// <summary>FR-DT-13 (HCT.01.04 – duy trì): kế hoạch downtime đến hạn rà soát hoặc có cờ cần rà soát → nhắc người phụ trách một lần.</summary>
    private async Task RaSoatKeHoachAsync(DateOnly homNay, CancellationToken ct)
    {
        foreach (var k in await db.KeHoachDowntimes.Include(x => x.HeThong).Where(x => x.TrangThai == TrangThaiKeHoach.HieuLuc && !x.DaNhacRaSoat).ToListAsync(ct))
        {
            if (!k.DenHanRaSoat(homNay)) continue;
            k.DaNhacRaSoat = true;
            await tb.GuiAsync(await PhuTrachAsync(k.HeThong), LoaiThongBao.DienTap, $"Rà soát kế hoạch downtime {k.HeThong?.Ma} v{k.PhienBan}",
                k.CanRaSoat ? k.LyDoRaSoat : $"Đến hạn rà soát định kỳ ({k.NgayRaSoat:dd/MM/yyyy}).", $"/KeHoachDowntime/Details/{k.Id}", MucDoThongBao.CanhBao);
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
                await tb.GuiAsync(await PhuTrachAsync(d.HeThong), LoaiThongBao.DienTap, $"Đến kỳ {(d.LoaiThuNghiem == LoaiThuNghiem.Downtime ? "diễn tập downtime" : "thử chương trình ứng phó tấn công mạng")} {d.HeThong?.Ten} (hạn {d.HanThucHien:dd/MM/yyyy})", d.LyDo, $"/DienTap/Details/{d.Id}");
            }
            var cap = Cap(d.HanThucHien);
            if (cap <= d.CapNhacQuaHan) continue;
            var nhan = await PhuTrachAsync(d.HeThong);
            if (cap >= 2) nhan.AddRange(NguoiNhanCap2());
            if (cap >= 3) nhan.AddRange(await LanhDaoAsync());
            d.CapNhacQuaHan = cap;
            await tb.GuiAsync(nhan, LoaiThongBao.DienTap, $"[Quá hạn{(cap > 1 ? $" – cấp {cap}" : "")}] {(d.LoaiThuNghiem == LoaiThuNghiem.Downtime ? "Diễn tập downtime" : "Thử chương trình ứng phó tấn công mạng")} {d.HeThong?.Ten} (hạn {d.HanThucHien:dd/MM/yyyy})",
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
            var tu = now.AddMinutes(-Math.Max(24 * 7 * 60, tv.ChuKyPhut * (nguong + 2)));
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
        var rs = await db.KeHoachDowntimes.CountAsync(k => k.TrangThai == TrangThaiKeHoach.HieuLuc && (k.CanRaSoat || k.NgayRaSoat <= d), ct);
        if (rs > 0) dong.Add($"• {rs} kế hoạch downtime đến hạn rà soát");
        var sl = await db.TacVuSaoLuus.CountAsync(t => t.DangApDung && t.SoLanLoiDaCanhBao > 0, ct);
        if (sl > 0) dong.Add($"• {sl} tác vụ sao lưu đang lỗi / không rõ kết quả");
        var anMoi = await db.SuCoAnNinhs.CountAsync(s => s.TrangThai == TrangThaiSuCoAn.Moi, ct);
        if (anMoi > 0) dong.Add($"• {anMoi} sự cố an ninh mạng mới chưa phân mức");
        var anMo = await db.SuCoAnNinhs.CountAsync(s => s.TrangThai != TrangThaiSuCoAn.DaDong && s.TrangThai != TrangThaiSuCoAn.Moi, ct);
        if (anMo > 0) dong.Add($"• {anMo} sự cố an ninh mạng đang xử lý");
        var tlHan = await db.TaiLieuQuyDinhs.CountAsync(t => t.TrangThai == TrangThaiKeHoach.HieuLuc && t.NgayRaSoat <= d, ct);
        if (tlHan > 0) dong.Add($"• {tlHan} chính sách / chương trình đến hạn rà soát");
        var aiHan = await db.HeThongThongTins.CountAsync(h => h.LaCongCuAi && h.TrangThai == TrangThaiHeThong.VanHanh && h.NgayXemXetAi <= d, ct);
        if (aiHan > 0) dong.Add($"• {aiHan} công cụ AI hỗ trợ quyết định đến hạn xem xét");
        var rrQuaHan = await db.RuiRos.CountAsync(r => (r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy) && r.NgayXemXetLai < d, ct);
        if (rrQuaHan > 0) dong.Add($"• {rrQuaHan} rủi ro CNTT quá hạn xem xét lại");
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
