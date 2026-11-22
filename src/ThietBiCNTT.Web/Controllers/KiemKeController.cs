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

public record QuetVm(int DotId, string Ma, int? KhoaThucTeId, TrangThaiTaiSan? TinhTrang, string? GhiChu);

/// <summary>Kiểm kê tài sản bằng quét QR.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao + "," + VaiTro.TruongKhoa)]
public class KiemKeController(AppDbContext db, SoPhieuService soPhieu) : Controller
{
    /// <summary>Trưởng khoa chỉ thấy đợt của khoa mình; CNTT/Lãnh đạo thấy tất cả.</summary>
    private IQueryable<DotKiemKe> PhamVi()
    {
        var q = db.DotKiemKes.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(d => d.KhoaPhongId == k); }
        return q;
    }

    private bool DuocQuet(DotKiemKe d) => !d.DaKhoa && (User.LaCNTT() || (User.IsInRole(VaiTro.TruongKhoa) && d.KhoaPhongId == User.KhoaPhongId()));

    public async Task<IActionResult> Index()
    {
        var ds = await PhamVi().Include(d => d.KhoaPhong).AsNoTracking().OrderByDescending(d => d.Id).ToListAsync();
        var ids = ds.Select(d => d.Id).ToList();
        ViewBag.ThongKe = (await db.ChiTietKiemKes.Where(c => ids.Contains(c.DotKiemKeId))
                .GroupBy(c => new { c.DotKiemKeId, c.KetQua }).Select(g => new { g.Key.DotKiemKeId, g.Key.KetQua, N = g.Count() }).ToListAsync())
            .GroupBy(x => x.DotKiemKeId).ToDictionary(g => g.Key, g => g.ToDictionary(x => x.KetQua, x => x.N));
        return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create()
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        return View(new DotKiemKe { Ten = $"Kiểm kê tài sản CNTT năm {DateTime.Today.Year}", NgayBatDau = DateTime.Today });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(DotKiemKe input)
    {
        if (string.IsNullOrWhiteSpace(input.Ten)) { TempData["err"] = "Nhập tên đợt kiểm kê."; return RedirectToAction(nameof(Create)); }
        if (await db.DotKiemKes.AnyAsync(d => d.TrangThai == TrangThaiKiemKe.DangKiemKe && (d.KhoaPhongId == null || input.KhoaPhongId == null || d.KhoaPhongId == input.KhoaPhongId)))
        { TempData["err"] = "Đang có đợt kiểm kê chưa khoá sổ trùng phạm vi — khoá sổ đợt đó trước."; return RedirectToAction(nameof(Index)); }
        var taiSans = await db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && (input.KhoaPhongId == null || t.KhoaPhongId == input.KhoaPhongId)).ToListAsync();
        var dot = new DotKiemKe { Ten = input.Ten.Trim(), KhoaPhongId = input.KhoaPhongId, ToKiemKe = input.ToKiemKe, GhiChu = input.GhiChu, NgayBatDau = DateTime.Now };
        dot.ChupSoSach(taiSans);
        db.DotKiemKes.Add(dot);
        await db.LuuMoiAsync(soPhieu, "KK", so => dot.Ma = so);
        TempData["ok"] = $"Đã mở đợt {dot.Ma}: {taiSans.Count} tài sản theo sổ sách. Bắt đầu quét QR.";
        return RedirectToAction(nameof(Details), new { id = dot.Id });
    }

    public async Task<IActionResult> Details(int id, KetQuaKiemKe? ketQua, int? khoa)
    {
        var dot = await PhamVi().Include(d => d.KhoaPhong).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dot is null) return NotFound();
        var q = db.ChiTietKiemKes.Where(c => c.DotKiemKeId == id);
        ViewBag.ThongKe = await q.GroupBy(c => c.KetQua).ToDictionaryAsync(g => g.Key, g => g.Count());
        if (ketQua is not null) q = q.Where(c => c.KetQua == ketQua);
        if (khoa is not null) q = q.Where(c => c.KhoaSoSachId == khoa || c.KhoaThucTeId == khoa);
        ViewBag.ChiTiet = await q.Include(c => c.TaiSan).ThenInclude(t => t!.LoaiThietBi).Include(c => c.KhoaSoSach).Include(c => c.KhoaThucTe)
            .AsNoTracking().OrderBy(c => c.KetQua == KetQuaKiemKe.Khop).ThenBy(c => c.MaQuet).Take(2000).ToListAsync();
        ViewBag.TheoKhoa = await db.ChiTietKiemKes.Where(c => c.DotKiemKeId == id)
            .GroupBy(c => c.KhoaSoSach!.Ten).Select(g => new { Khoa = g.Key, Tong = g.Count(), DaKiem = g.Count(c => c.KetQua != KetQuaKiemKe.ChuaKiem && c.KetQua != KetQuaKiemKe.Thieu) })
            .ToListAsync();
        ViewBag.DuocQuet = DuocQuet(dot);
        ViewData["ketQua"] = ketQua;
        return View(dot);
    }

    /// <summary>Màn hình quét (tối ưu cho điện thoại).</summary>
    public async Task<IActionResult> Quet(int id)
    {
        var dot = await PhamVi().Include(d => d.KhoaPhong).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dot is null) return NotFound();
        if (!DuocQuet(dot)) { TempData["err"] = dot.DaKhoa ? "Đợt đã khoá sổ." : "Bạn không có quyền quét đợt này."; return RedirectToAction(nameof(Details), new { id }); }
        var khoas = await db.KhoaPhongs.Where(k => k.DangHoatDong && (dot.KhoaPhongId == null || k.Id == dot.KhoaPhongId)).OrderBy(k => k.Ten).ToListAsync();
        ViewBag.DsKhoa = new SelectList(khoas, "Id", "Ten", dot.KhoaPhongId ?? (User.LaCNTT() ? null : User.KhoaPhongId()));
        return View(dot);
    }

    /// <summary>API ghi nhận một lần quét (gọi bằng fetch từ màn hình quét).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> QuetMa([FromForm] QuetVm vm)
    {
        var dot = await PhamVi().FirstOrDefaultAsync(d => d.Id == vm.DotId);
        if (dot is null) return NotFound();
        if (!DuocQuet(dot)) return Json(new { ok = false, loi = "Không có quyền quét hoặc đợt đã khoá sổ." });
        var ma = TachMa(vm.Ma);
        var ts = await db.TaiSans.Include(t => t.KhoaPhong).Include(t => t.LoaiThietBi).FirstOrDefaultAsync(t => t.MaTaiSan == ma);
        // Chỉ nạp dòng sổ sách liên quan (EF tự gắn vào dot.ChiTiets)
        await db.ChiTietKiemKes.Where(c => c.DotKiemKeId == dot.Id && (ts != null ? c.TaiSanId == ts.Id : c.TaiSanId == null && c.MaQuet == ma)).LoadAsync();
        try
        {
            var (ct, daKiem) = dot.GhiNhan(ma, ts, vm.KhoaThucTeId, vm.TinhTrang, User.Identity!.Name!, DateTime.Now, vm.GhiChu);
            await db.SaveChangesAsync();
            var khoaThucTe = vm.KhoaThucTeId is { } k ? (await db.KhoaPhongs.FindAsync(k))?.Ten : null;
            return Json(new
            {
                ok = true, daKiem, ma = ct.MaQuet, ten = ts?.Ten ?? "(mã không có trong hệ thống)",
                ketQua = ct.KetQua.ToString(), ketQuaTen = ct.KetQua.Ten(),
                khoaSoSach = ts?.KhoaPhong?.Ten ?? (ts is null ? "" : "Kho CNTT"), khoaThucTe,
                luc = ct.ThoiGianKiem?.ToString("HH:mm dd/MM"), nguoi = ct.NguoiKiem
            });
        }
        catch (DomainException ex) { return Json(new { ok = false, loi = ex.Message }); }
    }

    /// <summary>Lấy mã tài sản từ nội dung QR (URL …/qr/{ma}) hoặc mã nhập tay.</summary>
    public static string TachMa(string? s)
    {
        s = (s ?? "").Trim();
        var i = s.LastIndexOf("/qr/", StringComparison.OrdinalIgnoreCase);
        if (i >= 0) s = Uri.UnescapeDataString(s[(i + 4)..].Split('?', '#')[0]);
        return s.Trim().ToUpperInvariant();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> KhoaSo(int id)
    {
        var dot = await db.DotKiemKes.Include(d => d.ChiTiets).FirstOrDefaultAsync(d => d.Id == id);
        if (dot is null) return NotFound();
        try
        {
            dot.KhoaSo(DateTime.Now);
            await db.SaveChangesAsync();
            var tk = dot.ThongKe();
            TempData["ok"] = $"Đã khoá sổ {dot.Ma}: khớp {tk[KetQuaKiemKe.Khop]}, sai vị trí {tk[KetQuaKiemKe.SaiViTri]}, thừa {tk[KetQuaKiemKe.Thua]}, thiếu {tk[KetQuaKiemKe.Thieu]}.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Xuat(int id)
    {
        var dot = await PhamVi().Include(d => d.KhoaPhong).AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (dot is null) return NotFound();
        var ds = await db.ChiTietKiemKes.Where(c => c.DotKiemKeId == id).Include(c => c.TaiSan).ThenInclude(t => t!.LoaiThietBi)
            .Include(c => c.KhoaSoSach).Include(c => c.KhoaThucTe).AsNoTracking().OrderBy(c => c.KetQua).ThenBy(c => c.MaQuet).ToListAsync();
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("KiemKe");
        ws.Cell(1, 1).Value = $"KẾT QUẢ KIỂM KÊ {dot.Ma} – {dot.Ten}"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = $"Phạm vi: {dot.KhoaPhong?.Ten ?? "Toàn bệnh viện"} · Bắt đầu {dot.NgayBatDau:dd/MM/yyyy} · {(dot.DaKhoa ? $"Khoá sổ {dot.NgayKhoaSo:dd/MM/yyyy HH:mm}" : "Chưa khoá sổ")}";
        string[] hdr = ["Mã TS", "Tên tài sản", "Loại", "Serial", "Khoa theo sổ", "Khoa thực tế", "Kết quả", "Tình trạng thực tế", "Người kiểm", "Thời gian", "Ghi chú"];
        for (var i = 0; i < hdr.Length; i++) ws.Cell(4, i + 1).Value = hdr[i];
        var r = 5;
        foreach (var c in ds)
        {
            object?[] v = [c.MaQuet, c.TaiSan?.Ten, c.TaiSan?.LoaiThietBi?.Ten, c.TaiSan?.SoSerial, c.KhoaSoSach?.Ten ?? (c.TaiSan is null ? "" : "Kho CNTT"),
                c.KhoaThucTe?.Ten, c.KetQua.Ten(), c.TinhTrangThucTe?.Ten(), c.NguoiKiem, c.ThoiGianKiem, c.GhiChu];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = XLCellValue.FromObject(v[i]);
            var mau = c.KetQua switch { KetQuaKiemKe.Thieu => "#FDE2E1", KetQuaKiemKe.Thua => "#FFF1D6", KetQuaKiemKe.SaiViTri => "#FFF8E1", _ => null };
            if (mau is not null) ws.Range(r, 1, r, hdr.Length).Style.Fill.BackgroundColor = XLColor.FromHtml(mau);
            r++;
        }
        ExcelTaiSanService.DinhDangBang(ws, 4, r - 1, hdr.Length);
        ws.Column(10).Style.DateFormat.Format = "HH:mm dd/MM/yyyy";
        var tk = wb.AddWorksheet("TongHop");
        tk.Cell(1, 1).Value = "Kết quả"; tk.Cell(1, 2).Value = "Số lượng";
        var j = 2; foreach (var g in ds.GroupBy(c => c.KetQua).OrderBy(g => g.Key)) { tk.Cell(j, 1).Value = g.Key.Ten(); tk.Cell(j++, 2).Value = g.Count(); }
        ExcelTaiSanService.DinhDangBang(tk, 1, j - 1, 2);
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"KiemKe_{dot.Ma}.xlsx");
    }

    public async Task<IActionResult> InBienBan(int id)
    {
        var dot = await PhamVi().Include(d => d.KhoaPhong).Include(d => d.ChiTiets).ThenInclude(c => c.TaiSan)
            .Include(d => d.ChiTiets).ThenInclude(c => c.KhoaSoSach).Include(d => d.ChiTiets).ThenInclude(c => c.KhoaThucTe)
            .AsNoTracking().AsSplitQuery().FirstOrDefaultAsync(d => d.Id == id);
        return dot is null ? NotFound() : View(dot);
    }
}
