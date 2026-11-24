using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

public class DongPhieuVm { public int VatTuId { get; set; } public decimal SoLuong { get; set; } public decimal DonGia { get; set; } public string? GhiChu { get; set; } }

/// <summary>Kho vật tư: tồn theo kho, phiếu nhập/xuất/chuyển, thẻ kho, kiểm kê kho.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class KhoController(AppDbContext db, KhoService kho) : Controller
{
    public async Task<IActionResult> Index(int? khoId, bool duoiDinhMuc = false, string? q = null)
    {
        var khos = await db.Khos.Where(k => k.DangHoatDong).OrderBy(k => k.Loai).ThenBy(k => k.Ma).ToListAsync();
        var vt = db.VatTus.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) vt = vt.Where(v => v.Ma.Contains(q) || v.Ten.Contains(q) || (v.Nhom ?? "").Contains(q));
        if (duoiDinhMuc) vt = vt.Where(v => v.TonKho <= v.TonToiThieu);
        if (khoId is not null) vt = vt.Where(v => db.TonKhoVatTus.Any(t => t.VatTuId == v.Id && t.KhoId == khoId && t.SoLuong != 0));
        ViewBag.Khos = khos;
        ViewBag.Ton = (await db.TonKhoVatTus.AsNoTracking().ToListAsync()).ToDictionary(t => (t.VatTuId, t.KhoId), t => t.SoLuong);
        ViewData["khoId"] = khoId; ViewData["duoiDinhMuc"] = duoiDinhMuc; ViewData["q"] = q;
        return View(await vt.OrderBy(v => v.Ma).ToListAsync());
    }

    public async Task<IActionResult> Phieu(LoaiPhieuKho? loai, TrangThaiPhieuKho? trangThai, int page = 1)
    {
        var q = db.PhieuKhos.AsNoTracking().Include(p => p.Kho).Include(p => p.KhoDen).Include(p => p.KhoaPhong).Include(p => p.ChiTiets).AsQueryable();
        if (loai is not null) q = q.Where(p => p.Loai == loai);
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        ViewData["loai"] = loai; ViewData["trangThai"] = trangThai; ViewData["page"] = page;
        return View(await q.OrderByDescending(p => p.Id).Skip((page - 1) * 50).Take(50).ToListAsync());
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Lap(LoaiPhieuKho loai = LoaiPhieuKho.Nhap)
    {
        if (loai == LoaiPhieuKho.DieuChinh) return RedirectToAction(nameof(KiemKe));
        await Nap();
        var lyDo = Enum.GetValues<LyDoPhieuKho>().Where(l => PhieuKho.LoaiCua(l) == loai).ToList();
        ViewBag.DsLyDo = new SelectList(lyDo.Select(l => new { Ma = l, Ten = l.TenHienThi() }), "Ma", "Ten");
        return View(new PhieuKho { Loai = loai, LyDo = lyDo[0], KhoId = (await kho.KhoChinhAsync()).Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Lap(PhieuKho input, List<DongPhieuVm> dong)
    {
        var p = new PhieuKho
        {
            LyDo = input.LyDo, KhoId = input.KhoId, KhoDenId = input.KhoDenId, KhoaPhongId = input.KhoaPhongId, NhaCungCapId = input.NhaCungCapId,
            SoChungTu = input.SoChungTu, NguoiGiaoNhan = input.NguoiGiaoNhan, DienGiai = input.DienGiai,
            ChiTiets = dong.Where(d => d.VatTuId > 0).Select(d => new ChiTietPhieuKho { VatTuId = d.VatTuId, SoLuong = d.SoLuong, DonGia = d.DonGia, GhiChu = d.GhiChu }).ToList()
        };
        try
        {
            await kho.LapAsync(p, User.Identity?.Name);
            TempData["ok"] = $"Đã lập và ghi sổ phiếu {p.SoPhieu}.";
            return RedirectToAction(nameof(ChiTiet), new { id = p.Id });
        }
        catch (DomainException ex)
        {
            db.ChangeTracker.Clear();
            TempData["err"] = ex.Message;
            return RedirectToAction(nameof(Lap), new { loai = PhieuKho.LoaiCua(input.LyDo) });
        }
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var p = await db.PhieuKhos.AsNoTracking().Include(x => x.Kho).Include(x => x.KhoDen).Include(x => x.KhoaPhong).Include(x => x.NhaCungCap)
            .Include(x => x.PhieuSuaChua).Include(x => x.ChiTiets).ThenInclude(c => c.VatTu).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : View(p);
    }

    public async Task<IActionResult> InPhieu(int id)
    {
        var p = await db.PhieuKhos.AsNoTracking().Include(x => x.Kho).Include(x => x.KhoDen).Include(x => x.KhoaPhong).Include(x => x.NhaCungCap)
            .Include(x => x.ChiTiets).ThenInclude(c => c.VatTu).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : View(p);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.QuanTri)]
    public async Task<IActionResult> Duyet(int id, bool dongY)
    {
        var p = await db.PhieuKhos.Include(x => x.ChiTiets).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        try
        {
            if (dongY) { await kho.DuyetAsync(p, User.Identity!.Name!); TempData["ok"] = "Đã duyệt và ghi sổ phiếu điều chỉnh."; }
            else
            {
                if (p.TrangThai != TrangThaiPhieuKho.ChoDuyet) throw new DomainException("Phiếu không chờ duyệt.");
                p.TrangThai = TrangThaiPhieuKho.DaHuy; p.NguoiDuyet = User.Identity!.Name; await db.SaveChangesAsync(); TempData["ok"] = "Đã huỷ phiếu điều chỉnh.";
            }
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    public async Task<IActionResult> TheKho(int vatTuId, int? khoId)
    {
        var v = await db.VatTus.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vatTuId);
        if (v is null) return NotFound();
        var q = db.TheKhos.AsNoTracking().Include(t => t.Kho).Where(t => t.VatTuId == vatTuId);
        if (khoId is not null) q = q.Where(t => t.KhoId == khoId);
        ViewBag.DongThe = await q.OrderByDescending(t => t.Id).Take(500).ToListAsync();
        ViewBag.DsKho = new SelectList(await db.Khos.OrderBy(k => k.Ma).ToListAsync(), "Id", "Ten", khoId);
        ViewBag.SuDung = await db.ChiTietVatTuSuaChuas.AsNoTracking().Where(c => c.VatTuId == vatTuId).Include(c => c.PhieuSuaChua).ThenInclude(p => p!.TaiSan)
            .OrderByDescending(c => c.Id).Take(50).ToListAsync();
        ViewData["khoId"] = khoId;
        return View(v);
    }

    /// <summary>Kiểm kê kho: nhập số thực tế → phiếu điều chỉnh chờ Quản trị duyệt.</summary>
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> KiemKe(int? khoId)
    {
        var k = khoId is { } id ? await db.Khos.FindAsync(id) : await kho.KhoChinhAsync();
        if (k is null) return NotFound();
        ViewBag.DsKho = new SelectList(await db.Khos.Where(x => x.DangHoatDong).OrderBy(x => x.Ma).ToListAsync(), "Id", "Ten", k.Id);
        ViewBag.Ton = await db.TonKhoVatTus.AsNoTracking().Where(t => t.KhoId == k.Id).ToDictionaryAsync(t => t.VatTuId, t => t.SoLuong);
        ViewBag.VatTus = await db.VatTus.AsNoTracking().OrderBy(v => v.Ma).ToListAsync();
        return View(k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> KiemKe(int khoId, Dictionary<int, decimal?> thucTe, string? dienGiai)
    {
        var ton = await db.TonKhoVatTus.Where(t => t.KhoId == khoId).ToDictionaryAsync(t => t.VatTuId, t => t.SoLuong);
        var dong = thucTe.Where(x => x.Value is not null && x.Value >= 0 && x.Value != ton.GetValueOrDefault(x.Key))
            .Select(x => new ChiTietPhieuKho { VatTuId = x.Key, TonSoSach = ton.GetValueOrDefault(x.Key), SoLuong = x.Value!.Value - ton.GetValueOrDefault(x.Key) }).ToList();
        if (dong.Count == 0) { TempData["ok"] = "Số thực tế khớp sổ sách — không cần điều chỉnh."; return RedirectToAction(nameof(KiemKe), new { khoId }); }
        var p = new PhieuKho { LyDo = LyDoPhieuKho.KiemKe, KhoId = khoId, DienGiai = string.IsNullOrWhiteSpace(dienGiai) ? "Điều chỉnh theo kiểm kê kho" : dienGiai, ChiTiets = dong };
        try
        {
            await kho.LapAsync(p, User.Identity?.Name);
            TempData["ok"] = $"Đã lập phiếu điều chỉnh {p.SoPhieu} ({dong.Count} vật tư chênh lệch) — chờ Quản trị duyệt.";
            return RedirectToAction(nameof(ChiTiet), new { id = p.Id });
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(KiemKe), new { khoId }); }
    }

    private async Task Nap()
    {
        ViewBag.DsKho = new SelectList(await db.Khos.Where(k => k.DangHoatDong).OrderBy(k => k.Loai).ThenBy(k => k.Ma).ToListAsync(), "Id", "Ten");
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.VatTus = await db.VatTus.AsNoTracking().OrderBy(v => v.Ma).ToListAsync();
        ViewBag.Ton = (await db.TonKhoVatTus.AsNoTracking().ToListAsync()).ToDictionary(t => (t.VatTuId, t.KhoId), t => t.SoLuong);
    }
}

public class KhoDanhMucController(AppDbContext db) : DanhMucController<Kho>(db)
{
    protected override string TieuDe => "Kho";
    protected override string[] Cot => [nameof(Kho.Ma), nameof(Kho.Ten), nameof(Kho.Loai), nameof(Kho.ThuKho), nameof(Kho.ViTri), nameof(Kho.DangHoatDong)];
}
