using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Controllers;

public class NguoiDungVm
{
    public string? Id { get; set; }
    public string UserName { get; set; } = "";
    public string HoTen { get; set; } = "";
    public string? Email { get; set; }
    public string? DienThoai { get; set; }
    public int? KhoaPhongId { get; set; }
    public string VaiTro { get; set; } = ThietBiCNTT.Infrastructure.Identity.VaiTro.NhanVien;
    public bool DangHoatDong { get; set; } = true;
    public string? MatKhau { get; set; }
}

/// <summary>Quản lý tài khoản người dùng (chỉ Quản trị).</summary>
[Authorize(Roles = VaiTro.QuanTri)]
public class NguoiDungController(UserManager<AppUser> users, AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(string? q, string? vaiTro)
    {
        var ds = await users.Users.Include(u => u.KhoaPhong).AsNoTracking()
            .Where(u => q == null || u.UserName!.Contains(q) || u.HoTen.Contains(q))
            .OrderBy(u => u.UserName).ToListAsync();
        var roles = new Dictionary<string, string>();
        foreach (var u in ds) roles[u.Id] = (await users.GetRolesAsync(u)).FirstOrDefault() ?? "";
        if (!string.IsNullOrEmpty(vaiTro)) ds = ds.Where(u => roles[u.Id] == vaiTro).ToList();
        ViewBag.Roles = roles; ViewData["q"] = q; ViewData["vaiTro"] = vaiTro;
        return View(ds);
    }

    public async Task<IActionResult> Create() { await Nap(); return View("Form", new NguoiDungVm()); }

    public async Task<IActionResult> Edit(string id)
    {
        var u = await users.FindByIdAsync(id);
        if (u is null) return NotFound();
        await Nap();
        return View("Form", new NguoiDungVm
        {
            Id = u.Id, UserName = u.UserName!, HoTen = u.HoTen, Email = u.Email, DienThoai = u.PhoneNumber,
            KhoaPhongId = u.KhoaPhongId, DangHoatDong = u.DangHoatDong,
            VaiTro = (await users.GetRolesAsync(u)).FirstOrDefault() ?? VaiTro.NhanVien
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(NguoiDungVm vm)
    {
        if (!VaiTro.TatCa.Contains(vm.VaiTro)) ModelState.AddModelError(nameof(vm.VaiTro), "Vai trò không hợp lệ.");
        if (vm.VaiTro is VaiTro.TruongKhoa or VaiTro.NhanVien && vm.KhoaPhongId is null)
            ModelState.AddModelError(nameof(vm.KhoaPhongId), "Trưởng khoa / Nhân viên phải thuộc một khoa/phòng.");
        if (string.IsNullOrWhiteSpace(vm.UserName) || string.IsNullOrWhiteSpace(vm.HoTen)) ModelState.AddModelError("", "Nhập tên đăng nhập và họ tên.");
        if (vm.Id is null && string.IsNullOrWhiteSpace(vm.MatKhau)) ModelState.AddModelError(nameof(vm.MatKhau), "Nhập mật khẩu ban đầu.");
        if (vm.Id == UserId() && (!vm.DangHoatDong || vm.VaiTro != VaiTro.QuanTri))
            ModelState.AddModelError("", "Không tự khoá hoặc tự bỏ quyền Quản trị của chính mình.");
        if (!ModelState.IsValid) { await Nap(); return View("Form", vm); }

        IdentityResult rs;
        AppUser? u;
        if (vm.Id is null)
        {
            u = new AppUser { UserName = vm.UserName.Trim(), EmailConfirmed = true };
            Gan(u, vm);
            rs = await users.CreateAsync(u, vm.MatKhau!);
        }
        else
        {
            u = await users.FindByIdAsync(vm.Id);
            if (u is null) return NotFound();
            Gan(u, vm);
            rs = await users.UpdateAsync(u);
            if (rs.Succeeded && !string.IsNullOrWhiteSpace(vm.MatKhau))
            {
                await users.RemovePasswordAsync(u);
                rs = await users.AddPasswordAsync(u, vm.MatKhau);
            }
            if (rs.Succeeded) await users.UpdateSecurityStampAsync(u); // buộc đăng nhập lại khi đổi quyền/khoá/mật khẩu
        }
        if (rs.Succeeded)
        {
            var cu = await users.GetRolesAsync(u);
            if (!cu.SequenceEqual([vm.VaiTro])) { await users.RemoveFromRolesAsync(u, cu); await users.AddToRoleAsync(u, vm.VaiTro); }
            db.NhatKyHeThongs.Add(new NhatKyHeThong { ThoiGian = DateTime.Now, NguoiDung = User.Identity?.Name, HanhDong = vm.Id is null ? "Them" : "Sua",
                DoiTuong = "NguoiDung", DoiTuongId = u.UserName, ChiTiet = $"Vai trò {vm.VaiTro}, khoa {vm.KhoaPhongId}, hoạt động {vm.DangHoatDong}" + (string.IsNullOrWhiteSpace(vm.MatKhau) ? "" : ", đặt mật khẩu") });
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã lưu tài khoản {u.UserName}.";
            return RedirectToAction(nameof(Index));
        }
        foreach (var e in rs.Errors) ModelState.AddModelError("", DichLoi(e));
        await Nap(); return View("Form", vm);
    }

    private static void Gan(AppUser u, NguoiDungVm vm)
    {
        u.HoTen = vm.HoTen.Trim(); u.Email = vm.Email; u.PhoneNumber = vm.DienThoai;
        u.KhoaPhongId = vm.KhoaPhongId; u.DangHoatDong = vm.DangHoatDong;
    }

    private string? UserId() => users.GetUserId(User);

    public static string DichLoi(IdentityError e) => e.Code switch
    {
        "DuplicateUserName" => "Tên đăng nhập đã tồn tại.",
        "PasswordTooShort" => "Mật khẩu tối thiểu 8 ký tự.",
        "PasswordRequiresDigit" => "Mật khẩu phải có chữ số.",
        "PasswordRequiresUpper" => "Mật khẩu phải có chữ hoa.",
        "PasswordRequiresLower" => "Mật khẩu phải có chữ thường.",
        "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
        "InvalidUserName" => "Tên đăng nhập chỉ gồm chữ không dấu, số và . _ -",
        _ => e.Description
    };

    private async Task Nap()
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsVaiTro = new SelectList(VaiTro.TatCa.Select(r => new { Ma = r, Ten = VaiTro.TenHienThi(r) }), "Ma", "Ten");
    }
}
