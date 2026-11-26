using System.Text.Json;
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

/// <summary>Phân hệ 4.7 – Rà soát quyền truy cập (FR-TC-01..05; MOI.01.01).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao + "," + VaiTro.TruongKhoa)]
public class RaSoatQuyenController(AppDbContext db, RaSoatQuyenService svc, ThamSoService thamSo, ThongBaoService tb) : Controller
{
    private bool CNTT => User.LaCNTT();
    private static readonly string ThuMuc = Path.Combine(Path.GetTempPath(), "thietbi-nhap-quyen");

    public async Task<IActionResult> Index(bool chiMo = true)
    {
        if (!(CNTT || User.IsInRole(VaiTro.LanhDao))) return RedirectToAction(nameof(Cho));
        var q = db.DotRaSoatQuyens.AsNoTracking().Include(d => d.HeThong).Include(d => d.TaiKhoans).AsQueryable();
        if (chiMo) q = q.Where(d => d.TrangThai != TrangThaiRaSoat.DaDong);
        ViewData["chiMo"] = chiMo; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        // FR-TC-01: hệ thống Tối quan trọng / Cao chưa có đợt rà soát đã đóng trong chu kỳ
        var tqt = thamSo.SoNguyen(TS.JciRaSoatQuyenToiQuanTrongThang); var cao = thamSo.SoNguyen(TS.JciRaSoatQuyenCaoThang);
        var cuoi = await db.DotRaSoatQuyens.Where(d => d.TrangThai == TrangThaiRaSoat.DaDong).GroupBy(d => d.HeThongId).Select(g => new { g.Key, Ngay = g.Max(x => x.NgayDong) }).ToDictionaryAsync(x => x.Key, x => x.Ngay);
        var hts = await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao)).OrderBy(h => h.MucTrongYeu).ToListAsync();
        ViewBag.ChuaRaSoat = hts.Where(h => !cuoi.TryGetValue(h.Id, out var n) || n is null || n.Value.AddMonths(DotRaSoatQuyen.ChuKyThang(h.MucTrongYeu, tqt, cao)) < DateTime.Now).ToList();
        ViewBag.DsHeThong = new SelectList(hts.Concat(await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && h.MucTrongYeu != MucTrongYeu.ToiQuanTrong && h.MucTrongYeu != MucTrongYeu.Cao).ToListAsync()).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }), "Id", "Ten");
        return View(await q.OrderBy(d => d.TrangThai).ThenBy(d => d.Han).ToListAsync());
    }

    /// <summary>Trưởng khoa: đợt đang chờ xác nhận có tài khoản của khoa mình.</summary>
    public async Task<IActionResult> Cho()
    {
        var k = User.KhoaPhongId();
        var ds = await db.DotRaSoatQuyens.AsNoTracking().Include(d => d.HeThong).Include(d => d.TaiKhoans)
            .Where(d => d.TrangThai == TrangThaiRaSoat.DangXacNhan && d.TaiKhoans.Any(t => t.KhoaPhongId == k)).ToListAsync();
        ViewBag.Khoa = k;
        return View(ds);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Tao(int heThongId, string? lyDo)
    {
        try { var d = await svc.TaoDotAsync(heThongId, string.IsNullOrWhiteSpace(lyDo) ? "Theo yêu cầu" : lyDo.Trim(), User.Identity!.Name!); TempData["ok"] = $"Đã mở đợt rà soát {d.Ma}."; return RedirectToAction(nameof(Details), new { id = d.Id }); }
        catch (DomainException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(Index)); }
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await db.DotRaSoatQuyens.Include(x => x.HeThong).Include(x => x.TaiKhoans).ThenInclude(t => t.KhoaPhong).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        var xemAll = CNTT || User.IsInRole(VaiTro.LanhDao);
        var k = User.KhoaPhongId();
        if (!xemAll && !d.TaiKhoans.Any(t => t.KhoaPhongId == k)) return Forbid();
        ViewBag.XemAll = xemAll; ViewBag.Khoa = k; ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(d);
    }

    // ---- FR-TC-02: nhập danh sách ----
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Nhap(int id) { var d = await db.DotRaSoatQuyens.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id); return d is null ? NotFound() : View(d); }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT), RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Nhap(int id, IFormFile? file)
    {
        var d = await db.DotRaSoatQuyens.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        if (file is null || file.Length == 0 || !(file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) || file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))) { TempData["err"] = "Chọn tệp .xlsx hoặc .csv theo mẫu."; return View(d); }
        Directory.CreateDirectory(ThuMuc);
        foreach (var f in new DirectoryInfo(ThuMuc).GetFiles().Where(f => f.CreationTime < DateTime.Now.AddDays(-1))) f.Delete();
        var ma = $"{Guid.NewGuid():N}_{User.Identity!.Name}"; var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var duongDan = Path.Combine(ThuMuc, ma + ext);
        await using (var fs = System.IO.File.Create(duongDan)) await file.CopyToAsync(fs);
        try
        {
            await using var doc = System.IO.File.OpenRead(duongDan);
            ViewBag.Dong = await svc.DocAsync(doc, file.FileName); ViewBag.MaFile = ma + ext; ViewBag.TenFile = file.FileName;
            return View("NhapXemTruoc", d);
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException or ClosedXML.Excel.Exceptions.ClosedXMLException or InvalidDataException or IOException)
        {
            System.IO.File.Delete(duongDan); TempData["err"] = "Không đọc được tệp: " + ex.Message; return View(d);
        }
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XacNhanNhap(int id, string maFile)
    {
        if (string.IsNullOrEmpty(maFile) || !Path.GetFileNameWithoutExtension(maFile).EndsWith("_" + User.Identity!.Name) || maFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest();
        var duongDan = Path.Combine(ThuMuc, maFile);
        if (!System.IO.File.Exists(duongDan)) { TempData["err"] = "Phiên nhập đã hết hạn — tải tệp lên lại."; return RedirectToAction(nameof(Nhap), new { id }); }
        var d = await db.DotRaSoatQuyens.Include(x => x.HeThong).Include(x => x.TaiKhoans).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        try
        {
            List<DongTaiKhoan> dong; await using (var doc = System.IO.File.OpenRead(duongDan)) dong = await svc.DocAsync(doc, maFile);
            var hop = dong.Where(x => x.Loi.All(l => !l.StartsWith("thiếu") && !l.StartsWith("trùng"))).ToList();
            d.NhapDanhSach(hop.Select(x => new TaiKhoanRaSoat { TenDangNhap = x.TenDangNhap, HoTen = x.HoTen, KhoaText = x.Khoa, KhoaPhongId = x.KhoaPhongId, NhomQuyen = x.NhomQuyen, LanDangNhapCuoi = x.LanDangNhapCuoi, TrangThaiTk = x.TrangThai }), User.Identity!.Name!, DateTime.Now);
            await db.SaveChangesAsync(); System.IO.File.Delete(duongDan);
            // Thông báo Trưởng khoa các khoa có tài khoản
            foreach (var kid in d.TaiKhoans.Where(t => t.KhoaPhongId != null).Select(t => t.KhoaPhongId!.Value).Distinct())
                await tb.GuiAsync(await tb.TruongKhoaAsync(kid), LoaiThongBao.RaSoatQuyen, $"Xác nhận quyền truy cập {d.HeThong?.Ma} của khoa ({d.Ma})", $"Hạn {d.Han:dd/MM/yyyy}: chọn Giữ / Thu hồi / Đổi quyền cho từng tài khoản.", $"/RaSoatQuyen/Details/{d.Id}", MucDoThongBao.CanhBao);
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã nhập {d.TaiKhoans.Count} tài khoản, bỏ qua {dong.Count - hop.Count} dòng lỗi. Trưởng khoa các khoa đã được thông báo.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- FR-TC-03: xác nhận ----
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> XacNhan(int id, int[] taiKhoanId, string[] quyetDinh, string?[] quyenMoi, string?[] ghiChu)
    {
        var d = await db.DotRaSoatQuyens.Include(x => x.TaiKhoans).FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        var k = User.KhoaPhongId(); var daLuu = 0;
        try
        {
            for (var i = 0; i < taiKhoanId.Length; i++)
            {
                var t = d.TaiKhoans.FirstOrDefault(x => x.Id == taiKhoanId[i]); if (t is null) continue;
                var duoc = User.IsInRole(VaiTro.QuanTri) || (t.KhoaPhongId is null ? CNTT : (User.IsInRole(VaiTro.TruongKhoa) && t.KhoaPhongId == k));
                if (!duoc || !Enum.TryParse<QuyetDinhTaiKhoan>(quyetDinh[i], out var qd) || qd == QuyetDinhTaiKhoan.ChuaXacNhan) continue;
                if (t.QuyetDinh == qd && t.QuyenMoi == quyenMoi[i]?.Trim() && t.GhiChu == ghiChu[i]?.Trim()) continue;
                t.DotRaSoatQuyen = d; t.XacNhan(qd, quyenMoi[i], ghiChu[i], User.Identity!.Name!, DateTime.Now); daLuu++;
            }
            await db.SaveChangesAsync(); TempData["ok"] = $"Đã lưu xác nhận {daLuu} tài khoản.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ChuyenThucHien(int id)
    {
        var d = await db.DotRaSoatQuyens.Include(x => x.HeThong).Include(x => x.TaiKhoans).FirstOrDefaultAsync(x => x.Id == id); if (d is null) return NotFound();
        try { d.ChuyenThucHien(); await db.SaveChangesAsync(); TempData["ok"] = $"Đã chuyển sang bước thực hiện: {d.SoCanThucHien} yêu cầu thu hồi / đổi quyền."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> DaThucHien(int id, int taiKhoanId)
    {
        var d = await db.DotRaSoatQuyens.Include(x => x.TaiKhoans).FirstOrDefaultAsync(x => x.Id == id); if (d is null) return NotFound();
        try { var t = d.TaiKhoans.First(x => x.Id == taiKhoanId); t.DotRaSoatQuyen = d; t.DaThucHienTrenHeThong(User.Identity!.Name!, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = $"Đã ghi nhận thực hiện tài khoản {t.TenDangNhap}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Dong(int id)
    {
        var d = await db.DotRaSoatQuyens.Include(x => x.HeThong).Include(x => x.TaiKhoans).FirstOrDefaultAsync(x => x.Id == id); if (d is null) return NotFound();
        try { d.Dong(User.Identity!.Name!, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = "Đã đóng đợt và lập biên bản rà soát."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public async Task<IActionResult> Xuat(int id)
    {
        var d = await db.DotRaSoatQuyens.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiKhoans).ThenInclude(t => t.KhoaPhong).FirstOrDefaultAsync(x => x.Id == id); if (d is null) return NotFound();
        using var wb = new XLWorkbook(); var ws = wb.AddWorksheet("Rà soát quyền");
        string[] tieuDe = ["Tên đăng nhập", "Họ tên", "Khoa", "Nhóm quyền", "Đăng nhập cuối", "Dấu hiệu", "Quyết định", "Quyền mới", "Người xác nhận", "Đã thực hiện", "Ghi chú"];
        for (var c = 0; c < tieuDe.Length; c++) ws.Cell(1, c + 1).Value = tieuDe[c];
        var r = 2;
        foreach (var t in d.TaiKhoans.OrderBy(t => t.KhoaPhong?.Ten).ThenBy(t => t.TenDangNhap))
        {
            ws.Cell(r, 1).Value = t.TenDangNhap; ws.Cell(r, 2).Value = t.HoTen; ws.Cell(r, 3).Value = t.KhoaPhong?.Ten ?? t.KhoaText; ws.Cell(r, 4).Value = t.NhomQuyen; ws.Cell(r, 5).Value = t.LanDangNhapCuoi?.ToString("dd/MM/yyyy");
            ws.Cell(r, 6).Value = string.Join("; ", t.DauHieu()); ws.Cell(r, 7).Value = t.QuyetDinh.TenHienThi(); ws.Cell(r, 8).Value = t.QuyenMoi; ws.Cell(r, 9).Value = t.NguoiXacNhan; ws.Cell(r, 10).Value = t.DaThucHien ? "Đã" : ""; ws.Cell(r, 11).Value = t.GhiChu; r++;
        }
        ws.Row(1).Style.Font.Bold = true; ws.Columns().AdjustToContents();
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{d.Ma}.xlsx");
    }

    public async Task<IActionResult> BienBan(int id)
    {
        if (!(CNTT || User.IsInRole(VaiTro.LanhDao))) return Forbid();
        var d = await db.DotRaSoatQuyens.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        return d is null || d.BienBan is null ? NotFound() : View(d);
    }

    /// <summary>Mẫu tệp nhập (CSV).</summary>
    public IActionResult Mau() => File(System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes("Tên đăng nhập,Họ tên,Khoa,Nhóm quyền,Lần đăng nhập cuối,Trạng thái\nnguyenvana,Nguyễn Văn A,Khoa Nội,Bác sĩ,2026-10-15,Hoạt động\n")).ToArray(), "text/csv", "mau-danh-sach-tai-khoan.csv");
}
