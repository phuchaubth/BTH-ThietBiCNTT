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

public class SuKienVm
{
    public LoaiDowntime Loai { get; set; } = LoaiDowntime.DotXuat;
    public MucDoDowntime MucDo { get; set; } = MucDoDowntime.NgungHoanToan;
    public string MoTa { get; set; } = "";
    public DateTime? BatDau { get; set; }
    public DateTime? DuKienBatDau { get; set; }
    public DateTime? DuKienKetThuc { get; set; }
    public bool KichHoatKeHoach { get; set; } = true;
    public bool AnhHuongAnToan { get; set; }
    public List<int> HeThongIds { get; set; } = [];
    public int? PhieuSuaChuaId { get; set; }
    /// <summary>Sự cố an ninh mạng gây ra downtime (FR-AN-05).</summary>
    public int? SuCoAnNinhId { get; set; }
}

/// <summary>Phân hệ 4.2 – Sự kiện downtime (HCT.01.04): FR-DT-04..08.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao + "," + VaiTro.TruongKhoa)]
public class SuKienDowntimeController(AppDbContext db, HaTangService haTang, SoPhieuService soPhieu, ThongBaoService tb, ThamSoService thamSo, TepService tep) : Controller
{
    private IQueryable<SuKienDowntime> PhamVi()
    {
        var q = db.SuKienDowntimes.AsQueryable();
        if (User.XemToanVien()) return q;
        var k = User.KhoaPhongId() ?? -1;
        return q.Where(s => s.Khoas.Any(x => x.KhoaPhongId == k));
    }

    public async Task<IActionResult> Index(TrangThaiSuKien? trangThai, LoaiDowntime? loai, int? heThong, bool dangMo = false)
    {
        var q = PhamVi().AsNoTracking().Include(s => s.HeThongs).ThenInclude(h => h.HeThong).Include(s => s.Khoas).AsQueryable();
        if (trangThai is not null) q = q.Where(s => s.TrangThai == trangThai);
        if (dangMo) q = q.Where(s => s.TrangThai == TrangThaiSuKien.DangDienRa || s.TrangThai == TrangThaiSuKien.DaKhoiPhuc || s.TrangThai == TrangThaiSuKien.DaLenLich);
        if (loai is not null) q = q.Where(s => s.Loai == loai);
        if (heThong is not null) q = q.Where(s => s.HeThongs.Any(h => h.HeThongId == heThong));
        var ds = (await q.ToListAsync()).OrderBy(s => s.TrangThai == TrangThaiSuKien.DangDienRa ? 0 : s.TrangThai == TrangThaiSuKien.DaKhoiPhuc ? 1 : s.TrangThai == TrangThaiSuKien.DaLenLich ? 2 : 3)
            .ThenByDescending(s => s.BatDau ?? s.DuKienBatDau).Take(300).ToList();
        ViewData["trangThai"] = trangThai; ViewData["loai"] = loai; ViewData["heThong"] = heThong; ViewData["dangMo"] = dangMo;
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten", heThong);
        return View(ds);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(LoaiDowntime loai = LoaiDowntime.DotXuat, int? phieuId = null, int? heThongId = null, int? suCoId = null)
    {
        var vm = new SuKienVm { Loai = loai, PhieuSuaChuaId = phieuId, SuCoAnNinhId = suCoId };
        if (suCoId is { } sid && await db.SuCoAnNinhs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == sid) is { } sc) { vm.MoTa = $"Từ sự cố an ninh mạng {sc.Ma}: {sc.MoTa}"; vm.AnhHuongAnToan = sc.NghiemTrongTroLen; heThongId ??= sc.HeThongId; }
        if (loai == LoaiDowntime.DotXuat) vm.BatDau = DateTime.Now; else { vm.DuKienBatDau = DateTime.Today.AddDays(3).AddHours(22); vm.DuKienKetThuc = vm.DuKienBatDau.Value.AddHours(2); }
        if (heThongId is not null) vm.HeThongIds.Add(heThongId.Value);
        if (phieuId is { } pid)
        {
            var p = await db.PhieuSuaChuas.AsNoTracking().Include(x => x.TaiSan).FirstOrDefaultAsync(x => x.Id == pid);
            if (p is null) return NotFound();
            vm.MoTa = $"Từ phiếu {p.SoPhieu}: {p.MoTaSuCo}";
            vm.HeThongIds = (await haTang.AnhHuongCuaTaiSanAsync(p.TaiSanId)).HeThongs.Select(h => h.Id).ToList();
            ViewBag.Phieu = p;
        }
        await NapAsync(vm);
        return View(vm);
    }

    private async Task NapAsync(SuKienVm vm)
    {
        ViewBag.HeThongs = await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung).OrderBy(h => h.MucTrongYeu).ThenBy(h => h.Ma).ToListAsync();
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Create(SuKienVm vm)
    {
        var now = DateTime.Now;
        if (string.IsNullOrWhiteSpace(vm.MoTa)) ModelState.AddModelError(nameof(vm.MoTa), "Nhập mô tả / lý do.");
        if (vm.HeThongIds.Count == 0) ModelState.AddModelError(nameof(vm.HeThongIds), "Chọn ít nhất một hệ thống bị ảnh hưởng.");
        if (vm.Loai == LoaiDowntime.DotXuat)
        {
            if (vm.BatDau is null) ModelState.AddModelError(nameof(vm.BatDau), "Nhập thời điểm bắt đầu.");
            else if (vm.BatDau > now.AddMinutes(5)) ModelState.AddModelError(nameof(vm.BatDau), "Thời điểm bắt đầu ở tương lai.");
        }
        else
        {
            if (vm.DuKienBatDau is null || vm.DuKienKetThuc is null) ModelState.AddModelError(nameof(vm.DuKienBatDau), "Nhập khung giờ dự kiến.");
            else if (vm.DuKienKetThuc <= vm.DuKienBatDau) ModelState.AddModelError(nameof(vm.DuKienKetThuc), "Giờ kết thúc phải sau giờ bắt đầu.");
            else if (vm.DuKienBatDau < now) ModelState.AddModelError(nameof(vm.DuKienBatDau), "Downtime có kế hoạch phải đăng ký trước.");
        }
        if (!ModelState.IsValid)
        {
            await NapAsync(vm);
            if (vm.PhieuSuaChuaId is { } pid) ViewBag.Phieu = await db.PhieuSuaChuas.AsNoTracking().FirstOrDefaultAsync(x => x.Id == pid);
            return View(vm);
        }
        var s = new SuKienDowntime
        {
            Loai = vm.Loai, MucDo = vm.MucDo, MoTa = vm.MoTa.Trim(), KichHoatKeHoach = vm.KichHoatKeHoach && vm.Loai == LoaiDowntime.DotXuat,
            AnhHuongAnToan = vm.AnhHuongAnToan, PhieuSuaChuaId = vm.PhieuSuaChuaId,
            TrangThai = vm.Loai == LoaiDowntime.DotXuat ? TrangThaiSuKien.DangDienRa : TrangThaiSuKien.DaLenLich,
            BatDau = vm.Loai == LoaiDowntime.DotXuat ? vm.BatDau : null, DuKienBatDau = vm.DuKienBatDau, DuKienKetThuc = vm.DuKienKetThuc
        };
        try { await haTang.GanHeThongAsync(s, vm.HeThongIds); }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); await NapAsync(vm); return View(vm); }
        s.NhatKys.Add(new NhatKyDowntime
        {
            ThoiDiem = s.BatDau ?? now, NguoiGhi = User.Identity!.Name,
            NoiDung = vm.Loai == LoaiDowntime.DotXuat ? "Phát hiện / ghi nhận downtime đột xuất." + (s.KichHoatKeHoach ? " Kích hoạt kế hoạch downtime." : "")
                                                      : $"Đăng ký downtime có kế hoạch {s.DuKienBatDau:HH:mm dd/MM} – {s.DuKienKetThuc:HH:mm dd/MM}."
        });
        db.SuKienDowntimes.Add(s);
        await db.LuuMoiAsync(soPhieu, "DT", so => s.Ma = so);
        if (vm.SuCoAnNinhId is { } scId && await db.SuCoAnNinhs.FirstOrDefaultAsync(x => x.Id == scId) is { } suCo) suCo.SuKienDowntimeId = s.Id;
        if (s.PhieuSuaChuaId is { } pId)
            db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = pId, ThoiGian = now, NguoiViet = User.Identity!.Name!, NoiDung = $"[Downtime] Đã mở sự kiện {s.Ma} cho hệ thống bị ảnh hưởng." });
        // Thông báo
        var ten = string.Join(", ", await db.HeThongThongTins.Where(h => vm.HeThongIds.Contains(h.Id)).Select(h => h.Ten).ToListAsync());
        var nhan = new List<string?>(await tb.CanBoCNTTAsync());
        foreach (var k in s.Khoas) nhan.AddRange(await tb.TruongKhoaAsync(k.KhoaPhongId));
        if (s.Loai == LoaiDowntime.DotXuat)
        {
            var nghiem = s.MucTrongYeuCaoNhat is MucTrongYeu.ToiQuanTrong or MucTrongYeu.Cao;
            if (nghiem) nhan.AddRange((await tb.CanBoCNTTAsync()));
            await tb.GuiAsync(nhan, LoaiThongBao.Downtime, $"[Downtime] {ten} đang ngừng ({s.Ma})", $"{s.MoTa}\nKích hoạt quy trình giấy tại các khoa theo bộ downtime.",
                $"/SuKienDowntime/Details/{s.Id}", nghiem ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao, guiNhomCNTT: true, boQua: User.Identity!.Name);
            if (nghiem) await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.Downtime, $"[Downtime] {ten} đang ngừng ({s.Ma})", s.MoTa, $"/SuKienDowntime/Details/{s.Id}", MucDoThongBao.KhanCap);
        }
        else
            await tb.GuiAsync(nhan, LoaiThongBao.Downtime, $"Lịch downtime {ten}: {s.DuKienBatDau:HH:mm dd/MM} – {s.DuKienKetThuc:HH:mm dd/MM}", s.MoTa, $"/SuKienDowntime/Details/{s.Id}", boQua: User.Identity!.Name);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã ghi nhận sự kiện {s.Ma}.";
        return RedirectToAction(nameof(Details), new { id = s.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var s = await PhamVi().AsNoTracking().Include(x => x.HeThongs).ThenInclude(h => h.HeThong).Include(x => x.Khoas).ThenInclude(k => k.KhoaPhong)
            .Include(x => x.NhatKys).Include(x => x.PhieuSuaChua).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("SuKienDowntime", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.SuKienDowntime && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        var ids = s.HeThongs.Select(h => h.HeThongId).ToList();
        ViewBag.KeHoachs = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc && ids.Contains(k.HeThongId)).ToListAsync();
        ViewBag.LyDoChuaDong = s.TrangThai == TrangThaiSuKien.DaKhoiPhuc ? s.LyDoChuaDong(DateTime.Now) : [];
        ViewBag.Now = DateTime.Now;
        return View(s);
    }

    private async Task<IActionResult> ThucHien(int id, Func<SuKienDowntime, string> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var s = await db.SuKienDowntimes.Include(x => x.NhatKys).Include(x => x.HeThongs).Include(x => x.Khoas).FirstOrDefaultAsync(x => x.Id == id);
        if (s is null) return NotFound();
        try { TempData["ok"] = thaoTac(s); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Ghi(int id, string noiDung, DateTime? thoiDiem) => ThucHien(id, s => { s.Ghi(noiDung, User.Identity!.Name!, thoiDiem ?? DateTime.Now); return "Đã ghi nhật ký."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BatDauThucTe(int id, DateTime? thoiDiem) => ThucHien(id, s => { s.BatDauThucTe(thoiDiem ?? DateTime.Now, User.Identity!.Name!); return "Đã ghi nhận bắt đầu downtime."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> KhoiPhuc(int id, DateTime? thoiDiem) => ThucHien(id, s =>
    {
        s.KhoiPhuc(thoiDiem ?? DateTime.Now, User.Identity!.Name!);
        var t = s.ThoiLuong(DateTime.Now);
        return $"Đã ghi nhận khôi phục sau {HaTangHelpers.ThoiLuongText(t)}." + (s.VuotRto(DateTime.Now) ? $" Vượt RTO ({s.RtoPhut} phút) — bắt buộc rút kinh nghiệm." : "");
    });

    /// <summary>FR-DT-07: khoa chuyển sang / không dùng quy trình giấy (trước khi khôi phục) — quyết định của CNTT cùng khoa.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> QuyTrinhGiay(int id, int khoaId, bool kichHoat) => ThucHien(id, s =>
    {
        var k = s.Khoas.FirstOrDefault(x => x.KhoaPhongId == khoaId) ?? throw new DomainException("Khoa không thuộc sự kiện.");
        if (s.TrangThai is TrangThaiSuKien.DaDong or TrangThaiSuKien.DaHuy) throw new DomainException("Sự kiện đã đóng.");
        k.KichHoatGiay = kichHoat;
        if (!kichHoat) { k.DaNhapBu = false; k.NguoiXacNhan = null; k.ThoiDiemXacNhan = null; }
        s.Ghi(kichHoat ? $"Khoa (Id {khoaId}) chuyển sang quy trình giấy." : $"Khoa (Id {khoaId}) không dùng quy trình giấy.", User.Identity!.Name!, DateTime.Now);
        return "Đã cập nhật.";
    });

    /// <summary>FR-DT-07: khoa xác nhận đã nhập bù — CNTT hoặc Trưởng khoa của khoa đó.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> NhapBu(int id, int khoaId, string? ghiChu) => ThucHien(id, s =>
    {
        if (!User.LaCNTT() && (!User.IsInRole(VaiTro.TruongKhoa) || User.KhoaPhongId() != khoaId)) throw new DomainException("Chỉ Trưởng khoa đó hoặc Phòng CNTT xác nhận nhập bù.");
        var k = s.Khoas.FirstOrDefault(x => x.KhoaPhongId == khoaId) ?? throw new DomainException("Khoa không thuộc sự kiện.");
        if (s.TrangThai != TrangThaiSuKien.DaKhoiPhuc && s.TrangThai != TrangThaiSuKien.DangDienRa) throw new DomainException("Chỉ xác nhận nhập bù khi sự kiện đang diễn ra hoặc đã khôi phục.");
        if (s.KetThuc is null) throw new DomainException("Chỉ xác nhận nhập bù sau khi hệ thống đã khôi phục.");
        k.DaNhapBu = true; k.NguoiXacNhan = User.Identity!.Name; k.ThoiDiemXacNhan = DateTime.Now; k.GhiChu = ghiChu;
        s.NhatKys.Add(new NhatKyDowntime { ThoiDiem = DateTime.Now, NguoiGhi = User.Identity!.Name, NoiDung = $"Khoa (Id {khoaId}) xác nhận đã nhập bù dữ liệu." });
        return "Đã xác nhận nhập bù.";
    }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> RutKinhNghiem(int id, string nguyenNhan, string? diemTot, string canCaiThien, bool canSuaKeHoach) => ThucHien(id, s =>
    {
        s.RutKinhNghiem(nguyenNhan, diemTot, canCaiThien, canSuaKeHoach, User.Identity!.Name!, DateTime.Now);
        if (canSuaKeHoach)
        {
            var ids = s.HeThongs.Select(h => h.HeThongId).ToList();
            foreach (var k in db.KeHoachDowntimes.Where(k => ids.Contains(k.HeThongId) && k.TrangThai == TrangThaiKeHoach.HieuLuc).ToList())
                k.DanhDauCanRaSoat($"Sau sự kiện {s.Ma}: {canCaiThien.Trim()}");
        }
        return "Đã ghi rút kinh nghiệm." + (canSuaKeHoach ? " Kế hoạch downtime sẽ được nhắc rà soát." : "");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Dong(int id) => ThucHien(id, s => { s.Dong(User.Identity!.Name!, DateTime.Now); return "Đã đóng sự kiện."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Huy(int id) => ThucHien(id, s => { s.Huy(User.Identity!.Name!, DateTime.Now); return "Đã huỷ lịch downtime."; });
}
