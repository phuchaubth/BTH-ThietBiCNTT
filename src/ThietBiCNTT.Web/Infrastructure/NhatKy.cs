using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Mã thao tác trong nhật ký hệ thống (ngoài Them/Sua/Xoa tự động khi lưu dữ liệu).</summary>
public static class ThaoTac
{
    public const string DangNhap = "DangNhap", DangNhapLoi = "DangNhapLoi", DangXuat = "DangXuat", Xem = "XemNhayCam", Xuat = "XuatDuLieu";

    public static string Ten(string ma) => ma switch
    {
        "Them" => "Thêm", "Sua" => "Sửa", "Xoa" => "Xoá", DangNhap => "Đăng nhập", DangNhapLoi => "Đăng nhập lỗi", DangXuat => "Đăng xuất",
        Xem => "Xem thông tin nhạy cảm", Xuat => "Xuất dữ liệu", _ => ma
    };

    public static readonly string[] TatCa = ["Them", "Sua", "Xoa", DangNhap, DangNhapLoi, DangXuat, Xem, Xuat];
}

/// <summary>Ghi nhật ký thao tác không gắn với thay đổi dữ liệu (FR-HT-07).</summary>
public class NhatKyService(AppDbContext db, IHttpContextAccessor http)
{
    public async Task GhiAsync(string hanhDong, string doiTuong, string? doiTuongId, string? chiTiet, string? nguoiDung = null)
    {
        db.NhatKyHeThongs.Add(new NhatKyHeThong
        {
            ThoiGian = DateTime.Now, HanhDong = hanhDong, DoiTuong = doiTuong, DoiTuongId = doiTuongId is { Length: > 50 } ? doiTuongId[..50] : doiTuongId,
            ChiTiet = chiTiet, NguoiDung = nguoiDung ?? http.HttpContext?.User.Identity?.Name,
            DiaChiIp = http.HttpContext?.Connection.RemoteIpAddress?.ToString()
        });
        await db.SaveChangesAsync();
    }
}

/// <summary>Ghi nhật ký mọi lần tải tệp Excel/PDF/CSV do hệ thống dựng (xuất dữ liệu) — trừ tệp đính kèm người dùng tải lên.</summary>
public class GhiNhatKyXuatFilter(NhatKyService nhatKy) : IAsyncResultFilter
{
    private static readonly string[] KieuXuat = ["spreadsheetml", "application/pdf", "text/csv", "ms-excel"];
    private static readonly string[] BoQua = ["Tep", "Agent"];

    public async Task OnResultExecutionAsync(ResultExecutingContext ctx, ResultExecutionDelegate next)
    {
        if (ctx.Result is FileResult f && ctx.HttpContext.User.Identity?.IsAuthenticated == true
            && KieuXuat.Any(k => f.ContentType.Contains(k, StringComparison.OrdinalIgnoreCase))
            && ctx.RouteData.Values["controller"]?.ToString() is { } c && !BoQua.Contains(c))
        {
            var req = ctx.HttpContext.Request;
            await nhatKy.GhiAsync(ThaoTac.Xuat, c, ctx.RouteData.Values["id"]?.ToString(),
                $"{req.Method} {req.Path}{req.QueryString} → {f.FileDownloadName}".Trim());
        }
        await next();
    }
}
