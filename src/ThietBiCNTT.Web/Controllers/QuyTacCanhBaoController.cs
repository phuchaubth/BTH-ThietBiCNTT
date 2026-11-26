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

/// <summary>Quy tắc cảnh báo tuỳ chỉnh (FR-HT-15): cấu hình, bật/tắt, chạy thử, chạy ngay.</summary>
[Authorize(Roles = VaiTro.NhomCNTT)]
public class QuyTacCanhBaoController(AppDbContext db, QuyTacCanhBaoService svc) : Controller
{
    public async Task<IActionResult> Index()
    {
        var ds = await db.QuyTacCanhBaos.AsNoTracking().Include(x => x.DieuKiens).Include(x => x.KhoaPhong).Include(x => x.LoaiThietBi)
            .OrderBy(x => x.DoiTuong).ThenBy(x => x.Ten).ToListAsync();
        ViewBag.Truong = await svc.TatCaTruongAsync();
        return View(ds);
    }

    private async Task NapDanhMucAsync()
    {
        ViewBag.Truong = await svc.TatCaTruongAsync();
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.AsNoTracking().OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
    }

    public async Task<IActionResult> Form(int? id)
    {
        var q = id is null ? new QuyTacCanhBao { DieuKiens = [new DieuKienCanhBao()] }
            : await db.QuyTacCanhBaos.AsNoTracking().Include(x => x.DieuKiens).FirstOrDefaultAsync(x => x.Id == id);
        if (q is null) return NotFound();
        await NapDanhMucAsync();
        return View(q);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(QuyTacCanhBao input, string[]? vaiTro)
    {
        input.DieuKiens = input.DieuKiens.Where(d => !string.IsNullOrWhiteSpace(d.Truong)).ToList();
        input.VaiTroNhan = vaiTro is { Length: > 0 } ? string.Join(",", vaiTro.Where(VaiTro.TatCa.Contains)) : null;
        var truong = (await svc.TatCaTruongAsync()).GetValueOrDefault(input.DoiTuong) ?? [];
        foreach (var d in input.DieuKiens.Where(d => truong.All(t => t.Ma != d.Truong)))
            ModelState.AddModelError("", $"Trường '{d.Truong}' không thuộc đối tượng {input.DoiTuong.Ten()}.");
        try { input.KiemTra(); }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); }
        var hopLe = await db.Users.Where(u => u.DangHoatDong).Select(u => u.UserName!).ToListAsync();
        foreach (var u in input.DsNguoiNhan.Where(u => !hopLe.Contains(u, StringComparer.OrdinalIgnoreCase)))
            ModelState.AddModelError(nameof(input.NguoiNhan), $"Không có tài khoản đang hoạt động '{u}'.");
        foreach (var k in ModelState.Keys.Where(k => k.StartsWith("DieuKiens[")).ToList()) ModelState.Remove(k);
        if (!ModelState.IsValid)
        {
            if (input.DieuKiens.Count == 0) input.DieuKiens.Add(new DieuKienCanhBao());
            await NapDanhMucAsync();
            return View("Form", input);
        }
        QuyTacCanhBao q;
        if (input.Id == 0) { q = input; db.QuyTacCanhBaos.Add(q); }
        else
        {
            q = await db.QuyTacCanhBaos.Include(x => x.DieuKiens).FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new KeyNotFoundException();
            var doiDieuKien = q.DoiTuong != input.DoiTuong || q.DieuKiens.Count != input.DieuKiens.Count
                || q.DieuKiens.OrderBy(d => d.Id).Zip(input.DieuKiens).Any(p => p.First.Truong != p.Second.Truong || p.First.ToanTu != p.Second.ToanTu || p.First.GiaTri != p.Second.GiaTri);
            q.Ten = input.Ten; q.MoTa = input.MoTa; q.DoiTuong = input.DoiTuong; q.KhoaPhongId = input.KhoaPhongId; q.LoaiThietBiId = input.LoaiThietBiId;
            q.UuTienTu = input.UuTienTu; q.NguoiNhan = input.NguoiNhan; q.VaiTroNhan = input.VaiTroNhan; q.GuiNguoiPhuTrach = input.GuiNguoiPhuTrach;
            q.GuiTruongKhoa = input.GuiTruongKhoa; q.MucDo = input.MucDo; q.ChiTrongUngDung = input.ChiTrongUngDung; q.NhacLaiSauGio = input.NhacLaiSauGio;
            q.HanhDong = input.HanhDong; q.DangBat = input.DangBat;
            if (doiDieuKien)
            {
                db.DieuKienCanhBaos.RemoveRange(q.DieuKiens);
                q.DieuKiens = input.DieuKiens.Select(d => new DieuKienCanhBao { Truong = d.Truong, ToanTu = d.ToanTu, GiaTri = d.GiaTri }).ToList();
                // Điều kiện đổi → coi như quy tắc mới: đối tượng đang khớp sẽ được báo lại theo điều kiện mới.
                await db.CanhBaoDaGuis.Where(x => x.QuyTacCanhBaoId == q.Id).ExecuteDeleteAsync();
            }
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu quy tắc \"{q.Ten}\". Dùng \"Chạy thử\" để xem đối tượng đang thoả điều kiện.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatTat(int id)
    {
        var q = await db.QuyTacCanhBaos.FindAsync(id);
        if (q is null) return NotFound();
        q.DangBat = !q.DangBat;
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã {(q.DangBat ? "bật" : "tắt")} quy tắc \"{q.Ten}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Xoa(int id)
    {
        var q = await db.QuyTacCanhBaos.FindAsync(id);
        if (q is null) return NotFound();
        db.QuyTacCanhBaos.Remove(q);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã xoá quy tắc \"{q.Ten}\".";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Chạy thử: liệt kê đối tượng đang thoả điều kiện và người sẽ nhận — không gửi.</summary>
    public async Task<IActionResult> ChayThu(int id)
    {
        var q = await db.QuyTacCanhBaos.AsNoTracking().Include(x => x.DieuKiens).Include(x => x.KhoaPhong).Include(x => x.LoaiThietBi).FirstOrDefaultAsync(x => x.Id == id);
        if (q is null) return NotFound();
        ViewBag.Khop = await svc.TimAsync(q, DateTime.Now);
        ViewBag.NguoiNhan = await svc.NguoiNhanCoDinhAsync(q);
        ViewBag.Truong = await svc.TatCaTruongAsync();
        ViewBag.DaGui = await db.CanhBaoDaGuis.AsNoTracking().Where(x => x.QuyTacCanhBaoId == id).ToDictionaryAsync(x => x.DoiTuongId);
        return View(q);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChayNgay(int id)
    {
        var q = await db.QuyTacCanhBaos.Include(x => x.DieuKiens).FirstOrDefaultAsync(x => x.Id == id);
        if (q is null) return NotFound();
        var kq = await svc.ChayAsync(q, DateTime.Now);
        TempData["ok"] = $"Đã chạy: {kq.Khop.Count} đối tượng thoả điều kiện, gửi {kq.DaGui} cảnh báo mới/nhắc lại" + (kq.DaGui > 0 ? $" tới {kq.NguoiNhan.Count} người" : "")
            + (kq.HanhDong > 0 ? $", thực hiện {kq.HanhDong} hành động tự động." : ".");
        return RedirectToAction(nameof(ChayThu), new { id });
    }
}
