using Microsoft.AspNetCore.Authorization;
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

/// <summary>Phân hệ 4.5 – Quản lý thay đổi (FR-TD-01..09; HCT.01.00 ME3-4, FMS.10.00).</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao + "," + VaiTro.TruongKhoa)]
public class PhieuThayDoiController(AppDbContext db, ThayDoiService td, HanhDongService hanhDong, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb, TepService tep) : Controller
{
    private bool CNTT => User.LaCNTT();

    public async Task<IActionResult> Index(TrangThaiThayDoi? trangThai, LoaiThayDoi? loai, bool chiMo = true)
    {
        if (!(CNTT || User.IsInRole(VaiTro.LanhDao))) return RedirectToAction(nameof(Cho));
        var q = db.PhieuThayDois.AsNoTracking().Include(p => p.HeThong).AsQueryable();
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        else if (chiMo) q = q.Where(p => p.TrangThai == TrangThaiThayDoi.Nhap || p.TrangThai == TrangThaiThayDoi.ChoDuyet || p.TrangThai == TrangThaiThayDoi.DaDuyet || p.TrangThai == TrangThaiThayDoi.DangThucHien);
        if (loai is not null) q = q.Where(p => p.Loai == loai);
        ViewData["trangThai"] = trangThai; ViewData["loai"] = loai; ViewData["chiMo"] = chiMo;
        ViewBag.QuaHan = await db.PhieuThayDois.AsNoTracking().Where(p => p.Loai == LoaiThayDoi.KhanCap && p.NguoiDuyetCntt == null && p.HanDuyetBoSung != null).ToListAsync();
        ViewBag.DanhGiaDenHan = await db.DanhGiaSauThayDois.AsNoTracking().CountAsync(d => d.NgayDanhGia == null && d.Han <= DateOnly.FromDateTime(DateTime.Today));
        return View(await q.OrderByDescending(p => p.NgayTao).Take(300).ToListAsync());
    }

    /// <summary>Trưởng khoa: các phiếu chờ xác nhận với tư cách chủ sở hữu nghiệp vụ.</summary>
    public async Task<IActionResult> Cho()
    {
        var k = User.KhoaPhongId();
        var ids = await db.HeThongThongTins.Where(h => h.ChuSoHuuKhoaPhongId == k).Select(h => h.Id).ToListAsync();
        var ds = await db.PhieuThayDois.AsNoTracking().Include(p => p.HeThong).Where(p => p.TrangThai == TrangThaiThayDoi.ChoDuyet && p.HeThongId != null && ids.Contains(p.HeThongId.Value)).ToListAsync();
        return View(ds);
    }

    /// <summary>FR-TD-04: lịch tháng.</summary>
    public async Task<IActionResult> Lich(int? nam, int? thang)
    {
        if (!(CNTT || User.IsInRole(VaiTro.LanhDao))) return Forbid();
        var d = new DateTime(nam ?? DateTime.Today.Year, thang ?? DateTime.Today.Month, 1); var den = d.AddMonths(1);
        var ds = await db.PhieuThayDois.AsNoTracking().Include(p => p.HeThong).Include(p => p.HeThongs)
            .Where(p => p.DeXuatBatDau != null && p.DeXuatBatDau < den && p.DeXuatKetThuc >= d && p.TrangThai != TrangThaiThayDoi.Nhap && p.TrangThai != TrangThaiThayDoi.TuChoi).OrderBy(p => p.DeXuatBatDau).ToListAsync();
        var trung = new Dictionary<int, List<string>>();
        foreach (var p in ds) { var t = (await td.TrungLichAsync(p)).Where(x => ds.Any(y => y.Id == x.Id)).Select(x => x.Ma).ToList(); if (t.Count > 0) trung[p.Id] = t; }
        ViewBag.Trung = trung; ViewBag.Thang = d;
        return View(ds);
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await Nap(id);
        if (p is null) return NotFound();
        if (!await DuocXem(p)) return Forbid();
        await NapChiTiet(p);
        return View(p);
    }

    private async Task<PhieuThayDoi?> Nap(int id) => await db.PhieuThayDois.Include(x => x.HeThong).Include(x => x.TaiSan).Include(x => x.SuKienDowntime)
        .Include(x => x.HeThongs).ThenInclude(h => h.HeThong).Include(x => x.MucPcras).Include(x => x.Checklist).Include(x => x.DanhGiaSau).FirstOrDefaultAsync(x => x.Id == id);

    private async Task<bool> DuocXem(PhieuThayDoi p)
    {
        if (CNTT || User.IsInRole(VaiTro.LanhDao)) return true;
        var k = User.KhoaPhongId(); var ids = HeThongIds(p);
        return k is not null && await db.HeThongThongTins.AnyAsync(h => ids.Contains(h.Id) && h.ChuSoHuuKhoaPhongId == k);
    }

    private static List<int> HeThongIds(PhieuThayDoi p) => ThayDoiService.HeThongIds(p);

    private async Task NapChiTiet(PhieuThayDoi p)
    {
        var tq = await td.CoToiQuanTrongAsync(p);
        ViewBag.ToiQuanTrong = tq; ViewBag.BuocTiep = p.TrangThai is TrangThaiThayDoi.ChoDuyet || (p.LaKhanCap && p.TrangThai == TrangThaiThayDoi.DangThucHien) ? p.BuocTiepTheo(tq) : null;
        ViewBag.Thieu = p.ThieuDieuKien();
        ViewBag.Trung = await td.TrungLichAsync(p);
        ViewBag.Tep = await tep.DanhSachAsync("PhieuThayDoi", p.Id.ToString());
        ViewBag.HanhDong = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Nguon == NguonHanhDong.PhieuThayDoi && h.NguonId == p.Id).OrderBy(h => h.Han).ToListAsync();
        ViewBag.QuyenCntt = User.LaDuyetCntt(thamSo); ViewBag.QuyenLanhDao = User.IsInRole(VaiTro.LanhDao);
        var k = User.KhoaPhongId(); var ids = HeThongIds(p);
        ViewBag.QuyenChuSoHuu = User.IsInRole(VaiTro.QuanTri) || (k is not null && await db.HeThongThongTins.AnyAsync(h => ids.Contains(h.Id) && h.ChuSoHuuKhoaPhongId == k));
    }

    private async Task NapForm(PhieuThayDoi p)
    {
        ViewBag.DsHeThong = await db.HeThongThongTins.AsNoTracking().Where(h => h.TrangThai != Domain.Entities.TrangThaiHeThong.NgungSuDung).OrderBy(h => h.Ma).ToListAsync();
        ViewBag.DsTaiSan = new SelectList(await db.TaiSans.AsNoTracking().OrderBy(t => t.MaTaiSan).Take(500).Select(t => new { t.Id, Ten = t.MaTaiSan + " – " + t.Ten }).ToListAsync(), "Id", "Ten", p.TaiSanId);
        ViewBag.Chuan = await db.ThayDoiChuanMaus.AsNoTracking().Where(m => m.HoatDong && m.NguoiDuyet != null).OrderBy(m => m.Ten).ToListAsync();
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(int? id, LoaiThayDoi loai = LoaiThayDoi.Thuong, int? heThongId = null, int? chuanId = null)
    {
        PhieuThayDoi p;
        if (id is { } i) { p = await Nap(i) ?? new PhieuThayDoi(); if (p.Id == 0) return NotFound(); if (p.TrangThai != TrangThaiThayDoi.Nhap) { TempData["err"] = "Chỉ sửa phiếu ở trạng thái Nháp."; return RedirectToAction(nameof(Details), new { id }); } }
        else
        {
            p = new PhieuThayDoi { Loai = loai, HeThongId = heThongId, DeXuatBatDau = DateTime.Today.AddDays(3).AddHours(22), DeXuatKetThuc = DateTime.Today.AddDays(3).AddHours(24) };
            if (chuanId is { } c && await db.ThayDoiChuanMaus.AsNoTracking().FirstOrDefaultAsync(m => m.Id == c && m.NguoiDuyet != null) is { } m) { p.Loai = LoaiThayDoi.Chuan; p.Ten = m.Ten; p.KeHoachThucHien = m.HuongDan; p.KeHoachKiemThu = m.KiemThu; }
        }
        await NapForm(p); ViewBag.HeThongDaChon = HeThongIds(p);
        return View(p);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Form(PhieuThayDoi input, List<int>? heThongIds)
    {
        heThongIds ??= [];
        if (string.IsNullOrWhiteSpace(input.Ten)) ModelState.AddModelError(nameof(input.Ten), "Nhập tên thay đổi.");
        if (heThongIds.Count == 0 && input.TaiSanId is null) ModelState.AddModelError("", "Chọn ít nhất một hệ thống hoặc tài sản bị tác động.");
        if (input.DeXuatBatDau is { } b && input.DeXuatKetThuc is { } k && k <= b) ModelState.AddModelError(nameof(input.DeXuatKetThuc), "Giờ kết thúc phải sau giờ bắt đầu.");
        ModelState.Remove(nameof(input.Ma));
        if (!ModelState.IsValid) { await NapForm(input); ViewBag.HeThongDaChon = heThongIds; return View(input); }
        PhieuThayDoi p;
        if (input.Id == 0)
        {
            p = new PhieuThayDoi { NguoiDeXuat = User.Identity!.Name };
            db.PhieuThayDois.Add(p);
        }
        else
        {
            p = await Nap(input.Id) ?? throw new InvalidOperationException();
            if (p.TrangThai != TrangThaiThayDoi.Nhap) return RedirectToAction(nameof(Details), new { id = p.Id });
            p.HeThongs.Clear(); p.MucPcras.Clear(); p.Checklist.Clear();
        }
        p.Loai = input.Loai; p.Ten = input.Ten.Trim(); p.MoTa = input.MoTa; p.LyDo = input.LyDo; p.TaiSanId = input.TaiSanId; p.MucRui = input.MucRui; p.LaThayDoiLon = input.LaThayDoiLon || input.Loai == LoaiThayDoi.TrienKhaiMoi;
        p.KeHoachThucHien = input.KeHoachThucHien; p.KeHoachKiemThu = input.KeHoachKiemThu; p.PhuongAnQuayLui = input.PhuongAnQuayLui;
        p.CanDowntime = input.CanDowntime; p.DeXuatBatDau = input.DeXuatBatDau; p.DeXuatKetThuc = input.DeXuatKetThuc; p.LaHeThongSaoLuu = input.LaHeThongSaoLuu; p.LaCongCuHoTroQuyetDinh = input.LaCongCuHoTroQuyetDinh;
        p.PhienBanMoi = input.PhienBanMoi; p.AnhHuongChamSoc = input.AnhHuongChamSoc; p.BienPhapAnToan = input.BienPhapAnToan; p.NguoiPhuTrachKhoa = input.NguoiPhuTrachKhoa; p.NhaThau = input.NhaThau; p.GiamSatNhaThau = input.GiamSatNhaThau;
        p.HeThongId = heThongIds.Count > 0 ? heThongIds[0] : null;
        foreach (var h in heThongIds.Distinct()) p.HeThongs.Add(new PhieuThayDoiHeThong { HeThongId = h });
        if (p.AnhHuongChamSoc) foreach (var n in Enum.GetValues<NhomPcra>()) p.MucPcras.Add(new MucPcra { Nhom = n });
        if (p.Loai == LoaiThayDoi.TrienKhaiMoi) p.TaoChecklistMacDinh();
        if (p.Id == 0) await db.LuuMoiAsync(soPhieu, "TD", so => p.Ma = so); else await db.SaveChangesAsync();
        TempData["ok"] = $"Đã lưu phiếu {p.Ma}.";
        return RedirectToAction(nameof(Details), new { id = p.Id });
    }

    private async Task<IActionResult> Thuc(int id, Func<PhieuThayDoi, Task<string>> f, bool cntt = true)
    {
        var p = await Nap(id);
        if (p is null) return NotFound();
        if (cntt && !CNTT) return Forbid();
        try { TempData["ok"] = await f(p); await db.SaveChangesAsync(); } catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> GuiDuyet(int id) => Thuc(id, async p =>
    {
        p.GuiDuyet(User.Identity!.Name!, DateTime.Now);
        var nhan = thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        await tb.GuiAsync(nhan, LoaiThongBao.ChoDuyet, $"Phiếu thay đổi {p.Ma} chờ duyệt: {p.Ten}", null, $"/PhieuThayDoi/Details/{p.Id}", boQua: User.Identity!.Name);
        return "Đã gửi duyệt.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Duyet(int id, BuocDuyetThayDoi buoc) => Thuc(id, async p =>
    {
        var tq = await td.CoToiQuanTrongAsync(p);
        var ok = buoc switch
        {
            BuocDuyetThayDoi.Cntt => User.LaDuyetCntt(thamSo),
            BuocDuyetThayDoi.LanhDao => User.IsInRole(VaiTro.LanhDao),
            _ => User.IsInRole(VaiTro.QuanTri) || (User.KhoaPhongId() is { } k && await db.HeThongThongTins.AnyAsync(h => HeThongIds(p).Contains(h.Id) && h.ChuSoHuuKhoaPhongId == k))
        };
        if (!ok) throw new DomainException($"Bạn không có quyền duyệt bước \"{buoc.Ten()}\".");
        var xong = p.Duyet(buoc, User.Identity!.Name!, tq, DateTime.Now);
        if (xong) await tb.GuiAsync([p.NguoiDeXuat], LoaiThongBao.KetQuaDuyet, $"Phiếu thay đổi {p.Ma} đã duyệt đủ cấp", null, $"/PhieuThayDoi/Details/{p.Id}");
        else if (p.BuocTiepTheo(tq) == BuocDuyetThayDoi.LanhDao) await tb.GuiVaiTroAsync(VaiTro.LanhDao, LoaiThongBao.ChoDuyet, $"Phiếu thay đổi {p.Ma} chờ Lãnh đạo duyệt: {p.Ten}", null, $"/PhieuThayDoi/Details/{p.Id}");
        else if (p.BuocTiepTheo(tq) == BuocDuyetThayDoi.ChuSoHuu) foreach (var h in await db.HeThongThongTins.Where(h => HeThongIds(p).Contains(h.Id) && h.ChuSoHuuKhoaPhongId != null).Select(h => h.ChuSoHuuKhoaPhongId!.Value).Distinct().ToListAsync())
            await tb.GuiAsync(await tb.TruongKhoaAsync(h), LoaiThongBao.ChoDuyet, $"Phiếu thay đổi {p.Ma} chờ xác nhận của chủ sở hữu nghiệp vụ", p.Ten, $"/PhieuThayDoi/Details/{p.Id}");
        return xong ? "Đã duyệt đủ cấp." : $"Đã duyệt bước {buoc.Ten()}.";
    }, cntt: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TuChoi(int id, string lyDo) => Thuc(id, async p =>
    {
        if (!User.LaDuyetCntt(thamSo) && !User.IsInRole(VaiTro.LanhDao)) throw new DomainException("Bạn không có quyền từ chối phiếu.");
        p.TuChoi(User.Identity!.Name!, lyDo);
        await tb.GuiAsync([p.NguoiDeXuat], LoaiThongBao.KetQuaDuyet, $"Phiếu thay đổi {p.Ma} bị từ chối", p.LyDoTuChoi, $"/PhieuThayDoi/Details/{p.Id}", MucDoThongBao.CanhBao);
        return "Đã từ chối phiếu.";
    }, cntt: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BatDau(int id) => Thuc(id, async p =>
    {
        p.BatDau(User.Identity!.Name!, DateTime.Now);
        if (p.LaKhanCap) await tb.GuiAsync(thamSo.Chuoi(TS.JciNguoiDuyetCntt).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), LoaiThongBao.ChoDuyet,
            $"Thay đổi KHẨN CẤP {p.Ma} đã bắt đầu — cần duyệt bổ sung trước {p.HanDuyetBoSung:dd/MM/yyyy}", p.Ten, $"/PhieuThayDoi/Details/{p.Id}", MucDoThongBao.KhanCap, boQua: User.Identity!.Name);
        return "Đã bắt đầu thực hiện." + (p.LaKhanCap ? $" Cần duyệt bổ sung trước {p.HanDuyetBoSung:dd/MM/yyyy}." : "");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> KetQua(int id, TrangThaiThayDoi ketQua, string ketQuaMoTa, string? kiemThuSau, bool coVanDe, string? nguoiXacNhanNghiepVu) => Thuc(id, async p =>
    {
        p.GhiKetQua(ketQua, ketQuaMoTa, kiemThuSau, coVanDe, nguoiXacNhanNghiepVu, User.Identity!.Name!, DateTime.Now);
        var them = await td.HoanTatAsync(p, User.Identity!.Name!);
        return $"Đã ghi kết quả: {p.TrangThai.Ten()}." + (them.Count > 0 ? " " + string.Join("; ", them) + "." : "") + (p.NenTaoHanhDong ? " Nên tạo hành động khắc phục." : "");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TaoDowntime(int id) => Thuc(id, async p => { var s = await td.TaoDowntimeAsync(p, User.Identity!.Name!); return $"Đã tạo sự kiện downtime có kế hoạch {s.Ma} và thông báo các khoa."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> LuuPcra(int id, int[] nhom, string?[] noiDung, int[] khongApDung) => Thuc(id, p =>
    {
        if (p.TrangThai is not (TrangThaiThayDoi.Nhap or TrangThaiThayDoi.ChoDuyet)) throw new DomainException("Chỉ cập nhật PCRA trước khi duyệt xong.");
        for (var i = 0; i < nhom.Length; i++)
        {
            var m = p.MucPcras.FirstOrDefault(x => (int)x.Nhom == nhom[i]); if (m is null) continue;
            m.ApDung = !khongApDung.Contains(nhom[i]); m.NoiDung = noiDung[i]?.Trim();
            if (!m.ApDung && string.IsNullOrWhiteSpace(m.NoiDung)) throw new DomainException($"Mục \"{m.Nhom.Ten()}\" không áp dụng: ghi lý do.");
        }
        return Task.FromResult("Đã lưu đánh giá rủi ro (PCRA).");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> LuuPcraKhac(int id, string? bienPhapAnToan, string? nguoiPhuTrachKhoa, string? nhaThau, string? giamSatNhaThau) => Thuc(id, p =>
    {
        p.BienPhapAnToan = bienPhapAnToan?.Trim(); p.NguoiPhuTrachKhoa = nguoiPhuTrachKhoa?.Trim(); p.NhaThau = nhaThau?.Trim(); p.GiamSatNhaThau = giamSatNhaThau?.Trim();
        return Task.FromResult("Đã lưu biện pháp an toàn và giám sát nhà thầu.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> LuuChecklist(int id, int[] dat, int[] mucId, string?[] ghiChu) => Thuc(id, p =>
    {
        for (var i = 0; i < mucId.Length; i++)
        {
            var m = p.Checklist.FirstOrDefault(x => x.Id == mucId[i]); if (m is null) continue;
            var moi = dat.Contains(mucId[i]); if (moi && !m.Dat) m.NguoiXacNhan = User.Identity!.Name; m.Dat = moi; m.GhiChu = ghiChu[i]?.Trim();
        }
        return Task.FromResult("Đã lưu checklist.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TaoHeThongMoi(int id, string ma) => Thuc(id, async p => { var h = await td.TaoHeThongMoiAsync(p, ma, User.Identity!.Name!); return $"Đã tạo hệ thống {h.Ma} (Đang triển khai). Bổ sung hồ sơ hệ thống."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> DanhGiaSau(int id, string? deDung, string? dungMucDich, string? tichHop, string? anToan, string? tacDong, string ketLuan) => Thuc(id, p =>
    {
        if (p.DanhGiaSau is null) throw new DomainException("Phiếu không có lịch đánh giá sau triển khai.");
        p.DanhGiaSau.Ghi(deDung, dungMucDich, tichHop, anToan, tacDong, ketLuan, User.Identity!.Name!, DateTime.Now);
        return Task.FromResult("Đã ghi đánh giá sau triển khai.");
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TaoHanhDong(int id, string moTa, string nguoiChiuTrachNhiem)
    {
        if (!CNTT) return Forbid();
        var p = await db.PhieuThayDois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        try { var h = await hanhDong.TaoAsync(NguonHanhDong.PhieuThayDoi, id, moTa, p.HeThongId, nguoiChiuTrachNhiem, null, LoaiHanhDong.KhacPhuc, MucDoUuTien.TrungBinh, User.Identity!.Name!); TempData["ok"] = $"Đã tạo hành động {h.Ma}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- BR-TD-04: danh mục thay đổi Chuẩn ----
    public async Task<IActionResult> Chuan() => CNTT || User.IsInRole(VaiTro.LanhDao) ? View(await db.ThayDoiChuanMaus.AsNoTracking().OrderBy(m => m.Ten).ToListAsync()) : Forbid();

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ChuanLuu(string ten, string huongDan, string? kiemThu)
    {
        if (string.IsNullOrWhiteSpace(ten) || string.IsNullOrWhiteSpace(huongDan)) { TempData["err"] = "Nhập tên và hướng dẫn thực hiện."; return RedirectToAction(nameof(Chuan)); }
        db.ThayDoiChuanMaus.Add(new ThayDoiChuanMau { Ten = ten.Trim(), HuongDan = huongDan.Trim(), KiemThu = kiemThu?.Trim() });
        await db.SaveChangesAsync(); TempData["ok"] = "Đã đề xuất mục thay đổi chuẩn — chờ Trưởng phòng CNTT duyệt.";
        return RedirectToAction(nameof(Chuan));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ChuanDuyet(int id, bool dung)
    {
        if (!User.LaDuyetCntt(thamSo)) return Forbid();
        var m = await db.ThayDoiChuanMaus.FirstOrDefaultAsync(x => x.Id == id); if (m is null) return NotFound();
        if (dung && !m.DaDuyet) { m.NguoiDuyet = User.Identity!.Name; m.NgayDuyet = DateTime.Now; } else if (!dung) m.HoatDong = !m.HoatDong;
        await db.SaveChangesAsync(); TempData["ok"] = "Đã cập nhật danh mục thay đổi chuẩn.";
        return RedirectToAction(nameof(Chuan));
    }
}
