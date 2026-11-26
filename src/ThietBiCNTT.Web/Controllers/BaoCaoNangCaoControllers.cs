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

internal static class Xlsx { public const string Mime = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"; }

/// <summary>Tổng chi phí sở hữu (FR-BC-06).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class TcoController(AppDbContext db, TcoService tco, ThamSoService ts) : Controller
{
    public async Task<IActionResult> Index(NhomTco nhom = NhomTco.Loai, int? khoa = null, int? loai = null, bool thanhLy = false)
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoa);
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", loai);
        ViewData["nhom"] = nhom; ViewData["khoa"] = khoa; ViewData["loai"] = loai; ViewData["thanhLy"] = thanhLy;
        ViewBag.Nguong = ts.SoNguyen(TS.NguongChiPhiSuaPhanTram);
        return View(await tco.LapAsync(nhom, khoa, loai, thanhLy));
    }

    public async Task<IActionResult> Xuat(int? khoa, int? loai, bool thanhLy = false)
    {
        var phamVi = string.Join(" · ", new[] { khoa is null ? "Toàn viện" : (await db.KhoaPhongs.FindAsync(khoa))?.Ten, loai is null ? null : (await db.LoaiThietBis.FindAsync(loai))?.Ten }.OfType<string>());
        return File(BaoCaoExcel.Tco(await tco.LapAsync(NhomTco.TaiSan, khoa, loai, thanhLy), await tco.LapAsync(NhomTco.Loai, khoa, loai, thanhLy),
            await tco.LapAsync(NhomTco.Model, khoa, loai, thanhLy), phamVi), Xlsx.Mime, $"TCO_{DateTime.Today:yyyyMMdd}.xlsx");
    }
}

/// <summary>Báo cáo tuỳ biến (FR-BC-12): chọn trường, lọc, nhóm; lưu mẫu cá nhân / chia sẻ.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BaoCaoTuyBienController(AppDbContext db, BaoCaoTuyBienService svc) : Controller
{
    private IQueryable<BaoCaoMau> MauCuaToi() { var u = User.Identity!.Name; return db.BaoCaoMaus.Where(m => m.NguoiTao == u || m.ChiaSe); }

    public async Task<IActionResult> Index(int? mauId, [FromQuery] CauHinhBaoCao? c, bool chay = false)
    {
        BaoCaoMau? mau = null;
        if (mauId is not null)
        {
            mau = await MauCuaToi().AsNoTracking().FirstOrDefaultAsync(m => m.Id == mauId);
            if (mau is null) return NotFound();
            if (!chay) { c = BaoCaoTuyBienService.Doc(mau.CauHinh); chay = true; }
        }
        c ??= new CauHinhBaoCao();
        if (c.Truong.Count == 0 && !chay) c.Truong = c.Nguon == NguonBaoCao.TaiSan ? ["MaTaiSan", "Ten", "Loai", "Khoa", "TrangThai", "NguyenGia"] : ["SoPhieu", "MaTaiSan", "Khoa", "TrangThai", "NgayYeuCau", "TongChiPhi"];
        ViewBag.Mau = mau;
        ViewBag.Truong = await svc.TruongAsync(c.Nguon);
        ViewBag.DsMau = await MauCuaToi().AsNoTracking().OrderBy(m => m.Ten).ToListAsync();
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", c.KhoaPhongId);
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", c.LoaiThietBiId);
        if (chay)
        {
            try { ViewBag.KetQua = await svc.ChayAsync(c); }
            catch (DomainException ex) { TempData["err"] = ex.Message; }
        }
        return View(c);
    }

    public async Task<IActionResult> Xuat([FromQuery] CauHinhBaoCao c, string? ten)
    {
        try { return File(BaoCaoExcel.TuyBien(await svc.ChayAsync(c), ten ?? "Báo cáo tuỳ biến"), Xlsx.Mime, $"{BaoCaoDinhKyService.TenTep(ten ?? "BaoCao")}_{DateTime.Today:yyyyMMdd}.xlsx"); }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> LuuMau(int? mauId, string ten, bool chiaSe, [FromForm] CauHinhBaoCao c)
    {
        if (string.IsNullOrWhiteSpace(ten)) { TempData["err"] = "Nhập tên mẫu báo cáo."; return RedirectToAction(nameof(Index)); }
        var u = User.Identity!.Name;
        var m = mauId is null ? null : await db.BaoCaoMaus.FirstOrDefaultAsync(x => x.Id == mauId && x.NguoiTao == u);
        if (m is null) { m = new BaoCaoMau(); db.BaoCaoMaus.Add(m); }
        m.Ten = ten.Trim(); m.ChiaSe = chiaSe; m.Nguon = c.Nguon; m.CauHinh = BaoCaoTuyBienService.Ghi(c);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu mẫu \"{m.Ten}\".";
        return RedirectToAction(nameof(Index), new { mauId = m.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XoaMau(int id)
    {
        var u = User.Identity!.Name;
        var m = await db.BaoCaoMaus.FirstOrDefaultAsync(x => x.Id == id && (x.NguoiTao == u || User.IsInRole(VaiTro.QuanTri)));
        if (m is null) return NotFound();
        if (await db.LichGuiBaoCaos.AnyAsync(l => l.BaoCaoMauId == id)) TempData["err"] = "Mẫu đang dùng trong lịch gửi định kỳ — xoá lịch trước.";
        else { db.BaoCaoMaus.Remove(m); await db.SaveChangesAsync(); TempData["ok"] = "Đã xoá mẫu."; }
        return RedirectToAction(nameof(Index));
    }
}

/// <summary>Lịch tự gửi báo cáo qua email (FR-BC-13).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class LichGuiBaoCaoController(AppDbContext db, BaoCaoDinhKyService svc) : Controller
{
    public async Task<IActionResult> Index() => View(await db.LichGuiBaoCaos.AsNoTracking().Include(l => l.BaoCaoMau).OrderBy(l => l.Ten).ToListAsync());

    [Authorize(Roles = VaiTro.NhomDuyet + "," + VaiTro.CanBoCNTT)]
    public async Task<IActionResult> Sua(int? id)
    {
        var l = id is null ? new LichGuiBaoCao() : await db.LichGuiBaoCaos.FindAsync(id);
        if (l is null) return NotFound();
        await Nap(l); return View(l);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet + "," + VaiTro.CanBoCNTT)]
    public async Task<IActionResult> Sua(LichGuiBaoCao input)
    {
        try { input.KiemTra(); } catch (DomainException ex) { ModelState.AddModelError("", ex.Message); }
        if (input.BaoCaoMauId is { } mid && !await db.BaoCaoMaus.AnyAsync(m => m.Id == mid && (m.ChiaSe || m.NguoiTao == User.Identity!.Name))) ModelState.AddModelError("", "Mẫu báo cáo không hợp lệ.");
        if (!ModelState.IsValid) { await Nap(input); return View(input); }
        var l = input.Id == 0 ? new LichGuiBaoCao() : await db.LichGuiBaoCaos.FindAsync(input.Id);
        if (l is null) return NotFound();
        l.Ten = input.Ten; l.Loai = input.Loai; l.BaoCaoMauId = input.Loai == LoaiBaoCaoDinhKy.TuyBien ? input.BaoCaoMauId : null; l.ChuKy = input.ChuKy;
        l.NguoiNhan = string.Join(", ", input.DsNguoiNhan); l.DangBat = input.DangBat;
        if (input.Id == 0) db.LichGuiBaoCaos.Add(l);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu lịch \"{l.Ten}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet + "," + VaiTro.CanBoCNTT)]
    public async Task<IActionResult> Xoa(int id)
    {
        var l = await db.LichGuiBaoCaos.FindAsync(id);
        if (l is null) return NotFound();
        db.LichGuiBaoCaos.Remove(l); await db.SaveChangesAsync();
        TempData["ok"] = "Đã xoá lịch gửi."; return RedirectToAction(nameof(Index));
    }

    /// <summary>Gửi ngay báo cáo kỳ gần nhất (kiểm tra cấu hình người nhận / SMTP).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet + "," + VaiTro.CanBoCNTT)]
    public async Task<IActionResult> GuiNgay(int id, CancellationToken ct)
    {
        var l = await db.LichGuiBaoCaos.Include(x => x.BaoCaoMau).FirstOrDefaultAsync(x => x.Id == id);
        if (l is null) return NotFound();
        var kyDaGui = l.KyDaGui;
        var ky = LichGuiBaoCao.KyGanNhat(l.ChuKy, DateTime.Now);
        await svc.GuiAsync(l, ky, ct);
        l.KyDaGui = kyDaGui; await db.SaveChangesAsync(ct); // gửi tay không thay lịch tự động
        if (l.KetQuaCuoi?.StartsWith("Lỗi") == true) TempData["err"] = l.KetQuaCuoi; else TempData["ok"] = l.KetQuaCuoi;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Tải tệp báo cáo gửi gần nhất.</summary>
    public async Task<IActionResult> Tep(int id)
    {
        var l = await db.LichGuiBaoCaos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (l?.TepCuoi is null || !System.IO.File.Exists(l.TepCuoi)) return NotFound();
        return PhysicalFile(l.TepCuoi, Xlsx.Mime, $"{BaoCaoDinhKyService.TenTep(l.Ten)}_{l.LanGuiCuoi:yyyyMMdd}.xlsx");
    }

    private async Task Nap(LichGuiBaoCao l)
    {
        var u = User.Identity!.Name;
        ViewBag.DsMau = new SelectList(await db.BaoCaoMaus.Where(m => m.ChiaSe || m.NguoiTao == u).OrderBy(m => m.Ten).ToListAsync(), "Id", "Ten", l.BaoCaoMauId);
    }
}

/// <summary>Số liệu chuyển kế toán (IF-08): ghi tăng, ghi giảm, thực chi — Excel / CSV.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class KeToanController(KeToanService kt, ThamSoService ts) : Controller
{
    /// <summary>Kỳ mặc định: tháng trước. <paramref name="den"/> là ngày cuối (tính cả ngày đó).</summary>
    public static (DateTime Tu, DateTime Den) Ky(DateTime? tu, DateTime? den)
    {
        var dau = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var t = (tu ?? dau.AddMonths(-1)).Date; var d = (den ?? (tu is null ? dau.AddDays(-1) : t.AddMonths(1).AddDays(-1))).Date;
        if (d < t) (t, d) = (d, t);
        return (t, d.AddDays(1));
    }

    public async Task<IActionResult> Index(DateTime? tu, DateTime? den)
    {
        var (t, d) = Ky(tu, den);
        return View(await kt.LapAsync(t, d));
    }

    public async Task<IActionResult> Xuat(DateTime? tu, DateTime? den)
    {
        var (t, d) = Ky(tu, den);
        return File(BaoCaoExcel.KeToan(await kt.LapAsync(t, d), ts.Chuoi(TS.TenBenhVien)), Xlsx.Mime, $"KeToan_{t:yyyyMMdd}_{d.AddDays(-1):yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> Csv(string loai, DateTime? tu, DateTime? den)
    {
        var (t, d) = Ky(tu, den);
        var s = await kt.LapAsync(t, d);
        var (b, ten) = loai switch
        {
            "tang" => (BaoCaoExcel.Csv(BaoCaoExcel.CotTang, s.GhiTang.Select(BaoCaoExcel.Dong)), "GhiTang"),
            "giam" => (BaoCaoExcel.Csv(BaoCaoExcel.CotGiam, s.GhiGiam.Select(BaoCaoExcel.Dong)), "GhiGiam"),
            "chi" => (BaoCaoExcel.Csv(BaoCaoExcel.CotChi, s.ThucChi.Select(BaoCaoExcel.Dong)), "ThucChi"),
            _ => ((byte[]?)null, "")
        };
        return b is null ? NotFound() : File(b, "text/csv; charset=utf-8", $"{ten}_{t:yyyyMMdd}_{d.AddDays(-1):yyyyMMdd}.csv");
    }
}
