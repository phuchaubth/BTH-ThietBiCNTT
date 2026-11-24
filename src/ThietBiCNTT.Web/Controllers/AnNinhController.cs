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

/// <summary>
/// Phân hệ 4.6 – Sự cố an ninh mạng (HCT.01.05, MOI.01.02). Người dùng chỉ báo và xem trạng thái tiếp nhận;
/// chi tiết chỉ nhóm an toàn thông tin được phân quyền và Lãnh đạo xem (FR-AN-06).
/// </summary>
public class AnNinhController(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb, TepService tep, HanhDongService hanhDong, UserManager<AppUser> users) : Controller
{
    private bool Xem => User.XemAn(thamSo);
    private bool Xu => User.LaAttt(thamSo);

    // ===== Người dùng báo (mọi tài khoản) =====

    public IActionResult Bao() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Bao(string moTa, string? maTaiSan, LoaiSuCoAn loai = LoaiSuCoAn.Khac)
    {
        if (string.IsNullOrWhiteSpace(moTa)) { TempData["err"] = "Mô tả điều nghi ngờ (ví dụ: nhận email lạ, máy hiện thông báo đòi tiền, mất thiết bị…)."; return View(); }
        var ts = string.IsNullOrWhiteSpace(maTaiSan) ? null : await db.TaiSans.FirstOrDefaultAsync(t => t.MaTaiSan == maTaiSan.Trim().ToUpper());
        var s = new SuCoAnNinh { Nguon = NguonSuCoAn.NguoiDungBao, Loai = loai, MucDo = MucDoAn.ThongThuong, TrangThai = TrangThaiSuCoAn.Moi, MoTa = moTa.Trim(), PhatHien = DateTime.Now, NguoiBao = User.Identity!.Name, TaiSanId = ts?.Id };
        db.SuCoAnNinhs.Add(s);
        await db.LuuMoiAsync(soPhieu, "AN", so => s.Ma = so);
        await ThongBaoNhomAsync(s, "Báo sự cố an ninh mới", MucDoThongBao.CanhBao);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã gửi báo cáo {s.Ma}. Phòng CNTT sẽ tiếp nhận và xử lý.";
        return RedirectToAction(nameof(CuaToi));
    }

    /// <summary>Người báo chỉ thấy trạng thái tiếp nhận.</summary>
    public async Task<IActionResult> CuaToi()
    {
        var u = User.Identity!.Name;
        return View(await db.SuCoAnNinhs.AsNoTracking().Where(s => s.NguoiBao == u).OrderByDescending(s => s.Id).Select(s => new { s.Ma, s.PhatHien, s.TrangThai, s.MoTa }).ToListAsync().ContinueWith(t => t.Result.Select(x => (x.Ma, x.PhatHien, x.TrangThai, x.MoTa)).ToList()));
    }

    private async Task ThongBaoNhomAsync(SuCoAnNinh s, string tieuDe, MucDoThongBao muc)
    {
        // Không gửi chi tiết qua Telegram/email (NFR-JC-04): chỉ thông báo trong ứng dụng, tiêu đề không nêu nội dung.
        var nhom = thamSo.Chuoi(TS.JciNhomAnToanThongTin).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
        await tb.GuiAsync(nhom, LoaiThongBao.AnNinhMang, $"{tieuDe} ({s.Ma})", "Mở hệ thống để xem chi tiết.", $"/AnNinh/Details/{s.Id}", muc, boQua: User.Identity!.Name);
    }

    // ===== Nhóm an toàn thông tin + Lãnh đạo =====

    public async Task<IActionResult> Index(TrangThaiSuCoAn? trangThai, MucDoAn? mucDo, bool chuaDong = true)
    {
        if (!Xem) return Forbid();
        var q = db.SuCoAnNinhs.AsNoTracking().Include(s => s.HeThong).AsQueryable();
        if (trangThai is not null) { q = q.Where(s => s.TrangThai == trangThai); chuaDong = false; }
        if (chuaDong) q = q.Where(s => s.TrangThai != TrangThaiSuCoAn.DaDong);
        if (mucDo is not null) q = q.Where(s => s.MucDo == mucDo);
        ViewData["trangThai"] = trangThai; ViewData["mucDo"] = mucDo; ViewData["chuaDong"] = chuaDong; ViewBag.Xu = Xu; ViewBag.Gio = thamSo.SoNguyen(TS.JciHanBaoCaoSuCoGio);
        return View((await q.ToListAsync()).OrderByDescending(s => s.MucDo).ThenByDescending(s => s.PhatHien).Take(300).ToList());
    }

    private async Task NapAsync()
    {
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
        ViewBag.DsNguoi = await users.DsCnttAsync(User.Identity!.Name);
    }

    public async Task<IActionResult> Create()
    {
        if (!Xu) return Forbid();
        await NapAsync();
        return View(new SuCoAnNinh { PhatHien = DateTime.Now });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SuCoAnNinh input, string? maTaiSan)
    {
        if (!Xu) return Forbid();
        ModelState.Remove(nameof(input.Ma)); ModelState.Remove(nameof(input.HeThong)); ModelState.Remove(nameof(input.TaiSan)); ModelState.Remove(nameof(input.SuKienDowntime));
        if (input.PhatHien > DateTime.Now.AddMinutes(5)) ModelState.AddModelError(nameof(input.PhatHien), "Thời điểm phát hiện ở tương lai.");
        if (!ModelState.IsValid) { await NapAsync(); return View(input); }
        input.TrangThai = TrangThaiSuCoAn.Moi; input.NguoiBao ??= User.Identity!.Name; input.NguoiPhuTrach ??= User.Identity!.Name;
        if (!string.IsNullOrWhiteSpace(maTaiSan)) input.TaiSanId = (await db.TaiSans.FirstOrDefaultAsync(t => t.MaTaiSan == maTaiSan.Trim().ToUpper()))?.Id;
        db.SuCoAnNinhs.Add(input);
        await db.LuuMoiAsync(soPhieu, "AN", so => input.Ma = so);
        TempData["ok"] = $"Đã ghi nhận sự cố {input.Ma}. Phân mức và xử lý theo các bước ứng phó.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        if (!Xem) return Forbid();
        var s = await db.SuCoAnNinhs.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiSan).Include(x => x.SuKienDowntime).Include(x => x.Buocs).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        ViewBag.Xu = Xu; ViewBag.Tep = await tep.DanhSachAsync("SuCoAnNinh", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.SuCoAnNinh && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.LyDoChuaDong = s.TrangThai == TrangThaiSuCoAn.DaDong ? new List<string>() : s.LyDoChuaDong();
        ViewBag.Gio = thamSo.SoNguyen(TS.JciHanBaoCaoSuCoGio);
        await NapAsync();
        return View(s);
    }

    private async Task<IActionResult> Thuc(int id, Func<SuCoAnNinh, Task<string>> thaoTac)
    {
        if (!Xu) return Forbid();
        var s = await db.SuCoAnNinhs.Include(x => x.Buocs).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        try { TempData["ok"] = await thaoTac(s); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>FR-AN-02, FR-AN-04: phân mức, cờ dữ liệu cá nhân; nghiêm trọng trở lên → thông báo Trưởng phòng CNTT và Lãnh đạo.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhanMuc(int id, MucDoAn mucDo, LoaiSuCoAn loai, string? lienQuan, int? heThongId, string? nguoiPhuTrach) => Thuc(id, async s =>
    {
        bool? lq = lienQuan switch { "co" => true, "khong" => false, _ => null };
        s.PhanMuc(mucDo, loai, lq, User.Identity!.Name!);
        s.HeThongId = heThongId ?? s.HeThongId; if (!string.IsNullOrWhiteSpace(nguoiPhuTrach)) s.NguoiPhuTrach = nguoiPhuTrach;
        if (s.NghiemTrongTroLen && !s.DaThongBaoLanhDao)
        {
            s.DaThongBaoLanhDao = true;
            var nhan = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            await tb.GuiAsync(nhan, LoaiThongBao.AnNinhMang, $"[{s.MucDo.Ten()}] Sự cố an ninh mạng {s.Ma}", "Mở hệ thống để xem chi tiết.", $"/AnNinh/Details/{s.Id}", MucDoThongBao.KhanCap, boQua: User.Identity!.Name);
            await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.AnNinhMang, $"[{s.MucDo.Ten()}] Sự cố an ninh mạng {s.Ma}", "Mở hệ thống để xem chi tiết.", $"/AnNinh/Details/{s.Id}", MucDoThongBao.KhanCap);
        }
        return "Đã phân mức sự cố." + (s.NghiemTrongTroLen ? " Đã thông báo Trưởng phòng CNTT và Lãnh đạo." : "");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GhiBuoc(int id, BuocUngPhoAn buoc, string noiDung, bool hoanThanh) => Thuc(id, s => { s.GhiBuoc(buoc, noiDung, hoanThanh, User.Identity!.Name!, DateTime.Now); return Task.FromResult($"Đã ghi bước \"{buoc.Ten()}\"."); });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GhiBaoCao(int id, string? coQuan, string? soVanBan, DateOnly? ngay, string? lyDoKhong) => Thuc(id, s =>
    {
        if (!string.IsNullOrWhiteSpace(lyDoKhong)) { s.LyDoKhongBaoCao = lyDoKhong.Trim(); return Task.FromResult("Đã ghi lý do không phải báo cáo cơ quan chức năng."); }
        s.GhiBaoCao(coQuan ?? "", soVanBan ?? "", ngay ?? DateOnly.FromDateTime(DateTime.Today));
        return Task.FromResult("Đã ghi nhận báo cáo cơ quan chức năng.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RutKinhNghiem(int id, string nguyenNhan, string baiHoc) => Thuc(id, s =>
    {
        if (string.IsNullOrWhiteSpace(nguyenNhan) || string.IsNullOrWhiteSpace(baiHoc)) throw new DomainException("Nhập nguyên nhân gốc và bài học / cải thiện.");
        s.NguyenNhanGoc = nguyenNhan.Trim(); s.BaiHoc = baiHoc.Trim();
        return Task.FromResult("Đã ghi nguyên nhân gốc và bài học.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Dong(int id) => Thuc(id, s => { s.Dong(User.Identity!.Name!, DateTime.Now); return Task.FromResult("Đã đóng sự cố."); });

    /// <summary>FR-AN-05: tạo hành động khắc phục từ sự cố (bắt buộc cho sự cố nghiêm trọng khi rút kinh nghiệm).</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoHanhDong(int id, string moTa, string nguoiChiuTrachNhiem)
    {
        if (!Xu) return Forbid();
        var s = await db.SuCoAnNinhs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        try { var h = await hanhDong.TaoAsync(NguonHanhDong.SuCoAnNinh, id, moTa, s.HeThongId, nguoiChiuTrachNhiem, null, LoaiHanhDong.KhacPhuc, s.NghiemTrongTroLen ? MucDoUuTien.Cao : MucDoUuTien.TrungBinh, User.Identity!.Name!); TempData["ok"] = $"Đã tạo hành động {h.Ma}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
