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
public class BanQuyenController(AppDbContext db, ThamSoService thamSo) : Controller
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
        ViewBag.DsHopDong = new SelectList(await db.HopDongs.OrderByDescending(h => h.Id).Select(h => new { h.Id, Ten = h.SoHopDong + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(BanQuyenPhanMem input)
    {
        if (!ModelState.IsValid) return await Form(input.Id == 0 ? null : input.Id);
        if (input.Id == 0) db.BanQuyenPhanMems.Add(input);
        else
        {
            var b = await db.BanQuyenPhanMems.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = b.NgayTao; input.NguoiTao = b.NguoiTao;
            db.Entry(b).CurrentValues.SetValues(input);
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu bản quyền {input.Ten}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
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
        using var wb = new XLWorkbook();
        void Sheet(string ten, string tieuDe, string[] cot, IEnumerable<object?[]> dong, Action<IXLWorksheet>? dinhDang = null)
        {
            var ws = wb.AddWorksheet(ten);
            ws.Cell(1, 1).Value = tieuDe; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
            ws.Cell(2, 1).Value = $"Kỳ báo cáo: {r.Tu:dd/MM/yyyy} – {r.Den:dd/MM/yyyy} · {r.PhamVi}";
            for (var i = 0; i < cot.Length; i++) ws.Cell(4, i + 1).Value = cot[i];
            var n = 5;
            foreach (var x in dong) { for (var i = 0; i < x.Length; i++) ws.Cell(n, i + 1).Value = XLCellValue.FromObject(x[i]); n++; }
            ExcelTaiSanService.DinhDangBang(ws, 4, n - 1, cot.Length);
            dinhDang?.Invoke(ws);
        }
        static object? G(double? v) => v is null ? null : Math.Round(v.Value, 1);
        string[] cotSla = ["Nhóm", "Số phiếu", "Đã xử lý", "Đúng SLA", "% đúng SLA", "MTTR (giờ)", "Dừng máy (giờ)", "Chi phí (đ)"];
        object?[] DongSla(DongSla x) => [x.Nhom, x.SoPhieu, x.DaXong, x.DungHan, Math.Round(x.TyLeDungHan, 1), G(x.MttrGio), Math.Round(x.DungMayGio, 1), x.ChiPhi];
        Sheet("TheoThang", "SLA & MTTR THEO THÁNG", cotSla, r.TheoThang.Append(r.Tong).Select(DongSla), ws => ws.Column(8).Style.NumberFormat.Format = "#,##0");
        Sheet("TheoKhoa", "SLA & MTTR THEO KHOA/PHÒNG", cotSla, r.TheoKhoa.Select(DongSla), ws => ws.Column(8).Style.NumberFormat.Format = "#,##0");
        Sheet("TheoUuTien", "SLA THEO MỨC ƯU TIÊN", cotSla, r.TheoUuTien.Select(DongSla), ws => ws.Column(8).Style.NumberFormat.Format = "#,##0");
        Sheet("KhoaTrongYeu", "THỜI GIAN DỪNG MÁY – KHOA TRỌNG YẾU", cotSla, r.DungMayKhoaTrongYeu.Select(DongSla), ws => ws.Column(8).Style.NumberFormat.Format = "#,##0");
        Sheet("KyThuatVien", "NĂNG SUẤT KỸ THUẬT VIÊN", ["Kỹ thuật viên", "Số phiếu", "Sửa chữa đã xong", "Đúng SLA", "% đúng SLA", "MTTR (giờ)", "Giờ công", "Đánh giá TB (1-5)", "Lệnh bảo trì"],
            r.TheoKtv.Select(x => new object?[] { x.KyThuatVien, x.SoPhieu, x.DaXong, x.DungHan, Math.Round(x.TyLeDungHan, 1), G(x.MttrGio), x.GioCong, G(x.DanhGiaTb), x.BaoTri }));
        Sheet("ThietBiHongNhieu", "THIẾT BỊ HỎNG NHIỀU NHẤT (TOP 20)", ["Mã TS", "Tên", "Khoa", "Loại", "Số lần hỏng", "Dừng máy (giờ)", "Chi phí SC (đ)", "MTBF (ngày)", "Nguyên giá (đ)"],
            r.ThietBiHongNhieu.Select(x => new object?[] { x.MaTaiSan, x.Ten, x.Khoa, x.Loai, x.SoLanHong, Math.Round(x.DungMayGio, 1), x.ChiPhi, x.MtbfNgay, x.NguyenGia }),
            ws => { ws.Column(7).Style.NumberFormat.Format = "#,##0"; ws.Column(9).Style.NumberFormat.Format = "#,##0"; });
        string[] cotCp = ["Nhóm", "Số phiếu", "Vật tư (đ)", "Thuê ngoài (đ)", "Tổng (đ)"];
        Sheet("ChiPhiTheoKhoa", "CHI PHÍ SỬA CHỮA, BẢO TRÌ THEO KHOA", cotCp, r.ChiPhiTheoKhoa.Select(x => new object?[] { x.Nhom, x.SoPhieu, x.VatTu, x.ThueNgoai, x.Tong }),
            ws => ws.Columns(3, 5).Style.NumberFormat.Format = "#,##0");
        Sheet("ChiPhiTheoLoai", "CHI PHÍ SỬA CHỮA, BẢO TRÌ THEO LOẠI THIẾT BỊ", cotCp, r.ChiPhiTheoLoai.Select(x => new object?[] { x.Nhom, x.SoPhieu, x.VatTu, x.ThueNgoai, x.Tong }),
            ws => ws.Columns(3, 5).Style.NumberFormat.Format = "#,##0");
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCao_{r.Tu:yyyyMMdd}_{r.Den:yyyyMMdd}.xlsx");
    }
}
