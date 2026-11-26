using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>CRUD dùng chung cho các danh mục đơn giản (dùng view chung Views/Shared/DanhMuc*.cshtml).</summary>
[Authorize(Roles = VaiTro.NhomCNTT)]
public abstract class DanhMucController<T>(AppDbContext db) : Controller where T : BaseEntity, new()
{
    protected readonly AppDbContext Db = db;
    protected abstract string TieuDe { get; }
    protected abstract string[] Cot { get; }
    protected virtual IQueryable<T> TimKiem(IQueryable<T> q, string s) => q;

    public async Task<IActionResult> Index(string? q)
    {
        ViewData["Title"] = TieuDe; ViewData["Cot"] = Cot; ViewData["q"] = q;
        var query = Db.Set<T>().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q)) query = TimKiem(query, q.Trim());
        return View("~/Views/Shared/DanhMucIndex.cshtml", await query.OrderBy(x => x.Id).Take(500).ToListAsync());
    }

    protected virtual T TaoMoi() => new();

    public IActionResult Create() { ViewData["Title"] = "Thêm " + TieuDe.ToLower(); return View("~/Views/Shared/DanhMucForm.cshtml", TaoMoi()); }

    public async Task<IActionResult> Edit(int id)
    {
        var e = await Db.Set<T>().FindAsync(id);
        if (e is null) return NotFound();
        ViewData["Title"] = "Sửa " + TieuDe.ToLower();
        return View("~/Views/Shared/DanhMucForm.cshtml", e);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id)
    {
        var e = id == 0 ? new T() : await Db.Set<T>().FindAsync(id);
        if (e is null) return NotFound();
        if (!await TryUpdateModelAsync(e) || !ModelState.IsValid)
        {
            ViewData["Title"] = TieuDe;
            return View("~/Views/Shared/DanhMucForm.cshtml", e);
        }
        if (id == 0) Db.Add(e);
        try { await Db.SaveChangesAsync(); }
        catch (DbUpdateException)
        {
            ModelState.AddModelError("", "Không lưu được: mã có thể đã tồn tại.");
            ViewData["Title"] = TieuDe;
            return View("~/Views/Shared/DanhMucForm.cshtml", e);
        }
        TempData["ok"] = "Đã lưu.";
        return RedirectToAction(nameof(Index));
    }
}

public class KhoaPhongController(AppDbContext db) : DanhMucController<KhoaPhong>(db)
{
    protected override string TieuDe => "Khoa/phòng";
    protected override string[] Cot => [nameof(KhoaPhong.Ma), nameof(KhoaPhong.Ten), nameof(KhoaPhong.TruongKhoa), nameof(KhoaPhong.DienThoai), nameof(KhoaPhong.LaKhoaTrongYeu), nameof(KhoaPhong.DangHoatDong)];
    protected override IQueryable<KhoaPhong> TimKiem(IQueryable<KhoaPhong> q, string s) => q.Where(x => x.Ma.Contains(s) || x.Ten.Contains(s));
}

public class LoaiThietBiController(AppDbContext db, ThietBiCNTT.Infrastructure.Services.ThamSoService thamSo) : DanhMucController<LoaiThietBi>(db)
{
    protected override LoaiThietBi TaoMoi() => new() { TuoiThoNam = thamSo.SoNguyen(ThietBiCNTT.Infrastructure.Services.TS.TuoiThoMacDinhNam) };
    protected override string TieuDe => "Loại thiết bị";
    protected override string[] Cot => [nameof(LoaiThietBi.Ma), nameof(LoaiThietBi.Ten), nameof(LoaiThietBi.Nhom), nameof(LoaiThietBi.CoLuuTruDuLieu), nameof(LoaiThietBi.TuoiThoNam), nameof(LoaiThietBi.ChuKyBaoTriNgay)];
    protected override IQueryable<LoaiThietBi> TimKiem(IQueryable<LoaiThietBi> q, string s) => q.Where(x => x.Ma.Contains(s) || x.Ten.Contains(s));
}

public class NhaCungCapController(AppDbContext db) : DanhMucController<NhaCungCap>(db)
{
    protected override string TieuDe => "Nhà cung cấp";
    protected override string[] Cot => [nameof(NhaCungCap.Ten), nameof(NhaCungCap.MaSoThue), nameof(NhaCungCap.DienThoai), nameof(NhaCungCap.NguoiLienHe), nameof(NhaCungCap.LaDonViBaoHanh)];
    protected override IQueryable<NhaCungCap> TimKiem(IQueryable<NhaCungCap> q, string s) => q.Where(x => x.Ten.Contains(s) || (x.MaSoThue ?? "").Contains(s));
}

public class VatTuController(AppDbContext db) : DanhMucController<VatTu>(db)
{
    protected override string TieuDe => "Vật tư / linh kiện";
    protected override string[] Cot => [nameof(VatTu.Ma), nameof(VatTu.Ten), nameof(VatTu.Nhom), nameof(VatTu.DonViTinh), nameof(VatTu.TonKho), nameof(VatTu.TonToiThieu), nameof(VatTu.TonToiDa), nameof(VatTu.DonGia)];
    protected override IQueryable<VatTu> TimKiem(IQueryable<VatTu> q, string s) => q.Where(x => x.Ma.Contains(s) || x.Ten.Contains(s));
}

[Authorize(Roles = VaiTro.QuanTri)]
public class NhatKyController(AppDbContext db) : Controller
{
    private IQueryable<NhatKyHeThong> Loc(string? doiTuong, string? nguoi, string? hanhDong, DateTime? tu, DateTime? den, string? ip, string? q)
    {
        var x = db.NhatKyHeThongs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(doiTuong)) x = x.Where(n => n.DoiTuong == doiTuong.Trim());
        if (!string.IsNullOrWhiteSpace(nguoi)) x = x.Where(n => n.NguoiDung == nguoi.Trim());
        if (!string.IsNullOrWhiteSpace(hanhDong)) x = x.Where(n => n.HanhDong == hanhDong);
        if (tu is not null) x = x.Where(n => n.ThoiGian >= tu.Value.Date);
        if (den is not null) { var d = den.Value.Date.AddDays(1); x = x.Where(n => n.ThoiGian < d); }
        if (!string.IsNullOrWhiteSpace(ip)) x = x.Where(n => n.DiaChiIp == ip.Trim());
        if (!string.IsNullOrWhiteSpace(q)) x = x.Where(n => (n.ChiTiet != null && n.ChiTiet.Contains(q)) || n.DoiTuongId == q);
        return x;
    }

    public async Task<IActionResult> Index(string? doiTuong, string? nguoi, string? hanhDong, DateTime? tu, DateTime? den, string? ip, string? q, int page = 1)
    {
        ViewBag.Loc = new { doiTuong, nguoi, hanhDong, tu, den, ip, q };
        ViewData["page"] = page;
        var homNay = DateTime.Today;
        ViewBag.LoiDangNhap = await db.NhatKyHeThongs.CountAsync(n => n.HanhDong == ThietBiCNTT.Web.Infrastructure.ThaoTac.DangNhapLoi && n.ThoiGian >= homNay);
        return View(await Loc(doiTuong, nguoi, hanhDong, tu, den, ip, q).OrderByDescending(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync());
    }

    /// <summary>Xuất nhật ký theo bộ lọc (tối đa 50.000 dòng) — bản thân việc xuất cũng được ghi nhật ký.</summary>
    public async Task<IActionResult> Xuat(string? doiTuong, string? nguoi, string? hanhDong, DateTime? tu, DateTime? den, string? ip, string? q)
    {
        var ds = await Loc(doiTuong, nguoi, hanhDong, tu, den, ip, q).OrderByDescending(x => x.Id).Take(50_000).ToListAsync();
        using var wb = new ClosedXML.Excel.XLWorkbook();
        var ws = wb.AddWorksheet("NhatKy");
        string[] cot = ["Thời gian", "Người dùng", "IP", "Thao tác", "Đối tượng", "Id", "Chi tiết"];
        for (var i = 0; i < cot.Length; i++) ws.Cell(1, i + 1).Value = cot[i];
        var r = 2;
        foreach (var n in ds)
        {
            ws.Cell(r, 1).Value = n.ThoiGian; ws.Cell(r, 2).Value = n.NguoiDung; ws.Cell(r, 3).Value = n.DiaChiIp; ws.Cell(r, 4).Value = ThietBiCNTT.Web.Infrastructure.ThaoTac.Ten(n.HanhDong);
            ws.Cell(r, 5).Value = n.DoiTuong; ws.Cell(r, 6).Value = n.DoiTuongId; ws.Cell(r, 7).Value = n.ChiTiet; r++;
        }
        ws.Column(1).Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
        ThietBiCNTT.Infrastructure.Services.ExcelTaiSanService.DinhDangBang(ws, 1, Math.Max(1, r - 1), cot.Length);
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"NhatKy_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}

