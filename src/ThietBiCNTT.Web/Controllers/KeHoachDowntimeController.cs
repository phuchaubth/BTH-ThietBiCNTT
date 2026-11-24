using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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

/// <summary>Bộ downtime của một khoa: các hệ thống khoa phụ thuộc kèm kế hoạch hiệu lực (FR-DT-03).</summary>
public record BoKhoaVm(KhoaPhong Khoa, List<(HeThongThongTin HeThong, bool BatBuoc, KeHoachDowntime? KeHoach)> Muc);

/// <summary>Phân hệ 4.2 – Kế hoạch downtime (HCT.01.04: xây dựng, duy trì). FR-DT-01..03, FR-DT-13.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao + "," + VaiTro.TruongKhoa + "," + VaiTro.NhanVien)]
public class KeHoachDowntimeController(AppDbContext db, ThamSoService thamSo, ThongBaoService tb, TepService tep, UserManager<AppUser> users) : Controller
{
    private static bool CanLanhDao(HeThongThongTin h) => h.MucTrongYeu == MucTrongYeu.ToiQuanTrong;

    [Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public async Task<IActionResult> Index(TrangThaiKeHoach? trangThai)
    {
        var q = db.KeHoachDowntimes.AsNoTracking().Include(k => k.HeThong).AsQueryable();
        if (trangThai is not null) q = q.Where(k => k.TrangThai == trangThai);
        else q = q.Where(k => k.TrangThai != TrangThaiKeHoach.HetHieuLuc);
        ViewData["trangThai"] = trangThai;
        var ds = (await q.ToListAsync()).OrderBy(k => k.HeThong!.MucTrongYeu).ThenBy(k => k.HeThong!.Ma).ThenByDescending(k => k.PhienBan).ToList();
        // BR-HTT-02: hệ thống trọng yếu đang vận hành chưa có kế hoạch hiệu lực
        var coHieuLuc = (await db.KeHoachDowntimes.Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc).Select(k => k.HeThongId).ToListAsync()).ToHashSet();
        ViewBag.ThieuKeHoach = await db.HeThongThongTins.AsNoTracking()
            .Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao) && !coHieuLuc.Contains(h.Id))
            .OrderBy(h => h.MucTrongYeu).ThenBy(h => h.Ma).ToListAsync();
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(ds);
    }

    public async Task<IActionResult> Details(int id)
    {
        var k = await db.KeHoachDowntimes.AsNoTracking().Include(x => x.HeThong).ThenInclude(h => h!.KhoaPhongs).FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        // Trưởng khoa / Nhân viên: chỉ xem kế hoạch hiệu lực của hệ thống khoa mình dùng (qua Bộ downtime)
        if (!User.XemToanVien())
        {
            var khoa = User.KhoaPhongId() ?? -1;
            if (k.TrangThai != TrangThaiKeHoach.HieuLuc || k.HeThong!.KhoaPhongs.All(x => x.KhoaPhongId != khoa)) return NotFound();
        }
        ViewBag.PhienBans = await db.KeHoachDowntimes.AsNoTracking().Where(x => x.HeThongId == k.HeThongId).OrderByDescending(x => x.PhienBan).ToListAsync();
        ViewBag.Tep = await tep.DanhSachAsync("KeHoachDowntime", id.ToString());
        ViewBag.DuocDuyetCntt = User.LaDuyetCntt(thamSo);
        ViewBag.CanLanhDao = CanLanhDao(k.HeThong!);
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.DienTap = await db.DotDienTaps.AsNoTracking().Where(d => d.HeThongId == k.HeThongId).OrderByDescending(d => d.HanThucHien).Take(5).ToListAsync();
        return View(k);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id, int? heThongId)
    {
        KeHoachDowntime? k;
        if (id is not null)
        {
            k = await db.KeHoachDowntimes.AsNoTracking().Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
            if (k is null) return NotFound();
            if (!k.DuocSua) { TempData["err"] = "Chỉ sửa được kế hoạch ở trạng thái Nháp. Dùng \"Tạo phiên bản mới\" để thay đổi."; return RedirectToAction(nameof(Details), new { id }); }
        }
        else
        {
            var h = await db.HeThongThongTins.AsNoTracking().FirstOrDefaultAsync(x => x.Id == heThongId);
            if (h is null) return NotFound();
            if (await db.KeHoachDowntimes.AnyAsync(x => x.HeThongId == h.Id && x.TrangThai == TrangThaiKeHoach.Nhap))
            { TempData["err"] = "Hệ thống đã có kế hoạch nháp — mở bản nháp đó để tiếp tục."; return RedirectToAction(nameof(Index)); }
            k = new KeHoachDowntime { HeThongId = h.Id, HeThong = h, PhienBan = (await db.KeHoachDowntimes.Where(x => x.HeThongId == h.Id).MaxAsync(x => (int?)x.PhienBan) ?? 0) + 1 };
        }
        return View(k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(KeHoachDowntime input)
    {
        ModelState.Remove(nameof(input.HeThong));
        if (!ModelState.IsValid) { input.HeThong = await db.HeThongThongTins.FindAsync(input.HeThongId); return View(input); }
        if (input.Id == 0)
        {
            input.PhienBan = (await db.KeHoachDowntimes.Where(x => x.HeThongId == input.HeThongId).MaxAsync(x => (int?)x.PhienBan) ?? 0) + 1;
            input.TrangThai = TrangThaiKeHoach.Nhap;
            db.KeHoachDowntimes.Add(input);
        }
        else
        {
            var k = await db.KeHoachDowntimes.FirstOrDefaultAsync(x => x.Id == input.Id);
            if (k is null) return NotFound();
            if (!k.DuocSua) { TempData["err"] = "Kế hoạch không còn ở trạng thái Nháp."; return RedirectToAction(nameof(Details), new { id = k.Id }); }
            k.PhamVi = input.PhamVi; k.TieuChiKichHoat = input.TieuChiKichHoat; k.DanhBa = input.DanhBa; k.BuocBatDau = input.BuocBatDau;
            k.BuocTrongDowntime = input.BuocTrongDowntime; k.BuocKhoiPhuc = input.BuocKhoiPhuc; k.QuyTrinhThayThe = input.QuyTrinhThayThe;
            k.QuyTrinhNhapBu = input.QuyTrinhNhapBu; k.TieuChiKetThuc = input.TieuChiKetThuc;
            input = k;
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu kế hoạch downtime phiên bản {input.PhienBan}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    private async Task<IActionResult> Chuyen(int id, Func<KeHoachDowntime, Task<string>> thaoTac, bool chiCntt = true)
    {
        if (chiCntt && !User.LaCNTT()) return Forbid();
        var k = await db.KeHoachDowntimes.Include(x => x.HeThong).FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        try { TempData["ok"] = await thaoTac(k); await db.SaveChangesAsync(); }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDuyet(int id) => Chuyen(id, async k =>
    {
        k.GuiDuyet(User.Identity!.Name!);
        var nhan = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        await tb.GuiAsync(nhan, LoaiThongBao.ChoDuyet, $"Kế hoạch downtime {k.HeThong!.Ma} v{k.PhienBan} chờ duyệt", k.HeThong.Ten, $"/KeHoachDowntime/Details/{k.Id}", boQua: User.Identity!.Name);
        return "Đã gửi duyệt kế hoạch.";
    });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> Duyet(int id) => Chuyen(id, async k =>
    {
        var now = DateTime.Now;
        var xong = k.Duyet(User.Identity!.Name!, User.LaDuyetCntt(thamSo), User.IsInRole(VaiTro.LanhDao), CanLanhDao(k.HeThong!), now, thamSo.SoNguyen(TS.JciRaSoatKeHoachThang));
        if (xong)
        {
            // BR-DT-01: chỉ một phiên bản hiệu lực
            foreach (var cu in await db.KeHoachDowntimes.Where(x => x.HeThongId == k.HeThongId && x.Id != k.Id && x.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync())
                cu.TrangThai = TrangThaiKeHoach.HetHieuLuc;
            await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.KetQuaDuyet, $"Kế hoạch downtime {k.HeThong!.Ma} v{k.PhienBan} đã có hiệu lực", $"Rà soát lại trước {k.NgayRaSoat:dd/MM/yyyy}.", $"/KeHoachDowntime/Details/{k.Id}");
            return "Đã duyệt đủ cấp — kế hoạch có hiệu lực.";
        }
        await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.ChoDuyet, $"Kế hoạch downtime {k.HeThong!.Ma} (Tối quan trọng) chờ Lãnh đạo duyệt", k.HeThong.Ten, $"/KeHoachDowntime/Details/{k.Id}");
        return "Đã duyệt bước Trưởng phòng CNTT — chờ Lãnh đạo duyệt.";
    }, chiCntt: false);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public Task<IActionResult> TuChoi(int id, string lyDo) => Chuyen(id, async k =>
    {
        if (!User.LaDuyetCntt(thamSo) && !User.IsInRole(VaiTro.LanhDao)) throw new DomainException("Bạn không có quyền duyệt kế hoạch.");
        k.TuChoi(User.Identity!.Name!, lyDo);
        await tb.GuiAsync([k.NguoiGuiDuyet], LoaiThongBao.KetQuaDuyet, $"Kế hoạch downtime {k.HeThong!.Ma} v{k.PhienBan} bị từ chối", k.LyDoTuChoi, $"/KeHoachDowntime/Details/{k.Id}", MucDoThongBao.CanhBao);
        return "Đã từ chối, kế hoạch trở về Nháp.";
    }, chiCntt: false);

    /// <summary>FR-DT-02/13: tạo bản nháp phiên bản tiếp theo từ kế hoạch hiện có.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PhienBanMoi(int id)
    {
        if (!User.LaCNTT()) return Forbid();
        var k = await db.KeHoachDowntimes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        if (await db.KeHoachDowntimes.AnyAsync(x => x.HeThongId == k.HeThongId && (x.TrangThai == TrangThaiKeHoach.Nhap || x.TrangThai == TrangThaiKeHoach.ChoDuyet)))
        { TempData["err"] = "Hệ thống đã có phiên bản đang soạn / chờ duyệt."; return RedirectToAction(nameof(Details), new { id }); }
        var moi = k.TaoPhienBanMoi((await db.KeHoachDowntimes.Where(x => x.HeThongId == k.HeThongId).MaxAsync(x => x.PhienBan)) + 1);
        db.KeHoachDowntimes.Add(moi);
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã tạo bản nháp phiên bản {moi.PhienBan} từ phiên bản {k.PhienBan}.";
        return RedirectToAction(nameof(Form), new { id = moi.Id });
    }

    /// <summary>FR-DT-02: rà soát xong, giữ nguyên kế hoạch — dời hạn rà soát.</summary>
    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DaRaSoat(int id) => Chuyen(id, k =>
    {
        k.RaSoatXong(DateOnly.FromDateTime(DateTime.Today), thamSo.SoNguyen(TS.JciRaSoatKeHoachThang));
        return Task.FromResult($"Đã ghi nhận rà soát, hạn rà soát tiếp theo {k.NgayRaSoat:dd/MM/yyyy}.");
    });

    // ===== FR-DT-03: bộ downtime của khoa =====

    private async Task<int?> KhoaDangXemAsync(int? khoaId)
    {
        if (!User.XemToanVien()) return User.KhoaPhongId();
        return khoaId;
    }

    private async Task<BoKhoaVm?> DungBoAsync(int khoaId)
    {
        var khoa = await db.KhoaPhongs.AsNoTracking().FirstOrDefaultAsync(k => k.Id == khoaId);
        if (khoa is null) return null;
        var lk = await db.HeThongKhoaPhongs.AsNoTracking().Include(x => x.HeThong).Where(x => x.KhoaPhongId == khoaId && x.HeThong!.TrangThai != TrangThaiHeThong.NgungSuDung).ToListAsync();
        var ids = lk.Select(x => x.HeThongId).ToList();
        var kh = (await db.KeHoachDowntimes.AsNoTracking().Where(k => ids.Contains(k.HeThongId) && k.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync()).ToDictionary(k => k.HeThongId);
        return new BoKhoaVm(khoa, lk.OrderBy(x => x.HeThong!.MucTrongYeu).ThenBy(x => x.HeThong!.Ma).Select(x => (x.HeThong!, x.BatBuoc, kh.GetValueOrDefault(x.HeThongId))).ToList());
    }

    public async Task<IActionResult> BoKhoa(int? khoaId)
    {
        var k = await KhoaDangXemAsync(khoaId);
        ViewBag.DsKhoa = User.XemToanVien() ? new SelectList(await db.KhoaPhongs.OrderBy(x => x.Ten).ToListAsync(), "Id", "Ten", k) : null;
        if (k is null) return View(model: null);
        return View(await DungBoAsync(k.Value));
    }

    /// <summary>Bản in một trang để dán tại khoa / lưu ngoại tuyến (NFR-JC-01).</summary>
    public async Task<IActionResult> InBoKhoa(int? khoaId)
    {
        var k = await KhoaDangXemAsync(khoaId);
        if (k is null) return NotFound();
        var vm = await DungBoAsync(k.Value);
        return vm is null ? NotFound() : View(vm);
    }
}
