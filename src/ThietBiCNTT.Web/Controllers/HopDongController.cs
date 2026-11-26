using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
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

[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class HopDongController(AppDbContext db, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index(LoaiHopDong? loai, string? tinhTrang)
    {
        var d = DateOnly.FromDateTime(DateTime.Today); var moc = d.AddDays(thamSo.SoNguyen(TS.HopDongCanhBaoNgay));
        var q = db.HopDongs.AsNoTracking().Include(h => h.NhaCungCap).Include(h => h.TaiSans).AsQueryable();
        if (loai is not null) q = q.Where(h => h.Loai == loai);
        q = tinhTrang switch
        {
            "sapHet" => q.Where(h => h.HieuLucDen >= d && h.HieuLucDen <= moc),
            "hetHan" => q.Where(h => h.HieuLucDen < d),
            "hieuLuc" => q.Where(h => h.HieuLucDen == null || h.HieuLucDen >= d),
            _ => q
        };
        ViewData["loai"] = loai; ViewData["tinhTrang"] = tinhTrang; ViewBag.SoNgay = thamSo.SoNguyen(TS.HopDongCanhBaoNgay);
        return View(await q.OrderBy(h => h.HieuLucDen == null).ThenBy(h => h.HieuLucDen).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var h = await db.HopDongs.AsNoTracking().Include(x => x.NhaCungCap).Include(x => x.TaiSans).ThenInclude(t => t.TaiSan).ThenInclude(t => t!.KhoaPhong)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        ViewBag.BanQuyen = await db.BanQuyenPhanMems.AsNoTracking().Where(b => b.HopDongId == id).ToListAsync();
        ViewBag.SoNgay = thamSo.SoNguyen(TS.HopDongCanhBaoNgay);
        return View(h);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var h = id is null ? new HopDong { NgayKy = DateOnly.FromDateTime(DateTime.Today) } : await db.HopDongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten");
        return View(h);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(HopDong input)
    {
        if (input.HieuLucTu is not null && input.HieuLucDen < input.HieuLucTu) ModelState.AddModelError("", "Ngày hết hiệu lực trước ngày bắt đầu.");
        if (!ModelState.IsValid) { ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten"); return View(input); }
        if (input.Id == 0) db.HopDongs.Add(input);
        else
        {
            var h = await db.HopDongs.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = h.NgayTao; input.NguoiTao = h.NguoiTao;
            db.Entry(h).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu hợp đồng {input.SoHopDong}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Gắn tài sản vào hợp đồng theo danh sách mã (dán từ Excel, cách nhau bởi dấu phẩy / xuống dòng).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GanTaiSan(int id, string danhSachMa, bool capNhatBaoHanh)
    {
        var h = await db.HopDongs.Include(x => x.TaiSans).FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        var mas = (danhSachMa ?? "").Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList();
        var ts = await db.TaiSans.Where(t => mas.Contains(t.MaTaiSan)).ToListAsync();
        var them = 0;
        foreach (var t in ts.Where(t => h.TaiSans.All(x => x.TaiSanId != t.Id)))
        {
            h.TaiSans.Add(new HopDongTaiSan { TaiSanId = t.Id });
            if (capNhatBaoHanh && h.HieuLucDen is { } den && (t.HanBaoHanh is null || t.HanBaoHanh < den)) t.HanBaoHanh = den;
            them++;
        }
        await db.SaveChangesAsync();
        var khongCo = mas.Except(ts.Select(t => t.MaTaiSan)).ToList();
        TempData[khongCo.Count == 0 ? "ok" : "err"] = $"Đã gắn {them} tài sản." + (khongCo.Count > 0 ? " Không tìm thấy: " + string.Join(", ", khongCo) : "");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GoTaiSan(int id, int taiSanId)
    {
        await db.HopDongTaiSans.Where(x => x.HopDongId == id && x.TaiSanId == taiSanId).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Details), new { id });
    }
}

[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BanQuyenController(AppDbContext db, ThamSoService thamSo, NhatKyService nhatKy) : Controller
{
    public async Task<IActionResult> Index()
    {
        ViewBag.SoNgay = thamSo.SoNguyen(TS.HopDongCanhBaoNgay);
        return View(await db.BanQuyenPhanMems.AsNoTracking().Include(b => b.CapPhats).Include(b => b.HopDong).OrderBy(b => b.Ten).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var b = await db.BanQuyenPhanMems.AsNoTracking().Include(x => x.HopDong).Include(x => x.CapPhats).ThenInclude(c => c.TaiSan).ThenInclude(t => t!.KhoaPhong)
            .FirstOrDefaultAsync(x => x.Id == id);
        return b is null ? NotFound() : View(b);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var b = id is null ? new BanQuyenPhanMem() : await db.BanQuyenPhanMems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        // Không đưa khoá kích hoạt ra form: để trống = giữ nguyên (xem khoá qua nút "Hiện khoá" có ghi nhật ký)
        ViewBag.CoKhoa = !string.IsNullOrEmpty(b.KhoaKichHoat); b.KhoaKichHoat = null;
        ViewBag.DsHopDong = new SelectList(await db.HopDongs.OrderByDescending(h => h.Id).Select(h => new { h.Id, Ten = h.SoHopDong + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(BanQuyenPhanMem input, bool xoaKhoa = false)
    {
        if (!ModelState.IsValid) return await Form(input.Id == 0 ? null : input.Id);
        if (input.Id == 0) db.BanQuyenPhanMems.Add(input);
        else
        {
            var b = await db.BanQuyenPhanMems.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = b.NgayTao; input.NguoiTao = b.NguoiTao;
            if (string.IsNullOrWhiteSpace(input.KhoaKichHoat) && !xoaKhoa) input.KhoaKichHoat = b.KhoaKichHoat;
            db.Entry(b).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu bản quyền {input.Ten}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Xem khoá kích hoạt — thông tin nhạy cảm, mỗi lần xem ghi nhật ký (FR-HT-07).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XemKhoa(int id)
    {
        var b = await db.BanQuyenPhanMems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b?.KhoaKichHoat is null) return NotFound();
        await nhatKy.GhiAsync(ThaoTac.Xem, nameof(BanQuyenPhanMem), id.ToString(), $"Xem khoá kích hoạt: {b.Ten}");
        return Json(new { khoa = b.KhoaKichHoat });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> CapPhat(int id, string danhSachMa)
    {
        var b = await db.BanQuyenPhanMems.Include(x => x.CapPhats).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        var mas = (danhSachMa ?? "").Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList();
        var loi = new List<string>(); var ok = 0;
        foreach (var ma in mas)
        {
            var ts = await db.TaiSans.FirstOrDefaultAsync(t => t.MaTaiSan == ma);
            if (ts is null) { loi.Add($"{ma}: không tồn tại"); continue; }
            try { b.CapPhat(ts, User.Identity!.Name!, DateTime.Now); ok++; }
            catch (DomainException ex) { loi.Add(ex.Message); }
        }
        await db.SaveChangesAsync();
        TempData[loi.Count == 0 ? "ok" : "err"] = $"Đã cấp {ok} máy ({b.DaDung}/{b.SoLuong})." + (loi.Count > 0 ? " " + string.Join(" ", loi) : "");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThuHoi(int id, int capPhatId)
    {
        await db.CapPhatBanQuyens.Where(c => c.Id == capPhatId && c.BanQuyenPhanMemId == id).ExecuteDeleteAsync();
        TempData["ok"] = "Đã thu hồi giấy phép.";
        return RedirectToAction(nameof(Details), new { id });
    }
}

[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BaoCaoController(AppDbContext db, BaoCaoService svc) : Controller
{
    private static (DateTime Tu, DateTime Den) Ky(DateTime? tu, DateTime? den)
    {
        var d = den?.Date ?? DateTime.Today;
        return (tu?.Date ?? new DateTime(d.Year, d.Month, 1).AddMonths(-11), d);
    }

    public async Task<IActionResult> Index(DateTime? tu, DateTime? den, int? khoa)
    {
        var (t, d) = Ky(tu, den);
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoa);
        return View(await svc.LapAsync(t, d, khoa));
    }

    public async Task<IActionResult> Xuat(DateTime? tu, DateTime? den, int? khoa)
    {
        var (t, d) = Ky(tu, den);
        var r = await svc.LapAsync(t, d, khoa);
        return File(BaoCaoExcel.Kpi(r), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_{r.Tu:yyyyMMdd}_{r.Den:yyyyMMdd}.xlsx");
    }
}
