using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Quản trị tham số hệ thống và ngày nghỉ lễ.</summary>
[Authorize(Roles = VaiTro.QuanTri)]
public class ThamSoController(AppDbContext db, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index(string? nhom)
    {
        var ds = await db.ThamSoHeThongs.AsNoTracking().OrderBy(x => x.ThuTu).ToListAsync();
        ViewBag.NgayNghi = await db.NgayNghis.AsNoTracking().Where(x => x.Ngay >= DateOnly.FromDateTime(DateTime.Today.AddYears(-1)))
            .OrderBy(x => x.Ngay).ToListAsync();
        // Ví dụ minh hoạ SLA với tham số hiện tại
        var sla = thamSo.Sla();
        var mau = DateTime.Today.AddHours(16); // 16:00 hôm nay
        ViewBag.ViDu = Enum.GetValues<MucDoUuTien>().Reverse().Select(u => (
            UuTien: u,
            KhoaThuong: SlaCalculator.TinhHan(mau, u, false, sla),
            KhoaTrongYeu: SlaCalculator.TinhHan(mau, u, true, sla))).ToList();
        ViewBag.MocViDu = mau;
        ViewData["nhom"] = nhom;
        return View(ds);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(Dictionary<string, string> giaTri, Dictionary<string, string[]>? thu, string? nhom)
    {
        // Checkbox: không gửi khi bỏ chọn → mặc định false; ngày trong tuần gửi dạng mảng.
        var dsBatTat = ThamSoMacDinh.DanhSach().Where(x => x.Kieu == KieuThamSo.BatTat).Select(x => x.Ma);
        foreach (var ma in dsBatTat) if (Request.Form.ContainsKey($"co[{ma}]")) giaTri[ma] = Request.Form[$"co[{ma}]"].Contains("true") ? "true" : "false";
        if (thu is not null) foreach (var (ma, ds) in thu) giaTri[ma] = string.Join(",", ds.Where(x => x != "_"));
        var loi = await thamSo.LuuAsync(giaTri, User.Identity?.Name);
        if (loi.Count > 0) TempData["err"] = "Chưa lưu. " + string.Join("; ", loi);
        else TempData["ok"] = "Đã lưu tham số. Thay đổi có hiệu lực ngay.";
        return RedirectToAction(nameof(Index), new { nhom });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KhoiPhuc(string ma, string? nhom)
    {
        var r = await db.ThamSoHeThongs.FindAsync(ma);
        if (r is null) return NotFound();
        await thamSo.LuuAsync(new() { [ma] = r.GiaTriMacDinh }, User.Identity?.Name);
        TempData["ok"] = $"Đã khôi phục '{r.Ten}' về mặc định ({r.GiaTriMacDinh}).";
        return RedirectToAction(nameof(Index), new { nhom });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemNgayNghi(DateOnly tuNgay, DateOnly? denNgay, string ten)
    {
        var den = denNgay ?? tuNgay;
        if (string.IsNullOrWhiteSpace(ten) || den < tuNgay || den.DayNumber - tuNgay.DayNumber > 30)
        { TempData["err"] = "Nhập tên ngày nghỉ và khoảng ngày hợp lệ (tối đa 31 ngày)."; return RedirectToAction(nameof(Index), new { nhom = "Ngày nghỉ" }); }
        var co = await db.NgayNghis.Where(x => x.Ngay >= tuNgay && x.Ngay <= den).Select(x => x.Ngay).ToListAsync();
        for (var d = tuNgay; d <= den; d = d.AddDays(1))
            if (!co.Contains(d)) db.NgayNghis.Add(new NgayNghi { Ngay = d, Ten = ten.Trim() });
        await db.SaveChangesAsync();
        thamSo.XoaCache();
        TempData["ok"] = "Đã thêm ngày nghỉ.";
        return RedirectToAction(nameof(Index), new { nhom = "Ngày nghỉ" });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaNgayNghi(DateOnly ngay)
    {
        var r = await db.NgayNghis.FindAsync(ngay);
        if (r is not null) { db.NgayNghis.Remove(r); await db.SaveChangesAsync(); thamSo.XoaCache(); }
        return RedirectToAction(nameof(Index), new { nhom = "Ngày nghỉ" });
    }
}
