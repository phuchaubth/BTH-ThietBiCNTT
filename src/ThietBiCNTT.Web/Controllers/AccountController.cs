using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Controllers;

[AllowAnonymous]
public class AccountController(SignInManager<AppUser> signIn, UserManager<AppUser> users, ThietBiCNTT.Infrastructure.Services.ThamSoService thamSo) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, bool remember, string? returnUrl = null)
    {
        var user = await users.FindByNameAsync(userName ?? "");
        if (user is { DangHoatDong: false })
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["Error"] = "Tài khoản đã bị vô hiệu hoá. Liên hệ Phòng CNTT.";
            return View();
        }
        // QĐ-05: tài khoản khoa (máy dùng chung) không được ghi nhớ đăng nhập, trừ khi tham số cho phép.
        if (user is not null && remember && Infrastructure.PhienDangNhap.LaTaiKhoanKhoa(await users.GetRolesAsync(user))
            && !thamSo.BatTat(ThietBiCNTT.Infrastructure.Services.TS.ChoGhiNhoVoiKhoa))
            remember = false;
        var rs = await signIn.PasswordSignInAsync(userName ?? "", password ?? "", remember, lockoutOnFailure: true);
        if (rs.Succeeded) return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Error"] = rs.IsLockedOut ? "Tài khoản tạm khoá do đăng nhập sai nhiều lần. Thử lại sau 15 phút." : "Sai tên đăng nhập hoặc mật khẩu.";
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return RedirectToAction(nameof(Login)); }

    public IActionResult AccessDenied() => View();
}
