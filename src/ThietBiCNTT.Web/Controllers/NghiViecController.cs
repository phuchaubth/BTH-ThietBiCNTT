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

/// <summary>FR-TC-06: thu hồi quyền khi nghỉ việc / chuyển khoa (HCT.01.01 ME2, MOI.01.01).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class NghiViecController(AppDbContext db, RaSoatQuyenService svc) : Controller
{
    public async Task<IActionResult> Index(bool chiMo = true)
    {
        var q = db.ThongBaoNghiViecs.AsNoTracking().Include(t => t.Viecs).Include(t => t.KhoaPhong).AsQueryable();
        var ds = await q.OrderByDescending(t => t.NgayTao).Take(300).ToListAsync();
        if (chiMo) ds = ds.Where(t => !t.Xong).ToList();
        ViewData["chiMo"] = chiMo; return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create() { await Nap(); return View(new ThongBaoNghiViec { NgayHieuLuc = DateOnly.FromDateTime(DateTime.Today) }); }

    private async Task Nap() => ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(ThongBaoNghiViec input)
    {
        ModelState.Remove(nameof(input.Han));
        if (!ModelState.IsValid) { await Nap(); return View(input); }
        try { var t = await svc.GhiNghiViecAsync(input, User.Identity!.Name!); TempData["ok"] = $"Đã ghi nhận — {t.Viecs.Count} việc thu hồi, hạn {t.Han:dd/MM/yyyy}."; return RedirectToAction(nameof(Details), new { id = t.Id }); }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); await Nap(); return View(input); }
    }

    public async Task<IActionResult> Details(int id)
    {
        var t = await db.ThongBaoNghiViecs.AsNoTracking().Include(x => x.KhoaPhong).Include(x => x.Viecs).ThenInclude(v => v.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        return t is null ? NotFound() : View(t);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> DaThucHien(int id, int viecId)
    {
        var v = await db.ThongBaoNghiViecs.Where(t => t.Id == id).SelectMany(t => t.Viecs).FirstOrDefaultAsync(x => x.Id == viecId);
        if (v is null) return NotFound();
        v.DaThucHien = true; v.NguoiThucHien = User.Identity!.Name; v.NgayThucHien = DateTime.Now; await db.SaveChangesAsync();
        TempData["ok"] = "Đã ghi nhận đã thu hồi."; return RedirectToAction(nameof(Details), new { id });
    }
}
