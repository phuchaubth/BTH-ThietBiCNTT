using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public class VanBanVm
{
    public int Id { get; set; }
    public HuongVanBan Huong { get; set; } = HuongVanBan.Di;
    [Display(Name = "Loại văn bản")] public LoaiVanBan Loai { get; set; } = LoaiVanBan.ToTrinh;
    [StringLength(60), Display(Name = "Số, ký hiệu")] public string? SoKyHieu { get; set; }
    [Display(Name = "Ngày văn bản")] public DateTime? NgayVanBan { get; set; }
    [Display(Name = "Ngày đến")] public DateTime? NgayDen { get; set; }
    [StringLength(300), Display(Name = "Cơ quan ban hành / nơi gửi")] public string? CoQuanBanHanh { get; set; }
    [StringLength(500), Display(Name = "Nơi nhận")] public string? NoiNhan { get; set; }
    [Required(ErrorMessage = "Nhập trích yếu."), StringLength(500), Display(Name = "Trích yếu")] public string TrichYeu { get; set; } = "";
    [Display(Name = "Nội dung")] public string? NoiDung { get; set; }
    [Display(Name = "Độ khẩn")] public DoKhanVanBan DoKhan { get; set; } = DoKhanVanBan.Thuong;
    [StringLength(100), Display(Name = "Người ký")] public string? NguoiKy { get; set; }
    [StringLength(100), Display(Name = "Chức vụ người ký")] public string? ChucVuNguoiKy { get; set; }
    [Display(Name = "Hạn xử lý / hạn trình")] public DateTime? HanXuLy { get; set; }
    [Display(Name = "Theo dõi thực hiện sau khi phát hành")] public bool TheoDoiThucHien { get; set; }
    public int? VanBanLienQuanId { get; set; }
    public int? DeXuatMuaSamId { get; set; }
}

/// <summary>Văn thư Phòng CNTT: sổ văn bản đến/đi, soạn & trình duyệt tờ trình, phân công, theo dõi tiến độ.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class VanThuController(AppDbContext db, VanThuService vt, TepService tep, ThamSoService thamSo) : Controller
{
    private string Toi => User.Identity!.Name!;
    private bool QuanTri => User.IsInRole(VaiTro.QuanTri);
    private bool LaVanThu => vt.LaVanThu(Toi, QuanTri);
    private bool LaPhanCong => vt.LaPhanCong(Toi, QuanTri);

    private static readonly TrangThaiVanBan[] Mo =
        [TrangThaiVanBan.DuThao, TrangThaiVanBan.ChoDuyet, TrangThaiVanBan.TraLai, TrangThaiVanBan.DaDuyet, TrangThaiVanBan.ChoPhanCong, TrangThaiVanBan.DangXuLy];

    /// <summary>Việc của tôi: đang phải duyệt, xử lý, sửa, cấp số hoặc phân công.</summary>
    private bool CuaToi(VanBan x) => x.TrangThai switch
    {
        TrangThaiVanBan.ChoDuyet => string.Equals(x.BuocDangCho?.NguoiDuyet, Toi, StringComparison.OrdinalIgnoreCase),
        TrangThaiVanBan.DuThao or TrangThaiVanBan.TraLai => string.Equals(x.NguoiSoan, Toi, StringComparison.OrdinalIgnoreCase),
        TrangThaiVanBan.DaDuyet => LaVanThu,
        TrangThaiVanBan.ChoPhanCong => LaPhanCong,
        TrangThaiVanBan.DangXuLy => x.LaNguoiXuLy(Toi),
        _ => false
    };

    public async Task<IActionResult> Index(HuongVanBan? huong, TrangThaiVanBan? trangThai, LoaiVanBan? loai, string? q, int? nam,
        bool cuaToi = false, bool quaHan = false, string? nguoi = null, int page = 1)
    {
        var now = DateTime.Now;
        var qr = db.VanBans.AsNoTracking().Include(x => x.BuocDuyets).AsQueryable();
        if (huong is not null) qr = qr.Where(x => x.Huong == huong);
        if (trangThai is not null) qr = qr.Where(x => x.TrangThai == trangThai);
        if (loai is not null) qr = qr.Where(x => x.Loai == loai);
        if (nam is not null) qr = qr.Where(x => x.Nam == nam);
        if (!string.IsNullOrWhiteSpace(nguoi)) qr = qr.Where(x => x.NguoiChuTri == nguoi || x.NguoiSoan == nguoi || (x.NguoiPhoiHop != null && x.NguoiPhoiHop.Contains(nguoi)));
        if (!string.IsNullOrWhiteSpace(q))
        {
            var k = q.Trim();
            var so = int.TryParse(k, out var n) ? n : -1;
            qr = qr.Where(x => x.SoDen == so || x.SoDi == so || x.TrichYeu.Contains(k) || (x.SoKyHieu != null && x.SoKyHieu.Contains(k)) || (x.CoQuanBanHanh != null && x.CoQuanBanHanh.Contains(k))
                || (x.NoiNhan != null && x.NoiNhan.Contains(k)) || (x.NoiDung != null && x.NoiDung.Contains(k)) || (x.KetQua != null && x.KetQua.Contains(k)));
        }
        if (quaHan || cuaToi) qr = qr.Where(x => Mo.Contains(x.TrangThai));
        if (quaHan) qr = qr.Where(x => x.HanXuLy < now.Date);
        var ds = await qr.OrderByDescending(x => x.Id).ToListAsync();
        if (cuaToi) ds = ds.Where(CuaToi).ToList();
        ViewBag.Loc = new { huong, trangThai, loai, q, nam, cuaToi, quaHan, nguoi };
        ViewBag.Dem = new
        {
            CuaToi = (await db.VanBans.AsNoTracking().Include(x => x.BuocDuyets).Where(x => Mo.Contains(x.TrangThai)).ToListAsync()).Count(CuaToi),
            QuaHan = await db.VanBans.CountAsync(x => Mo.Contains(x.TrangThai) && x.HanXuLy < now.Date)
        };
        ViewBag.Tong = ds.Count; ViewBag.Page = page;
        ViewBag.LaVanThu = LaVanThu;
        return View(ds.Skip((page - 1) * 50).Take(50).ToList());
    }

    private async Task<VanBan?> TaiAsync(int id) => await db.VanBans.Include(x => x.BuocDuyets).Include(x => x.NhatKys)
        .Include(x => x.VanBanLienQuan).Include(x => x.DeXuatMuaSam).FirstOrDefaultAsync(x => x.Id == id);

    public async Task<IActionResult> ChiTiet(int id)
    {
        var vb = await TaiAsync(id);
        if (vb is null) return NotFound();
        ViewBag.Teps = await tep.DanhSachAsync("VanBan", id.ToString());
        ViewBag.TraLoi = await db.VanBans.AsNoTracking().Where(x => x.VanBanLienQuanId == id).ToListAsync();
        ViewBag.LaVanThu = LaVanThu; ViewBag.LaPhanCong = LaPhanCong; ViewBag.Toi = Toi; ViewBag.QuanTri = QuanTri;
        var canBo = await vt.CanBoAsync();
        ViewBag.CanBo = canBo.Select(u => new SelectListItem($"{u.HoTen} ({u.UserName})", u.UserName)).ToList();
        ViewBag.NguoiDuyetMacDinh = vt.NguoiDuyetMacDinh;
        ViewBag.HanGoiY = DateTime.Today.AddDays(vt.HanGoiY(vb.DoKhan));
        ViewBag.VanBanDi = vb.Huong == HuongVanBan.Den
            ? await db.VanBans.AsNoTracking().Where(x => x.Huong == HuongVanBan.Di && x.SoKyHieu != null).OrderByDescending(x => x.Id).Take(100)
                .Select(x => new SelectListItem(x.SoKyHieu + " – " + x.TrichYeu, x.Id.ToString())).ToListAsync()
            : [];
        return View(vb);
    }

    // ===== Vào sổ văn bản đến =====
    public IActionResult DenMoi()
    {
        if (!LaVanThu) return Forbid();
        return View("Form", new VanBanVm { Huong = HuongVanBan.Den, Loai = LoaiVanBan.CongVan, NgayDen = DateTime.Now });
    }

    // ===== Soạn văn bản đi / tờ trình =====
    public async Task<IActionResult> SoanMoi(int? mauId, int? deXuatId, int? traLoiId, LoaiVanBan loai = LoaiVanBan.ToTrinh)
    {
        var vm = new VanBanVm { Huong = HuongVanBan.Di, Loai = loai, TheoDoiThucHien = loai == LoaiVanBan.ToTrinh, VanBanLienQuanId = traLoiId, DeXuatMuaSamId = deXuatId };
        DeXuatMuaSam? dx = null;
        if (deXuatId is { } d)
        {
            dx = await db.DeXuatMuaSams.AsNoTracking().Include(x => x.Dongs).ThenInclude(x => x.VatTu).Include(x => x.Dongs).ThenInclude(x => x.LoaiThietBi).FirstOrDefaultAsync(x => x.Id == d);
            if (dx is null) return NotFound();
            vm.TrichYeu = $"V/v đề nghị mua sắm: {dx.TieuDe}";
            mauId ??= await db.MauVanBans.Where(m => m.DangDung && m.Loai == LoaiVanBan.ToTrinh && m.NoiDung.Contains("{{DE_XUAT}}")).Select(m => (int?)m.Id).FirstOrDefaultAsync();
        }
        if (traLoiId is { } t && await db.VanBans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == t && x.Huong == HuongVanBan.Den) is { } den)
        {
            vm.Loai = LoaiVanBan.CongVan; vm.TheoDoiThucHien = false;
            vm.TrichYeu = $"V/v trả lời văn bản số {den.SoKyHieu} ngày {den.NgayVanBan:dd/MM/yyyy} của {den.CoQuanBanHanh}";
            vm.NoiNhan = den.CoQuanBanHanh;
        }
        if (mauId is { } m && await db.MauVanBans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == m) is { } mau)
        {
            vm.Loai = mau.Loai; vm.TheoDoiThucHien = mau.Loai == LoaiVanBan.ToTrinh;
            if (string.IsNullOrWhiteSpace(vm.TrichYeu)) vm.TrichYeu = mau.TrichYeu ?? "";
            vm.NoiDung = vt.DienMau(mau.NoiDung, vm.TrichYeu, User.HoTen(), dx, DateTime.Now);
        }
        ViewBag.Maus = await db.MauVanBans.AsNoTracking().Where(x => x.DangDung).OrderBy(x => x.Ten).ToListAsync();
        return View("Form", vm);
    }

    public async Task<IActionResult> Sua(int id)
    {
        var vb = await db.VanBans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (vb is null) return NotFound();
        if (!DuocSua(vb)) return Forbid();
        return View("Form", new VanBanVm
        {
            Id = vb.Id, Huong = vb.Huong, Loai = vb.Loai, SoKyHieu = vb.SoKyHieu, NgayVanBan = vb.NgayVanBan, NgayDen = vb.NgayDen, CoQuanBanHanh = vb.CoQuanBanHanh,
            NoiNhan = vb.NoiNhan, TrichYeu = vb.TrichYeu, NoiDung = vb.NoiDung, DoKhan = vb.DoKhan, NguoiKy = vb.NguoiKy, ChucVuNguoiKy = vb.ChucVuNguoiKy,
            HanXuLy = vb.HanXuLy, TheoDoiThucHien = vb.TheoDoiThucHien, VanBanLienQuanId = vb.VanBanLienQuanId, DeXuatMuaSamId = vb.DeXuatMuaSamId
        });
    }

    private bool DuocSua(VanBan vb) => vb.DuocSua && (vb.Huong == HuongVanBan.Den ? LaVanThu
        : QuanTri || string.Equals(vb.NguoiSoan, Toi, StringComparison.OrdinalIgnoreCase));

    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(60_000_000)]
    public async Task<IActionResult> Luu(VanBanVm vm, List<IFormFile>? files, bool trinhNgay = false)
    {
        VanBan vb;
        if (vm.Id == 0)
        {
            if (vm.Huong == HuongVanBan.Den && !LaVanThu) return Forbid();
            vb = new VanBan { Huong = vm.Huong };
        }
        else
        {
            vb = (await TaiAsync(vm.Id))!;
            if (vb is null) return NotFound();
            if (!DuocSua(vb)) return Forbid();
        }
        if (vm.Huong == HuongVanBan.Den && string.IsNullOrWhiteSpace(vm.CoQuanBanHanh)) ModelState.AddModelError(nameof(vm.CoQuanBanHanh), "Nhập cơ quan ban hành / nơi gửi.");
        if (vm.HanXuLy is { } h && h.Date < DateTime.Today && vm.Id == 0) ModelState.AddModelError(nameof(vm.HanXuLy), "Hạn không được trước hôm nay.");
        if (vm.VanBanLienQuanId is { } lq && !await db.VanBans.AnyAsync(x => x.Id == lq && x.Id != vm.Id)) vm.VanBanLienQuanId = null;
        if (!ModelState.IsValid)
        {
            ViewBag.Maus = await db.MauVanBans.AsNoTracking().Where(x => x.DangDung).OrderBy(x => x.Ten).ToListAsync();
            return View("Form", vm);
        }
        vb.Loai = vm.Loai; vb.TrichYeu = vm.TrichYeu.Trim(); vb.DoKhan = vm.DoKhan; vb.HanXuLy = vm.HanXuLy?.Date; vb.VanBanLienQuanId = vm.VanBanLienQuanId;
        vb.NguoiKy = vm.NguoiKy; vb.ChucVuNguoiKy = vm.ChucVuNguoiKy;
        if (vb.Huong == HuongVanBan.Den)
        {
            vb.SoKyHieu = vm.SoKyHieu?.Trim(); vb.NgayVanBan = vm.NgayVanBan?.Date; vb.CoQuanBanHanh = vm.CoQuanBanHanh?.Trim(); vb.NoiDung = vm.NoiDung;
            if (vm.Id == 0) vb.NgayDen = vm.NgayDen ?? DateTime.Now;
        }
        else
        {
            vb.NoiNhan = vm.NoiNhan?.Trim(); vb.NoiDung = vm.NoiDung; vb.TheoDoiThucHien = vm.TheoDoiThucHien; vb.DeXuatMuaSamId = vm.DeXuatMuaSamId;
        }
        try
        {
            if (vm.Id == 0)
            {
                if (vb.Huong == HuongVanBan.Den) await vt.VaoSoDenAsync(vb, Toi);
                else await vt.TaoDuThaoAsync(vb, Toi);
            }
            else
            {
                vb.GhiChu("Sửa thông tin văn bản", Toi, DateTime.Now);
                await db.SaveChangesAsync();
            }
            var soTep = 0;
            foreach (var f in (files ?? []).Take(10))
            {
                await using var s = f.OpenReadStream();
                await tep.LuuAsync("VanBan", vb.Id.ToString(), f.FileName, s, f.Length, Toi);
                soTep++;
            }
            if (soTep > 0) await db.SaveChangesAsync();
            TempData["ok"] = vb.Huong == HuongVanBan.Den && vm.Id == 0 ? $"Đã vào sổ văn bản đến số {vb.SoDen}/{vb.Nam}." : "Đã lưu văn bản.";
            if (trinhNgay && vb.Huong == HuongVanBan.Di) return await TrinhDuyet(vb.Id, vt.NguoiDuyetMacDinh.ToArray());
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; if (vb.Id == 0) { ViewBag.Maus = await db.MauVanBans.AsNoTracking().ToListAsync(); return View("Form", vm); } }
        return RedirectToAction(nameof(ChiTiet), new { id = vb.Id });
    }

    private async Task<IActionResult> ThucHien(int id, Func<VanBan, Task> viec, string ok)
    {
        var vb = await TaiAsync(id);
        if (vb is null) return NotFound();
        try { await viec(vb); TempData["ok"] = ok; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        catch (UnauthorizedAccessException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    private void Can(bool dk, string loi) { if (!dk) throw new UnauthorizedAccessException(loi); }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TrinhDuyet(int id, string[] nguoiDuyet) => ThucHien(id, async vb =>
    {
        Can(QuanTri || string.Equals(vb.NguoiSoan, Toi, StringComparison.OrdinalIgnoreCase), "Chỉ người soạn được trình văn bản.");
        await vt.TrinhDuyetAsync(vb, nguoiDuyet.SelectMany(x => (x ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)).ToList(), Toi);
    }, "Đã trình duyệt.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Duyet(int id, bool dongY, string? yKien) =>
        ThucHien(id, vb => vt.DuyetAsync(vb, Toi, dongY, yKien), dongY ? "Đã duyệt." : "Đã trả lại cho người soạn.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhatHanh(int id, DateTime? ngay, string? nguoiKy, string? chucVu) => ThucHien(id, async vb =>
    {
        Can(LaVanThu, "Chỉ văn thư được cấp số, phát hành.");
        await vt.PhatHanhAsync(vb, ngay ?? DateTime.Today, nguoiKy, chucVu, Toi);
    }, "Đã cấp số và phát hành.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> PhanCong(int id, string chuTri, string[]? phoiHop, DateTime? han, string? yKien) => ThucHien(id, async vb =>
    {
        Can(LaPhanCong || (vb.Huong == HuongVanBan.Di && string.Equals(vb.NguoiSoan, Toi, StringComparison.OrdinalIgnoreCase)), "Bạn không có quyền phân công văn bản này.");
        await vt.PhanCongAsync(vb, chuTri, string.Join(",", phoiHop ?? []), han, yKien, Toi);
    }, "Đã phân công xử lý.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TienDo(int id, int tienDo, string noiDung) => ThucHien(id, async vb =>
    {
        Can(vb.LaNguoiXuLy(Toi) || LaPhanCong, "Chỉ người chủ trì/phối hợp cập nhật tiến độ.");
        await vt.CapNhatTienDoAsync(vb, tienDo, noiDung, Toi);
    }, "Đã cập nhật tiến độ.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> HoanThanh(int id, string ketQua, int? vanBanTraLoiId) => ThucHien(id, async vb =>
    {
        Can(string.Equals(vb.NguoiChuTri, Toi, StringComparison.OrdinalIgnoreCase) || LaPhanCong, "Chỉ người chủ trì (hoặc người phân công) kết thúc xử lý.");
        await vt.HoanThanhAsync(vb, ketQua, vanBanTraLoiId, Toi);
    }, "Đã hoàn thành xử lý văn bản.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GhiChu(int id, string noiDung) => ThucHien(id, async vb =>
    {
        vb.GhiChu(noiDung, Toi, DateTime.Now);
        await db.SaveChangesAsync();
    }, "Đã ghi nhận.");

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Huy(int id, string lyDo) => ThucHien(id, async vb =>
    {
        Can(QuanTri || (vb.Huong == HuongVanBan.Den ? LaVanThu : string.Equals(vb.NguoiSoan, Toi, StringComparison.OrdinalIgnoreCase)), "Bạn không có quyền huỷ văn bản này.");
        vb.Huy(lyDo, Toi, DateTime.Now);
        await db.SaveChangesAsync();
    }, "Đã huỷ văn bản.");

    /// <summary>Bản in theo thể thức Nghị định 30/2020/NĐ-CP (in trình duyệt / lưu PDF).</summary>
    public async Task<IActionResult> In(int id)
    {
        var vb = await db.VanBans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.Huong == HuongVanBan.Di);
        if (vb is null) return NotFound();
        ViewBag.CoQuan = thamSo.Chuoi(TS.TenBenhVien); ViewBag.DonVi = thamSo.Chuoi(TS.TenPhongCNTT); ViewBag.DiaDanh = thamSo.Chuoi(TS.VanThuDiaDanh);
        return View(vb);
    }

    // ===== Sổ văn bản =====
    public async Task<IActionResult> So(HuongVanBan huong = HuongVanBan.Den, int? nam = null, bool xuat = false)
    {
        var n = nam ?? DateTime.Today.Year;
        var ds = await db.VanBans.AsNoTracking().Where(x => x.Huong == huong && x.Nam == n && (huong == HuongVanBan.Den ? x.SoDen != null : x.SoDi != null))
            .OrderBy(x => huong == HuongVanBan.Den ? x.SoDen : x.SoDi).ToListAsync();
        if (xuat)
            return File(BaoCaoExcel.SoVanBan(ds, huong, n, thamSo.Chuoi(TS.TenPhongCNTT)), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"So{(huong == HuongVanBan.Den ? "Den" : "Di")}_{n}.xlsx");
        ViewBag.Huong = huong; ViewBag.Nam = n;
        ViewBag.CacNam = await db.VanBans.Select(x => x.Nam).Distinct().OrderByDescending(x => x).ToListAsync();
        return View(ds);
    }

    // ===== Mẫu văn bản =====
    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Mau() => View(await db.MauVanBans.AsNoTracking().OrderBy(x => x.Loai).ThenBy(x => x.Ten).ToListAsync());

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> MauForm(int? id)
    {
        if (id is null) return View(new MauVanBan());
        var m = await db.MauVanBans.FindAsync(id);
        return m is null ? NotFound() : View(m);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> LuuMau(MauVanBan m)
    {
        if (!ModelState.IsValid) return View("MauForm", m);
        if (m.Id == 0) db.MauVanBans.Add(m);
        else
        {
            var cu = await db.MauVanBans.FindAsync(m.Id);
            if (cu is null) return NotFound();
            cu.Ten = m.Ten; cu.Loai = m.Loai; cu.TrichYeu = m.TrichYeu; cu.NoiDung = m.NoiDung; cu.DangDung = m.DangDung;
        }
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu mẫu văn bản.";
        return RedirectToAction(nameof(Mau));
    }
}
