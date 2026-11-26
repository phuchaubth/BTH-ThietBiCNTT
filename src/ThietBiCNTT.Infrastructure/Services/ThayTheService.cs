using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

public record DeNghiThayThe(int TaiSanId, string MaTaiSan, string Ten, string Loai, int LoaiThietBiId, string? Khoa, int? KhoaPhongId,
    decimal? TuoiNam, int TuoiTho, decimal NguyenGia, int SoLanHong12T, decimal ChiPhiLuyKe, decimal? TyLeChiPhi, string? HeDieuHanh, decimal? RamGb,
    TrangThaiTaiSan TrangThai, List<string> LyDo)
{
    public int Diem => LyDo.Count;
}

public record TieuChiThayThe(int SoLanHong, decimal TyLeChiPhi, IReadOnlyList<string> HdhHetHoTro, decimal RamToiThieu, int SoTieuChiToiThieu);

/// <summary>
/// Cảnh báo / đề nghị thay thế (FR-BC-10): quá tuổi thọ của loại, hỏng lặp lại trong 12 tháng, chi phí sửa chữa luỹ kế vượt % nguyên giá,
/// dưới chuẩn cấu hình (RAM), hệ điều hành hết hỗ trợ, đang hỏng. Ngưỡng lấy từ tham số hệ thống.
/// </summary>
public class ThayTheService(AppDbContext db, ThamSoService thamSo, SoPhieuService soPhieu)
{
    public TieuChiThayThe TieuChi() => new(thamSo.SoNguyen(TS.ThayTheSoLanHong), thamSo.SoThuc(TS.ThayTheTyLeChiPhi),
        thamSo.Chuoi(TS.ThayTheHdhHetHoTro).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        thamSo.SoThuc(TS.AgentChuanRam), Math.Max(1, thamSo.SoNguyen(TS.ThayTheSoTieuChi)));

    /// <summary>"8 GB", "8GB DDR4", "16384 MB" → GB.</summary>
    public static decimal? DocRam(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var m = Regex.Match(s, @"(\d+(?:[.,]\d+)?)\s*(GB|G|MB)?", RegexOptions.IgnoreCase);
        if (!m.Success || !decimal.TryParse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var v)) return null;
        return m.Groups[2].Value.Equals("MB", StringComparison.OrdinalIgnoreCase) ? Math.Round(v / 1024, 1) : v;
    }

    /// <summary>Hệ điều hành có thuộc danh sách hết hỗ trợ ("Windows 7" khớp "Windows 7 Pro", không khớp "Windows 11").</summary>
    public static bool HdhHetHoTro(string? hdh, IEnumerable<string> ds) =>
        !string.IsNullOrWhiteSpace(hdh) && ds.Any(x => Regex.IsMatch(hdh, $@"(^|\W){Regex.Escape(x)}($|[^\d])", RegexOptions.IgnoreCase));

    public async Task<List<DeNghiThayThe>> DanhSachAsync(DateTime now, int? khoaId = null, int? loaiId = null, bool tatCa = false)
    {
        var tc = TieuChi();
        var homNay = DateOnly.FromDateTime(now);
        var ts = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.CauHinh)
            .Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy
                && (khoaId == null || t.KhoaPhongId == khoaId) && (loaiId == null || t.LoaiThietBiId == loaiId)).ToListAsync();
        var tu = now.AddYears(-1);
        var phieu = await db.PhieuSuaChuas.AsNoTracking().Where(p => p.LoaiCongViec == LoaiCongViec.SuaChua && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy)
            .Select(p => new { p.TaiSanId, p.NgayYeuCau, Chi = p.ChiPhiThueNgoai + p.VatTus.Sum(v => v.SoLuong * v.DonGia) }).ToListAsync();
        var hong = phieu.Where(p => p.NgayYeuCau >= tu).GroupBy(p => p.TaiSanId).ToDictionary(g => g.Key, g => g.Count());
        var chi = phieu.GroupBy(p => p.TaiSanId).ToDictionary(g => g.Key, g => g.Sum(x => x.Chi));
        var agent = (await db.MayTramAgents.AsNoTracking().Where(m => m.TaiSanId != null).ToListAsync())
            .GroupBy(m => m.TaiSanId!.Value).ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.LanCuoi).First());
        var kq = new List<DeNghiThayThe>();
        foreach (var t in ts)
        {
            var lyDo = new List<string>();
            var bd = t.NgaySuDung ?? t.NgayMua;
            decimal? tuoi = bd is { } b ? Math.Round((homNay.DayNumber - b.DayNumber) / 365.25m, 1) : null;
            var tuoiTho = t.LoaiThietBi?.TuoiThoNam ?? 0;
            if (tuoi is { } x && tuoiTho > 0 && x >= tuoiTho) lyDo.Add($"Quá tuổi thọ ({x:0.#}/{tuoiTho} năm)");
            var nHong = hong.GetValueOrDefault(t.Id);
            if (tc.SoLanHong > 0 && nHong >= tc.SoLanHong) lyDo.Add($"Hỏng {nHong} lần/12 tháng");
            var luyKe = chi.GetValueOrDefault(t.Id);
            decimal? tyLe = t.NguyenGia > 0 ? Math.Round(luyKe * 100 / t.NguyenGia, 1) : null;
            if (tc.TyLeChiPhi > 0 && tyLe >= tc.TyLeChiPhi) lyDo.Add($"Chi phí sửa {tyLe:0.#}% nguyên giá");
            var may = agent.GetValueOrDefault(t.Id);
            var hdh = may?.HeDieuHanh ?? t.CauHinh?.HeDieuHanh;
            if (HdhHetHoTro(hdh, tc.HdhHetHoTro)) lyDo.Add($"HĐH hết hỗ trợ ({hdh})");
            var ram = may?.RamGb ?? DocRam(t.CauHinh?.RAM);
            var laMay = t.LoaiThietBi?.Nhom is NhomThietBi.MayTinhDeBan or NhomThietBi.MayTinhXachTay;
            if (laMay && tc.RamToiThieu > 0 && ram < tc.RamToiThieu) lyDo.Add($"RAM {ram:0.#} GB dưới chuẩn {tc.RamToiThieu:0.#} GB");
            if (t.TrangThai == TrangThaiTaiSan.Hong) lyDo.Add("Đang hỏng");
            if (!tatCa && lyDo.Count < tc.SoTieuChiToiThieu) continue;
            kq.Add(new DeNghiThayThe(t.Id, t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten ?? "", t.LoaiThietBiId, t.KhoaPhong?.Ten, t.KhoaPhongId, tuoi, tuoiTho,
                t.NguyenGia, nHong, luyKe, tyLe, hdh, ram, t.TrangThai, lyDo));
        }
        return kq.OrderByDescending(x => x.Diem).ThenByDescending(x => x.TyLeChiPhi ?? 0).ThenByDescending(x => x.TuoiNam ?? 0).ToList();
    }

    /// <summary>Lập đề xuất mua sắm nháp (căn cứ: thay thế) — mỗi loại thiết bị một dòng, đơn giá gợi ý = nguyên giá bình quân.</summary>
    public async Task<DeXuatMuaSam> LapDeXuatAsync(IReadOnlyCollection<int> taiSanIds, string nguoi, DateTime now)
    {
        var ds = (await DanhSachAsync(now, tatCa: true)).Where(x => taiSanIds.Contains(x.TaiSanId)).ToList();
        if (ds.Count == 0) throw new DomainException("Chọn ít nhất một tài sản cần thay thế.");
        var dx = new DeXuatMuaSam
        {
            TieuDe = $"Thay thế {ds.Count} thiết bị CNTT cũ, hỏng nhiều", CanCu = CanCuDeXuat.ThayThe, NguoiTao = nguoi,
            LyDo = "Theo báo cáo đề nghị thay thế ngày " + now.ToString("dd/MM/yyyy") + ":\n" +
                   string.Join("\n", ds.Select(x => $"- {x.MaTaiSan} ({x.Khoa ?? "kho"}): {string.Join("; ", x.LyDo)}")),
            Dongs = ds.GroupBy(x => x.LoaiThietBiId).Select(g => new DongDeXuat
            {
                Loai = LoaiHangMua.ThietBi, LoaiThietBiId = g.Key, DonViTinh = "Bộ", SoLuong = g.Count(),
                DonGiaDuKien = Math.Round(g.Average(x => x.NguyenGia) / 1000) * 1000,
                MoTa = $"{g.First().Loai} thay thế (cấu hình theo chuẩn hiện hành)", GhiChu = "Thay cho: " + string.Join(", ", g.Select(x => x.MaTaiSan))
            }).ToList()
        };
        if (dx.Dongs.Sum(d => d.GhiChu!.Length) > 0) foreach (var d in dx.Dongs) if (d.GhiChu!.Length > 300) d.GhiChu = d.GhiChu[..297] + "…";
        if (dx.LyDo.Length > 1000) dx.LyDo = dx.LyDo[..997] + "…";
        dx.KiemTraDong();
        db.DeXuatMuaSams.Add(dx);
        await db.LuuMoiAsync(soPhieu, "DX", so => dx.SoPhieu = so);
        return dx;
    }
}
