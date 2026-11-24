using ClosedXML.Excel;
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

public record DongPhuDaoTao(HeThongThongTin HeThong, KhoaPhong Khoa, bool BatBuoc, DateTime? LanCuoi, int SoNguoi);
public record GoiYDaoTao(string LyDo, string DoiTuong, string DeXuat, LoaiDaoTao Loai, int? KhoaId, int? HeThongId);

/// <summary>FR-DT-09 (MOI.01.04, SQE.01.07, HCT.01.02 ME4): đào tạo downtime, hệ thống thông tin, an toàn thông tin và an ninh mạng.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class DaoTaoController(AppDbContext db, ThamSoService thamSo, TepService tep) : Controller
{
    public async Task<IActionResult> Index(LoaiDaoTao? loai, int? khoa, DateOnly? tu, DateOnly? den)
    {
        var q = db.BuoiDaoTaos.AsNoTracking().Include(b => b.HeThong).Include(b => b.KhoaPhong).Include(b => b.ThamDus).AsQueryable();
        if (loai is not null) q = q.Where(b => b.Loai == loai);
        if (khoa is not null) q = q.Where(b => b.KhoaPhongId == khoa || b.ThamDus.Any(t => t.KhoaPhongId == khoa));
        if (tu is { } t1) q = q.Where(b => b.Ngay >= t1.ToDateTime(TimeOnly.MinValue));
        if (den is { } d1) q = q.Where(b => b.Ngay < d1.AddDays(1).ToDateTime(TimeOnly.MinValue));
        ViewData["loai"] = loai; ViewData["tu"] = tu; ViewData["den"] = den;
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoa);
        return View((await q.ToListAsync()).OrderByDescending(b => b.Ngay).Take(300).ToList());
    }

    private async Task NapAsync()
    {
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten");
        ViewBag.DsHeThong = new SelectList(await db.HeThongThongTins.OrderBy(h => h.Ma).Select(h => new { h.Id, Ten = h.Ma + " – " + h.Ten }).ToListAsync(), "Id", "Ten");
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id, LoaiDaoTao loai = LoaiDaoTao.Downtime, int? heThongId = null, int? khoaId = null)
    {
        var b = id is null ? new BuoiDaoTao { Ngay = DateTime.Now, Loai = loai, HeThongId = heThongId, KhoaPhongId = khoaId } : await db.BuoiDaoTaos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        await NapAsync();
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(BuoiDaoTao input)
    {
        ModelState.Remove(nameof(input.HeThong)); ModelState.Remove(nameof(input.KhoaPhong));
        if (input.Ngay > DateTime.Now.AddDays(1)) ModelState.AddModelError(nameof(input.Ngay), "Ghi nhận đào tạo đã diễn ra (ngày không ở tương lai).");
        if (!ModelState.IsValid) { await NapAsync(); return View(input); }
        if (input.Id == 0) db.BuoiDaoTaos.Add(input);
        else { var b = await db.BuoiDaoTaos.FirstAsync(x => x.Id == input.Id); input.NgayTao = b.NgayTao; input.NguoiTao = b.NguoiTao; db.Entry(b).CurrentValues.SetValues(input); }
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã lưu buổi đào tạo. Thêm danh sách người tham dự.";
        return RedirectToAction(nameof(Details), new { id = input.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var b = await db.BuoiDaoTaos.AsNoTracking().Include(x => x.HeThong).Include(x => x.KhoaPhong).Include(x => x.ThamDus).ThenInclude(t => t.KhoaPhong).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        ViewBag.Tep = await tep.DanhSachAsync("BuoiDaoTao", id.ToString());
        return View(b);
    }

    /// <summary>Dán danh sách: mỗi dòng "Họ tên; Mã nhân viên; Mã khoa; Chức danh; Đạt/Không đạt (tuỳ chọn)".</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThemNguoi(int id, string danhSach)
    {
        var b = await db.BuoiDaoTaos.Include(x => x.ThamDus).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        var khoas = await db.KhoaPhongs.AsNoTracking().ToListAsync();
        int them = 0, trung = 0; var loi = new List<string>();
        foreach (var dong in (danhSach ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var c = dong.Split([';', '\t'], StringSplitOptions.TrimEntries);
            if (c.Length == 0 || c[0].Length == 0) continue;
            var ma = c.Length > 1 && c[1].Length > 0 ? c[1] : null;
            KhoaPhong? k = c.Length > 2 && c[2].Length > 0 ? khoas.FirstOrDefault(x => string.Equals(x.Ma, c[2], StringComparison.OrdinalIgnoreCase) || string.Equals(x.Ten, c[2], StringComparison.OrdinalIgnoreCase)) : khoas.FirstOrDefault(x => x.Id == b.KhoaPhongId);
            if (c.Length > 2 && c[2].Length > 0 && k is null) { loi.Add($"\"{c[0]}\": không tìm thấy khoa \"{c[2]}\""); continue; }
            if (b.ThamDus.Any(t => string.Equals(t.HoTen, c[0], StringComparison.OrdinalIgnoreCase) && t.MaNhanVien == ma)) { trung++; continue; }
            bool? dat = c.Length > 4 ? (c[4].StartsWith("đ", StringComparison.OrdinalIgnoreCase) || c[4].StartsWith("d", StringComparison.OrdinalIgnoreCase) ? true : c[4].StartsWith("k", StringComparison.OrdinalIgnoreCase) ? false : null) : null;
            b.ThamDus.Add(new ThamDuDaoTao { HoTen = c[0], MaNhanVien = ma, KhoaPhongId = k?.Id, ChucDanh = c.Length > 3 && c[3].Length > 0 ? c[3] : null, DatDanhGia = dat });
            them++;
        }
        await db.SaveChangesAsync();
        TempData[loi.Count == 0 ? "ok" : "err"] = $"Đã thêm {them} người." + (trung > 0 ? $" Bỏ qua {trung} trùng." : "") + (loi.Count > 0 ? " Lỗi: " + string.Join("; ", loi) : "");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> GoNguoi(int id, int thamDuId)
    {
        await db.ThamDuDaoTaos.Where(t => t.Id == thamDuId && t.BuoiDaoTaoId == id).ExecuteDeleteAsync();
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Độ phủ: khoa phụ thuộc hệ thống trọng yếu đã được đào tạo downtime trong khoảng thời gian cấu hình.</summary>
    public async Task<IActionResult> BaoCao()
    {
        var thang = thamSo.SoNguyen(TS.JciDaoTaoTrongThang); var moc = DateTime.Today.AddMonths(-thang);
        var lk = await db.HeThongKhoaPhongs.AsNoTracking().Include(x => x.HeThong).Include(x => x.KhoaPhong)
            .Where(x => x.BatBuoc && x.HeThong!.TrangThai == TrangThaiHeThong.VanHanh && (x.HeThong.MucTrongYeu == MucTrongYeu.ToiQuanTrong || x.HeThong.MucTrongYeu == MucTrongYeu.Cao)).ToListAsync();
        var buoi = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).Where(b => b.Ngay >= moc && (b.Loai == LoaiDaoTao.Downtime || b.Loai == LoaiDaoTao.HeThongThongTin)).ToListAsync();
        var dong = lk.OrderBy(x => x.HeThong!.MucTrongYeu).ThenBy(x => x.HeThong!.Ma).ThenBy(x => x.KhoaPhong!.Ten).Select(x =>
        {
            var bs = buoi.Where(b => b.Loai == LoaiDaoTao.Downtime && (b.HeThongId == null || b.HeThongId == x.HeThongId) && (b.KhoaPhongId == x.KhoaPhongId || b.ThamDus.Any(t => t.KhoaPhongId == x.KhoaPhongId))).ToList();
            return new DongPhuDaoTao(x.HeThong!, x.KhoaPhong!, x.BatBuoc, bs.Count == 0 ? null : bs.Max(b => b.Ngay), bs.SelectMany(b => b.ThamDus).Where(t => t.KhoaPhongId == x.KhoaPhongId).Select(t => (t.MaNhanVien ?? t.HoTen).ToLower()).Distinct().Count());
        }).ToList();
        ViewBag.Thang = thang;
        // Đào tạo an toàn thông tin / an ninh mạng hằng năm (MOI.01.04 ME3)
        ViewBag.ATTT = await db.BuoiDaoTaos.AsNoTracking().Where(b => b.Loai == LoaiDaoTao.AnToanThongTin && b.Ngay >= moc).OrderByDescending(b => b.Ngay).Select(b => new { b.Id, b.Ngay, b.ChuDe, Co = b.ThamDus.Count }).ToListAsync().ContinueWith(t => t.Result.Select(x => (x.Id, x.Ngay, x.ChuDe, x.Co)).ToList());
        ViewBag.GoiY = await GoiYAsync();
        return View(dong);
    }

    /// <summary>SQE.01.07 ME3: nhu cầu đào tạo xác định từ dữ liệu chất lượng và an toàn.</summary>
    private async Task<List<GoiYDaoTao>> GoiYAsync()
    {
        var kq = new List<GoiYDaoTao>(); var sau6 = DateTime.Today.AddMonths(-6); var moc12 = DateTime.Today.AddMonths(-thamSo.SoNguyen(TS.JciDaoTaoTrongThang));
        var buoi = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).ToListAsync();
        bool DaDaoTao(int? khoa, DateTime sauNgay, LoaiDaoTao l) => buoi.Any(b => b.Loai == l && b.Ngay >= sauNgay && (khoa is null || b.KhoaPhongId == khoa || b.ThamDus.Any(t => t.KhoaPhongId == khoa)));
        foreach (var s in await db.SuKienDowntimes.AsNoTracking().Include(x => x.Khoas).ThenInclude(k => k.KhoaPhong).Include(x => x.HeThongs).Where(x => x.BatDau >= sau6 && x.Loai == LoaiDowntime.DotXuat).ToListAsync())
            if (s.VuotRto(DateTime.Now) || s.MucTrongYeuCaoNhat is MucTrongYeu.ToiQuanTrong)
                foreach (var k in s.Khoas.Where(k => k.KichHoatGiay && !DaDaoTao(k.KhoaPhongId, s.BatDau!.Value, LoaiDaoTao.Downtime)))
                    kq.Add(new GoiYDaoTao($"Downtime {s.Ma}{(s.VuotRto(DateTime.Now) ? " vượt RTO" : "")} phải dùng quy trình giấy", k.KhoaPhong!.Ten, "Đào tạo lại quy trình downtime", LoaiDaoTao.Downtime, k.KhoaPhongId, s.HeThongs.FirstOrDefault()?.HeThongId));
        foreach (var d in await db.DotDienTaps.AsNoTracking().Include(x => x.HeThong).ThenInclude(h => h!.KhoaPhongs).ThenInclude(k => k.KhoaPhong).Where(x => x.DaChot && x.KetLuan == KetLuanDienTap.KhongDat && x.NgayChot >= sau6 && x.LoaiThuNghiem == LoaiThuNghiem.Downtime).ToListAsync())
            foreach (var k in d.HeThong!.KhoaPhongs.Where(k => k.BatBuoc && !DaDaoTao(k.KhoaPhongId, d.NgayChot!.Value, LoaiDaoTao.Downtime)))
                kq.Add(new GoiYDaoTao($"Diễn tập downtime {d.HeThong.Ma} không đạt", k.KhoaPhong!.Ten, "Đào tạo bổ sung trước lần diễn tập lại", LoaiDaoTao.Downtime, k.KhoaPhongId, d.HeThongId));
        var an = await db.SuCoAnNinhs.AsNoTracking().Where(x => x.PhatHien >= sau6 && x.Loai != LoaiSuCoAn.LoHong).OrderByDescending(x => x.PhatHien).FirstOrDefaultAsync();
        if (an is not null && !DaDaoTao(null, an.PhatHien, LoaiDaoTao.AnToanThongTin)) kq.Add(new GoiYDaoTao($"Có sự cố an ninh mạng ({an.Loai.Ten()}) gần đây", "Toàn bệnh viện", "Đào tạo nhận biết lừa đảo, mật khẩu, báo sự cố", LoaiDaoTao.AnToanThongTin, null, null));
        else if (!DaDaoTao(null, moc12, LoaiDaoTao.AnToanThongTin)) kq.Add(new GoiYDaoTao("Chưa có đào tạo an toàn thông tin / an ninh mạng trong chu kỳ (MOI.01.04 ME3)", "Toàn bệnh viện", "Tổ chức đào tạo hằng năm theo vai trò", LoaiDaoTao.AnToanThongTin, null, null));
        return kq.DistinctBy(x => (x.LyDo, x.DoiTuong)).ToList();
    }

    /// <summary>SQE.01.07 ME7: xuất bản ghi hoàn thành đào tạo theo từng nhân viên để Phòng Tổ chức cán bộ lưu hồ sơ nhân sự.</summary>
    public async Task<IActionResult> XuatNhanVien(DateOnly? tu, DateOnly? den, int? khoa, LoaiDaoTao? loai)
    {
        var q = db.ThamDuDaoTaos.AsNoTracking().Include(t => t.BuoiDaoTao).Include(t => t.KhoaPhong).AsQueryable();
        if (tu is { } a) q = q.Where(t => t.BuoiDaoTao!.Ngay >= a.ToDateTime(TimeOnly.MinValue));
        if (den is { } b) q = q.Where(t => t.BuoiDaoTao!.Ngay < b.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (khoa is not null) q = q.Where(t => t.KhoaPhongId == khoa);
        if (loai is not null) q = q.Where(t => t.BuoiDaoTao!.Loai == loai);
        var ds = (await q.ToListAsync()).OrderBy(t => t.HoTen).ThenBy(t => t.BuoiDaoTao!.Ngay).ToList();
        using var wb = new XLWorkbook(); var ws = wb.AddWorksheet("DaoTaoTheoNhanVien");
        string[] cot = ["Họ tên", "Mã nhân viên", "Khoa/phòng", "Chức danh", "Nội dung đào tạo", "Chủ đề", "Ngày", "Người giảng", "Kết quả đánh giá"];
        for (var i = 0; i < cot.Length; i++) ws.Cell(1, i + 1).Value = cot[i];
        var r = 2;
        foreach (var t in ds)
        {
            object?[] v = [t.HoTen, t.MaNhanVien, t.KhoaPhong?.Ten, t.ChucDanh, t.BuoiDaoTao!.Loai.Ten(), t.BuoiDaoTao.ChuDe, t.BuoiDaoTao.Ngay.ToString("dd/MM/yyyy"), t.BuoiDaoTao.NguoiGiang, t.DatDanhGia is { } d ? (d ? "Đạt" : "Không đạt") : null];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = XLCellValue.FromObject(v[i]);
            r++;
        }
        ExcelTaiSanService.DinhDangBang(ws, 1, r - 1, cot.Length);
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"DaoTao_TheoNhanVien_{DateTime.Now:yyyyMMdd}.xlsx");
    }
}
