using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public class TaiSanFormVm
{
    public TaiSan TaiSan { get; set; } = new();
    public CauHinh CauHinh { get; set; } = new();
    public string? LyDoThayDoi { get; set; }
    /// <summary>Trường tuỳ chỉnh: Id trường → giá trị nhập.</summary>
    public Dictionary<int, string?> TuyChinh { get; set; } = [];
}

public class TaiSanController(AppDbContext db, LichSuService lichSu, ThamSoService thamSo, TepService tep,
    MaTaiSanService maTs, TruongTuyChinhService tuyChinh, HoSoVanHanhService hoSo) : Controller
{
    private IQueryable<TaiSan> PhamViTaiSan()
    {
        var q = db.TaiSans.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(t => t.KhoaPhongId == k); }
        return q;
    }

    private IQueryable<TaiSan> Loc(string? q, int? khoa, int? loai, TrangThaiTaiSan? trangThai, bool? hetBaoHanh, int? tt = null, string? ttv = null)
    {
        var query = PhamViTaiSan().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.CauHinh).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(t => t.MaTaiSan.Contains(q) || t.Ten.Contains(q) || (t.SoSerial ?? "").Contains(q)
                || (t.NguoiSuDung ?? "").Contains(q) || (t.CauHinh!.DiaChiIP ?? "").Contains(q) || (t.CauHinh!.Hostname ?? "").Contains(q));
        }
        if (khoa is not null) query = query.Where(t => t.KhoaPhongId == khoa);
        if (loai is not null) query = query.Where(t => t.LoaiThietBiId == loai);
        if (trangThai is not null) query = query.Where(t => t.TrangThai == trangThai);
        else query = query.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy);
        if (tt is not null)
        {
            // Lọc theo trường tuỳ chỉnh: có nhập giá trị thì so khớp chuỗi con, để trống = có khai báo trường này
            var v = (ttv ?? "").Trim();
            query = v.Length == 0 ? query.Where(t => t.GiaTriTuyChinhs.Any(g => g.TruongTuyChinhId == tt))
                : query.Where(t => t.GiaTriTuyChinhs.Any(g => g.TruongTuyChinhId == tt && g.GiaTri.Contains(v)));
        }
        if (hetBaoHanh == true)
        {
            var d = DateOnly.FromDateTime(DateTime.Today);
            var n = thamSo.SoNguyen(TS.CanhBaoBaoHanhNgay);
            query = query.Where(t => t.HanBaoHanh >= d && t.HanBaoHanh <= d.AddDays(n));
        }
        return query;
    }

    public async Task<IActionResult> Index(string? q, int? khoa, int? loai, TrangThaiTaiSan? trangThai, bool? hetBaoHanh, int? tt, string? ttv, int page = 1)
    {
        const int size = 25;
        var query = Loc(q, khoa, loai, trangThai, hetBaoHanh, tt, ttv);
        var total = await query.CountAsync();
        var items = await query.OrderBy(t => t.MaTaiSan).Skip((page - 1) * size).Take(size).ToListAsync();
        await NapDanhMuc();
        ViewData["q"] = q; ViewData["khoa"] = khoa; ViewData["loai"] = loai; ViewData["trangThai"] = trangThai; ViewData["hetBaoHanh"] = hetBaoHanh;
        ViewData["tt"] = tt; ViewData["ttv"] = ttv;
        return View(new PagedList<TaiSan>(items, page, size, total));
    }

    /// <summary>Xuất Excel (.xlsx) theo bộ lọc hiện tại.</summary>
    public async Task<IActionResult> Export(string? q, int? khoa, int? loai, TrangThaiTaiSan? trangThai, bool? hetBaoHanh, int? tt, string? ttv)
    {
        var ds = await Loc(q, khoa, loai, trangThai, hetBaoHanh, tt, ttv).Include(t => t.NhaCungCap).Include(t => t.GiaTriTuyChinhs).OrderBy(t => t.MaTaiSan).ToListAsync();
        return File(ExcelTaiSanService.XuatTaiSan(ds, "DANH SÁCH TÀI SẢN CNTT", await tuyChinh.DangDungAsync()),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TaiSan_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // ===== Nhập từ Excel =====
    private static string ThuMucNhap => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "thietbicntt-nhap")).FullName;

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> MauNhap([FromServices] ExcelTaiSanService excel) =>
        File(await excel.TaoFileMauAsync(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "MauNhapTaiSan.xlsx");

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public IActionResult Nhap() => View();

    /// <summary>Bước 1: tải file lên → đọc & kiểm tra → xem trước (chưa ghi CSDL).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT), RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Nhap(IFormFile? file, bool capNhatDaCo, [FromServices] ExcelTaiSanService excel)
    {
        if (file is null || file.Length == 0 || !file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        { TempData["err"] = "Chọn file Excel .xlsx theo mẫu."; return View(); }
        // Dọn file tạm cũ hơn 1 ngày
        foreach (var f in new DirectoryInfo(ThuMucNhap).GetFiles().Where(f => f.CreationTime < DateTime.Now.AddDays(-1))) f.Delete();
        var ma = $"{Guid.NewGuid():N}_{User.Identity!.Name}";
        var duongDan = Path.Combine(ThuMucNhap, ma + ".xlsx");
        await using (var fs = System.IO.File.Create(duongDan)) await file.CopyToAsync(fs);
        try
        {
            await using var doc = System.IO.File.OpenRead(duongDan);
            var dong = await excel.DocAsync(doc);
            ViewBag.MaFile = ma; ViewBag.TenFile = file.FileName; ViewBag.CapNhatDaCo = capNhatDaCo;
            ViewBag.TenKhoa = await db.KhoaPhongs.ToDictionaryAsync(k => k.Id, k => k.Ten);
            return View("NhapXemTruoc", dong);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ClosedXML.Excel.Exceptions.ClosedXMLException or InvalidDataException or System.IO.FileFormatException)
        {
            System.IO.File.Delete(duongDan);
            TempData["err"] = "Không đọc được file: " + ex.Message;
            return View();
        }
    }

    /// <summary>Bước 2: xác nhận → ghi CSDL (trong 1 giao dịch).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XacNhanNhap(string maFile, bool capNhatDaCo, [FromServices] ExcelTaiSanService excel)
    {
        // Chỉ người đã tải lên mới xác nhận được file của mình
        if (string.IsNullOrEmpty(maFile) || !maFile.EndsWith("_" + User.Identity!.Name) || maFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return BadRequest();
        var duongDan = Path.Combine(ThuMucNhap, maFile + ".xlsx");
        if (!System.IO.File.Exists(duongDan)) { TempData["err"] = "Phiên nhập đã hết hạn — tải file lên lại."; return RedirectToAction(nameof(Nhap)); }
        try
        {
            List<DongNhapTaiSan> dong;
            await using (var doc = System.IO.File.OpenRead(duongDan)) dong = await excel.DocAsync(doc);
            var kq = await excel.NhapAsync(dong, capNhatDaCo, User.Identity.Name);
            System.IO.File.Delete(duongDan);
            TempData["ok"] = $"Đã nhập: {kq.ThemMoi} tài sản mới, cập nhật {kq.CapNhat}, bỏ qua {kq.BoQua} (mã đã có).";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex) { TempData["err"] = ex.Message; return RedirectToAction(nameof(Nhap)); }
    }

    public async Task<IActionResult> Details(int id)
    {
        var ts = await PhamViTaiSan().AsNoTracking()
            .Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.NhaCungCap).Include(t => t.ViTri)
            .Include(t => t.CauHinh).Include(t => t.TaiSanCha).Include(t => t.TaiSanCon)
            .Include(t => t.LichSu.OrderByDescending(l => l.ThoiGian).Take(50))
            .Include(t => t.PhieuSuaChuas.OrderByDescending(p => p.NgayYeuCau)).ThenInclude(p => p.VatTus)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (ts is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("TaiSan", id.ToString());
        ViewBag.HopDong = await db.HopDongTaiSans.AsNoTracking().Where(x => x.TaiSanId == id).Select(x => x.HopDong!).ToListAsync();
        ViewBag.BanQuyen = await db.CapPhatBanQuyens.AsNoTracking().Where(x => x.TaiSanId == id).Select(x => x.BanQuyenPhanMem!).ToListAsync();
        // Phiếu kiểm tra áp dụng cho thiết bị này (người dùng được ghi)
        var k = User.KhoaPhongId();
        ViewBag.MauKiemTra = await db.MauKiemTras.AsNoTracking().Where(m => m.DangApDung
                && (m.TaiSanId == id || (m.TaiSanId == null && m.LoaiThietBiId == ts.LoaiThietBiId && (m.KhoaPhongId == null || m.KhoaPhongId == ts.KhoaPhongId)))
                && (User.LaCNTT() || (m.KhoaPhongId != null && m.KhoaPhongId == k)))
            .OrderBy(m => m.Ten).ToListAsync();
        ViewBag.SoDo = await db.DiemSoDos.AsNoTracking().Where(d => d.TaiSanId == id && (User.XemToanVien() || d.SoDoMatBang!.KhoaPhongId == null || d.SoDoMatBang.KhoaPhongId == k))
            .Select(d => d.SoDoMatBang!).ToListAsync();
        ViewBag.Agent = await db.MayTramAgents.AsNoTracking().FirstOrDefaultAsync(m => m.TaiSanId == id);
        var gt = await tuyChinh.GiaTriAsync(id);
        ViewBag.TuyChinh = (await tuyChinh.DangDungAsync()).Where(t => t.ApDung(ts.LoaiThietBiId) || gt.ContainsKey(t.Id))
            .Select(t => (Truong: t, GiaTri: t.HienThi(gt.GetValueOrDefault(t.Id)))).ToList();
        ViewBag.HoSo = await hoSo.LapAsync(ts);
        return View(ts);
    }

    /// <summary>Đích của mã QR trên tem: /qr/{ma}</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Qr(string ma)
    {
        var id = await db.TaiSans.Where(t => t.MaTaiSan == ma).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        if (id is null) return NotFound();
        if (User.Identity?.IsAuthenticated != true) return Redirect($"/Account/Login?returnUrl=/TaiSan/Details/{id}");
        return RedirectToAction(nameof(Details), new { id });
    }

    [ResponseCache(Duration = 86400)]
    public async Task<IActionResult> QrImage(int id)
    {
        var ma = await db.TaiSans.Where(t => t.Id == id).Select(t => t.MaTaiSan).FirstOrDefaultAsync();
        if (ma is null) return NotFound();
        var goc = thamSo.Chuoi(TS.DiaChiTruyCap).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(goc)) goc = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var url = $"{goc}/qr/{Uri.EscapeDataString(ma)}";
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        return File(new PngByteQRCode(data).GetGraphic(8), "image/png");
    }

    /// <summary>In tem QR cho danh sách id (hoặc theo bộ lọc).</summary>
    public async Task<IActionResult> InTem(int[] ids, int? khoa)
    {
        var q = PhamViTaiSan().Include(t => t.KhoaPhong).AsNoTracking();
        q = ids.Length > 0 ? q.Where(t => ids.Contains(t.Id)) : q.Where(t => khoa == null || t.KhoaPhongId == khoa);
        return View(await q.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy).OrderBy(t => t.MaTaiSan).Take(300).ToListAsync());
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(int? copyFrom)
    {
        var vm = new TaiSanFormVm();
        vm.TaiSan.NgayMua = DateOnly.FromDateTime(DateTime.Today);
        if (copyFrom is { } cid && await db.TaiSans.Include(t => t.CauHinh).AsNoTracking().FirstOrDefaultAsync(t => t.Id == cid) is { } src)
        {
            vm.TaiSan = src; vm.TaiSan.Id = 0; vm.TaiSan.MaTaiSan = ""; vm.TaiSan.SoSerial = null;
            vm.CauHinh = src.CauHinh ?? new(); vm.CauHinh.DiaChiIP = vm.CauHinh.DiaChiMAC = vm.CauHinh.Hostname = null;
            vm.TuyChinh = (await tuyChinh.GiaTriAsync(cid)).ToDictionary(x => x.Key, x => (string?)x.Value);
        }
        await NapDanhMuc();
        return View("Form", vm);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Edit(int id)
    {
        var ts = await db.TaiSans.Include(t => t.CauHinh).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (ts is null) return NotFound();
        await NapDanhMuc(id);
        return View("Form", new TaiSanFormVm { TaiSan = ts, CauHinh = ts.CauHinh ?? new(),
            TuyChinh = (await tuyChinh.GiaTriAsync(id)).ToDictionary(x => x.Key, x => (string?)x.Value) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Save(TaiSanFormVm vm)
    {
        ModelState.Remove("TaiSan.LoaiThietBi"); ModelState.Remove("CauHinh.TaiSan");
        var input = vm.TaiSan;
        input.MaTaiSan = (input.MaTaiSan ?? "").Trim().ToUpperInvariant();
        var tuSinh = input.Id == 0 && input.MaTaiSan.Length == 0 && maTs.TuSinh;
        if (tuSinh) ModelState.Remove("TaiSan.MaTaiSan");
        var truong = await tuyChinh.DangDungAsync();
        var (loiTc, giaTriTc) = TruongTuyChinhService.KiemTra(truong, input.LoaiThietBiId, vm.TuyChinh);
        foreach (var l in loiTc) ModelState.AddModelError("", l);
        if (!tuSinh && await db.TaiSans.AnyAsync(t => t.MaTaiSan == input.MaTaiSan && t.Id != input.Id))
            ModelState.AddModelError("TaiSan.MaTaiSan", "Mã tài sản đã tồn tại.");
        if (input.TaiSanChaId == input.Id && input.Id != 0)
            ModelState.AddModelError("TaiSan.TaiSanChaId", "Thiết bị cha không hợp lệ.");
        if (!ModelState.IsValid) { await NapDanhMuc(input.Id); return View("Form", vm); }

        var user = User.Identity?.Name;
        if (input.Id == 0)
        {
            input.CauHinh = CoCauHinh(vm.CauHinh) ? vm.CauHinh : null;
            if (input.CauHinh is not null) input.CauHinh.NgayCapNhat = DateTime.Now;
            if (tuSinh)
            {
                try { input.MaTaiSan = await maTs.TaoAsync(input.LoaiThietBiId, input.KhoaPhongId); }
                catch (ThietBiCNTT.Domain.Common.DomainException ex) { ModelState.AddModelError("", ex.Message); await NapDanhMuc(); return View("Form", vm); }
            }
            db.TaiSans.Add(input);
            // Hai người thêm cùng lúc có thể trùng mã tự sinh (unique index) → sinh lại và thử lại
            for (var lan = 1; ; lan++)
            {
                try { await db.SaveChangesAsync(); break; }
                catch (DbUpdateException) when (tuSinh && lan < 4) { input.MaTaiSan = await maTs.TaoAsync(input.LoaiThietBiId, input.KhoaPhongId); }
            }
            lichSu.Ghi(input.Id, LoaiSuKien.TaoMoi, "Tạo mới hồ sơ tài sản", user);
            await tuyChinh.GhiAsync(input.Id, truong, giaTriTc, user, ghiLichSu: false);
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã thêm tài sản {input.MaTaiSan}.";
            return RedirectToAction(nameof(Details), new { id = input.Id });
        }

        var ts = await db.TaiSans.Include(t => t.CauHinh).FirstOrDefaultAsync(t => t.Id == input.Id);
        if (ts is null) return NotFound();
        var khoaCu = ts.KhoaPhongId;
        db.Entry(ts).CurrentValues.SetValues(input);
        // Điều chuyển khoa phải qua phiếu điều chuyển → không cho sửa trực tiếp (trừ tài sản đang trong kho).
        if (khoaCu is not null && khoaCu != input.KhoaPhongId)
        {
            ts.KhoaPhongId = khoaCu;
            TempData["err"] = "Không đổi khoa/phòng trực tiếp — hãy lập phiếu điều chuyển.";
        }
        ts.NgayTao = db.Entry(ts).OriginalValues.GetValue<DateTime>(nameof(TaiSan.NgayTao));
        ts.NguoiTao = db.Entry(ts).OriginalValues.GetValue<string?>(nameof(TaiSan.NguoiTao));
        if (CoCauHinh(vm.CauHinh) || ts.CauHinh is not null) lichSu.CapNhatCauHinh(ts, vm.CauHinh, user);
        await tuyChinh.GhiAsync(ts.Id, truong, giaTriTc, user);
        var thayDoi = db.Entry(ts).Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)).Select(p => p.Metadata.Name)
                        .Where(n => n is not (nameof(TaiSan.NgayCapNhat) or nameof(TaiSan.NguoiCapNhat))).ToList();
        if (thayDoi.Count > 0)
            lichSu.Ghi(ts.Id, LoaiSuKien.CapNhat, "Cập nhật: " + string.Join(", ", thayDoi) + (string.IsNullOrWhiteSpace(vm.LyDoThayDoi) ? "" : $" — {vm.LyDoThayDoi}"), user);
        await db.SaveChangesAsync();
        TempData["ok"] ??= "Đã lưu thay đổi.";
        return RedirectToAction(nameof(Details), new { id = ts.Id });
    }

    /// <summary>Xem trước mã sẽ sinh (form thêm tài sản).</summary>
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XemTruocMa(int loai, int? khoa)
    {
        try { return Json(new { ma = await maTs.TaoAsync(loai, khoa), tuSinh = maTs.TuSinh }); }
        catch (ThietBiCNTT.Domain.Common.DomainException ex) { return Json(new { loi = ex.Message }); }
    }

    private static bool CoCauHinh(CauHinh c) =>
        CauHinh.CacTruong.Any(f => !string.IsNullOrWhiteSpace((string?)typeof(CauHinh).GetProperty(f)!.GetValue(c)));

    private async Task NapDanhMuc(int? boQuaId = null)
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.Where(k => k.DangHoatDong).OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsLoai = new SelectList(await db.LoaiThietBis.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsViTri = new SelectList(await db.ViTris.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.TruongTuyChinh = await tuyChinh.DangDungAsync();
        ViewBag.TuSinhMa = maTs.TuSinh;
        ViewBag.DsCha = new SelectList(await db.TaiSans.Where(t => t.Id != boQuaId && t.TrangThai != TrangThaiTaiSan.DaThanhLy)
            .OrderBy(t => t.MaTaiSan).Select(t => new { t.Id, Ten = t.MaTaiSan + " - " + t.Ten }).ToListAsync(), "Id", "Ten");
    }
}
