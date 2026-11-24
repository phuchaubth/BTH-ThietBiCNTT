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

/// <summary>Phân hệ 4.4 – Sổ rủi ro CNTT (FMS.02.00, MOI.01.02 ME1/ME4, QPS.04.01). FR-RR-01, 02, 04, 06.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class RuiRoController(AppDbContext db, SoPhieuService soPhieu, HanhDongService hanhDong, ThamSoService thamSo, TepService tep, UserManager<AppUser> users) : Controller
{
    public async Task<IActionResult> Index(NhomRuiRo? nhom, MucRuiRo? muc, TrangThaiRuiRo? trangThai, int? heThong, bool chiMo = true, bool sau = false, int? kn = null, int? hq = null)
    {
        var q = db.RuiRos.AsNoTracking().Include(r => r.HeThong).AsQueryable();
        if (trangThai is not null) { q = q.Where(r => r.TrangThai == trangThai); chiMo = false; }
        if (chiMo) q = q.Where(r => r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy);
        if (nhom is not null) q = q.Where(r => r.Nhom == nhom);
        if (heThong is not null) q = q.Where(r => r.HeThongId == heThong);
        var tatCa = await q.ToListAsync();
        // FR-RR-06: bản đồ nhiệt 5×5 (trước hoặc sau biện pháp)
        var heat = new int[6, 6];
        foreach (var r in tatCa) { var (a, b) = sau && r.KhaNangTonDu is { } k && r.HauQuaTonDu is { } h ? (k, h) : (r.KhaNang, r.HauQua); heat[a, b]++; }
        var ds = tatCa.AsEnumerable();
        if (kn is not null && hq is not null) ds = ds.Where(r => (sau && r.KhaNangTonDu is not null ? r.KhaNangTonDu : r.KhaNang) == kn && (sau && r.HauQuaTonDu is not null ? r.HauQuaTonDu : r.HauQua) == hq);
        if (muc is not null) ds = ds.Where(r => (sau ? r.MucTonDu ?? r.Muc : r.Muc) == muc);
        ViewData["nhom"] = nhom; ViewData["muc"] = muc; ViewData["trangThai"] = trangThai; ViewData["heThong"] = heThong; ViewData["chiMo"] = chiMo; ViewData["sau"] = sau; ViewData["kn"] = kn; ViewData["hq"] = hq;
        ViewBag.Heat = heat; ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThong);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        // BR-RR-04: từ 01/11 chưa có đợt đánh giá năm nay đã chốt
        var nam = DateTime.Today.Year;
        ViewBag.CanhBaoNam = DateTime.Today.Month >= 11 && !await db.DotDanhGiaRuiRos.AnyAsync(d => d.Nam == nam && d.DaChot) ? nam : (int?)null;
        return View(ds.OrderByDescending(r => (sau ? r.DiemTonDu ?? r.Diem : r.Diem)).ThenBy(r => r.Ma).ToList());
    }

    private async Task NapAsync()
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsNguoi = await users.DsCnttAsync(User.Identity!.Name);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id, int? heThongId, int? taiSanId)
    {
        RuiRo? r;
        if (id is null) r = new RuiRo { HeThongId = heThongId, TaiSanId = taiSanId, NguoiChiuTrachNhiem = User.Identity!.Name };
        else { r = await db.RuiRos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); if (r is null) return NotFound(); }
        await NapAsync();
        return View(r);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(RuiRo input, string? lyDoDiem)
    {
        ModelState.Remove(nameof(input.Ma)); ModelState.Remove(nameof(input.HeThong)); ModelState.Remove(nameof(input.TaiSan));
        if (!ModelState.IsValid) { await NapAsync(); return View(input); }
        var now = DateTime.Now; var nguoi = User.Identity!.Name!;
        try
        {
            if (input.Id == 0)
            {
                var moi = new RuiRo { MoTa = input.MoTa.Trim(), Nhom = input.Nhom, HeThongId = input.HeThongId, TaiSanId = input.TaiSanId, KiemSoatHienCo = input.KiemSoatHienCo, NguoiChiuTrachNhiem = input.NguoiChiuTrachNhiem, NguoiTao = nguoi };
                moi.ChamDiem(input.KhaNang, input.HauQua, input.KhaNangTonDu, input.HauQuaTonDu, "", nguoi, now);
                moi.NgayXemXetLai = DateOnly.FromDateTime(now).AddMonths(moi.Muc >= MucRuiRo.Cao ? 3 : 12);
                db.RuiRos.Add(moi);
                await db.LuuMoiAsync(soPhieu, "RR", so => moi.Ma = so);
                input = moi;
            }
            else
            {
                var r = await db.RuiRos.Include(x => x.LichSus).FirstAsync(x => x.Id == input.Id);
                r.MoTa = input.MoTa.Trim(); r.Nhom = input.Nhom; r.HeThongId = input.HeThongId; r.TaiSanId = input.TaiSanId; r.KiemSoatHienCo = input.KiemSoatHienCo; r.NguoiChiuTrachNhiem = input.NguoiChiuTrachNhiem;
                if (input.KhaNang != r.KhaNang || input.HauQua != r.HauQua || input.KhaNangTonDu != r.KhaNangTonDu || input.HauQuaTonDu != r.HauQuaTonDu) r.ChamDiem(input.KhaNang, input.HauQua, input.KhaNangTonDu, input.HauQuaTonDu, lyDoDiem ?? "", nguoi, now);
                await db.SaveChangesAsync();
                input = r;
            }
        }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); await NapAsync(); return View(input); }
        TempData["ok"] = $"Đã lưu rủi ro {input.Ma}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var r = await db.RuiRos.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiSan).Include(x => x.LichSus).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.RuiRo && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.Tep = await tep.DanhSachAsync("RuiRo", id.ToString());
        await NapAsync();
        return View(r);
    }

    private async Task<IActionResult> Thuc(int id, Func<RuiRo, Task<string>> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var r = await db.RuiRos.Include(x => x.LichSus).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        try { TempData["ok"] = await thaoTac(r); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ChamLai(int id, int khaNang, int hauQua, int? khaNangTonDu, int? hauQuaTonDu, string? lyDo) => Thuc(id, r =>
    {
        r.ChamDiem(khaNang, hauQua, khaNangTonDu, hauQuaTonDu, lyDo ?? "", User.Identity!.Name!, DateTime.Now);
        return Task.FromResult($"Đã chấm lại: {r.Diem} điểm — {r.Muc.Ten()}.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhuongAn(int id, PhuongAnRuiRo phuongAn, string? lyDoChapNhan, DateOnly? ngayXemXetLai, string? nguoiChiuTrachNhiem) => Thuc(id, r =>
    {
        r.PhuongAn = phuongAn; r.LyDoChapNhan = lyDoChapNhan?.Trim(); r.NgayXemXetLai = ngayXemXetLai ?? r.NgayXemXetLai;
        if (!string.IsNullOrWhiteSpace(nguoiChiuTrachNhiem)) r.NguoiChiuTrachNhiem = nguoiChiuTrachNhiem;
        if (phuongAn != PhuongAnRuiRo.ChapNhan) { r.NguoiDuyetChapNhan = null; r.NgayDuyetChapNhan = null; }
        return Task.FromResult("Đã ghi phương án xử lý." + (r.CanLanhDaoDuyetChapNhan ? " Chấp nhận rủi ro mức Cao trở lên cần Lãnh đạo phê duyệt." : ""));
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.LanhDao)]
    public Task<IActionResult> DuyetChapNhan(int id) => Thuc(id, r => { r.DuyetChapNhan(User.Identity!.Name!, DateTime.Now); return Task.FromResult("Đã phê duyệt chấp nhận rủi ro."); }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TrangThai(int id, TrangThaiRuiRo trangThai) => Thuc(id, async r =>
    {
        var chuaXong = await db.HanhDongKhacPhucs.CountAsync(h => h.Nguon == NguonHanhDong.RuiRo && h.NguonId == id && h.TrangThai != TrangThaiHanhDong.DaDong);
        r.ChuyenTrangThai(trangThai, chuaXong);
        return $"Đã chuyển sang \"{trangThai.Ten()}\".";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoHanhDong(int id, string moTa, string nguoiChiuTrachNhiem, DateOnly? han)
    {
        if (!User.LaCNTT()) return Forbid();
        var r = await db.RuiRos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (r is null) return NotFound();
        try { var h = await hanhDong.TaoAsync(NguonHanhDong.RuiRo, id, moTa, r.HeThongId, nguoiChiuTrachNhiem, han, LoaiHanhDong.PhongNgua, r.Muc >= MucRuiRo.Cao ? MucDoUuTien.Cao : MucDoUuTien.TrungBinh, User.Identity!.Name!); TempData["ok"] = $"Đã tạo hành động {h.Ma}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
