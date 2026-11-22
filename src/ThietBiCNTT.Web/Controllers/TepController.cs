using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Tệp đính kèm: tải lên, xem, xoá — kiểm tra quyền theo đối tượng gắn kèm.</summary>
public class TepController(AppDbContext db, TepService tep) : Controller
{
    public static readonly string[] DoiTuongHopLe = ["PhieuSuaChua", "TaiSan", "LanKiemTra"];

    /// <summary>Người dùng có được xem đối tượng không (cùng quy tắc phạm vi khoa của từng phân hệ).</summary>
    private async Task<bool> DuocXemAsync(string doiTuong, string id)
    {
        if (User.XemToanVien()) return true;
        var k = User.KhoaPhongId();
        return doiTuong switch
        {
            "PhieuSuaChua" => int.TryParse(id, out var p) && await db.PhieuSuaChuas.AnyAsync(x => x.Id == p && x.KhoaPhongId == k),
            "TaiSan" => int.TryParse(id, out var t) && await db.TaiSans.AnyAsync(x => x.Id == t && x.KhoaPhongId == k),
            "LanKiemTra" => long.TryParse(id, out var l) && await db.LanKiemTras.AnyAsync(x => x.Id == l && x.MauKiemTra!.KhoaPhongId == k),
            _ => false
        };
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> TaiLen(string doiTuong, string doiTuongId, List<IFormFile> files, string? quayLai)
    {
        if (!DoiTuongHopLe.Contains(doiTuong) || !await DuocXemAsync(doiTuong, doiTuongId)) return Forbid();
        try
        {
            foreach (var f in files.Take(10))
            {
                await using var s = f.OpenReadStream();
                await tep.LuuAsync(doiTuong, doiTuongId, f.FileName, s, f.Length, User.Identity?.Name);
            }
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã đính kèm {Math.Min(files.Count, 10)} tệp.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return LocalRedirect(Url.IsLocalUrl(quayLai) ? quayLai! : "/");
    }

    public async Task<IActionResult> Xem(long id, bool taiVe = false)
    {
        var t = await db.TepDinhKems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || !await DuocXemAsync(t.DoiTuong, t.DoiTuongId)) return NotFound();
        var duongDan = tep.DuongDan(t);
        if (!System.IO.File.Exists(duongDan)) return NotFound();
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return PhysicalFile(duongDan, t.ContentType, taiVe || !t.LaAnh ? t.TenFile : null, enableRangeProcessing: true);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(long id, string? quayLai)
    {
        var t = await db.TepDinhKems.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || !await DuocXemAsync(t.DoiTuong, t.DoiTuongId)) return NotFound();
        if (!User.LaCNTT() && t.NguoiTai != User.Identity?.Name) return Forbid();
        db.TepDinhKems.Remove(t);
        await db.SaveChangesAsync();
        try { System.IO.File.Delete(tep.DuongDan(t)); } catch (IOException) { }
        TempData["ok"] = $"Đã xoá tệp {t.TenFile}.";
        return LocalRedirect(Url.IsLocalUrl(quayLai) ? quayLai! : "/");
    }
}
