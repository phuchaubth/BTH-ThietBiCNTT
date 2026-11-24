using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Quản trị tích hợp: khoá API, webhook, nhật ký gọi API, tài liệu API.</summary>
[Authorize(Roles = VaiTro.QuanTri)]
public class TichHopController(AppDbContext db, TichHopService th, WebhookSender sender) : Controller
{
    public async Task<IActionResult> Index() => View(await db.KhoaApis.AsNoTracking().OrderByDescending(k => k.DangHoatDong).ThenByDescending(k => k.Id).ToListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoKhoa(string ten, List<string> phamVi, string? ipChoPhep, DateOnly? hetHan)
    {
        try
        {
            var (k, goc) = await th.TaoKhoaAsync(ten, phamVi, ipChoPhep, hetHan);
            TempData["khoaMoi"] = goc; TempData["tenKhoaMoi"] = k.Ten;
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuHoi(int id)
    {
        var k = await db.KhoaApis.FindAsync(id);
        if (k is null) return NotFound();
        k.DangHoatDong = false; await db.SaveChangesAsync();
        TempData["ok"] = $"Đã thu hồi khoá '{k.Ten}' — mọi lời gọi dùng khoá này bị từ chối ngay.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> NhatKy(int? khoaApiId, bool loi = false)
    {
        var q = db.NhatKyApis.AsNoTracking().AsQueryable();
        if (khoaApiId is not null) q = q.Where(x => x.KhoaApiId == khoaApiId);
        if (loi) q = q.Where(x => x.MaHttp >= 400);
        ViewBag.Khoas = await db.KhoaApis.AsNoTracking().OrderBy(k => k.Ten).ToListAsync();
        ViewData["khoaApiId"] = khoaApiId; ViewData["loi"] = loi;
        return View(await q.OrderByDescending(x => x.Id).Take(500).ToListAsync());
    }

    public IActionResult TaiLieu() => View();

    // ===== Webhook =====
    public async Task<IActionResult> Webhooks()
    {
        ViewBag.Gui = await db.WebhookGuis.AsNoTracking().Include(g => g.Webhook).OrderByDescending(g => g.Id).Take(100).ToListAsync();
        return View(await db.Webhooks.AsNoTracking().OrderBy(w => w.Ten).ToListAsync());
    }

    public async Task<IActionResult> SuaWebhook(int? id) =>
        View(id is null ? new Webhook { DangHoatDong = true } : await db.Webhooks.FindAsync(id) is { } w ? w : null);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SuaWebhook(Webhook input, List<string> suKien)
    {
        input.SuKien = string.Join(",", suKien.Where(s => SuKienWebhook.TatCa.Any(x => x.Ma == s)));
        if (input.SuKien.Length == 0) ModelState.AddModelError("", "Chọn ít nhất một sự kiện.");
        if (!Uri.TryCreate(input.Url, UriKind.Absolute, out var u) || u.Scheme is not ("http" or "https")) ModelState.AddModelError(nameof(input.Url), "URL phải bắt đầu bằng http:// hoặc https://");
        ModelState.Remove(nameof(input.SuKien)); ModelState.Remove(nameof(input.BiMat));
        if (!ModelState.IsValid) return View(input);
        var w = input.Id == 0 ? new Webhook { BiMat = TichHopService.TaoBiMat() } : await db.Webhooks.FindAsync(input.Id);
        if (w is null) return NotFound();
        w.Ten = input.Ten; w.Url = input.Url; w.SuKien = input.SuKien; w.DangHoatDong = input.DangHoatDong;
        if (input.Id == 0) db.Webhooks.Add(w);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu webhook '{w.Ten}'. Bí mật ký HMAC: {w.BiMat}";
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiBiMat(int id)
    {
        var w = await db.Webhooks.FindAsync(id);
        if (w is null) return NotFound();
        w.BiMat = TichHopService.TaoBiMat(); await db.SaveChangesAsync();
        TempData["ok"] = $"Bí mật mới của '{w.Ten}': {w.BiMat} — cập nhật cho hệ thống nhận.";
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaWebhook(int id)
    {
        var w = await db.Webhooks.FindAsync(id);
        if (w is null) return NotFound();
        db.Webhooks.Remove(w); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã xoá webhook '{w.Ten}'.";
        return RedirectToAction(nameof(Webhooks));
    }

    /// <summary>Gửi thử sự kiện "ping" ngay lập tức.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ThuGui(int id, CancellationToken ct)
    {
        var w = await db.Webhooks.FindAsync(id);
        if (w is null) return NotFound();
        var now = DateTime.Now;
        var g = new WebhookGui { WebhookId = w.Id, Webhook = w, SuKien = "ping", ThoiGianTao = now, GuiTu = now,
            NoiDung = System.Text.Json.JsonSerializer.Serialize(new { suKien = "ping", thoiGian = now, duLieu = new { webhook = w.Ten } }) };
        db.WebhookGuis.Add(g);
        await db.SaveChangesAsync();
        await sender.GuiAsync(g, ct);
        await db.SaveChangesAsync();
        if (g.TrangThai == TrangThaiGuiWebhook.DaGui) TempData["ok"] = $"Gửi thử thành công (HTTP {g.MaHttp})."; else TempData["err"] = $"Gửi thử lỗi: {g.LoiCuoi} — sẽ tự thử lại.";
        return RedirectToAction(nameof(Webhooks));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiLai(long id)
    {
        var g = await db.WebhookGuis.FindAsync(id);
        if (g is null) return NotFound();
        g.TrangThai = TrangThaiGuiWebhook.Cho; g.SoLan = 0; g.GuiTu = DateTime.Now;
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã xếp lại hàng đợi — sẽ gửi trong vòng 30 giây.";
        return RedirectToAction(nameof(Webhooks));
    }
}
