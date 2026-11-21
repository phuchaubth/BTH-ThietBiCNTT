using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public class DieuChuyenController(AppDbContext db, SoPhieuService soPhieu, LichSuService lichSu) : Controller
{
    private IQueryable<PhieuDieuChuyen> PhamVi()
    {
        var q = db.PhieuDieuChuyens.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(p => p.TuKhoaPhongId == k || p.DenKhoaPhongId == k); }
        return q;
    }

    public async Task<IActionResult> Index(TrangThaiDieuChuyen? trangThai)
    {
        var q = PhamVi().Include(p => p.TuKhoaPhong).Include(p => p.DenKhoaPhong).Include(p => p.ChiTiets).AsNoTracking();
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        ViewData["trangThai"] = trangThai;
        return View(await q.OrderByDescending(p => p.Id).Take(200).ToListAsync());
    }

    public async Task<IActionResult> Create(int? tuKhoa, int[]? taiSanIds)
    {
        if (!User.XemToanVien()) tuKhoa = User.KhoaPhongId();
        await Nap(tuKhoa);
        ViewData["chon"] = taiSanIds ?? [];
        return View(new PhieuDieuChuyen { TuKhoaPhongId = tuKhoa });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuDieuChuyen input, int[] taiSanIds, bool guiDuyet)
    {
        if (!User.XemToanVien() && input.TuKhoaPhongId != User.KhoaPhongId()) return Forbid();
        var dsTs = await db.TaiSans.Where(t => taiSanIds.Contains(t.Id)).ToListAsync();
        if (dsTs.Count == 0) ModelState.AddModelError("", "Chọn ít nhất một tài sản.");
        if (dsTs.Any(t => t.KhoaPhongId != input.TuKhoaPhongId)) ModelState.AddModelError("", "Có tài sản không thuộc khoa đi.");
        if (dsTs.Any(t => t.TrangThai is TrangThaiTaiSan.DaThanhLy or TrangThaiTaiSan.ChoThanhLy)) ModelState.AddModelError("", "Có tài sản đang/đã thanh lý.");
        if (!ModelState.IsValid) { await Nap(input.TuKhoaPhongId); ViewData["chon"] = taiSanIds; return View(input); }

        input.SoPhieu = await soPhieu.TaoAsync("DC");
        input.ChiTiets = dsTs.Select(t => new ChiTietDieuChuyen { TaiSanId = t.Id, TaiSan = t }).ToList();
        try { if (guiDuyet) input.GuiDuyet(); }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); await Nap(input.TuKhoaPhongId); ViewData["chon"] = taiSanIds; return View(input); }
        db.PhieuDieuChuyens.Add(input);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã tạo phiếu {input.SoPhieu}" + (guiDuyet ? " và gửi duyệt." : " (nháp).");
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await Load(id, tracking: false);
        return p is null ? NotFound() : View(p);
    }

    public async Task<IActionResult> InBienBan(int id)
    {
        var p = await Load(id, tracking: false);
        return p is null ? NotFound() : View(p);
    }

    private Task<PhieuDieuChuyen?> Load(int id, bool tracking)
    {
        var q = PhamVi().Include(p => p.TuKhoaPhong).Include(p => p.DenKhoaPhong)
            .Include(p => p.ChiTiets).ThenInclude(c => c.TaiSan).ThenInclude(t => t!.LoaiThietBi);
        return (tracking ? q : q.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<IActionResult> ThucHien(int id, Func<PhieuDieuChuyen, string> action)
    {
        var p = await Load(id, tracking: true);
        if (p is null) return NotFound();
        try { TempData["ok"] = action(p); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDuyet(int id) => ThucHien(id, p => { p.GuiDuyet(); return "Đã gửi duyệt."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Duyet(int id, bool dongY, string? yKien) => ThucHien(id, p =>
    {
        if (!User.LaDuyet() && !User.LaCNTT()) throw new DomainException("Bạn không có quyền duyệt.");
        p.Duyet(User.HoTen(), dongY, yKien, DateTime.Now);
        return dongY ? "Đã duyệt phiếu." : "Đã từ chối phiếu.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BanGiao(int id) => ThucHien(id, p =>
    {
        if (!User.LaCNTT()) throw new DomainException("Chỉ Phòng CNTT xác nhận bàn giao.");
        p.BanGiao(DateTime.Now);
        foreach (var ct in p.ChiTiets)
            lichSu.Ghi(ct.TaiSanId, LoaiSuKien.DieuChuyen, $"{p.SoPhieu}: {p.TuKhoaPhong?.Ten} → {p.DenKhoaPhong?.Ten}" + (p.NguoiNhan is null ? "" : $", người nhận {p.NguoiNhan}"), User.Identity?.Name);
        return "Đã bàn giao, tài sản đã được cập nhật khoa/phòng mới.";
    });

    private async Task Nap(int? tuKhoa)
    {
        var khoa = await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync();
        ViewBag.Khoa = new SelectList(khoa, "Id", "Ten");
        ViewBag.TaiSan = tuKhoa is null ? new List<TaiSan>() : await db.TaiSans.Include(t => t.LoaiThietBi).AsNoTracking()
            .Where(t => t.KhoaPhongId == tuKhoa && t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy)
            .OrderBy(t => t.MaTaiSan).ToListAsync();
    }
}
