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

/// <summary>FR-AN-07: theo dõi lỗ hổng, hạn vá theo mức CVSS (HCT.01.05, MOI.01.02).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class LoHongController(AppDbContext db, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index(bool chiMo = true)
    {
        var q = db.LoHongs.AsNoTracking().Include(l => l.HeThong).AsQueryable();
        if (chiMo) q = q.Where(l => l.TrangThai == TrangThaiLoHong.Mo || l.TrangThai == TrangThaiLoHong.DangVa);
        ViewData["chiMo"] = chiMo; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View((await q.ToListAsync()).OrderBy(l => l.DangMo ? 0 : 1).ThenBy(l => l.HanVa).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var l = await db.LoHongs.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiSan).FirstOrDefaultAsync(x => x.Id == id);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return l is null ? NotFound() : View(l);
    }

    private async Task Nap()
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.AsNoTracking().OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.AsNoTracking().OrderBy(t => t.MaTaiSan).Take(500).Select(t => new { t.Id, Ten = t.MaTaiSan + " – " + t.Ten }).ToListAsync(), "Id", "Ten");
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create() { await Nap(); return View(new LoHong { NgayPhatHien = DateOnly.FromDateTime(DateTime.Today), Cvss = 5 }); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(LoHong input)
    {
        ModelState.Remove(nameof(input.HanVa));
        if (input.HeThongId is null && input.TaiSanId is null) ModelState.AddModelError("", "Chọn hệ thống hoặc tài sản bị ảnh hưởng.");
        if (!ModelState.IsValid) { await Nap(); return View(input); }
        input.HanVa = LoHong.TinhHan(input.NgayPhatHien, input.Cvss, thamSo.SoNguyen(TS.JciVaNghiemTrongNgay), thamSo.SoNguyen(TS.JciVaCaoNgay), thamSo.SoNguyen(TS.JciVaTrungBinhNgay), thamSo.SoNguyen(TS.JciVaThapNgay));
        input.NguoiPhuTrach ??= User.Identity!.Name;
        db.LoHongs.Add(input); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã ghi nhận lỗ hổng mức {input.Muc.TenHienThi()}, hạn vá {input.HanVa:dd/MM/yyyy}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>Nhập danh sách lỗ hổng từ báo cáo công cụ quét (.xlsx / .csv): Tiêu đề, CVE, CVSS, Hệ thống (mã), Tài sản (mã), Ngày phát hiện.</summary>
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Nhap() { ViewBag.DsHeThong = await db.HeThongThongTins.AsNoTracking().OrderBy(h => h.Ma).ToListAsync(); return View(); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT), RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> Nhap(IFormFile? file, int? heThongMacDinh)
    {
        if (file is null || file.Length == 0 || !(file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))) { TempData["err"] = "Chọn tệp .xlsx hoặc .csv."; return View(); }
        List<string?[]> raw;
        try { await using var st = file.OpenReadStream(); raw = await BangTinh.DocAsync(st, file.FileName); }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ClosedXML.Excel.Exceptions.ClosedXMLException) { TempData["err"] = "Không đọc được tệp: " + ex.Message; return View(); }
        if (raw.Count < 2) { TempData["err"] = "Tệp không có dòng dữ liệu."; return View(); }
        var c = BangTinh.Cot(raw[0], ["tiêu đề", "tieu de", "title", "name", "plugin name", "vulnerability"], ["cve", "mã cve", "cve id"], ["cvss", "điểm cvss", "cvss score", "score"], ["hệ thống", "he thong", "system"], ["tài sản", "tai san", "host", "asset", "mã tài sản"], ["ngày phát hiện", "ngay phat hien", "date", "first seen", "discovered"]);
        if (c[0] < 0 || c[2] < 0) { TempData["err"] = "Cần có cột \"Tiêu đề\" và \"CVSS\"."; return View(); }
        var hts = await db.HeThongThongTins.AsNoTracking().ToDictionaryAsync(h => h.Ma.ToLower(), h => h.Id);
        var tss = await db.TaiSans.AsNoTracking().ToDictionaryAsync(t => t.MaTaiSan.ToLower(), t => t.Id);
        var daCo = (await db.LoHongs.AsNoTracking().Where(l => l.TrangThai == TrangThaiLoHong.Mo || l.TrangThai == TrangThaiLoHong.DangVa).Select(l => new { l.MaCve, l.TieuDe, l.HeThongId, l.TaiSanId }).ToListAsync()).ToList();
        int them = 0, trung = 0; var loi = new List<string>(); var homNay = DateOnly.FromDateTime(DateTime.Today);
        for (var i = 1; i < raw.Count; i++)
        {
            string? L(int k) => k >= 0 && k < raw[i].Length ? raw[i][k]?.Trim() : null;
            if (raw[i].All(string.IsNullOrWhiteSpace)) continue;
            var tieuDe = L(c[0]); if (string.IsNullOrEmpty(tieuDe)) { loi.Add($"Dòng {i + 1}: thiếu tiêu đề"); continue; }
            if (!decimal.TryParse(L(c[2])?.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cvss) || cvss < 0 || cvss > 10) { loi.Add($"Dòng {i + 1}: CVSS không hợp lệ"); continue; }
            int? ht = L(c[3]) is { Length: > 0 } mh && hts.TryGetValue(mh.ToLower(), out var hid) ? hid : heThongMacDinh;
            int? ts = L(c[4]) is { Length: > 0 } mt && tss.TryGetValue(mt.ToLower(), out var tid) ? tid : null;
            if (ht is null && ts is null) { loi.Add($"Dòng {i + 1}: không xác định được hệ thống / tài sản"); continue; }
            var cve = L(c[1]);
            if (daCo.Any(x => (cve is not null ? x.MaCve == cve : x.TieuDe == tieuDe) && x.HeThongId == ht && x.TaiSanId == ts)) { trung++; continue; }
            var ngay = DateOnly.TryParse(L(c[5]), out var n) ? n : homNay;
            db.LoHongs.Add(new LoHong { TieuDe = tieuDe, MaCve = cve, Nguon = NguonLoHong.QuetLoHong, Cvss = cvss, HeThongId = ht, TaiSanId = ts, NgayPhatHien = ngay, NguoiPhuTrach = User.Identity!.Name,
                HanVa = LoHong.TinhHan(ngay, cvss, thamSo.SoNguyen(TS.JciVaNghiemTrongNgay), thamSo.SoNguyen(TS.JciVaCaoNgay), thamSo.SoNguyen(TS.JciVaTrungBinhNgay), thamSo.SoNguyen(TS.JciVaThapNgay)) });
            daCo.Add(new { MaCve = cve, TieuDe = tieuDe, HeThongId = ht, TaiSanId = ts }); them++;
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã nhập {them} lỗ hổng, bỏ qua {trung} trùng" + (loi.Count > 0 ? $", {loi.Count} dòng lỗi: " + string.Join("; ", loi.Take(5)) : "") + ".";
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> Thuc(int id, Func<LoHong, string> f)
    {
        var l = await db.LoHongs.FirstOrDefaultAsync(x => x.Id == id); if (l is null) return NotFound();
        try { TempData["ok"] = f(l); await db.SaveChangesAsync(); } catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> DangVa(int id) => Thuc(id, l => { if (l.TrangThai != TrangThaiLoHong.Mo) throw new DomainException("Chỉ chuyển Đang vá từ trạng thái Mở."); l.TrangThai = TrangThaiLoHong.DangVa; return "Đã chuyển sang Đang vá."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> DaVa(int id) => Thuc(id, l => { l.DaVaXong(User.Identity!.Name!, DateTime.Now); return "Đã ghi nhận vá xong."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> ChapNhan(int id, string lyDo) => Thuc(id, l =>
    {
        if (!User.LaDuyetCntt(thamSo)) throw new DomainException("Chỉ Trưởng phòng CNTT được chấp nhận rủi ro.");
        l.ChapNhanRuiRo(lyDo, User.Identity!.Name!, DateTime.Now); return "Đã ghi nhận chấp nhận rủi ro.";
    });
}
