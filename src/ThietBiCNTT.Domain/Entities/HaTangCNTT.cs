using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

// =====================================================================================
// GĐ3.1 – Quản lý hạ tầng CNTT theo JCI phiên bản 8 (docs/JCI8-HaTangCNTT.md)
// Phân hệ HTT (4.1), DT (4.2), SL (4.3), HD (mục 5).
// =====================================================================================

#region 4.1 Hệ thống thông tin (HTT)

public enum LoaiHeThong
{
    [Display(Name = "Lâm sàng")] LamSang = 1,
    [Display(Name = "Cận lâm sàng")] CanLamSang,
    [Display(Name = "Hành chính – tài chính")] HanhChinh,
    [Display(Name = "Hạ tầng dùng chung")] HaTangDungChung
}

/// <summary>FR-HTT-03: 4 mức trọng yếu.</summary>
public enum MucTrongYeu
{
    [Display(Name = "Tối quan trọng")] ToiQuanTrong = 1,
    [Display(Name = "Cao")] Cao,
    [Display(Name = "Trung bình")] TrungBinh,
    [Display(Name = "Thấp")] Thap
}

public enum TrangThaiHeThong
{
    [Display(Name = "Đang triển khai")] DangTrienKhai = 1,
    [Display(Name = "Vận hành")] VanHanh,
    [Display(Name = "Ngừng sử dụng")] NgungSuDung
}

/// <summary>FR-HTT-01..05, FR-HTT-10: hồ sơ một hệ thống thông tin.</summary>
public class HeThongThongTin : BaseEntity
{
    [Required, StringLength(30), Display(Name = "Mã hệ thống")] public string Ma { get; set; } = "";
    [Required, StringLength(200), Display(Name = "Tên hệ thống")] public string Ten { get; set; } = "";
    [StringLength(2000), Display(Name = "Mô tả nghiệp vụ")] public string? MoTa { get; set; }
    [Display(Name = "Loại")] public LoaiHeThong Loai { get; set; } = LoaiHeThong.LamSang;
    [Display(Name = "Mức trọng yếu")] public MucTrongYeu MucTrongYeu { get; set; } = MucTrongYeu.TrungBinh;
    [Display(Name = "Trạng thái")] public TrangThaiHeThong TrangThai { get; set; } = TrangThaiHeThong.VanHanh;
    [StringLength(50), Display(Name = "Phiên bản hiện hành")] public string? PhienBan { get; set; }
    [Display(Name = "Ngày đưa vào sử dụng"), DataType(DataType.Date)] public DateOnly? NgaySuDung { get; set; }
    // FR-HTT-01: nhà cung cấp phần mềm + hợp đồng hỗ trợ
    [Display(Name = "Nhà cung cấp phần mềm")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [Display(Name = "Hợp đồng hỗ trợ")] public int? HopDongId { get; set; }
    public HopDong? HopDong { get; set; }
    // FR-HTT-02: người chịu trách nhiệm
    [Display(Name = "Khoa/phòng chủ sở hữu nghiệp vụ")] public int? ChuSoHuuKhoaPhongId { get; set; }
    public KhoaPhong? ChuSoHuuKhoaPhong { get; set; }
    [StringLength(100), Display(Name = "Chủ sở hữu nghiệp vụ (họ tên)")] public string? ChuSoHuu { get; set; }
    [StringLength(100), Display(Name = "Phụ trách kỹ thuật chính (tài khoản)")] public string? PhuTrachChinh { get; set; }
    [StringLength(100), Display(Name = "Phụ trách kỹ thuật dự phòng (tài khoản)")] public string? PhuTrachDuPhong { get; set; }
    [StringLength(500), Display(Name = "Đầu mối hỗ trợ của nhà cung cấp (tên, điện thoại, giờ hỗ trợ)")] public string? HoTroNhaCungCap { get; set; }
    // FR-HTT-03: RTO/RPO ghi đè (null = dùng mặc định theo mức trọng yếu, bảng 4.1.3)
    [Range(1, 100000), Display(Name = "RTO riêng (phút)")] public int? RtoPhut { get; set; }
    [Range(1, 100000), Display(Name = "RPO riêng (phút)")] public int? RpoPhut { get; set; }
    // FR-HTT-04 (S)
    [Range(1, 5), Display(Name = "Cấp độ an toàn thông tin (NĐ 85/2016)")] public int? CapDoAttt { get; set; }
    [StringLength(100), Display(Name = "Số quyết định phê duyệt cấp độ")] public string? SoQdCapDo { get; set; }
    [Display(Name = "Ngày quyết định"), DataType(DataType.Date)] public DateOnly? NgayQdCapDo { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }

    public List<HeThongKhoaPhong> KhoaPhongs { get; set; } = [];
    public List<PhuThuocHeThong> PhuThuocs { get; set; } = [];

    public bool LaTrongYeu => MucTrongYeu is MucTrongYeu.ToiQuanTrong or MucTrongYeu.Cao;

    /// <summary>BR-HTT-01: các trường bắt buộc còn thiếu với hệ thống đang vận hành.</summary>
    public List<string> HoSoThieu()
    {
        if (TrangThai != TrangThaiHeThong.VanHanh) return [];
        var t = new List<string>();
        if (string.IsNullOrWhiteSpace(ChuSoHuu) && ChuSoHuuKhoaPhongId is null) t.Add("chủ sở hữu nghiệp vụ");
        if (string.IsNullOrWhiteSpace(PhuTrachChinh)) t.Add("phụ trách kỹ thuật");
        return t;
    }
}

public class HeThongKhoaPhong
{
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    public int KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    /// <summary>FR-HTT-05: true = bắt buộc (không có hệ thống thì khoa phải chuyển quy trình giấy), false = hỗ trợ.</summary>
    public bool BatBuoc { get; set; } = true;
}

/// <summary>FR-HTT-06: hệ thống phụ thuộc một tài sản hạ tầng HOẶC một hệ thống khác.</summary>
public class PhuThuocHeThong
{
    public int Id { get; set; }
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    public int? HeThongPhuThuocId { get; set; }
    public HeThongThongTin? HeThongPhuThuoc { get; set; }
    [StringLength(100), Display(Name = "Vai trò")] public string? VaiTro { get; set; }
    /// <summary>FR-HTT-08: thành phần có dự phòng (cụm, nguồn kép, đường truyền dự phòng).</summary>
    [Display(Name = "Có dự phòng")] public bool CoDuPhong { get; set; }
}

/// <summary>Cạnh phụ thuộc rút gọn để phân tích (không phụ thuộc EF).</summary>
public record CanhPhuThuoc(int HeThongId, int? TaiSanId, int? HeThongPhuThuocId, bool CoDuPhong);

/// <summary>FR-HTT-07, BR-HTT-03: phân tích ảnh hưởng trên đồ thị phụ thuộc.</summary>
public static class PhanTichPhuThuoc
{
    /// <summary>Các hệ thống bị ảnh hưởng (trực tiếp + gián tiếp qua chuỗi phụ thuộc) khi một tài sản ngừng.</summary>
    public static HashSet<int> HeThongBiAnhHuongBoiTaiSan(int taiSanId, IReadOnlyCollection<CanhPhuThuoc> canh)
    {
        var kq = canh.Where(c => c.TaiSanId == taiSanId).Select(c => c.HeThongId).ToHashSet();
        return LanTruyen(kq, canh);
    }

    /// <summary>Các hệ thống bị ảnh hưởng khi một hệ thống ngừng (không gồm chính nó).</summary>
    public static HashSet<int> HeThongBiAnhHuongBoiHeThong(int heThongId, IReadOnlyCollection<CanhPhuThuoc> canh)
    {
        var kq = canh.Where(c => c.HeThongPhuThuocId == heThongId).Select(c => c.HeThongId).ToHashSet();
        kq = LanTruyen(kq, canh);
        kq.Remove(heThongId);
        return kq;
    }

    private static HashSet<int> LanTruyen(HashSet<int> kq, IReadOnlyCollection<CanhPhuThuoc> canh)
    {
        var hang = new Queue<int>(kq);
        while (hang.TryDequeue(out var h))
            foreach (var c in canh.Where(c => c.HeThongPhuThuocId == h))
                if (kq.Add(c.HeThongId)) hang.Enqueue(c.HeThongId);
        return kq;
    }

    /// <summary>BR-HTT-03: thêm "<paramref name="heThongId"/> phụ thuộc <paramref name="phuThuocId"/>" có tạo vòng không.</summary>
    public static bool TaoVong(int heThongId, int phuThuocId, IReadOnlyCollection<CanhPhuThuoc> canh)
    {
        if (heThongId == phuThuocId) return true;
        // Vòng xuất hiện nếu phuThuocId đã (trực tiếp/gián tiếp) phụ thuộc heThongId.
        var daXet = new HashSet<int>();
        var hang = new Queue<int>([phuThuocId]);
        while (hang.TryDequeue(out var h))
        {
            if (!daXet.Add(h)) continue;
            foreach (var c in canh.Where(c => c.HeThongId == h && c.HeThongPhuThuocId is not null))
            {
                if (c.HeThongPhuThuocId == heThongId) return true;
                hang.Enqueue(c.HeThongPhuThuocId!.Value);
            }
        }
        return false;
    }

    /// <summary>FR-HTT-07/08: điểm lỗi đơn của một hệ thống — tài sản phụ thuộc trực tiếp hoặc qua hệ thống con, không có dự phòng.</summary>
    public static HashSet<int> DiemLoiDon(int heThongId, IReadOnlyCollection<CanhPhuThuoc> canh)
    {
        var kq = new HashSet<int>();
        var daXet = new HashSet<int>();
        var hang = new Queue<int>([heThongId]);
        while (hang.TryDequeue(out var h))
        {
            if (!daXet.Add(h)) continue;
            foreach (var c in canh.Where(c => c.HeThongId == h && !c.CoDuPhong))
            {
                if (c.TaiSanId is { } t) kq.Add(t);
                if (c.HeThongPhuThuocId is { } p) hang.Enqueue(p);
            }
        }
        return kq;
    }
}

/// <summary>Bảng 4.1.3 — giá trị tham số theo mức trọng yếu (đọc từ Tham số hệ thống).</summary>
public record MucTieuTheoMuc(int RtoPhut, int RpoPhut, int ChuKyDienTapThang, int ChuKyThuPhucHoiThang);

#endregion

#region 4.2 Downtime (DT)

public enum TrangThaiKeHoach
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Hiệu lực")] HieuLuc,
    [Display(Name = "Hết hiệu lực")] HetHieuLuc
}

/// <summary>FR-DT-01, FR-DT-02: kế hoạch downtime có cấu trúc, có phiên bản và phê duyệt.</summary>
public class KeHoachDowntime : BaseEntity
{
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Phiên bản")] public int PhienBan { get; set; } = 1;
    [Display(Name = "Trạng thái")] public TrangThaiKeHoach TrangThai { get; set; } = TrangThaiKeHoach.Nhap;
    [StringLength(2000), Display(Name = "Phạm vi")] public string? PhamVi { get; set; }
    [Required, StringLength(1000), Display(Name = "Tiêu chí kích hoạt")] public string TieuChiKichHoat { get; set; } = "Hệ thống không truy cập được quá 15 phút hoặc CNTT thông báo ngừng.";
    [Required, StringLength(2000), Display(Name = "Vai trò & danh bạ liên lạc (CNTT, nhà cung cấp, đầu mối khoa)")] public string DanhBa { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Các bước khi bắt đầu downtime")] public string BuocBatDau { get; set; } = "";
    [StringLength(4000), Display(Name = "Các bước trong thời gian downtime")] public string? BuocTrongDowntime { get; set; }
    [Required, StringLength(4000), Display(Name = "Các bước khi khôi phục")] public string BuocKhoiPhuc { get; set; } = "";
    [Required, StringLength(4000), Display(Name = "Quy trình thay thế tại khoa (làm việc bằng giấy)")] public string QuyTrinhThayThe { get; set; } = "";
    [Required, StringLength(2000), Display(Name = "Quy trình nhập bù dữ liệu & người đối chiếu")] public string QuyTrinhNhapBu { get; set; } = "";
    [StringLength(1000), Display(Name = "Tiêu chí kết thúc downtime")] public string? TieuChiKetThuc { get; set; }
    [Display(Name = "Ngày hiệu lực")] public DateOnly? NgayHieuLuc { get; set; }
    [Display(Name = "Hạn rà soát lại")] public DateOnly? NgayRaSoat { get; set; }
    [StringLength(100)] public string? NguoiGuiDuyet { get; set; }
    [StringLength(100)] public string? NguoiDuyetCntt { get; set; }
    public DateTime? NgayDuyetCntt { get; set; }
    [StringLength(100)] public string? NguoiDuyetLanhDao { get; set; }
    public DateTime? NgayDuyetLanhDao { get; set; }
    [StringLength(1000)] public string? LyDoTuChoi { get; set; }

    public bool DuocSua => TrangThai == TrangThaiKeHoach.Nhap;

    public void GuiDuyet(string nguoi)
    {
        if (TrangThai != TrangThaiKeHoach.Nhap) throw new DomainException("Chỉ gửi duyệt kế hoạch ở trạng thái Nháp.");
        TrangThai = TrangThaiKeHoach.ChoDuyet; NguoiGuiDuyet = nguoi; LyDoTuChoi = null;
        NguoiDuyetCntt = null; NgayDuyetCntt = null; NguoiDuyetLanhDao = null; NgayDuyetLanhDao = null;
    }

    /// <summary>
    /// FR-DT-02: Trưởng phòng CNTT duyệt; hệ thống Tối quan trọng cần thêm Lãnh đạo.
    /// Trả về true khi đủ cấp → kế hoạch Hiệu lực (người gọi chuyển phiên bản cũ sang Hết hiệu lực — BR-DT-01).
    /// </summary>
    public bool Duyet(string nguoi, bool laDuyetCntt, bool laLanhDao, bool canLanhDao, DateTime now, int thangRaSoat)
    {
        if (TrangThai != TrangThaiKeHoach.ChoDuyet) throw new DomainException("Kế hoạch không ở trạng thái Chờ duyệt.");
        if (laDuyetCntt && NguoiDuyetCntt is null) { NguoiDuyetCntt = nguoi; NgayDuyetCntt = now; }
        else if (laLanhDao && canLanhDao && NguoiDuyetCntt is not null && NguoiDuyetLanhDao is null) { NguoiDuyetLanhDao = nguoi; NgayDuyetLanhDao = now; }
        else if (laLanhDao && canLanhDao && NguoiDuyetCntt is null) throw new DomainException("Trưởng phòng CNTT duyệt trước, sau đó Lãnh đạo duyệt.");
        else throw new DomainException("Bạn không có quyền duyệt ở bước này.");
        if (NguoiDuyetCntt is null || (canLanhDao && NguoiDuyetLanhDao is null)) return false;
        TrangThai = TrangThaiKeHoach.HieuLuc;
        NgayHieuLuc = DateOnly.FromDateTime(now);
        NgayRaSoat = NgayHieuLuc.Value.AddMonths(Math.Max(1, thangRaSoat));
        return true;
    }

    public void TuChoi(string nguoi, string lyDo)
    {
        if (TrangThai != TrangThaiKeHoach.ChoDuyet) throw new DomainException("Kế hoạch không ở trạng thái Chờ duyệt.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do từ chối.");
        TrangThai = TrangThaiKeHoach.Nhap; LyDoTuChoi = $"{nguoi}: {lyDo.Trim()}";
        NguoiDuyetCntt = null; NgayDuyetCntt = null; NguoiDuyetLanhDao = null; NgayDuyetLanhDao = null;
    }

    /// <summary>Tạo bản nháp phiên bản tiếp theo từ kế hoạch này.</summary>
    public KeHoachDowntime TaoPhienBanMoi(int phienBanMoi) => new()
    {
        HeThongId = HeThongId, PhienBan = phienBanMoi, PhamVi = PhamVi, TieuChiKichHoat = TieuChiKichHoat, DanhBa = DanhBa,
        BuocBatDau = BuocBatDau, BuocTrongDowntime = BuocTrongDowntime, BuocKhoiPhuc = BuocKhoiPhuc, QuyTrinhThayThe = QuyTrinhThayThe,
        QuyTrinhNhapBu = QuyTrinhNhapBu, TieuChiKetThuc = TieuChiKetThuc
    };
}

public enum LoaiDowntime
{
    [Display(Name = "Đột xuất")] DotXuat = 1,
    [Display(Name = "Có kế hoạch")] CoKeHoach
}

public enum MucDoDowntime
{
    [Display(Name = "Ngừng hoàn toàn")] NgungHoanToan = 1,
    [Display(Name = "Suy giảm")] SuyGiam
}

public enum TrangThaiSuKien
{
    [Display(Name = "Đã lên lịch")] DaLenLich = 1,
    [Display(Name = "Đang diễn ra")] DangDienRa,
    [Display(Name = "Đã khôi phục")] DaKhoiPhuc,
    [Display(Name = "Đã đóng")] DaDong,
    [Display(Name = "Đã huỷ")] DaHuy
}

/// <summary>FR-DT-04..08: một lần hệ thống ngừng (đột xuất hoặc có kế hoạch).</summary>
public class SuKienDowntime : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã sự kiện")] public string Ma { get; set; } = "";
    [Display(Name = "Loại")] public LoaiDowntime Loai { get; set; } = LoaiDowntime.DotXuat;
    [Display(Name = "Mức độ")] public MucDoDowntime MucDo { get; set; } = MucDoDowntime.NgungHoanToan;
    [Display(Name = "Trạng thái")] public TrangThaiSuKien TrangThai { get; set; } = TrangThaiSuKien.DangDienRa;
    [Required, StringLength(2000), Display(Name = "Mô tả / lý do")] public string MoTa { get; set; } = "";
    [Display(Name = "Bắt đầu")] public DateTime? BatDau { get; set; }
    [Display(Name = "Khôi phục")] public DateTime? KetThuc { get; set; }
    [Display(Name = "Dự kiến bắt đầu")] public DateTime? DuKienBatDau { get; set; }
    [Display(Name = "Dự kiến kết thúc")] public DateTime? DuKienKetThuc { get; set; }
    [Display(Name = "Kích hoạt kế hoạch downtime")] public bool KichHoatKeHoach { get; set; }
    [Display(Name = "Phiếu sửa chữa liên quan")] public int? PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    /// <summary>RTO nhỏ nhất của các hệ thống bị ảnh hưởng tại thời điểm ghi nhận (phút).</summary>
    public int? RtoPhut { get; set; }
    /// <summary>Mức trọng yếu cao nhất của các hệ thống bị ảnh hưởng.</summary>
    public MucTrongYeu? MucTrongYeuCaoNhat { get; set; }
    public bool DaThongBaoTruoc { get; set; }
    public bool DaNhacTruoc1Gio { get; set; }
    // FR-DT-08: rút kinh nghiệm
    [StringLength(2000), Display(Name = "Nguyên nhân gốc")] public string? NguyenNhanGoc { get; set; }
    [StringLength(2000), Display(Name = "Điều làm tốt")] public string? DiemTot { get; set; }
    [StringLength(2000), Display(Name = "Điều cần cải thiện")] public string? CanCaiThien { get; set; }
    [Display(Name = "Cần sửa kế hoạch downtime")] public bool CanSuaKeHoach { get; set; }
    public DateTime? NgayRutKinhNghiem { get; set; }
    [StringLength(100)] public string? NguoiRutKinhNghiem { get; set; }
    public DateTime? NgayDong { get; set; }
    [StringLength(100)] public string? NguoiDong { get; set; }

    public List<SuKienDowntimeHeThong> HeThongs { get; set; } = [];
    public List<SuKienDowntimeKhoa> Khoas { get; set; } = [];
    public List<NhatKyDowntime> NhatKys { get; set; } = [];

    /// <summary>BR-DT-02: thời lượng thực tế, không tạm dừng.</summary>
    public TimeSpan? ThoiLuong(DateTime now) => BatDau is { } b ? (KetThuc ?? now) - b : null;

    /// <summary>BR-DT-04: phần downtime tính như đột xuất (với downtime có kế hoạch: phần vượt khung đã đăng ký).</summary>
    public TimeSpan ThoiLuongDotXuat(DateTime now)
    {
        if (BatDau is not { } b) return TimeSpan.Zero;
        var kt = KetThuc ?? now;
        if (Loai == LoaiDowntime.DotXuat) return kt - b;
        return DuKienKetThuc is { } dk && kt > dk ? kt - (dk > b ? dk : b) : TimeSpan.Zero;
    }

    public bool VuotRto(DateTime now) => RtoPhut is { } r && ThoiLuong(now) is { } t && t.TotalMinutes > r;

    /// <summary>FR-DT-08: bắt buộc rút kinh nghiệm với downtime đột xuất của hệ thống Tối quan trọng/Cao, hoặc vượt RTO.</summary>
    public bool BatBuocRutKinhNghiem(DateTime now) =>
        (Loai == LoaiDowntime.DotXuat && MucTrongYeuCaoNhat is MucTrongYeu.ToiQuanTrong or MucTrongYeu.Cao) || VuotRto(now);

    public bool DaRutKinhNghiem => NgayRutKinhNghiem is not null;

    public void Ghi(string noiDung, string nguoi, DateTime thoiDiem)
    {
        if (TrangThai is TrangThaiSuKien.DaDong or TrangThaiSuKien.DaHuy) throw new DomainException("Sự kiện đã đóng.");
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung nhật ký.");
        NhatKys.Add(new NhatKyDowntime { ThoiDiem = thoiDiem, NoiDung = noiDung.Trim(), NguoiGhi = nguoi });
    }

    /// <summary>Downtime có kế hoạch bắt đầu thực tế.</summary>
    public void BatDauThucTe(DateTime t, string nguoi)
    {
        if (TrangThai != TrangThaiSuKien.DaLenLich) throw new DomainException("Chỉ bắt đầu sự kiện đã lên lịch.");
        BatDau = t; TrangThai = TrangThaiSuKien.DangDienRa;
        NhatKys.Add(new NhatKyDowntime { ThoiDiem = t, NoiDung = "Bắt đầu downtime có kế hoạch.", NguoiGhi = nguoi });
    }

    /// <summary>FR-DT-07: ghi thời điểm khôi phục.</summary>
    public void KhoiPhuc(DateTime t, string nguoi)
    {
        if (TrangThai != TrangThaiSuKien.DangDienRa) throw new DomainException("Chỉ khôi phục sự kiện đang diễn ra.");
        if (BatDau is { } b && t < b) throw new DomainException("Thời điểm khôi phục trước thời điểm bắt đầu.");
        if (t > DateTime.Now.AddMinutes(5)) throw new DomainException("Thời điểm khôi phục ở tương lai.");
        KetThuc = t; TrangThai = TrangThaiSuKien.DaKhoiPhuc;
        NhatKys.Add(new NhatKyDowntime { ThoiDiem = t, NoiDung = "Hệ thống đã khôi phục.", NguoiGhi = nguoi });
    }

    public void RutKinhNghiem(string? nguyenNhan, string? diemTot, string? canCaiThien, bool canSuaKeHoach, string nguoi, DateTime now)
    {
        if (TrangThai is TrangThaiSuKien.DaDong or TrangThaiSuKien.DaHuy) throw new DomainException("Sự kiện đã đóng.");
        if (string.IsNullOrWhiteSpace(nguyenNhan) || string.IsNullOrWhiteSpace(canCaiThien)) throw new DomainException("Nhập nguyên nhân gốc và điều cần cải thiện.");
        NguyenNhanGoc = nguyenNhan.Trim(); DiemTot = diemTot?.Trim(); CanCaiThien = canCaiThien.Trim(); CanSuaKeHoach = canSuaKeHoach;
        NgayRutKinhNghiem = now; NguoiRutKinhNghiem = nguoi;
    }

    /// <summary>BR-DT-03: lý do chưa được đóng (rỗng = đóng được).</summary>
    public List<string> LyDoChuaDong(DateTime now)
    {
        var l = new List<string>();
        if (KetThuc is null) l.Add("chưa ghi thời điểm khôi phục");
        var chua = Khoas.Where(k => k.KichHoatGiay && !k.DaNhapBu).Select(k => k.KhoaPhong?.Ten ?? k.KhoaPhongId.ToString()).ToList();
        if (chua.Count > 0) l.Add("khoa chưa xác nhận nhập bù: " + string.Join(", ", chua));
        if (BatBuocRutKinhNghiem(now) && !DaRutKinhNghiem) l.Add("chưa rút kinh nghiệm (bắt buộc với hệ thống Tối quan trọng/Cao hoặc vượt RTO)");
        return l;
    }

    public void Dong(string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiSuKien.DaKhoiPhuc) throw new DomainException("Chỉ đóng sự kiện đã khôi phục.");
        var l = LyDoChuaDong(now);
        if (l.Count > 0) throw new DomainException("Chưa đóng được: " + string.Join("; ", l) + ".");
        TrangThai = TrangThaiSuKien.DaDong; NgayDong = now; NguoiDong = nguoi;
    }

    public void Huy(string nguoi, DateTime now)
    {
        if (TrangThai != TrangThaiSuKien.DaLenLich) throw new DomainException("Chỉ huỷ sự kiện có kế hoạch chưa bắt đầu.");
        TrangThai = TrangThaiSuKien.DaHuy; NgayDong = now; NguoiDong = nguoi;
    }
}

public class SuKienDowntimeHeThong
{
    public int SuKienDowntimeId { get; set; }
    public SuKienDowntime? SuKienDowntime { get; set; }
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
}

/// <summary>Khoa bị ảnh hưởng; FR-DT-07: xác nhận đã nhập bù dữ liệu.</summary>
public class SuKienDowntimeKhoa
{
    public int SuKienDowntimeId { get; set; }
    public SuKienDowntime? SuKienDowntime { get; set; }
    public int KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    /// <summary>Khoa đã chuyển sang quy trình giấy → phải nhập bù sau khôi phục.</summary>
    public bool KichHoatGiay { get; set; }
    public bool DaNhapBu { get; set; }
    [StringLength(100)] public string? NguoiXacNhan { get; set; }
    public DateTime? ThoiDiemXacNhan { get; set; }
    [StringLength(500)] public string? GhiChu { get; set; }
}

public class NhatKyDowntime
{
    public long Id { get; set; }
    public int SuKienDowntimeId { get; set; }
    public DateTime ThoiDiem { get; set; }
    [StringLength(2000)] public string NoiDung { get; set; } = "";
    [StringLength(100)] public string? NguoiGhi { get; set; }
}

/// <summary>BR-DT-05: tỷ lệ khả dụng theo khoảng thời gian.</summary>
public static class KhaDung
{
    /// <summary>Gộp các khoảng (cắt theo kỳ) rồi tính % khả dụng = (kỳ − downtime) / kỳ.</summary>
    public static (double PhanTram, TimeSpan Downtime) Tinh(DateTime tu, DateTime den, IEnumerable<(DateTime BatDau, DateTime KetThuc)> khoang)
    {
        var ky = den - tu;
        if (ky <= TimeSpan.Zero) return (100, TimeSpan.Zero);
        var ds = khoang.Select(k => (B: k.BatDau < tu ? tu : k.BatDau, K: k.KetThuc > den ? den : k.KetThuc))
            .Where(k => k.K > k.B).OrderBy(k => k.B).ToList();
        var tong = TimeSpan.Zero; DateTime? b = null, e = null;
        foreach (var k in ds)
        {
            if (b is null) { b = k.B; e = k.K; continue; }
            if (k.B <= e) { if (k.K > e) e = k.K; continue; }
            tong += e!.Value - b.Value; b = k.B; e = k.K;
        }
        if (b is not null) tong += e!.Value - b.Value;
        return (Math.Round((ky - tong).TotalMinutes / ky.TotalMinutes * 100, 3), tong);
    }

    /// <summary>Khoảng downtime tính vào khả dụng của một sự kiện (<paramref name="chiDotXuat"/>: BR-DT-04/05).</summary>
    public static (DateTime, DateTime)? Khoang(SuKienDowntime s, DateTime now, bool chiDotXuat)
    {
        if (s.BatDau is not { } b || s.TrangThai == TrangThaiSuKien.DaHuy) return null;
        var kt = s.KetThuc ?? now;
        if (!chiDotXuat || s.Loai == LoaiDowntime.DotXuat) return (b, kt);
        return s.DuKienKetThuc is { } dk && kt > dk ? (dk > b ? dk : b, kt) : null;
    }
}

public enum HinhThucDienTap
{
    [Display(Name = "Thảo luận trên bàn (tabletop)")] TrenBan = 1,
    [Display(Name = "Mô phỏng tại khoa")] MoPhong,
    [Display(Name = "Ngừng thật có kế hoạch")] NgungThat
}

public enum KetLuanDienTap
{
    [Display(Name = "Đạt")] Dat = 1,
    [Display(Name = "Đạt có khuyến nghị")] DatCoKhuyenNghi,
    [Display(Name = "Không đạt")] KhongDat
}

/// <summary>FR-DT-10..12: đợt diễn tập kế hoạch downtime.</summary>
public class DotDienTap : BaseEntity
{
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    public int? KeHoachDowntimeId { get; set; }
    public KeHoachDowntime? KeHoachDowntime { get; set; }
    [Display(Name = "Hạn thực hiện")] public DateOnly HanThucHien { get; set; }
    [Display(Name = "Ngày thực hiện")] public DateTime? NgayThucHien { get; set; }
    [Display(Name = "Hình thức")] public HinhThucDienTap HinhThuc { get; set; } = HinhThucDienTap.TrenBan;
    [StringLength(2000), Display(Name = "Kịch bản")] public string? KichBan { get; set; }
    [StringLength(1000), Display(Name = "Khoa tham gia")] public string? KhoaThamGia { get; set; }
    [StringLength(500), Display(Name = "Người quan sát / đánh giá")] public string? NguoiQuanSat { get; set; }
    [StringLength(4000), Display(Name = "Diễn biến & thời gian từng bước")] public string? DienBien { get; set; }
    [StringLength(2000), Display(Name = "Phát hiện / khuyến nghị")] public string? PhatHien { get; set; }
    [Display(Name = "Kết luận")] public KetLuanDienTap? KetLuan { get; set; }
    /// <summary>FR-DT-12: downtime thực tế có rút kinh nghiệm đầy đủ được ghi nhận thay cho đợt diễn tập.</summary>
    public int? ThayBangSuKienId { get; set; }
    public SuKienDowntime? ThayBangSuKien { get; set; }
    [StringLength(1000), Display(Name = "Lý do tạo đợt")] public string? LyDo { get; set; }
    public bool DaChot { get; set; }
    [StringLength(100)] public string? NguoiChot { get; set; }
    public DateTime? NgayChot { get; set; }
    /// <summary>NFR-JC-02: bổ sung sau khi chốt (chỉ thêm, không sửa).</summary>
    [StringLength(4000)] public string? PhuLuc { get; set; }
    public int CapNhacQuaHan { get; set; }
    public bool DaNhacTruoc { get; set; }

    public bool QuaHan(DateOnly homNay) => !DaChot && HanThucHien < homNay;

    public void Ghi(DateTime ngay, HinhThucDienTap hinhThuc, string? kichBan, string? khoa, string? quanSat, string? dienBien, string? phatHien)
    {
        if (DaChot) throw new DomainException("Biên bản diễn tập đã chốt — chỉ được thêm phụ lục.");
        if (ngay > DateTime.Now.AddDays(1)) throw new DomainException("Ngày thực hiện ở tương lai.");
        NgayThucHien = ngay; HinhThuc = hinhThuc; KichBan = kichBan; KhoaThamGia = khoa; NguoiQuanSat = quanSat; DienBien = dienBien; PhatHien = phatHien;
    }

    /// <summary>FR-DT-12: chốt kết luận. Trả về true nếu cần lên lịch diễn tập lại (Không đạt).</summary>
    public bool Chot(KetLuanDienTap ketLuan, string nguoi, DateTime now)
    {
        if (DaChot) throw new DomainException("Biên bản đã chốt.");
        if (ThayBangSuKienId is null && (NgayThucHien is null || string.IsNullOrWhiteSpace(DienBien)))
            throw new DomainException("Ghi ngày thực hiện và diễn biến trước khi chốt.");
        if (ketLuan != KetLuanDienTap.Dat && string.IsNullOrWhiteSpace(PhatHien) && ThayBangSuKienId is null)
            throw new DomainException("Kết luận khác 'Đạt' cần ghi phát hiện / khuyến nghị.");
        KetLuan = ketLuan; DaChot = true; NguoiChot = nguoi; NgayChot = now;
        return ketLuan == KetLuanDienTap.KhongDat;
    }

    /// <summary>FR-DT-12: ghi nhận một downtime thực tế (đã rút kinh nghiệm) thay cho đợt diễn tập — người duyệt xác nhận.</summary>
    public void ThayBang(SuKienDowntime s, string nguoi, DateTime now)
    {
        if (DaChot) throw new DomainException("Biên bản đã chốt.");
        if (!s.DaRutKinhNghiem || s.TrangThai != TrangThaiSuKien.DaDong) throw new DomainException("Chỉ dùng sự kiện đã đóng và có rút kinh nghiệm đầy đủ.");
        if (s.HeThongs.All(h => h.HeThongId != HeThongId)) throw new DomainException("Sự kiện không ảnh hưởng hệ thống này.");
        ThayBangSuKienId = s.Id; NgayThucHien = s.BatDau; HinhThuc = HinhThucDienTap.NgungThat;
        DienBien = $"Ghi nhận thay bằng downtime thực tế {s.Ma}."; PhatHien = s.CanCaiThien;
        KetLuan = KetLuanDienTap.DatCoKhuyenNghi; DaChot = true; NguoiChot = nguoi; NgayChot = now;
    }

    public void ThemPhuLuc(string noiDung, string nguoi, DateTime now)
    {
        if (!DaChot) throw new DomainException("Biên bản chưa chốt — sửa trực tiếp.");
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung phụ lục.");
        PhuLuc = (PhuLuc is null ? "" : PhuLuc + "\n") + $"[{now:dd/MM/yyyy HH:mm} · {nguoi}] {noiDung.Trim()}";
        if (PhuLuc.Length > 4000) throw new DomainException("Phụ lục quá dài.");
    }
}

#endregion

#region 4.3 Sao lưu & thử phục hồi (SL)

public enum LoaiSaoLuu
{
    [Display(Name = "Toàn phần (full)")] Full = 1,
    [Display(Name = "Vi sai (differential)")] Differential,
    [Display(Name = "Nhật ký giao dịch (log)")] Log,
    [Display(Name = "Snapshot / bản sao máy ảo")] Snapshot,
    [Display(Name = "Tệp / thư mục")] TepTin,
    [Display(Name = "Cấu hình thiết bị")] CauHinh
}

public enum KetQuaSaoLuu
{
    [Display(Name = "Thành công")] ThanhCong = 1,
    [Display(Name = "Có cảnh báo")] CanhBao,
    [Display(Name = "Lỗi")] Loi
}

/// <summary>FR-SL-01: chính sách / tác vụ sao lưu của một hệ thống.</summary>
public class TacVuSaoLuu : BaseEntity
{
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Required, StringLength(200), Display(Name = "Tên tác vụ")] public string Ten { get; set; } = "";
    [Required, StringLength(300), Display(Name = "Đối tượng sao lưu")] public string DoiTuong { get; set; } = "";
    [Display(Name = "Loại")] public LoaiSaoLuu Loai { get; set; } = LoaiSaoLuu.Full;
    [Range(1, 8760), Display(Name = "Chu kỳ (giờ)")] public int ChuKyGio { get; set; } = 24;
    [Display(Name = "Giờ chạy dự kiến")] public TimeOnly GioChay { get; set; } = new(1, 0);
    [StringLength(100), Display(Name = "Phần mềm sao lưu")] public string? PhanMem { get; set; }
    [StringLength(500), Display(Name = "Nơi lưu")] public string? NoiLuu { get; set; }
    [Range(1, 20), Display(Name = "Số bản sao dữ liệu (kể cả bản gốc)")] public int SoBanSao { get; set; } = 2;
    [Range(1, 10), Display(Name = "Số loại phương tiện lưu trữ khác nhau")] public int SoLoaiPhuongTien { get; set; } = 1;
    [Display(Name = "Có bản ngoài cơ sở hoặc ngoại tuyến")] public bool CoBanNgoaiCoSo { get; set; }
    [Range(1, 36500), Display(Name = "Thời gian lưu giữ (ngày)")] public int LuuGiuNgay { get; set; } = 30;
    [Display(Name = "Mã hoá bản sao lưu")] public bool MaHoa { get; set; }
    [Display(Name = "Tài sản (máy chủ) để tự tạo phiếu sửa chữa khi lỗi")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Display(Name = "Đang áp dụng")] public bool DangApDung { get; set; } = true;
    public int SoLanLoiDaCanhBao { get; set; }
    public List<KetQuaSaoLuuLan> KetQuas { get; set; } = [];

    /// <summary>FR-SL-01: quy tắc 3-2-1.</summary>
    public bool Dat321 => SoBanSao >= 3 && SoLoaiPhuongTien >= 2 && CoBanNgoaiCoSo;

    /// <summary>FR-SL-01: chu kỳ sao lưu có đáp ứng RPO (phút) không.</summary>
    public bool DapUngRpo(int rpoPhut) => ChuKyGio * 60 <= rpoPhut;

    /// <summary>Các mốc dự kiến chạy trong [tu, den) — theo giờ chạy + chu kỳ.</summary>
    public IEnumerable<DateTime> LichDuKien(DateTime tu, DateTime den)
    {
        var moc = tu.Date + GioChay.ToTimeSpan();
        var buoc = TimeSpan.FromHours(Math.Max(1, ChuKyGio));
        while (moc - buoc >= tu) moc -= buoc;
        while (moc < tu) moc += buoc;
        for (; moc < den; moc += buoc) yield return moc;
    }
}

public enum NguonKetQua
{
    [Display(Name = "Nhập tay")] NhapTay = 1,
    [Display(Name = "Phiếu kiểm tra")] KiemTra,
    [Display(Name = "Tự động (API)")] Api
}

/// <summary>FR-SL-02: nhật ký kết quả một lần sao lưu.</summary>
public class KetQuaSaoLuuLan
{
    public long Id { get; set; }
    public int TacVuSaoLuuId { get; set; }
    public TacVuSaoLuu? TacVuSaoLuu { get; set; }
    [Display(Name = "Thời điểm chạy")] public DateTime ThoiDiem { get; set; }
    [Display(Name = "Kết quả")] public KetQuaSaoLuu KetQua { get; set; }
    [Display(Name = "Dung lượng (GB)")] public decimal? DungLuongGb { get; set; }
    [Display(Name = "Thời lượng (phút)")] public int? ThoiLuongPhut { get; set; }
    public NguonKetQua Nguon { get; set; } = NguonKetQua.NhapTay;
    [StringLength(1000)] public string? GhiChu { get; set; }
    [StringLength(100)] public string? NguoiGhi { get; set; }
    public DateTime ThoiGianGhi { get; set; }
}

public enum TrangThaiMocSaoLuu
{
    [Display(Name = "Thành công")] ThanhCong = 1,
    [Display(Name = "Có cảnh báo")] CanhBao,
    [Display(Name = "Lỗi")] Loi,
    [Display(Name = "Không rõ")] KhongRo,
    [Display(Name = "Chưa đến hạn")] ChuaDenHan
}

/// <summary>FR-SL-04, BR-SL-01: đối chiếu lịch dự kiến với nhật ký kết quả.</summary>
public static class DoiChieuSaoLuu
{
    /// <summary>
    /// Mỗi mốc dự kiến được ghép với kết quả gần nhất trong cửa sổ [mốc − 1 chu kỳ/2, mốc + chu kỳ).
    /// Chưa có kết quả: sau mốc + khoảng chờ → Không rõ; trước đó → Chưa đến hạn.
    /// </summary>
    public static List<(DateTime Moc, TrangThaiMocSaoLuu TrangThai, KetQuaSaoLuuLan? KetQua)> Moc(
        TacVuSaoLuu tv, IReadOnlyCollection<KetQuaSaoLuuLan> ketQuas, DateTime tu, DateTime den, DateTime now, TimeSpan khoangCho)
    {
        var buoc = TimeSpan.FromHours(Math.Max(1, tv.ChuKyGio));
        var kq = new List<(DateTime, TrangThaiMocSaoLuu, KetQuaSaoLuuLan?)>();
        var daDung = new HashSet<long>();
        foreach (var m in tv.LichDuKien(tu, den))
        {
            var cuaSoTu = m - buoc / 2; var cuaSoDen = m + buoc;
            var trongCuaSo = ketQuas.Where(k => k.ThoiDiem >= cuaSoTu && k.ThoiDiem < cuaSoDen && !daDung.Contains(k.Id)).ToList();
            // Ưu tiên lần thành công (chạy lại sau lỗi vẫn tính là đã sao lưu được).
            var chon = trongCuaSo.OrderBy(k => k.KetQua).ThenByDescending(k => k.ThoiDiem).FirstOrDefault();
            if (chon is not null)
            {
                daDung.Add(chon.Id);
                kq.Add((m, chon.KetQua switch { KetQuaSaoLuu.ThanhCong => TrangThaiMocSaoLuu.ThanhCong, KetQuaSaoLuu.CanhBao => TrangThaiMocSaoLuu.CanhBao, _ => TrangThaiMocSaoLuu.Loi }, chon));
            }
            else kq.Add((m, now >= m + khoangCho ? TrangThaiMocSaoLuu.KhongRo : TrangThaiMocSaoLuu.ChuaDenHan, null));
        }
        return kq;
    }

    /// <summary>BR-SL-01: tỷ lệ thành công (kể cả có cảnh báo) / số mốc đã đến hạn; mốc Không rõ tính là không thành công.</summary>
    public static double? TyLeThanhCong(IEnumerable<(DateTime Moc, TrangThaiMocSaoLuu TrangThai, KetQuaSaoLuuLan? KetQua)> moc)
    {
        var ds = moc.Where(m => m.TrangThai != TrangThaiMocSaoLuu.ChuaDenHan).ToList();
        if (ds.Count == 0) return null;
        return Math.Round(ds.Count(m => m.TrangThai is TrangThaiMocSaoLuu.ThanhCong or TrangThaiMocSaoLuu.CanhBao) * 100.0 / ds.Count, 1);
    }

    /// <summary>FR-SL-04: số mốc gần nhất liên tiếp lỗi hoặc không rõ.</summary>
    public static int LoiLienTiep(IEnumerable<(DateTime Moc, TrangThaiMocSaoLuu TrangThai, KetQuaSaoLuuLan? KetQua)> moc) =>
        moc.Where(m => m.TrangThai != TrangThaiMocSaoLuu.ChuaDenHan).Reverse()
            .TakeWhile(m => m.TrangThai is TrangThaiMocSaoLuu.Loi or TrangThaiMocSaoLuu.KhongRo).Count();
}

/// <summary>FR-SL-05..07: lần thử phục hồi.</summary>
public class LanThuPhucHoi : BaseEntity
{
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Hạn thực hiện")] public DateOnly HanThucHien { get; set; }
    [Display(Name = "Tác vụ sao lưu dùng để phục hồi")] public int? TacVuSaoLuuId { get; set; }
    public TacVuSaoLuu? TacVuSaoLuu { get; set; }
    [StringLength(300), Display(Name = "Bản sao lưu được chọn (ngày, loại, nơi lưu)")] public string? BanSaoLuu { get; set; }
    [Display(Name = "Thời điểm dữ liệu của bản sao lưu")] public DateTime? ThoiDiemDuLieu { get; set; }
    [Display(Name = "Thời điểm giả định sự cố")] public DateTime? ThoiDiemSuCo { get; set; }
    [StringLength(300), Display(Name = "Môi trường phục hồi")] public string? MoiTruong { get; set; }
    [Display(Name = "Bắt đầu phục hồi")] public DateTime? BatDau { get; set; }
    [Display(Name = "Hoàn tất phục hồi")] public DateTime? KetThuc { get; set; }
    [StringLength(2000), Display(Name = "Kiểm tra toàn vẹn (ứng dụng mở được, đối chiếu số bản ghi mẫu…)")] public string? KiemTraToanVen { get; set; }
    [StringLength(200), Display(Name = "Người dùng nghiệp vụ xác nhận")] public string? NguoiXacNhanNghiepVu { get; set; }
    [Display(Name = "Kiểm tra toàn vẹn đạt")] public bool ToanVenDat { get; set; }
    /// <summary>Mục tiêu tại thời điểm chốt (snapshot, phút).</summary>
    public int? RtoMucTieu { get; set; }
    public int? RpoMucTieu { get; set; }
    [Display(Name = "Kết quả")] public bool? Dat { get; set; }
    [StringLength(1000), Display(Name = "Lý do tạo")] public string? LyDo { get; set; }
    public bool DaChot { get; set; }
    [StringLength(100)] public string? NguoiChot { get; set; }
    public DateTime? NgayChot { get; set; }
    [StringLength(4000)] public string? PhuLuc { get; set; }
    public int CapNhacQuaHan { get; set; }
    public bool DaNhacTruoc { get; set; }

    public double? RtoDatPhut => BatDau is { } b && KetThuc is { } k ? Math.Round((k - b).TotalMinutes, 1) : null;
    public double? RpoDatPhut => ThoiDiemDuLieu is { } d && (ThoiDiemSuCo ?? BatDau) is { } s ? Math.Round((s - d).TotalMinutes, 1) : null;
    public bool QuaHan(DateOnly homNay) => !DaChot && HanThucHien < homNay;

    public void Ghi(int? tacVu, string? banSaoLuu, DateTime? duLieu, DateTime? suCo, string? moiTruong, DateTime? batDau, DateTime? ketThuc,
        string? kiemTra, string? nguoiXacNhan, bool toanVenDat)
    {
        if (DaChot) throw new DomainException("Biên bản thử phục hồi đã chốt — chỉ được thêm phụ lục (BR-SL-03).");
        if (batDau is { } b && ketThuc is { } k && k < b) throw new DomainException("Thời điểm hoàn tất trước thời điểm bắt đầu.");
        if (duLieu is { } d && (suCo ?? batDau) is { } s && d > s) throw new DomainException("Thời điểm dữ liệu sau thời điểm sự cố giả định.");
        TacVuSaoLuuId = tacVu; BanSaoLuu = banSaoLuu; ThoiDiemDuLieu = duLieu; ThoiDiemSuCo = suCo; MoiTruong = moiTruong;
        BatDau = batDau; KetThuc = ketThuc; KiemTraToanVen = kiemTra; NguoiXacNhanNghiepVu = nguoiXacNhan; ToanVenDat = toanVenDat;
    }

    /// <summary>FR-SL-06/07: chốt biên bản. Đạt = toàn vẹn đạt + RTO, RPO đạt được không vượt mục tiêu. Trả về kết quả.</summary>
    public bool Chot(int rtoMucTieu, int rpoMucTieu, string nguoi, DateTime now)
    {
        if (DaChot) throw new DomainException("Biên bản đã chốt.");
        if (BatDau is null || KetThuc is null || ThoiDiemDuLieu is null) throw new DomainException("Ghi thời điểm dữ liệu, bắt đầu và hoàn tất phục hồi trước khi chốt.");
        if (string.IsNullOrWhiteSpace(KiemTraToanVen)) throw new DomainException("Ghi nội dung kiểm tra toàn vẹn trước khi chốt.");
        RtoMucTieu = rtoMucTieu; RpoMucTieu = rpoMucTieu;
        Dat = ToanVenDat && RtoDatPhut <= rtoMucTieu && RpoDatPhut <= rpoMucTieu;
        DaChot = true; NguoiChot = nguoi; NgayChot = now;
        return Dat.Value;
    }

    public List<string> LyDoKhongDat()
    {
        var l = new List<string>();
        if (!ToanVenDat) l.Add("kiểm tra toàn vẹn không đạt");
        if (RtoDatPhut > RtoMucTieu) l.Add($"RTO {RtoDatPhut:0.#} phút > mục tiêu {RtoMucTieu}");
        if (RpoDatPhut > RpoMucTieu) l.Add($"RPO {RpoDatPhut:0.#} phút > mục tiêu {RpoMucTieu}");
        return l;
    }

    public void ThemPhuLuc(string noiDung, string nguoi, DateTime now)
    {
        if (!DaChot) throw new DomainException("Biên bản chưa chốt — sửa trực tiếp.");
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung phụ lục.");
        PhuLuc = (PhuLuc is null ? "" : PhuLuc + "\n") + $"[{now:dd/MM/yyyy HH:mm} · {nguoi}] {noiDung.Trim()}";
        if (PhuLuc.Length > 4000) throw new DomainException("Phụ lục quá dài.");
    }
}

#endregion

#region Mục 5 – Hành động khắc phục dùng chung (HD)

public enum NguonHanhDong
{
    [Display(Name = "Tạo trực tiếp")] TrucTiep = 1,
    [Display(Name = "Rút kinh nghiệm downtime")] SuKienDowntime,
    [Display(Name = "Diễn tập downtime")] DienTap,
    [Display(Name = "Thử phục hồi")] ThuPhucHoi,
    [Display(Name = "Kiểm tra bất thường")] KiemTra,
    [Display(Name = "Sao lưu lỗi")] SaoLuu
}

public enum LoaiHanhDong
{
    [Display(Name = "Khắc phục")] KhacPhuc = 1,
    [Display(Name = "Phòng ngừa")] PhongNgua,
    [Display(Name = "Cải tiến")] CaiTien
}

public enum TrangThaiHanhDong
{
    [Display(Name = "Mở")] Mo = 1,
    [Display(Name = "Đang làm")] DangLam,
    [Display(Name = "Chờ kiểm chứng")] ChoKiemChung,
    [Display(Name = "Đã đóng")] DaDong
}

/// <summary>FR-HD-01..05: hành động khắc phục / phòng ngừa (CAPA).</summary>
public class HanhDongKhacPhuc : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã")] public string Ma { get; set; } = "";
    [Display(Name = "Nguồn")] public NguonHanhDong Nguon { get; set; } = NguonHanhDong.TrucTiep;
    public int? NguonId { get; set; }
    [Required, StringLength(2000), Display(Name = "Nội dung hành động")] public string MoTa { get; set; } = "";
    [Display(Name = "Loại")] public LoaiHanhDong Loai { get; set; } = LoaiHanhDong.KhacPhuc;
    [Display(Name = "Hệ thống liên quan")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Required, StringLength(100), Display(Name = "Người chịu trách nhiệm (tài khoản)")] public string NguoiChiuTrachNhiem { get; set; } = "";
    [Display(Name = "Hạn hoàn thành")] public DateOnly Han { get; set; }
    [Display(Name = "Ưu tiên")] public Enums.MucDoUuTien UuTien { get; set; } = Enums.MucDoUuTien.TrungBinh;
    [Display(Name = "Trạng thái")] public TrangThaiHanhDong TrangThai { get; set; } = TrangThaiHanhDong.Mo;
    [StringLength(2000), Display(Name = "Kết quả thực hiện")] public string? KetQuaThucHien { get; set; }
    public DateTime? NgayHoanThanh { get; set; }
    [StringLength(2000), Display(Name = "Bằng chứng hiệu quả")] public string? BangChungHieuQua { get; set; }
    [StringLength(100)] public string? NguoiKiemChung { get; set; }
    public DateTime? NgayKiemChung { get; set; }
    [StringLength(2000)] public string? LichSuGiaHan { get; set; }
    public bool DaNhacTruocHan { get; set; }
    public int CapLeoThang { get; set; }

    public bool QuaHan(DateOnly homNay) => TrangThai is TrangThaiHanhDong.Mo or TrangThaiHanhDong.DangLam && Han < homNay;

    public void BatDau(string nguoi)
    {
        if (TrangThai != TrangThaiHanhDong.Mo) throw new DomainException("Hành động không ở trạng thái Mở.");
        TrangThai = TrangThaiHanhDong.DangLam;
    }

    public void BaoCaoXong(string ketQua, DateTime now)
    {
        if (TrangThai is not (TrangThaiHanhDong.Mo or TrangThaiHanhDong.DangLam)) throw new DomainException("Hành động đã báo cáo xong hoặc đã đóng.");
        if (string.IsNullOrWhiteSpace(ketQua)) throw new DomainException("Nhập kết quả thực hiện.");
        KetQuaThucHien = ketQua.Trim(); NgayHoanThanh = now; TrangThai = TrangThaiHanhDong.ChoKiemChung;
    }

    /// <summary>FR-HD-04: người khác người thực hiện xác nhận hiệu quả. <paramref name="hieuQua"/> = false → mở lại.</summary>
    public void KiemChung(string nguoi, bool hieuQua, string bangChung, DateTime now)
    {
        if (TrangThai != TrangThaiHanhDong.ChoKiemChung) throw new DomainException("Hành động chưa chờ kiểm chứng.");
        if (string.Equals(nguoi, NguoiChiuTrachNhiem, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người kiểm chứng phải khác người thực hiện.");
        if (string.IsNullOrWhiteSpace(bangChung)) throw new DomainException("Nhập bằng chứng / nhận xét kiểm chứng.");
        if (hieuQua)
        {
            BangChungHieuQua = bangChung.Trim(); NguoiKiemChung = nguoi; NgayKiemChung = now; TrangThai = TrangThaiHanhDong.DaDong;
        }
        else
        {
            TrangThai = TrangThaiHanhDong.DangLam; NgayHoanThanh = null;
            LichSuGiaHan = (LichSuGiaHan is null ? "" : LichSuGiaHan + "\n") + $"[{now:dd/MM/yyyy} · {nguoi}] Kiểm chứng chưa hiệu quả: {bangChung.Trim()}";
        }
    }

    /// <summary>FR-HD-03: gia hạn cần lý do và người duyệt (khác người thực hiện).</summary>
    public void GiaHan(DateOnly hanMoi, string lyDo, string nguoiDuyet, DateTime now)
    {
        if (TrangThai == TrangThaiHanhDong.DaDong) throw new DomainException("Hành động đã đóng.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do gia hạn.");
        if (hanMoi <= Han) throw new DomainException("Hạn mới phải sau hạn hiện tại.");
        if (string.Equals(nguoiDuyet, NguoiChiuTrachNhiem, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người duyệt gia hạn phải khác người thực hiện.");
        LichSuGiaHan = (LichSuGiaHan is null ? "" : LichSuGiaHan + "\n") + $"[{now:dd/MM/yyyy} · {nguoiDuyet}] Gia hạn {Han:dd/MM/yyyy} → {hanMoi:dd/MM/yyyy}: {lyDo.Trim()}";
        Han = hanMoi; DaNhacTruocHan = false; CapLeoThang = 0;
    }
}

#endregion
