using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Điều chuyển tài sản — QĐ-02: duyệt nhiều cấp theo giá trị (tham số DieuChuyen.NguongDuyetLanhDao).</summary>
public class DieuChuyenController(AppDbContext db, SoPhieuService soPhieu, LichSuService lichSu, ThamSoService thamSo, ThongBaoService tb, Microsoft.AspNetCore.Identity.UserManager<AppUser> users) : Controller
{
    private IQueryable<PhieuDieuChuyen> PhamVi()
    {
        var q = db.PhieuDieuChuyens.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(p => p.TuKhoaPhongId == k || p.DenKhoaPhongId == k); }
        return q;
    }

    /// <summary>Người dùng hiện tại có quyền duyệt cấp đang chờ của phiếu không.</summary>
    private bool CoQuyenDuyet(PhieuDieuChuyen p) => CoQuyenDuyet(p, User);

    public static bool CoQuyenDuyet(PhieuDieuChuyen p, System.Security.Claims.ClaimsPrincipal u) => p.TrangThai == TrangThaiDieuChuyen.ChoDuyet && p.CapHienTai switch
    {
        PhieuDieuChuyen.CapTruongKhoa => u.IsInRole(VaiTro.QuanTri) || (u.IsInRole(VaiTro.TruongKhoa) && u.KhoaPhongId() == p.TuKhoaPhongId),
        PhieuDieuChuyen.CapCNTT => u.LaCNTT(),
        PhieuDieuChuyen.CapLanhDao => u.IsInRole(VaiTro.LanhDao) || u.IsInRole(VaiTro.QuanTri),
        _ => false
    } && !string.Equals(p.NguoiTao, u.Identity?.Name, StringComparison.OrdinalIgnoreCase);

    public async Task<IActionResult> Index(TrangThaiDieuChuyen? trangThai, bool choToiDuyet = false)
    {
        var q = PhamVi().Include(p => p.TuKhoaPhong).Include(p => p.DenKhoaPhong).Include(p => p.ChiTiets).AsNoTracking();
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        var ds = await q.OrderByDescending(p => p.Id).Take(300).ToListAsync();
        if (choToiDuyet) ds = ds.Where(CoQuyenDuyet).ToList();
        ViewData["trangThai"] = trangThai; ViewData["choToiDuyet"] = choToiDuyet;
        ViewBag.CoQuyen = ds.Where(CoQuyenDuyet).Select(p => p.Id).ToHashSet();
        return View(ds);
    }

    public async Task<IActionResult> Create(int? tuKhoa, int[]? taiSanIds)
    {
        if (!User.XemToanVien()) tuKhoa = User.KhoaPhongId();
        await Nap(tuKhoa);
        ViewData["chon"] = taiSanIds ?? [];
        return View(new PhieuDieuChuyen { TuKhoaPhongId = tuKhoa });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PhieuDieuChuyen input, int[] taiSanIds, bool guiDuyet)
    {
        if (!User.XemToanVien() && input.TuKhoaPhongId != User.KhoaPhongId()) return Forbid();
        ModelState.Remove(nameof(PhieuDieuChuyen.SoPhieu));
        var dsTs = await db.TaiSans.Where(t => taiSanIds.Contains(t.Id)).ToListAsync();
        if (dsTs.Count == 0) ModelState.AddModelError("", "Chọn ít nhất một tài sản.");
        if (dsTs.Any(t => t.KhoaPhongId != input.TuKhoaPhongId)) ModelState.AddModelError("", "Có tài sản không thuộc khoa đi.");
        if (dsTs.Any(t => t.TrangThai is TrangThaiTaiSan.DaThanhLy or TrangThaiTaiSan.ChoThanhLy)) ModelState.AddModelError("", "Có tài sản đang/đã thanh lý.");
        foreach (var loi in await KiemTraXungDot(taiSanIds, 0)) ModelState.AddModelError("", loi);
        if (!ModelState.IsValid) { await Nap(input.TuKhoaPhongId); ViewData["chon"] = taiSanIds; return View(input); }

        input.ChiTiets = dsTs.Select(t => new ChiTietDieuChuyen { TaiSanId = t.Id, TaiSan = t }).ToList();
        try { if (guiDuyet) input.GuiDuyet(thamSo.SoLon(TS.DieuChuyenNguongLanhDao)); }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); await Nap(input.TuKhoaPhongId); ViewData["chon"] = taiSanIds; return View(input); }
        db.PhieuDieuChuyens.Add(input);
        await db.LuuMoiAsync(soPhieu, "DC", so => input.SoPhieu = so);
        if (guiDuyet) { await BaoNguoiDuyetAsync(input); await db.SaveChangesAsync(); }
        TempData["ok"] = $"Đã tạo phiếu {input.SoPhieu}" + (guiDuyet ? $" và gửi duyệt ({input.CacCap().Count()} cấp)." : " (nháp).");
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Tài sản không được nằm trong phiếu điều chuyển khác đang mở, hoặc đang có phiếu sửa chữa chưa xong.</summary>
    private async Task<List<string>> KiemTraXungDot(IEnumerable<int> taiSanIds, int boQuaPhieuId)
    {
        var ids = taiSanIds.ToList();
        var loi = new List<string>();
        var dcMo = await db.ChiTietDieuChuyens.Where(c => ids.Contains(c.TaiSanId) && c.PhieuDieuChuyenId != boQuaPhieuId
                && (c.PhieuDieuChuyen!.TrangThai == TrangThaiDieuChuyen.Nhap || c.PhieuDieuChuyen.TrangThai == TrangThaiDieuChuyen.ChoDuyet || c.PhieuDieuChuyen.TrangThai == TrangThaiDieuChuyen.DaDuyet))
            .Select(c => new { c.TaiSan!.MaTaiSan, c.PhieuDieuChuyen!.SoPhieu }).ToListAsync();
        loi.AddRange(dcMo.Select(x => $"{x.MaTaiSan} đang nằm trong phiếu {x.SoPhieu}."));
        var scMo = await db.PhieuSuaChuas.Where(p => ids.Contains(p.TaiSanId) && p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu
                && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh)
            .Select(p => new { p.TaiSan!.MaTaiSan, p.SoPhieu }).ToListAsync();
        loi.AddRange(scMo.Select(x => $"{x.MaTaiSan} đang có phiếu sửa chữa {x.SoPhieu} chưa xong."));
        return loi;
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await Load(id, tracking: false);
        if (p is null) return NotFound();
        ViewBag.CoQuyenDuyet = CoQuyenDuyet(p);
        ViewBag.Nguong = thamSo.SoLon(TS.DieuChuyenNguongLanhDao);
        return View(p);
    }

    public async Task<IActionResult> InBienBan(int id)
    {
        var p = await Load(id, tracking: false);
        return p is null ? NotFound() : View(p);
    }

    private Task<PhieuDieuChuyen?> Load(int id, bool tracking)
    {
        var q = PhamVi().Include(p => p.TuKhoaPhong).Include(p => p.DenKhoaPhong).Include(p => p.DenViTri)
            .Include(p => p.BuocDuyets)
            .Include(p => p.ChiTiets).ThenInclude(c => c.TaiSan).ThenInclude(t => t!.LoaiThietBi);
        return (tracking ? q : q.AsNoTracking()).AsSplitQuery().FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<IActionResult> ThucHien(int id, Func<PhieuDieuChuyen, Task<string>> action)
    {
        var p = await Load(id, tracking: true);
        if (p is null) return NotFound();
        try { TempData["ok"] = await action(p); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDuyet(int id) => ThucHien(id, async p =>
    {
        if (!string.Equals(p.NguoiTao, User.Identity?.Name, StringComparison.OrdinalIgnoreCase) && !User.LaCNTT())
            throw new DomainException("Chỉ người lập phiếu hoặc Phòng CNTT được gửi duyệt.");
        var loi = await KiemTraXungDot(p.ChiTiets.Select(c => c.TaiSanId), p.Id);
        if (loi.Count > 0) throw new DomainException(string.Join(" ", loi));
        p.GuiDuyet(thamSo.SoLon(TS.DieuChuyenNguongLanhDao));
        await BaoNguoiDuyetAsync(p);
        return $"Đã gửi duyệt. Chờ: {PhieuDieuChuyen.TenCap(p.CapHienTai)}.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Duyet(int id, bool dongY, string? yKien) => ThucHien(id, async p =>
    {
        if (!CoQuyenDuyet(p)) throw new DomainException($"Bạn không có quyền duyệt cấp '{PhieuDieuChuyen.TenCap(p.CapHienTai)}' của phiếu này.");
        var cap = p.CapHienTai;
        p.Duyet(User.Identity!.Name!, User.HoTen(), dongY, yKien, DateTime.Now);
        if (p.TrangThai == TrangThaiDieuChuyen.ChoDuyet) await BaoNguoiDuyetAsync(p);
        else
        {
            var nhan = new List<string?> { p.NguoiTao };
            if (p.TrangThai == TrangThaiDieuChuyen.DaDuyet) nhan.AddRange(await tb.CanBoCNTTAsync());
            await tb.GuiAsync(nhan, LoaiThongBao.KetQuaDuyet, $"Phiếu điều chuyển {p.SoPhieu} {(dongY ? "đã được duyệt đủ — chờ bàn giao" : "bị từ chối")}",
                yKien, $"/DieuChuyen/Details/{p.Id}", dongY ? MucDoThongBao.ThongTin : MucDoThongBao.CanhBao, boQua: User.Identity?.Name);
        }
        return !dongY ? "Đã từ chối phiếu."
            : p.TrangThai == TrangThaiDieuChuyen.DaDuyet ? "Đã duyệt cấp cuối. Phòng CNTT có thể bàn giao."
            : $"Đã duyệt cấp '{PhieuDieuChuyen.TenCap(cap)}'. Chờ: {PhieuDieuChuyen.TenCap(p.CapHienTai)}.";
    });

    /// <summary>Báo những người có quyền duyệt cấp đang chờ.</summary>
    private async Task BaoNguoiDuyetAsync(PhieuDieuChuyen p)
    {
        var nhan = p.CapHienTai switch
        {
            PhieuDieuChuyen.CapTruongKhoa => await tb.TruongKhoaAsync(p.TuKhoaPhongId),
            PhieuDieuChuyen.CapCNTT => await tb.CanBoCNTTAsync(),
            PhieuDieuChuyen.CapLanhDao => (await users.GetUsersInRoleAsync(VaiTro.LanhDao)).Select(u => u.UserName!).ToList(),
            _ => []
        };
        await tb.GuiAsync(nhan, LoaiThongBao.ChoDuyet, $"Phiếu điều chuyển {p.SoPhieu} chờ bạn duyệt ({PhieuDieuChuyen.TenCap(p.CapHienTai)})",
            $"{p.ChiTiets.Count} tài sản · {p.LyDo}", $"/DieuChuyen/Details/{p.Id}", boQua: p.NguoiTao);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BanGiao(int id) => ThucHien(id, p =>
    {
        if (!User.LaCNTT()) throw new DomainException("Chỉ Phòng CNTT xác nhận bàn giao.");
        p.BanGiao(DateTime.Now);
        foreach (var ct in p.ChiTiets)
            lichSu.Ghi(ct.TaiSanId, LoaiSuKien.DieuChuyen, $"{p.SoPhieu}: {p.TuKhoaPhong?.Ten ?? "Kho CNTT"} → {p.DenKhoaPhong?.Ten}" + (p.NguoiNhan is null ? "" : $", người nhận {p.NguoiNhan}"), User.Identity?.Name);
        return Task.FromResult("Đã bàn giao, tài sản đã được cập nhật khoa/phòng mới.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Huy(int id)
    {
        var p = await Load(id, tracking: true);
        if (p is null) return NotFound();
        if (p.TrangThai is not (TrangThaiDieuChuyen.Nhap or TrangThaiDieuChuyen.TuChoi)) { TempData["err"] = "Chỉ huỷ được phiếu nháp hoặc bị từ chối."; return RedirectToAction(nameof(Details), new { id }); }
        if (!string.Equals(p.NguoiTao, User.Identity?.Name, StringComparison.OrdinalIgnoreCase) && !User.LaCNTT()) return Forbid();
        db.PhieuDieuChuyens.Remove(p);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã huỷ phiếu {p.SoPhieu}.";
        return RedirectToAction(nameof(Index));
    }

    private async Task Nap(int? tuKhoa)
    {
        var khoa = await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync();
        ViewBag.DsKhoa = new SelectList(khoa, "Id", "Ten");
        ViewBag.DsViTri = new SelectList(await db.ViTris.OrderBy(v => v.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = await db.TaiSans.Include(t => t.LoaiThietBi).AsNoTracking()
            .Where(t => t.KhoaPhongId == tuKhoa && t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy)
            .OrderBy(t => t.MaTaiSan).ToListAsync();
        ViewBag.Nguong = thamSo.SoLon(TS.DieuChuyenNguongLanhDao);
    }
}
