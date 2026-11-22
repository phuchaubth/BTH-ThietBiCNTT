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

public class BaoHongVm
{
    public int TaiSanId { get; set; }
    public string MoTaSuCo { get; set; } = "";
    public MucDoUuTien UuTien { get; set; } = MucDoUuTien.TrungBinh;
    public string? NguoiYeuCau { get; set; }
    public string? DienThoaiLienHe { get; set; }
    /// <summary>Người dùng đã xem cảnh báo trùng và vẫn muốn gửi.</summary>
    public bool XacNhanTrung { get; set; }
}

public class PhieuSuaChuaController(AppDbContext db, SoPhieuService soPhieu, LichSuService lichSu, UserManager<AppUser> users, ThamSoService thamSo, ThongBaoService tb) : Controller
{
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];

    private IQueryable<PhieuSuaChua> PhamVi()
    {
        var q = db.PhieuSuaChuas.AsQueryable();
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(p => p.KhoaPhongId == k); }
        return q;
    }

    public async Task<IActionResult> Index(TrangThaiPhieuSuaChua? trangThai, int? khoa, LoaiCongViec? loai, bool cuaToi = false, bool quaHan = false, bool dangMo = true, int page = 1)
    {
        const int size = 25;
        var now = DateTime.Now;
        var q = PhamVi().Include(p => p.TaiSan).Include(p => p.KhoaPhong).AsNoTracking();
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        else if (dangMo) q = q.Where(p => !Dong.Contains(p.TrangThai));
        if (khoa is not null) q = q.Where(p => p.KhoaPhongId == khoa);
        if (loai is not null) q = q.Where(p => p.LoaiCongViec == loai);
        if (cuaToi) { var u = User.Identity!.Name; q = q.Where(p => p.KyThuatVien == u || p.NguoiTao == u); }
        if (quaHan) q = q.Where(p => p.HanXuLy < now && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh && !Dong.Contains(p.TrangThai));
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(p => p.UuTien).ThenBy(p => p.HanXuLy).Skip((page - 1) * size).Take(size).ToListAsync();
        ViewBag.DsKhoa = new SelectList(await db.KhoaPhongs.OrderBy(k => k.Ten).ToListAsync(), "Id", "Ten", khoa);
        ViewData["trangThai"] = trangThai; ViewData["loai"] = loai; ViewData["cuaToi"] = cuaToi; ViewData["quaHan"] = quaHan; ViewData["dangMo"] = dangMo;
        return View(new PagedList<PhieuSuaChua>(items, page, size, total));
    }

    /// <summary>Bảng Kanban theo trạng thái (các phiếu chưa đóng + đã nghiệm thu 7 ngày gần nhất).</summary>
    public async Task<IActionResult> Kanban(bool cuaToi = false)
    {
        var tu = DateTime.Now.AddDays(-7);
        var q = PhamVi().Include(p => p.TaiSan).Include(p => p.KhoaPhong).AsNoTracking()
            .Where(p => p.TrangThai != TrangThaiPhieuSuaChua.DaHuy && (p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu || p.NgayNghiemThu >= tu));
        if (cuaToi) { var u = User.Identity!.Name; q = q.Where(p => p.KyThuatVien == u); }
        ViewData["cuaToi"] = cuaToi;
        return View(await q.OrderByDescending(p => p.UuTien).ThenBy(p => p.HanXuLy).Take(400).ToListAsync());
    }

    public async Task<IActionResult> Create(int? taiSanId)
    {
        var vm = new BaoHongVm { TaiSanId = taiSanId ?? 0, NguoiYeuCau = User.HoTen() };
        await NapTaiSan(taiSanId);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BaoHongVm vm)
    {
        var ts = await db.TaiSans.Include(t => t.KhoaPhong).FirstOrDefaultAsync(t => t.Id == vm.TaiSanId);
        if (ts is null) ModelState.AddModelError(nameof(vm.TaiSanId), "Chọn thiết bị cần sửa.");
        else if (ts.DaThanhLy) ModelState.AddModelError(nameof(vm.TaiSanId), "Thiết bị đã thanh lý.");
        else if (!User.XemToanVien() && ts.KhoaPhongId != User.KhoaPhongId()) ModelState.AddModelError(nameof(vm.TaiSanId), "Thiết bị không thuộc khoa/phòng của bạn.");
        if (string.IsNullOrWhiteSpace(vm.MoTaSuCo)) ModelState.AddModelError(nameof(vm.MoTaSuCo), "Mô tả sự cố.");
        if (ModelState.IsValid && !vm.XacNhanTrung)
        {
            // Chống trùng: đã có yêu cầu đang mở cho cùng thiết bị?
            var trung = await db.PhieuSuaChuas.AsNoTracking()
                .Where(p => p.TaiSanId == vm.TaiSanId && p.LoaiCongViec == LoaiCongViec.SuaChua
                            && !Dong.Contains(p.TrangThai) && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh)
                .OrderByDescending(p => p.NgayYeuCau).ToListAsync();
            if (trung.Count > 0) ViewBag.Trung = trung;
        }
        if (!ModelState.IsValid || ViewBag.Trung is not null) { await NapTaiSan(vm.TaiSanId); return View(vm); }

        var now = DateTime.Now;
        var p = new PhieuSuaChua
        {
            TaiSanId = ts!.Id, KhoaPhongId = ts.KhoaPhongId,
            MoTaSuCo = vm.MoTaSuCo.Trim(), UuTien = vm.UuTien, NguoiYeuCau = vm.NguoiYeuCau, DienThoaiLienHe = vm.DienThoaiLienHe,
            NgayYeuCau = now, HanXuLy = thamSo.TinhHanSla(now, vm.UuTien, ts.KhoaPhong?.LaKhoaTrongYeu == true),
        };
        db.PhieuSuaChuas.Add(p);
        await db.LuuMoiAsync(soPhieu, "SC", so => p.SoPhieu = so);
        lichSu.Ghi(ts.Id, LoaiSuKien.SuaChua, $"Báo hỏng {p.SoPhieu}: {p.MoTaSuCo}", User.Identity?.Name);
        await tb.PhieuMoiAsync(p, ts.KhoaPhong?.Ten, $"{ts.MaTaiSan} {ts.Ten}");
        await db.SaveChangesAsync();
        TempData["ok"] = $"Đã gửi yêu cầu {p.SoPhieu}. Hạn xử lý: {p.HanXuLy:HH:mm dd/MM}.";
        return RedirectToAction(nameof(Details), new { id = p.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var p = await PhamVi().AsNoTracking()
            .Include(x => x.TaiSan).ThenInclude(t => t!.CauHinh)
            .Include(x => x.TaiSan).ThenInclude(t => t!.LoaiThietBi)
            .Include(x => x.KhoaPhong).Include(x => x.NhaCungCap).Include(x => x.KeHoachBaoTri)
            .Include(x => x.VatTus).ThenInclude(v => v.VatTu)
            .Include(x => x.BinhLuans.OrderBy(b => b.ThoiGian))
            .FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        ViewBag.KTV = new SelectList((await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Concat(await users.GetUsersInRoleAsync(VaiTro.QuanTri))
            .Select(u => new { u.UserName, Ten = $"{u.HoTen} ({u.UserName})" }), "UserName", "Ten", p.KyThuatVien);
        ViewBag.Ncc = new SelectList(await db.NhaCungCaps.OrderBy(n => n.Ten).ToListAsync(), "Id", "Ten", p.NhaCungCapId);
        ViewBag.VatTu = new SelectList(await db.VatTus.Where(v => v.TonKho > 0).OrderBy(v => v.Ten)
            .Select(v => new { v.Id, Ten = v.Ma + " - " + v.Ten + " (tồn " + ((double)v.TonKho).ToString() + " " + v.DonViTinh + ")" }).ToListAsync(), "Id", "Ten");
        ViewBag.LichSuThietBi = await db.PhieuSuaChuas.AsNoTracking().Where(x => x.TaiSanId == p.TaiSanId && x.Id != p.Id)
            .OrderByDescending(x => x.NgayYeuCau).Take(5).ToListAsync();
        return View(p);
    }

    // ===== Thao tác theo luồng =====

    private async Task<IActionResult> ThucHien(int id, Func<PhieuSuaChua, string> action, bool chiCNTT = true)
    {
        if (chiCNTT && !User.LaCNTT()) return Forbid();
        var p = await PhamVi().Include(x => x.TaiSan).ThenInclude(t => t!.LoaiThietBi)
                    .Include(x => x.KhoaPhong).Include(x => x.VatTus).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        var truoc = p.TrangThai;
        var kyThuatVienTruoc = p.KyThuatVien;
        try
        {
            var msg = action(p);
            if (p.TrangThai != truoc)
            {
                db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = p.Id, ThoiGian = DateTime.Now, NguoiViet = User.Identity!.Name!,
                    LaHeThong = true, NoiDung = $"{truoc.Ten()} → {p.TrangThai.Ten()}" });
                DongBoTrangThaiTaiSan(p);
                // Thông báo theo chuyển trạng thái
                switch (p.TrangThai)
                {
                    case TrangThaiPhieuSuaChua.DaTiepNhan: await tb.GiaoViecAsync(p, User.Identity?.Name); break;
                    case TrangThaiPhieuSuaChua.HoanThanh: await tb.ChoNghiemThuAsync(p); break;
                    case TrangThaiPhieuSuaChua.DangXuLy when truoc == TrangThaiPhieuSuaChua.HoanThanh: await tb.TraLaiAsync(p, User.Identity?.Name); break;
                }
            }
            else if (p.TrangThai == TrangThaiPhieuSuaChua.DaTiepNhan && kyThuatVienTruoc != p.KyThuatVien) await tb.GiaoViecAsync(p, User.Identity?.Name);
            await db.SaveChangesAsync();
            TempData["ok"] = msg;
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    private void DongBoTrangThaiTaiSan(PhieuSuaChua p)
    {
        if (p.TaiSan is not { } ts || ts.DaThanhLy || ts.TrangThai == TrangThaiTaiSan.ChoThanhLy) return;
        var user = User.Identity?.Name;
        switch (p.TrangThai)
        {
            case TrangThaiPhieuSuaChua.DangXuLy or TrangThaiPhieuSuaChua.ChoLinhKien or TrangThaiPhieuSuaChua.GuiHang
                when p.LoaiCongViec == LoaiCongViec.SuaChua:
                ts.TrangThai = TrangThaiTaiSan.DangSuaChua; break;
            case TrangThaiPhieuSuaChua.HoanThanh:
                ts.TrangThai = TrangThaiTaiSan.DangSuDung;
                lichSu.Ghi(ts.Id, p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy ? LoaiSuKien.BaoTri : LoaiSuKien.SuaChua,
                    $"{p.SoPhieu} hoàn thành: {p.BienPhapKhacPhuc} (chi phí {p.TongChiPhi:N0}đ)", user);
                break;
        }
    }

    /// <summary>QĐ-04: hàm dời hạn SLA khi hết tạm dừng, theo lịch của khoa phiếu.</summary>
    private PhieuSuaChua.DoiHanSla DoiHan(PhieuSuaChua p)
    {
        var sla = thamSo.Sla();
        var h24 = SlaCalculator.Tinh24h(p.KhoaPhong?.LaKhoaTrongYeu == true, sla);
        return (han, bd, tiep) => SlaCalculator.DoiHanSauTamDung(han, bd, tiep, h24, sla);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TiepNhan(int id, string kyThuatVien, HinhThucXuLy hinhThuc, int? nhaCungCapId) =>
        ThucHien(id, p => { p.TiepNhan(kyThuatVien, hinhThuc, DateTime.Now); p.NhaCungCapId = nhaCungCapId; return $"Đã giao cho {kyThuatVien}."; });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> BatDau(int id) => ThucHien(id, p =>
    {
        var dangDung = p.DangTamDung;
        p.BatDauXuLy(DateTime.Now, DoiHan(p));
        return dangDung ? $"Tiếp tục xử lý. Hạn SLA mới: {p.HanXuLy:HH:mm dd/MM}." : "Đã bắt đầu xử lý.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TamDung(int id, TrangThaiPhieuSuaChua lyDo) => ThucHien(id, p =>
    {
        var dung = thamSo.BatTat(TS.SlaTamDungKhiCho);
        p.TamDung(lyDo, DateTime.Now, dung);
        return dung ? "Đã chuyển trạng thái, đồng hồ SLA tạm dừng." : "Đã chuyển trạng thái.";
    });

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> HoanThanh(int id, string? nguyenNhan, string? bienPhap, decimal gioCong, decimal chiPhiThueNgoai) =>
        ThucHien(id, p => { p.HoanThanh(nguyenNhan, bienPhap, gioCong, chiPhiThueNgoai, DateTime.Now, DoiHan(p)); return "Đã hoàn thành, chờ khoa nghiệm thu."; });

    private void KiemTraQuyenNghiemThu(PhieuSuaChua p)
    {
        if (!p.DuocNghiemThu(User.Identity?.Name, User.IsInRole(VaiTro.TruongKhoa), User.KhoaPhongId()))
            throw new DomainException("Chỉ người báo hỏng hoặc Trưởng khoa/phòng được nghiệm thu (KTV xử lý phiếu không tự nghiệm thu).");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> NghiemThu(int id, int? danhGia, string? yKien) =>
        ThucHien(id, p => { KiemTraQuyenNghiemThu(p); p.NghiemThu(User.HoTen(), danhGia, yKien, DateTime.Now); return "Đã nghiệm thu. Cảm ơn bạn!"; }, chiCNTT: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> TraLai(int id, string yKien) =>
        ThucHien(id, p => { KiemTraQuyenNghiemThu(p); if (string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Nhập lý do chưa đạt."); p.TraLai(yKien); return "Đã trả lại cho KTV."; }, chiCNTT: false);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Huy(int id, string lyDo) =>
        ThucHien(id, p =>
        {
            if (!User.LaCNTT() && p.NguoiTao != User.Identity?.Name) throw new DomainException("Chỉ người tạo hoặc CNTT được huỷ.");
            if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do huỷ.");
            p.Huy(lyDo); return "Đã huỷ phiếu.";
        }, chiCNTT: false);

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThemVatTu(int id, int vatTuId, decimal soLuong)
    {
        var p = await db.PhieuSuaChuas.FirstOrDefaultAsync(x => x.Id == id);
        var v = await db.VatTus.FindAsync(vatTuId);
        if (p is null || v is null) return NotFound();
        try
        {
            if (p.DaDong || p.TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Phiếu đã hoàn thành.");
            v.XuatKho(soLuong, thamSo.BatTat(TS.KhoChoXuatAm));
            db.ChiTietVatTuSuaChuas.Add(new ChiTietVatTuSuaChua { PhieuSuaChuaId = id, VatTuId = vatTuId, SoLuong = soLuong, DonGia = v.DonGia });
            db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = id, ThoiGian = DateTime.Now, NguoiViet = User.Identity!.Name!, LaHeThong = true,
                NoiDung = $"Xuất kho {soLuong} {v.DonViTinh} {v.Ten}" });
            await db.SaveChangesAsync();
            if (v.DuoiTonToiThieu) { await tb.VatTuDuoiDinhMucAsync(v); await db.SaveChangesAsync(); }
            TempData["ok"] = v.DuoiTonToiThieu ? $"Đã xuất kho. {v.Ten} còn {v.TonKho:0.##}, dưới định mức — đã báo Phòng CNTT." : "Đã xuất kho vật tư.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BinhLuan(int id, string noiDung)
    {
        var phieu = await PhamVi().FirstOrDefaultAsync(p => p.Id == id);
        if (phieu is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(noiDung))
        {
            db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = id, ThoiGian = DateTime.Now, NguoiViet = User.Identity!.Name!, NoiDung = noiDung.Trim() });
            await tb.BinhLuanAsync(phieu, User.Identity!.Name!, noiDung.Trim());
            await db.SaveChangesAsync();
        }
        return Redirect(Url.Action(nameof(Details), new { id }) + "#trao-doi");
    }

    /// <summary>In phiếu yêu cầu / biên bản sửa chữa.</summary>
    public async Task<IActionResult> InPhieu(int id)
    {
        var p = await PhamVi().AsNoTracking().Include(x => x.TaiSan).Include(x => x.KhoaPhong)
            .Include(x => x.VatTus).ThenInclude(v => v.VatTu).FirstOrDefaultAsync(x => x.Id == id);
        return p is null ? NotFound() : View(p);
    }

    private async Task NapTaiSan(int? chon)
    {
        var q = db.TaiSans.Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy);
        if (!User.XemToanVien()) { var k = User.KhoaPhongId(); q = q.Where(t => t.KhoaPhongId == k); }
        ViewBag.TaiSan = new SelectList(await q.OrderBy(t => t.MaTaiSan)
            .Select(t => new { t.Id, Ten = t.MaTaiSan + " - " + t.Ten + (t.KhoaPhong != null ? " [" + t.KhoaPhong.Ten + "]" : "") }).ToListAsync(), "Id", "Ten", chon);
    }
}
