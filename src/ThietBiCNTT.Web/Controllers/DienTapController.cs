using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

/// <summary>Phân hệ 4.2 – Diễn tập kế hoạch downtime (HCT.01.04: thử nghiệm). FR-DT-10..12.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class DienTapController(AppDbContext db, ThamSoService thamSo, HanhDongService hanhDong, TepService tep, UserManager<AppUser> users) : Controller
{
    public async Task<IActionResult> Index(bool daChot = false, int? heThong = null)
    {
        var q = db.DotDienTaps.AsNoTracking().Include(d => d.HeThong).Where(d => d.DaChot == daChot);
        if (heThong is not null) q = q.Where(d => d.HeThongId == heThong);
        var ds = await (daChot ? q.OrderByDescending(d => d.NgayChot) : q.OrderBy(d => d.HanThucHien)).Take(300).ToListAsync();
        ViewData["daChot"] = daChot; ViewData["heThong"] = heThong;
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThong);
        return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(int? heThongId)
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung).OrderBy(h => h.Ma)
            .Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThongId);
        return View(new DotDienTap { HeThongId = heThongId ?? 0, HanThucHien = DateOnly.FromDateTime(DateTime.Today).AddDays(30) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(int heThongId, DateOnly hanThucHien, string? lyDo)
    {
        if (!await db.HeThongThongTins.AnyAsync(h => h.Id == heThongId)) return NotFound();
        if (await db.DotDienTaps.AnyAsync(d => d.HeThongId == heThongId && !d.DaChot)) { TempData["err"] = "Hệ thống đã có đợt diễn tập chưa chốt."; return RedirectToAction(nameof(Index)); }
        var kh = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == heThongId && k.TrangThai == TrangThaiKeHoach.HieuLuc).OrderByDescending(k => k.PhienBan).FirstOrDefaultAsync();
        var d = new DotDienTap { HeThongId = heThongId, KeHoachDowntimeId = kh?.Id, HanThucHien = hanThucHien, LyDo = string.IsNullOrWhiteSpace(lyDo) ? "Diễn tập theo yêu cầu" : lyDo.Trim() };
        db.DotDienTaps.Add(d);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = d.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await db.DotDienTaps.AsNoTracking().Include(x => x.HeThong).Include(x => x.KeHoachDowntime).Include(x => x.ThayBangSuKien).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("DotDienTap", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.DienTap && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.SuKienThay = await db.SuKienDowntimes.AsNoTracking()
            .Where(s => s.HeThongs.Any(h => h.HeThongId == d.HeThongId) && s.TrangThai == TrangThaiSuKien.DaDong && s.NgayRutKinhNghiem != null)
            .OrderByDescending(s => s.BatDau).Take(20).ToListAsync();
        ViewBag.DuocDuyetCntt = User.LaDuyetCntt(thamSo);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(d);
    }

    private async Task<IActionResult> ThucHien(int id, Func<DotDienTap, Task<string>> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var d = await db.DotDienTaps.Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        try { TempData["ok"] = await thaoTac(d); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Ghi(int id, DateTime ngayThucHien, HinhThucDienTap hinhThuc, string? kichBan, string? khoaThamGia, string? nguoiQuanSat, string? dienBien, string? phatHien) =>
        ThucHien(id, d => { d.Ghi(ngayThucHien, hinhThuc, kichBan, khoaThamGia, nguoiQuanSat, dienBien, phatHien); return Task.FromResult("Đã lưu biên bản diễn tập (chưa chốt)."); });

    /// <summary>FR-DT-12: chốt kết luận. Không đạt → tự tạo hành động khắc phục và lên lịch diễn tập lại.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Chot(int id, KetLuanDienTap ketLuan) => ThucHien(id, async d =>
    {
        var now = DateTime.Now;
        var khongDat = d.Chot(ketLuan, User.Identity!.Name!, now);
        var msg = "Đã chốt biên bản diễn tập — không sửa được nữa (chỉ thêm phụ lục).";
        var kh = await db.KeHoachDowntimes.Where(k => k.HeThongId == d.HeThongId && k.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync();
        if (ketLuan != KetLuanDienTap.Dat) foreach (var k in kh) k.DanhDauCanRaSoat($"Diễn tập {d.HanThucHien:dd/MM/yyyy}: {ketLuan.Ten()} — {d.PhatHien}");
        if (khongDat)
        {
            var nguoi = d.HeThong!.PhuTrachChinh ?? User.Identity!.Name!;
            var h = await hanhDong.TaoAsync(NguonHanhDong.DienTap, d.Id, $"Khắc phục phát hiện diễn tập downtime {d.HeThong.Ma}: {d.PhatHien}", d.HeThongId, nguoi, null, LoaiHanhDong.KhacPhuc, MucDoUuTien.Cao, User.Identity!.Name!);
            db.DotDienTaps.Add(new DotDienTap { HeThongId = d.HeThongId, KeHoachDowntimeId = d.KeHoachDowntimeId, NguoiTao = User.Identity!.Name,
                HanThucHien = DateOnly.FromDateTime(now).AddDays(thamSo.SoNguyen(TS.JciDienTapLaiNgay)), LyDo = $"Diễn tập lại sau kết quả Không đạt ({d.HanThucHien:dd/MM/yyyy})" });
            msg += $" Kết luận Không đạt: đã tạo hành động {h.Ma} và lên lịch diễn tập lại trong {thamSo.SoNguyen(TS.JciDienTapLaiNgay)} ngày.";
        }
        return msg;
    });

    /// <summary>FR-DT-12: ghi nhận một downtime thực tế đã rút kinh nghiệm thay cho đợt diễn tập — người duyệt (Trưởng phòng CNTT) xác nhận.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ThayBang(int id, int suKienId) => ThucHien(id, async d =>
    {
        if (!User.LaDuyetCntt(thamSo)) throw new DomainException("Chỉ người duyệt (Trưởng phòng CNTT) xác nhận dùng downtime thực tế thay diễn tập.");
        var s = await db.SuKienDowntimes.Include(x => x.HeThongs).FirstOrDefaultAsync(x => x.Id == suKienId) ?? throw new DomainException("Không tìm thấy sự kiện.");
        d.ThayBang(s, User.Identity!.Name!, DateTime.Now);
        return $"Đã ghi nhận downtime {s.Ma} thay cho đợt diễn tập.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhuLuc(int id, string noiDung) => ThucHien(id, d => { d.ThemPhuLuc(noiDung, User.Identity!.Name!, DateTime.Now); return Task.FromResult("Đã thêm phụ lục."); });
}
