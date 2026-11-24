using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Đợt đánh giá rủi ro hằng năm (FMS.02.00 ME1–ME4, MOI.01.02 ME1/ME4, FR-RR-05, FR-RR-08).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class DotRuiRoController(AppDbContext db, ThamSoService thamSo, ThongBaoService tb, TepService tep) : Controller
{
    // Nhóm rủi ro thuộc "hệ thống tiện ích" (FMS.08.00 – gồm hệ thống thông tin, liên lạc, dữ liệu, điện) chuyển đầu mối FMS gộp vào đánh giá chung của bệnh viện
    private static readonly NhomRuiRo[] NhomTienIch = [NhomRuiRo.DienDieuHoa, NhomRuiRo.HaTangVatLy, NhomRuiRo.Mang];

    public async Task<IActionResult> Index()
    {
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(await db.DotDanhGiaRuiRos.AsNoTracking().OrderByDescending(d => d.Nam).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Tao(int nam, string? thanhPhan)
    {
        if (nam < 2020 || nam > DateTime.Today.Year + 1) { TempData["err"] = "Năm không hợp lệ."; return RedirectToAction(nameof(Index)); }
        if (await db.DotDanhGiaRuiRos.AnyAsync(d => d.Nam == nam)) { TempData["err"] = $"Đã có đợt đánh giá năm {nam}."; return RedirectToAction(nameof(Index)); }
        var gs = await db.NguoiGiamSatCntts.AsNoTracking().Where(x => x.NgayKetThuc == null || x.NgayKetThuc >= DateOnly.FromDateTime(DateTime.Today)).OrderByDescending(x => x.NgayBatDau).FirstOrDefaultAsync();
        var d = new DotDanhGiaRuiRo { Nam = nam, ChuTri = gs?.HoTen, ThanhPhan = thanhPhan?.Trim() };
        db.DotDanhGiaRuiRos.Add(d); await db.SaveChangesAsync();
        TempData["ok"] = gs is null ? "Đã tạo đợt. Chưa có người giám sát CNTT y tế được chỉ định để chủ trì (HCT.01.00)." : $"Đã tạo đợt đánh giá năm {nam}, chủ trì: {gs.HoTen}.";
        return RedirectToAction(nameof(Details), new { id = d.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var d = await db.DotDanhGiaRuiRos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        var mo = await db.RuiRos.AsNoTracking().Include(r => r.HeThong).Include(r => r.LichSus).Where(r => r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy || r.TrangThai == TrangThaiRuiRo.DaKiemSoat).ToListAsync();
        ViewBag.Ruis = mo.OrderByDescending(r => r.Diem).ThenBy(r => r.Ma).ToList();
        ViewBag.Tep = await tep.DanhSachAsync("DotDanhGiaRuiRo", id.ToString());
        return View(d);
    }

    /// <summary>Chấm lại một rủi ro trong đợt (lưu lịch sử điểm gắn đợt).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ChamLai(int id, int ruiRoId, int khaNang, int hauQua, int? khaNangTonDu, int? hauQuaTonDu, string? lyDo)
    {
        var d = await db.DotDanhGiaRuiRos.FirstOrDefaultAsync(x => x.Id == id);
        var r = await db.RuiRos.Include(x => x.LichSus).FirstOrDefaultAsync(x => x.Id == ruiRoId);
        if (d is null || r is null) return NotFound();
        if (d.DaChot) { TempData["err"] = "Đợt đã chốt."; return RedirectToAction(nameof(Details), new { id }); }
        try
        {
            // Giữ nguyên điểm vẫn được tính là đã chấm lại trong đợt (ghi một dòng lịch sử gắn đợt)
            r.LichSus.Add(new LichSuDiemRuiRo { NgayDanhGia = DateTime.Now, KhaNang = khaNang, HauQua = hauQua, NguoiDanhGia = User.Identity!.Name, DotDanhGiaRuiRoId = id,
                LyDo = string.IsNullOrWhiteSpace(lyDo) ? (khaNang == r.KhaNang && hauQua == r.HauQua ? $"Rà soát năm {d.Nam}: giữ nguyên" : $"Đánh giá năm {d.Nam}") : lyDo.Trim() });
            if (khaNang != r.KhaNang || hauQua != r.HauQua || khaNangTonDu != r.KhaNangTonDu || hauQuaTonDu != r.HauQuaTonDu)
            {
                r.LichSus.Remove(r.LichSus.Last());
                r.ChamDiem(khaNang, hauQua, khaNangTonDu, hauQuaTonDu, lyDo ?? "", User.Identity!.Name!, DateTime.Now, id);
            }
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã chấm lại {r.Ma}: {r.Diem} điểm — {r.Muc.Ten()}.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Chot(int id)
    {
        var d = await db.DotDanhGiaRuiRos.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        var ruis = await db.RuiRos.Include(r => r.HeThong).Include(r => r.LichSus).Where(r => r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy || r.TrangThai == TrangThaiRuiRo.DaKiemSoat).ToListAsync();
        var chua = ruis.Count(r => r.LichSus.All(l => l.DotDanhGiaRuiRoId != id));
        try
        {
            d.Chot(ruis.Count, chua, User.Identity!.Name!, DateTime.Now);
            d.BaoCao = await DungBaoCaoAsync(d, ruis);
            await db.SaveChangesAsync();
            await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.RuiRo, $"Báo cáo đánh giá rủi ro CNTT năm {d.Nam} chờ phê duyệt", null, $"/DotRuiRo/Details/{d.Id}");
            TempData["ok"] = "Đã chốt đợt và lập báo cáo — không sửa được nữa. Đã gửi Lãnh đạo phê duyệt.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.LanhDao)]
    public async Task<IActionResult> Duyet(int id)
    {
        var d = await db.DotDanhGiaRuiRos.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        if (!d.DaChot || d.NguoiDuyet is not null) { TempData["err"] = "Báo cáo chưa chốt hoặc đã được duyệt."; return RedirectToAction(nameof(Details), new { id }); }
        d.NguoiDuyet = User.Identity!.Name; d.NgayDuyet = DateTime.Now;
        await db.SaveChangesAsync(); TempData["ok"] = "Đã phê duyệt báo cáo đánh giá rủi ro.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>FMS.02.00 ME4: lãnh đạo gửi báo cáo hằng năm cho cơ quan quản trị; ghi nhận hành động của cơ quan quản trị.</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
    public async Task<IActionResult> GuiCoQuan(int id, string? hanhDong)
    {
        var d = await db.DotDanhGiaRuiRos.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();
        if (d.NguoiDuyet is null) { TempData["err"] = "Báo cáo cần được Lãnh đạo phê duyệt trước khi gửi cơ quan quản trị."; return RedirectToAction(nameof(Details), new { id }); }
        if (d.NgayGuiCoQuanQuanTri is null) { d.NguoiGuiCoQuanQuanTri = User.Identity!.Name; d.NgayGuiCoQuanQuanTri = DateTime.Now; }
        if (!string.IsNullOrWhiteSpace(hanhDong)) d.HanhDongCuaCoQuanQuanTri = hanhDong.Trim();
        await db.SaveChangesAsync(); TempData["ok"] = "Đã ghi nhận việc gửi báo cáo cho cơ quan quản trị.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> InBaoCao(int id)
    {
        var d = await db.DotDanhGiaRuiRos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return d is null || d.BaoCao is null ? NotFound() : View(d);
    }

    private async Task<string> DungBaoCaoAsync(DotDanhGiaRuiRo d, List<RuiRo> ruis)
    {
        var sb = new StringBuilder(); var hn = DateOnly.FromDateTime(DateTime.Today);
        void Dong(RuiRo r) => sb.AppendLine($"- {r.Ma} [{r.Nhom.Ten()}] {r.MoTa.Replace('\n', ' ')} — {r.Diem} điểm ({r.Muc.Ten()})" + (r.DiemTonDu is { } t ? $", tồn dư {t} ({r.MucTonDu!.Value.Ten()})" : "") + $" — {r.TrangThai.Ten()}, phụ trách {r.NguoiChiuTrachNhiem ?? "—"}" + (r.PhuongAn is { } p ? $", phương án {p.Ten()}" : ""));
        sb.AppendLine($"BÁO CÁO ĐÁNH GIÁ RỦI RO CÔNG NGHỆ THÔNG TIN NĂM {d.Nam}");
        sb.AppendLine($"Chủ trì (người giám sát CNTT y tế): {d.ChuTri ?? "chưa chỉ định"}. Thành phần: {d.ThanhPhan ?? "—"}.");
        sb.AppendLine($"Chốt ngày {d.NgayChot:dd/MM/yyyy} bởi {d.NguoiChot}.\n");
        sb.AppendLine($"1. TỔNG QUAN: {ruis.Count} rủi ro được đánh giá — " + string.Join(", ", Enum.GetValues<MucRuiRo>().OrderByDescending(m => m).Select(m => $"{m.Ten()}: {ruis.Count(r => r.Muc == m)}")) + ".");
        var truoc = await db.DotDanhGiaRuiRos.AsNoTracking().Where(x => x.Nam < d.Nam && x.DaChot).OrderByDescending(x => x.Nam).FirstOrDefaultAsync();
        if (truoc is not null)
        {
            var thayDoi = ruis.Select(r => (r, truocDiem: r.LichSus.Where(l => l.DotDanhGiaRuiRoId != d.Id && l.NgayDanhGia <= (truoc.NgayChot ?? DateTime.MinValue)).OrderByDescending(l => l.NgayDanhGia).Select(l => (int?)l.Diem).FirstOrDefault())).ToList();
            sb.AppendLine($"So với đợt năm {truoc.Nam}: {ruis.Count(r => r.LichSus.All(l => l.NgayDanhGia > (truoc.NgayChot ?? DateTime.MinValue)))} rủi ro mới; tăng điểm {thayDoi.Count(x => x.truocDiem is { } a && x.r.Diem > a)}; giảm điểm {thayDoi.Count(x => x.truocDiem is { } a && x.r.Diem < a)}.");
        }
        sb.AppendLine("\n2. 10 RỦI RO HÀNG ĐẦU"); foreach (var r in ruis.OrderByDescending(r => r.Diem).ThenBy(r => r.Ma).Take(10)) Dong(r);
        sb.AppendLine("\n3. RỦI RO AN NINH MẠNG VÀ AN TOÀN THÔNG TIN (MOI.01.02 ME1, HCT.01.05)");
        var an = ruis.Where(r => r.Nhom == NhomRuiRo.AnNinhMang || r.Nhom == NhomRuiRo.DuLieu).OrderByDescending(r => r.Diem).ToList();
        if (an.Count == 0) sb.AppendLine("(chưa có rủi ro nào trong nhóm)"); foreach (var r in an) Dong(r);
        sb.AppendLine("\n4. RỦI RO HỆ THỐNG TIỆN ÍCH (thông tin, liên lạc, dữ liệu, điện/điều hoà) — chuyển đầu mối FMS gộp vào đánh giá chung (FMS.02.00 ME1)");
        var ti = ruis.Where(r => NhomTienIch.Contains(r.Nhom)).OrderByDescending(r => r.Diem).ToList();
        if (ti.Count == 0) sb.AppendLine("(chưa có rủi ro nào trong nhóm)"); foreach (var r in ti) Dong(r);
        sb.AppendLine("\n5. RỦI RO CHẤP NHẬN");
        var cn = ruis.Where(r => r.PhuongAn == PhuongAnRuiRo.ChapNhan).ToList();
        if (cn.Count == 0) sb.AppendLine("(không có)"); foreach (var r in cn) sb.AppendLine($"- {r.Ma} ({r.Muc.Ten()}): {r.LyDoChapNhan}; xem xét lại {r.NgayXemXetLai:dd/MM/yyyy}; duyệt: {r.NguoiDuyetChapNhan ?? "không cần / chưa duyệt"}.");
        var hd = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.RuiRo).ToListAsync();
        sb.AppendLine($"\n6. HIỆU QUẢ CẢI THIỆN (FMS.02.00 ME3): {hd.Count} hành động gắn với rủi ro — hoàn thành và kiểm chứng {hd.Count(h => h.TrangThai == TrangThaiHanhDong.DaDong)}, đang thực hiện {hd.Count(h => h.TrangThai is TrangThaiHanhDong.Mo or TrangThaiHanhDong.DangLam)}, quá hạn {hd.Count(h => h.QuaHan(hn))}.");
        return sb.ToString();
    }
}
