using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>
/// Quy tắc đánh mã thông minh (FR-TS-16) theo tham số TaiSan.MauMa, VD <c>{LOAI}-{KHOA}-{NAM}-{STT:5}</c> → PC-CC-2026-00012.
/// Số thứ tự tăng riêng cho từng "phần cố định" (mọi thứ trừ {STT}).
/// </summary>
public partial class MaTaiSanService(AppDbContext db, ThamSoService ts)
{
    public const string MauMacDinh = "{LOAI}-{KHOA}-{NAM}-{STT:5}";
    [GeneratedRegex(@"\{STT(?::(\d))?\}", RegexOptions.IgnoreCase)] private static partial Regex StT();

    public bool TuSinh => ts.BatTat(TS.TuSinhMaTaiSan);

    /// <summary>Kiểm tra mẫu mã: phải có đúng một {STT}, chỉ dùng các thành phần đã biết, độ dài hợp lý.</summary>
    public static string? KiemTraMau(string mau)
    {
        if (StT().Matches(mau).Count != 1) return "Mẫu phải có đúng một {STT} hoặc {STT:n}.";
        var la = Regex.Matches(mau, @"\{([^}]*)\}").Select(m => m.Groups[1].Value.ToUpperInvariant().Split(':')[0]).Where(x => x is not ("LOAI" or "KHOA" or "NAM" or "NAM2" or "STT")).ToList();
        if (la.Count > 0) return "Thành phần không hợp lệ: {" + string.Join("}, {", la) + "}.";
        return null;
    }

    private string Mau() { var m = ts.Chuoi(TS.MauMaTaiSan); return KiemTraMau(m) is null ? m : MauMacDinh; }

    /// <summary>Tách mẫu đã thay thành phần thành (đầu, số chữ số, cuối).</summary>
    public static (string Dau, int SoChuSo, string Cuoi) Dung(string mau, string? maLoai, string? maKhoa, DateTime ngay)
    {
        var s = Regex.Replace(mau, @"\{LOAI\}", (maLoai ?? "TS").ToUpperInvariant(), RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\{KHOA\}", (maKhoa ?? "KHO").ToUpperInvariant(), RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\{NAM2\}", ngay.ToString("yy", CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"\{NAM\}", ngay.ToString("yyyy", CultureInfo.InvariantCulture), RegexOptions.IgnoreCase);
        var m = StT().Match(s);
        var n = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : 4;
        return (s[..m.Index], Math.Clamp(n, 1, 9), s[(m.Index + m.Length)..]);
    }

    /// <summary>Mã kế tiếp cho loại/khoa. <paramref name="daCap"/> giữ số đã cấp trong cùng lô (chưa lưu CSDL).</summary>
    public async Task<string> TaoAsync(int loaiId, int? khoaId, Dictionary<string, int>? daCap = null)
    {
        var maLoai = await db.LoaiThietBis.Where(l => l.Id == loaiId).Select(l => l.Ma).FirstOrDefaultAsync() ?? throw new DomainException("Loại thiết bị không tồn tại.");
        var maKhoa = khoaId is null ? null : await db.KhoaPhongs.Where(k => k.Id == khoaId).Select(k => k.Ma).FirstOrDefaultAsync();
        var (dau, so, cuoi) = Dung(Mau(), maLoai, maKhoa, DateTime.Now);
        var khoa = dau + "|" + cuoi;
        if (daCap is null || !daCap.TryGetValue(khoa, out var n))
        {
            var ds = await db.TaiSans.Where(t => t.MaTaiSan.StartsWith(dau) && t.MaTaiSan.EndsWith(cuoi)).Select(t => t.MaTaiSan).ToListAsync();
            n = ds.Select(m => m.Length > dau.Length + cuoi.Length && int.TryParse(m[dau.Length..^cuoi.Length], NumberStyles.None, CultureInfo.InvariantCulture, out var k) ? k : 0)
                  .DefaultIfEmpty(0).Max();
        }
        n++;
        if (daCap is not null) daCap[khoa] = n;
        var ma = dau + n.ToString(new string('0', so), CultureInfo.InvariantCulture) + cuoi;
        if (ma.Length > 30) throw new DomainException($"Mã sinh ra '{ma}' dài quá 30 ký tự — rút gọn mẫu mã (Tham số → Tài sản).");
        return ma;
    }
}

/// <summary>Đọc / ghi giá trị trường tuỳ chỉnh của tài sản, ghi lịch sử khi thay đổi.</summary>
public class TruongTuyChinhService(AppDbContext db, LichSuService lichSu)
{
    public Task<List<TruongTuyChinh>> DangDungAsync() =>
        db.TruongTuyChinhs.AsNoTracking().Include(t => t.LoaiThietBi).Where(t => t.DangDung).OrderBy(t => t.ThuTu).ThenBy(t => t.Ten).ToListAsync();

    public Task<Dictionary<int, string>> GiaTriAsync(int taiSanId) =>
        db.GiaTriTuyChinhs.AsNoTracking().Where(g => g.TaiSanId == taiSanId).ToDictionaryAsync(g => g.TruongTuyChinhId, g => g.GiaTri);

    /// <summary>
    /// Kiểm tra toàn bộ trường áp dụng cho loại của tài sản; trả về danh sách lỗi (rỗng = hợp lệ) và giá trị đã chuẩn hoá.
    /// Trường không áp dụng cho loại hiện tại bị bỏ qua (giá trị cũ giữ nguyên).
    /// </summary>
    public static (List<string> Loi, Dictionary<int, string?> ChuanHoa) KiemTra(IEnumerable<TruongTuyChinh> truong, int loaiId, IReadOnlyDictionary<int, string?> nhap)
    {
        var loi = new List<string>(); var kq = new Dictionary<int, string?>();
        foreach (var t in truong.Where(t => t.ApDung(loaiId)))
        {
            try { kq[t.Id] = t.ChuanHoa(nhap.GetValueOrDefault(t.Id)); }
            catch (DomainException ex) { loi.Add(ex.Message); }
        }
        return (loi, kq);
    }

    /// <summary>Ghi giá trị (đã kiểm tra) cho tài sản. Không SaveChanges — người gọi lưu cùng giao dịch.</summary>
    public async Task GhiAsync(int taiSanId, IReadOnlyList<TruongTuyChinh> truong, Dictionary<int, string?> giaTri, string? nguoi, bool ghiLichSu = true)
    {
        var cu = await db.GiaTriTuyChinhs.Where(g => g.TaiSanId == taiSanId).ToDictionaryAsync(g => g.TruongTuyChinhId);
        var doi = new List<string>();
        foreach (var (id, v) in giaTri)
        {
            var t = truong.First(x => x.Id == id);
            var c = cu.GetValueOrDefault(id);
            var giaTriCu = c?.GiaTri;
            if (giaTriCu == v) continue;
            if (v is null) { if (c is not null) db.GiaTriTuyChinhs.Remove(c); }
            else if (c is null) db.GiaTriTuyChinhs.Add(new GiaTriTuyChinh { TaiSanId = taiSanId, TruongTuyChinhId = id, GiaTri = v });
            else c.GiaTri = v;
            doi.Add($"{t.Ten}: '{t.HienThi(giaTriCu)}' → '{t.HienThi(v)}'");
        }
        if (ghiLichSu && doi.Count > 0)
            lichSu.Ghi(taiSanId, LoaiSuKien.CapNhat, "Thông tin bổ sung: " + string.Join("; ", doi), nguoi);
    }
}

public record PhuTungDaDung(string Ma, string Ten, string? DonViTinh, decimal SoLuong, decimal ThanhTien, DateTime LanCuoi);

/// <summary>Hồ sơ vận hành của một tài sản (FR-TS-17).</summary>
public record HoSoVanHanh(
    int SoLanHong, int SoLanBaoTri, decimal ChiPhiSuaChua, decimal ChiPhiBaoTri, decimal ChiPhiVatTu, decimal ChiPhiThueNgoai,
    decimal TongGioCong, double DungMayGio, double? MtbfNgay, double? MttrGio, DateTime? LanHongCuoi, DateTime? LanBaoTriCuoi,
    DateOnly? BaoTriTiepTheo, long? ChiSoBoDem, decimal Tco, decimal TyLeSuaNguyenGia, List<PhuTungDaDung> PhuTung, int PhieuDangMo);

public class HoSoVanHanhService(AppDbContext db)
{
    public async Task<HoSoVanHanh> LapAsync(TaiSan ts)
    {
        var now = DateTime.Now;
        var ph = await db.PhieuSuaChuas.AsNoTracking().Where(p => p.TaiSanId == ts.Id && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy)
            .Include(p => p.VatTus).ThenInclude(v => v.VatTu).ToListAsync();
        var sua = ph.Where(p => p.LoaiCongViec == LoaiCongViec.SuaChua).ToList();
        var bt = ph.Where(p => p.LoaiCongViec == LoaiCongViec.BaoTriDinhKy).ToList();
        // Dừng máy: từ lúc báo đến khi hoàn thành (hoặc hiện tại nếu chưa xong) — gồm cả thời gian chờ linh kiện / gửi hãng (máy vẫn không dùng được).
        // MTTR: thời gian xử lý hiệu dụng = dừng máy trừ thời gian tạm dừng (cùng cách tính với báo cáo KPI).
        double DungMay(PhieuSuaChua p) => Math.Max(0, ((p.NgayHoanThanh ?? now) - p.NgayYeuCau).TotalHours);
        var xong = sua.Where(p => p.NgayHoanThanh is not null).ToList();
        var hong = sua.OrderBy(p => p.NgayYeuCau).Select(p => p.NgayYeuCau).ToList();
        double? mtbf = hong.Count >= 2 ? (hong[^1] - hong[0]).TotalDays / (hong.Count - 1)
            : hong.Count == 1 && (ts.NgaySuDung ?? ts.NgayMua) is { } bd ? (hong[0] - bd.ToDateTime(TimeOnly.MinValue)).TotalDays : null;
        var kh = await db.KeHoachBaoTris.AsNoTracking().Where(k => k.DangApDung && (k.TaiSanId == ts.Id || (k.TaiSanId == null && k.LoaiThietBiId == ts.LoaiThietBiId)))
            .OrderBy(k => k.NgayTiepTheo).Select(k => (DateOnly?)k.NgayTiepTheo).FirstOrDefaultAsync();
        var pt = ph.SelectMany(p => p.VatTus.Select(v => (p, v))).GroupBy(x => x.v.VatTuId)
            .Select(g => new PhuTungDaDung(g.First().v.VatTu?.Ma ?? "", g.First().v.VatTu?.Ten ?? "", g.First().v.VatTu?.DonViTinh, g.Sum(x => x.v.SoLuong),
                g.Sum(x => x.v.ThanhTien), g.Max(x => x.p.NgayYeuCau))).OrderByDescending(x => x.ThanhTien).ToList();
        var cpSua = sua.Sum(p => p.TongChiPhi); var cpBt = bt.Sum(p => p.TongChiPhi);
        var tco = (await new TcoService(db).LapAsync(NhomTco.TaiSan, gomThanhLy: true, taiSanId: ts.Id)).FirstOrDefault()?.Tong ?? ts.NguyenGia + cpSua + cpBt;
        return new HoSoVanHanh(sua.Count, bt.Count, cpSua, cpBt, ph.Sum(p => p.ChiPhiVatTu), ph.Sum(p => p.ChiPhiThueNgoai), ph.Sum(p => p.GioCong),
            Math.Round(sua.Sum(DungMay), 1), mtbf is null ? null : Math.Round(mtbf.Value, 1),
            xong.Count == 0 ? null : Math.Round(xong.Average(p => p.ThoiGianXuLyGio ?? 0), 1),
            sua.Max(p => (DateTime?)p.NgayYeuCau), bt.Max(p => p.NgayHoanThanh ?? (DateTime?)p.NgayYeuCau), kh, ts.ChiSoBoDem, tco,
            ts.NguyenGia > 0 ? Math.Round(cpSua * 100 / ts.NguyenGia, 1) : 0, pt,
            ph.Count(p => p.TrangThai is not (TrangThaiPhieuSuaChua.DaNghiemThu or TrangThaiPhieuSuaChua.HoanThanh)));
    }
}
