using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Đăng nhập / đăng xuất / bước 2 lớp: cho phép ẩn danh từng action; đổi mật khẩu cần đăng nhập (quy ước mặc định).</summary>
public class AccountController(AppSignInManager signIn, UserManager<AppUser> users, ThietBiCNTT.Infrastructure.Services.ThamSoService thamSo, LdapService ldap, NhatKyService nhatKy) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(); }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Login(string userName, string password, bool remember, string? returnUrl = null)
    {
        var user = await users.FindByNameAsync(userName ?? "");
        if (user is { DangHoatDong: false })
        {
            await nhatKy.GhiAsync(ThaoTac.DangNhapLoi, "NguoiDung", user.UserName, "Tài khoản đã vô hiệu hoá", user.UserName);
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Error"] = "Tài khoản đã bị vô hiệu hoá. Liên hệ Phòng CNTT.";
            return View();
        }
        // QĐ-05: tài khoản khoa (máy dùng chung) không được ghi nhớ đăng nhập, trừ khi tham số cho phép.
        if (user is not null && remember && Infrastructure.PhienDangNhap.LaTaiKhoanKhoa(await users.GetRolesAsync(user))
            && !thamSo.BatTat(ThietBiCNTT.Infrastructure.Services.TS.ChoGhiNhoVoiKhoa))
            remember = false;
        ViewData["ReturnUrl"] = returnUrl;
        Microsoft.AspNetCore.Identity.SignInResult rs;
        if (user is { XacThucAD: true })
        {
            // Tài khoản AD: kiểm tra mật khẩu tên miền qua LDAP, vẫn áp dụng khoá tạm khi sai nhiều lần và bước 2 lớp
            if (await users.IsLockedOutAsync(user)) rs = Microsoft.AspNetCore.Identity.SignInResult.LockedOut;
            else
            {
                bool dung;
                try { dung = await ldap.KiemTraAsync(user.UserName!, password ?? ""); }
                catch (ThietBiCNTT.Domain.Common.DomainException ex) { ViewData["Error"] = ex.Message; return View(); }
                if (dung) { await users.ResetAccessFailedCountAsync(user); rs = await signIn.DangNhapSauXacThucNgoaiAsync(user, remember); }
                else { await users.AccessFailedAsync(user); rs = await users.IsLockedOutAsync(user) ? Microsoft.AspNetCore.Identity.SignInResult.LockedOut : Microsoft.AspNetCore.Identity.SignInResult.Failed; }
            }
        }
        else rs = await signIn.PasswordSignInAsync(userName ?? "", password ?? "", remember, lockoutOnFailure: true);
        var ten = user?.UserName ?? (userName ?? "").Trim();
        if (ten.Length > 100) ten = ten[..100];
        if (rs.Succeeded)
        {
            await nhatKy.GhiAsync(ThaoTac.DangNhap, "NguoiDung", ten, user is { XacThucAD: true } ? "Mật khẩu tên miền (AD)" : "Mật khẩu", ten);
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
        if (rs.RequiresTwoFactor) return RedirectToAction(nameof(XacThuc2Lop), new { returnUrl, remember });
        await nhatKy.GhiAsync(ThaoTac.DangNhapLoi, "NguoiDung", ten, rs.IsLockedOut ? "Tài khoản bị khoá tạm" : user is null ? "Tên đăng nhập không tồn tại" : "Sai mật khẩu", ten);
        ViewData["Error"] = rs.IsLockedOut ? "Tài khoản tạm khoá do đăng nhập sai nhiều lần. Thử lại sau 15 phút."
            : user is { XacThucAD: true } ? "Sai mật khẩu tên miền (AD)." : "Sai tên đăng nhập hoặc mật khẩu.";
        return View();
    }

    /// <summary>Bước 2: mã 6 số từ ứng dụng xác thực, hoặc mã khôi phục.</summary>
    [HttpGet, AllowAnonymous]
    public async Task<IActionResult> XacThuc2Lop(string? returnUrl, bool remember)
    {
        if (await signIn.GetTwoFactorAuthenticationUserAsync() is null) return RedirectToAction(nameof(Login));
        ViewData["ReturnUrl"] = returnUrl; ViewData["Remember"] = remember;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> XacThuc2Lop(string? ma, bool remember, bool laMaKhoiPhuc, string? returnUrl)
    {
        var u = await signIn.GetTwoFactorAuthenticationUserAsync();
        if (u is null) return RedirectToAction(nameof(Login));
        // Mã khôi phục có dạng XXXXX-XXXXX (giữ dấu gạch); mã ứng dụng chỉ lấy chữ số
        var code = laMaKhoiPhuc ? new string((ma ?? "").Where(ch => !char.IsWhiteSpace(ch)).ToArray()) : new string((ma ?? "").Where(char.IsDigit).ToArray());
        var rs = laMaKhoiPhuc ? await signIn.TwoFactorRecoveryCodeSignInAsync(code)
                              : await signIn.TwoFactorAuthenticatorSignInAsync(code, remember, rememberClient: false);
        if (rs.Succeeded)
        {
            await nhatKy.GhiAsync(ThaoTac.DangNhap, "NguoiDung", u.UserName, laMaKhoiPhuc ? "Mật khẩu + mã khôi phục 2 lớp" : "Mật khẩu + xác thực 2 lớp", u.UserName);
            if (laMaKhoiPhuc) TempData["err"] = $"Bạn vừa dùng mã khôi phục — còn {await users.CountRecoveryCodesAsync(u)} mã. Nên tạo bộ mã mới ở trang Tài khoản.";
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        }
        await nhatKy.GhiAsync(ThaoTac.DangNhapLoi, "NguoiDung", u.UserName, "Sai mã xác thực 2 lớp", u.UserName);
        ViewData["ReturnUrl"] = returnUrl; ViewData["Remember"] = remember;
        ViewData["Error"] = rs.IsLockedOut ? "Tài khoản tạm khoá do nhập sai nhiều lần. Thử lại sau 15 phút." : "Mã không đúng hoặc đã hết hạn.";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        if (User.Identity?.IsAuthenticated == true) await nhatKy.GhiAsync(ThaoTac.DangXuat, "NguoiDung", User.Identity.Name, null);
        await signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [HttpGet]
    public IActionResult DoiMatKhau() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiMatKhau(string matKhauCu, string matKhauMoi, string nhapLai)
    {
        if (User.HasClaim("ad", "1")) { ViewData["Error"] = "Tài khoản đăng nhập bằng mật khẩu tên miền (AD) — đổi mật khẩu trên Windows (Ctrl+Alt+Del)."; return View(); }
        if (matKhauMoi != nhapLai) { ViewData["Error"] = "Mật khẩu nhập lại không khớp."; return View(); }
        var u = await users.GetUserAsync(User);
        if (u is null) return RedirectToAction(nameof(Login));
        var rs = await users.ChangePasswordAsync(u, matKhauCu ?? "", matKhauMoi ?? "");
        if (!rs.Succeeded) { ViewData["Error"] = string.Join(" ", rs.Errors.Select(NguoiDungController.DichLoi)); return View(); }
        await signIn.RefreshSignInAsync(u);
        TempData["ok"] = "Đã đổi mật khẩu.";
        return Redirect("/");
    }
}
