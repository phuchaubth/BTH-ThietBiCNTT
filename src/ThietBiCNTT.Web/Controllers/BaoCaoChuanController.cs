using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Báo cáo chuẩn (FR-BC-11) xuất PDF/Excel có quốc hiệu, chữ ký; đề nghị thay thế thiết bị (FR-BC-10).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BaoCaoChuanController(AppDbContext db, BaoCaoChuanService svc, ThayTheService thayThe, IConfiguration config) : Controller
{
    private async Task DanhMucKhoaAsync(int? khoaId) =>
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoaId);

    private static ThamSoBaoCao ThamSo(DateTime? tu, DateTime? den, int? khoaId, int? nam, int? soNgay)
    {
        var homNay = DateTime.Today;
        var t = tu ?? new DateTime(homNay.Year, homNay.Month, 1).AddMonths(-1);
        var d = den ?? (tu is null ? new DateTime(homNay.Year, homNay.Month, 1).AddDays(-1) : homNay);
        if (d < t) (t, d) = (d, t);
        return new(t.Date, d.Date, khoaId, nam ?? homNay.Year, Math.Clamp(soNgay ?? 90, 1, 730));
    }

    public async Task<IActionResult> Index(DateTime? tu, DateTime? den, int? khoaId, int? nam, int? soNgay)
    {
        ViewBag.P = ThamSo(tu, den, khoaId, nam, soNgay);
        await DanhMucKhoaAsync(khoaId);
        return View(BaoCaoChuanService.DanhMuc);
    }

    /// <summary>Xem / tải báo cáo: dang = "web" | "pdf" | "xlsx".</summary>
    public async Task<IActionResult> Xem(string ma, DateTime? tu, DateTime? den, int? khoaId, int? nam, int? soNgay, string dang = "web")
    {
        var p = ThamSo(tu, den, khoaId, nam, soNgay);
        var now = DateTime.Now;
        var b = await svc.LapAsync(ma, p, now);
        if (b is null) return NotFound();
        var tt = svc.ThongTin(User.HoTen());
        var ten = $"{b.Ma}_{now:yyyyMMdd_HHmm}";
        switch (dang)
        {
            case "pdf":
                PhongChuBaoCao.CaiDat(config["BaoCao:FontDir"]);
                return File(BaoCaoChuanXuat.Pdf(b, tt, now), "application/pdf", ten + ".pdf");
            case "xlsx":
                return File(BaoCaoChuanXuat.Excel(b, tt, now), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ten + ".xlsx");
        }
        ViewBag.P = p; ViewBag.TT = tt; ViewBag.Mau = BaoCaoChuanService.DanhMuc.First(x => x.Ma == ma);
        return View(b);
    }

    // ===== Đề nghị thay thế (FR-BC-10) =====
    public async Task<IActionResult> ThayThe(int? khoaId, int? loaiId)
    {
        ViewBag.TieuChi = thayThe.TieuChi();
        await DanhMucKhoaAsync(khoaId);
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", loaiId);
        ViewBag.KhoaId = khoaId; ViewBag.LoaiId = loaiId;
        return View(await thayThe.DanhSachAsync(DateTime.Now, khoaId, loaiId));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LapDeXuat(int[] taiSanId)
    {
        try
        {
            var dx = await thayThe.LapDeXuatAsync(taiSanId, User.Identity!.Name!, DateTime.Now);
            TempData["ok"] = $"Đã lập đề xuất nháp {dx.SoPhieu} — kiểm tra đơn giá, cấu hình rồi gửi duyệt.";
            return RedirectToAction("DeXuat", "MuaSam", new { id = dx.Id });
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(ThayThe)); }
    }
}
