using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-RR-03: gợi ý rủi ro từ dữ liệu hệ thống.</summary>
[Authorize(Roles = VaiTro.NhomCNTT)]
public class GoiYRuiRoController(AppDbContext db, GoiYRuiRoService svc) : Controller
{
    public async Task<IActionResult> Index(bool tatCa = false)
    {
        var q = db.GoiYRuiRos.AsNoTracking().Include(g => g.HeThong).AsQueryable();
        if (!tatCa) q = q.Where(g => g.TrangThai == TrangThaiGoiY.Moi);
        ViewData["tatCa"] = tatCa; return View(await q.OrderByDescending(g => g.NgayTao).Take(300).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sinh() { var n = await svc.SinhAsync(DateOnly.FromDateTime(DateTime.Today)); TempData["ok"] = n > 0 ? $"Có {n} gợi ý mới." : "Không có gợi ý mới."; return RedirectToAction(nameof(Index)); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChapNhan(int id, string? nguoiPhuTrach)
    {
        try { var r = await svc.ChapNhanAsync(id, User.Identity!.Name!, string.IsNullOrWhiteSpace(nguoiPhuTrach) ? User.Identity!.Name! : nguoiPhuTrach.Trim()); TempData["ok"] = $"Đã tạo rủi ro {r.Ma} — hãy chấm điểm lại."; return RedirectToAction("Form", "RuiRo", new { id = r.Id }); }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BoQua(int id)
    {
        var g = await db.GoiYRuiRos.FirstOrDefaultAsync(x => x.Id == id); if (g is null) return NotFound();
        if (g.TrangThai == TrangThaiGoiY.Moi) { g.TrangThai = TrangThaiGoiY.BoQua; g.NguoiXuLy = User.Identity!.Name; g.NgayXuLy = DateTime.Now; await db.SaveChangesAsync(); }
        return RedirectToAction(nameof(Index));
    }
}
