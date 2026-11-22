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
public class ThanhLyController(AppDbContext db, SoPhieuService soPhieu, LichSuService lichSu, ThamSoService thamSo, ThongBaoService tb) : Controller
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
        if (string.IsNullOrWhiteSpace(input.LyDo)) { TempData["err"] = "Nhập lý do thanh lý."; return RedirectToAction(nameof(Create)); }
        var ds = await db.TaiSans.Where(t => taiSanIds.Contains(t.Id) && t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy).ToListAsync();
        if (ds.Count == 0) { TempData["err"] = "Chọn ít nhất một tài sản."; return RedirectToAction(nameof(Create)); }
        input.ChiTiets = ds.Select(t => new ChiTietThanhLy { TaiSanId = t.Id, TaiSan = t }).ToList();
        db.DeXuatThanhLys.Add(input);
        await db.LuuMoiAsync(soPhieu, "TL", so => input.SoPhieu = so);
        TempData["ok"] = $"Đã lập đề xuất {input.SoPhieu}. Nhập tình trạng & giá trị còn lại rồi gửi hội đồng.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await Load(id, false);
        if (p is null) return NotFound();
        ViewBag.BatBuocThaoOCung = thamSo.BatTat(TS.ThanhLyBatBuocThaoOCung);
        return View(p);
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
        return (tracking ? q : q.AsNoTracking()).AsSplitQuery().FirstOrDefaultAsync(p => p.Id == id);
    }

    private async Task<IActionResult> ThucHien(int id, Func<DeXuatThanhLy, string> action)
    {
        var p = await Load(id, true);
        if (p is null) return NotFound();
        try { TempData["ok"] = action(p); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Cập nhật tình trạng kỹ thuật & giá trị còn lại (khi còn nháp / bị từ chối).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> CapNhatChiTiet(int id, List<ChiTietThanhLy> chiTiets) => ThucHien(id, p =>
    {
        if (p.TrangThai is not (TrangThaiThanhLy.Nhap or TrangThaiThanhLy.TuChoi)) throw new DomainException("Chỉ sửa chi tiết khi đề xuất còn nháp.");
        foreach (var input in chiTiets)
        {
            var ct = p.ChiTiets.FirstOrDefault(c => c.Id == input.Id);
            if (ct is null) continue;
            if (input.GiaTriConLai < 0) throw new DomainException("Giá trị còn lại không được âm.");
            ct.TinhTrang = input.TinhTrang; ct.GiaTriConLai = input.GiaTriConLai;
        }
        return "Đã cập nhật chi tiết.";
    });

    /// <summary>QĐ-03: ghi nhận đã tháo ổ cứng (serial, nơi lưu) trước khi giao máy.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> ThaoOCung(int id, int chiTietId, string serial, string? noiLuu) => ThucHien(id, p =>
    {
        if (p.TrangThai is TrangThaiThanhLy.DaThanhLy or TrangThaiThanhLy.TuChoi) throw new DomainException("Đề xuất đã kết thúc.");
        var ct = p.ChiTiets.FirstOrDefault(c => c.Id == chiTietId) ?? throw new DomainException("Không tìm thấy tài sản.");
        ct.GhiNhanThaoOCung(serial, noiLuu ?? "", User.Identity!.Name!, DateTime.Now);
        lichSu.Ghi(ct.TaiSanId, LoaiSuKien.ThanhLy, $"Tháo ổ cứng SN {ct.SerialOCung}, lưu tại {ct.NoiLuuOCung} ({p.SoPhieu})", User.Identity?.Name);
        return $"Đã ghi nhận tháo ổ cứng {ct.TaiSan?.MaTaiSan}.";
    });

    /// <summary>Danh sách ổ cứng thu hồi từ thanh lý — theo dõi đến khi huỷ vật lý.</summary>
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> OCung(bool chuaHuy = true)
    {
        var q = db.ChiTietThanhLys.Include(c => c.TaiSan).Include(c => c.DeXuatThanhLy).AsNoTracking().Where(c => c.DaThaoOCung);
        if (chuaHuy) q = q.Where(c => c.XuLyOCung != XuLyOCung.HuyVatLy);
        ViewData["chuaHuy"] = chuaHuy;
        return View(await q.OrderBy(c => c.NgayThao).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> HuyOCung(int[] chiTietIds, string soBienBan)
    {
        var ds = await db.ChiTietThanhLys.Where(c => chiTietIds.Contains(c.Id)).ToListAsync();
        try
        {
            if (ds.Count == 0) throw new DomainException("Chọn ổ cứng cần huỷ.");
            foreach (var c in ds) c.HuyOCung(soBienBan, DateTime.Now);
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã ghi nhận huỷ {ds.Count} ổ cứng theo biên bản {soBienBan}.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(OCung));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> GuiHoiDong(int id) => ThucHien(id, p => { p.GuiHoiDong(); return "Đã gửi hội đồng đánh giá."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DanhGia(int id, string hoiDong, string ketLuan, bool dongY) => ThucHien(id, p =>
    {
        if (string.IsNullOrWhiteSpace(hoiDong) || string.IsNullOrWhiteSpace(ketLuan)) throw new DomainException("Nhập thành phần và kết luận hội đồng.");
        p.DanhGia(hoiDong, ketLuan, dongY, DateTime.Now);
        if (dongY) tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.ChoDuyet, $"Đề xuất thanh lý {p.SoPhieu} chờ quyết định",
            $"{p.ChiTiets.Count} tài sản · Hội đồng: {ketLuan}", $"/ThanhLy/Details/{p.Id}").GetAwaiter().GetResult();
        return dongY ? "Hội đồng đồng ý thanh lý — đã báo Lãnh đạo." : "Hội đồng không đồng ý.";
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet)]
    public Task<IActionResult> PheDuyet(int id, string soQuyetDinh, DateOnly ngayQuyetDinh, HinhThucThanhLy hinhThuc) =>
        ThucHien(id, p => { p.PheDuyet(soQuyetDinh, ngayQuyetDinh, hinhThuc); return "Đã ghi nhận quyết định thanh lý."; });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public Task<IActionResult> HoanTat(int id, decimal giaTriThuHoi) => ThucHien(id, p =>
    {
        p.HoanTat(giaTriThuHoi, DateTime.Now, thamSo.BatTat(TS.ThanhLyBatBuocThaoOCung));
        foreach (var ct in p.ChiTiets)
            lichSu.Ghi(ct.TaiSanId, LoaiSuKien.ThanhLy, $"Thanh lý theo QĐ {p.SoQuyetDinh} ({p.HinhThuc?.Ten()})", User.Identity?.Name);
        return "Đã hoàn tất thanh lý, tài sản được ghi giảm.";
    });
}
