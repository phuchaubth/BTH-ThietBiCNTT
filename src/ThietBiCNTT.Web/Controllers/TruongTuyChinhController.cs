using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Quản trị trường thông tin tuỳ chỉnh của tài sản (FR-TS-15).</summary>
[Authorize(Roles = VaiTro.NhomCNTT)]
public class TruongTuyChinhController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.SoGiaTri = await db.GiaTriTuyChinhs.GroupBy(g => g.TruongTuyChinhId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        return View(await db.TruongTuyChinhs.AsNoTracking().Include(t => t.LoaiThietBi).OrderBy(t => t.ThuTu).ThenBy(t => t.Ten).ToListAsync());
    }

    public async Task<IActionResult> Sua(int? id)
    {
        var t = id is null ? new TruongTuyChinh() : await db.TruongTuyChinhs.FindAsync(id);
        if (t is null) return NotFound();
        await Nap(t); return View(t);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sua(TruongTuyChinh input)
    {
        ModelState.Remove(nameof(input.LoaiThietBi));
        input.Ma = (input.Ma ?? "").Trim();
        try { input.KiemTraDinhNghia(); } catch (DomainException ex) { ModelState.AddModelError(nameof(input.LuaChon), ex.Message); }
        if (await db.TruongTuyChinhs.AnyAsync(t => t.Ma == input.Ma && t.Id != input.Id)) ModelState.AddModelError(nameof(input.Ma), "Mã trường đã tồn tại.");
        var t = input.Id == 0 ? new TruongTuyChinh() : await db.TruongTuyChinhs.FindAsync(input.Id);
        if (t is null) return NotFound();
        // Đã có dữ liệu: không đổi kiểu / mã để tránh hỏng giá trị đã lưu
        if (input.Id != 0 && await db.GiaTriTuyChinhs.AnyAsync(g => g.TruongTuyChinhId == input.Id) && (t.Kieu != input.Kieu || t.Ma != input.Ma))
            ModelState.AddModelError("", "Trường đã có dữ liệu — không đổi mã và kiểu dữ liệu (tạo trường mới nếu cần).");
        if (!ModelState.IsValid) { await Nap(input); return View(input); }
        t.Ma = input.Ma; t.Ten = input.Ten; t.Kieu = input.Kieu; t.LuaChon = input.LuaChon; t.BatBuoc = input.BatBuoc; t.ThuTu = input.ThuTu;
        t.LoaiThietBiId = input.LoaiThietBiId; t.MoTa = input.MoTa; t.DangDung = input.DangDung;
        if (input.Id == 0) db.TruongTuyChinhs.Add(t);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu trường \"{t.Ten}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.QuanTri)]
    public async Task<IActionResult> Xoa(int id)
    {
        var t = await db.TruongTuyChinhs.FindAsync(id);
        if (t is null) return NotFound();
        var n = await db.GiaTriTuyChinhs.CountAsync(g => g.TruongTuyChinhId == id);
        if (n > 0) { TempData["err"] = $"Trường đang có {n} giá trị — bỏ chọn 'Đang dùng' để ẩn thay vì xoá."; return RedirectToAction(nameof(Index)); }
        db.TruongTuyChinhs.Remove(t); await db.SaveChangesAsync();
        TempData["ok"] = "Đã xoá trường."; return RedirectToAction(nameof(Index));
    }

    private async Task Nap(TruongTuyChinh t) =>
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(l => l.Ten).ToListAsync(), "Id", "Ten", t.LoaiThietBiId);
}
