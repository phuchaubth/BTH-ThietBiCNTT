using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

/// <summary>Mục 5 – Hành động khắc phục dùng chung (CAPA). FR-HD-01..05.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class HanhDongController(AppDbContext db, HanhDongService svc, ThamSoService thamSo, TepService tep, UserManager<AppUser> users) : Controller
{
    private IQueryable<HanhDongKhacPhuc> Loc(TrangThaiHanhDong? trangThai, bool quaHan, NguonHanhDong? nguon, string? nguoi, int? heThong, bool conMo)
    {
        var q = db.HanhDongKhacPhucs.AsNoTracking().Include(h => h.HeThong).AsQueryable();
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        if (trangThai is not null) q = q.Where(h => h.TrangThai == trangThai);
        if (conMo) q = q.Where(h => h.TrangThai != TrangThaiHanhDong.DaDong);
        if (quaHan) q = q.Where(h => (h.TrangThai == TrangThaiHanhDong.Mo || h.TrangThai == TrangThaiHanhDong.DangLam) && h.Han < homNay);
        if (nguon is not null) q = q.Where(h => h.Nguon == nguon);
        if (!string.IsNullOrWhiteSpace(nguoi)) q = q.Where(h => h.NguoiChiuTrachNhiem == nguoi);
        if (heThong is not null) q = q.Where(h => h.HeThongId == heThong);
        return q;
    }

    public async Task<IActionResult> Index(TrangThaiHanhDong? trangThai, bool quaHan = false, NguonHanhDong? nguon = null, string? nguoi = null, int? heThong = null, bool conMo = true)
    {
        if (trangThai is not null) conMo = false;
        var ds = (await Loc(trangThai, quaHan, nguon, nguoi, heThong, conMo).ToListAsync()).OrderBy(h => h.TrangThai == TrangThaiHanhDong.DaDong).ThenBy(h => h.Han).Take(500).ToList();
        ViewData["trangThai"] = trangThai; ViewData["quaHan"] = quaHan; ViewData["nguon"] = nguon; ViewData["nguoi"] = nguoi; ViewData["heThong"] = heThong; ViewData["conMo"] = conMo;
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.DsNguoi = await users.DsCnttAsync(nguoi);
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThong);
        ViewBag.HoTen = await users.Users.ToDictionaryAsync(u => u.UserName!, u => u.HoTen);
        return View(ds);
    }

    /// <summary>FR-HD-05: xuất Excel theo bộ lọc hiện tại.</summary>
    public async Task<IActionResult> Xuat(TrangThaiHanhDong? trangThai, bool quaHan = false, NguonHanhDong? nguon = null, string? nguoi = null, int? heThong = null, bool conMo = true)
    {
        var ds = (await Loc(trangThai, quaHan, nguon, nguoi, heThong, conMo && trangThai is null).ToListAsync()).OrderBy(h => h.Han).ToList();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("HanhDongKhacPhuc");
        string[] cot = ["Mã", "Nguồn", "Nội dung", "Loại", "Hệ thống", "Người chịu trách nhiệm", "Hạn", "Trạng thái", "Hoàn thành", "Kiểm chứng bởi", "Bằng chứng hiệu quả"];
        for (var i = 0; i < cot.Length; i++) ws.Cell(1, i + 1).Value = cot[i];
        var r = 2;
        foreach (var h in ds)
        {
            object?[] v = [h.Ma, h.Nguon.Ten(), h.MoTa, h.Loai.Ten(), h.HeThong?.Ma, h.NguoiChiuTrachNhiem, h.Han.ToString("dd/MM/yyyy"), h.TrangThai.Ten(), h.NgayHoanThanh?.ToString("dd/MM/yyyy"), h.NguoiKiemChung, h.BangChungHieuQua];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = XLCellValue.FromObject(v[i]);
            r++;
        }
        ExcelTaiSanService.DinhDangBang(ws, 1, r - 1, cot.Length);
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"HanhDongKhacPhuc_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(NguonHanhDong nguon = NguonHanhDong.TrucTiep, int? nguonId = null, int? heThongId = null, string? moTa = null)
    {
        ViewBag.DsNguoi = await users.DsCnttAsync(User.Identity!.Name);
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThongId);
        ViewBag.HanMacDinh = DateOnly.FromDateTime(DateTime.Today).AddDays(thamSo.SoNguyen(TS.JciHanhDongMacDinhNgay));
        ViewBag.Nguon = nguon; ViewBag.NguonId = nguonId; ViewBag.LinkNguon = HanhDongService.LinkNguon(nguon, nguonId);
        ViewBag.MoTa = moTa;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(NguonHanhDong nguon, int? nguonId, string moTa, int? heThongId, string nguoiChiuTrachNhiem, DateOnly? han, LoaiHanhDong loai, MucDoUuTien uuTien)
    {
        try
        {
            var h = await svc.TaoAsync(nguon, nguonId, moTa, heThongId, nguoiChiuTrachNhiem, han, loai, uuTien, User.Identity!.Name!);
            TempData["ok"] = $"Đã tạo hành động {h.Ma}.";
            return RedirectToAction(nameof(Details), new { id = h.Id });
        }
        catch (DomainException ex)
        {
            TempData["err"] = ex.Message;
            return RedirectToAction(nameof(Create), new { nguon, nguonId, heThongId, moTa });
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await db.HanhDongKhacPhucs.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("HanhDongKhacPhuc", id.ToString());
        ViewBag.LinkNguon = HanhDongService.LinkNguon(h.Nguon, h.NguonId);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.HoTen = await users.Users.ToDictionaryAsync(u => u.UserName!, u => u.HoTen);
        ViewBag.DuocDuyet = User.LaDuyetCntt(thamSo) || User.IsInRole(VaiTro.LanhDao);
        return View(h);
    }

    private async Task<IActionResult> ThucHien(int id, Func<HanhDongKhacPhuc, string> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var h = await db.HanhDongKhacPhucs.FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        try { TempData["ok"] = thaoTac(h); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool LaNguoiThucHien(HanhDongKhacPhuc h) => string.Equals(h.NguoiChiuTrachNhiem, User.Identity!.Name, StringComparison.OrdinalIgnoreCase) || User.IsInRole(VaiTro.QuanTri);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BatDau(int id) => ThucHien(id, h => { if (!LaNguoiThucHien(h)) throw new DomainException("Chỉ người chịu trách nhiệm bắt đầu hành động."); h.BatDau(User.Identity!.Name!); return "Đã chuyển sang Đang làm."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BaoCaoXong(int id, string ketQua) => ThucHien(id, h => { if (!LaNguoiThucHien(h)) throw new DomainException("Chỉ người chịu trách nhiệm báo hoàn thành."); h.BaoCaoXong(ketQua, DateTime.Now); return "Đã báo hoàn thành — chờ người khác kiểm chứng hiệu quả."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> KiemChung(int id, bool hieuQua, string bangChung) => ThucHien(id, h =>
    {
        h.KiemChung(User.Identity!.Name!, hieuQua, bangChung, DateTime.Now);
        return hieuQua ? "Đã kiểm chứng hiệu quả — đóng hành động." : "Kiểm chứng chưa hiệu quả — hành động được mở lại.";
    }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> GiaHan(int id, DateOnly hanMoi, string lyDo) => ThucHien(id, h =>
    {
        if (!User.LaDuyetCntt(thamSo) && !User.IsInRole(VaiTro.LanhDao)) throw new DomainException("Chỉ người duyệt (Trưởng phòng CNTT / Lãnh đạo) được gia hạn.");
        h.GiaHan(hanMoi, lyDo, User.Identity!.Name!, DateTime.Now);
        return $"Đã gia hạn đến {hanMoi:dd/MM/yyyy}.";
    }, chiCntt: false);
}
