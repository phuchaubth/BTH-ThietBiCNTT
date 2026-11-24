using System.Text.Json;
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

public record SnapSuCo(string Loai, string Ma, string Mo, string ChiTiet);
public record SnapHanhDong(string Ma, string MoTa, string PhuTrach, string Han, string TrangThai);
public record SnapBaoCao(List<ChiSoJci> ChiSo, List<SnapSuCo> SuCo, List<SnapHanhDong> HanhDong, List<string> HoanThanh, List<string> RuiRoCao, List<string> CanQuyetDinh, int SuCoAnNghiemTrong, int SuKienAnToanNguoiBenh);

/// <summary>FR-JC-04 (GLD.04.01 ME2, QPS.03.04 ME5): báo cáo chất lượng và an toàn bằng văn bản cho Lãnh đạo ít nhất hằng quý.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class BaoCaoLanhDaoController(AppDbContext db, ChiSoJciService chiSo, ThamSoService thamSo, ThongBaoService tb, HanhDongService hanhDong, TepService tep, UserManager<AppUser> users) : Controller
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    public async Task<IActionResult> Index()
    {
        ViewBag.HomNay = DateOnly.FromDateTime(DateTime.Today);
        return View(await db.BaoCaoLanhDaos.AsNoTracking().OrderByDescending(b => b.TuNgay).ToListAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Tao(int nam, int quy)
    {
        if (quy is < 1 or > 4 || nam < 2020) { TempData["err"] = "Kỳ không hợp lệ."; return RedirectToAction(nameof(Index)); }
        var (tu, den) = BaoCaoLanhDao.KhoangQuy(nam, quy); var ky = $"{nam}-Q{quy}";
        if (await db.BaoCaoLanhDaos.AnyAsync(b => b.Ky == ky)) { TempData["err"] = $"Đã có báo cáo kỳ {ky}."; return RedirectToAction(nameof(Index)); }
        var snap = await DungAsync(tu, den);
        var b = new BaoCaoLanhDao { Ky = ky, TuNgay = tu, DenNgay = den, Snapshot = JsonSerializer.Serialize(snap, Json) };
        db.BaoCaoLanhDaos.Add(b); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lập báo cáo quý {ky} từ dữ liệu hệ thống. Bổ sung nhận xét rồi gửi Lãnh đạo.";
        return RedirectToAction(nameof(Details), new { id = b.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var b = await db.BaoCaoLanhDaos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        ViewBag.Snap = JsonSerializer.Deserialize<SnapBaoCao>(b.Snapshot, Json);
        ViewBag.Tep = await tep.DanhSachAsync("BaoCaoLanhDao", id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.BaoCaoLanhDao && h.NguonId == id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.DsNguoi = await users.DsCnttAsync(User.Identity!.Name);
        return View(b);
    }

    public async Task<IActionResult> In(int id)
    {
        var b = await db.BaoCaoLanhDaos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        ViewBag.Snap = JsonSerializer.Deserialize<SnapBaoCao>(b.Snapshot, Json);
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Gui(int id, string? nhanXet, string? suCoNghiemTrong)
    {
        var b = await db.BaoCaoLanhDaos.FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        if (!User.LaDuyetCntt(thamSo)) { TempData["err"] = "Chỉ Trưởng phòng CNTT (người duyệt) gửi báo cáo cho Lãnh đạo."; return RedirectToAction(nameof(Details), new { id }); }
        try
        {
            b.Gui(User.Identity!.Name!, nhanXet, suCoNghiemTrong, DateTime.Now);
            await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.BaoCaoLanhDao, $"Báo cáo hạ tầng CNTT quý {b.Ky} chờ xem", $"Từ {b.NguoiGui}. Xác nhận đã xem và ghi ý kiến chỉ đạo.", $"/BaoCaoLanhDao/Details/{b.Id}");
            await db.SaveChangesAsync(); TempData["ok"] = "Đã gửi báo cáo cho Lãnh đạo — không sửa được nữa.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.LanhDao)]
    public async Task<IActionResult> XacNhan(int id, string? yKien)
    {
        var b = await db.BaoCaoLanhDaos.FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return NotFound();
        try { b.XacNhanDaXem(User.Identity!.Name!, yKien, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = "Đã xác nhận đã xem báo cáo."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Ý kiến chỉ đạo → hành động khắc phục (FR-JC-04).</summary>
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> TaoHanhDong(int id, string moTa, string nguoiChiuTrachNhiem, DateOnly? han)
    {
        if (!await db.BaoCaoLanhDaos.AnyAsync(x => x.Id == id)) return NotFound();
        try { var h = await hanhDong.TaoAsync(NguonHanhDong.BaoCaoLanhDao, id, moTa, null, nguoiChiuTrachNhiem, han, LoaiHanhDong.CaiTien, MucDoUuTien.Cao, User.Identity!.Name!); TempData["ok"] = $"Đã tạo hành động {h.Ma} từ ý kiến chỉ đạo."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Lập ảnh chụp số liệu: chỉ số, mọi sự cố hệ thống/quy trình, hành động, rủi ro, hạng mục cần quyết định.</summary>
    private async Task<SnapBaoCao> DungAsync(DateOnly tu, DateOnly den)
    {
        var now = DateTime.Now; var t0 = tu.ToDateTime(TimeOnly.MinValue); var t1 = den.AddDays(1).ToDateTime(TimeOnly.MinValue); var hn = DateOnly.FromDateTime(DateTime.Today);
        var chiSoList = await chiSo.TinhAsync(tu, den, now);
        var suCo = new List<SnapSuCo>();
        foreach (var s in await db.SuKienDowntimes.AsNoTracking().Include(x => x.HeThongs).ThenInclude(h => h.HeThong).Where(s => s.BatDau >= t0 && s.BatDau < t1 && s.TrangThai != TrangThaiSuKien.DaHuy).OrderBy(s => s.BatDau).ToListAsync())
            suCo.Add(new SnapSuCo(s.Loai == LoaiDowntime.DotXuat ? "Downtime đột xuất" : "Downtime có kế hoạch", s.Ma, s.BatDau!.Value.ToString("dd/MM/yyyy HH:mm"),
                $"{string.Join(", ", s.HeThongs.Select(h => h.HeThong?.Ma))} · {(s.ThoiLuong(now) is { } tl ? (int)tl.TotalMinutes : 0)} phút{(s.VuotRto(now) ? " · VƯỢT RTO" : "")}{(s.AnhHuongAnToan ? " · ảnh hưởng an toàn người bệnh" : "")} · {s.MoTa}"));
        var anSuCo = await db.SuCoAnNinhs.AsNoTracking().Where(s => s.PhatHien >= t0 && s.PhatHien < t1).OrderBy(s => s.PhatHien).ToListAsync();
        foreach (var s in anSuCo) suCo.Add(new SnapSuCo("Sự cố an ninh mạng", s.Ma, s.PhatHien.ToString("dd/MM/yyyy HH:mm"), $"{s.Loai.Ten()} · {(s.DaPhanLoai ? s.MucDo.Ten() : "chưa phân mức")}{(s.LienQuanDuLieuCaNhan == true ? " · liên quan dữ liệu cá nhân" : "")} · {s.TrangThai.Ten()}"));
        foreach (var k in await db.KetQuaSaoLuuLans.AsNoTracking().Include(x => x.TacVuSaoLuu).ThenInclude(t => t!.HeThong).Where(x => x.KetQua == KetQuaSaoLuu.Loi && x.ThoiDiem >= t0 && x.ThoiDiem < t1).OrderBy(x => x.ThoiDiem).ToListAsync())
            suCo.Add(new SnapSuCo("Sao lưu lỗi", k.TacVuSaoLuu!.HeThong!.Ma, k.ThoiDiem.ToString("dd/MM/yyyy HH:mm"), $"{k.TacVuSaoLuu.Ten} · {k.GhiChu}"));
        foreach (var l in await db.LanThuPhucHois.AsNoTracking().Include(x => x.HeThong).Where(x => x.DaChot && x.Dat == false && x.NgayChot >= t0 && x.NgayChot < t1).ToListAsync())
            suCo.Add(new SnapSuCo("Thử phục hồi không đạt", l.HeThong!.Ma, l.NgayChot!.Value.ToString("dd/MM/yyyy"), string.Join("; ", l.LyDoKhongDat())));
        foreach (var d in await db.DotDienTaps.AsNoTracking().Include(x => x.HeThong).Where(x => x.DaChot && x.KetLuan == KetLuanDienTap.KhongDat && x.NgayChot >= t0 && x.NgayChot < t1).ToListAsync())
            suCo.Add(new SnapSuCo(d.LoaiThuNghiem == LoaiThuNghiem.Downtime ? "Diễn tập downtime không đạt" : "Thử ứng phó tấn công mạng không đạt", d.HeThong?.Ma ?? "—", d.NgayChot!.Value.ToString("dd/MM/yyyy"), d.PhatHien ?? ""));
        var hd = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.TrangThai != TrangThaiHanhDong.DaDong || (h.NgayKiemChung >= t0 && h.NgayKiemChung < t1)).OrderBy(h => h.Han).ToListAsync();
        var hanhDongQuaHan = hd.Where(h => h.QuaHan(hn)).Select(h => new SnapHanhDong(h.Ma, h.MoTa, h.NguoiChiuTrachNhiem, h.Han.ToString("dd/MM/yyyy"), h.TrangThai.Ten())).ToList();
        var hoanThanh = hd.Where(h => h.TrangThai == TrangThaiHanhDong.DaDong && h.NgayKiemChung >= t0 && h.NgayKiemChung < t1).Select(h => $"{h.Ma}: {h.MoTa} — kiểm chứng {h.NgayKiemChung:dd/MM/yyyy}: {h.BangChungHieuQua}").ToList();
        var rui = (await db.RuiRos.AsNoTracking().Where(r => r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy).ToListAsync()).Where(r => r.Muc >= MucRuiRo.Cao).OrderByDescending(r => r.Diem).Select(r => $"{r.Ma} [{r.Nhom.Ten()}] {r.MoTa.Replace('\n', ' ')} — {r.Diem} điểm ({r.Muc.Ten()}), phụ trách {r.NguoiChiuTrachNhiem}").ToList();
        var quyet = new List<string>();
        foreach (var r in await db.RuiRos.AsNoTracking().Where(r => r.PhuongAn == PhuongAnRuiRo.ChapNhan && r.NguoiDuyetChapNhan == null && (r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy)).ToListAsync()) if (r.CanLanhDaoDuyetChapNhan) quyet.Add($"Chấp nhận rủi ro {r.Ma} ({r.Muc.Ten()}): {r.LyDoChapNhan}");
        foreach (var k in await db.KeHoachDowntimes.AsNoTracking().Include(x => x.HeThong).Where(x => x.TrangThai == TrangThaiKeHoach.ChoDuyet && x.NguoiDuyetCntt != null && x.NguoiDuyetLanhDao == null).ToListAsync()) quyet.Add($"Phê duyệt kế hoạch downtime {k.HeThong!.Ma} v{k.PhienBan}");
        foreach (var t in await db.TaiLieuQuyDinhs.AsNoTracking().Where(x => x.TrangThai == TrangThaiKeHoach.ChoDuyet && x.NguoiDuyetCntt != null).ToListAsync()) quyet.Add($"Phê duyệt {t.Loai.Ten()}: {t.Ten} v{t.PhienBan}");
        foreach (var d in await db.DotDanhGiaRuiRos.AsNoTracking().Where(x => x.DaChot && x.NguoiDuyet == null).ToListAsync()) quyet.Add($"Phê duyệt báo cáo đánh giá rủi ro năm {d.Nam}");
        return new SnapBaoCao(chiSoList, suCo, hanhDongQuaHan, hoanThanh, rui, quyet, anSuCo.Count(s => s.NghiemTrongTroLen), await db.SuKienDowntimes.CountAsync(s => s.AnhHuongAnToan && s.BatDau >= t0 && s.BatDau < t1));
    }
}
