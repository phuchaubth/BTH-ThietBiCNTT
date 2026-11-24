using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers.Api;

public record TaiSanDto(int Id, string MaTaiSan, string Ten, string? Loai, string? Hang, string? Model, string? SoSerial, string TrangThai,
    int? KhoaPhongId, string? KhoaPhong, string? ViTri, string? NguoiSuDung, decimal NguyenGia, DateOnly? NgayMua, DateOnly? HanBaoHanh, long? ChiSoBoDem,
    CauHinhDto? CauHinh = null);
public record CauHinhDto(string? CPU, string? RAM, string? OCung, string? HeDieuHanh, string? DiaChiIP, string? DiaChiMAC, string? Hostname, string? PhanMem, DateTime? NgayCapNhat);
public record PhieuDto(int Id, string SoPhieu, string Loai, string? MaTaiSan, string? KhoaPhong, string TrangThai, string UuTien, string MoTa,
    string? NguoiYeuCau, DateTime NgayYeuCau, DateTime HanXuLy, string? KyThuatVien, DateTime? NgayHoanThanh, bool QuaHan);

public class CapNhatTaiSanApi
{
    [StringLength(100)] public string? NguoiSuDung { get; set; }
    public int? ViTriId { get; set; }
    [Range(0, long.MaxValue)] public long? ChiSoBoDem { get; set; }
    [StringLength(1000)] public string? GhiChu { get; set; }
}

public class TaoYeuCauApi
{
    /// <summary>Mã tài sản (in trên tem QR).</summary>
    [Required] public string MaTaiSan { get; set; } = "";
    [Required, StringLength(2000)] public string MoTa { get; set; } = "";
    /// <summary>KhanCap | Cao | TrungBinh | Thap</summary>
    public string UuTien { get; set; } = "TrungBinh";
    [StringLength(100)] public string? NguoiYeuCau { get; set; }
    [StringLength(20)] public string? DienThoai { get; set; }
    /// <summary>true: vẫn tạo dù thiết bị đang có yêu cầu mở.</summary>
    public bool BoQuaTrung { get; set; }
}

/// <summary>Tài sản & danh mục.</summary>
[Route("api/v1")]
public class TaiSanApiController(AppDbContext db, LichSuService lichSu) : ApiV1Controller
{
    private static TaiSanDto Dto(TaiSan t, bool cauHinh = false) => new(t.Id, t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten, t.Hang, t.Model, t.SoSerial, t.TrangThai.ToString(),
        t.KhoaPhongId, t.KhoaPhong?.Ten, t.ViTri?.Ten, t.NguoiSuDung, t.NguyenGia, t.NgayMua, t.HanBaoHanh, t.ChiSoBoDem,
        cauHinh && t.CauHinh is { } c ? new CauHinhDto(c.CPU, c.RAM, c.OCung, c.HeDieuHanh, c.DiaChiIP, c.DiaChiMAC, c.Hostname, c.PhanMem, c.NgayCapNhat) : null);

    /// <summary>Danh sách tài sản (phân trang, tối đa 500/trang).</summary>
    [HttpGet("taisan"), Authorize(Policy = "api:" + PhamViApi.TaiSanDoc)]
    public async Task<IActionResult> DanhSach(string? q, int? khoaPhongId, string? trangThai, DateTime? capNhatTu, int page = 1, int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500); page = Math.Max(1, page);
        var x = db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.ViTri).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) x = x.Where(t => t.MaTaiSan.Contains(q) || t.Ten.Contains(q) || (t.SoSerial ?? "").Contains(q));
        if (khoaPhongId is not null) x = x.Where(t => t.KhoaPhongId == khoaPhongId);
        if (trangThai is not null)
        {
            if (!Enum.TryParse<TrangThaiTaiSan>(trangThai, true, out var tt)) return Loi(400, $"trangThai không hợp lệ: {string.Join(", ", Enum.GetNames<TrangThaiTaiSan>())}");
            x = x.Where(t => t.TrangThai == tt);
        }
        if (capNhatTu is not null) x = x.Where(t => (t.NgayCapNhat ?? t.NgayTao) >= capNhatTu);
        var total = await x.CountAsync();
        var ds = await x.OrderBy(t => t.MaTaiSan).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(Trang(page, pageSize, total, ds.Select(t => Dto(t))));
    }

    [HttpGet("taisan/{ma}"), Authorize(Policy = "api:" + PhamViApi.TaiSanDoc)]
    public async Task<IActionResult> ChiTiet(string ma)
    {
        var t = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.ViTri).Include(t => t.CauHinh)
            .FirstOrDefaultAsync(t => t.MaTaiSan == ma);
        return t is null ? Loi(404, $"Không có tài sản {ma}.") : Ok(Dto(t, true));
    }

    /// <summary>Cập nhật có kiểm soát: người sử dụng, vị trí, chỉ số bộ đếm (số trang in…), ghi chú.</summary>
    [HttpPatch("taisan/{ma}"), Authorize(Policy = "api:" + PhamViApi.TaiSanGhi)]
    public async Task<IActionResult> CapNhat(string ma, CapNhatTaiSanApi body)
    {
        var t = await db.TaiSans.FirstOrDefaultAsync(t => t.MaTaiSan == ma);
        if (t is null) return Loi(404, $"Không có tài sản {ma}.");
        if (t.DaThanhLy) return Loi(409, "Tài sản đã thanh lý.");
        var doi = new List<string>();
        if (body.NguoiSuDung is not null && body.NguoiSuDung != t.NguoiSuDung) { doi.Add($"người sử dụng: {t.NguoiSuDung} → {body.NguoiSuDung}"); t.NguoiSuDung = body.NguoiSuDung; }
        if (body.ViTriId is not null && body.ViTriId != t.ViTriId)
        {
            if (!await db.ViTris.AnyAsync(v => v.Id == body.ViTriId)) return Loi(400, "viTriId không tồn tại.");
            doi.Add($"vị trí → #{body.ViTriId}"); t.ViTriId = body.ViTriId;
        }
        if (body.ChiSoBoDem is not null && body.ChiSoBoDem != t.ChiSoBoDem)
        {
            if (body.ChiSoBoDem < t.ChiSoBoDem) return Loi(400, $"Chỉ số mới ({body.ChiSoBoDem}) nhỏ hơn chỉ số hiện tại ({t.ChiSoBoDem}).");
            doi.Add($"chỉ số: {t.ChiSoBoDem} → {body.ChiSoBoDem}"); t.ChiSoBoDem = body.ChiSoBoDem;
        }
        if (body.GhiChu is not null && body.GhiChu != t.GhiChu) { doi.Add("ghi chú"); t.GhiChu = body.GhiChu; }
        if (doi.Count > 0) { lichSu.Ghi(t.Id, LoaiSuKien.CapNhat, "Qua API: " + string.Join("; ", doi), User.Identity?.Name); await db.SaveChangesAsync(); }
        return Ok(new { capNhat = doi });
    }

    [HttpGet("khoaphong"), Authorize(Policy = "api:" + PhamViApi.TaiSanDoc)]
    public async Task<IActionResult> KhoaPhong() =>
        Ok(await db.KhoaPhongs.AsNoTracking().OrderBy(k => k.Ma).Select(k => new { k.Id, k.Ma, k.Ten, k.LaKhoaTrongYeu, k.DangHoatDong }).ToListAsync());

    [HttpGet("loaithietbi"), Authorize(Policy = "api:" + PhamViApi.TaiSanDoc)]
    public async Task<IActionResult> LoaiThietBi() =>
        Ok(await db.LoaiThietBis.AsNoTracking().OrderBy(k => k.Ma).Select(k => new { k.Id, k.Ma, k.Ten }).ToListAsync());

    [HttpGet("vitri"), Authorize(Policy = "api:" + PhamViApi.TaiSanDoc)]
    public async Task<IActionResult> ViTri() =>
        Ok(await db.ViTris.AsNoTracking().OrderBy(k => k.Ten).Select(k => new { k.Id, k.Ten, k.ChaId }).ToListAsync());
}

/// <summary>Yêu cầu sửa chữa / phiếu công việc.</summary>
[Route("api/v1")]
public class PhieuApiController(AppDbContext db, ThamSoService thamSo, SoPhieuService soPhieu, LichSuService lichSu, ThongBaoService tb) : ApiV1Controller
{
    private static readonly TrangThaiPhieuSuaChua[] Dong = [TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy];

    private static PhieuDto Dto(PhieuSuaChua p, DateTime now) => new(p.Id, p.SoPhieu, p.LoaiCongViec.ToString(), p.TaiSan?.MaTaiSan, p.KhoaPhong?.Ten,
        p.TrangThai.ToString(), p.UuTien.ToString(), p.MoTaSuCo, p.NguoiYeuCau, p.NgayYeuCau, p.HanXuLy, p.KyThuatVien, p.NgayHoanThanh, p.QuaHan(now));

    /// <summary>Tạo yêu cầu sửa chữa từ hệ thống khác (VD: HIS, tổng đài). Trả 409 nếu thiết bị đang có yêu cầu mở (trừ khi boQuaTrung).</summary>
    [HttpPost("yeucau"), Authorize(Policy = "api:" + PhamViApi.YeuCauTao)]
    public async Task<IActionResult> Tao(TaoYeuCauApi body)
    {
        if (!Enum.TryParse<MucDoUuTien>(body.UuTien, true, out var uuTien)) return Loi(400, $"uuTien không hợp lệ: {string.Join(", ", Enum.GetNames<MucDoUuTien>())}");
        var ts = await db.TaiSans.Include(t => t.KhoaPhong).FirstOrDefaultAsync(t => t.MaTaiSan == body.MaTaiSan);
        if (ts is null) return Loi(404, $"Không có tài sản {body.MaTaiSan}.");
        if (ts.DaThanhLy) return Loi(409, "Tài sản đã thanh lý.");
        if (!body.BoQuaTrung)
        {
            var trung = await db.PhieuSuaChuas.AsNoTracking().Where(p => p.TaiSanId == ts.Id && p.LoaiCongViec == LoaiCongViec.SuaChua
                && !Dong.Contains(p.TrangThai) && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh).Select(p => p.SoPhieu).ToListAsync();
            if (trung.Count > 0) return StatusCode(409, new { loi = "Thiết bị đang có yêu cầu chưa xử lý xong.", phieuDangMo = trung });
        }
        var now = DateTime.Now;
        var p = new PhieuSuaChua
        {
            TaiSanId = ts.Id, KhoaPhongId = ts.KhoaPhongId, MoTaSuCo = body.MoTa.Trim(), UuTien = uuTien,
            NguoiYeuCau = body.NguoiYeuCau ?? User.Identity?.Name, DienThoaiLienHe = body.DienThoai,
            NgayYeuCau = now, HanXuLy = thamSo.TinhHanSla(now, uuTien, ts.KhoaPhong?.LaKhoaTrongYeu == true),
        };
        db.PhieuSuaChuas.Add(p);
        await db.LuuMoiAsync(soPhieu, "SC", so => p.SoPhieu = so);
        lichSu.Ghi(ts.Id, LoaiSuKien.SuaChua, $"Báo hỏng {p.SoPhieu} (qua API): {p.MoTaSuCo}", User.Identity?.Name);
        await tb.PhieuMoiAsync(p, ts.KhoaPhong?.Ten, $"{ts.MaTaiSan} {ts.Ten}");
        await db.SaveChangesAsync();
        p.TaiSan = ts;
        return Created($"/api/v1/phieusuachua/{p.SoPhieu}", Dto(p, now));
    }

    [HttpGet("phieusuachua"), Authorize(Policy = "api:" + PhamViApi.CongViecDoc)]
    public async Task<IActionResult> DanhSach(string? trangThai, string? maTaiSan, int? khoaPhongId, DateTime? tu, DateTime? den, bool dangMo = false, int page = 1, int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500); page = Math.Max(1, page);
        var x = db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Include(p => p.KhoaPhong).AsQueryable();
        if (trangThai is not null)
        {
            if (!Enum.TryParse<TrangThaiPhieuSuaChua>(trangThai, true, out var tt)) return Loi(400, $"trangThai không hợp lệ: {string.Join(", ", Enum.GetNames<TrangThaiPhieuSuaChua>())}");
            x = x.Where(p => p.TrangThai == tt);
        }
        if (dangMo) x = x.Where(p => !Dong.Contains(p.TrangThai));
        if (maTaiSan is not null) x = x.Where(p => p.TaiSan!.MaTaiSan == maTaiSan);
        if (khoaPhongId is not null) x = x.Where(p => p.KhoaPhongId == khoaPhongId);
        if (tu is not null) x = x.Where(p => p.NgayYeuCau >= tu);
        if (den is not null) x = x.Where(p => p.NgayYeuCau < den);
        var total = await x.CountAsync();
        var now = DateTime.Now;
        var ds = await x.OrderByDescending(p => p.NgayYeuCau).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(Trang(page, pageSize, total, ds.Select(p => Dto(p, now))));
    }

    [HttpGet("phieusuachua/{soPhieu}"), Authorize(Policy = "api:" + PhamViApi.CongViecDoc)]
    public async Task<IActionResult> ChiTiet(string soPhieu)
    {
        var p = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Include(p => p.KhoaPhong).FirstOrDefaultAsync(p => p.SoPhieu == soPhieu);
        return p is null ? Loi(404, $"Không có phiếu {soPhieu}.") : Ok(Dto(p, DateTime.Now));
    }
}

/// <summary>Kho, đơn mua hàng, báo cáo KPI (chỉ đọc).</summary>
[Route("api/v1")]
public class KhoBaoCaoApiController(AppDbContext db, BaoCaoService baoCao) : ApiV1Controller
{
    [HttpGet("tonkho"), Authorize(Policy = "api:" + PhamViApi.KhoDoc)]
    public async Task<IActionResult> TonKho(int? khoId)
    {
        var vt = await db.VatTus.AsNoTracking().OrderBy(v => v.Ma).ToListAsync();
        var ton = await db.TonKhoVatTus.AsNoTracking().Include(t => t.Kho).Where(t => khoId == null || t.KhoId == khoId).ToListAsync();
        return Ok(vt.Select(v => new
        {
            v.Id, v.Ma, v.Ten, v.DonViTinh, v.DonGia, tongTon = v.TonKho, v.TonToiThieu, duoiDinhMuc = v.DuoiTonToiThieu,
            theoKho = ton.Where(t => t.VatTuId == v.Id).Select(t => new { t.KhoId, kho = t.Kho?.Ma, t.SoLuong })
        }));
    }

    [HttpGet("donhang"), Authorize(Policy = "api:" + PhamViApi.KhoDoc)]
    public async Task<IActionResult> DonHang(string? trangThai, int page = 1, int pageSize = 100)
    {
        pageSize = Math.Clamp(pageSize, 1, 500); page = Math.Max(1, page);
        var x = db.DonMuaHangs.AsNoTracking().Include(d => d.NhaCungCap).Include(d => d.Dongs).Include(d => d.PhieuNhans).AsQueryable();
        if (trangThai is not null)
        {
            if (!Enum.TryParse<TrangThaiDonHang>(trangThai, true, out var tt)) return Loi(400, $"trangThai không hợp lệ: {string.Join(", ", Enum.GetNames<TrangThaiDonHang>())}");
            x = x.Where(d => d.TrangThai == tt);
        }
        var total = await x.CountAsync();
        var ds = await x.OrderByDescending(d => d.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(Trang(page, pageSize, total, ds.Select(d => new
        {
            d.Id, d.SoPhieu, nhaCungCap = d.NhaCungCap?.Ten, trangThai = d.TrangThai.ToString(), d.NgayDatHang, d.HanGiao, d.TienHang, d.TienThue, d.TongTien,
            daNhan = d.PhieuNhans.Sum(p => p.GiaTri), quaHan = d.TreHan(DateOnly.FromDateTime(DateTime.Today))
        })));
    }

    /// <summary>KPI vận hành trong kỳ (mặc định 30 ngày gần nhất): SLA, MTTR, dừng máy, chi phí.</summary>
    [HttpGet("baocao/kpi"), Authorize(Policy = "api:" + PhamViApi.BaoCaoDoc)]
    public async Task<IActionResult> Kpi(DateTime? tu, DateTime? den, int? khoaPhongId)
    {
        var d = (den ?? DateTime.Today).Date.AddDays(1);
        var t = (tu ?? d.AddDays(-30)).Date;
        if (t >= d || (d - t).TotalDays > 731) return Loi(400, "Kỳ báo cáo không hợp lệ (tối đa 2 năm).");
        var r = await baoCao.LapAsync(t, d, khoaPhongId);
        object Dong(DongSla x) => new { nhom = x.Nhom, soPhieu = x.SoPhieu, daXong = x.DaXong, dungHan = x.DungHan, tyLeDungHan = Math.Round(x.TyLeDungHan, 1),
            mttrGio = x.MttrGio is { } m ? Math.Round(m, 1) : (double?)null, chiPhi = x.ChiPhi, dungMayGio = Math.Round(x.DungMayGio, 1) };
        return Ok(new { tu = r.Tu, den = r.Den, phamVi = r.PhamVi, tong = Dong(r.Tong), theoThang = r.TheoThang.Select(Dong), theoKhoa = r.TheoKhoa.Select(Dong),
            thietBiHongNhieu = r.ThietBiHongNhieu.Take(10).Select(x => new { x.MaTaiSan, x.Ten, x.Khoa, x.SoLanHong, x.ChiPhi, x.MtbfNgay }) });
    }
}
