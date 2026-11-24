using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public record DongTieuChuan(TieuChuanJci TieuChuan, KetQuaBangChung BangChung, DanhGiaTieuChuanJci? DanhGia);

/// <summary>Phân hệ 4.8 – hồ sơ bằng chứng JCI: danh mục tiêu chuẩn và tự đánh giá (FR-JC-01, 02, 08), bộ chỉ số (FR-JC-03), truy vết theo hệ thống (FR-JC-06).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class JciController(AppDbContext db, BangChungJciService bangChung, ChiSoJciService chiSo, HaTangService haTang, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> TieuChuan(string? chuong, MucBangChung? bangChungLoc)
    {
        var tc = await db.TieuChuanJcis.AsNoTracking().OrderBy(t => t.Ma).ToListAsync();
        var bc = await bangChung.TinhTatCaAsync(DateTime.Now);
        var dg = (await db.DanhGiaTieuChuanJcis.AsNoTracking().ToListAsync()).GroupBy(d => d.TieuChuanJciId).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.NgayDanhGia).First());
        var dong = tc.Select(t => new DongTieuChuan(t, bc.GetValueOrDefault(t.Ma) ?? new KetQuaBangChung(MucBangChung.ChuaTuDong, ""), dg.GetValueOrDefault(t.Id))).ToList();
        ViewBag.Tong = dong.GroupBy(d => d.BangChung.Muc).ToDictionary(g => g.Key, g => g.Count());
        if (!string.IsNullOrEmpty(chuong)) dong = dong.Where(d => d.TieuChuan.Chuong == chuong).ToList();
        if (bangChungLoc is not null) dong = dong.Where(d => d.BangChung.Muc == bangChungLoc).ToList();
        ViewData["chuong"] = chuong; ViewData["bangChung"] = bangChungLoc;
        ViewBag.DsChuong = tc.Select(t => t.Chuong).Distinct().ToList();
        return View(dong);
    }

    /// <summary>FR-JC-02: người phụ trách đánh giá bổ sung (Đạt / Đạt một phần / Không đạt).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> DanhGia(int id, DanhGiaTieuChuan danhGia, string? ghiChu)
    {
        if (!await db.TieuChuanJcis.AnyAsync(t => t.Id == id)) return NotFound();
        if (danhGia != DanhGiaTieuChuan.Dat && string.IsNullOrWhiteSpace(ghiChu)) { TempData["err"] = "Đánh giá Đạt một phần / Không đạt cần ghi chú lý do."; return RedirectToAction(nameof(TieuChuan)); }
        db.DanhGiaTieuChuanJcis.Add(new DanhGiaTieuChuanJci { TieuChuanJciId = id, DanhGia = danhGia, GhiChu = ghiChu?.Trim(), NguoiDanhGia = User.Identity!.Name, NgayDanhGia = DateTime.Now });
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã ghi đánh giá.";
        return RedirectToAction(nameof(TieuChuan));
    }

    /// <summary>FR-JC-01: Phòng QLCL bổ sung tóm lược / ghi chú theo sách tiêu chuẩn (Quản trị thực hiện thay).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.QuanTri)]
    public async Task<IActionResult> GhiChu(int id, string tomTat, string? ghiChu)
    {
        var t = await db.TieuChuanJcis.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(tomTat)) t.TomTat = tomTat.Trim();
        t.GhiChu = ghiChu?.Trim();
        await db.SaveChangesAsync(); TempData["ok"] = $"Đã cập nhật {t.Ma}.";
        return RedirectToAction(nameof(TieuChuan));
    }

    public async Task<IActionResult> ChiSo(int? nam, int? quy)
    {
        var d = DateTime.Today; nam ??= d.Year; quy ??= (d.Month - 1) / 3 + 1;
        var (tu, den) = BaoCaoLanhDao.KhoangQuy(nam.Value, Math.Clamp(quy.Value, 1, 4));
        ViewData["nam"] = nam; ViewData["quy"] = quy; ViewBag.Tu = tu; ViewBag.Den = den;
        return View(await chiSo.TinhAsync(tu, den, DateTime.Now));
    }

    /// <summary>FR-JC-06: chuỗi bằng chứng của một hệ thống trên một trang (dùng khi đoàn đánh giá truy vết).</summary>
    public async Task<IActionResult> Tracer(int id)
    {
        var h = await db.HeThongThongTins.AsNoTracking().Include(x => x.NhaCungCap).Include(x => x.HopDong).Include(x => x.KhoaPhongs).ThenInclude(k => k.KhoaPhong).FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        ViewBag.KeHoach = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == id).OrderByDescending(k => k.PhienBan).ToListAsync();
        ViewBag.DaoTao = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).Include(b => b.KhoaPhong).Where(b => b.HeThongId == id || (b.HeThongId == null && b.Loai == LoaiDaoTao.Downtime)).OrderByDescending(b => b.Ngay).Take(8).ToListAsync();
        ViewBag.DienTap = await db.DotDienTaps.AsNoTracking().Where(x => x.HeThongId == id).OrderByDescending(x => x.HanThucHien).Take(6).ToListAsync();
        ViewBag.SuKien = await db.SuKienDowntimes.AsNoTracking().Where(s => s.HeThongs.Any(x => x.HeThongId == id)).OrderByDescending(s => s.BatDau ?? s.DuKienBatDau).Take(8).ToListAsync();
        ViewBag.TacVu = await db.TacVuSaoLuus.AsNoTracking().Where(t => t.HeThongId == id).ToListAsync();
        ViewBag.PhucHoi = await db.LanThuPhucHois.AsNoTracking().Where(l => l.HeThongId == id).OrderByDescending(l => l.HanThucHien).Take(6).ToListAsync();
        ViewBag.RuiRo = await db.RuiRos.AsNoTracking().Where(r => r.HeThongId == id).OrderByDescending(r => r.KhaNang * r.HauQua).Take(8).ToListAsync();
        ViewBag.SuCoAn = await db.SuCoAnNinhs.AsNoTracking().Where(s => s.HeThongId == id).OrderByDescending(s => s.PhatHien).Take(6).ToListAsync();
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(x => x.HeThongId == id).OrderBy(x => x.TrangThai == TrangThaiHanhDong.DaDong).ThenBy(x => x.Han).Take(8).ToListAsync();
        ViewBag.TaiLieu = await db.TaiLieuQuyDinhs.AsNoTracking().Where(t => t.HeThongId == id && t.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync();
        ViewBag.GiamSat = await db.NguoiGiamSatCntts.AsNoTracking().OrderByDescending(x => x.NgayBatDau).FirstOrDefaultAsync(x => x.NgayKetThuc == null || x.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        ViewBag.Rto = thamSo.Rto(h); ViewBag.Rpo = thamSo.Rpo(h); ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.PhuThuoc = await db.PhuThuocHeThongs.AsNoTracking().Include(p => p.TaiSan).Include(p => p.HeThongPhuThuoc).Where(p => p.HeThongId == id).ToListAsync();
        return View(h);
    }
}
