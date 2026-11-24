using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Dòng danh sách hệ thống kèm cảnh báo hồ sơ (BR-HTT-01, BR-HTT-02).</summary>
public record DongHeThong(HeThongThongTin HeThong, int Rto, int Rpo, bool CoKeHoach, bool CoSaoLuu, int SoKhoa, int SoPhuThuoc, List<string> CanhBao);

/// <summary>Phân hệ 4.1 – Danh mục hệ thống thông tin & mức trọng yếu (FR-HTT-01..07, 10).</summary>
public class HeThongController(AppDbContext db, ThamSoService thamSo, HaTangService haTang, TepService tep, UserManager<AppUser> users) : Controller
{
    /// <summary>Trưởng khoa / Nhân viên chỉ thấy hệ thống khoa mình sử dụng (mục 6: K).</summary>
    private IQueryable<HeThongThongTin> PhamVi()
    {
        var q = db.HeThongThongTins.AsQueryable();
        if (User.XemToanVien()) return q;
        var k = User.KhoaPhongId() ?? -1;
        return q.Where(h => h.KhoaPhongs.Any(x => x.KhoaPhongId == k) || h.ChuSoHuuKhoaPhongId == k);
    }

    public async Task<IActionResult> Index(MucTrongYeu? muc, TrangThaiHeThong? trangThai, int? khoa, bool chiCanhBao = false)
    {
        var q = PhamVi().AsNoTracking().Include(h => h.ChuSoHuuKhoaPhong).AsQueryable();
        if (muc is not null) q = q.Where(h => h.MucTrongYeu == muc);
        if (trangThai is not null) q = q.Where(h => h.TrangThai == trangThai);
        else q = q.Where(h => h.TrangThai != TrangThaiHeThong.NgungSuDung);
        if (khoa is not null) q = q.Where(h => h.KhoaPhongs.Any(x => x.KhoaPhongId == khoa));
        var ds = await q.OrderBy(h => h.MucTrongYeu).ThenBy(h => h.Ma).ToListAsync();
        var ids = ds.Select(h => h.Id).ToList();
        var coKh = (await db.KeHoachDowntimes.Where(k => ids.Contains(k.HeThongId) && k.TrangThai == TrangThaiKeHoach.HieuLuc).Select(k => k.HeThongId).ToListAsync()).ToHashSet();
        var coSl = (await db.TacVuSaoLuus.Where(k => ids.Contains(k.HeThongId) && k.DangApDung).Select(k => k.HeThongId).ToListAsync()).ToHashSet();
        var soKhoa = await db.HeThongKhoaPhongs.Where(k => ids.Contains(k.HeThongId)).GroupBy(k => k.HeThongId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var soPt = await db.PhuThuocHeThongs.Where(k => ids.Contains(k.HeThongId)).GroupBy(k => k.HeThongId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var dong = ds.Select(h =>
        {
            var cb = h.HoSoThieu().Select(x => "Thiếu " + x).ToList();
            if (h.TrangThai == TrangThaiHeThong.VanHanh && h.LaTrongYeu)
            {
                if (!coKh.Contains(h.Id)) cb.Add("Chưa có kế hoạch downtime hiệu lực");
                if (!coSl.Contains(h.Id)) cb.Add("Chưa khai báo sao lưu");
            }
            if (h.TrangThai == TrangThaiHeThong.VanHanh && soPt.GetValueOrDefault(h.Id) == 0) cb.Add("Chưa khai báo phụ thuộc hạ tầng");
            return new DongHeThong(h, thamSo.Rto(h), thamSo.Rpo(h), coKh.Contains(h.Id), coSl.Contains(h.Id), soKhoa.GetValueOrDefault(h.Id), soPt.GetValueOrDefault(h.Id), cb);
        }).Where(d => !chiCanhBao || d.CanhBao.Count > 0).ToList();
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoa);
        ViewData["muc"] = muc; ViewData["trangThai"] = trangThai; ViewData["chiCanhBao"] = chiCanhBao;
        return View(dong);
    }

    /// <summary>FR-HTT-10: hồ sơ hệ thống.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var h = await PhamVi().AsNoTracking().Include(x => x.ChuSoHuuKhoaPhong).Include(x => x.NhaCungCap).Include(x => x.HopDong)
            .Include(x => x.KhoaPhongs).ThenInclude(k => k.KhoaPhong)
            .Include(x => x.PhuThuocs).ThenInclude(p => p.TaiSan).ThenInclude(t => t!.LoaiThietBi)
            .Include(x => x.PhuThuocs).ThenInclude(p => p.HeThongPhuThuoc)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        var now = DateTime.Now;
        var canh = await haTang.CanhAsync();
        ViewBag.DiemLoiDon = await db.TaiSans.AsNoTracking().Where(t => PhanTichPhuThuoc.DiemLoiDon(id, canh).Contains(t.Id)).OrderBy(t => t.MaTaiSan).ToListAsync();
        var anhHuong = PhanTichPhuThuoc.HeThongBiAnhHuongBoiHeThong(id, canh);
        ViewBag.HeThongPhuThuocVao = await db.HeThongThongTins.AsNoTracking().Where(x => anhHuong.Contains(x.Id)).OrderBy(x => x.MucTrongYeu).ToListAsync();
        ViewBag.KeHoach = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == id).OrderByDescending(k => k.PhienBan).ToListAsync();
        var suKien = await db.SuKienDowntimes.AsNoTracking().Where(s => s.HeThongs.Any(x => x.HeThongId == id) && s.TrangThai != TrangThaiSuKien.DaHuy)
            .OrderByDescending(s => s.BatDau ?? s.DuKienBatDau).Take(20).ToListAsync();
        ViewBag.SuKien = suKien;
        // BR-DT-05: khả dụng 30 / 90 ngày (chỉ tính downtime đột xuất và phần vượt khung có kế hoạch)
        var tatCa = await db.SuKienDowntimes.AsNoTracking().Where(s => s.HeThongs.Any(x => x.HeThongId == id) && s.BatDau != null && (s.KetThuc == null || s.KetThuc >= now.AddDays(-90))).ToListAsync();
        (double, TimeSpan) KD(int ngay, bool chiDotXuat) => KhaDung.Tinh(now.AddDays(-ngay), now,
            tatCa.Select(s => KhaDung.Khoang(s, now, chiDotXuat)).Where(x => x is not null).Select(x => x!.Value));
        ViewBag.KhaDung30 = KD(30, true); ViewBag.KhaDung90 = KD(90, true); ViewBag.KhaDung30TatCa = KD(30, false);
        ViewBag.TacVu = await db.TacVuSaoLuus.AsNoTracking().Where(t => t.HeThongId == id).OrderBy(t => t.Ten).ToListAsync();
        ViewBag.PhucHoi = await db.LanThuPhucHois.AsNoTracking().Where(l => l.HeThongId == id).OrderByDescending(l => l.NgayChot ?? DateTime.MaxValue).ThenByDescending(l => l.Id).Take(5).ToListAsync();
        ViewBag.DienTap = await db.DotDienTaps.AsNoTracking().Where(l => l.HeThongId == id).OrderByDescending(l => l.NgayChot ?? DateTime.MaxValue).ThenByDescending(l => l.Id).Take(5).ToListAsync();
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(x => x.HeThongId == id && x.TrangThai != TrangThaiHanhDong.DaDong).OrderBy(x => x.Han).ToListAsync();
        ViewBag.Rto = thamSo.Rto(h); ViewBag.Rpo = thamSo.Rpo(h); ViewBag.MucTieu = thamSo.MucTieu(h.MucTrongYeu);
        ViewBag.Tep = await tep.DanhSachAsync("HeThong", id.ToString());
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.Where(x => x.Id != id && x.TrangThai != TrangThaiHeThong.NgungSuDung).OrderBy(x => x.Ma).Select(x => new { x.Id, Ten = x.Ma + " – " + x.Ten }).ToListAsync(), "Id", "Ten");
        return View(h);
    }

    private async Task NapDanhMucAsync(HeThongThongTin h)
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", h.ChuSoHuuKhoaPhongId);
        ViewBag.DsNcc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten", h.NhaCungCapId);
        ViewBag.DsHopDong = new SelectList(await db.HopDongs.OrderByDescending(x => x.Id).Select(x => new { x.Id, Ten = x.SoHopDong + " – " + x.Ten }).ToListAsync(), "Id", "Ten", h.HopDongId);
        ViewBag.DsCntt = new SelectList((await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Concat(await users.GetUsersInRoleAsync(VaiTro.QuanTri))
            .Where(u => u.DangHoatDong).DistinctBy(u => u.Id).Select(u => new { u.UserName, Ten = $"{u.HoTen} ({u.UserName})" }), "UserName", "Ten");
        ViewBag.MacDinh = Enum.GetValues<MucTrongYeu>().ToDictionary(m => m, thamSo.MucTieu);
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id)
    {
        var h = id is null ? new HeThongThongTin() : await db.HeThongThongTins.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        await NapDanhMucAsync(h);
        return View(h);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(HeThongThongTin input)
    {
        input.Ma = (input.Ma ?? "").Trim().ToUpperInvariant();
        if (await db.HeThongThongTins.AnyAsync(x => x.Ma == input.Ma && x.Id != input.Id)) ModelState.AddModelError(nameof(input.Ma), "Mã hệ thống đã tồn tại.");
        if (!ModelState.IsValid) { await NapDanhMucAsync(input); return View(input); }
        if (input.Id == 0) db.HeThongThongTins.Add(input);
        else
        {
            var h = await db.HeThongThongTins.FirstAsync(x => x.Id == input.Id);
            input.NgayTao = h.NgayTao; input.NguoiTao = h.NguoiTao;
            db.Entry(h).CurrentValues.SetValues(input);
            // BR-HTT-05: ngừng sử dụng → kế hoạch & lịch hết hiệu lực (giữ lịch sử)
            if (input.TrangThai == TrangThaiHeThong.NgungSuDung)
            {
                await db.KeHoachDowntimes.Where(k => k.HeThongId == h.Id && k.TrangThai != TrangThaiKeHoach.HetHieuLuc)
                    .ExecuteUpdateAsync(u => u.SetProperty(k => k.TrangThai, TrangThaiKeHoach.HetHieuLuc));
                await db.TacVuSaoLuus.Where(k => k.HeThongId == h.Id).ExecuteUpdateAsync(u => u.SetProperty(k => k.DangApDung, false));
            }
        }
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu hệ thống {input.Ma}.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    /// <summary>FR-HTT-05: khoa/phòng sử dụng.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThemKhoa(int id, List<int> khoaIds, bool batBuoc = true)
    {
        var co = await db.HeThongKhoaPhongs.Where(x => x.HeThongId == id).Select(x => x.KhoaPhongId).ToListAsync();
        foreach (var k in khoaIds.Distinct().Except(co)) db.HeThongKhoaPhongs.Add(new HeThongKhoaPhong { HeThongId = id, KhoaPhongId = k, BatBuoc = batBuoc });
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id, tab = "khoa" });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GoKhoa(int id, int khoaId)
    {
        await db.HeThongKhoaPhongs.Where(x => x.HeThongId == id && x.KhoaPhongId == khoaId).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>FR-HTT-06: thêm phụ thuộc — danh sách mã tài sản, hoặc một hệ thống khác (BR-HTT-03 chống vòng).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThemPhuThuoc(int id, string? danhSachMa, int? heThongPhuThuocId, string? vaiTro, bool coDuPhong)
    {
        if (!await db.HeThongThongTins.AnyAsync(x => x.Id == id)) return NotFound();
        var thongBao = new List<string>();
        if (heThongPhuThuocId is { } pt)
        {
            if (PhanTichPhuThuoc.TaoVong(id, pt, await haTang.CanhAsync())) thongBao.Add("Không thêm được: phụ thuộc này tạo vòng (BR-HTT-03).");
            else if (await db.PhuThuocHeThongs.AnyAsync(x => x.HeThongId == id && x.HeThongPhuThuocId == pt)) thongBao.Add("Hệ thống đã có trong danh sách phụ thuộc.");
            else db.PhuThuocHeThongs.Add(new PhuThuocHeThong { HeThongId = id, HeThongPhuThuocId = pt, VaiTro = vaiTro?.Trim(), CoDuPhong = coDuPhong });
        }
        var mas = (danhSachMa ?? "").Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim().ToUpperInvariant()).Distinct().ToList();
        var them = 0;
        if (mas.Count > 0)
        {
            var ts = await db.TaiSans.Where(t => mas.Contains(t.MaTaiSan)).ToListAsync();
            var co = await db.PhuThuocHeThongs.Where(x => x.HeThongId == id && x.TaiSanId != null).Select(x => x.TaiSanId!.Value).ToListAsync();
            foreach (var t in ts.Where(t => !co.Contains(t.Id)))
            {
                if (t.TrangThai == Domain.Enums.TrangThaiTaiSan.DaThanhLy) { thongBao.Add($"{t.MaTaiSan} đã thanh lý."); continue; }
                db.PhuThuocHeThongs.Add(new PhuThuocHeThong { HeThongId = id, TaiSanId = t.Id, VaiTro = vaiTro?.Trim(), CoDuPhong = coDuPhong }); them++;
            }
            var khongCo = mas.Except(ts.Select(t => t.MaTaiSan)).ToList();
            if (khongCo.Count > 0) thongBao.Add("Không tìm thấy: " + string.Join(", ", khongCo));
        }
        await db.SaveChangesAsync();
        TempData[thongBao.Count == 0 ? "ok" : "err"] = (them > 0 ? $"Đã thêm {them} tài sản. " : "") + string.Join(" ", thongBao) + (thongBao.Count == 0 && them == 0 ? "Đã cập nhật phụ thuộc." : "");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> SuaPhuThuoc(int id, int phuThuocId, bool coDuPhong)
    {
        await db.PhuThuocHeThongs.Where(x => x.Id == phuThuocId && x.HeThongId == id).ExecuteUpdateAsync(u => u.SetProperty(x => x.CoDuPhong, coDuPhong));
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GoPhuThuoc(int id, int phuThuocId)
    {
        await db.PhuThuocHeThongs.Where(x => x.Id == phuThuocId && x.HeThongId == id).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>FR-HTT-07: phân tích ảnh hưởng từ một tài sản (nhập mã).</summary>
    [Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public async Task<IActionResult> AnhHuong(string? ma, int? taiSanId)
    {
        TaiSan? ts = null;
        if (taiSanId is { } tid) ts = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).FirstOrDefaultAsync(t => t.Id == tid);
        else if (!string.IsNullOrWhiteSpace(ma)) ts = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).FirstOrDefaultAsync(t => t.MaTaiSan == ma.Trim().ToUpper());
        ViewBag.Ma = ma ?? ts?.MaTaiSan;
        ViewBag.TaiSan = ts;
        ViewBag.KetQua = ts is null ? null : await haTang.AnhHuongCuaTaiSanAsync(ts.Id);
        // Danh sách tài sản là điểm lỗi đơn của hệ thống Tối quan trọng / Cao (FR-HTT-08)
        var canh = await haTang.CanhAsync();
        var trongYeu = await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao)).ToListAsync();
        var spof = trongYeu.SelectMany(h => PhanTichPhuThuoc.DiemLoiDon(h.Id, canh).Select(t => (t, h))).ToList();
        var tsIds = spof.Select(x => x.t).Distinct().ToList();
        var dsTs = await db.TaiSans.AsNoTracking().Where(t => tsIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id);
        ViewBag.DiemLoiDon = spof.GroupBy(x => x.t).Where(g => dsTs.ContainsKey(g.Key))
            .Select(g => (TaiSan: dsTs[g.Key], HeThongs: g.Select(x => x.h).OrderBy(h => h.MucTrongYeu).ToList())).OrderBy(x => x.HeThongs.Min(h => h.MucTrongYeu)).ThenBy(x => x.TaiSan.MaTaiSan).ToList();
        return View();
    }
}
