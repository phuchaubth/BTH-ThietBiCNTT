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

public class DongVatTuKhVm { public int VatTuId { get; set; } public decimal SoLuong { get; set; } }
public record MocLich(DateOnly Ngay, KeHoachBaoTri KeHoach, List<TaiSan> TaiSans, bool LaLenhMo, PhieuSuaChua? Lenh);

/// <summary>Bảo trì định kỳ (FR-PM): kế hoạch có thẩm định/duyệt, lịch, dự trù, sinh lệnh.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class KeHoachBaoTriController(AppDbContext db, BaoTriService baoTri, ThongBaoService tb, UserManager<AppUser> users) : Controller
{
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];
    private DateOnly HomNay => DateOnly.FromDateTime(DateTime.Today);

    public async Task<IActionResult> Index(TrangThaiKeHoach? trangThai, bool choToi = false)
    {
        var q = db.KeHoachBaoTris.Include(k => k.LoaiThietBi).Include(k => k.TaiSan).Include(k => k.KhoaPhong).Include(k => k.MauKiemTra).AsNoTracking();
        if (trangThai is not null) q = q.Where(k => k.TrangThai == trangThai);
        var u = User.Identity!.Name;
        if (choToi) q = q.Where(k => (k.TrangThai == TrangThaiKeHoach.ChoThamDinh && k.NguoiThamDinh == u) || (k.TrangThai == TrangThaiKeHoach.ChoDuyet && k.NguoiDuyet == u));
        var ds = await q.OrderBy(k => k.TrangThai).ThenBy(k => k.NgayTiepTheo).ToListAsync();
        ViewBag.SoLenhMo = await db.PhieuSuaChuas.CountAsync(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy && !Dong.Contains(p.TrangThai));
        ViewBag.ChoToi = await db.KeHoachBaoTris.CountAsync(k => (k.TrangThai == TrangThaiKeHoach.ChoThamDinh && k.NguoiThamDinh == u) || (k.TrangThai == TrangThaiKeHoach.ChoDuyet && k.NguoiDuyet == u));
        ViewData["trangThai"] = trangThai; ViewData["choToi"] = choToi;
        return View(ds);
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var kh = await db.KeHoachBaoTris.AsNoTracking().Include(k => k.LoaiThietBi).Include(k => k.TaiSan).Include(k => k.KhoaPhong).Include(k => k.MauKiemTra)
            .Include(k => k.VatTus).ThenInclude(v => v.VatTu).FirstOrDefaultAsync(k => k.Id == id);
        if (kh is null) return NotFound();
        var ts = await baoTri.PhamVi(kh).AsNoTracking().Include(t => t.KhoaPhong).OrderBy(t => t.MaTaiSan).ToListAsync();
        var lenh = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Where(p => p.KeHoachBaoTriId == id).OrderByDescending(p => p.NgayYeuCau).Take(200).ToListAsync();
        ViewBag.TaiSans = ts; ViewBag.Lenh = lenh;
        ViewBag.CacMoc = kh.CacMoc(HomNay, HomNay.AddYears(1)).Take(6).ToList();
        return View(kh);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(int? loaiId, int? taiSanId)
    {
        await Nap();
        var kh = new KeHoachBaoTri { NgayTiepTheo = HomNay.AddDays(7), TrangThai = TrangThaiKeHoach.Nhap, DangApDung = false, LoaiThietBiId = loaiId, TaiSanId = taiSanId };
        return View("Form", kh);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Edit(int id)
    {
        var k = await db.KeHoachBaoTris.Include(x => x.VatTus).FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        await Nap(); return View("Form", k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Save(KeHoachBaoTri input, List<DongVatTuKhVm> vatTu, bool guiTrinh = false)
    {
        ModelState.Remove(nameof(input.LoaiThietBi)); ModelState.Remove(nameof(input.TaiSan));
        try { input.KiemTra(); } catch (DomainException ex) { ModelState.AddModelError("", ex.Message); }
        if (!ModelState.IsValid) { await Nap(); input.VatTus = vatTu.Where(v => v.VatTuId > 0).Select(v => new KeHoachVatTu { VatTuId = v.VatTuId, SoLuong = v.SoLuong }).ToList(); return View("Form", input); }
        KeHoachBaoTri k;
        var thongBao = "Đã lưu kế hoạch bảo trì.";
        if (input.Id == 0)
        {
            k = new KeHoachBaoTri { TrangThai = TrangThaiKeHoach.Nhap, DangApDung = false };
            db.KeHoachBaoTris.Add(k);
        }
        else
        {
            k = await db.KeHoachBaoTris.Include(x => x.VatTus).FirstOrDefaultAsync(x => x.Id == input.Id) ?? throw new InvalidOperationException();
            // Đổi phạm vi / chu kỳ của kế hoạch đã duyệt → phải trình duyệt lại
            var doiCotLoi = k.LoaiThietBiId != input.LoaiThietBiId || k.TaiSanId != input.TaiSanId || k.KhoaPhongId != input.KhoaPhongId || k.KieuChuKy != input.KieuChuKy
                            || k.ChuKyNgay != input.ChuKyNgay || k.ChuKyChiSo != input.ChuKyChiSo;
            if (doiCotLoi && k.TrangThai is TrangThaiKeHoach.HieuLuc or TrangThaiKeHoach.ChoDuyet or TrangThaiKeHoach.ChoThamDinh or TrangThaiKeHoach.TamDung)
            { k.VeNhap(); thongBao = "Đã lưu. Kế hoạch đổi phạm vi/chu kỳ nên trở về Nháp — cần trình duyệt lại."; }
            db.KeHoachVatTus.RemoveRange(k.VatTus);
        }
        k.Ten = input.Ten; k.LoaiThietBiId = input.TaiSanId is null ? input.LoaiThietBiId : null; k.TaiSanId = input.TaiSanId; k.KhoaPhongId = input.TaiSanId is null ? input.KhoaPhongId : null;
        k.KieuChuKy = input.KieuChuKy; k.ChuKyNgay = input.ChuKyNgay; k.ChuKyChiSo = input.KieuChuKy == KieuChuKy.ThoiGian ? null : input.ChuKyChiSo; k.DonViChiSo = input.DonViChiSo;
        k.NgayTiepTheo = input.NgayTiepTheo; k.NgayKetThuc = input.NgayKetThuc; k.Checklist = input.Checklist; k.MauKiemTraId = input.MauKiemTraId;
        k.KyThuatVien = input.KyThuatVien; k.GioCongDuKien = input.GioCongDuKien; k.ChiPhiDuKien = input.ChiPhiDuKien;
        k.NguoiThamDinh = string.IsNullOrWhiteSpace(input.NguoiThamDinh) ? null : input.NguoiThamDinh; k.NguoiDuyet = input.NguoiDuyet;
        k.VatTus = vatTu.Where(v => v.VatTuId > 0 && v.SoLuong > 0).GroupBy(v => v.VatTuId).Select(g => new KeHoachVatTu { VatTuId = g.Key, SoLuong = g.Sum(x => x.SoLuong) }).ToList();
        await db.SaveChangesAsync();
        if (guiTrinh)
        {
            try { k.GuiTrinh(); await db.SaveChangesAsync(); await BaoBuocTiep(k); thongBao = $"Đã gửi trình — chờ {(k.TrangThai == TrangThaiKeHoach.ChoThamDinh ? "thẩm định: " + k.NguoiThamDinh : "duyệt: " + k.NguoiDuyet)}."; }
            catch (DomainException ex) { TempData["err"] = ex.Message; }
        }
        TempData["ok"] = thongBao;
        return RedirectToAction(nameof(ChiTiet), new { id = k.Id });
    }

    private async Task BaoBuocTiep(KeHoachBaoTri k)
    {
        var (nhan, td) = k.TrangThai switch
        {
            TrangThaiKeHoach.ChoThamDinh => (k.NguoiThamDinh, $"Kế hoạch bảo trì \"{k.Ten}\" chờ bạn thẩm định"),
            TrangThaiKeHoach.ChoDuyet => (k.NguoiDuyet, $"Kế hoạch bảo trì \"{k.Ten}\" chờ bạn duyệt"),
            TrangThaiKeHoach.HieuLuc => (k.NguoiTao, $"Kế hoạch bảo trì \"{k.Ten}\" đã được duyệt — có hiệu lực"),
            _ => (k.NguoiTao, $"Kế hoạch bảo trì \"{k.Ten}\" bị trả lại")
        };
        var ds = new List<string?> { nhan };
        if (k.TrangThai == TrangThaiKeHoach.HieuLuc) ds.Add(k.KyThuatVien);
        await tb.GuiAsync(ds, k.TrangThai is TrangThaiKeHoach.ChoThamDinh or TrangThaiKeHoach.ChoDuyet ? LoaiThongBao.ChoDuyet : LoaiThongBao.KetQuaDuyet, td,
            k.YKien, $"/KeHoachBaoTri/ChiTiet/{k.Id}", k.TrangThai == TrangThaiKeHoach.Nhap ? MucDoThongBao.CanhBao : MucDoThongBao.ThongTin, boQua: User.Identity?.Name);
        await db.SaveChangesAsync();
    }

    private async Task<IActionResult> ChuyenBuoc(int id, Action<KeHoachBaoTri> a, string ok)
    {
        var k = await db.KeHoachBaoTris.FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        try { a(k); await db.SaveChangesAsync(); await BaoBuocTiep(k); TempData["ok"] = ok; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> GuiTrinh(int id) => ChuyenBuoc(id, k => k.GuiTrinh(), "Đã gửi trình.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> ThamDinh(int id, bool dongY, string? yKien) =>
        ChuyenBuoc(id, k => k.ThamDinh(User.Identity!.Name!, dongY, yKien, DateTime.Now), dongY ? "Đã thẩm định — chuyển xét duyệt." : "Đã trả lại người lập.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Duyet(int id, bool dongY, string? yKien) =>
        ChuyenBuoc(id, k => k.Duyet(User.Identity!.Name!, User.IsInRole(VaiTro.QuanTri), dongY, yKien, DateTime.Now), dongY ? "Đã duyệt — kế hoạch có hiệu lực." : "Đã trả lại người lập.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> TamDung(int id) => ChuyenBuoc(id, k => k.TamDung(), "Đã tạm dừng — không sinh lệnh mới.");

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> TiepTuc(int id) => ChuyenBuoc(id, k => k.TiepTuc(HomNay), "Đã cho kế hoạch tiếp tục hiệu lực.");

    /// <summary>Sinh ngay các lệnh bảo trì đến hạn (bình thường chạy nền mỗi vài giờ).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> SinhLenh()
    {
        var n = await baoTri.SinhLenhDenHanAsync(HomNay);
        TempData["ok"] = n == 0 ? "Không có kế hoạch nào đến hạn." : $"Đã sinh {n} lệnh bảo trì định kỳ.";
        return RedirectToAction(nameof(Index));
    }

    // ===== Lịch (FR-PM-06, 13) =====
    public async Task<IActionResult> Lich(int? nam, int? thang, string nhom = "nhiemvu", string? ktv = null, int? khoa = null)
    {
        var dau = new DateOnly(nam ?? HomNay.Year, thang ?? HomNay.Month, 1);
        var cuoi = dau.AddMonths(1).AddDays(-1);
        var khs = await db.KeHoachBaoTris.AsNoTracking().Include(k => k.LoaiThietBi).Include(k => k.TaiSan)
            .Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc && (ktv == null || k.KyThuatVien == ktv)).ToListAsync();
        var moc = new List<MocLich>();
        foreach (var k in khs)
        {
            var cac = k.CacMoc(dau < HomNay ? HomNay : dau, cuoi).ToList();
            if (cac.Count == 0) continue;
            var ts = await baoTri.PhamVi(k).AsNoTracking().Where(t => khoa == null || t.KhoaPhongId == khoa).OrderBy(t => t.MaTaiSan).ToListAsync();
            if (ts.Count == 0) continue;
            moc.AddRange(cac.Select(d => new MocLich(d, k, ts, false, null)));
        }
        // Lệnh bảo trì đang mở (gồm lệnh sinh theo chỉ số) theo hạn hoàn thành
        var lenh = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Include(p => p.KeHoachBaoTri)
            .Where(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy && !Dong.Contains(p.TrangThai) && (ktv == null || p.KyThuatVien == ktv) && (khoa == null || p.KhoaPhongId == khoa))
            .ToListAsync();
        moc.AddRange(lenh.Where(p => DateOnly.FromDateTime(p.HanXuLy) >= dau && DateOnly.FromDateTime(p.HanXuLy) <= cuoi && p.KeHoachBaoTri is not null)
            .Select(p => new MocLich(DateOnly.FromDateTime(p.HanXuLy), p.KeHoachBaoTri!, [p.TaiSan!], true, p)));
        ViewData["dau"] = dau; ViewData["nhom"] = nhom; ViewData["ktv"] = ktv; ViewData["khoa"] = khoa;
        ViewBag.DsKtv = new SelectList((await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Select(x => x.UserName), ktv);
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(x => x.DangHoatDong).OrderBy(x => x.Ten).ToListAsync(), "Id", "Ten", khoa);
        ViewBag.QuaHan = lenh.Count(p => p.HanXuLy < DateTime.Now);
        return View(moc);
    }

    /// <summary>Dời mốc kế tiếp của kế hoạch (kéo thả trên lịch) — bắt buộc lý do, ghi nhật ký, báo KTV.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> DoiLich(int id, DateOnly tuNgay, DateOnly ngayMoi, string? lyDo)
    {
        var k = await db.KeHoachBaoTris.FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        if (string.IsNullOrWhiteSpace(lyDo)) return Json(new { ok = false, loi = "Nhập lý do dời lịch." });
        if (!k.TheoThoiGian || k.TrangThai != TrangThaiKeHoach.HieuLuc) return Json(new { ok = false, loi = "Chỉ dời lịch kế hoạch theo thời gian đang hiệu lực." });
        if (tuNgay != k.NgayTiepTheo) return Json(new { ok = false, loi = $"Chỉ dời được mốc kế tiếp ({k.NgayTiepTheo:dd/MM/yyyy}); các mốc sau tự tính theo chu kỳ." });
        if (ngayMoi < HomNay) return Json(new { ok = false, loi = "Không dời về quá khứ." });
        k.NgayTiepTheo = ngayMoi;
        db.NhatKyHeThongs.Add(new NhatKyHeThong { ThoiGian = DateTime.Now, NguoiDung = User.Identity?.Name, HanhDong = "Sua", DoiTuong = nameof(KeHoachBaoTri), DoiTuongId = k.Id.ToString(),
            ChiTiet = $"Dời lịch {tuNgay:dd/MM/yyyy} → {ngayMoi:dd/MM/yyyy}: {lyDo}" });
        await db.SaveChangesAsync();
        await tb.GuiAsync([k.KyThuatVien], LoaiThongBao.BaoTri, $"Dời lịch bảo trì \"{k.Ten}\" sang {ngayMoi:dd/MM/yyyy}", lyDo, $"/KeHoachBaoTri/ChiTiet/{k.Id}", boQua: User.Identity?.Name);
        await db.SaveChangesAsync();
        return Json(new { ok = true });
    }

    // ===== Dự trù (FR-PM-12) =====
    public async Task<IActionResult> DuTru(DateOnly? tu, DateOnly? den)
    {
        var t = tu ?? HomNay; var d = den ?? t.AddYears(1).AddDays(-1);
        ViewData["tu"] = t; ViewData["den"] = d;
        return View(await baoTri.DuTruAsync(t, d));
    }

    public async Task<IActionResult> XuatDuTru(DateOnly? tu, DateOnly? den)
    {
        var t = tu ?? HomNay; var d = den ?? t.AddYears(1).AddDays(-1);
        var ds = await baoTri.DuTruAsync(t, d);
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("DuTru");
        ws.Cell(1, 1).Value = $"DỰ TRÙ BẢO TRÌ ĐỊNH KỲ {t:dd/MM/yyyy} – {d:dd/MM/yyyy}"; ws.Cell(1, 1).Style.Font.Bold = true;
        string[] cot = ["Kế hoạch", "Phạm vi", "KTV", "Số TS", "Số mốc", "Số lượt", "Giờ công", "Vật tư (đ)", "Thuê ngoài (đ)", "Tổng (đ)"];
        for (var i = 0; i < cot.Length; i++) ws.Cell(3, i + 1).Value = cot[i];
        var r = 4;
        foreach (var x in ds)
        {
            object?[] v = [x.KeHoach.Ten, x.KeHoach.TaiSan?.MaTaiSan ?? x.KeHoach.LoaiThietBi?.Ten, x.KeHoach.KyThuatVien, x.SoTaiSan, x.SoMoc, x.SoLuot, x.GioCong, x.ChiPhiVatTu, x.ChiPhiThueNgoai, x.TongChiPhi];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = ClosedXML.Excel.XLCellValue.FromObject(v[i]);
            r++;
        }
        ExcelTaiSanService.DinhDangBang(ws, 3, Math.Max(3, r - 1), cot.Length);
        ws.Columns(8, 10).Style.NumberFormat.Format = "#,##0";
        var vt = wb.AddWorksheet("VatTu");
        string[] c2 = ["Mã", "Vật tư", "ĐVT", "Số lượng dự trù", "Tồn hiện tại", "Cần mua thêm", "Đơn giá", "Thành tiền"];
        for (var i = 0; i < c2.Length; i++) vt.Cell(1, i + 1).Value = c2[i];
        r = 2;
        foreach (var g in ds.SelectMany(x => x.VatTu).GroupBy(x => x.VatTu.Id))
        {
            var v0 = g.First().VatTu; var sl = g.Sum(x => x.SoLuong);
            object?[] v = [v0.Ma, v0.Ten, v0.DonViTinh, sl, v0.TonKho, Math.Max(0, sl - v0.TonKho), v0.DonGia, g.Sum(x => x.ThanhTien)];
            for (var i = 0; i < v.Length; i++) vt.Cell(r, i + 1).Value = ClosedXML.Excel.XLCellValue.FromObject(v[i]);
            r++;
        }
        ExcelTaiSanService.DinhDangBang(vt, 1, Math.Max(1, r - 1), c2.Length);
        vt.Columns(7, 8).Style.NumberFormat.Format = "#,##0";
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DuTruBaoTri_{t:yyyyMMdd}.xlsx");
    }

    /// <summary>Gợi ý chu kỳ (FR-PM-15): mặc định theo khuyến cáo của loại; rút ngắn 1/2 cho tài sản ở khoa trọng yếu hoặc hỏng ≥ 3 lần/12 tháng.</summary>
    public async Task<IActionResult> GoiYChuKy(int? loaiId, int? taiSanId)
    {
        var ts = taiSanId is null ? null : await db.TaiSans.Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).AsNoTracking().FirstOrDefaultAsync(t => t.Id == taiSanId);
        var loai = ts?.LoaiThietBi ?? (loaiId is null ? null : await db.LoaiThietBis.FindAsync(loaiId));
        if (loai?.ChuKyBaoTriNgay is not { } goc) return Json(new { });
        if (ts is null) return Json(new { chuKy = goc, lyDo = $"Khuyến cáo cho {loai.Ten}: {goc} ngày" });
        var hong = await db.PhieuSuaChuas.CountAsync(p => p.TaiSanId == ts.Id && p.LoaiCongViec == LoaiCongViec.SuaChua && p.NgayYeuCau > DateTime.Now.AddYears(-1));
        var rutNgan = ts.KhoaPhong?.LaKhoaTrongYeu == true || hong >= 3;
        return Json(new { chuKy = rutNgan ? Math.Max(7, goc / 2) : goc,
            lyDo = rutNgan ? $"Rút ngắn từ {goc} ngày: " + (ts.KhoaPhong?.LaKhoaTrongYeu == true ? "khoa trọng yếu" : $"hỏng {hong} lần trong 12 tháng") : $"Khuyến cáo cho {loai.Ten}: {goc} ngày" });
    }

    private async Task Nap()
    {
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(l => l.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy).OrderBy(t => t.MaTaiSan)
            .Select(t => new { t.Id, Ten = t.MaTaiSan + " - " + t.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsMau = new SelectList(await db.MauKiemTras.Where(m => m.DangApDung).OrderByDescending(m => m.DungChoBaoTri).ThenBy(m => m.Ten)
            .Select(m => new { m.Id, Ten = m.Ten + (m.DungChoBaoTri ? " (checklist bảo trì)" : "") }).ToListAsync(), "Id", "Ten");
        var cntt = (await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Concat(await users.GetUsersInRoleAsync(VaiTro.QuanTri)).Where(u => u.DangHoatDong).DistinctBy(u => u.Id).ToList();
        var duyet = cntt.Concat(await users.GetUsersInRoleAsync(VaiTro.LanhDao)).Where(u => u.DangHoatDong).DistinctBy(u => u.Id).ToList();
        ViewBag.DsKtv = new SelectList(cntt.Select(u => new { u.UserName, Ten = $"{u.HoTen} ({u.UserName})" }), "UserName", "Ten");
        ViewBag.DsNguoiDuyet = new SelectList(duyet.Select(u => new { u.UserName, Ten = $"{u.HoTen} ({u.UserName})" }), "UserName", "Ten");
        ViewBag.VatTus = await db.VatTus.AsNoTracking().OrderBy(v => v.Ma).ToListAsync();
    }
}
