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
    public async Task<IActionResult> Index(string? doiTuong, string? nguoi, int page = 1)
    {
        var q = db.NhatKyHeThongs.AsNoTracking();
        if (!string.IsNullOrEmpty(doiTuong)) q = q.Where(x => x.DoiTuong == doiTuong);
        if (!string.IsNullOrEmpty(nguoi)) q = q.Where(x => x.NguoiDung == nguoi);
        ViewData["doiTuong"] = doiTuong; ViewData["nguoi"] = nguoi; ViewData["page"] = page;
        return View(await q.OrderByDescending(x => x.Id).Skip((page - 1) * 50).Take(50).ToListAsync());
    }
}
