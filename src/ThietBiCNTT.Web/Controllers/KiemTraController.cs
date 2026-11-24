using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Kiểm tra thiết bị theo phiếu mẫu.</summary>
public class KiemTraController(AppDbContext db, KiemTraService svc, TepService tep) : Controller
{
    private int? KhoaLoc => User.XemToanVien() ? null : User.KhoaPhongId() ?? -1;
    private bool DuocGhi(MauKiemTra m) => m.DangApDung && (User.LaCNTT() || (m.KhoaPhongId is not null && m.KhoaPhongId == User.KhoaPhongId()));

    /// <summary>Bảng việc: đến hạn / quá hạn, các lần kiểm tra gần đây.</summary>
    public async Task<IActionResult> Index(bool tatCa = false)
    {
        var now = DateTime.Now;
        ViewBag.Viec = await svc.DanhSachViecAsync(now, KhoaLoc, chiDenHan: !tatCa);
        var lan = db.LanKiemTras.AsNoTracking().Include(l => l.MauKiemTra).Include(l => l.TaiSan).Include(l => l.PhieuSuaChua).AsQueryable();
        if (KhoaLoc is { } k) lan = lan.Where(l => l.MauKiemTra!.KhoaPhongId == k);
        ViewBag.GanDay = await lan.OrderByDescending(l => l.Id).Take(30).ToListAsync();
        ViewData["tatCa"] = tatCa;
        return View();
    }

    // ===== Mẫu phiếu (CNTT) =====
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Mau() =>
        View(await db.MauKiemTras.AsNoTracking().Include(m => m.LoaiThietBi).Include(m => m.TaiSan).Include(m => m.KhoaPhong).Include(m => m.Mucs)
            .OrderBy(m => m.Ten).ToListAsync());

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> MauForm(int? id)
    {
        var m = id is null ? new MauKiemTra { Mucs = [new MucKiemTra { ThuTu = 1 }] }
            : await db.MauKiemTras.Include(x => x.Mucs).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();
        m.Mucs = m.Mucs.OrderBy(x => x.ThuTu).ToList();
        await Nap();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> MauForm(MauKiemTra input)
    {
        var mucs = (input.Mucs ?? []).Where(x => !string.IsNullOrWhiteSpace(x.NoiDung)).ToList();
        if (string.IsNullOrWhiteSpace(input.Ten)) ModelState.AddModelError("", "Nhập tên phiếu kiểm tra.");
        if (mucs.Count == 0) ModelState.AddModelError("", "Phiếu phải có ít nhất một mục kiểm tra.");
        if (mucs.Any(x => x.Kieu == KieuMucKiemTra.So && x.NguongMin > x.NguongMax)) ModelState.AddModelError("", "Ngưỡng nhỏ nhất lớn hơn ngưỡng lớn nhất.");
        if (input.TaiSanId is not null) input.LoaiThietBiId = null;
        foreach (var k in ModelState.Keys.Where(k => k.StartsWith("Mucs[") || k.Contains("MauKiemTra")).ToList()) ModelState.Remove(k);
        if (!ModelState.IsValid) { input.Mucs = mucs; await Nap(); return View(input); }

        MauKiemTra m;
        if (input.Id == 0) { m = new MauKiemTra(); db.MauKiemTras.Add(m); }
        else m = await db.MauKiemTras.Include(x => x.Mucs).FirstAsync(x => x.Id == input.Id);
        m.Ten = input.Ten.Trim(); m.MoTa = input.MoTa; m.LoaiThietBiId = input.LoaiThietBiId; m.TaiSanId = input.TaiSanId;
        m.KhoaPhongId = input.KhoaPhongId; m.ViTri = input.ViTri; m.ChuKyGio = input.ChuKyGio; m.TuTaoPhieu = input.TuTaoPhieu; m.DangApDung = input.DangApDung; m.LaThuNguonDuPhong = input.LaThuNguonDuPhong;
        // Đồng bộ mục: cập nhật mục cũ theo Id, thêm mục mới, xoá mục bị bỏ (kết quả cũ vẫn giữ nội dung mục đã lưu)
        var giu = mucs.Where(x => x.Id != 0).Select(x => x.Id).ToHashSet();
        foreach (var cu in m.Mucs.Where(x => !giu.Contains(x.Id)).ToList()) m.Mucs.Remove(cu);
        var tt = 1;
        foreach (var x in mucs)
        {
            var muc = x.Id != 0 ? m.Mucs.FirstOrDefault(y => y.Id == x.Id) : null;
            if (muc is null) { muc = new MucKiemTra(); m.Mucs.Add(muc); }
            muc.ThuTu = tt++; muc.NoiDung = x.NoiDung.Trim(); muc.Kieu = x.Kieu; muc.DonVi = x.DonVi; muc.BatBuoc = x.BatBuoc;
            muc.NguongMin = x.Kieu == KieuMucKiemTra.So ? x.NguongMin : null; muc.NguongMax = x.Kieu == KieuMucKiemTra.So ? x.NguongMax : null;
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu phiếu kiểm tra '{m.Ten}'.";
        return RedirectToAction(nameof(Mau));
    }

    // ===== Ghi kiểm tra =====
    public async Task<IActionResult> Ghi(int mauId, int? taiSanId)
    {
        var m = await db.MauKiemTras.Include(x => x.Mucs).Include(x => x.KhoaPhong).AsNoTracking().FirstOrDefaultAsync(x => x.Id == mauId);
        if (m is null) return NotFound();
        if (!DuocGhi(m)) return Forbid();
        m.Mucs = m.Mucs.OrderBy(x => x.ThuTu).ToList();
        if (!m.KhongGanTaiSan)
            ViewBag.DsTaiSan = new SelectList((await svc.DoiTuongAsync(m)).Select(t => new { t!.Id, Ten = $"{t.MaTaiSan} – {t.Ten} ({t.KhoaPhong?.Ten ?? "Kho"})" }), "Id", "Ten", taiSanId);
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Ghi(int mauId, int? taiSanId, Dictionary<int, string?> giaTri, string? ghiChu, List<IFormFile>? anh)
    {
        var m = await db.MauKiemTras.AsNoTracking().FirstOrDefaultAsync(x => x.Id == mauId);
        if (m is null) return NotFound();
        if (!DuocGhi(m)) return Forbid();
        try
        {
            var lan = await svc.GhiAsync(mauId, taiSanId, giaTri, User.Identity!.Name!, ghiChu);
            foreach (var f in (anh ?? []).Take(10))
            {
                await using var s = f.OpenReadStream();
                await tep.LuuAsync("LanKiemTra", lan.Id.ToString(), f.FileName, s, f.Length, User.Identity.Name);
            }
            await db.SaveChangesAsync();
            TempData[lan.CoBatThuong ? "err" : "ok"] = lan.CoBatThuong
                ? $"Đã ghi kiểm tra — CÓ BẤT THƯỜNG: {lan.TomTatBatThuong()}." + (lan.PhieuSuaChuaId is not null ? " Đã tạo/gắn phiếu sửa chữa và báo Phòng CNTT." : " Đã báo Phòng CNTT.")
                : "Đã ghi kiểm tra — tất cả đạt.";
            return RedirectToAction(nameof(Lan), new { id = lan.Id });
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(Ghi), new { mauId, taiSanId }); }
    }

    public async Task<IActionResult> Lan(long id)
    {
        var lan = await db.LanKiemTras.AsNoTracking().Include(l => l.MauKiemTra).ThenInclude(m => m!.KhoaPhong).Include(l => l.TaiSan)
            .Include(l => l.PhieuSuaChua).Include(l => l.KetQuas).FirstOrDefaultAsync(l => l.Id == id);
        if (lan is null || (KhoaLoc is { } k && lan.MauKiemTra!.KhoaPhongId != k)) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("LanKiemTra", id.ToString());
        return View(lan);
    }

    /// <summary>Lịch sử kiểm tra (lọc theo phiếu mẫu / tài sản) — dùng xem xu hướng số đo.</summary>
    public async Task<IActionResult> LichSu(int? mauId, int? taiSanId)
    {
        var q = db.LanKiemTras.AsNoTracking().Include(l => l.MauKiemTra).Include(l => l.TaiSan).Include(l => l.KetQuas).AsQueryable();
        if (KhoaLoc is { } k) q = q.Where(l => l.MauKiemTra!.KhoaPhongId == k);
        if (mauId is not null) q = q.Where(l => l.MauKiemTraId == mauId);
        if (taiSanId is not null) q = q.Where(l => l.TaiSanId == taiSanId);
        ViewBag.DsMau = new SelectList(await db.MauKiemTras.OrderBy(m => m.Ten).ToListAsync(), "Id", "Ten", mauId);
        ViewData["mauId"] = mauId; ViewData["taiSanId"] = taiSanId;
        return View(await q.OrderByDescending(l => l.Id).Take(200).ToListAsync());
    }

    private async Task Nap()
    {
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(l => l.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.Where(t => t.TrangThai != Domain.Enums.TrangThaiTaiSan.DaThanhLy).OrderBy(t => t.MaTaiSan)
            .Select(t => new { t.Id, Ten = t.MaTaiSan + " – " + t.Ten }).ToListAsync(), "Id", "Ten");
    }
}
