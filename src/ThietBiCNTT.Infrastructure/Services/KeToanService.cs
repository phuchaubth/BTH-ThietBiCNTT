using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

public record DongGhiTang(string MaTaiSan, string Ten, string? Loai, string NguonVon, decimal NguyenGia, DateOnly? NgayMua, DateOnly? NgaySuDung,
    string? KhoaPhong, string? NhaCungCap, string? SoChungTu);
public record DongGhiGiam(string MaTaiSan, string Ten, string? Loai, decimal NguyenGia, decimal GiaTriConLai, string SoDeXuat, string? SoQuyetDinh,
    DateOnly? NgayQuyetDinh, DateTime? NgayHoanTat, string? HinhThuc, decimal GiaTriThuHoiPhanBo);
public record DongThucChi(DateTime Ngay, string Nguon, string SoChungTu, string NoiDung, string? DoiTuong, decimal SoTien, string? KhoanMuc);
public record SoLieuKeToan(DateTime Tu, DateTime Den, List<DongGhiTang> GhiTang, List<DongGhiGiam> GhiGiam, List<DongThucChi> ThucChi);

/// <summary>
/// Số liệu chuyển Phòng TCKT / phần mềm kế toán tài sản (IF-08): ghi tăng (tài sản mua mới trong kỳ),
/// ghi giảm (thanh lý hoàn tất trong kỳ), thực chi (nhận hàng, chi ghi tay ngân sách, sửa chữa thuê ngoài).
/// Kỳ: [tu, den) — den là ngày sau ngày cuối.
/// </summary>
public class KeToanService(AppDbContext db)
{
    public async Task<SoLieuKeToan> LapAsync(DateTime tu, DateTime den)
    {
        var d0 = DateOnly.FromDateTime(tu); var d1 = DateOnly.FromDateTime(den);
        var tang = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.NhaCungCap)
            .Where(t => (t.NgayMua != null && t.NgayMua >= d0 && t.NgayMua < d1) || (t.NgayMua == null && t.NgayTao >= tu && t.NgayTao < den))
            .OrderBy(t => t.NgayMua).ThenBy(t => t.MaTaiSan).ToListAsync();
        var giam = await db.ChiTietThanhLys.AsNoTracking().Include(c => c.TaiSan!).ThenInclude(t => t.LoaiThietBi).Include(c => c.DeXuatThanhLy)
            .Where(c => c.DeXuatThanhLy!.TrangThai == TrangThaiThanhLy.DaThanhLy && c.DeXuatThanhLy.NgayHoanTat >= tu && c.DeXuatThanhLy.NgayHoanTat < den)
            .ToListAsync();
        // Giá trị thu hồi của đề xuất phân bổ theo tỷ lệ giá trị còn lại của từng tài sản
        var tongCl = giam.GroupBy(c => c.DeXuatThanhLyId).ToDictionary(g => g.Key, g => (Tong: g.Sum(c => c.GiaTriConLai), So: g.Count()));
        decimal PhanBo(Domain.Entities.ChiTietThanhLy c)
        {
            var (tong, so) = tongCl[c.DeXuatThanhLyId];
            return Math.Round(c.DeXuatThanhLy!.GiaTriThuHoi * (tong > 0 ? c.GiaTriConLai / tong : 1m / so), 0);
        }
        var nhan = await db.PhieuNhanHangs.AsNoTracking().Include(p => p.DonMuaHang!).ThenInclude(d => d.NhaCungCap).Include(p => p.DonMuaHang!).ThenInclude(d => d.KhoanMucNganSach)
            .Where(p => p.NgayNhan >= tu && p.NgayNhan < den).ToListAsync();
        var chi = await db.ChiNganSachs.AsNoTracking().Where(c => c.Ngay >= d0 && c.Ngay < d1).ToListAsync();
        var kmIds = chi.Select(c => c.KhoanMucNganSachId).Distinct().ToList();
        var km = await db.KhoanMucNganSachs.AsNoTracking().Where(k => kmIds.Contains(k.Id)).ToDictionaryAsync(k => k.Id, k => k.Ma);
        var sua = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Include(p => p.NhaCungCap)
            .Where(p => p.ChiPhiThueNgoai > 0 && p.NgayHoanThanh >= tu && p.NgayHoanThanh < den && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy).ToListAsync();

        var thucChi = nhan.Select(p => new DongThucChi(p.NgayNhan, "Nhận hàng theo đơn", p.SoHoaDon ?? p.DonMuaHang!.SoPhieu,
                $"Đơn {p.DonMuaHang!.SoPhieu}" + (p.TaiSanDaTao is null ? "" : $" · TS: {p.TaiSanDaTao}"), p.DonMuaHang.NhaCungCap?.Ten, p.GiaTri, p.DonMuaHang.KhoanMucNganSach?.Ma))
            .Concat(chi.Select(c => new DongThucChi(c.Ngay.ToDateTime(TimeOnly.MinValue), "Chi ngân sách", c.SoChungTu ?? $"#{c.Id}", c.NoiDung, null, c.SoTien, km.GetValueOrDefault(c.KhoanMucNganSachId))))
            .Concat(sua.Select(p => new DongThucChi(p.NgayHoanThanh!.Value, "Sửa chữa thuê ngoài", p.SoPhieu, $"{p.TaiSan?.MaTaiSan} · {p.BienPhapKhacPhuc}", p.NhaCungCap?.Ten, p.ChiPhiThueNgoai, null)))
            .OrderBy(x => x.Ngay).ToList();

        return new SoLieuKeToan(tu, den,
            tang.Select(t => new DongGhiTang(t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten, ExcelTaiSanService.Ten(t.NguonVon), t.NguyenGia, t.NgayMua, t.NgaySuDung,
                t.KhoaPhong?.Ten, t.NhaCungCap?.Ten, t.SoHopDong)).ToList(),
            giam.OrderBy(c => c.DeXuatThanhLy!.NgayHoanTat).Select(c => new DongGhiGiam(c.TaiSan!.MaTaiSan, c.TaiSan.Ten, c.TaiSan.LoaiThietBi?.Ten, c.TaiSan.NguyenGia,
                c.GiaTriConLai, c.DeXuatThanhLy!.SoPhieu, c.DeXuatThanhLy.SoQuyetDinh, c.DeXuatThanhLy.NgayQuyetDinh, c.DeXuatThanhLy.NgayHoanTat,
                c.DeXuatThanhLy.HinhThuc is { } h ? ExcelTaiSanService.Ten(h) : null, PhanBo(c))).ToList(),
            thucChi);
    }
}
