using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

public record ViecChoDuyet(string Loai, string So, string NoiDung, string Url, DateTime ThoiGian);

/// <summary>Trang chủ điện thoại / PWA (start_url = /m): ô chức năng theo vai trò, quét QR, việc chờ duyệt.</summary>
[Route("m")]
public class DiDongController(AppDbContext db, ThamSoService ts, MuaSamService ms) : Controller
{
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var menu = DiDong.Menu(User, ts);
        var them = DiDong.DanhMuc.Where(m => m.ChoPhep(User) && menu.All(x => x.Ma != m.Ma) && menu.All(x => x.Url != m.Url)).ToList();
        var u = User.Identity!.Name;
        var dem = new Dictionary<string, int>();
        foreach (var m in menu.Concat(them).Select(m => m.Ma))
            dem[m] = m switch
            {
                "yeucau" => await db.PhieuSuaChuas.CountAsync(p => p.NguoiTao == u && !Dong.Contains(p.TrangThai)),
                "viec" => await db.PhieuSuaChuas.CountAsync(p => p.KyThuatVien == u && !Dong.Contains(p.TrangThai) && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh),
                "thongbao" => await db.ThongBaos.CountAsync(t => t.NguoiNhan == u && !t.DaDoc),
                "kiemke" => await db.DotKiemKes.CountAsync(d => d.TrangThai != TrangThaiKiemKe.DaKhoaSo),
                "duyet" => (await ChoDuyetAsync()).Count,
                _ => 0
            };
        ViewBag.Them = them; ViewBag.Dem = dem;
        return View(menu);
    }

    [HttpGet("Quet")]
    public IActionResult Quet() => View();

    /// <summary>Quét tem → mở form báo hỏng đã chọn sẵn thiết bị.</summary>
    [HttpGet("BaoHong")]
    public async Task<IActionResult> BaoHong(string ma)
    {
        var id = await db.TaiSans.Where(t => t.MaTaiSan == (ma ?? "").Trim().ToUpper()).Select(t => (int?)t.Id).FirstOrDefaultAsync();
        if (id is null) { TempData["err"] = $"Không tìm thấy tài sản mã {ma}."; return RedirectToAction(nameof(Quet)); }
        return Redirect($"/PhieuSuaChua/Create?taiSanId={id}");
    }

    [HttpGet("ChoDuyet")]
    public async Task<IActionResult> ChoDuyet() => View(await ChoDuyetAsync());

    /// <summary>Gom các việc đang chờ chính người dùng này xử lý/duyệt.</summary>
    private async Task<List<ViecChoDuyet>> ChoDuyetAsync()
    {
        var u = User.Identity!.Name!; var kq = new List<ViecChoDuyet>();
        var khoa = User.KhoaPhongId();
        // Nghiệm thu: người báo hoặc trưởng khoa của khoa có phiếu
        var nt = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan)
            .Where(p => p.TrangThai == TrangThaiPhieuSuaChua.HoanThanh && (p.NguoiTao == u || (User.IsInRole(VaiTro.TruongKhoa) && p.KhoaPhongId == khoa)))
            .OrderBy(p => p.NgayHoanThanh).Take(50).ToListAsync();
        kq.AddRange(nt.Select(p => new ViecChoDuyet("Nghiệm thu sửa chữa", p.SoPhieu, $"{p.TaiSan?.MaTaiSan} · {p.BienPhapKhacPhuc}", $"/PhieuSuaChua/Details/{p.Id}", p.NgayHoanThanh ?? p.NgayYeuCau)));
        var dc = await db.PhieuDieuChuyens.AsNoTracking().Include(p => p.TuKhoaPhong).Include(p => p.DenKhoaPhong)
            .Where(p => p.TrangThai == TrangThaiDieuChuyen.ChoDuyet).ToListAsync();
        kq.AddRange(dc.Where(p => DieuChuyenController.CoQuyenDuyet(p, User))
            .Select(p => new ViecChoDuyet("Điều chuyển", p.SoPhieu, $"{p.TuKhoaPhong?.Ten} → {p.TenNoiDen}", $"/DieuChuyen/Details/{p.Id}", p.NgayTao)));
        if (User.LaCNTT() || User.IsInRole(VaiTro.LanhDao))
        {
            foreach (var x in await db.DeXuatMuaSams.Include(x => x.Dongs).Where(x => x.TrangThai == TrangThaiDeXuat.ChoDuyet).ToListAsync())
                if (await ms.DuocDuyetAsync(x, u)) kq.Add(new("Đề xuất mua sắm", x.SoPhieu, $"{x.TieuDe} · {x.TongDuToan:N0} đ", $"/MuaSam/DeXuat/{x.Id}", x.NgayTao));
            foreach (var d in await db.DonMuaHangs.Include(x => x.Dongs).Include(x => x.NhaCungCap).Where(x => x.TrangThai == TrangThaiDonHang.Nhap).ToListAsync())
                if (await ms.DuocDatHangAsync(d, u)) kq.Add(new("Xác nhận đặt hàng", d.SoPhieu, $"{d.NhaCungCap?.Ten} · {d.TongTien:N0} đ", $"/MuaSam/DonHang/{d.Id}", d.NgayTao));
        }
        if (User.LaDuyet())
        {
            kq.AddRange((await db.DeXuatThanhLys.AsNoTracking().Where(x => x.TrangThai == TrangThaiThanhLy.DaDanhGia).ToListAsync())
                .Select(x => new ViecChoDuyet("Quyết định thanh lý", x.SoPhieu, "Hội đồng đã đánh giá — chờ quyết định", $"/ThanhLy/Details/{x.Id}", x.NgayTao)));
            kq.AddRange((await db.KhoanMucNganSachs.AsNoTracking().Where(k => k.TrangThai == TrangThaiNganSach.DuThao && k.NguoiTao != u).ToListAsync())
                .Select(k => new ViecChoDuyet("Dự toán ngân sách", k.Ma, $"{k.Ten} · {k.DuToan:N0} đ", $"/NganSach/ChiTiet/{k.Id}", k.NgayTao)));
            kq.AddRange((await db.DieuChinhNganSachs.AsNoTracking().Where(d => !d.DaDuyet && !d.TuChoi && d.NguoiDeNghi != u).ToListAsync())
                .Select(d => new ViecChoDuyet("Điều chỉnh ngân sách", $"#{d.Id}", $"{d.SoTien:+#,0;-#,0} đ · {d.LyDo}", $"/NganSach/ChiTiet/{d.KhoanMucNganSachId}", d.NgayDeNghi)));
        }
        if (User.LaCNTT())
            kq.AddRange((await db.DeXuatThanhLys.AsNoTracking().Where(x => x.TrangThai == TrangThaiThanhLy.ChoHoiDong).ToListAsync())
                .Select(x => new ViecChoDuyet("Đánh giá thanh lý", x.SoPhieu, "Chờ hội đồng đánh giá", $"/ThanhLy/Details/{x.Id}", x.NgayTao)));
        if (User.IsInRole(VaiTro.QuanTri))
            kq.AddRange((await db.PhieuKhos.AsNoTracking().Where(p => p.TrangThai == TrangThaiPhieuKho.ChoDuyet && p.NguoiTao != u).ToListAsync())
                .Select(p => new ViecChoDuyet("Điều chỉnh kho", p.SoPhieu, p.DienGiai ?? "", $"/Kho/ChiTiet/{p.Id}", p.NgayTao)));
        return kq.OrderBy(x => x.ThoiGian).ToList();
    }
}
