using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum HuongVanBan
{
    [Display(Name = "Văn bản đến")] Den = 1,
    [Display(Name = "Văn bản đi / tờ trình")] Di
}

public enum LoaiVanBan
{
    [Display(Name = "Công văn")] CongVan = 1,
    [Display(Name = "Tờ trình")] ToTrinh,
    [Display(Name = "Quyết định")] QuyetDinh,
    [Display(Name = "Kế hoạch")] KeHoach,
    [Display(Name = "Báo cáo")] BaoCao,
    [Display(Name = "Thông báo")] ThongBao,
    [Display(Name = "Giấy mời")] GiayMoi,
    [Display(Name = "Biên bản")] BienBan,
    [Display(Name = "Đề án / phương án")] PhuongAn,
    [Display(Name = "Khác")] Khac
}

public enum DoKhanVanBan
{
    [Display(Name = "Thường")] Thuong = 1,
    [Display(Name = "Khẩn")] Khan,
    [Display(Name = "Thượng khẩn")] ThuongKhan,
    [Display(Name = "Hoả tốc")] HoaToc
}

public enum TrangThaiVanBan
{
    [Display(Name = "Dự thảo")] DuThao = 1,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Bị trả lại")] TraLai,
    [Display(Name = "Đã duyệt – chờ phát hành")] DaDuyet,
    [Display(Name = "Đã phát hành")] DaPhatHanh,
    [Display(Name = "Chờ phân công")] ChoPhanCong,
    [Display(Name = "Đang xử lý")] DangXuLy,
    [Display(Name = "Hoàn thành")] HoanThanh,
    [Display(Name = "Đã huỷ")] DaHuy
}

/// <summary>
/// Văn bản của văn thư Phòng CNTT.
/// Đến: vào sổ (số đến theo năm) → phân công chủ trì/phối hợp + hạn → cập nhật tiến độ → hoàn thành (kết quả).
/// Đi / tờ trình: dự thảo → trình duyệt tuần tự nhiều người (trả lại để sửa, trình lại) → đã duyệt →
/// văn thư cấp số đi + ký hiệu, phát hành → (nếu cần theo dõi) theo dõi thực hiện đến khi hoàn thành.
/// </summary>
public class VanBan : BaseEntity
{
    [Display(Name = "Loại sổ")] public HuongVanBan Huong { get; set; }
    [Display(Name = "Loại văn bản")] public LoaiVanBan Loai { get; set; } = LoaiVanBan.CongVan;
    [Display(Name = "Năm sổ")] public int Nam { get; set; }
    [Display(Name = "Số đến")] public int? SoDen { get; set; }
    [Display(Name = "Số đi")] public int? SoDi { get; set; }
    [StringLength(60), Display(Name = "Số, ký hiệu")] public string? SoKyHieu { get; set; }
    [Display(Name = "Ngày văn bản")] public DateTime? NgayVanBan { get; set; }
    [Display(Name = "Ngày đến")] public DateTime? NgayDen { get; set; }
    [StringLength(300), Display(Name = "Cơ quan ban hành / nơi gửi")] public string? CoQuanBanHanh { get; set; }
    [StringLength(500), Display(Name = "Nơi nhận")] public string? NoiNhan { get; set; }
    [Required(ErrorMessage = "Nhập trích yếu."), StringLength(500), Display(Name = "Trích yếu nội dung")] public string TrichYeu { get; set; } = "";
    [Display(Name = "Nội dung")] public string? NoiDung { get; set; }
    [Display(Name = "Độ khẩn")] public DoKhanVanBan DoKhan { get; set; } = DoKhanVanBan.Thuong;
    [StringLength(100), Display(Name = "Người ký")] public string? NguoiKy { get; set; }
    [StringLength(100), Display(Name = "Chức vụ người ký")] public string? ChucVuNguoiKy { get; set; }
    [Display(Name = "Hạn xử lý")] public DateTime? HanXuLy { get; set; }
    public TrangThaiVanBan TrangThai { get; set; }
    [StringLength(100), Display(Name = "Người soạn")] public string? NguoiSoan { get; set; }
    [StringLength(100), Display(Name = "Người chủ trì")] public string? NguoiChuTri { get; set; }
    [StringLength(500), Display(Name = "Người phối hợp")] public string? NguoiPhoiHop { get; set; }
    [StringLength(1000), Display(Name = "Ý kiến chỉ đạo")] public string? YKienChiDao { get; set; }
    [Range(0, 100), Display(Name = "Tiến độ (%)")] public int TienDo { get; set; }
    [StringLength(2000), Display(Name = "Kết quả xử lý")] public string? KetQua { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    public DateTime? NgayPhatHanh { get; set; }
    [Display(Name = "Theo dõi thực hiện sau phát hành")] public bool TheoDoiThucHien { get; set; }
    [Display(Name = "Trả lời / liên quan văn bản")] public int? VanBanLienQuanId { get; set; }
    public VanBan? VanBanLienQuan { get; set; }
    [Display(Name = "Đề xuất mua sắm")] public int? DeXuatMuaSamId { get; set; }
    public DeXuatMuaSam? DeXuatMuaSam { get; set; }
    public int LuotGui { get; set; }
    public int BuocHienTai { get; set; }
    public DateTime? NgayNhacCuoi { get; set; }
    [StringLength(300)] public string? LyDoHuy { get; set; }
    public List<BuocDuyetVanBan> BuocDuyets { get; set; } = [];
    public List<NhatKyVanBan> NhatKys { get; set; } = [];

    public bool LaToTrinh => Loai == LoaiVanBan.ToTrinh;
    public bool KetThuc => TrangThai is TrangThaiVanBan.HoanThanh or TrangThaiVanBan.DaHuy || (TrangThai == TrangThaiVanBan.DaPhatHanh && !TheoDoiThucHien);
    public bool DuocSua => Huong == HuongVanBan.Den ? TrangThai is TrangThaiVanBan.ChoPhanCong or TrangThaiVanBan.DangXuLy : TrangThai is TrangThaiVanBan.DuThao or TrangThaiVanBan.TraLai;
    public bool QuaHan(DateTime now) => !KetThuc && HanXuLy is { } h && h.Date < now.Date;
    public bool SapDenHan(DateTime now, int ngay) => !KetThuc && HanXuLy is { } h && h.Date >= now.Date && h.Date <= now.Date.AddDays(ngay);
    public string SoHienThi => Huong == HuongVanBan.Den ? (SoDen is { } d ? $"Đến {d}/{Nam}" : "") : (SoKyHieu ?? (SoDi is { } s ? $"{s}" : "(chưa cấp số)"));

    public IEnumerable<BuocDuyetVanBan> CacBuocLuotNay => BuocDuyets.Where(b => b.LuotGui == LuotGui).OrderBy(b => b.ThuTu);
    public BuocDuyetVanBan? BuocDangCho => TrangThai == TrangThaiVanBan.ChoDuyet ? CacBuocLuotNay.FirstOrDefault(b => b.ThuTu == BuocHienTai) : null;
    public IEnumerable<string> NguoiPhoiHopDs => (NguoiPhoiHop ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    public bool LaNguoiXuLy(string? user) => user is not null &&
        (string.Equals(NguoiChuTri, user, StringComparison.OrdinalIgnoreCase) || NguoiPhoiHopDs.Contains(user, StringComparer.OrdinalIgnoreCase));

    /// <summary>Chữ viết tắt tên loại văn bản (Phụ lục III Nghị định 30/2020/NĐ-CP).</summary>
    public static string VietTat(LoaiVanBan l) => l switch
    {
        LoaiVanBan.ToTrinh => "TTr", LoaiVanBan.QuyetDinh => "QĐ", LoaiVanBan.KeHoach => "KH", LoaiVanBan.BaoCao => "BC",
        LoaiVanBan.ThongBao => "TB", LoaiVanBan.GiayMoi => "GM", LoaiVanBan.BienBan => "BB", LoaiVanBan.PhuongAn => "PA", _ => ""
    };

    /// <summary>Ký hiệu văn bản đi: công văn "12/CNTT"; loại khác "12/TTr-CNTT".</summary>
    public static string TaoSoKyHieu(int so, LoaiVanBan loai, string donVi)
    {
        var vt = VietTat(loai);
        donVi = donVi.Trim();
        return string.IsNullOrEmpty(vt) ? $"{so}/{donVi}" : $"{so}/{vt}-{donVi}";
    }

    private void Ghi(string nguoi, string noiDung, DateTime now, int? tienDo = null) =>
        NhatKys.Add(new NhatKyVanBan { NguoiDung = nguoi, NoiDung = noiDung, ThoiGian = now, TienDo = tienDo });

    // ===== Văn bản đến =====
    public void VaoSoDen(int soDen, string nguoi, DateTime now)
    {
        if (Huong != HuongVanBan.Den) throw new DomainException("Chỉ vào sổ đến cho văn bản đến.");
        if (string.IsNullOrWhiteSpace(CoQuanBanHanh)) throw new DomainException("Nhập cơ quan ban hành / nơi gửi.");
        NgayDen ??= now;
        if (NgayVanBan > NgayDen) throw new DomainException("Ngày văn bản không được sau ngày đến.");
        SoDen = soDen; Nam = NgayDen.Value.Year;
        TrangThai = TrangThaiVanBan.ChoPhanCong;
        Ghi(nguoi, $"Vào sổ văn bản đến số {soDen}/{Nam}", now);
    }

    public void PhanCong(string chuTri, string? phoiHop, DateTime? han, string? yKien, string nguoi, DateTime now)
    {
        var moTheoDoi = Huong == HuongVanBan.Di && TrangThai is TrangThaiVanBan.DaPhatHanh or TrangThaiVanBan.DangXuLy && TheoDoiThucHien;
        if (!(Huong == HuongVanBan.Den && TrangThai is TrangThaiVanBan.ChoPhanCong or TrangThaiVanBan.DangXuLy) && !moTheoDoi)
            throw new DomainException("Chỉ phân công văn bản đến đang chờ phân công/đang xử lý, hoặc văn bản đi cần theo dõi thực hiện.");
        if (string.IsNullOrWhiteSpace(chuTri)) throw new DomainException("Chọn người chủ trì xử lý.");
        if (han is { } h && h.Date < now.Date) throw new DomainException("Hạn xử lý không được trước hôm nay.");
        var ph = string.Join(",", (phoiHop ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.Equals(x, chuTri, StringComparison.OrdinalIgnoreCase)).Distinct(StringComparer.OrdinalIgnoreCase));
        var giaoLai = NguoiChuTri is not null && TrangThai == TrangThaiVanBan.DangXuLy;
        NguoiChuTri = chuTri; NguoiPhoiHop = ph.Length > 0 ? ph : null; HanXuLy = han; NgayNhacCuoi = null;
        if (!string.IsNullOrWhiteSpace(yKien)) YKienChiDao = yKien.Trim();
        TrangThai = TrangThaiVanBan.DangXuLy;
        Ghi(nguoi, $"{(giaoLai ? "Giao lại" : "Phân công")}: chủ trì {chuTri}{(NguoiPhoiHop is null ? "" : $", phối hợp {NguoiPhoiHop}")}{(han is null ? "" : $", hạn {han:dd/MM/yyyy}")}{(string.IsNullOrWhiteSpace(yKien) ? "" : $" — “{yKien.Trim()}”")}", now);
    }

    public void CapNhatTienDo(int tienDo, string noiDung, string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiVanBan.DangXuLy) throw new DomainException("Chỉ cập nhật tiến độ khi văn bản đang xử lý.");
        if (tienDo is < 0 or > 100) throw new DomainException("Tiến độ từ 0 đến 100%.");
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung công việc đã làm.");
        TienDo = tienDo;
        Ghi(nguoi, noiDung.Trim(), now, tienDo);
    }

    public void HoanThanh(string ketQua, string nguoi, DateTime now, int? vanBanTraLoiId = null)
    {
        if (TrangThai is not (TrangThaiVanBan.DangXuLy or TrangThaiVanBan.ChoPhanCong)) throw new DomainException("Văn bản không ở trạng thái đang xử lý.");
        if (string.IsNullOrWhiteSpace(ketQua)) throw new DomainException("Nhập kết quả xử lý.");
        KetQua = ketQua.Trim(); TienDo = 100; NgayHoanThanh = now; TrangThai = TrangThaiVanBan.HoanThanh;
        if (vanBanTraLoiId is not null && Huong == HuongVanBan.Den) VanBanLienQuanId ??= vanBanTraLoiId;
        Ghi(nguoi, $"Hoàn thành: {KetQua}", now, 100);
    }

    // ===== Văn bản đi / tờ trình =====
    public void KhoiTaoDuThao(string nguoi, DateTime now)
    {
        Huong = HuongVanBan.Di; TrangThai = TrangThaiVanBan.DuThao; NguoiSoan ??= nguoi; Nam = now.Year;
        if (LaToTrinh) TheoDoiThucHien = true;
        Ghi(nguoi, "Tạo dự thảo", now);
    }

    public void TrinhDuyet(IReadOnlyList<string> nguoiDuyet, string nguoi, DateTime now)
    {
        if (Huong != HuongVanBan.Di || TrangThai is not (TrangThaiVanBan.DuThao or TrangThaiVanBan.TraLai))
            throw new DomainException("Chỉ trình duyệt văn bản dự thảo hoặc bị trả lại.");
        if (string.IsNullOrWhiteSpace(NoiDung)) throw new DomainException("Chưa soạn nội dung văn bản.");
        var ds = nguoiDuyet.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (ds.Count == 0) throw new DomainException("Chọn ít nhất một người duyệt.");
        if (ds.Count > 6) throw new DomainException("Tối đa 6 bước duyệt.");
        LuotGui++; BuocHienTai = 1; TrangThai = TrangThaiVanBan.ChoDuyet; NgayNhacCuoi = null;
        for (var i = 0; i < ds.Count; i++) BuocDuyets.Add(new BuocDuyetVanBan { LuotGui = LuotGui, ThuTu = i + 1, NguoiDuyet = ds[i] });
        Ghi(nguoi, $"Trình duyệt{(LuotGui > 1 ? $" lần {LuotGui}" : "")}: {string.Join(" → ", ds)}", now);
    }

    /// <summary>Người duyệt bước hiện tại cho ý kiến. Trả về true nếu đã duyệt xong toàn bộ.</summary>
    public bool Duyet(string nguoi, bool dongY, string? yKien, DateTime now)
    {
        var b = BuocDangCho ?? throw new DomainException("Văn bản không ở trạng thái chờ duyệt.");
        if (!string.Equals(b.NguoiDuyet, nguoi, StringComparison.OrdinalIgnoreCase)) throw new DomainException($"Bước hiện tại do {b.NguoiDuyet} duyệt.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Nhập ý kiến khi trả lại.");
        b.DongY = dongY; b.YKien = yKien?.Trim(); b.ThoiGian = now; NgayNhacCuoi = null;
        if (!dongY)
        {
            TrangThai = TrangThaiVanBan.TraLai;
            Ghi(nguoi, $"Trả lại (bước {b.ThuTu}): {b.YKien}", now);
            return false;
        }
        Ghi(nguoi, $"Đồng ý (bước {b.ThuTu}){(string.IsNullOrWhiteSpace(yKien) ? "" : $": {b.YKien}")}", now);
        if (CacBuocLuotNay.All(x => x.DongY == true)) { TrangThai = TrangThaiVanBan.DaDuyet; return true; }
        BuocHienTai++;
        return false;
    }

    public void PhatHanh(int soDi, string donVi, DateTime ngay, string? nguoiKy, string? chucVu, string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiVanBan.DaDuyet) throw new DomainException("Chỉ phát hành văn bản đã duyệt xong.");
        if (string.IsNullOrWhiteSpace(NoiNhan)) throw new DomainException("Nhập nơi nhận trước khi phát hành.");
        SoDi = soDi; Nam = ngay.Year; SoKyHieu = TaoSoKyHieu(soDi, Loai, donVi); NgayVanBan = ngay.Date; NgayPhatHanh = now;
        if (!string.IsNullOrWhiteSpace(nguoiKy)) NguoiKy = nguoiKy.Trim();
        if (!string.IsNullOrWhiteSpace(chucVu)) ChucVuNguoiKy = chucVu.Trim();
        if (TheoDoiThucHien) { TrangThai = TrangThaiVanBan.DangXuLy; NguoiChuTri ??= NguoiSoan; }
        else TrangThai = TrangThaiVanBan.DaPhatHanh;
        Ghi(nguoi, $"Cấp số {SoKyHieu} ngày {ngay:dd/MM/yyyy}, phát hành tới: {NoiNhan}", now);
    }

    public void Huy(string lyDo, string nguoi, DateTime now)
    {
        if (TrangThai is TrangThaiVanBan.DaPhatHanh or TrangThaiVanBan.HoanThanh or TrangThaiVanBan.DaHuy || SoDi is not null)
            throw new DomainException("Văn bản đã phát hành/hoàn thành không huỷ được.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do huỷ.");
        LyDoHuy = lyDo.Trim(); TrangThai = TrangThaiVanBan.DaHuy;
        Ghi(nguoi, $"Huỷ: {LyDoHuy}", now);
    }

    public void GhiChu(string noiDung, string nguoi, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung.");
        Ghi(nguoi, noiDung.Trim(), now);
    }
}

public class BuocDuyetVanBan
{
    public int Id { get; set; }
    public int VanBanId { get; set; }
    public int LuotGui { get; set; }
    public int ThuTu { get; set; }
    [StringLength(100)] public string NguoiDuyet { get; set; } = "";
    public bool? DongY { get; set; }
    [StringLength(1000)] public string? YKien { get; set; }
    public DateTime? ThoiGian { get; set; }
}

/// <summary>Dòng thời gian xử lý văn bản: phân công, tiến độ, duyệt, phát hành, ghi chú.</summary>
public class NhatKyVanBan
{
    public long Id { get; set; }
    public int VanBanId { get; set; }
    public DateTime ThoiGian { get; set; }
    [StringLength(100)] public string NguoiDung { get; set; } = "";
    [StringLength(2000)] public string NoiDung { get; set; } = "";
    public int? TienDo { get; set; }
}

/// <summary>Mẫu văn bản để soạn nhanh. Nội dung có thể chứa {{NGAY}}, {{DON_VI}}, {{BENH_VIEN}}, {{NGUOI_SOAN}}, {{TRICH_YEU}}, {{DE_XUAT}}.</summary>
public class MauVanBan : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên mẫu")] public string Ten { get; set; } = "";
    [Display(Name = "Loại văn bản")] public LoaiVanBan Loai { get; set; } = LoaiVanBan.ToTrinh;
    [StringLength(500), Display(Name = "Trích yếu gợi ý")] public string? TrichYeu { get; set; }
    [Required, Display(Name = "Nội dung mẫu")] public string NoiDung { get; set; } = "";
    [Display(Name = "Đang dùng")] public bool DangDung { get; set; } = true;
}
