using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class KeHoachBaoTriController(AppDbContext db, BaoTriService baoTri) : Controller
{
    public async Task<IActionResult> Index()
    {
        var ds = await db.KeHoachBaoTris.Include(k => k.LoaiThietBi).Include(k => k.TaiSan).AsNoTracking()
            .OrderBy(k => k.NgayTiepTheo).ToListAsync();
        // Lịch 60 ngày tới: dự kiến các mốc của mỗi kế hoạch
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.Lich = ds.Where(k => k.DangApDung).SelectMany(k =>
                Enumerable.Range(0, 400).Select(i => k.NgayTiepTheo.AddDays(i * k.ChuKyNgay))
                    .TakeWhile(d => d <= homNay.AddDays(60)).Select(d => (Ngay: d, KeHoach: k)))
            .OrderBy(x => x.Ngay).ToList();
        ViewBag.SoLenhMo = await db.PhieuSuaChuas.CountAsync(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy
            && p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy);
        return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create() { await Nap(); return View("Form", new KeHoachBaoTri { NgayTiepTheo = DateOnly.FromDateTime(DateTime.Today.AddDays(7)) }); }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Edit(int id)
    {
        var k = await db.KeHoachBaoTris.FindAsync(id);
        if (k is null) return NotFound();
        await Nap(); return View("Form", k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Save(KeHoachBaoTri input)
    {
        if (input.LoaiThietBiId is null && input.TaiSanId is null)
            ModelState.AddModelError("", "Chọn loại thiết bị hoặc một tài sản cụ thể.");
        if (!ModelState.IsValid) { await Nap(); return View("Form", input); }
        if (input.Id == 0) db.KeHoachBaoTris.Add(input);
        else
        {
            var k = await db.KeHoachBaoTris.FindAsync(input.Id);
            if (k is null) return NotFound();
            input.NgayTao = k.NgayTao; input.NguoiTao = k.NguoiTao;
            db.Entry(k).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu kế hoạch bảo trì.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Sinh ngay các lệnh bảo trì đến hạn (bình thường chạy nền mỗi 6 giờ).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> SinhLenh()
    {
        var n = await baoTri.SinhLenhDenHanAsync(DateOnly.FromDateTime(DateTime.Today));
        TempData["ok"] = n == 0 ? "Không có kế hoạch nào đến hạn." : $"Đã sinh {n} lệnh bảo trì định kỳ.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Nap()
    {
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(l => l.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy).OrderBy(t => t.MaTaiSan)
            .Select(t => new { t.Id, Ten = t.MaTaiSan + " - " + t.Ten }).ToListAsync(), "Id", "Ten");
    }
}
