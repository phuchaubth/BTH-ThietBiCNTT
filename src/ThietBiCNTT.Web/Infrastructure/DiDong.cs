using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Một mục menu điện thoại (FR-HT-16).</summary>
public record MucDiDong(string Ma, string Ten, string Icon, string Url, string MoTa, Func<ClaimsPrincipal, bool> ChoPhep);

/// <summary>Danh mục mục menu điện thoại và cấu hình theo vai trò (tham số Mobile.Menu.*).</summary>
public static class DiDong
{
    private static bool QuanLy(ClaimsPrincipal u) => u.LaCNTT() || u.IsInRole(VaiTro.LanhDao);

    public static readonly MucDiDong[] DanhMuc =
    [
        new("baohong", "Báo hỏng", "bi-exclamation-triangle", "/PhieuSuaChua/Create", "Gửi yêu cầu sửa chữa", _ => true),
        new("yeucau", "Yêu cầu của tôi", "bi-card-checklist", "/PhieuSuaChua?cuaToi=true", "Theo dõi yêu cầu đã gửi", _ => true),
        new("viec", "Việc của tôi", "bi-tools", "/PhieuSuaChua?cuaToi=true", "Phiếu được giao đang mở", u => u.LaCNTT()),
        new("quetqr", "Quét QR", "bi-qr-code-scan", "/m/Quet", "Quét tem để mở hồ sơ thiết bị", _ => true),
        new("kiemtra", "Kiểm tra", "bi-clipboard-check", "/KiemTra", "Phiếu kiểm tra đến hạn", _ => true),
        new("kiemke", "Kiểm kê", "bi-upc-scan", "/KiemKe", "Đợt kiểm kê đang mở", u => QuanLy(u) || u.IsInRole(VaiTro.TruongKhoa)),
        new("duyet", "Chờ duyệt", "bi-check2-square", "/m/ChoDuyet", "Điều chuyển, thanh lý, mua sắm, ngân sách", u => QuanLy(u) || u.IsInRole(VaiTro.TruongKhoa)),
        new("thongbao", "Thông báo", "bi-bell", "/ThongBao", "Thông báo chưa đọc", _ => true),
        new("tongquan", "Tổng quan", "bi-speedometer2", "/", "Dashboard KPI", _ => true),
        new("baocao", "Báo cáo", "bi-bar-chart", "/BaoCao", "SLA, MTTR, chi phí", QuanLy),
        new("taisan", "Tài sản", "bi-pc-display", "/TaiSan", "Tra cứu tài sản", _ => true),
        new("kho", "Kho", "bi-box-seam", "/Kho", "Tồn kho vật tư", QuanLy),
        new("muasam", "Mua sắm", "bi-cart3", "/MuaSam", "Đề xuất, đơn hàng", QuanLy),
        new("ngansach", "Ngân sách", "bi-piggy-bank", "/NganSach", "Dự toán – thực hiện", QuanLy),
    ];

    /// <summary>Vai trò dùng để chọn menu khi người dùng có nhiều vai trò.</summary>
    public static string VaiTroChinh(ClaimsPrincipal u) =>
        new[] { VaiTro.QuanTri, VaiTro.CanBoCNTT, VaiTro.LanhDao, VaiTro.TruongKhoa }.FirstOrDefault(u.IsInRole) ?? VaiTro.NhanVien;

    public static List<MucDiDong> Menu(ClaimsPrincipal u, ThamSoService ts) =>
        ts.Chuoi(TS.MobileMenu(VaiTroChinh(u))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(m => DanhMuc.FirstOrDefault(x => x.Ma == m.ToLowerInvariant())).OfType<MucDiDong>().Where(x => x.ChoPhep(u)).DistinctBy(x => x.Ma).Take(5).ToList();
}

/// <summary>Thanh menu dưới cùng trên điện thoại.</summary>
public class MenuDiDongViewComponent(ThamSoService ts) : ViewComponent
{
    public IViewComponentResult Invoke() => View(DiDong.Menu(UserClaimsPrincipal, ts));
}
