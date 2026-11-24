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

public class DongDeXuatVm
{
    public LoaiHangMua Loai { get; set; } = LoaiHangMua.VatTu;
    public int? VatTuId { get; set; }
    public int? LoaiThietBiId { get; set; }
    public string? MoTa { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public string? GhiChu { get; set; }
}
public class DongDatVm { public int Id { get; set; } public bool Chon { get; set; } public decimal SoLuong { get; set; } public decimal DonGia { get; set; } }
public class DongNhanVm { public int Id { get; set; } public decimal SoLuong { get; set; } public string? Serial { get; set; } }

/// <summary>Mua sắm: đề xuất → duyệt → đơn hàng → nhận hàng.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class MuaSamController(AppDbContext db, MuaSamService ms) : Controller
{
    // ===== Đề xuất =====
    public async Task<IActionResult> Index(TrangThaiDeXuat? trangThai, bool choToi = false, int page = 1)
    {
        var q = db.DeXuatMuaSams.AsNoTracking().Include(x => x.Dongs).AsQueryable();
        if (trangThai is not null) q = q.Where(x => x.TrangThai == trangThai);
        var ds = await q.OrderByDescending(x => x.Id).ToListAsync();
        if (choToi)
        {
            var toi = User.Identity!.Name!;
            var loc = new List<DeXuatMuaSam>();
            foreach (var x in ds.Where(x => x.TrangThai == TrangThaiDeXuat.ChoDuyet)) if (await ms.DuocDuyetAsync(x, toi)) loc.Add(x);
            ds = loc;
        }
        ViewData["trangThai"] = trangThai; ViewData["choToi"] = choToi; ViewData["page"] = page;
        return View(ds.Skip((page - 1) * 50).Take(50).ToList());
    }

    public async Task<IActionResult> DeXuat(int id)
    {
        var dx = await db.DeXuatMuaSams.AsNoTracking().Include(x => x.Dongs).ThenInclude(d => d.VatTu).Include(x => x.Dongs).ThenInclude(d => d.LoaiThietBi)
            .Include(x => x.BuocDuyets).Include(x => x.PhieuSuaChua).FirstOrDefaultAsync(x => x.Id == id);
        if (dx is null) return NotFound();
        ViewBag.DuocDuyet = await ms.DuocDuyetAsync(dx, User.Identity?.Name);
        ViewBag.NguoiDuyet = dx.TrangThai == TrangThaiDeXuat.ChoDuyet ? await ms.NguoiDuyetCapAsync(dx.CapHienTai) : new List<string>();
        ViewBag.DonHangs = await db.DongDonHangs.AsNoTracking().Where(d => d.DongDeXuat!.DeXuatMuaSamId == id).Select(d => d.DonMuaHang!).Distinct().ToListAsync();
        ViewBag.Nguong = ms.NguongLanhDao;
        return View(dx);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LapDeXuat(int? id, int? phieuSuaChuaId)
    {
        var dx = id is null ? new DeXuatMuaSam { PhieuSuaChuaId = phieuSuaChuaId, CanCu = phieuSuaChuaId is null ? CanCuDeXuat.TrangBiMoi : CanCuDeXuat.SuaChua }
            : await db.DeXuatMuaSams.Include(x => x.Dongs).FirstOrDefaultAsync(x => x.Id == id);
        if (dx is null) return NotFound();
        if (id is not null && dx.TrangThai is not (TrangThaiDeXuat.Nhap or TrangThaiDeXuat.TuChoi)) { TempData["err"] = "Chỉ sửa đề xuất nháp hoặc bị từ chối."; return RedirectToAction(nameof(DeXuat), new { id }); }
        await Nap();
        var dangMua = (Dictionary<int, decimal>)ViewBag.DangMua; // không tính chính đề xuất đang sửa
        foreach (var d in dx.Dongs.Where(d => d.VatTuId is not null && dx.TrangThai == TrangThaiDeXuat.Nhap))
            dangMua[d.VatTuId!.Value] = Math.Max(0, dangMua.GetValueOrDefault(d.VatTuId.Value) - d.ConLai);
        return View(dx);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LapDeXuat(DeXuatMuaSam input, List<DongDeXuatVm> dong, bool guiDuyet = false)
    {
        var dongs = dong.Where(d => d.SoLuong > 0 && (d.VatTuId > 0 || d.LoaiThietBiId > 0)).Select(d => new DongDeXuat
        { Loai = d.Loai, VatTuId = d.Loai == LoaiHangMua.VatTu ? d.VatTuId : null, LoaiThietBiId = d.Loai == LoaiHangMua.ThietBi ? d.LoaiThietBiId : null,
          MoTa = d.MoTa, SoLuong = d.SoLuong, DonGiaDuKien = d.DonGia, GhiChu = d.GhiChu }).ToList();
        DeXuatMuaSam dx;
        try
        {
            if (input.Id == 0)
            {
                dx = new DeXuatMuaSam { TieuDe = input.TieuDe, CanCu = input.CanCu, LyDo = input.LyDo, PhieuSuaChuaId = input.PhieuSuaChuaId, Dongs = dongs };
                await ms.LapDeXuatAsync(dx);
            }
            else
            {
                dx = await db.DeXuatMuaSams.Include(x => x.Dongs).FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new DomainException("Không tìm thấy đề xuất.");
                if (dx.TrangThai is not (TrangThaiDeXuat.Nhap or TrangThaiDeXuat.TuChoi)) throw new DomainException("Chỉ sửa đề xuất nháp hoặc bị từ chối.");
                dx.TieuDe = input.TieuDe; dx.CanCu = input.CanCu; dx.LyDo = input.LyDo; dx.PhieuSuaChuaId = input.PhieuSuaChuaId;
                db.DongDeXuats.RemoveRange(dx.Dongs); dx.Dongs = dongs;
                dx.KiemTraDong();
                await db.SaveChangesAsync();
            }
            if (guiDuyet) await ms.GuiDuyetAsync(dx, User.Identity?.Name);
            TempData["ok"] = guiDuyet ? $"Đã gửi duyệt đề xuất {dx.SoPhieu}." : $"Đã lưu nháp {dx.SoPhieu}.";
            return RedirectToAction(nameof(DeXuat), new { id = dx.Id });
        }
        catch (DomainException ex)
        {
            db.ChangeTracker.Clear();
            TempData["err"] = ex.Message;
            return input.Id == 0 ? RedirectToAction(nameof(LapDeXuat), new { phieuSuaChuaId = input.PhieuSuaChuaId }) : RedirectToAction(nameof(LapDeXuat), new { id = input.Id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GuiDuyet(int id)
    {
        var dx = await db.DeXuatMuaSams.Include(x => x.Dongs).FirstOrDefaultAsync(x => x.Id == id);
        if (dx is null) return NotFound();
        try { await ms.GuiDuyetAsync(dx, User.Identity?.Name); TempData["ok"] = $"Đã gửi duyệt — chờ {DeXuatMuaSam.TenCap(dx.CapHienTai)}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(DeXuat), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duyet(int id, bool dongY, string? yKien)
    {
        var dx = await db.DeXuatMuaSams.Include(x => x.Dongs).Include(x => x.BuocDuyets).FirstOrDefaultAsync(x => x.Id == id);
        if (dx is null) return NotFound();
        try
        {
            await ms.DuyetAsync(dx, User.Identity!.Name!, User.HoTen(), dongY, yKien);
            TempData["ok"] = !dongY ? "Đã từ chối đề xuất." : dx.TrangThai == TrangThaiDeXuat.DaDuyet ? "Đã duyệt — đề xuất hoàn tất phê duyệt." : $"Đã duyệt — chuyển {DeXuatMuaSam.TenCap(dx.CapHienTai)}.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(DeXuat), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> HuyDeXuat(int id)
    {
        var dx = await db.DeXuatMuaSams.Include(x => x.Dongs).FirstOrDefaultAsync(x => x.Id == id);
        if (dx is null) return NotFound();
        if (dx.Dongs.Any(d => d.SoLuongDaDat > 0) || dx.TrangThai is TrangThaiDeXuat.DaHuy or TrangThaiDeXuat.DaDatDu) TempData["err"] = "Không huỷ được: đề xuất đã có đơn hàng.";
        else { dx.TrangThai = TrangThaiDeXuat.DaHuy; await db.SaveChangesAsync(); TempData["ok"] = "Đã huỷ đề xuất."; }
        return RedirectToAction(nameof(DeXuat), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> TuDong()
    {
        var dx = await ms.TuDongDeXuatAsync(User.Identity?.Name);
        if (dx is null) { TempData["ok"] = "Không có vật tư nào cần bổ sung (đã đủ hoặc đang trong quy trình mua)."; return RedirectToAction(nameof(Index)); }
        TempData["ok"] = $"Đã lập/bổ sung đề xuất nháp {dx.SoPhieu} từ vật tư dưới định mức.";
        return RedirectToAction(nameof(DeXuat), new { id = dx.Id });
    }

    // ===== Đơn hàng =====
    public async Task<IActionResult> DonHangs(TrangThaiDonHang? trangThai, bool treHan = false)
    {
        var q = db.DonMuaHangs.AsNoTracking().Include(x => x.NhaCungCap).Include(x => x.Dongs).AsQueryable();
        if (trangThai is not null) q = q.Where(x => x.TrangThai == trangThai);
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (treHan) q = q.Where(x => (x.TrangThai == TrangThaiDonHang.DaDatHang || x.TrangThai == TrangThaiDonHang.NhanMotPhan) && x.HanGiao < homNay);
        ViewData["trangThai"] = trangThai; ViewData["treHan"] = treHan;
        return View(await q.OrderByDescending(x => x.Id).Take(200).ToListAsync());
    }

    public async Task<IActionResult> DonHang(int id)
    {
        var dh = await TaiDon(id, true);
        if (dh is null) return NotFound();
        ViewBag.DuocDatHang = await ms.DuocDatHangAsync(dh, User.Identity?.Name);
        ViewBag.Nguong = ms.NguongLanhDao;
        ViewBag.DsKho = new SelectList(await db.Khos.Where(k => k.DangHoatDong).OrderBy(k => k.Loai).ThenBy(k => k.Ma).ToListAsync(), "Id", "Ten");
        return View(dh);
    }

    public async Task<IActionResult> InDon(int id)
    {
        var dh = await TaiDon(id, true);
        return dh is null ? NotFound() : View(dh);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LapDon(int? deXuatId)
    {
        var q = db.DongDeXuats.AsNoTracking().Include(d => d.DeXuatMuaSam).Include(d => d.VatTu).Include(d => d.LoaiThietBi)
            .Where(d => (d.DeXuatMuaSam!.TrangThai == TrangThaiDeXuat.DaDuyet || d.DeXuatMuaSam.TrangThai == TrangThaiDeXuat.DatMotPhan) && d.SoLuongDaDat < d.SoLuong);
        if (deXuatId is not null) q = q.Where(d => d.DeXuatMuaSamId == deXuatId);
        ViewBag.Dongs = await q.OrderBy(d => d.DeXuatMuaSamId).ThenBy(d => d.Id).ToListAsync();
        ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsHopDong = new SelectList(await db.HopDongs.OrderByDescending(h => h.Id).Select(h => new { h.Id, Ten = h.SoHopDong + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        return View(new DonMuaHang { HanGiao = DateOnly.FromDateTime(DateTime.Today.AddDays(14)) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LapDon(DonMuaHang input, List<DongDatVm> dong)
    {
        var dh = new DonMuaHang { NhaCungCapId = input.NhaCungCapId, HopDongId = input.HopDongId, CanCu = input.CanCu, HanGiao = input.HanGiao,
            ThueSuat = input.ThueSuat, BaoHanhThang = input.BaoHanhThang, GhiChu = input.GhiChu };
        try
        {
            await ms.LapDonHangAsync(dh, dong.Where(d => d.Chon).Select(d => new DongDatHang(d.Id, d.SoLuong, d.DonGia)));
            TempData["ok"] = $"Đã lập đơn hàng {dh.SoPhieu} (nháp) — chờ xác nhận đặt hàng.";
            return RedirectToAction(nameof(DonHang), new { id = dh.Id });
        }
        catch (DomainException ex) { db.ChangeTracker.Clear(); TempData["err"] = ex.Message; return RedirectToAction(nameof(LapDon)); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DatHang(int id)
    {
        var dh = await TaiDon(id, false);
        if (dh is null) return NotFound();
        try { await ms.DatHangAsync(dh, User.Identity!.Name!); TempData["ok"] = $"Đã xác nhận đặt hàng {dh.SoPhieu}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(DonHang), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> HuyDon(int id)
    {
        var dh = await TaiDon(id, false);
        if (dh is null) return NotFound();
        try { await ms.HuyDonAsync(dh); TempData["ok"] = "Đã huỷ đơn hàng — số lượng trả lại đề xuất."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(DonHang), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> NhanHang(int id, int? khoId, List<DongNhanVm> nhan, string? soHoaDon, string? bienBan)
    {
        var dh = await TaiDon(id, false);
        if (dh is null) return NotFound();
        try
        {
            var pn = await ms.NhanHangAsync(dh, khoId, nhan.Select(n => new DongNhanHang(n.Id, n.SoLuong,
                (n.Serial ?? "").Split(['\n', ',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))), soHoaDon, bienBan, User.Identity!.Name!);
            TempData["ok"] = $"Đã nhận hàng trị giá {pn.GiaTri:N0} đ." + (pn.PhieuKhoId is null ? "" : " Đã lập phiếu nhập kho.") + (pn.TaiSanDaTao is null ? "" : $" Tài sản mới: {pn.TaiSanDaTao}.");
        }
        catch (DomainException ex) { db.ChangeTracker.Clear(); TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(DonHang), new { id });
    }

    private Task<DonMuaHang?> TaiDon(int id, bool chiDoc)
    {
        var q = db.DonMuaHangs.Include(x => x.NhaCungCap).Include(x => x.HopDong).Include(x => x.Dongs).ThenInclude(d => d.VatTu)
            .Include(x => x.Dongs).ThenInclude(d => d.LoaiThietBi).Include(x => x.Dongs).ThenInclude(d => d.DongDeXuat).ThenInclude(d => d!.DeXuatMuaSam)
            .Include(x => x.PhieuNhans).ThenInclude(p => p.PhieuKho).AsSplitQuery();
        return (chiDoc ? q.AsNoTracking() : q).FirstOrDefaultAsync(x => x.Id == id);
    }

    private async Task Nap()
    {
        ViewBag.VatTus = await db.VatTus.AsNoTracking().OrderBy(v => v.Ma).ToListAsync();
        ViewBag.LoaiTBs = await db.LoaiThietBis.AsNoTracking().OrderBy(v => v.Ten).ToListAsync();
        ViewBag.DangMua = await ms.DangMuaAsync();
    }
}
