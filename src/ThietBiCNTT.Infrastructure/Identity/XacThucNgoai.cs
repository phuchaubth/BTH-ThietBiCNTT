using System.DirectoryServices.Protocols;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Infrastructure.Identity;

/// <summary>
/// Kiểm tra mật khẩu tên miền (Active Directory) bằng LDAP bind — cấu hình trong appsettings:
/// <c>Ldap:Server</c> (VD dc01.bth.local), <c>Ldap:Port</c> (mặc định 636), <c>Ldap:Ssl</c> (mặc định true),
/// <c>Ldap:TenMien</c> (hậu tố UPN, VD bth.local). Không lưu mật khẩu AD trong hệ thống.
/// </summary>
public class LdapService(IConfiguration cfg, ILogger<LdapService> log)
{
    public bool DaCauHinh => !string.IsNullOrWhiteSpace(cfg["Ldap:Server"]);

    /// <summary>Tên đăng nhập AD (UPN) của tài khoản: dùng nguyên nếu đã có @, ngược lại ghép hậu tố tên miền.</summary>
    public string Upn(string tenDangNhap) => tenDangNhap.Contains('@') || string.IsNullOrWhiteSpace(cfg["Ldap:TenMien"]) ? tenDangNhap : $"{tenDangNhap}@{cfg["Ldap:TenMien"]}";

    /// <returns>true nếu đúng mật khẩu; false nếu sai. Lỗi kết nối → DomainException.</returns>
    public Task<bool> KiemTraAsync(string tenDangNhap, string matKhau) => Task.Run(() =>
    {
        if (!DaCauHinh) throw new DomainException("Chưa cấu hình máy chủ AD (Ldap:Server) — liên hệ Phòng CNTT.");
        // AD chấp nhận "bind ẩn danh" khi mật khẩu rỗng → phải chặn, nếu không ai cũng đăng nhập được.
        if (string.IsNullOrEmpty(matKhau) || string.IsNullOrWhiteSpace(tenDangNhap)) return false;
        var ssl = cfg.GetValue("Ldap:Ssl", true);
        var id = new LdapDirectoryIdentifier(cfg["Ldap:Server"]!, cfg.GetValue("Ldap:Port", ssl ? 636 : 389));
        using var c = new LdapConnection(id) { AuthType = AuthType.Basic, Timeout = TimeSpan.FromSeconds(8) };
        c.SessionOptions.ProtocolVersion = 3;
        if (ssl) c.SessionOptions.SecureSocketLayer = true;
        try { c.Bind(new NetworkCredential(Upn(tenDangNhap), matKhau)); return true; }
        catch (LdapException ex) when (ex.ErrorCode == 49) { return false; } // invalid credentials
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException or PlatformNotSupportedException or DllNotFoundException)
        {
            log.LogWarning(ex, "Không kết nối được máy chủ AD {Server}", cfg["Ldap:Server"]);
            throw new DomainException("Không kết nối được máy chủ AD — thử lại sau hoặc liên hệ Phòng CNTT.");
        }
    });
}

/// <summary>Mở rộng SignInManager: đăng nhập sau khi đã xác thực bằng nguồn ngoài (AD) nhưng vẫn qua bước 2 lớp nếu bật.</summary>
public class AppSignInManager(UserManager<AppUser> um, IHttpContextAccessor ctx, IUserClaimsPrincipalFactory<AppUser> f, IOptions<IdentityOptions> o,
    ILogger<SignInManager<AppUser>> l, IAuthenticationSchemeProvider s, IUserConfirmation<AppUser> c)
    : SignInManager<AppUser>(um, ctx, f, o, l, s, c)
{
    public Task<SignInResult> DangNhapSauXacThucNgoaiAsync(AppUser u, bool ghiNho) => SignInOrTwoFactorAsync(u, ghiNho);
}
