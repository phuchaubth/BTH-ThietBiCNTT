using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public record ThanhPhanTienIch(TaiSan TaiSan, int SoHeThong, int SoHeThongToiQuanTrong, bool CoDuPhong, KeHoachBaoTri? KeHoachBaoTri, DateTime? BaoTriCuoi, bool QuaHanBaoTri, bool TacDongLon);
public record NguonDuPhong(MauKiemTra Mau, LanKiemTra? LanCuoi, DateTime? HanTiep, bool QuaHan, bool ChuKyQuaDai);

/// <summary>
/// FR-HTT-15, FR-HTT-16 (FMS.08.00 ME1–ME4, FMS.08.01 ME3): hệ thống thông tin, liên lạc, dữ liệu là hệ thống tiện ích —
/// chương trình, danh mục thành phần có kế hoạch bảo trì, và thử nguồn điện dự phòng ít nhất mỗi quý.
/// </summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class TienIchController(AppDbContext db, HaTangService haTang, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index()
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today); var now = DateTime.Now;
        // ===== Chương trình (ME1, FMS.01.01 ME2) =====
        ViewBag.ChuongTrinh = await db.TaiLieuQuyDinhs.AsNoTracking().Where(t => t.Loai == LoaiTaiLieu.ChuongTrinhTienIch && t.TrangThai == TrangThaiKeHoach.HieuLuc).OrderByDescending(t => t.PhienBan).FirstOrDefaultAsync();
        ViewBag.PhamVi = await db.TaiLieuQuyDinhs.AsNoTracking().Where(t => t.Loai == LoaiTaiLieu.PhamViDichVu && t.TrangThai == TrangThaiKeHoach.HieuLuc).OrderByDescending(t => t.PhienBan).FirstOrDefaultAsync();
        // ===== Danh mục thành phần (ME3) và chu kỳ bảo trì (ME4) =====
        var canh = await haTang.CanhAsync();
        var tsIds = canh.Where(c => c.TaiSanId is not null).Select(c => c.TaiSanId!.Value).Distinct().ToList();
        var taiSans = await db.TaiSans.AsNoTracking().Include(t => t.KhoaPhong).Include(t => t.LoaiThietBi).Include(t => t.ViTri).Where(t => tsIds.Contains(t.Id) && t.TrangThai != TrangThaiTaiSan.DaThanhLy).OrderBy(t => t.MaTaiSan).ToListAsync();
        var hts = (await db.HeThongThongTins.AsNoTracking().ToListAsync()).ToDictionary(h => h.Id);
        var kh = await db.KeHoachBaoTris.AsNoTracking().Where(k => k.DangApDung).ToListAsync();
        var cuoi = (await db.PhieuSuaChuas.AsNoTracking().Where(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy && p.NgayHoanThanh != null && tsIds.Contains(p.TaiSanId)).Select(p => new { p.TaiSanId, p.NgayHoanThanh }).ToListAsync())
            .GroupBy(x => x.TaiSanId).ToDictionary(g => g.Key, g => g.Max(x => x.NgayHoanThanh));
        var dong = new List<ThanhPhanTienIch>();
        foreach (var t in taiSans)
        {
            var anh = PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(t.Id, canh).Select(i => hts[i]).Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung).ToList();
            var k = kh.FirstOrDefault(x => x.TaiSanId == t.Id) ?? kh.FirstOrDefault(x => x.TaiSanId == null && x.LoaiThietBiId == t.LoaiThietBiId);
            var coDp = canh.Where(c => c.TaiSanId == t.Id).All(c => c.CoDuPhong);
            dong.Add(new ThanhPhanTienIch(t, anh.Count, anh.Count(h => h.MucTrongYeu == MucTrongYeu.ToiQuanTrong), coDp, k, cuoi.GetValueOrDefault(t.Id), k is not null && k.DenHan(homNay), anh.Any(h => h.MucTrongYeu == MucTrongYeu.ToiQuanTrong) || anh.Count(h => h.LaTrongYeu) >= 2));
        }
        ViewBag.ThanhPhan = dong.OrderByDescending(x => x.TacDongLon).ThenBy(x => x.TaiSan.MaTaiSan).ToList();
        // ===== Thử nguồn điện dự phòng (FMS.08.01 ME3) =====
        var thang = thamSo.SoNguyen(TS.JciThuNguonDuPhongThang);
        var maus = await db.MauKiemTras.AsNoTracking().Include(m => m.KhoaPhong).Where(m => m.LaThuNguonDuPhong && m.DangApDung).OrderBy(m => m.Ten).ToListAsync();
        var lans = (await db.LanKiemTras.AsNoTracking().Include(l => l.KetQuas).Where(l => maus.Select(m => m.Id).Contains(l.MauKiemTraId)).ToListAsync()).GroupBy(l => l.MauKiemTraId).ToDictionary(g => g.Key, g => g.OrderByDescending(l => l.ThoiGian).First());
        ViewBag.Nguon = maus.Select(m =>
        {
            var l = lans.GetValueOrDefault(m.Id); var han = (l?.ThoiGian ?? m.NgayTao).AddHours(Math.Min(m.ChuKyGio, thang * 31 * 24));
            return new NguonDuPhong(m, l, han, han < now, m.ChuKyGio > thang * 31 * 24);
        }).ToList();
        ViewBag.Thang = thang; ViewBag.HomNay = homNay;
        return View();
    }
}
