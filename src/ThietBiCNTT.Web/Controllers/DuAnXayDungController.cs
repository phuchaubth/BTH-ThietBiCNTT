using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-TD-10 (FMS.10.00 Intent): Phòng CNTT tham gia đánh giá rủi ro dự án xây dựng, cải tạo, phá dỡ.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class DuAnXayDungController(AppDbContext db, TepService tep) : Controller
{
    public async Task<IActionResult> Index() => View(await db.DuAnXayDungs.AsNoTracking().Include(d => d.PhieuThayDoi).OrderBy(d => d.DaHoanThanh).ThenBy(d => d.TuNgay).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var d = await db.DuAnXayDungs.AsNoTracking().Include(x => x.PhieuThayDoi).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("DuAnXayDung", id.ToString());
        return View(d);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var d = id is null ? new DuAnXayDung { TuNgay = DateOnly.FromDateTime(DateTime.Today) } : await db.DuAnXayDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        ViewBag.DsPhieu = new SelectList(await db.PhieuThayDois.AsNoTracking().OrderByDescending(p => p.Ma).Take(200).Select(p => new { p.Id, Ten = p.Ma + " – " + p.Ten }).ToListAsync(), "Id", "Ten", d.PhieuThayDoiId);
        return View(d);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(DuAnXayDung input)
    {
        if (input.TuNgay is { } t && input.DenNgay is { } e && e < t) ModelState.AddModelError(nameof(input.DenNgay), "Ngày kết thúc trước ngày bắt đầu.");
        if (!ModelState.IsValid) { ViewBag.DsPhieu = new SelectList(await db.PhieuThayDois.AsNoTracking().Select(p => new { p.Id, Ten = p.Ma + " – " + p.Ten }).ToListAsync(), "Id", "Ten", input.PhieuThayDoiId); return View(input); }
        if (input.Id == 0) db.DuAnXayDungs.Add(input);
        else { var x = await db.DuAnXayDungs.FirstAsync(a => a.Id == input.Id); input.NgayTao = x.NgayTao; input.NguoiTao = x.NguoiTao; db.Entry(x).CurrentValues.SetValues(input); }
        await db.SaveChangesAsync(); TempData["ok"] = "Đã lưu dự án.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }
}
