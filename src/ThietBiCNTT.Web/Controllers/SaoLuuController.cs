using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public record MocSl(DateTime Moc, TrangThaiMocSaoLuu TrangThai, KetQuaSaoLuuLan? KetQua);
public record DongSaoLuu(TacVuSaoLuu TacVu, List<MocSl> Moc, double? TyLe, int Rpo);

/// <summary>Phân hệ 4.3 – Sao lưu & thử phục hồi (HCT.01.04 ME5, HCT.01.05 ME3, MOI.01.02). FR-SL-01..09.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class SaoLuuController(AppDbContext db, HaTangService haTang, HanhDongService hanhDong, ThamSoService thamSo, TepService tep) : Controller
{
    private static int Xep(TrangThaiMocSaoLuu t) => t switch { TrangThaiMocSaoLuu.Loi => 0, TrangThaiMocSaoLuu.KhongRo => 1, TrangThaiMocSaoLuu.CanhBao => 2, TrangThaiMocSaoLuu.ThanhCong => 3, _ => 4 };

    /// <summary>FR-SL-08: ma trận hệ thống × ngày.</summary>
    public async Task<IActionResult> Index(int ngay = 30)
    {
        ngay = Math.Clamp(ngay, 7, 60);
        var now = DateTime.Now; var tu = now.Date.AddDays(-(ngay - 1));
        var tvs = await db.TacVuSaoLuus.AsNoTracking().Include(t => t.HeThong).Where(t => t.DangApDung && t.HeThong!.TrangThai != TrangThaiHeThong.NgungSuDung).ToListAsync();
        var dong = new List<DongSaoLuu>();
        foreach (var t in tvs.OrderBy(t => t.HeThong!.MucTrongYeu).ThenBy(t => t.HeThong!.Ma).ThenBy(t => t.Ten))
        {
            var moc = (await haTang.MocSaoLuuAsync(t, tu, now, now)).Select(m => new MocSl(m.Moc, m.TrangThai, m.KetQua)).ToList();
            dong.Add(new DongSaoLuu(t, moc, DoiChieuSaoLuu.TyLeThanhCong(moc.Select(m => (m.Moc, m.TrangThai, m.KetQua))), thamSo.Rpo(t.HeThong!)));
        }
        // Lần thử phục hồi gần nhất đã chốt, theo hệ thống
        ViewBag.PhucHoi = (await db.LanThuPhucHois.AsNoTracking().Where(l => l.DaChot).OrderByDescending(l => l.NgayChot).ToListAsync())
            .GroupBy(l => l.HeThongId).ToDictionary(g => g.Key, g => g.First());
        ViewBag.ChuaChungMinh = await ChuaChungMinhAsync();
        ViewBag.Tu = tu; ViewBag.Ngay = ngay; ViewBag.Now = now;
        ViewBag.TrongYeuThieu = await db.HeThongThongTins.AsNoTracking()
            .Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao) && !db.TacVuSaoLuus.Any(t => t.HeThongId == h.Id && t.DangApDung))
            .OrderBy(h => h.MucTrongYeu).ThenBy(h => h.Ma).ToListAsync();
        return View(dong);
    }

    /// <summary>BR-SL-02: hệ thống Tối quan trọng/Cao không có lần thử phục hồi Đạt trong chu kỳ quy định.</summary>
    private async Task<List<HeThongThongTin>> ChuaChungMinhAsync()
    {
        var hts = await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao)).ToListAsync();
        var datCuoi = (await db.LanThuPhucHois.AsNoTracking().Where(l => l.DaChot && l.Dat == true).ToListAsync()).GroupBy(l => l.HeThongId).ToDictionary(g => g.Key, g => g.Max(x => x.NgayChot!.Value));
        var kq = new List<HeThongThongTin>();
        foreach (var h in hts)
        {
            var thang = thamSo.MucTieu(h.MucTrongYeu).ChuKyThuPhucHoiThang;
            if (thang <= 0) continue;
            if (!datCuoi.TryGetValue(h.Id, out var d) || d.AddMonths(thang) < DateTime.Now) kq.Add(h);
        }
        return kq;
    }

    public async Task<IActionResult> TacVu(int id)
    {
        var t = await db.TacVuSaoLuus.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiSan).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        var now = DateTime.Now;
        ViewBag.Moc = (await haTang.MocSaoLuuAsync(t, now.Date.AddDays(-14), now, now)).Select(m => new MocSl(m.Moc, m.TrangThai, m.KetQua)).OrderByDescending(m => m.Moc).ToList();
        ViewBag.KetQuas = await db.KetQuaSaoLuuLans.AsNoTracking().Where(k => k.TacVuSaoLuuId == id).OrderByDescending(k => k.ThoiDiem).Take(40).ToListAsync();
        ViewBag.Rpo = thamSo.Rpo(t.HeThong!); ViewBag.Now = now;
        return View(t);
    }

    private async Task NapFormAsync(TacVuSaoLuu t)
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung).OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", t.HeThongId);
        ViewBag.DsMayChu = new SelectList(await db.TaiSans.Where(x => x.LoaiThietBi!.Nhom == NhomThietBi.MayChu && x.TrangThai != TrangThaiTaiSan.DaThanhLy).OrderBy(x => x.MaTaiSan)
            .Select(x => new { x.Id, Ten = x.MaTaiSan + " – " + x.Ten }).ToListAsync(), "Id", "Ten", t.TaiSanId);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> TacVuForm(int? id, int? heThongId)
    {
        var t = id is null ? new TacVuSaoLuu { HeThongId = heThongId ?? 0 } : await db.TacVuSaoLuus.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        await NapFormAsync(t);
        ViewBag.Rpo = t.HeThongId > 0 ? thamSo.Rpo((await db.HeThongThongTins.FindAsync(t.HeThongId))!) : (int?)null;
        return View(t);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> TacVuForm(TacVuSaoLuu input)
    {
        ModelState.Remove(nameof(input.HeThong)); ModelState.Remove(nameof(input.TaiSan));
        if (!await db.HeThongThongTins.AnyAsync(h => h.Id == input.HeThongId)) ModelState.AddModelError(nameof(input.HeThongId), "Chọn hệ thống.");
        if (!ModelState.IsValid) { await NapFormAsync(input); return View(input); }
        if (input.Id == 0) db.TacVuSaoLuus.Add(input);
        else
        {
            var t = await db.TacVuSaoLuus.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = t.NgayTao; input.NguoiTao = t.NguoiTao; input.SoLanLoiDaCanhBao = t.SoLanLoiDaCanhBao;
            db.Entry(t).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu tác vụ sao lưu {input.Ten}.";
        return RedirectToAction(nameof(TacVu), new { id = input.Id });
    }

    /// <summary>FR-SL-02: ghi nhận kết quả một lần sao lưu (nhập tay).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GhiKetQua(int id, DateTime thoiDiem, KetQuaSaoLuu ketQua, decimal? dungLuongGb, int? thoiLuongPhut, string? ghiChu)
    {
        var tv = await db.TacVuSaoLuus.FirstOrDefaultAsync(x => x.Id == id);
        if (tv is null) return NotFound();
        if (thoiDiem > DateTime.Now.AddMinutes(5)) { TempData["err"] = "Thời điểm chạy ở tương lai."; return RedirectToAction(nameof(TacVu), new { id }); }
        if (ketQua != KetQuaSaoLuu.ThanhCong && string.IsNullOrWhiteSpace(ghiChu)) { TempData["err"] = "Kết quả có cảnh báo / lỗi cần ghi chú nguyên nhân."; return RedirectToAction(nameof(TacVu), new { id }); }
        db.KetQuaSaoLuuLans.Add(new KetQuaSaoLuuLan { TacVuSaoLuuId = id, ThoiDiem = thoiDiem, KetQua = ketQua, DungLuongGb = dungLuongGb, ThoiLuongPhut = thoiLuongPhut,
            GhiChu = ghiChu?.Trim(), Nguon = NguonKetQua.NhapTay, NguoiGhi = User.Identity!.Name, ThoiGianGhi = DateTime.Now });
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã ghi kết quả sao lưu.";
        return RedirectToAction(nameof(TacVu), new { id });
    }

    // ===== Thử phục hồi (FR-SL-05..07) =====

    public async Task<IActionResult> PhucHoiDs(bool daChot = false)
    {
        var q = db.LanThuPhucHois.AsNoTracking().Include(l => l.HeThong).Where(l => l.DaChot == daChot);
        ViewData["daChot"] = daChot; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.ChuaChungMinh = await ChuaChungMinhAsync();
        return View(await (daChot ? q.OrderByDescending(l => l.NgayChot) : q.OrderBy(l => l.HanThucHien)).Take(300).ToListAsync());
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> PhucHoiTao(int? heThongId)
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung).OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThongId);
        return View(new LanThuPhucHoi { HeThongId = heThongId ?? 0, HanThucHien = DateOnly.FromDateTime(DateTime.Today).AddDays(14) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> PhucHoiTao(int heThongId, DateOnly hanThucHien, string? lyDo)
    {
        if (!await db.HeThongThongTins.AnyAsync(h => h.Id == heThongId)) return NotFound();
        if (await db.LanThuPhucHois.AnyAsync(l => l.HeThongId == heThongId && !l.DaChot)) { TempData["err"] = "Hệ thống đã có lần thử phục hồi chưa chốt."; return RedirectToAction(nameof(PhucHoiDs)); }
        var l = new LanThuPhucHoi { HeThongId = heThongId, HanThucHien = hanThucHien, LyDo = string.IsNullOrWhiteSpace(lyDo) ? "Thử phục hồi theo yêu cầu" : lyDo.Trim() };
        db.LanThuPhucHois.Add(l);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(PhucHoi), new { id = l.Id });
    }

    public async Task<IActionResult> PhucHoi(int id)
    {
        var l = await db.LanThuPhucHois.AsNoTracking().Include(x => x.HeThong).Include(x => x.TacVuSaoLuu).FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        ViewBag.Rto = thamSo.Rto(l.HeThong!); ViewBag.Rpo = thamSo.Rpo(l.HeThong!);
        ViewBag.TacVus = new SelectList(await db.TacVuSaoLuus.Where(t => t.HeThongId == l.HeThongId).OrderBy(t => t.Ten).Select(t => new { t.Id, t.Ten }).ToListAsync(), "Id", "Ten", l.TacVuSaoLuuId);
        ViewBag.Tep = await tep.DanhSachAsync("LanThuPhucHoi", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.ThuPhucHoi && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(l);
    }

    private async Task<IActionResult> ThucHien(int id, Func<LanThuPhucHoi, Task<string>> thaoTac)
    {
        if (!User.LaCNTT()) return Forbid();
        var l = await db.LanThuPhucHois.Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        try { TempData["ok"] = await thaoTac(l); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(PhucHoi), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhucHoiGhi(int id, int? tacVuId, string? banSaoLuu, DateTime? thoiDiemDuLieu, DateTime? thoiDiemSuCo, string? moiTruong, DateTime? batDau, DateTime? ketThuc,
        string? kiemTraToanVen, string? nguoiXacNhan, bool toanVenDat) =>
        ThucHien(id, l => { l.Ghi(tacVuId, banSaoLuu, thoiDiemDuLieu, thoiDiemSuCo, moiTruong, batDau, ketThuc, kiemTraToanVen, nguoiXacNhan, toanVenDat); return Task.FromResult("Đã lưu biên bản thử phục hồi (chưa chốt)."); });

    /// <summary>FR-SL-06/07: chốt biên bản; không đạt → bắt buộc CAPA + lên lịch thử lại.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhucHoiChot(int id) => ThucHien(id, async l =>
    {
        var now = DateTime.Now;
        var dat = l.Chot(thamSo.Rto(l.HeThong!), thamSo.Rpo(l.HeThong!), User.Identity!.Name!, now);
        if (dat) return $"Đã chốt — ĐẠT (RTO {l.RtoDatPhut:0.#}/{l.RtoMucTieu} phút, RPO {l.RpoDatPhut:0.#}/{l.RpoMucTieu} phút).";
        var ly = string.Join("; ", l.LyDoKhongDat());
        var h = await hanhDong.TaoAsync(NguonHanhDong.ThuPhucHoi, l.Id, $"Khắc phục thử phục hồi {l.HeThong!.Ma} không đạt: {ly}", l.HeThongId, l.HeThong.PhuTrachChinh ?? User.Identity!.Name!,
            null, LoaiHanhDong.KhacPhuc, MucDoUuTien.Cao, User.Identity!.Name!);
        db.LanThuPhucHois.Add(new LanThuPhucHoi { HeThongId = l.HeThongId, NguoiTao = User.Identity!.Name,
            HanThucHien = DateOnly.FromDateTime(now).AddDays(thamSo.SoNguyen(TS.JciPhucHoiLaiNgay)), LyDo = $"Thử lại sau lần không đạt ({l.HanThucHien:dd/MM/yyyy})" });
        return $"Đã chốt — KHÔNG ĐẠT ({ly}). Đã tạo hành động khắc phục {h.Ma} và lên lịch thử lại trong {thamSo.SoNguyen(TS.JciPhucHoiLaiNgay)} ngày.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhucHoiPhuLuc(int id, string noiDung) => ThucHien(id, l => { l.ThemPhuLuc(noiDung, User.Identity!.Name!, DateTime.Now); return Task.FromResult("Đã thêm phụ lục."); });
}
