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

/// <summary>FR-RR-09 (QPS.04.01 ME3–ME4): bài tập giảm thiểu rủi ro chủ động.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BaiTapChuDongController(AppDbContext db, TepService tep, HanhDongService hanhDong) : Controller
{
    public async Task<IActionResult> Index() => View(await db.BaiTapChuDongs.AsNoTracking().Include(b => b.DotDienTap).OrderByDescending(b => b.Nam).ThenByDescending(b => b.NgayThucHien).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var b = await db.BaiTapChuDongs.AsNoTracking().Include(x => x.DotDienTap).ThenInclude(d => d!.HeThong).FirstOrDefaultAsync(x => x.Id == id); if (b is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("BaiTapChuDong", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.NguonId == id && h.MoTa.StartsWith("[Bài tập chủ động]")).ToListAsync();
        return View(b);
    }

    private async Task Nap(int? chon) => ViewBag.DsDienTap = new SelectList(await db.DotDienTaps.AsNoTracking().Where(d => d.DaChot).OrderByDescending(d => d.NgayChot).Take(50).Select(d => new { d.Id, Ten = (d.HeThong != null ? d.HeThong.Ma : "Ứng phó mạng") + " – " + d.HanThucHien.ToString() }).ToListAsync(), "Id", "Ten", chon);

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var b = id is null ? new BaiTapChuDong { Nam = DateTime.Today.Year, NgayThucHien = DateOnly.FromDateTime(DateTime.Today) } : await db.BaiTapChuDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound(); if (b.DaChot) return RedirectToAction(nameof(Details), new { id });
        await Nap(b.DotDienTapId); return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(BaiTapChuDong input)
    {
        if (!ModelState.IsValid) { await Nap(input.DotDienTapId); return View(input); }
        if (input.Id == 0) db.BaiTapChuDongs.Add(input);
        else { var x = await db.BaiTapChuDongs.FirstAsync(a => a.Id == input.Id); if (x.DaChot) return RedirectToAction(nameof(Details), new { id = x.Id }); input.NgayTao = x.NgayTao; input.NguoiTao = x.NguoiTao; input.DaChot = false; db.Entry(x).CurrentValues.SetValues(input); }
        await db.SaveChangesAsync(); TempData["ok"] = "Đã lưu bài tập."; return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Chot(int id)
    {
        var b = await db.BaiTapChuDongs.FirstOrDefaultAsync(x => x.Id == id); if (b is null) return NotFound();
        try { b.Chot(User.Identity!.Name!, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = "Đã chốt bài tập — không sửa được nữa."; } catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> TaoHanhDong(int id, string moTa, string nguoiChiuTrachNhiem)
    {
        try { var h = await hanhDong.TaoAsync(NguonHanhDong.TrucTiep, id, "[Bài tập chủ động] " + moTa, null, nguoiChiuTrachNhiem, null, LoaiHanhDong.CaiTien, Domain.Enums.MucDoUuTien.TrungBinh, User.Identity!.Name!); TempData["ok"] = $"Đã tạo hành động {h.Ma}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
