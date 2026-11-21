using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Controllers;

[AllowAnonymous]
public class AccountController(SignInManager<AppUser> signIn) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) { ViewData["ReturnUrl"] = returnUrl; return View(); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string userName, string password, bool remember, string? returnUrl = null)
    {
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
