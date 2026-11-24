using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-HTT-09 (HCT.01.01): kênh truyền dữ liệu người bệnh qua điện tử, thiết bị di động, nhắn tin, cổng người bệnh; checklist theo ME.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class KenhDuLieuController(AppDbContext db) : Controller
{
    private Task<bool> CoHuongDanAsync() => db.TaiLieuQuyDinhs.AnyAsync(t => t.Loai == LoaiTaiLieu.HuongDanThietBiDiDong && t.TrangThai == TrangThaiKeHoach.HieuLuc);

    public async Task<IActionResult> Index(LoaiKenh? loai)
    {
        var q = db.KenhTruyenDuLieus.AsNoTracking().Include(k => k.HeThong).Where(k => k.DangSuDung);
        if (loai is not null) q = q.Where(k => k.Loai == loai);
        ViewData["loai"] = loai; ViewBag.CoHuongDan = await CoHuongDanAsync();
        return View(await q.OrderBy(k => k.Loai).ThenBy(k => k.Ten).ToListAsync());
    }

    private async Task NapAsync() =>
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var k = id is null ? new KenhTruyenDuLieu() : await db.KenhTruyenDuLieus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        await NapAsync(); ViewBag.CoHuongDan = await CoHuongDanAsync();
        return View(k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(KenhTruyenDuLieu input)
    {
        ModelState.Remove(nameof(input.HeThong));
        if (!ModelState.IsValid) { await NapAsync(); ViewBag.CoHuongDan = await CoHuongDanAsync(); return View(input); }
        if (input.Id == 0) db.KenhTruyenDuLieus.Add(input);
        else
        {
            var k = await db.KenhTruyenDuLieus.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = k.NgayTao; input.NguoiTao = k.NguoiTao;
            db.Entry(k).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu kênh {input.Ten}.";
        return RedirectToAction(nameof(Index));
    }
}
