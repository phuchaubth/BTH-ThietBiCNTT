using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-HTT-12 (HCT.01.00): hồ sơ người được lãnh đạo chỉ định giám sát CNTT y tế.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class GiamSatCnttController(AppDbContext db, TepService tep) : Controller
{
    public async Task<IActionResult> Index()
    {
        var ds = await db.NguoiGiamSatCntts.AsNoTracking().OrderByDescending(x => x.NgayBatDau).ToListAsync();
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(ds);
    }

    public async Task<IActionResult> Details(int id)
    {
        var x = await db.NguoiGiamSatCntts.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        if (x is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("NguoiGiamSatCntt", id.ToString());
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(x);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var x = id is null ? new NguoiGiamSatCntt { NgayVanBan = DateOnly.FromDateTime(DateTime.Today), NgayBatDau = DateOnly.FromDateTime(DateTime.Today) } : await db.NguoiGiamSatCntts.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return x is null ? NotFound() : View(x);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(NguoiGiamSatCntt input)
    {
        if (input.NgayKetThuc is { } kt && kt < input.NgayBatDau) ModelState.AddModelError(nameof(input.NgayKetThuc), "Ngày kết thúc trước ngày bắt đầu.");
        if (!ModelState.IsValid) return View(input);
        if (input.Id == 0)
        {
            // Chỉ định người mới → bản ghi đang hiệu lực trước đó kết thúc vào ngày trước (giữ lịch sử thay đổi)
            foreach (var cu in await db.NguoiGiamSatCntts.Where(n => n.NgayKetThuc == null && n.NgayBatDau < input.NgayBatDau).ToListAsync()) cu.NgayKetThuc = input.NgayBatDau.AddDays(-1);
            db.NguoiGiamSatCntts.Add(input);
        }
        else
        {
            var x = await db.NguoiGiamSatCntts.FirstAsync(n => n.Id == input.Id);
            input.NgayTao = x.NgayTao; input.NguoiTao = x.NguoiTao;
            db.Entry(x).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu hồ sơ người giám sát CNTT y tế: {input.HoTen}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }
}
