using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
public class ThanhLyController(AppDbContext db, SoPhieuService soPhieu, LichSuService lichSu) : Controller
{
    public async Task<IActionResult> Index(TrangThaiThanhLy? trangThai)
    {
        var q = db.DeXuatThanhLys.Include(p => p.ChiTiets).ThenInclude(c => c.TaiSan).AsNoTracking();
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        ViewData["trangThai"] = trangThai;
        return View(await q.OrderByDescending(p => p.Id).Take(200).ToListAsync());
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create()
    {
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        // Gợi ý: thiết bị hỏng hoặc quá tuổi thọ.
        var ds = await db.TaiSans.Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).AsNoTracking()
            .Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy)
            .OrderBy(t => t.MaTaiSan).ToListAsync();
        ViewBag.TaiSan = ds.OrderByDescending(t => t.TrangThai == TrangThaiTaiSan.Hong)
            .ThenByDescending(t => (t.TuoiNam(homNay) ?? 0) - (t.LoaiThietBi?.TuoiThoNam ?? 5)).ToList();
        return View(new DeXuatThanhLy());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(DeXuatThanhLy input, int[] taiSanIds)
    {
        var ds = await db.TaiSans.Where(t => taiSanIds.Contains(t.Id) && t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy).ToListAsync();
        if (ds.Count == 0) { TempData["err"] = "Chọn ít nhất một tài sản."; return RedirectToAction(nameof(Create)); }
        input.SoPhieu = await soPhieu.TaoAsync("TL");
        input.ChiTiets = ds.Select(t => new ChiTietThanhLy { TaiSanId = t.Id, TaiSan = t }).ToList();
        db.DeXuatThanhLys.Add(input);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lập đề xuất {input.SoPhieu}. Nhập tình trạng & giá trị còn lại rồi gửi hội đồng.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await Load(id, false);
        return p is null ? NotFound() : View(p);
    }

    public async Task<IActionResult> InBienBan(int id)
    {
        var p = await Load(id, false);
        return p is null ? NotFound() : View(p);
    }

    private Task<DeXuatThanhLy?> Load(int id, bool tracking)
    {
        var q = db.DeXuatThanhLys.Include(p => p.ChiTiets).ThenInclude(c => c.TaiSan).ThenInclude(t => t!.LoaiThietBi)
            .Include(p => p.ChiTiets).ThenInclude(c => c.TaiSan).ThenInclude(t => t!.KhoaPhong);
        return (tracking ? q : q.AsNoTracking()).FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<IActionResult> ThucHien(int id, Func<DeXuatThanhLy, string> action)
    {
        var p = await Load(id, true);
        if (p is null) return NotFound();
        try { TempData["ok"] = action(p); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Cập nhật chi tiết: tình trạng, giá trị còn lại, xác nhận xoá dữ liệu.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> CapNhatChiTiet(int id, List<ChiTietThanhLy> chiTiets) => ThucHien(id, p =>
    {
        if (p.TrangThai is TrangThaiThanhLy.DaThanhLy) throw new DomainException("Đề xuất đã hoàn tất.");
        foreach (var input in chiTiets)
        {
            var ct = p.ChiTiets.FirstOrDefault(c => c.Id == input.Id);
            if (ct is null) continue;
            if (p.TrangThai is TrangThaiThanhLy.Nhap or TrangThaiThanhLy.TuChoi) { ct.TinhTrang = input.TinhTrang; ct.GiaTriConLai = input.GiaTriConLai; }
            if (input.DaXoaDuLieu && !ct.DaXoaDuLieu)
            {
                if (string.IsNullOrWhiteSpace(input.XacNhanXoaDuLieu)) throw new DomainException($"Ghi phương pháp xoá dữ liệu cho {ct.TaiSan?.MaTaiSan}.");
                ct.DaXoaDuLieu = true; ct.XacNhanXoaDuLieu = $"{input.XacNhanXoaDuLieu} — {User.Identity?.Name} {DateTime.Now:dd/MM/yyyy HH:mm}";
            }
        }
        return "Đã cập nhật chi tiết.";
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> GuiHoiDong(int id) => ThucHien(id, p => { p.GuiHoiDong(); return "Đã gửi hội đồng đánh giá."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DanhGia(int id, string hoiDong, string ketLuan, bool dongY) => ThucHien(id, p =>
    {
        if (string.IsNullOrWhiteSpace(hoiDong) || string.IsNullOrWhiteSpace(ketLuan)) throw new DomainException("Nhập thành phần và kết luận hội đồng.");
        p.DanhGia(hoiDong, ketLuan, dongY, DateTime.Now);
        return dongY ? "Hội đồng đồng ý thanh lý." : "Hội đồng không đồng ý.";
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet)]
    public Task<IActionResult> PheDuyet(int id, string soQuyetDinh, DateOnly ngayQuyetDinh, HinhThucThanhLy hinhThuc) =>
        ThucHien(id, p => { p.PheDuyet(soQuyetDinh, ngayQuyetDinh, hinhThuc); return "Đã ghi nhận quyết định thanh lý."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> HoanTat(int id, decimal giaTriThuHoi) => ThucHien(id, p =>
    {
        p.HoanTat(giaTriThuHoi, DateTime.Now);
        foreach (var ct in p.ChiTiets)
            lichSu.Ghi(ct.TaiSanId, LoaiSuKien.ThanhLy, $"Thanh lý theo QĐ {p.SoQuyetDinh} ({p.HinhThuc?.Ten()})", User.Identity?.Name);
        return "Đã hoàn tất thanh lý, tài sản được ghi giảm.";
    });
}
