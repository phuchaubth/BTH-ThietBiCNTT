using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>FR-JC-07, FR-JC-09 (GLD.05.00): hợp đồng dịch vụ CNTT và đánh giá nhà cung cấp hằng năm.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class NhaCungCapDanhGiaController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? nam)
    {
        var n = nam ?? DateTime.Today.Year;
        var hds = await db.HopDongs.AsNoTracking().Include(h => h.NhaCungCap).Where(h => h.Loai == LoaiHopDong.BaoTri || h.Loai == LoaiHopDong.ThueNgoai).OrderBy(h => h.SoHopDong).ToListAsync();
        ViewBag.Nam = n; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.DanhGia = await db.DanhGiaNhaCungCaps.AsNoTracking().Where(d => d.Nam == n).ToDictionaryAsync(d => d.HopDongId);
        return View(hds.Where(h => h.ConHieuLuc(DateOnly.FromDateTime(DateTime.Today)) || h.HieuLucDen?.Year == n).ToList());
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int hopDongId, int nam)
    {
        var hd = await db.HopDongs.AsNoTracking().Include(h => h.NhaCungCap).Include(h => h.TaiSans).FirstOrDefaultAsync(h => h.Id == hopDongId); if (hd is null) return NotFound();
        var d = await db.DanhGiaNhaCungCaps.FirstOrDefaultAsync(x => x.HopDongId == hopDongId && x.Nam == nam);
        if (d is null)
        {
            var tsIds = hd.TaiSans.Select(t => t.TaiSanId).ToList(); var tu = new DateTime(nam, 1, 1); var den = tu.AddYears(1);
            var gui = await db.PhieuSuaChuas.AsNoTracking().Where(p => tsIds.Contains(p.TaiSanId) && p.NgayGuiHang != null && p.NgayGuiHang >= tu && p.NgayGuiHang < den).ToListAsync();
            var tre = gui.Count(p => p.NgayNhanVe != null && p.NgayHenTra != null && DateOnly.FromDateTime(p.NgayNhanVe.Value) > p.NgayHenTra);
            d = new DanhGiaNhaCungCap { HopDongId = hopDongId, Nam = nam, NguoiGiamSat = hd.NguoiPhuTrach, DuLieuThamChieu = $"Số phiếu gửi hãng trong năm: {gui.Count}; trả trễ hẹn: {tre}. Chỉ tiêu hợp đồng: {hd.ChiTieuNhaThau}" };
        }
        ViewBag.HopDong = hd; return View(d);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(DanhGiaNhaCungCap input, bool chot)
    {
        var hd = await db.HopDongs.AsNoTracking().Include(h => h.NhaCungCap).FirstOrDefaultAsync(h => h.Id == input.HopDongId); if (hd is null) return NotFound();
        var d = await db.DanhGiaNhaCungCaps.FirstOrDefaultAsync(x => x.HopDongId == input.HopDongId && x.Nam == input.Nam);
        if (d is { NgayDanhGia: not null }) { TempData["err"] = "Phiếu đã chốt."; return RedirectToAction(nameof(Index), new { nam = input.Nam }); }
        if (d is null) { d = new DanhGiaNhaCungCap { HopDongId = input.HopDongId, Nam = input.Nam }; db.DanhGiaNhaCungCaps.Add(d); }
        d.DiemPhanHoi = input.DiemPhanHoi; d.DiemChatLuong = input.DiemChatLuong; d.DiemTuanThu = input.DiemTuanThu; d.DiemBaoMat = input.DiemBaoMat; d.DuLieuThamChieu = input.DuLieuThamChieu;
        d.NguoiGiamSat = input.NguoiGiamSat; d.TruongBoPhan = input.TruongBoPhan; d.KetLuan = input.KetLuan; d.NhanXet = input.NhanXet;
        try { if (chot) d.Chot(DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = chot ? "Đã chốt đánh giá nhà cung cấp." : "Đã lưu nháp."; }
        catch (DomainException ex) { await db.SaveChangesAsync(); TempData["err"] = ex.Message; return RedirectToAction(nameof(Form), new { hopDongId = input.HopDongId, nam = input.Nam }); }
        return RedirectToAction(nameof(Index), new { nam = input.Nam });
    }
}
