using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

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
public class TaiKhoanController(UserManager<AppUser> users, AppDbContext db, ThongBaoService tb, HangDoiGuiService hangDoi, SignInManager<AppUser> signIn, ThamSoService thamSo) : Controller
{
    // ===== Xác thực 2 lớp (TOTP) =====
    public async Task<IActionResult> XacThuc2Lop(bool batBuoc = false)
    {
        var u = await users.GetUserAsync(User);
        if (u is null) return Challenge();
        ViewData["batBuoc"] = batBuoc;
        if (!u.TwoFactorEnabled)
        {
            var key = await users.GetAuthenticatorKeyAsync(u);
            if (string.IsNullOrEmpty(key)) { await users.ResetAuthenticatorKeyAsync(u); key = await users.GetAuthenticatorKeyAsync(u); }
            var ten = Uri.EscapeDataString("TTB CNTT");
            var uri = $"otpauth://totp/{ten}:{Uri.EscapeDataString(u.UserName!)}?secret={key}&issuer={ten}&digits=6";
            using var gen = new QRCoder.QRCodeGenerator();
            ViewBag.Qr = "data:image/png;base64," + Convert.ToBase64String(new QRCoder.PngByteQRCode(gen.CreateQrCode(uri, QRCoder.QRCodeGenerator.ECCLevel.M)).GetGraphic(5));
            ViewBag.Khoa = string.Join(" ", Enumerable.Range(0, (key!.Length + 3) / 4).Select(i => key.Substring(i * 4, Math.Min(4, key.Length - i * 4)))).ToLowerInvariant();
        }
        else ViewBag.SoMaKhoiPhuc = await users.CountRecoveryCodesAsync(u);
        return View(u);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BatXacThuc2Lop(string ma)
    {
        var u = await users.GetUserAsync(User);
        if (u is null) return Challenge();
        var code = new string((ma ?? "").Where(char.IsDigit).ToArray());
        if (!await users.VerifyTwoFactorTokenAsync(u, users.Options.Tokens.AuthenticatorTokenProvider, code))
        { TempData["err"] = "Mã không đúng — kiểm tra giờ trên điện thoại (tự động) và nhập mã mới nhất."; return RedirectToAction(nameof(XacThuc2Lop)); }
        await users.SetTwoFactorEnabledAsync(u, true);
        TempData["maKhoiPhuc"] = string.Join(" ", (await users.GenerateNewTwoFactorRecoveryCodesAsync(u, 10))!);
        db.NhatKyHeThongs.Add(new NhatKyHeThong { ThoiGian = DateTime.Now, NguoiDung = u.UserName, HanhDong = "Sua", DoiTuong = "NguoiDung", DoiTuongId = u.UserName, ChiTiet = "Bật xác thực 2 lớp" });
        await db.SaveChangesAsync();
        await signIn.RefreshSignInAsync(u);
        TempData["ok"] = "Đã bật xác thực 2 lớp. Lưu lại các mã khôi phục bên dưới ở nơi an toàn.";
        return RedirectToAction(nameof(XacThuc2Lop));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoMaKhoiPhuc()
    {
        var u = await users.GetUserAsync(User);
        if (u is null || !u.TwoFactorEnabled) return RedirectToAction(nameof(XacThuc2Lop));
        TempData["maKhoiPhuc"] = string.Join(" ", (await users.GenerateNewTwoFactorRecoveryCodesAsync(u, 10))!);
        TempData["ok"] = "Đã tạo bộ mã khôi phục mới — các mã cũ hết hiệu lực.";
        return RedirectToAction(nameof(XacThuc2Lop));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TatXacThuc2Lop(string ma)
    {
        var u = await users.GetUserAsync(User);
        if (u is null) return Challenge();
        if (thamSo.BatTat(TS.BatBuoc2FA) && User.LaCNTT()) { TempData["err"] = "Tài khoản Quản trị / CNTT bắt buộc dùng xác thực 2 lớp (tham số hệ thống)."; return RedirectToAction(nameof(XacThuc2Lop)); }
        var code = new string((ma ?? "").Where(char.IsDigit).ToArray());
        if (!await users.VerifyTwoFactorTokenAsync(u, users.Options.Tokens.AuthenticatorTokenProvider, code))
        { TempData["err"] = "Mã không đúng."; return RedirectToAction(nameof(XacThuc2Lop)); }
        await users.SetTwoFactorEnabledAsync(u, false);
        await users.ResetAuthenticatorKeyAsync(u);
        db.NhatKyHeThongs.Add(new NhatKyHeThong { ThoiGian = DateTime.Now, NguoiDung = u.UserName, HanhDong = "Sua", DoiTuong = "NguoiDung", DoiTuongId = u.UserName, ChiTiet = "Tắt xác thực 2 lớp" });
        await db.SaveChangesAsync();
        await signIn.RefreshSignInAsync(u);
        TempData["ok"] = "Đã tắt xác thực 2 lớp.";
        return RedirectToAction(nameof(XacThuc2Lop));
    }

    public async Task<IActionResult> Index() => View(await users.GetUserAsync(User));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Luu(string? email, string? dienThoai, string? telegramChatId, bool nhanEmail, bool nhanTelegram, string? zaloUserId, bool nhanZalo)
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
        if (!string.IsNullOrWhiteSpace(zaloUserId) && !zaloUserId.Trim().All(char.IsDigit)) { TempData["err"] = "Zalo user_id là một dãy số."; return RedirectToAction(nameof(Index)); }
        u.ZaloUserId = string.IsNullOrWhiteSpace(zaloUserId) ? null : zaloUserId.Trim(); u.NhanZalo = nhanZalo;
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
