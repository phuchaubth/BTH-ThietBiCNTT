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

/// <summary>FR-AN-07: theo dõi lỗ hổng, hạn vá theo mức CVSS (HCT.01.05, MOI.01.02).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class LoHongController(AppDbContext db, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index(bool chiMo = true)
    {
        var q = db.LoHongs.AsNoTracking().Include(l => l.HeThong).AsQueryable();
        if (chiMo) q = q.Where(l => l.TrangThai == TrangThaiLoHong.Mo || l.TrangThai == TrangThaiLoHong.DangVa);
        ViewData["chiMo"] = chiMo; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View((await q.ToListAsync()).OrderBy(l => l.DangMo ? 0 : 1).ThenBy(l => l.HanVa).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var l = await db.LoHongs.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiSan).FirstOrDefaultAsync(x => x.Id == id);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return l is null ? NotFound() : View(l);
    }

    private async Task Nap()
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.AsNoTracking().OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.AsNoTracking().OrderBy(t => t.MaTaiSan).Take(500).Select(t => new { t.Id, Ten = t.MaTaiSan + " – " + t.Ten }).ToListAsync(), "Id", "Ten");
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create() { await Nap(); return View(new LoHong { NgayPhatHien = DateOnly.FromDateTime(DateTime.Today), Cvss = 5 }); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(LoHong input)
    {
        ModelState.Remove(nameof(input.HanVa));
        if (input.HeThongId is null && input.TaiSanId is null) ModelState.AddModelError("", "Chọn hệ thống hoặc tài sản bị ảnh hưởng.");
        if (!ModelState.IsValid) { await Nap(); return View(input); }
        input.HanVa = LoHong.TinhHan(input.NgayPhatHien, input.Cvss, thamSo.SoNguyen(TS.JciVaNghiemTrongNgay), thamSo.SoNguyen(TS.JciVaCaoNgay), thamSo.SoNguyen(TS.JciVaTrungBinhNgay), thamSo.SoNguyen(TS.JciVaThapNgay));
        input.NguoiPhuTrach ??= User.Identity!.Name;
        db.LoHongs.Add(input); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã ghi nhận lỗ hổng mức {input.Muc.TenHienThi()}, hạn vá {input.HanVa:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    private async Task<IActionResult> Thuc(int id, Func<LoHong, string> f)
    {
        var l = await db.LoHongs.FirstOrDefaultAsync(x => x.Id == id); if (l is null) return NotFound();
        try { TempData["ok"] = f(l); await db.SaveChangesAsync(); } catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> DangVa(int id) => Thuc(id, l => { if (l.TrangThai != TrangThaiLoHong.Mo) throw new DomainException("Chỉ chuyển Đang vá từ trạng thái Mở."); l.TrangThai = TrangThaiLoHong.DangVa; return "Đã chuyển sang Đang vá."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> DaVa(int id) => Thuc(id, l => { l.DaVaXong(User.Identity!.Name!, DateTime.Now); return "Đã ghi nhận vá xong."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> ChapNhan(int id, string lyDo) => Thuc(id, l =>
    {
        if (!User.LaDuyetCntt(thamSo)) throw new DomainException("Chỉ Trưởng phòng CNTT được chấp nhận rủi ro.");
        l.ChapNhanRuiRo(lyDo, User.Identity!.Name!, DateTime.Now); return "Đã ghi nhận chấp nhận rủi ro.";
    });
}
