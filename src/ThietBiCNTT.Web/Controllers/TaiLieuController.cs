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

/// <summary>
/// Chính sách, chương trình, hướng dẫn có phiên bản và duyệt hai cấp (MOI.02.00–02.01): an ninh mạng (HCT.01.05),
/// ứng phó tấn công mạng, telehealth (HCT.01.02), AI (HCT.01.03), thiết bị di động (HCT.01.01), tiện ích (FMS.08.00), phạm vi dịch vụ (GLD.06.00).
/// </summary>
public class TaiLieuController(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb, TepService tep) : Controller
{
    private bool Quan => User.XemToanVien();

    public async Task<IActionResult> Index(LoaiTaiLieu? loai, TrangThaiKeHoach? trangThai)
    {
        var q = db.TaiLieuQuyDinhs.AsNoTracking().Include(t => t.HeThong).AsQueryable();
        if (!Quan) q = q.Where(t => t.TrangThai == TrangThaiKeHoach.HieuLuc);
        else if (trangThai is null) q = q.Where(t => t.TrangThai != TrangThaiKeHoach.HetHieuLuc);
        if (trangThai is not null && Quan) q = q.Where(t => t.TrangThai == trangThai);
        if (loai is not null) q = q.Where(t => t.Loai == loai);
        ViewData["loai"] = loai; ViewData["trangThai"] = trangThai; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        // Loại tài liệu bắt buộc chưa có bản hiệu lực
        var coHieuLuc = (await db.TaiLieuQuyDinhs.Where(t => t.TrangThai == TrangThaiKeHoach.HieuLuc).Select(t => t.Loai).Distinct().ToListAsync()).ToHashSet();
        ViewBag.ThieuLoai = new[] { LoaiTaiLieu.ChinhSachAnNinhMang, LoaiTaiLieu.ChuongTrinhUngPhoMang, LoaiTaiLieu.ChuongTrinhTienIch, LoaiTaiLieu.HuongDanThietBiDiDong }.Where(l => !coHieuLuc.Contains(l)).ToList();
        return View(await q.OrderBy(t => t.Loai).ThenBy(t => t.Ten).ThenByDescending(t => t.PhienBan).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var t = await db.TaiLieuQuyDinhs.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (t is null || (!Quan && t.TrangThai != TrangThaiKeHoach.HieuLuc)) return NotFound();
        ViewBag.PhienBans = await db.TaiLieuQuyDinhs.AsNoTracking().Where(x => x.Ten == t.Ten && x.Loai == t.Loai).OrderByDescending(x => x.PhienBan).ToListAsync();
        ViewBag.Tep = await tep.DanhSachAsync("TaiLieuQuyDinh", id.ToString());
        ViewBag.DuocDuyetCntt = User.LaDuyetCntt(thamSo); ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(t);
    }

    private async Task NapAsync() =>
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id, LoaiTaiLieu loai = LoaiTaiLieu.ChinhSachAnNinhMang, int? heThongId = null)
    {
        TaiLieuQuyDinh? t;
        if (id is null) t = new TaiLieuQuyDinh { Loai = loai, HeThongId = heThongId };
        else
        {
            t = await db.TaiLieuQuyDinhs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (t is null) return NotFound();
            if (!t.DuocSua) { TempData["err"] = "Chỉ sửa được tài liệu ở trạng thái Nháp. Dùng \"Tạo phiên bản mới\"."; return RedirectToAction(nameof(Details), new { id }); }
        }
        await NapAsync();
        return View(t);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(TaiLieuQuyDinh input)
    {
        ModelState.Remove(nameof(input.Ma)); ModelState.Remove(nameof(input.HeThong));
        if (!ModelState.IsValid) { await NapAsync(); return View(input); }
        if (input.Id == 0)
        {
            input.PhienBan = (await db.TaiLieuQuyDinhs.Where(x => x.Loai == input.Loai && x.Ten == input.Ten).MaxAsync(x => (int?)x.PhienBan) ?? 0) + 1;
            input.TrangThai = TrangThaiKeHoach.Nhap;
            db.TaiLieuQuyDinhs.Add(input);
            await db.LuuMoiAsync(soPhieu, "VB", so => input.Ma = so);
        }
        else
        {
            var t = await db.TaiLieuQuyDinhs.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (t is null) return NotFound();
            if (!t.DuocSua) { TempData["err"] = "Tài liệu không còn ở trạng thái Nháp."; return RedirectToAction(nameof(Details), new { id = t.Id }); }
            t.Ten = input.Ten; t.Loai = input.Loai; t.HeThongId = input.HeThongId; t.NoiDung = input.NoiDung;
            await db.SaveChangesAsync();
            input = t;
        }
        TempData["ok"] = $"Đã lưu tài liệu {input.Ma} phiên bản {input.PhienBan}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    private async Task<IActionResult> Chuyen(int id, Func<TaiLieuQuyDinh, Task<string>> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var t = await db.TaiLieuQuyDinhs.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        try { TempData["ok"] = await thaoTac(t); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDuyet(int id) => Chuyen(id, async t =>
    {
        t.GuiDuyet(User.Identity!.Name!);
        var nhan = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        await tb.GuiAsync(nhan, LoaiThongBao.ChoDuyet, $"{t.Loai.Ten()} \"{t.Ten}\" v{t.PhienBan} chờ duyệt", null, $"/TaiLieu/Details/{t.Id}", boQua: User.Identity!.Name);
        return "Đã gửi duyệt.";
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> Duyet(int id) => Chuyen(id, async t =>
    {
        var xong = t.Duyet(User.Identity!.Name!, User.LaDuyetCntt(thamSo), User.IsInRole(VaiTro.LanhDao), DateTime.Now, thamSo.SoNguyen(TS.JciRaSoatTaiLieuThang));
        if (!xong) { await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.ChoDuyet, $"{t.Loai.Ten()} \"{t.Ten}\" v{t.PhienBan} chờ Lãnh đạo duyệt", null, $"/TaiLieu/Details/{t.Id}"); return "Đã duyệt bước Trưởng phòng CNTT — chờ Lãnh đạo duyệt."; }
        foreach (var cu in await db.TaiLieuQuyDinhs.Where(x => x.Loai == t.Loai && x.Ten == t.Ten && x.Id != t.Id && x.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync()) cu.TrangThai = TrangThaiKeHoach.HetHieuLuc;
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.KetQuaDuyet, $"{t.Loai.Ten()} \"{t.Ten}\" v{t.PhienBan} đã có hiệu lực", $"Rà soát lại trước {t.NgayRaSoat:dd/MM/yyyy}.", $"/TaiLieu/Details/{t.Id}");
        return "Đã duyệt đủ cấp — tài liệu có hiệu lực.";
    }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> TuChoi(int id, string lyDo) => Chuyen(id, async t =>
    {
        if (!User.LaDuyetCntt(thamSo) && !User.IsInRole(VaiTro.LanhDao)) throw new DomainException("Bạn không có quyền duyệt tài liệu.");
        t.TuChoi(User.Identity!.Name!, lyDo);
        await tb.GuiAsync([t.NguoiGuiDuyet], LoaiThongBao.KetQuaDuyet, $"{t.Loai.Ten()} \"{t.Ten}\" v{t.PhienBan} bị từ chối", t.LyDoTuChoi, $"/TaiLieu/Details/{t.Id}", MucDoThongBao.CanhBao);
        return "Đã từ chối, tài liệu trở về Nháp.";
    }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhienBanMoi(int id)
    {
        if (!User.LaCNTT()) return Forbid();
        var t = await db.TaiLieuQuyDinhs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (await db.TaiLieuQuyDinhs.AnyAsync(x => x.Loai == t.Loai && x.Ten == t.Ten && (x.TrangThai == TrangThaiKeHoach.Nhap || x.TrangThai == TrangThaiKeHoach.ChoDuyet)))
        { TempData["err"] = "Đã có phiên bản đang soạn / chờ duyệt."; return RedirectToAction(nameof(Details), new { id }); }
        var moi = t.TaoPhienBanMoi((await db.TaiLieuQuyDinhs.Where(x => x.Loai == t.Loai && x.Ten == t.Ten).MaxAsync(x => x.PhienBan)) + 1);
        db.TaiLieuQuyDinhs.Add(moi);
        await db.LuuMoiAsync(soPhieu, "VB", so => moi.Ma = so);
        TempData["ok"] = $"Đã tạo bản nháp phiên bản {moi.PhienBan}.";
        return RedirectToAction(nameof(Form), new { id = moi.Id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DaRaSoat(int id) => Chuyen(id, t =>
    {
        t.RaSoatXong(DateOnly.FromDateTime(DateTime.Today), thamSo.SoNguyen(TS.JciRaSoatTaiLieuThang));
        return Task.FromResult($"Đã ghi nhận rà soát, hạn rà soát tiếp theo {t.NgayRaSoat:dd/MM/yyyy}.");
    });
}
