using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Trường dùng trong điều kiện: mã, tên hiển thị, kiểu ("so", "chuoi", "ngay", "bool"), gợi ý giá trị.</summary>
public record TruongCanhBao(string Ma, string Ten, string Kieu, string? GoiY = null);

/// <summary>Một đối tượng đã nạp để đánh giá quy tắc.</summary>
public record DongCanhBao(string Id, string Ten, string Link, int? KhoaPhongId, int? LoaiThietBiId, MucDoUuTien? UuTien,
    string? PhuTrach, int? TaiSanId, Dictionary<string, object?> GiaTri);

public record KetQuaChayQuyTac(QuyTacCanhBao QuyTac, List<DongCanhBao> Khop, List<string> NguoiNhan, int DaGui, int HanhDong);

/// <summary>
/// Quy tắc cảnh báo tuỳ chỉnh (FR-HT-15): nạp đối tượng theo loại, lọc phạm vi, đánh giá điều kiện, gửi thông báo
/// (chống lặp theo đối tượng, nhắc lại theo giờ) và thực hiện hành động tự động.
/// </summary>
public class QuyTacCanhBaoService(AppDbContext db, ThongBaoService tb, ThamSoService thamSo, UserManager<AppUser> users,
    SoPhieuService soPhieu, LichSuService lichSu, NganSachService nganSach)
{
    public const int GopKhiQua = 10;

    private static string T(Enum e) => VanThuService.TenEnum(e);
    private static readonly string[] TrangThaiTs = Enum.GetValues<TrangThaiTaiSan>().Select(x => T(x)).ToArray();
    private static string DsEnum<TE>() where TE : struct, Enum => string.Join("; ", Enum.GetValues<TE>().Select(x => T(x)));

    public static readonly IReadOnlyDictionary<DoiTuongCanhBao, List<TruongCanhBao>> DanhMucTruong = new Dictionary<DoiTuongCanhBao, List<TruongCanhBao>>
    {
        [DoiTuongCanhBao.TaiSan] =
        [
            new("ma", "Mã tài sản", "chuoi"), new("ten", "Tên tài sản", "chuoi"), new("loai", "Loại thiết bị", "chuoi"), new("khoa", "Khoa/phòng", "chuoi"),
            new("trang_thai", "Trạng thái", "chuoi", string.Join("; ", TrangThaiTs)), new("tuoi_nam", "Tuổi sử dụng (năm)", "so"),
            new("nguyen_gia", "Nguyên giá (đ)", "so"), new("han_bao_hanh", "Hạn bảo hành", "ngay"),
            new("so_lan_hong_12t", "Số lần hỏng 12 tháng", "so"), new("chi_phi_sc_12t", "Chi phí sửa chữa 12 tháng (đ)", "so"),
            new("ty_le_chi_phi", "Chi phí SC 12 tháng / nguyên giá (%)", "so"), new("chi_so_bo_dem", "Bộ đếm (số trang…)", "so"),
            new("ram", "RAM", "chuoi"), new("he_dieu_hanh", "Hệ điều hành", "chuoi"), new("dia_chi_ip", "Địa chỉ IP", "chuoi"),
            new("dang_sua", "Đang có phiếu sửa chữa mở", "bool")
        ],
        [DoiTuongCanhBao.PhieuSuaChua] =
        [
            new("so_phieu", "Số phiếu", "chuoi"), new("trang_thai", "Trạng thái", "chuoi", DsEnum<TrangThaiPhieuSuaChua>()),
            new("uu_tien", "Mức ưu tiên", "chuoi", DsEnum<MucDoUuTien>()), new("loai_cong_viec", "Loại công việc", "chuoi", DsEnum<LoaiCongViec>()),
            new("so_gio_mo", "Số giờ từ khi báo", "so"), new("qua_han_gio", "Số giờ quá hạn SLA (âm = còn hạn)", "so"),
            new("han_xu_ly", "Hạn xử lý", "ngay"), new("chua_giao", "Chưa giao kỹ thuật viên", "bool"), new("ky_thuat_vien", "Kỹ thuật viên", "chuoi"),
            new("tam_dung", "Đang tạm dừng SLA", "bool"), new("mo_ta", "Mô tả sự cố", "chuoi"), new("khoa", "Khoa/phòng", "chuoi")
        ],
        [DoiTuongCanhBao.KeHoachBaoTri] =
        [
            new("ten", "Tên kế hoạch", "chuoi"), new("trang_thai", "Trạng thái", "chuoi", DsEnum<TrangThaiKeHoach>()),
            new("ngay_tiep_theo", "Ngày thực hiện tiếp theo", "ngay"), new("chu_ky_ngay", "Chu kỳ (ngày)", "so"),
            new("ky_thuat_vien", "Kỹ thuật viên", "chuoi"), new("chua_giao", "Chưa giao kỹ thuật viên", "bool")
        ],
        [DoiTuongCanhBao.KetQuaKiemTra] =
        [
            new("mau", "Phiếu mẫu", "chuoi"), new("muc", "Nội dung mục", "chuoi"), new("gia_tri_so", "Giá trị đo", "so"),
            new("dat", "Đạt", "bool"), new("bat_thuong", "Bất thường theo ngưỡng mẫu", "bool"), new("ma_tai_san", "Mã tài sản", "chuoi"),
            new("nguoi_kiem", "Người kiểm tra", "chuoi"), new("so_gio_truoc", "Số giờ kể từ lúc kiểm tra", "so")
        ],
        [DoiTuongCanhBao.VatTu] =
        [
            new("ma", "Mã vật tư", "chuoi"), new("ten", "Tên vật tư", "chuoi"), new("ton_kho", "Tồn kho", "so"),
            new("ton_toi_thieu", "Tồn tối thiểu", "so"), new("duoi_dinh_muc", "Dưới định mức", "bool")
        ],
        [DoiTuongCanhBao.HopDong] =
        [
            new("so_hop_dong", "Số hợp đồng", "chuoi"), new("ten", "Tên hợp đồng", "chuoi"), new("loai", "Loại", "chuoi"),
            new("hieu_luc_den", "Hiệu lực đến", "ngay"), new("gia_tri", "Giá trị (đ)", "so"), new("phu_trach", "Cán bộ phụ trách", "chuoi")
        ],
        [DoiTuongCanhBao.BanQuyen] =
        [
            new("ten", "Phần mềm", "chuoi"), new("ngay_het_han", "Ngày hết hạn", "ngay"), new("so_luong", "Số giấy phép", "so"),
            new("da_cap", "Đã cấp phát", "so"), new("con_lai", "Còn lại", "so")
        ],
        [DoiTuongCanhBao.NganSach] =
        [
            new("ma", "Mã khoản mục", "chuoi"), new("ten", "Tên khoản mục", "chuoi"), new("ty_le_su_dung", "Tỷ lệ đã dùng (%)", "so"),
            new("con_lai", "Còn lại (đ)", "so"), new("du_toan", "Dự toán hiệu lực (đ)", "so"), new("thuc_chi", "Thực chi (đ)", "so")
        ],
        [DoiTuongCanhBao.DeXuatMuaSam] =
        [
            new("so_phieu", "Số đề xuất", "chuoi"), new("trang_thai", "Trạng thái", "chuoi", DsEnum<TrangThaiDeXuat>()),
            new("tong_du_toan", "Tổng dự toán (đ)", "so"), new("so_ngay_tu_khi_lap", "Số ngày từ khi lập", "so")
        ],
        [DoiTuongCanhBao.VanBan] =
        [
            new("so", "Số / ký hiệu", "chuoi"), new("huong", "Sổ", "chuoi", DsEnum<HuongVanBan>()), new("loai", "Loại", "chuoi", DsEnum<LoaiVanBan>()),
            new("trang_thai", "Trạng thái", "chuoi", DsEnum<TrangThaiVanBan>()), new("han_xu_ly", "Hạn xử lý", "ngay"),
            new("tien_do", "Tiến độ (%)", "so"), new("do_khan", "Độ khẩn", "chuoi", DsEnum<DoKhanVanBan>()), new("trich_yeu", "Trích yếu", "chuoi")
        ],
    };

    /// <summary>Danh mục trường gồm cả trường bổ sung tài sản (tt_MA).</summary>
    public async Task<Dictionary<DoiTuongCanhBao, List<TruongCanhBao>>> TatCaTruongAsync()
    {
        var kq = DanhMucTruong.ToDictionary(x => x.Key, x => x.Value.ToList());
        var tt = await db.TruongTuyChinhs.AsNoTracking().Where(t => t.DangDung).OrderBy(t => t.ThuTu).ToListAsync();
        foreach (var t in tt)
            kq[DoiTuongCanhBao.TaiSan].Add(new("tt_" + t.Ma, $"[Bổ sung] {t.Ten}", t.Kieu switch { KieuTruong.So => "so", KieuTruong.Ngay => "ngay", KieuTruong.CoKhong => "bool", _ => "chuoi" }, t.LuaChon));
        return kq;
    }

    public async Task<string> TenTruongAsync(DoiTuongCanhBao dt, string ma) =>
        (await TatCaTruongAsync()).GetValueOrDefault(dt)?.FirstOrDefault(x => x.Ma == ma)?.Ten ?? ma;

    // ===================== Nạp đối tượng =====================
    public async Task<List<DongCanhBao>> NapAsync(DoiTuongCanhBao dt, DateTime now)
    {
        var homNay = DateOnly.FromDateTime(now);
        static Dictionary<string, object?> D(params (string, object?)[] x) => x.ToDictionary(a => a.Item1, a => a.Item2);
        var moSc = new[] { TrangThaiPhieuSuaChua.DaNghiemThu, TrangThaiPhieuSuaChua.DaHuy };
        switch (dt)
        {
            case DoiTuongCanhBao.TaiSan:
            {
                var ts = await db.TaiSans.AsNoTracking().Include(t => t.LoaiThietBi).Include(t => t.KhoaPhong).Include(t => t.CauHinh)
                    .Include(t => t.GiaTriTuyChinhs).ThenInclude(g => g.TruongTuyChinh)
                    .Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy).ToListAsync();
                var tu = now.AddYears(-1);
                var sc = (await db.PhieuSuaChuas.AsNoTracking().Include(p => p.VatTus)
                        .Where(p => p.LoaiCongViec == LoaiCongViec.SuaChua && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy && p.NgayYeuCau >= tu).ToListAsync())
                    .GroupBy(p => p.TaiSanId).ToDictionary(g => g.Key, g => (SoLan: g.Count(), ChiPhi: g.Sum(p => p.TongChiPhi)));
                var dangSua = (await db.PhieuSuaChuas.Where(p => p.LoaiCongViec == LoaiCongViec.SuaChua
                    && p.TrangThai < TrangThaiPhieuSuaChua.HoanThanh).Select(p => p.TaiSanId).Distinct().ToListAsync()).ToHashSet();
                return ts.Select(t =>
                {
                    var bd = t.NgaySuDung ?? t.NgayMua;
                    var s = sc.GetValueOrDefault(t.Id);
                    var g = D(("ma", t.MaTaiSan), ("ten", t.Ten), ("loai", t.LoaiThietBi?.Ten), ("khoa", t.KhoaPhong?.Ten), ("trang_thai", T(t.TrangThai)),
                        ("tuoi_nam", bd is { } b ? Math.Round((decimal)(homNay.DayNumber - b.DayNumber) / 365.25m, 1) : null),
                        ("nguyen_gia", t.NguyenGia), ("han_bao_hanh", t.HanBaoHanh), ("so_lan_hong_12t", s.SoLan), ("chi_phi_sc_12t", s.ChiPhi),
                        ("ty_le_chi_phi", t.NguyenGia > 0 ? Math.Round(s.ChiPhi * 100 / t.NguyenGia, 1) : null), ("chi_so_bo_dem", t.ChiSoBoDem),
                        ("ram", t.CauHinh?.RAM), ("he_dieu_hanh", t.CauHinh?.HeDieuHanh), ("dia_chi_ip", t.CauHinh?.DiaChiIP), ("dang_sua", dangSua.Contains(t.Id)));
                    foreach (var v in t.GiaTriTuyChinhs.Where(v => v.TruongTuyChinh is not null))
                        g["tt_" + v.TruongTuyChinh!.Ma] = v.TruongTuyChinh.Kieu switch
                        {
                            KieuTruong.Ngay => DateOnly.TryParse(v.GiaTri, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
                            KieuTruong.CoKhong => v.GiaTri is "true" or "1",
                            _ => v.GiaTri
                        };
                    return new DongCanhBao(t.Id.ToString(), $"{t.MaTaiSan} – {t.Ten}", $"/TaiSan/Details/{t.Id}", t.KhoaPhongId, t.LoaiThietBiId, null, null, t.Id, g);
                }).ToList();
            }
            case DoiTuongCanhBao.PhieuSuaChua:
            {
                var ps = await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Include(p => p.KhoaPhong).Where(p => !moSc.Contains(p.TrangThai)).ToListAsync();
                return ps.Select(p => new DongCanhBao(p.Id.ToString(), $"{p.SoPhieu} – {p.TaiSan?.MaTaiSan ?? p.KhoaPhong?.Ten}", $"/PhieuSuaChua/Details/{p.Id}",
                    p.KhoaPhongId, p.TaiSan?.LoaiThietBiId, p.UuTien, p.KyThuatVien, p.TaiSanId,
                    D(("so_phieu", p.SoPhieu), ("trang_thai", T(p.TrangThai)), ("uu_tien", T(p.UuTien)), ("loai_cong_viec", T(p.LoaiCongViec)),
                      ("so_gio_mo", Math.Round((decimal)(now - p.NgayYeuCau).TotalHours, 1)),
                      ("qua_han_gio", p.TrangThai >= TrangThaiPhieuSuaChua.HoanThanh ? null : Math.Round((decimal)(now - p.HanXuLy).TotalHours, 1)),
                      ("han_xu_ly", p.HanXuLy), ("chua_giao", p.KyThuatVien is null), ("ky_thuat_vien", p.KyThuatVien), ("tam_dung", p.DangTamDung),
                      ("mo_ta", p.MoTaSuCo), ("khoa", p.KhoaPhong?.Ten)))).ToList();
            }
            case DoiTuongCanhBao.KeHoachBaoTri:
            {
                var ks = await db.KeHoachBaoTris.AsNoTracking().Include(k => k.TaiSan).Where(k => k.TrangThai != TrangThaiKeHoach.HetHieuLuc).ToListAsync();
                return ks.Select(k => new DongCanhBao(k.Id.ToString(), k.Ten, $"/KeHoachBaoTri/ChiTiet/{k.Id}", k.KhoaPhongId ?? k.TaiSan?.KhoaPhongId,
                    k.LoaiThietBiId ?? k.TaiSan?.LoaiThietBiId, null, k.KyThuatVien, k.TaiSanId,
                    D(("ten", k.Ten), ("trang_thai", T(k.TrangThai)), ("ngay_tiep_theo", k.NgayTiepTheo), ("chu_ky_ngay", k.ChuKyNgay),
                      ("ky_thuat_vien", k.KyThuatVien), ("chua_giao", k.KyThuatVien is null)))).ToList();
            }
            case DoiTuongCanhBao.KetQuaKiemTra:
            {
                var tu = now.AddDays(-3);
                var kq = await db.KetQuaKiemTras.AsNoTracking().Include(k => k.LanKiemTra!).ThenInclude(l => l.MauKiemTra)
                    .Include(k => k.LanKiemTra!).ThenInclude(l => l.TaiSan).Where(k => k.LanKiemTra!.ThoiGian >= tu).ToListAsync();
                return kq.Select(k =>
                {
                    var l = k.LanKiemTra!;
                    return new DongCanhBao(k.Id.ToString(), $"{l.MauKiemTra?.Ten} · {k.NoiDungMuc}: {(k.GiaTriSo is { } s ? $"{s:0.##} {k.DonVi}" : k.Dat == false ? "Không đạt" : k.GiaTriText ?? "Đạt")} ({l.ThoiGian:dd/MM HH:mm})",
                        $"/KiemTra/Lan/{l.Id}", l.TaiSan?.KhoaPhongId ?? l.MauKiemTra?.KhoaPhongId, l.TaiSan?.LoaiThietBiId, null, l.NguoiKiem, l.TaiSanId,
                        D(("mau", l.MauKiemTra?.Ten), ("muc", k.NoiDungMuc), ("gia_tri_so", k.GiaTriSo), ("dat", k.Dat), ("bat_thuong", k.BatThuong),
                          ("ma_tai_san", l.TaiSan?.MaTaiSan), ("nguoi_kiem", l.NguoiKiem), ("so_gio_truoc", Math.Round((decimal)(now - l.ThoiGian).TotalHours, 1))));
                }).ToList();
            }
            case DoiTuongCanhBao.VatTu:
                return (await db.VatTus.AsNoTracking().ToListAsync()).Select(v => new DongCanhBao(v.Id.ToString(), $"{v.Ma} – {v.Ten}", $"/Kho/TheKho?vatTuId={v.Id}",
                    null, null, null, null, null, D(("ma", v.Ma), ("ten", v.Ten), ("ton_kho", v.TonKho), ("ton_toi_thieu", v.TonToiThieu), ("duoi_dinh_muc", v.DuoiTonToiThieu)))).ToList();
            case DoiTuongCanhBao.HopDong:
                return (await db.HopDongs.AsNoTracking().ToListAsync()).Select(h => new DongCanhBao(h.Id.ToString(), $"{h.SoHopDong} – {h.Ten}", $"/HopDong/Details/{h.Id}",
                    null, null, null, h.NguoiPhuTrach, null, D(("so_hop_dong", h.SoHopDong), ("ten", h.Ten), ("loai", T(h.Loai)), ("hieu_luc_den", h.HieuLucDen),
                    ("gia_tri", h.GiaTri), ("phu_trach", h.NguoiPhuTrach)))).ToList();
            case DoiTuongCanhBao.BanQuyen:
                return (await db.BanQuyenPhanMems.AsNoTracking().Select(b => new { b.Id, b.Ten, b.NgayHetHan, b.SoLuong, DaCap = b.CapPhats.Count }).ToListAsync())
                    .Select(b => new DongCanhBao(b.Id.ToString(), b.Ten, $"/BanQuyen/Details/{b.Id}", null, null, null, null, null,
                        D(("ten", b.Ten), ("ngay_het_han", b.NgayHetHan), ("so_luong", b.SoLuong), ("da_cap", b.DaCap), ("con_lai", b.SoLuong - b.DaCap)))).ToList();
            case DoiTuongCanhBao.NganSach:
                return (await nganSach.SuDungAsync(now.Year)).Select(n => new DongCanhBao(n.KhoanMuc.Id.ToString(), $"{n.KhoanMuc.Ma} – {n.KhoanMuc.Ten} ({n.KhoanMuc.Nam})",
                    $"/NganSach/ChiTiet/{n.KhoanMuc.Id}", null, null, null, null, null,
                    D(("ma", n.KhoanMuc.Ma), ("ten", n.KhoanMuc.Ten), ("ty_le_su_dung", n.TyLe), ("con_lai", n.ConLai), ("du_toan", n.DuToan), ("thuc_chi", n.ThucChi)))).ToList();
            case DoiTuongCanhBao.DeXuatMuaSam:
            {
                var mo = new[] { TrangThaiDeXuat.Nhap, TrangThaiDeXuat.ChoDuyet, TrangThaiDeXuat.DaDuyet, TrangThaiDeXuat.DatMotPhan, TrangThaiDeXuat.TuChoi };
                return (await db.DeXuatMuaSams.AsNoTracking().Include(x => x.Dongs).Where(x => mo.Contains(x.TrangThai)).ToListAsync())
                    .Select(x => new DongCanhBao(x.Id.ToString(), $"{x.SoPhieu} – {x.TieuDe}", $"/MuaSam/DeXuat/{x.Id}", null, null, null, x.NguoiTao, null,
                        D(("so_phieu", x.SoPhieu), ("trang_thai", T(x.TrangThai)), ("tong_du_toan", x.TongDuToan), ("so_ngay_tu_khi_lap", (now.Date - x.NgayTao.Date).Days)))).ToList();
            }
            case DoiTuongCanhBao.VanBan:
            {
                var ds = await db.VanBans.AsNoTracking().Where(x => x.TrangThai != TrangThaiVanBan.HoanThanh && x.TrangThai != TrangThaiVanBan.DaHuy).ToListAsync();
                return ds.Select(x => new DongCanhBao(x.Id.ToString(), $"{x.SoHienThi} – {x.TrichYeu}", $"/VanThu/ChiTiet/{x.Id}", null, null, null, x.NguoiChuTri ?? x.NguoiSoan, null,
                    D(("so", x.SoHienThi), ("huong", T(x.Huong)), ("loai", T(x.Loai)), ("trang_thai", T(x.TrangThai)), ("han_xu_ly", x.HanXuLy),
                      ("tien_do", x.TienDo), ("do_khan", T(x.DoKhan)), ("trich_yeu", x.TrichYeu)))).ToList();
            }
            default: return [];
        }
    }

    private static bool TrongPhamVi(QuyTacCanhBao q, DongCanhBao d) =>
        (q.KhoaPhongId is null || d.KhoaPhongId == q.KhoaPhongId) && (q.LoaiThietBiId is null || d.LoaiThietBiId == q.LoaiThietBiId)
        && (q.UuTienTu is null || (d.UuTien is { } u && u >= q.UuTienTu));

    /// <summary>Các đối tượng thoả quy tắc (không gửi gì).</summary>
    public async Task<List<DongCanhBao>> TimAsync(QuyTacCanhBao q, DateTime now) =>
        (await NapAsync(q.DoiTuong, now)).Where(d => TrongPhamVi(q, d) && q.Khop(d.GiaTri, DateOnly.FromDateTime(now))).ToList();

    /// <summary>Người nhận cố định (người cụ thể + vai trò) của quy tắc.</summary>
    public async Task<List<string>> NguoiNhanCoDinhAsync(QuyTacCanhBao q)
    {
        var ds = q.DsNguoiNhan.ToList();
        foreach (var r in q.DsVaiTro.Where(r => VaiTro.TatCa.Contains(r)))
            ds.AddRange((await users.GetUsersInRoleAsync(r)).Where(u => u.DangHoatDong).Select(u => u.UserName!));
        return ds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<List<string>> NguoiNhanAsync(QuyTacCanhBao q, List<string> coDinh, DongCanhBao d)
    {
        var ds = coDinh.ToList();
        if (q.GuiNguoiPhuTrach && !string.IsNullOrWhiteSpace(d.PhuTrach)) ds.Add(d.PhuTrach);
        if (q.GuiTruongKhoa) ds.AddRange(await tb.TruongKhoaAsync(d.KhoaPhongId));
        return ds.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    // ===================== Chạy =====================
    public async Task ChayTatCaAsync(DateTime now, CancellationToken ct = default)
    {
        var ids = await db.QuyTacCanhBaos.Where(q => q.DangBat).Select(q => q.Id).ToListAsync(ct);
        foreach (var id in ids)
        {
            var q = await db.QuyTacCanhBaos.Include(x => x.DieuKiens).FirstAsync(x => x.Id == id, ct);
            try { await ChayAsync(q, now); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Một quy tắc lỗi không chặn các quy tắc khác; lưu lỗi để Quản trị thấy trên danh sách.
                db.ChangeTracker.Clear();
                await db.QuyTacCanhBaos.Where(x => x.Id == id).ExecuteUpdateAsync(u => u
                    .SetProperty(x => x.LoiCuoi, (ex.Message.Length > 480 ? ex.Message[..480] : ex.Message))
                    .SetProperty(x => x.LanChayCuoi, now), ct);
            }
        }
    }

    public async Task<KetQuaChayQuyTac> ChayAsync(QuyTacCanhBao q, DateTime now)
    {
        var khop = await TimAsync(q, now);
        var coDinh = await NguoiNhanCoDinhAsync(q);
        var daGui = await db.CanhBaoDaGuis.Where(x => x.QuyTacCanhBaoId == q.Id).ToDictionaryAsync(x => x.DoiTuongId);
        var idKhop = khop.Select(k => k.Id).ToHashSet();
        foreach (var x in daGui.Values.Where(x => x.DangKhop && !idKhop.Contains(x.DoiTuongId))) x.DangKhop = false;

        var canGui = new List<(DongCanhBao Dong, bool LanDau)>();
        foreach (var d in khop)
        {
            var r = daGui.GetValueOrDefault(d.Id);
            var lanDau = r is null || !r.DangKhop;
            if (!lanDau && (q.NhacLaiSauGio <= 0 || now - r!.LanCuoi < TimeSpan.FromHours(q.NhacLaiSauGio))) continue;
            if (r is null) { r = new CanhBaoDaGui { QuyTacCanhBaoId = q.Id, DoiTuongId = d.Id, LanDau = now }; db.CanhBaoDaGuis.Add(r); daGui[d.Id] = r; }
            r.LanCuoi = now; r.SoLan++; r.DangKhop = true;
            canGui.Add((d, lanDau));
        }

        var hanhDong = 0;
        foreach (var (d, lanDau) in canGui.Where(x => x.LanDau)) if (await HanhDongAsync(q, d, now)) hanhDong++;

        var tatCaNhan = new HashSet<string>(coDinh, StringComparer.OrdinalIgnoreCase);
        var lienKetQuyTac = $"/QuyTacCanhBao/ChayThu/{q.Id}";
        var dieuKien = q.MoTaDieuKien(m => DanhMucTruong.GetValueOrDefault(q.DoiTuong)?.FirstOrDefault(t => t.Ma == m)?.Ten ?? m);
        if (canGui.Count > GopKhiQua)
        {
            // Gộp một thông báo cho người nhận cố định; người phụ trách / trưởng khoa nhận riêng phần của mình.
            var dong = string.Join("\n", canGui.Take(GopKhiQua).Select(x => "• " + x.Dong.Ten)) + $"\n… và {canGui.Count - GopKhiQua} đối tượng khác.";
            await tb.GuiAsync(coDinh, LoaiThongBao.CanhBaoTuyChinh, $"[{q.Ten}] {canGui.Count} đối tượng thoả điều kiện",
                $"{q.MoTa}\nĐiều kiện: {dieuKien}\n{dong}".Trim(), lienKetQuyTac, q.MucDo, chiTrongUngDung: q.ChiTrongUngDung);
            foreach (var g in canGui.GroupBy(x => x.Dong.PhuTrach))
                foreach (var (d, _) in g)
                {
                    var rieng = (await NguoiNhanAsync(q, [], d)).Where(n => !coDinh.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
                    if (rieng.Count == 0) continue;
                    rieng.ForEach(n => tatCaNhan.Add(n));
                    await tb.GuiAsync(rieng, LoaiThongBao.CanhBaoTuyChinh, $"[{q.Ten}] {d.Ten}", $"{q.MoTa}\nĐiều kiện: {dieuKien}".Trim(), d.Link, q.MucDo, chiTrongUngDung: q.ChiTrongUngDung);
                }
        }
        else
            foreach (var (d, lanDau) in canGui)
            {
                var nhan = await NguoiNhanAsync(q, coDinh, d);
                nhan.ForEach(n => tatCaNhan.Add(n));
                await tb.GuiAsync(nhan, LoaiThongBao.CanhBaoTuyChinh, $"[{q.Ten}] {d.Ten}{(lanDau ? "" : " (nhắc lại)")}",
                    $"{q.MoTa}\nĐiều kiện: {dieuKien}".Trim(), d.Link, q.MucDo, chiTrongUngDung: q.ChiTrongUngDung);
            }

        q.LanChayCuoi = now; q.LoiCuoi = null;
        q.SoLanGui += canGui.Count;
        await db.SaveChangesAsync();
        return new KetQuaChayQuyTac(q, khop, tatCaNhan.ToList(), canGui.Count, hanhDong);
    }

    private async Task<bool> HanhDongAsync(QuyTacCanhBao q, DongCanhBao d, DateTime now)
    {
        switch (q.HanhDong)
        {
            case HanhDongCanhBao.NangUuTien when q.DoiTuong == DoiTuongCanhBao.PhieuSuaChua && int.TryParse(d.Id, out var pid):
            {
                var p = await db.PhieuSuaChuas.Include(x => x.KhoaPhong).FirstOrDefaultAsync(x => x.Id == pid);
                if (p is null || p.UuTien >= MucDoUuTien.KhanCap || p.TrangThai >= TrangThaiPhieuSuaChua.HoanThanh) return false;
                var cu = p.UuTien;
                p.UuTien = cu + 1;
                var hanMoi = thamSo.TinhHanSla(p.NgayYeuCau, p.UuTien, p.KhoaPhong?.LaKhoaTrongYeu == true);
                if (hanMoi < p.HanXuLy) p.HanXuLy = hanMoi;
                db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = p.Id, ThoiGian = now, NguoiViet = "Hệ thống", LaHeThong = true,
                    NoiDung = $"Quy tắc \"{q.Ten}\": nâng ưu tiên {T(cu)} → {T(p.UuTien)}, hạn xử lý {p.HanXuLy:HH:mm dd/MM}." });
                return true;
            }
            case HanhDongCanhBao.TaoYeuCauSuaChua when d.TaiSanId is { } tsId:
            {
                if (await db.PhieuSuaChuas.AnyAsync(p => p.TaiSanId == tsId && p.LoaiCongViec == LoaiCongViec.SuaChua && p.TrangThai < TrangThaiPhieuSuaChua.HoanThanh)) return false;
                var ts = await db.TaiSans.Include(t => t.KhoaPhong).FirstAsync(t => t.Id == tsId);
                var p = new PhieuSuaChua
                {
                    TaiSanId = ts.Id, KhoaPhongId = ts.KhoaPhongId, NguoiYeuCau = "Hệ thống", NguoiTao = "Hệ thống",
                    MoTaSuCo = $"[Quy tắc cảnh báo – {q.Ten}] {d.Ten}" + (string.IsNullOrWhiteSpace(q.MoTa) ? "" : $"\n{q.MoTa}"),
                    UuTien = MucDoUuTien.Cao, NgayYeuCau = now, HanXuLy = thamSo.TinhHanSla(now, MucDoUuTien.Cao, ts.KhoaPhong?.LaKhoaTrongYeu == true)
                };
                db.PhieuSuaChuas.Add(p);
                await db.LuuMoiAsync(soPhieu, "SC", so => p.SoPhieu = so);
                lichSu.Ghi(ts.Id, LoaiSuKien.SuaChua, $"Quy tắc cảnh báo \"{q.Ten}\" → tự tạo {p.SoPhieu}", "Hệ thống");
                await tb.PhieuMoiAsync(p, ts.KhoaPhong?.Ten, $"{ts.MaTaiSan} {ts.Ten}");
                return true;
            }
            default: return false;
        }
    }
}
