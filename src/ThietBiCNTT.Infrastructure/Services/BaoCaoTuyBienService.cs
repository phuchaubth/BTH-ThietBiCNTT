using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Định nghĩa một trường báo cáo. <c>LaSo</c>: cộng được khi nhóm.</summary>
public record TruongBaoCao(string Ma, string Ten, bool LaSo, Func<object, object?> Lay);

/// <summary>Cấu hình một báo cáo tuỳ biến (lưu vào BaoCaoMau.CauHinh).</summary>
public class CauHinhBaoCao
{
    public NguonBaoCao Nguon { get; set; } = NguonBaoCao.TaiSan;
    public List<string> Truong { get; set; } = [];
    public int? KhoaPhongId { get; set; }
    public int? LoaiThietBiId { get; set; }
    public string? TrangThai { get; set; }
    public DateTime? Tu { get; set; }
    public DateTime? Den { get; set; }
    /// <summary>Khoảng ngày tương đối (khi gửi định kỳ): true = dùng kỳ của lịch gửi thay cho Tu/Den.</summary>
    public bool TheoKyGui { get; set; } = true;
    public string? TuKhoa { get; set; }
    public string? NhomTheo { get; set; }
    public string? SapXep { get; set; }
    public bool Giam { get; set; }
}

public record KetQuaBaoCao(List<TruongBaoCao> Cot, List<object?[]> Dong, bool DaNhom, int TongBanGhi, string MoTaLoc);

/// <summary>Báo cáo tuỳ biến (FR-BC-12): chọn trường, lọc, nhóm (đếm + cộng trường số), sắp xếp.</summary>
public class BaoCaoTuyBienService(AppDbContext db)
{
    private static readonly CultureInfo Vi = new("vi-VN");
    private static string? Ten(Enum? e) => e is null ? null : ExcelTaiSanService.Ten(e);

    public static readonly IReadOnlyList<TruongBaoCao> TruongTaiSan =
    [
        new("MaTaiSan", "Mã tài sản", false, o => T(o).MaTaiSan), new("Ten", "Tên", false, o => T(o).Ten),
        new("Loai", "Loại thiết bị", false, o => T(o).LoaiThietBi?.Ten), new("Hang", "Hãng", false, o => T(o).Hang), new("Model", "Model", false, o => T(o).Model),
        new("SoSerial", "Serial", false, o => T(o).SoSerial), new("Khoa", "Khoa/phòng", false, o => T(o).KhoaPhong?.Ten ?? "Kho CNTT"),
        new("ViTri", "Vị trí", false, o => T(o).ViTri?.Ten), new("TrangThai", "Trạng thái", false, o => Ten(T(o).TrangThai)),
        new("NguonVon", "Nguồn vốn", false, o => Ten(T(o).NguonVon)), new("NguyenGia", "Nguyên giá", true, o => T(o).NguyenGia),
        new("NgayMua", "Ngày mua", false, o => T(o).NgayMua), new("NamMua", "Năm mua", false, o => T(o).NgayMua?.Year),
        new("Tuoi", "Tuổi (năm)", true, o => T(o).TuoiNam(DateOnly.FromDateTime(DateTime.Today))),
        new("HanBaoHanh", "Hạn bảo hành", false, o => T(o).HanBaoHanh), new("ConBaoHanh", "Còn bảo hành", false, o => T(o).ConBaoHanh(DateOnly.FromDateTime(DateTime.Today)) ? "Còn" : "Hết"),
        new("NhaCungCap", "Nhà cung cấp", false, o => T(o).NhaCungCap?.Ten), new("NguoiSuDung", "Người sử dụng", false, o => T(o).NguoiSuDung),
        new("CPU", "CPU", false, o => T(o).CauHinh?.CPU), new("RAM", "RAM", false, o => T(o).CauHinh?.RAM), new("OCung", "Ổ cứng", false, o => T(o).CauHinh?.OCung),
        new("HeDieuHanh", "Hệ điều hành", false, o => T(o).CauHinh?.HeDieuHanh), new("IP", "Địa chỉ IP", false, o => T(o).CauHinh?.DiaChiIP),
        new("Hostname", "Hostname", false, o => T(o).CauHinh?.Hostname), new("SoLuong", "Số lượng", true, _ => 1),
    ];

    public static readonly IReadOnlyList<TruongBaoCao> TruongPhieu =
    [
        new("SoPhieu", "Số phiếu", false, o => P(o).SoPhieu), new("LoaiCongViec", "Loại công việc", false, o => Ten(P(o).LoaiCongViec)),
        new("MaTaiSan", "Mã tài sản", false, o => P(o).TaiSan?.MaTaiSan), new("TenTaiSan", "Tên thiết bị", false, o => P(o).TaiSan?.Ten),
        new("LoaiThietBi", "Loại thiết bị", false, o => P(o).TaiSan?.LoaiThietBi?.Ten), new("Khoa", "Khoa/phòng", false, o => P(o).KhoaPhong?.Ten),
        new("UuTien", "Ưu tiên", false, o => Ten(P(o).UuTien)), new("TrangThai", "Trạng thái", false, o => Ten(P(o).TrangThai)),
        new("NgayYeuCau", "Thời điểm báo", false, o => P(o).NgayYeuCau), new("Thang", "Tháng", false, o => P(o).NgayYeuCau.ToString("yyyy-MM")),
        new("HanXuLy", "Hạn SLA", false, o => P(o).HanXuLy), new("NgayHoanThanh", "Hoàn thành", false, o => P(o).NgayHoanThanh),
        new("KyThuatVien", "Kỹ thuật viên", false, o => P(o).KyThuatVien), new("NguoiYeuCau", "Người báo", false, o => P(o).NguoiYeuCau),
        new("DungHan", "Đúng SLA", false, o => P(o).NgayHoanThanh is { } h ? (h <= P(o).HanXuLy ? "Đúng hạn" : "Trễ hạn") : null),
        new("GioXuLy", "Thời gian xử lý (giờ)", true, o => P(o).NgayHoanThanh is { } h ? Math.Round((decimal)(h - P(o).NgayYeuCau).TotalHours, 1) : null),
        new("ChiPhiVatTu", "Chi phí vật tư", true, o => P(o).ChiPhiVatTu), new("ChiPhiThueNgoai", "Chi phí thuê ngoài", true, o => P(o).ChiPhiThueNgoai),
        new("TongChiPhi", "Tổng chi phí", true, o => P(o).TongChiPhi), new("GioCong", "Giờ công", true, o => P(o).GioCong),
        new("DanhGia", "Đánh giá (1-5)", false, o => P(o).DanhGia), new("MoTa", "Mô tả sự cố", false, o => P(o).MoTaSuCo),
        new("SoLuong", "Số phiếu", true, _ => 1),
    ];

    private static TaiSan T(object o) => (TaiSan)o;
    private static PhieuSuaChua P(object o) => (PhieuSuaChua)o;

    public static IReadOnlyList<TruongBaoCao> Truong(NguonBaoCao n) => n == NguonBaoCao.TaiSan ? TruongTaiSan : TruongPhieu;
    public static string[] TrangThai(NguonBaoCao n) => n == NguonBaoCao.TaiSan ? Enum.GetNames<TrangThaiTaiSan>() : Enum.GetNames<TrangThaiPhieuSuaChua>();

    public static CauHinhBaoCao Doc(string json) => JsonSerializer.Deserialize<CauHinhBaoCao>(json) ?? new();
    public static string Ghi(CauHinhBaoCao c) => JsonSerializer.Serialize(c);

    /// <param name="ky">Khoảng ngày ghi đè (báo cáo gửi định kỳ có TheoKyGui).</param>
    public async Task<KetQuaBaoCao> ChayAsync(CauHinhBaoCao c, (DateTime Tu, DateTime Den)? ky = null)
    {
        var dsTruong = Truong(c.Nguon);
        var cot = c.Truong.Select(m => dsTruong.FirstOrDefault(t => t.Ma == m)).OfType<TruongBaoCao>().DistinctBy(t => t.Ma).ToList();
        if (cot.Count == 0) throw new DomainException("Chọn ít nhất một trường hiển thị.");
        var tu = ky is not null && c.TheoKyGui ? ky.Value.Tu : c.Tu;
        var den = ky is not null && c.TheoKyGui ? ky.Value.Den : c.Den;
        var moTa = new List<string>();
        List<object> nguon;
        if (c.Nguon == NguonBaoCao.TaiSan)
        {
            var q = db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.ViTri).Include(t => t.NhaCungCap).Include(t => t.CauHinh).AsQueryable();
            if (c.KhoaPhongId is { } k) q = q.Where(t => t.KhoaPhongId == k);
            if (c.LoaiThietBiId is { } l) q = q.Where(t => t.LoaiThietBiId == l);
            if (Enum.TryParse<TrangThaiTaiSan>(c.TrangThai, out var tt)) { q = q.Where(t => t.TrangThai == tt); moTa.Add(Ten(tt)!); }
            if (tu is { } a) { var d0 = DateOnly.FromDateTime(a); q = q.Where(t => t.NgayMua >= d0); }
            if (den is { } b) { var d1 = DateOnly.FromDateTime(b); q = q.Where(t => t.NgayMua < d1); }
            if (!string.IsNullOrWhiteSpace(c.TuKhoa)) q = q.Where(t => t.MaTaiSan.Contains(c.TuKhoa) || t.Ten.Contains(c.TuKhoa) || (t.Model ?? "").Contains(c.TuKhoa));
            nguon = [.. await q.OrderBy(t => t.MaTaiSan).Take(20000).ToListAsync()];
        }
        else
        {
            var q = db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).ThenInclude(t => t!.LoaiThietBi).Include(p => p.KhoaPhong).Include(p => p.VatTus).AsSplitQuery().AsQueryable();
            if (c.KhoaPhongId is { } k) q = q.Where(p => p.KhoaPhongId == k);
            if (c.LoaiThietBiId is { } l) q = q.Where(p => p.TaiSan!.LoaiThietBiId == l);
            if (Enum.TryParse<TrangThaiPhieuSuaChua>(c.TrangThai, out var tt)) { q = q.Where(p => p.TrangThai == tt); moTa.Add(Ten(tt)!); }
            if (tu is { } a) q = q.Where(p => p.NgayYeuCau >= a);
            if (den is { } b) q = q.Where(p => p.NgayYeuCau < b);
            if (!string.IsNullOrWhiteSpace(c.TuKhoa)) q = q.Where(p => p.SoPhieu.Contains(c.TuKhoa) || p.MoTaSuCo.Contains(c.TuKhoa) || p.TaiSan!.MaTaiSan.Contains(c.TuKhoa));
            nguon = [.. await q.OrderByDescending(p => p.NgayYeuCau).Take(20000).ToListAsync()];
        }
        if (c.KhoaPhongId is { } kk) moTa.Add("Khoa: " + (await db.KhoaPhongs.FindAsync(kk))?.Ten);
        if (c.LoaiThietBiId is { } ll) moTa.Add("Loại: " + (await db.LoaiThietBis.FindAsync(ll))?.Ten);
        if (tu is not null || den is not null) moTa.Add($"{(c.Nguon == NguonBaoCao.TaiSan ? "Ngày mua" : "Ngày báo")}: {tu:dd/MM/yyyy} – {den?.AddDays(-1):dd/MM/yyyy}");
        if (!string.IsNullOrWhiteSpace(c.TuKhoa)) moTa.Add($"Từ khoá: {c.TuKhoa}");

        List<object?[]> dong;
        var nhom = c.NhomTheo is null ? null : dsTruong.FirstOrDefault(t => t.Ma == c.NhomTheo);
        if (nhom is not null)
        {
            // Nhóm: cột nhóm + số bản ghi + tổng các trường số đã chọn
            var so = cot.Where(t => t.LaSo && t.Ma != "SoLuong").ToList();
            cot = [nhom, new("SoLuong", c.Nguon == NguonBaoCao.TaiSan ? "Số tài sản" : "Số phiếu", true, _ => 1), .. so];
            dong = nguon.GroupBy(o => nhom.Lay(o)?.ToString() ?? "(trống)")
                .Select(g => new object?[] { g.Key, g.Count() }.Concat(so.Select(t => (object?)g.Sum(o => Convert.ToDecimal(t.Lay(o) ?? 0m, Vi)))).ToArray()).ToList();
        }
        else dong = nguon.Select(o => cot.Select(t => t.Lay(o)).ToArray()).ToList();

        var i = c.SapXep is null ? -1 : cot.FindIndex(t => t.Ma == c.SapXep);
        if (i < 0 && nhom is not null) i = 1;
        if (i >= 0)
        {
            var cmp = Comparer<object?>.Create((a, b) => a is null ? (b is null ? 0 : -1) : b is null ? 1
                : a is IComparable ca && a.GetType() == b.GetType() ? ca.CompareTo(b) : string.Compare(a.ToString(), b.ToString(), StringComparison.CurrentCulture));
            dong = (c.Giam || (nhom is not null && c.SapXep is null) ? dong.OrderByDescending(r => r[i], cmp) : dong.OrderBy(r => r[i], cmp)).ToList();
        }
        return new KetQuaBaoCao(cot, dong, nhom is not null, nguon.Count, string.Join(" · ", moTa));
    }
}
