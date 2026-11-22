using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

public class ThongBaoController(AppDbContext db) : Controller
{
    private IQueryable<ThongBao> CuaToi() => db.ThongBaos.Where(t => t.NguoiNhan == User.Identity!.Name);

    public async Task<IActionResult> Index(bool chuaDoc = false, int page = 1)
    {
        var q = CuaToi(); if (chuaDoc) q = q.Where(t => !t.DaDoc);
        ViewData["chuaDoc"] = chuaDoc; ViewData["page"] = page;
        return View(await q.OrderByDescending(t => t.Id).Skip((page - 1) * 50).Take(50).AsNoTracking().ToListAsync());
    }

    public async Task<IActionResult> Mo(long id)
    {
        var t = await CuaToi().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!t.DaDoc) { t.DaDoc = true; t.ThoiGianDoc = DateTime.Now; await db.SaveChangesAsync(); }
        return LocalRedirect(t.Link is { Length: > 0 } l && Url.IsLocalUrl(l) ? l : "/ThongBao");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DocTatCa()
    {
        await CuaToi().Where(t => !t.DaDoc).ExecuteUpdateAsync(s => s.SetProperty(t => t.DaDoc, true).SetProperty(t => t.ThoiGianDoc, DateTime.Now));
        return Redirect(Request.Headers.Referer.FirstOrDefault() is { } r && Url.IsLocalUrl(new Uri(r).PathAndQuery) ? new Uri(r).PathAndQuery : "/ThongBao");
    }

    public async Task<IActionResult> Dem() => Json(await CuaToi().CountAsync(t => !t.DaDoc));
}

/// <summary>Tài khoản của tôi: liên hệ, kênh nhận thông báo, Telegram.</summary>
public class TaiKhoanController(UserManager<AppUser> users, AppDbContext db, ThongBaoService tb, HangDoiGuiService hangDoi) : Controller
{
    public async Task<IActionResult> Index() => View(await users.GetUserAsync(User));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(string? email, string? dienThoai, string? telegramChatId, bool nhanEmail, bool nhanTelegram)
    {
        var u = await users.GetUserAsync(User);
        if (u is null) return Challenge();
        if (!string.IsNullOrWhiteSpace(email) && !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email.Trim()))
        { TempData["err"] = "Email không hợp lệ."; return RedirectToAction(nameof(Index)); }
        if (!string.IsNullOrWhiteSpace(telegramChatId) && !long.TryParse(telegramChatId.Trim(), out _))
        { TempData["err"] = "Chat ID Telegram là một số (VD 123456789)."; return RedirectToAction(nameof(Index)); }
        u.Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(); u.PhoneNumber = dienThoai?.Trim();
        u.TelegramChatId = string.IsNullOrWhiteSpace(telegramChatId) ? null : telegramChatId.Trim();
        u.NhanEmail = nhanEmail; u.NhanTelegram = nhanTelegram;
        await users.UpdateAsync(u);
        TempData["ok"] = "Đã lưu.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Gửi một thông báo thử cho chính mình qua mọi kênh đang bật.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiThu()
    {
        await tb.GuiAsync([User.Identity!.Name], LoaiThongBao.HeThong, "Thông báo thử", "Nếu bạn nhận được tin này, kênh thông báo đã hoạt động.", "/TaiKhoan");
        await db.SaveChangesAsync();
        await hangDoi.XuLyAsync();
        var loi = await db.TinNhanChos.Where(t => t.ThoiGianTao >= DateTime.Now.AddMinutes(-1) && t.LoiCuoi != null).OrderByDescending(t => t.Id).Select(t => t.Kenh + ": " + t.LoiCuoi).FirstOrDefaultAsync();
        TempData[loi is null ? "ok" : "err"] = loi is null ? "Đã gửi thử — kiểm tra chuông, email, Telegram." : "Gửi thử lỗi — " + loi;
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Quản trị: theo dõi hàng đợi email/Telegram, gửi lại tin lỗi, gửi thử tới địa chỉ bất kỳ.</summary>
[Authorize(Roles = VaiTro.QuanTri)]
public class HangDoiController(AppDbContext db, HangDoiGuiService hangDoi) : Controller
{
    public async Task<IActionResult> Index(TrangThaiGui? trangThai)
    {
        var q = db.TinNhanChos.AsNoTracking();
        if (trangThai is not null) q = q.Where(t => t.TrangThai == trangThai);
        ViewData["trangThai"] = trangThai;
        ViewBag.Dem = await db.TinNhanChos.GroupBy(t => t.TrangThai).ToDictionaryAsync(g => g.Key, g => g.Count());
        return View(await q.OrderByDescending(t => t.Id).Take(200).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiLai(long[] ids)
    {
        var ds = await db.TinNhanChos.Where(t => ids.Contains(t.Id) && t.TrangThai == TrangThaiGui.Loi).ToListAsync();
        foreach (var t in ds) t.GuiLai(DateTime.Now);
        await db.SaveChangesAsync();
        await hangDoi.XuLyAsync();
        TempData["ok"] = $"Đã gửi lại {ds.Count} tin.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GuiThu(KenhGui kenh, string diaChi)
    {
        if (string.IsNullOrWhiteSpace(diaChi)) { TempData["err"] = "Nhập địa chỉ email / chat ID."; return RedirectToAction(nameof(Index)); }
        var t = new TinNhanCho { Kenh = kenh, DiaChi = diaChi.Trim(), TieuDe = "Thử kênh gửi — Quản lý TTB CNTT",
            NoiDung = kenh == KenhGui.Email ? "<p>Email thử từ hệ thống Quản lý Trang thiết bị CNTT.</p>" : "<b>Tin thử</b> từ hệ thống Quản lý TTB CNTT",
            ThoiGianTao = DateTime.Now, GuiTu = DateTime.Now };
        db.TinNhanChos.Add(t);
        await db.SaveChangesAsync();
        await hangDoi.XuLyAsync();
        TempData[t.TrangThai == TrangThaiGui.DaGui ? "ok" : "err"] = t.TrangThai == TrangThaiGui.DaGui ? $"Đã gửi thử {kenh} tới {t.DiaChi}." : $"Gửi thử lỗi: {t.LoiCuoi}";
        return RedirectToAction(nameof(Index));
    }
}
