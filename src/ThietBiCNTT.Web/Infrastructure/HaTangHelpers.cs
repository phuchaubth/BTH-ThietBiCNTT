using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Hàm dùng chung cho các màn hình phân hệ hạ tầng CNTT (GĐ3.1).</summary>
public static class HaTangHelpers
{
    /// <summary>Danh sách tài khoản Cán bộ CNTT + Quản trị đang hoạt động (chọn người phụ trách / chịu trách nhiệm).</summary>
    public static async Task<SelectList> DsCnttAsync(this UserManager<AppUser> users, string? chon = null) =>
        new((await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Concat(await users.GetUsersInRoleAsync(VaiTro.QuanTri))
            .Where(u => u.DangHoatDong).DistinctBy(u => u.Id).OrderBy(u => u.HoTen)
            .Select(u => new { u.UserName, Ten = $"{u.HoTen} ({u.UserName})" }), "UserName", "Ten", chon);

    /// <summary>Tên hiển thị tài khoản (họ tên), rơi về tên đăng nhập.</summary>
    public static string TenNguoi(this IReadOnlyDictionary<string, string> hoTen, string? u) =>
        u is null ? "" : hoTen.TryGetValue(u, out var h) && h.Length > 0 ? $"{h} ({u})" : u;

    /// <summary>Chu kỳ sao lưu dạng chữ: "15 phút", "6 giờ", "24 giờ".</summary>
    public static string ChuKyText(int phut) => phut % 60 == 0 ? $"{phut / 60} giờ" : $"{phut} phút";

    public static string ThoiLuongText(TimeSpan? t) =>
        t is not { } x ? "—" : x.TotalHours >= 1 ? $"{(int)x.TotalHours} giờ {x.Minutes} phút" : $"{Math.Max(0, (int)x.TotalMinutes)} phút";
}
