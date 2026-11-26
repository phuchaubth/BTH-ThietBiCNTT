using System.IO.Compression;
using ClosedXML.Excel;
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

public record DongTieuChuan(TieuChuanJci TieuChuan, KetQuaBangChung BangChung, DanhGiaTieuChuanJci? DanhGia);

/// <summary>Phân hệ 4.8 – hồ sơ bằng chứng JCI: danh mục tiêu chuẩn và tự đánh giá (FR-JC-01, 02, 08), bộ chỉ số (FR-JC-03), truy vết theo hệ thống (FR-JC-06).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class JciController(AppDbContext db, BangChungJciService bangChung, ChiSoJciService chiSo, HaTangService haTang, ThamSoService thamSo, TepService tep) : Controller
{
    public async Task<IActionResult> TieuChuan(string? chuong, MucBangChung? bangChungLoc)
    {
        var tc = await db.TieuChuanJcis.AsNoTracking().OrderBy(t => t.Ma).ToListAsync();
        var bc = await bangChung.TinhTatCaAsync(DateTime.Now);
        var dg = (await db.DanhGiaTieuChuanJcis.AsNoTracking().ToListAsync()).GroupBy(d => d.TieuChuanJciId).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.NgayDanhGia).First());
        var dong = tc.Select(t => new DongTieuChuan(t, bc.GetValueOrDefault(t.Ma) ?? new KetQuaBangChung(MucBangChung.ChuaTuDong, ""), dg.GetValueOrDefault(t.Id))).ToList();
        ViewBag.Tong = dong.GroupBy(d => d.BangChung.Muc).ToDictionary(g => g.Key, g => g.Count());
        if (!string.IsNullOrEmpty(chuong)) dong = dong.Where(d => d.TieuChuan.Chuong == chuong).ToList();
        if (bangChungLoc is not null) dong = dong.Where(d => d.BangChung.Muc == bangChungLoc).ToList();
        ViewData["chuong"] = chuong; ViewData["bangChung"] = bangChungLoc;
        ViewBag.DsChuong = tc.Select(t => t.Chuong).Distinct().ToList();
        return View(dong);
    }

    /// <summary>FR-JC-05: xuất gói hồ sơ bằng chứng (ZIP): mục lục theo tiêu chuẩn, dữ liệu theo phân hệ (Excel), tệp đính kèm gốc.</summary>
    public async Task<IActionResult> XuatBangChung(string? chuong, DateOnly? tu, DateOnly? den)
    {
        var t0 = (tu ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-1))).ToDateTime(TimeOnly.MinValue);
        var t1 = (den ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(new TimeOnly(23, 59, 59));
        var now = DateTime.Now; var xemAn = User.XemAn(thamSo);
        var tc = await db.TieuChuanJcis.AsNoTracking().OrderBy(t => t.Ma).ToListAsync();
        if (!string.IsNullOrEmpty(chuong)) tc = tc.Where(t => t.Chuong == chuong).ToList();
        var bc = await bangChung.TinhTatCaAsync(now);
        var dg = (await db.DanhGiaTieuChuanJcis.AsNoTracking().ToListAsync()).GroupBy(d => d.TieuChuanJciId).ToDictionary(g => g.Key, g => g.OrderByDescending(d => d.NgayDanhGia).First());

        void Sheet(XLWorkbook wb, string ten, string[] cot, IEnumerable<object?[]> dong)
        {
            var ws = wb.AddWorksheet(ten); for (var c = 0; c < cot.Length; c++) ws.Cell(1, c + 1).Value = cot[c];
            var r = 2; foreach (var d in dong) { for (var c = 0; c < d.Length; c++) ws.Cell(r, c + 1).Value = d[c] switch { null => "", DateTime x => x.ToString("dd/MM/yyyy HH:mm"), DateOnly x => x.ToString("dd/MM/yyyy"), var o => o.ToString() ?? "" }; r++; }
            ws.Row(1).Style.Font.Bold = true; ws.Columns().AdjustToContents(30);
        }
        bool Trong(DateTime x) => x >= t0 && x <= t1;
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            void Them(string ten, XLWorkbook wb) { var e = zip.CreateEntry(ten); using var es = e.Open(); wb.SaveAs(es); }
            using (var wb = new XLWorkbook())
            {
                Sheet(wb, "Muc luc", ["Tiêu chuẩn", "Chương", "Tóm lược", "Bằng chứng từ hệ thống", "Chi tiết", "Đánh giá người phụ trách", "Ghi chú", "Nơi tìm hồ sơ"],
                    tc.Select(t => new object?[] { t.Ma, t.Chuong, t.TomTat, (bc.GetValueOrDefault(t.Ma)?.Muc)?.Ten(), bc.GetValueOrDefault(t.Ma)?.ChiTiet, dg.GetValueOrDefault(t.Id)?.DanhGia.Ten(), dg.GetValueOrDefault(t.Id)?.GhiChu, t.ChucNang }));
                Sheet(wb, "Thong tin", ["Mục", "Giá trị"], [["Khoảng thời gian", $"{t0:dd/MM/yyyy} – {t1:dd/MM/yyyy}"], ["Chương", chuong ?? "Tất cả"], ["Người xuất", User.Identity!.Name], ["Thời điểm xuất", now]]);
                Them("00_MucLuc.xlsx", wb);
            }
            using (var wb = new XLWorkbook())
            {
                Sheet(wb, "Downtime", ["Mã", "Loại", "Bắt đầu", "Khôi phục", "Trạng thái", "Mô tả", "Rút kinh nghiệm"], (await db.SuKienDowntimes.AsNoTracking().ToListAsync()).Where(x => Trong(x.BatDau ?? x.DuKienBatDau ?? x.NgayTao)).Select(x => new object?[] { x.Ma, x.Loai.Ten(), x.BatDau, x.KetThuc, x.TrangThai.Ten(), x.MoTa, x.NgayRutKinhNghiem }));
                Sheet(wb, "Dien tap", ["Hạn", "Loại", "Hệ thống", "Kết luận", "Ngày chốt"], (await db.DotDienTaps.AsNoTracking().Include(x => x.HeThong).ToListAsync()).Where(x => x.NgayChot is { } n ? Trong(n) : Trong(x.NgayTao)).Select(x => new object?[] { x.HanThucHien, x.LoaiThuNghiem.Ten(), x.HeThong?.Ma, x.KetLuan?.Ten(), x.NgayChot }));
                Sheet(wb, "Thu phuc hoi", ["Hệ thống", "Hạn", "Kết quả", "RTO đạt (phút)", "Ngày chốt"], (await db.LanThuPhucHois.AsNoTracking().Include(x => x.HeThong).ToListAsync()).Where(x => x.NgayChot is { } n ? Trong(n) : Trong(x.NgayTao)).Select(x => new object?[] { x.HeThong?.Ma, x.HanThucHien, x.Dat is null ? "" : x.Dat == true ? "Đạt" : "Không đạt", x.RtoDatPhut, x.NgayChot }));
                Sheet(wb, "Hanh dong", ["Mã", "Nguồn", "Mô tả", "Người phụ trách", "Hạn", "Trạng thái"], (await db.HanhDongKhacPhucs.AsNoTracking().ToListAsync()).Where(x => Trong(x.NgayTao)).Select(x => new object?[] { x.Ma, x.Nguon.Ten(), x.MoTa, x.NguoiChiuTrachNhiem, x.Han, x.TrangThai.Ten() }));
                Sheet(wb, "Rui ro", ["Mã", "Nhóm", "Mô tả", "Điểm", "Mức", "Phương án", "Trạng thái"], (await db.RuiRos.AsNoTracking().ToListAsync()).Where(x => Trong(x.NgayTao) || x.TrangThai != TrangThaiRuiRo.Dong).Select(x => new object?[] { x.Ma, x.Nhom.Ten(), x.MoTa, x.Diem, x.Muc.Ten(), x.PhuongAn?.Ten(), x.TrangThai.Ten() }));
                Sheet(wb, "Thay doi", ["Số phiếu", "Loại", "Tên", "Trạng thái", "Kết quả", "Ngày"], (await db.PhieuThayDois.AsNoTracking().ToListAsync()).Where(x => Trong(x.NgayTao)).Select(x => new object?[] { x.Ma, x.Loai.Ten(), x.Ten, x.TrangThai.Ten(), x.KetQua, x.NgayTao }));
                Sheet(wb, "Ra soat quyen", ["Số đợt", "Hệ thống", "Trạng thái", "Số tài khoản", "Ngày đóng", "Biên bản"], (await db.DotRaSoatQuyens.AsNoTracking().Include(x => x.HeThong).Include(x => x.TaiKhoans).ToListAsync()).Where(x => Trong(x.NgayTao)).Select(x => new object?[] { x.Ma, x.HeThong?.Ma, x.TrangThai.Ten(), x.TaiKhoans.Count, x.NgayDong, x.BienBan }));
                Sheet(wb, "Tai lieu", ["Mã", "Loại", "Tên", "Phiên bản", "Trạng thái", "Hiệu lực", "Rà soát"], (await db.TaiLieuQuyDinhs.AsNoTracking().ToListAsync()).Where(x => x.TrangThai == TrangThaiKeHoach.HieuLuc || Trong(x.NgayTao)).Select(x => new object?[] { x.Ma, x.Loai.Ten(), x.Ten, x.PhienBan, x.TrangThai.Ten(), x.NgayHieuLuc, x.NgayRaSoat }));
                Sheet(wb, "Dao tao", ["Ngày", "Loại", "Chủ đề", "Số người tham dự"], (await db.BuoiDaoTaos.AsNoTracking().Include(x => x.ThamDus).ToListAsync()).Where(x => Trong(x.Ngay)).Select(x => new object?[] { x.Ngay, x.Loai.Ten(), x.ChuDe, x.ThamDus.Count }));
                Sheet(wb, "Bao cao quy", ["Kỳ", "Ngày gửi", "Ngày xác nhận đã xem", "Nhận xét"], (await db.BaoCaoLanhDaos.AsNoTracking().ToListAsync()).Where(x => Trong(x.NgayTao)).Select(x => new object?[] { x.Ky, x.NgayGui, x.NgayXem, x.NhanXet }));
                Sheet(wb, "Nha cung cap", ["Hợp đồng", "Năm", "Điểm TB", "Kết luận", "Ngày đánh giá"], (await db.DanhGiaNhaCungCaps.AsNoTracking().Include(x => x.HopDong).ToListAsync()).Where(x => x.NgayDanhGia != null).Select(x => new object?[] { x.HopDong?.SoHopDong, x.Nam, x.DiemTb, x.KetLuan.Ten(), x.NgayDanhGia }));
                if (xemAn)
                {
                    Sheet(wb, "Su co an ninh", ["Mã", "Loại", "Mức độ", "Trạng thái", "Phát hiện", "Đóng"], (await db.SuCoAnNinhs.AsNoTracking().ToListAsync()).Where(x => Trong(x.PhatHien)).Select(x => new object?[] { x.Ma, x.Loai.Ten(), x.MucDo.Ten(), x.TrangThai.Ten(), x.PhatHien, x.NgayDong }));
                    Sheet(wb, "Lo hong", ["Tiêu đề", "CVE", "CVSS", "Hạn vá", "Trạng thái"], (await db.LoHongs.AsNoTracking().ToListAsync()).Where(x => Trong(x.NgayTao)).Select(x => new object?[] { x.TieuDe, x.MaCve, x.Cvss, x.HanVa, x.TrangThai.Ten() }));
                }
                Them("01_DuLieu.xlsx", wb);
            }
            // Tệp đính kèm gốc (loại trừ hồ sơ sự cố an ninh nếu không có quyền)
            var cam = xemAn ? Array.Empty<string>() : new[] { "SuCoAnNinh" };
            foreach (var f in (await db.TepDinhKems.AsNoTracking().Where(x => x.ThoiGian >= t0 && x.ThoiGian <= t1).ToListAsync()).Where(x => !cam.Contains(x.DoiTuong)))
            {
                string duong; try { duong = tep.DuongDan(f); } catch { continue; }
                if (!System.IO.File.Exists(duong)) continue;
                var e = zip.CreateEntry($"TepDinhKem/{f.DoiTuong}/{f.DoiTuongId}_{f.Id}_{Path.GetFileName(f.TenFile)}"); using var es = e.Open(); await using var fs = System.IO.File.OpenRead(duong); await fs.CopyToAsync(es);
            }
        }
        return File(ms.ToArray(), "application/zip", $"ho-so-bang-chung-{chuong ?? "JCI"}-{now:yyyyMMdd}.zip");
    }

    /// <summary>FR-JC-02: người phụ trách đánh giá bổ sung (Đạt / Đạt một phần / Không đạt).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> DanhGia(int id, DanhGiaTieuChuan danhGia, string? ghiChu)
    {
        if (!await db.TieuChuanJcis.AnyAsync(t => t.Id == id)) return NotFound();
        if (danhGia != DanhGiaTieuChuan.Dat && string.IsNullOrWhiteSpace(ghiChu)) { TempData["err"] = "Đánh giá Đạt một phần / Không đạt cần ghi chú lý do."; return RedirectToAction(nameof(TieuChuan)); }
        db.DanhGiaTieuChuanJcis.Add(new DanhGiaTieuChuanJci { TieuChuanJciId = id, DanhGia = danhGia, GhiChu = ghiChu?.Trim(), NguoiDanhGia = User.Identity!.Name, NgayDanhGia = DateTime.Now });
        await db.SaveChangesAsync();
        TempData["ok"] = "Đã ghi đánh giá.";
        return RedirectToAction(nameof(TieuChuan));
    }

    /// <summary>FR-JC-01: Phòng QLCL bổ sung tóm lược / ghi chú theo sách tiêu chuẩn (Quản trị thực hiện thay).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.QuanTri)]
    public async Task<IActionResult> GhiChu(int id, string tomTat, string? ghiChu)
    {
        var t = await db.TieuChuanJcis.FirstOrDefaultAsync(x => x.Id == id);
        if (t is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(tomTat)) t.TomTat = tomTat.Trim();
        t.GhiChu = ghiChu?.Trim();
        await db.SaveChangesAsync(); TempData["ok"] = $"Đã cập nhật {t.Ma}.";
        return RedirectToAction(nameof(TieuChuan));
    }

    public async Task<IActionResult> ChiSo(int? nam, int? quy)
    {
        var d = DateTime.Today; nam ??= d.Year; quy ??= (d.Month - 1) / 3 + 1;
        var (tu, den) = BaoCaoLanhDao.KhoangQuy(nam.Value, Math.Clamp(quy.Value, 1, 4));
        ViewData["nam"] = nam; ViewData["quy"] = quy; ViewBag.Tu = tu; ViewBag.Den = den;
        return View(await chiSo.TinhAsync(tu, den, DateTime.Now));
    }

    /// <summary>FR-JC-06: chuỗi bằng chứng của một hệ thống trên một trang (dùng khi đoàn đánh giá truy vết).</summary>
    public async Task<IActionResult> Tracer(int id)
    {
        var h = await db.HeThongThongTins.AsNoTracking().Include(x => x.NhaCungCap).Include(x => x.HopDong).Include(x => x.KhoaPhongs).ThenInclude(k => k.KhoaPhong).FirstOrDefaultAsync(x => x.Id == id);
        if (h is null) return NotFound();
        ViewBag.KeHoach = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.HeThongId == id).OrderByDescending(k => k.PhienBan).ToListAsync();
        ViewBag.DaoTao = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).Include(b => b.KhoaPhong).Where(b => b.HeThongId == id || (b.HeThongId == null && b.Loai == LoaiDaoTao.Downtime)).OrderByDescending(b => b.Ngay).Take(8).ToListAsync();
        ViewBag.DienTap = await db.DotDienTaps.AsNoTracking().Where(x => x.HeThongId == id).OrderByDescending(x => x.HanThucHien).Take(6).ToListAsync();
        ViewBag.SuKien = await db.SuKienDowntimes.AsNoTracking().Where(s => s.HeThongs.Any(x => x.HeThongId == id)).OrderByDescending(s => s.BatDau ?? s.DuKienBatDau).Take(8).ToListAsync();
        ViewBag.TacVu = await db.TacVuSaoLuus.AsNoTracking().Where(t => t.HeThongId == id).ToListAsync();
        ViewBag.PhucHoi = await db.LanThuPhucHois.AsNoTracking().Where(l => l.HeThongId == id).OrderByDescending(l => l.HanThucHien).Take(6).ToListAsync();
        ViewBag.RuiRo = await db.RuiRos.AsNoTracking().Where(r => r.HeThongId == id).OrderByDescending(r => r.KhaNang * r.HauQua).Take(8).ToListAsync();
        ViewBag.SuCoAn = await db.SuCoAnNinhs.AsNoTracking().Where(s => s.HeThongId == id).OrderByDescending(s => s.PhatHien).Take(6).ToListAsync();
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(x => x.HeThongId == id).OrderBy(x => x.TrangThai == TrangThaiHanhDong.DaDong).ThenBy(x => x.Han).Take(8).ToListAsync();
        ViewBag.TaiLieu = await db.TaiLieuQuyDinhs.AsNoTracking().Where(t => t.HeThongId == id && t.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync();
        ViewBag.GiamSat = await db.NguoiGiamSatCntts.AsNoTracking().OrderByDescending(x => x.NgayBatDau).FirstOrDefaultAsync(x => x.NgayKetThuc == null || x.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today));
        ViewBag.Rto = thamSo.Rto(h); ViewBag.Rpo = thamSo.Rpo(h); ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.PhuThuoc = await db.PhuThuocHeThongs.AsNoTracking().Include(p => p.TaiSan).Include(p => p.HeThongPhuThuoc).Where(p => p.HeThongId == id).ToListAsync();
        return View(h);
    }
}
