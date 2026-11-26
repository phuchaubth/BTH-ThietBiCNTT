using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

public record MauBaoCaoChuan(string Ma, string Ten, string MoTa, bool CanKy, bool CanKhoa, bool CanNam, bool CanSoNgay);

internal record SuCo(NhomNguyenNhan? Nhom, HanhDongKhacPhuc? HanhDong, double? DungMay, decimal ChiPhi);

public record ThamSoBaoCao(DateTime Tu, DateTime Den, int? KhoaId, int Nam, int SoNgay);

/// <summary>Danh mục báo cáo chuẩn (FR-BC-11): dựng bảng dữ liệu dùng chung cho xem web, PDF, Excel.</summary>
public class BaoCaoChuanService(AppDbContext db, BaoCaoService baoCao, NganSachService nganSach, ThayTheService thayThe, ThamSoService thamSo)
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");
    private static string N(decimal v) => v.ToString("#,##0", Vi);
    private static string N1(double? v) => v is null ? "" : v.Value.ToString("0.#", Vi);
    private static string N1(decimal? v) => v is null ? "" : v.Value.ToString("0.#", Vi);
    private static string T(Enum e) => VanThuService.TenEnum(e);

    public static readonly IReadOnlyList<MauBaoCaoChuan> DanhMuc =
    [
        new("BC01", "Danh sách tài sản CNTT", "Toàn bộ tài sản đang quản lý (trừ đã thanh lý) theo khoa/phòng, nguyên giá, trạng thái.", false, true, false, false),
        new("BC02", "Tình hình sửa chữa, bảo trì theo khoa/phòng", "Số yêu cầu, đã xử lý, tỷ lệ đúng hạn SLA, thời gian xử lý (MTTR), dừng máy, chi phí trong kỳ.", true, true, false, false),
        new("BC03", "Chi phí sửa chữa, bảo trì theo loại thiết bị", "Chi phí vật tư và thuê ngoài trong kỳ, theo loại thiết bị.", true, true, false, false),
        new("BC04", "Danh sách thiết bị đề nghị thay thế", "Theo tiêu chí: quá tuổi thọ, hỏng lặp lại, chi phí sửa chữa cao, HĐH hết hỗ trợ, dưới chuẩn cấu hình, đang hỏng.", false, true, false, false),
        new("BC05", "Tình hình thực hiện ngân sách CNTT", "Dự toán, cam kết, thực chi, còn lại theo khoản mục trong năm.", false, false, true, false),
        new("BC06", "Bảo hành, hợp đồng, bản quyền sắp hết hạn", "Các mốc hết hạn trong N ngày tới.", false, false, false, true),
        new("BC07", "Thống kê nguyên nhân sự cố", "Số phiếu, tỷ lệ, thời gian dừng máy, chi phí và cách khắc phục chủ yếu theo nhóm nguyên nhân trong kỳ.", true, true, false, false),
    ];

    public ThongTinIn ThongTin(string nguoiLap) => new(thamSo.Chuoi(TS.TenBenhVien), thamSo.Chuoi(TS.TenPhongCNTT), thamSo.Chuoi(TS.VanThuDiaDanh),
        thamSo.Chuoi(TS.BaoCaoChuKy).Split('|', StringSplitOptions.TrimEntries), nguoiLap);

    public async Task<BangBaoCao?> LapAsync(string ma, ThamSoBaoCao p, DateTime now)
    {
        var khoa = p.KhoaId is { } k ? (await db.KhoaPhongs.FindAsync(k))?.Ten : null;
        var phamVi = khoa is null ? "Toàn bệnh viện" : khoa;
        var ky = $"Từ ngày {p.Tu:dd/MM/yyyy} đến ngày {p.Den:dd/MM/yyyy} · {phamVi}";
        switch (ma)
        {
            case "BC01":
            {
                var ds = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong)
                    .Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && (p.KhoaId == null || t.KhoaPhongId == p.KhoaId))
                    .OrderBy(t => t.KhoaPhong!.Ten).ThenBy(t => t.MaTaiSan).ToListAsync();
                var dong = ds.Select((t, i) => new string?[] { (i + 1).ToString(), t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten, t.SoSerial,
                    (t.NgaySuDung ?? t.NgayMua)?.Year.ToString(), N(t.NguyenGia), t.KhoaPhong?.Ten ?? "Kho CNTT", T(t.TrangThai) }).ToList();
                return new("BC01", "Danh sách tài sản công nghệ thông tin", $"Tính đến ngày {now:dd/MM/yyyy} · {phamVi}",
                    ["STT", "Mã tài sản", "Tên tài sản", "Loại", "Số serial", "Năm SD", "Nguyên giá (đ)", "Khoa/phòng", "Trạng thái"],
                    [1, 2.6, 5.5, 3, 3, 1.4, 2.6, 4, 2.4], dong, ["", "Cộng", $"{ds.Count} tài sản", "", "", "", N(ds.Sum(t => t.NguyenGia)), "", ""], true, [0, 5, 6]);
            }
            case "BC02":
            {
                var r = await baoCao.LapAsync(p.Tu, p.Den, p.KhoaId);
                string?[] D(DongSla x, string stt) => [stt, x.Nhom, x.SoPhieu.ToString(), x.DaXong.ToString(), x.DungHan.ToString(), N1(x.TyLeDungHan), N1(x.MttrGio), N1(x.DungMayGio), N(x.ChiPhi)];
                return new("BC02", "Báo cáo tình hình sửa chữa, bảo trì thiết bị CNTT", ky,
                    ["STT", "Khoa/phòng", "Số yêu cầu", "Đã xử lý", "Đúng hạn", "% đúng hạn", "MTTR (giờ)", "Dừng máy (giờ)", "Chi phí (đ)"],
                    [1, 6, 2, 2, 2, 2, 2, 2.4, 3], r.TheoKhoa.Select((x, i) => D(x, (i + 1).ToString())).ToList(), D(r.Tong, "") is var t ? [.. t[..1], "Tổng cộng", .. t[2..]] : null,
                    true, [0, 2, 3, 4, 5, 6, 7, 8], "MTTR: thời gian xử lý bình quân từ khi báo đến khi hoàn thành (trừ thời gian tạm dừng chờ linh kiện). Đúng hạn: hoàn thành trong hạn SLA.");
            }
            case "BC03":
            {
                var r = await baoCao.LapAsync(p.Tu, p.Den, p.KhoaId);
                var ds = r.ChiPhiTheoLoai;
                return new("BC03", "Báo cáo chi phí sửa chữa, bảo trì theo loại thiết bị", ky,
                    ["STT", "Loại thiết bị", "Số phiếu", "Vật tư, linh kiện (đ)", "Thuê ngoài (đ)", "Nhân công nội bộ (đ)", "Tổng (đ)"], [1, 6, 2, 3.3, 3.3, 3.3, 3.3],
                    ds.Select((x, i) => new string?[] { (i + 1).ToString(), x.Nhom, x.SoPhieu.ToString(), N(x.VatTu), N(x.ThueNgoai), N(x.NhanCong), N(x.Tong) }).ToList(),
                    ["", "Tổng cộng", ds.Sum(x => x.SoPhieu).ToString(), N(ds.Sum(x => x.VatTu)), N(ds.Sum(x => x.ThueNgoai)), N(ds.Sum(x => x.NhanCong)), N(ds.Sum(x => x.Tong))], false, [0, 2, 3, 4, 5, 6]);
            }
            case "BC07":
            {
                var den = p.Den.AddDays(1);
                var ds = (await db.PhieuSuaChuas.AsNoTracking().Where(x => x.LoaiCongViec == LoaiCongViec.SuaChua && x.TrangThai != TrangThaiPhieuSuaChua.DaHuy
                        && x.NgayYeuCau >= p.Tu && x.NgayYeuCau < den && (p.KhoaId == null || x.KhoaPhongId == p.KhoaId))
                    .Select(x => new { x.NhomNguyenNhan, x.HanhDongKhacPhuc, x.NgayYeuCau, x.NgayHoanThanh, x.ChiPhiThueNgoai, x.ChiPhiNhanCong,
                        VatTu = x.VatTus.Sum(v => (decimal?)(v.SoLuong * v.DonGia)) ?? 0 }).ToListAsync())
                    .Select(x => new SuCo(x.NhomNguyenNhan, x.HanhDongKhacPhuc, x.NgayHoanThanh is { } h ? (h - x.NgayYeuCau).TotalHours : null,
                        x.ChiPhiThueNgoai + x.ChiPhiNhanCong + x.VatTu)).ToList();
                var tong = ds.Count;
                var dong = ds.GroupBy(x => x.Nhom).OrderByDescending(g => g.Count()).Select((g, i) =>
                {
                    var xong = g.Where(x => x.DungMay is not null).ToList();
                    return new string?[] { (i + 1).ToString(), g.Key is { } k ? T(k) : "Chưa phân loại", g.Count().ToString(), N1(Math.Round(100.0 * g.Count() / tong, 1)),
                        N1(xong.Count == 0 ? null : xong.Average(x => x.DungMay!.Value)), N(g.Sum(x => x.ChiPhi)),
                        string.Join("; ", g.Where(x => x.HanhDong is not null).GroupBy(x => x.HanhDong!.Value).OrderByDescending(h => h.Count()).Take(3).Select(h => $"{T(h.Key)} ({h.Count()})")) };
                }).ToList();
                return new("BC07", "Thống kê nguyên nhân sự cố thiết bị CNTT", ky,
                    ["STT", "Nhóm nguyên nhân", "Số phiếu", "Tỷ lệ (%)", "Dừng máy TB (giờ)", "Chi phí (đ)", "Khắc phục chủ yếu"], [1, 5, 2, 2, 2.4, 3, 6],
                    dong, ["", "Tổng cộng", tong.ToString(), tong == 0 ? "" : "100", "", N(ds.Sum(x => x.ChiPhi)), ""], true, [0, 2, 3, 4, 5],
                    "Nhóm nguyên nhân do KTV chọn khi hoàn thành phiếu. \"Chưa phân loại\": phiếu chưa hoàn thành hoặc lập trước khi có danh mục nguyên nhân.");
            }
            case "BC04":
            {
                var ds = await thayThe.DanhSachAsync(now, p.KhoaId);
                var tc = thayThe.TieuChi();
                return new("BC04", "Danh sách thiết bị đề nghị thay thế", $"Tính đến ngày {now:dd/MM/yyyy} · {phamVi}",
                    ["STT", "Mã tài sản", "Tên tài sản", "Khoa/phòng", "Tuổi / tuổi thọ (năm)", "Hỏng 12 tháng", "Chi phí SC luỹ kế (đ)", "% nguyên giá", "Lý do đề nghị"],
                    [1, 2.5, 4.5, 3.5, 2, 1.6, 2.8, 1.6, 7], ds.Select((x, i) => new string?[] { (i + 1).ToString(), x.MaTaiSan, x.Ten, x.Khoa ?? "Kho CNTT",
                        $"{N1(x.TuoiNam)}/{x.TuoiTho}", x.SoLanHong12T.ToString(), N(x.ChiPhiLuyKe), N1(x.TyLeChiPhi), string.Join("; ", x.LyDo) }).ToList(),
                    ["", "Cộng", $"{ds.Count} thiết bị", "", "", "", N(ds.Sum(x => x.ChiPhiLuyKe)), "", ""], true, [0, 5, 6, 7],
                    $"Tiêu chí: quá tuổi thọ của loại thiết bị; hỏng ≥ {tc.SoLanHong} lần/12 tháng; chi phí sửa chữa luỹ kế ≥ {tc.TyLeChiPhi:0.#}% nguyên giá; HĐH hết hỗ trợ ({string.Join(", ", tc.HdhHetHoTro)}); RAM < {tc.RamToiThieu:0.#} GB; đang hỏng. Đưa vào danh sách khi thoả từ {tc.SoTieuChiToiThieu} tiêu chí.");
            }
            case "BC05":
            {
                var ds = await nganSach.SuDungAsync(p.Nam);
                return new("BC05", $"Báo cáo tình hình thực hiện ngân sách CNTT năm {p.Nam}", $"Số liệu đến ngày {now:dd/MM/yyyy}",
                    ["STT", "Mã", "Khoản mục", "Dự toán (đ)", "Cam kết (đ)", "Thực chi (đ)", "Còn lại (đ)", "% đã dùng"], [1, 1.8, 6, 3, 3, 3, 3, 1.8],
                    ds.Select((x, i) => new string?[] { (i + 1).ToString(), x.KhoanMuc.Ma, x.KhoanMuc.Ten, N(x.DuToan), N(x.CamKet), N(x.ThucChi), N(x.ConLai), N1(x.TyLe) }).ToList(),
                    ["", "", "Tổng cộng", N(ds.Sum(x => x.DuToan)), N(ds.Sum(x => x.CamKet)), N(ds.Sum(x => x.ThucChi)), N(ds.Sum(x => x.ConLai)),
                        ds.Sum(x => x.DuToan) is > 0 and var dt ? N1(Math.Round((ds.Sum(x => x.CamKet) + ds.Sum(x => x.ThucChi)) * 100 / dt, 1)) : ""], true, [0, 3, 4, 5, 6, 7],
                    "Cam kết: đề xuất đã duyệt và đơn hàng chưa nhận; thực chi: nhận hàng, sửa chữa thuê ngoài và chi khác.");
            }
            case "BC06":
            {
                var d = DateOnly.FromDateTime(now); var moc = d.AddDays(p.SoNgay);
                var bh = await db.TaiSans.AsNoTracking().Include(t => t.KhoaPhong).Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.HanBaoHanh >= d && t.HanBaoHanh <= moc)
                    .Select(t => new { Loai = "Bảo hành tài sản", So = t.MaTaiSan, t.Ten, Han = t.HanBaoHanh!.Value, PhuTrach = (string?)t.KhoaPhong!.Ten }).ToListAsync();
                var hd = await db.HopDongs.AsNoTracking().Where(h => h.HieuLucDen >= d && h.HieuLucDen <= moc)
                    .Select(h => new { Loai = "Hợp đồng", So = h.SoHopDong, h.Ten, Han = h.HieuLucDen!.Value, PhuTrach = h.NguoiPhuTrach }).ToListAsync();
                var bq = await db.BanQuyenPhanMems.AsNoTracking().Where(b => b.NgayHetHan >= d && b.NgayHetHan <= moc)
                    .Select(b => new { Loai = "Bản quyền phần mềm", So = b.NhaSanXuat ?? "", b.Ten, Han = b.NgayHetHan!.Value, PhuTrach = (string?)null }).ToListAsync();
                var ds = bh.Concat(hd).Concat(bq).OrderBy(x => x.Han).ToList();
                return new("BC06", "Danh sách bảo hành, hợp đồng, bản quyền sắp hết hạn", $"Trong {p.SoNgay} ngày, từ {d:dd/MM/yyyy} đến {moc:dd/MM/yyyy}",
                    ["STT", "Loại", "Mã / số", "Tên", "Hết hạn", "Còn (ngày)", "Khoa / phụ trách"], [1, 3, 3, 6, 2.2, 1.8, 3.5],
                    ds.Select((x, i) => new string?[] { (i + 1).ToString(), x.Loai, x.So, x.Ten, x.Han.ToString("dd/MM/yyyy"), (x.Han.DayNumber - d.DayNumber).ToString(), x.PhuTrach }).ToList(),
                    null, false, [0, 5]);
            }
            default: return null;
        }
    }
}
