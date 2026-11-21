using Microsoft.AspNetCore.Identity;
using ThietBiCNTT.Domain.Entities;

namespace ThietBiCNTT.Infrastructure.Identity;

public class AppUser : IdentityUser
{
    public string HoTen { get; set; } = "";
    public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    public bool DangHoatDong { get; set; } = true;
}

/// <summary>Các vai trò trong hệ thống.</summary>
public static class VaiTro
{
    public const string QuanTri = "QuanTri";
    public const string CanBoCNTT = "CanBoCNTT";
    public const string TruongKhoa = "TruongKhoa";
    public const string LanhDao = "LanhDao";
    public const string NhanVien = "NhanVien";

    public static readonly string[] TatCa = [QuanTri, CanBoCNTT, TruongKhoa, LanhDao, NhanVien];

    /// <summary>Nhóm được quản lý tài sản / xử lý phiếu.</summary>
    public const string NhomCNTT = QuanTri + "," + CanBoCNTT;
    /// <summary>Nhóm được duyệt điều chuyển / thanh lý.</summary>
    public const string NhomDuyet = QuanTri + "," + LanhDao;

    public static string TenHienThi(string role) => role switch
    {
        QuanTri => "Quản trị hệ thống",
        CanBoCNTT => "Cán bộ CNTT / Kỹ thuật viên",
        TruongKhoa => "Trưởng khoa/phòng",
        LanhDao => "Lãnh đạo / Vật tư / TCKT",
        NhanVien => "Nhân viên",
        _ => role
    };
}

public interface ICurrentUser
{
    string? UserName { get; }
}
