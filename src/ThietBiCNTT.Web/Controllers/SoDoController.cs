using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public record DiemVm(int TaiSanId, decimal X, decimal Y);
public record DiemHienThi(int TaiSanId, string Ma, string Ten, string? Loai, string Icon, string TrangThai, string MauTrangThai, decimal X, decimal Y,
    string? Ip, string? NguoiSuDung, int PhieuMo, string? PhieuMoiNhat, int? PhieuMoiNhatId, bool QuaHan);

/// <summary>Sơ đồ mặt bằng tương tác (FR-VT-05).</summary>
public class SoDoController(AppDbContext db, TepService tep) : Controller
{
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];

    private IQueryable<SoDoMatBang> PhamVi()
    {
        var q = db.SoDoMatBangs.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(s => s.KhoaPhongId == null || s.KhoaPhongId == k); }
        return q;
    }

    public async Task<IActionResult> Index() =>
        View(await PhamVi().AsNoTracking().Include(s => s.KhoaPhong).Include(s => s.ViTri).Include(s => s.Diems)
            .OrderBy(s => s.ThuTu).ThenBy(s => s.Ten).ToListAsync());

    public async Task<IActionResult> Xem(int id, int? chon)
    {
        var s = await PhamVi().AsNoTracking().Include(x => x.KhoaPhong).Include(x => x.ViTri).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        ViewBag.Diem = await DiemAsync(id);
        ViewData["chon"] = chon;
        if (User.LaCNTT())
        {
            var daDat = await db.DiemSoDos.Where(d => d.SoDoMatBangId == id).Select(d => d.TaiSanId).ToListAsync();
            var q = db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && !daDat.Contains(t.Id));
            if (s.KhoaPhongId is not null) q = q.Where(t => t.KhoaPhongId == s.KhoaPhongId);
            ViewBag.ChuaDat = await q.OrderBy(t => t.MaTaiSan).Take(500).ToListAsync();
        }
        return View(s);
    }

    /// <summary>Dữ liệu điểm (JSON) — dùng làm mới trạng thái định kỳ trên màn hình giám sát.</summary>
    public async Task<IActionResult> Diem(int id) =>
        await PhamVi().AnyAsync(x => x.Id == id) ? Json(await DiemAsync(id)) : NotFound();

    private async Task<List<DiemHienThi>> DiemAsync(int id)
    {
        var now = DateTime.Now;
        var ds = await db.DiemSoDos.AsNoTracking().Where(d => d.SoDoMatBangId == id)
            .Include(d => d.TaiSan!).ThenInclude(t => t.LoaiThietBi).Include(d => d.TaiSan!).ThenInclude(t => t.CauHinh).ToListAsync();
        var ids = ds.Select(d => d.TaiSanId).ToList();
        var phieu = await db.PhieuSuaChuas.AsNoTracking().Where(p => ids.Contains(p.TaiSanId) && !Dong.Contains(p.TrangThai) && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh)
            .Select(p => new { p.Id, p.TaiSanId, p.SoPhieu, p.HanXuLy, p.NgayYeuCau, p.BatDauTamDung }).ToListAsync();
        return ds.Select(d =>
        {
            var t = d.TaiSan!; var pm = phieu.Where(p => p.TaiSanId == t.Id).OrderByDescending(p => p.NgayYeuCau).ToList();
            return new DiemHienThi(t.Id, t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten, DiemSoDo.Icon(t.LoaiThietBi?.Nhom), t.TrangThai.Ten(), Badge.Mau(t.TrangThai).Split(' ')[0],
                d.X, d.Y, t.CauHinh?.DiaChiIP, t.NguoiSuDung, pm.Count, pm.FirstOrDefault()?.SoPhieu, pm.FirstOrDefault()?.Id,
                pm.Any(p => p.BatDauTamDung == null && p.HanXuLy < now));
        }).ToList();
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Sua(int? id)
    {
        var s = id is null ? new SoDoMatBang() : await db.SoDoMatBangs.FindAsync(id);
        if (s is null) return NotFound();
        await Nap(s);
        return View(s);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT), RequestSizeLimit(12_000_000)]
    public async Task<IActionResult> Sua(SoDoMatBang input, IFormFile? anh)
    {
        var s = input.Id == 0 ? new SoDoMatBang() : await db.SoDoMatBangs.FindAsync(input.Id);
        if (s is null) return NotFound();
        if (s.Id == 0 && anh is null) ModelState.AddModelError("", "Chọn ảnh sơ đồ (PNG/JPG/WEBP).");
        if (!ModelState.IsValid) { await Nap(input); return View(input); }
        s.Ten = input.Ten; s.MoTa = input.MoTa; s.KhoaPhongId = input.KhoaPhongId; s.ViTriId = input.ViTriId; s.ThuTu = input.ThuTu;
        if (s.Id == 0) { db.SoDoMatBangs.Add(s); await db.SaveChangesAsync(); }
        if (anh is not null)
        {
            try
            {
                if (!anh.ContentType.StartsWith("image/")) throw new DomainException("Sơ đồ phải là ảnh PNG/JPG/WEBP (có thể xuất từ PDF/AutoCAD ra PNG).");
                await using var st = anh.OpenReadStream();
                var t = await tep.LuuAsync("SoDo", s.Id.ToString(), anh.FileName, st, anh.Length, User.Identity?.Name);
                await db.SaveChangesAsync();
                s.TepAnhId = t.Id;
            }
            catch (DomainException ex)
            {
                if (input.Id == 0) { db.SoDoMatBangs.Remove(s); await db.SaveChangesAsync(); }
                ModelState.AddModelError("", ex.Message); await Nap(input); return View(input);
            }
        }
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu sơ đồ. Bấm \"Đặt thiết bị\" để gắn tài sản lên sơ đồ.";
        return RedirectToAction(nameof(Xem), new { id = s.Id });
    }

    /// <summary>Lưu toàn bộ vị trí điểm của sơ đồ (thêm / di chuyển / gỡ) — gọi bằng fetch khi chỉnh sơ đồ.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LuuDiem(int id, [FromForm] List<DiemVm> diem)
    {
        var s = await db.SoDoMatBangs.Include(x => x.Diems).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        try
        {
            var moi = diem.GroupBy(d => d.TaiSanId).Select(g => g.Last()).ToList();
            var ids = moi.Select(d => d.TaiSanId).ToList();
            var hopLe = await db.TaiSans.Where(t => ids.Contains(t.Id) && t.TrangThai != TrangThaiTaiSan.DaThanhLy).Select(t => t.Id).ToListAsync();
            if (hopLe.Count != ids.Count) throw new DomainException("Có tài sản không tồn tại hoặc đã thanh lý.");
            db.DiemSoDos.RemoveRange(s.Diems.Where(d => !ids.Contains(d.TaiSanId)));
            foreach (var d in moi)
            {
                var (x, y) = DiemSoDo.KiemTraToaDo(d.X, d.Y);
                var cu = s.Diems.FirstOrDefault(e => e.TaiSanId == d.TaiSanId);
                if (cu is null) s.Diems.Add(new DiemSoDo { TaiSanId = d.TaiSanId, X = x, Y = y }); else { cu.X = x; cu.Y = y; }
            }
            s.NgayCapNhat = DateTime.Now;
            await db.SaveChangesAsync();
            return Json(new { ok = true, so = moi.Count });
        }
        catch (DomainException ex) { return Json(new { ok = false, loi = ex.Message }); }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Xoa(int id)
    {
        var s = await db.SoDoMatBangs.FindAsync(id);
        if (s is null) return NotFound();
        db.SoDoMatBangs.Remove(s); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã xoá sơ đồ {s.Ten}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Nap(SoDoMatBang s)
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", s.KhoaPhongId);
        ViewBag.DsViTri = new SelectList(await db.ViTris.OrderBy(v => v.Ten).ToListAsync(), "Id", "Ten", s.ViTriId);
    }
}
