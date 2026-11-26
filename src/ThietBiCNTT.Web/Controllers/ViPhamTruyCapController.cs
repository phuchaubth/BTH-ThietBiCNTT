using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-TC-07 (MOI.01.01 ME6): giám sát tuân thủ truy cập, hành động khi có vi phạm.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class ViPhamTruyCapController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? nam)
    {
        var n = nam ?? DateTime.Today.Year; var tu = new DateOnly(n, 1, 1); var den = new DateOnly(n, 12, 31);
        var ds = await db.ViPhamTruyCaps.AsNoTracking().Include(v => v.HeThong).Include(v => v.KhoaPhong).Where(v => v.NgayPhatHien >= tu && v.NgayPhatHien <= den || v.TrangThai != TrangThaiViPham.DaDong).OrderBy(v => v.TrangThai == TrangThaiViPham.DaDong).ThenByDescending(v => v.NgayPhatHien).ToListAsync();
        ViewBag.Nam = n; ViewBag.TheoQuy = Enumerable.Range(1, 4).Select(q => (Quy: q, Moi: ds.Count(v => v.NgayPhatHien.Year == n && (v.NgayPhatHien.Month - 1) / 3 + 1 == q), Dong: ds.Count(v => v.NgayDong is { } d && d.Year == n && (d.Month - 1) / 3 + 1 == q))).ToList();
        return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create() { await Nap(); return View(new ViPhamTruyCap { NgayPhatHien = DateOnly.FromDateTime(DateTime.Today) }); }

    private async Task Nap()
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.AsNoTracking().OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(ViPhamTruyCap input)
    {
        if (!ModelState.IsValid) { await Nap(); return View(input); }
        input.NguoiXuLy = User.Identity!.Name; db.ViPhamTruyCaps.Add(input); await db.SaveChangesAsync();
        TempData["ok"] = "Đã ghi nhận vi phạm truy cập."; return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XuLy(int id, string hanhDong, bool dong)
    {
        var v = await db.ViPhamTruyCaps.FirstOrDefaultAsync(x => x.Id == id); if (v is null) return NotFound();
        try { v.GhiHanhDong(User.Identity!.Name!, hanhDong, dong, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = dong ? "Đã đóng vi phạm." : "Đã ghi hành động xử lý."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
