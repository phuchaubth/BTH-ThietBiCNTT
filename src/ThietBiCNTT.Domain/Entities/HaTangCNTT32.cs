using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

// =====================================================================================
// GĐ3.2 – Hoàn thiện chương HCT và khép vòng đo lường → cải tiến → báo cáo (docs/JCI8-HaTangCNTT.md)
// TL (tài liệu), HCT.01.00 (người giám sát), HCT.01.01 (kênh dữ liệu), HCT.01.02–03 (telehealth, AI),
// AN (sự cố an ninh mạng), RR (rủi ro), đào tạo (FR-DT-09), JC (tiêu chuẩn, báo cáo lãnh đạo).
// =====================================================================================

#region Tài liệu chính sách / chương trình có phiên bản và phê duyệt (MOI.02.00–02.01)

public enum LoaiTaiLieu
{
    [Display(Name = "Chính sách & quy trình an ninh mạng")] ChinhSachAnNinhMang = 1,
    [Display(Name = "Chương trình ứng phó tấn công mạng")] ChuongTrinhUngPhoMang,
    [Display(Name = "Hướng dẫn bảo vệ dữ liệu – telehealth")] HuongDanTelehealth,
    [Display(Name = "Hướng dẫn giám sát công cụ AI hỗ trợ quyết định")] HuongDanAi,
    [Display(Name = "Hướng dẫn thiết bị di động & nhắn tin")] HuongDanThietBiDiDong,
    [Display(Name = "Chương trình quản lý tiện ích CNTT")] ChuongTrinhTienIch,
    [Display(Name = "Hồ sơ phạm vi dịch vụ Phòng CNTT")] PhamViDichVu,
    [Display(Name = "Khác")] Khac
}

/// <summary>
/// Tài liệu chính sách / chương trình / hướng dẫn: có phiên bản, hai cấp phê duyệt (Trưởng phòng CNTT → Lãnh đạo),
/// hạn rà soát. Dùng cho FR-AN-08, FR-AN-09, FR-HTT-09/13/15, FR-JC-10.
/// </summary>
public class TaiLieuQuyDinh : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã")] public string Ma { get; set; } = "";
    [Display(Name = "Loại tài liệu")] public LoaiTaiLieu Loai { get; set; } = LoaiTaiLieu.ChinhSachAnNinhMang;
    [Required, StringLength(250), Display(Name = "Tên tài liệu")] public string Ten { get; set; } = "";
    [Display(Name = "Hệ thống liên quan")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Phiên bản")] public int PhienBan { get; set; } = 1;
    [Display(Name = "Trạng thái")] public TrangThaiKeHoach TrangThai { get; set; } = TrangThaiKeHoach.Nhap;
    [Required, StringLength(8000), Display(Name = "Nội dung")] public string NoiDung { get; set; } = "";
    [Display(Name = "Ngày hiệu lực")] public DateOnly? NgayHieuLuc { get; set; }
    [Display(Name = "Hạn rà soát")] public DateOnly? NgayRaSoat { get; set; }
    [StringLength(100)] public string? NguoiGuiDuyet { get; set; }
    [StringLength(100)] public string? NguoiDuyetCntt { get; set; }
    public DateTime? NgayDuyetCntt { get; set; }
    [StringLength(100)] public string? NguoiDuyetLanhDao { get; set; }
    public DateTime? NgayDuyetLanhDao { get; set; }
    [StringLength(1000)] public string? LyDoTuChoi { get; set; }
    public bool DaNhacRaSoat { get; set; }

    public bool DuocSua => TrangThai == TrangThaiKeHoach.Nhap;
    public bool DenHanRaSoat(DateOnly homNay) => TrangThai == TrangThaiKeHoach.HieuLuc && NgayRaSoat is { } h && h <= homNay;

    public void GuiDuyet(string nguoi)
    {
        if (TrangThai != TrangThaiKeHoach.Nhap) throw new DomainException("Chỉ gửi duyệt tài liệu ở trạng thái Nháp.");
        TrangThai = TrangThaiKeHoach.ChoDuyet; NguoiGuiDuyet = nguoi; LyDoTuChoi = null;
        NguoiDuyetCntt = null; NgayDuyetCntt = null; NguoiDuyetLanhDao = null; NgayDuyetLanhDao = null;
    }

    /// <summary>Trưởng phòng CNTT duyệt trước, Lãnh đạo duyệt sau (MOI.02.01: lãnh đạo xem xét, phê duyệt). Trả về true khi đủ cấp.</summary>
    public bool Duyet(string nguoi, bool laDuyetCntt, bool laLanhDao, DateTime now, int thangRaSoat)
    {
        if (TrangThai != TrangThaiKeHoach.ChoDuyet) throw new DomainException("Tài liệu không ở trạng thái Chờ duyệt.");
        if (laDuyetCntt && NguoiDuyetCntt is null) { NguoiDuyetCntt = nguoi; NgayDuyetCntt = now; return false; }
        if (laLanhDao && NguoiDuyetCntt is null) throw new DomainException("Trưởng phòng CNTT duyệt trước, sau đó Lãnh đạo duyệt.");
        if (laLanhDao && NguoiDuyetLanhDao is null)
        {
            NguoiDuyetLanhDao = nguoi; NgayDuyetLanhDao = now;
            TrangThai = TrangThaiKeHoach.HieuLuc; NgayHieuLuc = DateOnly.FromDateTime(now);
            NgayRaSoat = NgayHieuLuc.Value.AddMonths(Math.Max(1, thangRaSoat)); DaNhacRaSoat = false;
            return true;
        }
        throw new DomainException("Bạn không có quyền duyệt ở bước này.");
    }

    public void TuChoi(string nguoi, string lyDo)
    {
        if (TrangThai != TrangThaiKeHoach.ChoDuyet) throw new DomainException("Tài liệu không ở trạng thái Chờ duyệt.");
        if (string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Nhập lý do từ chối.");
        TrangThai = TrangThaiKeHoach.Nhap; LyDoTuChoi = $"{nguoi}: {lyDo.Trim()}";
        NguoiDuyetCntt = null; NgayDuyetCntt = null; NguoiDuyetLanhDao = null; NgayDuyetLanhDao = null;
    }

    public void RaSoatXong(DateOnly homNay, int thang)
    {
        if (TrangThai != TrangThaiKeHoach.HieuLuc) throw new DomainException("Chỉ rà soát tài liệu đang hiệu lực.");
        NgayRaSoat = homNay.AddMonths(Math.Max(1, thang)); DaNhacRaSoat = false;
    }

    public TaiLieuQuyDinh TaoPhienBanMoi(int phienBanMoi) => new() { Loai = Loai, Ten = Ten, HeThongId = HeThongId, PhienBan = phienBanMoi, NoiDung = NoiDung };
}

#endregion

#region HCT.01.00 – người giám sát công nghệ thông tin y tế

/// <summary>FR-HTT-12: hồ sơ người được lãnh đạo chỉ định giám sát CNTT y tế; mỗi lần thay đổi là một bản ghi (lịch sử).</summary>
public class NguoiGiamSatCntt : BaseEntity
{
    [Required, StringLength(150), Display(Name = "Họ tên")] public string HoTen { get; set; } = "";
    [StringLength(150), Display(Name = "Chức danh")] public string? ChucDanh { get; set; }
    [StringLength(100), Display(Name = "Tài khoản hệ thống")] public string? TaiKhoan { get; set; }
    [Required, StringLength(100), Display(Name = "Số văn bản chỉ định")] public string SoVanBan { get; set; } = "";
    [Display(Name = "Ngày văn bản"), DataType(DataType.Date)] public DateOnly NgayVanBan { get; set; }
    [StringLength(1000), Display(Name = "Trình độ / chứng chỉ / kinh nghiệm")] public string? TrinhDo { get; set; }
    [Display(Name = "Hiệu lực từ"), DataType(DataType.Date)] public DateOnly NgayBatDau { get; set; }
    [Display(Name = "Hiệu lực đến"), DataType(DataType.Date)] public DateOnly? NgayKetThuc { get; set; }
    [StringLength(150), Display(Name = "Người thay thế khi vắng")] public string? NguoiThayThe { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú")] public string? GhiChu { get; set; }

    public bool HieuLuc(DateOnly d) => NgayBatDau <= d && (NgayKetThuc is null || NgayKetThuc >= d);
}

#endregion

#region HCT.01.01 – kênh truyền dữ liệu người bệnh (điện tử, di động, nhắn tin, cổng người bệnh)

public enum LoaiKenh
{
    [Display(Name = "Giao diện kết nối (HL7 / API / tệp)")] GiaoDienKetNoi = 1,
    [Display(Name = "Nền tảng nhắn tin bảo mật")] NhanTinBaoMat,
    [Display(Name = "Email")] Email,
    [Display(Name = "Thiết bị di động")] ThietBiDiDong,
    [Display(Name = "Cổng thông tin người bệnh")] CongNguoiBenh,
    [Display(Name = "Khác")] Khac
}

/// <summary>FR-HTT-09 (HCT.01.01): đăng ký kênh và đánh giá theo checklist ME của loại kênh.</summary>
public class KenhTruyenDuLieu : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên kênh")] public string Ten { get; set; } = "";
    [Display(Name = "Loại kênh")] public LoaiKenh Loai { get; set; } = LoaiKenh.NhanTinBaoMat;
    [Display(Name = "Hệ thống liên quan")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [StringLength(1000), Display(Name = "Mô tả / đối tượng kết nối")] public string? MoTa { get; set; }
    [StringLength(500), Display(Name = "Phạm vi sử dụng")] public string? PhamViDung { get; set; }
    [Display(Name = "Mã hoá dữ liệu")] public bool MaHoa { get; set; }
    [Display(Name = "Đăng nhập mã hoá, riêng từng người")] public bool DangNhapRieng { get; set; }
    [Display(Name = "Danh bạ chỉ người được phép nhận")] public bool DanhBaNguoiDuocPhep { get; set; }
    [Display(Name = "Biên nhận gửi / đọc")] public bool BienNhanGuiDoc { get; set; }
    [Display(Name = "Dấu ngày giờ tin nhắn")] public bool DauThoiGian { get; set; }
    [Display(Name = "Chống truy cập trái phép")] public bool BaoVeTruyCap { get; set; }
    [Display(Name = "Có quy trình thu hồi thiết bị khi nhân viên thôi việc")] public bool ThuHoiKhiThoiViec { get; set; }
    [Display(Name = "Xoá dữ liệu từ xa khi mất thiết bị")] public bool XoaTuXa { get; set; }
    [Display(Name = "Nhật ký thao tác")] public bool NhatKyThaoTac { get; set; }
    [Display(Name = "Có sự đồng ý của người bệnh")] public bool DongYNguoiBenh { get; set; }
    [Display(Name = "Đang sử dụng")] public bool DangSuDung { get; set; } = true;

    /// <summary>Các yêu cầu ME HCT.01.01 chưa đáp ứng theo loại kênh. <paramref name="coHuongDan"/>: có hướng dẫn bằng văn bản đang hiệu lực.</summary>
    public List<string> ThieuYeuCau(bool coHuongDan)
    {
        var t = new List<string>();
        void Can(bool dat, string ten) { if (!dat) t.Add(ten); }
        switch (Loai)
        {
            case LoaiKenh.NhanTinBaoMat:
                Can(DangNhapRieng && MaHoa, "đăng nhập mã hoá, riêng từng người"); Can(DanhBaNguoiDuocPhep, "danh bạ chỉ người được phép");
                Can(BienNhanGuiDoc, "biên nhận gửi/đọc"); Can(DauThoiGian, "dấu ngày giờ"); Can(BaoVeTruyCap, "chống truy cập trái phép"); break;
            case LoaiKenh.ThietBiDiDong:
                Can(coHuongDan, "hướng dẫn bằng văn bản"); Can(ThuHoiKhiThoiViec, "thu hồi thiết bị khi thôi việc"); Can(XoaTuXa, "xoá dữ liệu từ xa"); break;
            case LoaiKenh.CongNguoiBenh:
                Can(MaHoa, "mã hoá dữ liệu"); Can(DangNhapRieng, "đăng nhập bảo mật riêng từng người"); Can(NhatKyThaoTac, "nhật ký thao tác"); Can(DongYNguoiBenh, "sự đồng ý của người bệnh"); break;
            case LoaiKenh.Email:
                Can(BaoVeTruyCap, "giới hạn và bảo vệ truy cập"); Can(coHuongDan, "hướng dẫn: không gửi thông tin nhạy cảm qua nền tảng kém an toàn"); break;
            case LoaiKenh.GiaoDienKetNoi:
                Can(MaHoa, "mã hoá đường truyền"); break;
        }
        return t;
    }
}

#endregion

#region HCT.01.03 – vòng đời công cụ AI hỗ trợ quyết định lâm sàng

public enum LoaiCongCuAi
{
    [Display(Name = "Hỗ trợ chẩn đoán")] ChanDoan = 1,
    [Display(Name = "Hỗ trợ điều trị")] DieuTri,
    [Display(Name = "Phân tích dự báo")] DuBao,
    [Display(Name = "Quản lý sức khoẻ dân số")] SucKhoeDanSo,
    [Display(Name = "Khác")] Khac
}

public enum BuocVongDoiAi
{
    [Display(Name = "Lựa chọn")] LuaChon = 1,
    [Display(Name = "Triển khai")] TrienKhai,
    [Display(Name = "Giám sát")] GiamSat,
    [Display(Name = "Cải tiến")] CaiTien
}

/// <summary>HCT.01.03: mỗi bước có người phụ trách, ngày, nội dung, kết luận.</summary>
public class BuocVongDoi
{
    public int Id { get; set; }
    public int HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Bước")] public BuocVongDoiAi Buoc { get; set; }
    [StringLength(150), Display(Name = "Người phụ trách (chuyên môn)")] public string NguoiPhuTrach { get; set; } = "";
    [Display(Name = "Ngày"), DataType(DataType.Date)] public DateOnly Ngay { get; set; }
    [StringLength(2000), Display(Name = "Nội dung")] public string? NoiDung { get; set; }
    [StringLength(1000), Display(Name = "Kết luận")] public string? KetLuan { get; set; }
}

#endregion

#region Sự cố an ninh mạng (HCT.01.05, MOI.01.02)

public enum NguonSuCoAn
{
    [Display(Name = "Người dùng báo")] NguoiDungBao = 1,
    [Display(Name = "Cảnh báo từ hệ thống")] CanhBaoHeThong,
    [Display(Name = "Cơ quan chức năng thông báo")] CoQuanChucNang,
    [Display(Name = "Tự phát hiện")] TuPhatHien
}

public enum LoaiSuCoAn
{
    [Display(Name = "Mã độc / mã hoá tống tiền")] MaDoc = 1,
    [Display(Name = "Lừa đảo (phishing)")] Phishing,
    [Display(Name = "Truy cập trái phép")] TruyCapTraiPhep,
    [Display(Name = "Rò rỉ dữ liệu")] RoRiDuLieu,
    [Display(Name = "Từ chối dịch vụ")] TuChoiDichVu,
    [Display(Name = "Lỗ hổng nghiêm trọng")] LoHong,
    [Display(Name = "Khác")] Khac
}

public enum MucDoAn
{
    [Display(Name = "Thông thường")] ThongThuong = 1,
    [Display(Name = "Nghiêm trọng")] NghiemTrong,
    [Display(Name = "Rất nghiêm trọng")] RatNghiemTrong
}

public enum TrangThaiSuCoAn
{
    [Display(Name = "Mới tiếp nhận")] Moi = 1,
    [Display(Name = "Đang xử lý")] DangXuLy,
    [Display(Name = "Đã khắc phục")] DaKhacPhuc,
    [Display(Name = "Đã đóng")] DaDong
}

public enum BuocUngPhoAn
{
    [Display(Name = "Phát hiện")] PhatHien = 1,
    [Display(Name = "Ngăn chặn")] NganChan,
    [Display(Name = "Loại bỏ")] LoaiBo,
    [Display(Name = "Khôi phục")] KhoiPhuc,
    [Display(Name = "Rút kinh nghiệm")] RutKinhNghiem
}

public class SuCoAnNinh : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã sự cố")] public string Ma { get; set; } = "";
    [Display(Name = "Nguồn phát hiện")] public NguonSuCoAn Nguon { get; set; } = NguonSuCoAn.TuPhatHien;
    [Display(Name = "Loại")] public LoaiSuCoAn Loai { get; set; } = LoaiSuCoAn.Khac;
    [Display(Name = "Mức độ")] public MucDoAn MucDo { get; set; } = MucDoAn.ThongThuong;
    /// <summary>FR-AN-02: cờ bắt buộc chọn khi phân mức; null = chưa phân loại (sự cố người dùng mới báo).</summary>
    [Display(Name = "Liên quan dữ liệu cá nhân / thông tin người bệnh")] public bool? LienQuanDuLieuCaNhan { get; set; }
    [Required, StringLength(3000), Display(Name = "Mô tả")] public string MoTa { get; set; } = "";
    [Display(Name = "Thời điểm phát hiện")] public DateTime PhatHien { get; set; }
    [Display(Name = "Hệ thống bị ảnh hưởng")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Tài sản bị ảnh hưởng")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(100), Display(Name = "Người báo")] public string? NguoiBao { get; set; }
    [StringLength(100), Display(Name = "Người phụ trách xử lý")] public string? NguoiPhuTrach { get; set; }
    [Display(Name = "Trạng thái")] public TrangThaiSuCoAn TrangThai { get; set; } = TrangThaiSuCoAn.Moi;
    [Display(Name = "Đã báo cáo cơ quan chức năng")] public bool DaBaoCaoCoQuan { get; set; }
    [Display(Name = "Ngày báo cáo"), DataType(DataType.Date)] public DateOnly? NgayBaoCao { get; set; }
    [StringLength(200), Display(Name = "Cơ quan nhận")] public string? CoQuan { get; set; }
    [StringLength(100), Display(Name = "Số văn bản")] public string? SoVanBan { get; set; }
    [StringLength(1000), Display(Name = "Lý do không phải báo cáo")] public string? LyDoKhongBaoCao { get; set; }
    [StringLength(2000), Display(Name = "Nguyên nhân gốc")] public string? NguyenNhanGoc { get; set; }
    [StringLength(2000), Display(Name = "Bài học / cải thiện")] public string? BaiHoc { get; set; }
    public int? SuKienDowntimeId { get; set; }
    public SuKienDowntime? SuKienDowntime { get; set; }
    public DateTime? NgayDong { get; set; }
    [StringLength(100)] public string? NguoiDong { get; set; }
    public bool DaThongBaoLanhDao { get; set; }
    public bool DaNhacBaoCao { get; set; }
    public List<BuocUngPho> Buocs { get; set; } = [];

    public bool NghiemTrongTroLen => MucDo >= MucDoAn.NghiemTrong;
    public bool DaPhanLoai => LienQuanDuLieuCaNhan is not null;

    /// <summary>FR-AN-02: phân mức; bắt buộc chọn cờ dữ liệu cá nhân.</summary>
    public void PhanMuc(MucDoAn muc, LoaiSuCoAn loai, bool? lienQuanDuLieuCaNhan, string nguoi)
    {
        if (TrangThai == TrangThaiSuCoAn.DaDong) throw new DomainException("Sự cố đã đóng.");
        if (lienQuanDuLieuCaNhan is null) throw new DomainException("Chọn sự cố có liên quan dữ liệu cá nhân / thông tin người bệnh hay không.");
        MucDo = muc; Loai = loai; LienQuanDuLieuCaNhan = lienQuanDuLieuCaNhan;
        if (TrangThai == TrangThaiSuCoAn.Moi) { TrangThai = TrangThaiSuCoAn.DangXuLy; NguoiPhuTrach ??= nguoi; }
    }

    /// <summary>FR-AN-03: ghi/cập nhật một bước ứng phó.</summary>
    public BuocUngPho GhiBuoc(BuocUngPhoAn buoc, string noiDung, bool hoanThanh, string nguoi, DateTime now)
    {
        if (TrangThai == TrangThaiSuCoAn.DaDong) throw new DomainException("Sự cố đã đóng.");
        if (!DaPhanLoai) throw new DomainException("Phân mức sự cố trước khi ghi các bước ứng phó.");
        if (string.IsNullOrWhiteSpace(noiDung)) throw new DomainException("Nhập nội dung / kết quả của bước.");
        var b = Buocs.FirstOrDefault(x => x.Buoc == buoc);
        if (b is null) { b = new BuocUngPho { Buoc = buoc }; Buocs.Add(b); }
        b.NoiDung = noiDung.Trim(); b.NguoiThucHien = nguoi; b.ThoiDiem = now; b.HoanThanh = hoanThanh;
        if (buoc == BuocUngPhoAn.KhoiPhuc && hoanThanh && TrangThai == TrangThaiSuCoAn.DangXuLy) TrangThai = TrangThaiSuCoAn.DaKhacPhuc;
        return b;
    }

    public bool BuocXong(BuocUngPhoAn b) => Buocs.Any(x => x.Buoc == b && x.HoanThanh);

    /// <summary>FR-AN-04: hạn báo cáo cơ quan chức năng theo tham số (giờ, 0 = không áp dụng).</summary>
    public DateTime? HanBaoCao(int gio) => gio > 0 && NghiemTrongTroLen && LienQuanDuLieuCaNhan == true ? PhatHien.AddHours(gio) : null;

    public void GhiBaoCao(string coQuan, string soVanBan, DateOnly ngay)
    {
        if (string.IsNullOrWhiteSpace(coQuan) || string.IsNullOrWhiteSpace(soVanBan)) throw new DomainException("Nhập cơ quan nhận và số văn bản.");
        DaBaoCaoCoQuan = true; CoQuan = coQuan.Trim(); SoVanBan = soVanBan.Trim(); NgayBaoCao = ngay; LyDoKhongBaoCao = null;
    }

    /// <summary>Điều kiện đóng sự cố.</summary>
    public List<string> LyDoChuaDong()
    {
        var l = new List<string>();
        if (!DaPhanLoai) { l.Add("chưa phân mức / phân loại"); return l; }
        var can = NghiemTrongTroLen ? Enum.GetValues<BuocUngPhoAn>() : [BuocUngPhoAn.PhatHien, BuocUngPhoAn.NganChan];
        foreach (var b in can.Where(b => !BuocXong(b))) l.Add($"chưa hoàn thành bước \"{b.Ten()}\"");
        if (NghiemTrongTroLen && LienQuanDuLieuCaNhan == true && !DaBaoCaoCoQuan && string.IsNullOrWhiteSpace(LyDoKhongBaoCao))
            l.Add("chưa ghi nhận báo cáo cơ quan chức năng hoặc lý do không phải báo cáo");
        return l;
    }

    public void Dong(string nguoi, DateTime now)
    {
        if (TrangThai == TrangThaiSuCoAn.DaDong) throw new DomainException("Sự cố đã đóng.");
        var l = LyDoChuaDong();
        if (l.Count > 0) throw new DomainException("Chưa đóng được: " + string.Join("; ", l) + ".");
        TrangThai = TrangThaiSuCoAn.DaDong; NgayDong = now; NguoiDong = nguoi;
    }
}

public class BuocUngPho
{
    public int Id { get; set; }
    public int SuCoAnNinhId { get; set; }
    [Display(Name = "Bước")] public BuocUngPhoAn Buoc { get; set; }
    [StringLength(2000)] public string NoiDung { get; set; } = "";
    [StringLength(100)] public string? NguoiThucHien { get; set; }
    public DateTime ThoiDiem { get; set; }
    public bool HoanThanh { get; set; }
}

internal static class TenEnumDomain
{
    public static string Ten(this Enum e)
    {
        var m = e.GetType().GetMember(e.ToString()).FirstOrDefault();
        return (m?.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute)?.GetName() ?? e.ToString();
    }
}

#endregion

#region Thử nghiệm chung (diễn tập downtime & thử chương trình ứng phó tấn công mạng)

public enum LoaiThuNghiem
{
    [Display(Name = "Diễn tập downtime hệ thống")] Downtime = 1,
    [Display(Name = "Thử chương trình ứng phó tấn công mạng")] UngPhoTanCong
}

#endregion

#region Rủi ro CNTT (FMS.02.00, MOI.01.02 ME1, QPS.04.01)

public enum NhomRuiRo
{
    [Display(Name = "Hạ tầng vật lý")] HaTangVatLy = 1,
    [Display(Name = "Điện / điều hoà")] DienDieuHoa,
    [Display(Name = "Phần cứng")] PhanCung,
    [Display(Name = "Phần mềm")] PhanMem,
    [Display(Name = "Mạng")] Mang,
    [Display(Name = "An ninh mạng")] AnNinhMang,
    [Display(Name = "Dữ liệu")] DuLieu,
    [Display(Name = "Nhà cung cấp")] NhaCungCap,
    [Display(Name = "Nhân lực")] NhanLuc,
    [Display(Name = "Quy trình")] QuyTrinh
}

public enum PhuongAnRuiRo
{
    [Display(Name = "Giảm thiểu")] GiamThieu = 1,
    [Display(Name = "Chuyển giao")] ChuyenGiao,
    [Display(Name = "Tránh")] Tranh,
    [Display(Name = "Chấp nhận")] ChapNhan
}

public enum TrangThaiRuiRo
{
    [Display(Name = "Mở")] Mo = 1,
    [Display(Name = "Đang xử lý")] DangXuLy,
    [Display(Name = "Đã kiểm soát")] DaKiemSoat,
    [Display(Name = "Đóng")] Dong
}

public enum MucRuiRo
{
    [Display(Name = "Thấp")] Thap = 1,
    [Display(Name = "Trung bình")] TrungBinh,
    [Display(Name = "Cao")] Cao,
    [Display(Name = "Rất cao")] RatCao
}

public class RuiRo : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Mã rủi ro")] public string Ma { get; set; } = "";
    [Required, StringLength(2000), Display(Name = "Mô tả (nguyên nhân → sự kiện → hậu quả)")] public string MoTa { get; set; } = "";
    [Display(Name = "Nhóm")] public NhomRuiRo Nhom { get; set; } = NhomRuiRo.HaTangVatLy;
    [Display(Name = "Hệ thống liên quan")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Tài sản liên quan")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp kiểm soát hiện có")] public string? KiemSoatHienCo { get; set; }
    [Range(1, 5), Display(Name = "Khả năng xảy ra (1–5)")] public int KhaNang { get; set; } = 3;
    [Range(1, 5), Display(Name = "Mức độ hậu quả (1–5)")] public int HauQua { get; set; } = 3;
    [Range(1, 5), Display(Name = "Khả năng sau biện pháp")] public int? KhaNangTonDu { get; set; }
    [Range(1, 5), Display(Name = "Hậu quả sau biện pháp")] public int? HauQuaTonDu { get; set; }
    [Display(Name = "Phương án xử lý")] public PhuongAnRuiRo? PhuongAn { get; set; }
    [Display(Name = "Trạng thái")] public TrangThaiRuiRo TrangThai { get; set; } = TrangThaiRuiRo.Mo;
    [StringLength(100), Display(Name = "Người chịu trách nhiệm")] public string? NguoiChiuTrachNhiem { get; set; }
    [Display(Name = "Ngày xem xét lại"), DataType(DataType.Date)] public DateOnly? NgayXemXetLai { get; set; }
    [StringLength(1000), Display(Name = "Lý do chấp nhận rủi ro")] public string? LyDoChapNhan { get; set; }
    [StringLength(100)] public string? NguoiDuyetChapNhan { get; set; }
    public DateTime? NgayDuyetChapNhan { get; set; }
    public List<LichSuDiemRuiRo> LichSus { get; set; } = [];

    public int Diem => KhaNang * HauQua;
    public MucRuiRo Muc => MucTheoDiem(Diem);
    public int? DiemTonDu => KhaNangTonDu is { } k && HauQuaTonDu is { } h ? k * h : null;
    public MucRuiRo? MucTonDu => DiemTonDu is { } d ? MucTheoDiem(d) : null;

    /// <summary>Ma trận 5×5: 1–4 Thấp, 5–9 Trung bình, 10–16 Cao, ≥ 17 Rất cao (các tích khả dĩ: 20, 25).</summary>
    public static MucRuiRo MucTheoDiem(int diem) => diem switch { <= 4 => MucRuiRo.Thap, <= 9 => MucRuiRo.TrungBinh, <= 16 => MucRuiRo.Cao, _ => MucRuiRo.RatCao };

    public bool DangMo => TrangThai is TrangThaiRuiRo.Mo or TrangThaiRuiRo.DangXuLy;

    /// <summary>BR-RR-03: mọi thay đổi điểm lưu lịch sử.</summary>
    public void ChamDiem(int khaNang, int hauQua, int? khaNangTonDu, int? hauQuaTonDu, string lyDo, string nguoi, DateTime now, int? dotId = null)
    {
        if (khaNang is < 1 or > 5 || hauQua is < 1 or > 5) throw new DomainException("Khả năng và hậu quả phải từ 1 đến 5.");
        if ((khaNangTonDu is null) != (hauQuaTonDu is null)) throw new DomainException("Nhập đủ cả khả năng và hậu quả sau biện pháp.");
        if (khaNangTonDu is < 1 or > 5 || hauQuaTonDu is < 1 or > 5) throw new DomainException("Điểm sau biện pháp phải từ 1 đến 5.");
        if (khaNangTonDu is { } k && hauQuaTonDu is { } h && k * h > khaNang * hauQua) throw new DomainException("Rủi ro tồn dư không được cao hơn rủi ro ban đầu.");
        if (LichSus.Count > 0 && (khaNang != KhaNang || hauQua != HauQua) && string.IsNullOrWhiteSpace(lyDo)) throw new DomainException("Thay đổi điểm cần ghi lý do.");
        KhaNang = khaNang; HauQua = hauQua; KhaNangTonDu = khaNangTonDu; HauQuaTonDu = hauQuaTonDu;
        LichSus.Add(new LichSuDiemRuiRo { NgayDanhGia = now, KhaNang = khaNang, HauQua = hauQua, LyDo = string.IsNullOrWhiteSpace(lyDo) ? "Đánh giá ban đầu" : lyDo.Trim(), NguoiDanhGia = nguoi, DotDanhGiaRuiRoId = dotId });
    }

    /// <summary>FR-RR-04: chấp nhận rủi ro mức Cao trở lên cần Lãnh đạo phê duyệt kèm lý do và ngày xem xét lại.</summary>
    public bool CanLanhDaoDuyetChapNhan => PhuongAn == PhuongAnRuiRo.ChapNhan && Muc >= MucRuiRo.Cao;

    /// <summary>BR-RR-01/02: chuyển trạng thái. <paramref name="hanhDongChuaXong"/>: số hành động khắc phục liên quan chưa đóng.</summary>
    public void ChuyenTrangThai(TrangThaiRuiRo moi, int hanhDongChuaXong, bool nguoiDuyetXacNhan = false)
    {
        if (moi == TrangThaiRuiRo.DaKiemSoat)
        {
            if (PhuongAn is null) throw new DomainException("Chọn phương án xử lý trước khi chuyển Đã kiểm soát.");
            if (PhuongAn == PhuongAnRuiRo.ChapNhan)
            {
                if (string.IsNullOrWhiteSpace(LyDoChapNhan) || NgayXemXetLai is null) throw new DomainException("Chấp nhận rủi ro cần ghi lý do và ngày xem xét lại.");
                if (CanLanhDaoDuyetChapNhan && NguoiDuyetChapNhan is null) throw new DomainException("Chấp nhận rủi ro mức Cao trở lên cần Lãnh đạo phê duyệt.");
            }
            else if (hanhDongChuaXong > 0 && !nguoiDuyetXacNhan) throw new DomainException($"Còn {hanhDongChuaXong} hành động khắc phục chưa hoàn thành.");
        }
        TrangThai = moi;
    }

    public void DuyetChapNhan(string nguoi, DateTime now)
    {
        if (!CanLanhDaoDuyetChapNhan) throw new DomainException("Rủi ro này không cần Lãnh đạo duyệt chấp nhận.");
        if (string.IsNullOrWhiteSpace(LyDoChapNhan) || NgayXemXetLai is null) throw new DomainException("Ghi lý do và ngày xem xét lại trước khi duyệt.");
        NguoiDuyetChapNhan = nguoi; NgayDuyetChapNhan = now;
    }
}

public class LichSuDiemRuiRo
{
    public int Id { get; set; }
    public int RuiRoId { get; set; }
    public DateTime NgayDanhGia { get; set; }
    public int KhaNang { get; set; }
    public int HauQua { get; set; }
    [StringLength(1000)] public string? LyDo { get; set; }
    [StringLength(100)] public string? NguoiDanhGia { get; set; }
    public int? DotDanhGiaRuiRoId { get; set; }
    public int Diem => KhaNang * HauQua;
}

/// <summary>FR-RR-05, FR-RR-08: đợt đánh giá rủi ro hằng năm.</summary>
public class DotDanhGiaRuiRo : BaseEntity
{
    [Display(Name = "Năm")] public int Nam { get; set; }
    [StringLength(150), Display(Name = "Chủ trì (người giám sát CNTT y tế)")] public string? ChuTri { get; set; }
    [StringLength(1000), Display(Name = "Thành phần tham gia")] public string? ThanhPhan { get; set; }
    public bool DaChot { get; set; }
    public DateTime? NgayChot { get; set; }
    [StringLength(100)] public string? NguoiChot { get; set; }
    [StringLength(100)] public string? NguoiDuyet { get; set; }
    public DateTime? NgayDuyet { get; set; }
    [StringLength(100)] public string? NguoiGuiCoQuanQuanTri { get; set; }
    public DateTime? NgayGuiCoQuanQuanTri { get; set; }
    [StringLength(1000)] public string? HanhDongCuaCoQuanQuanTri { get; set; }
    /// <summary>Ảnh chụp báo cáo tại thời điểm chốt (NFR-JC-02: không đổi sau khi chốt).</summary>
    public string? BaoCao { get; set; }

    public void Chot(int soRuiRoMo, int soChuaChamLai, string nguoi, DateTime now)
    {
        if (DaChot) throw new DomainException("Đợt đã chốt.");
        if (soChuaChamLai > 0) throw new DomainException($"Còn {soChuaChamLai}/{soRuiRoMo} rủi ro đang mở chưa chấm lại trong đợt này.");
        DaChot = true; NgayChot = now; NguoiChot = nguoi;
    }
}

#endregion

#region Đào tạo (FR-DT-09 – MOI.01.04, SQE.01.07, HCT.01.02 ME4)

public enum LoaiDaoTao
{
    [Display(Name = "Quy trình downtime")] Downtime = 1,
    [Display(Name = "Sử dụng hệ thống thông tin")] HeThongThongTin,
    [Display(Name = "An toàn thông tin & an ninh mạng")] AnToanThongTin,
    [Display(Name = "Telehealth")] Telehealth,
    [Display(Name = "Khác")] Khac
}

public class BuoiDaoTao : BaseEntity
{
    [Display(Name = "Ngày đào tạo")] public DateTime Ngay { get; set; }
    [Display(Name = "Loại nội dung")] public LoaiDaoTao Loai { get; set; } = LoaiDaoTao.Downtime;
    [Required, StringLength(250), Display(Name = "Chủ đề")] public string ChuDe { get; set; } = "";
    [Display(Name = "Hệ thống liên quan")] public int? HeThongId { get; set; }
    public HeThongThongTin? HeThong { get; set; }
    [Display(Name = "Khoa/phòng được đào tạo")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(150), Display(Name = "Người giảng")] public string? NguoiGiang { get; set; }
    [StringLength(3000), Display(Name = "Nội dung")] public string? NoiDung { get; set; }
    public List<ThamDuDaoTao> ThamDus { get; set; } = [];
}

public class ThamDuDaoTao
{
    public int Id { get; set; }
    public int BuoiDaoTaoId { get; set; }
    public BuoiDaoTao? BuoiDaoTao { get; set; }
    [Required, StringLength(150)] public string HoTen { get; set; } = "";
    [StringLength(50)] public string? MaNhanVien { get; set; }
    public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(100)] public string? ChucDanh { get; set; }
    /// <summary>MOI.01.04 ME2: nhân viên dùng bệnh án điện tử được đào tạo và đánh giá.</summary>
    public bool? DatDanhGia { get; set; }
}

#endregion

#region Tiêu chuẩn JCI, tự đánh giá, báo cáo lãnh đạo (FR-JC-01..04, 06, 08)

public enum DanhGiaTieuChuan
{
    [Display(Name = "Đạt")] Dat = 1,
    [Display(Name = "Đạt một phần")] DatMotPhan,
    [Display(Name = "Không đạt")] KhongDat
}

public class TieuChuanJci
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Chương")] public string Chuong { get; set; } = "";
    [Required, StringLength(20), Display(Name = "Mã tiêu chuẩn")] public string Ma { get; set; } = "";
    [Required, StringLength(600), Display(Name = "Tóm lược")] public string TomTat { get; set; } = "";
    [Display(Name = "Số ME")] public int SoMe { get; set; }
    [StringLength(1000), Display(Name = "Ghi chú của Phòng QLCL")] public string? GhiChu { get; set; }
    [StringLength(500), Display(Name = "Chức năng hệ thống đáp ứng")] public string? ChucNang { get; set; }
    [Display(Name = "Có tự đánh giá bằng chứng")] public bool CoQuyTacTuDong { get; set; }
}

public class DanhGiaTieuChuanJci
{
    public int Id { get; set; }
    public int TieuChuanJciId { get; set; }
    public TieuChuanJci? TieuChuanJci { get; set; }
    public DanhGiaTieuChuan DanhGia { get; set; }
    [StringLength(1000)] public string? GhiChu { get; set; }
    [StringLength(100)] public string? NguoiDanhGia { get; set; }
    public DateTime NgayDanhGia { get; set; }
}

/// <summary>Kết quả tự đánh giá bằng chứng cho một tiêu chuẩn (tính từ dữ liệu hệ thống).</summary>
public enum MucBangChung
{
    [Display(Name = "Đủ")] Du = 1,
    [Display(Name = "Một phần")] MotPhan,
    [Display(Name = "Thiếu")] Thieu,
    [Display(Name = "Không áp dụng")] KhongApDung,
    [Display(Name = "Chưa tự động")] ChuaTuDong
}

public record KetQuaBangChung(MucBangChung Muc, string ChiTiet);

/// <summary>FR-JC-04: báo cáo định kỳ cho Lãnh đạo (GLD.04.01 ME2 – ít nhất hằng quý, bằng văn bản).</summary>
public class BaoCaoLanhDao : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Kỳ")] public string Ky { get; set; } = "";
    [Display(Name = "Từ ngày")] public DateOnly TuNgay { get; set; }
    [Display(Name = "Đến ngày")] public DateOnly DenNgay { get; set; }
    /// <summary>Ảnh chụp số liệu (JSON) tại thời điểm lập.</summary>
    public string Snapshot { get; set; } = "";
    [StringLength(4000), Display(Name = "Nhận xét của Trưởng phòng CNTT")] public string? NhanXet { get; set; }
    [StringLength(2000), Display(Name = "Số sự cố nghiêm trọng & việc đã thông báo người bệnh/gia đình")] public string? SuCoNghiemTrong { get; set; }
    public DateTime? NgayGui { get; set; }
    [StringLength(100)] public string? NguoiGui { get; set; }
    [StringLength(100)] public string? NguoiXem { get; set; }
    public DateTime? NgayXem { get; set; }
    [StringLength(4000), Display(Name = "Ý kiến chỉ đạo")] public string? YKienChiDao { get; set; }
    public int SoLanNhac { get; set; }

    public bool DaGui => NgayGui is not null;
    public bool DaXem => NgayXem is not null;

    public void Gui(string nguoi, string? nhanXet, string? suCoNghiemTrong, DateTime now)
    {
        if (DaGui) throw new DomainException("Báo cáo đã gửi — không sửa được.");
        if (string.IsNullOrWhiteSpace(nhanXet)) throw new DomainException("Trưởng phòng CNTT bổ sung nhận xét trước khi gửi.");
        NhanXet = nhanXet.Trim(); SuCoNghiemTrong = suCoNghiemTrong?.Trim(); NgayGui = now; NguoiGui = nguoi;
    }

    public void XacNhanDaXem(string nguoi, string? yKien, DateTime now)
    {
        if (!DaGui) throw new DomainException("Báo cáo chưa được gửi.");
        if (DaXem) throw new DomainException("Báo cáo đã được xác nhận.");
        NguoiXem = nguoi; NgayXem = now; YKienChiDao = yKien?.Trim();
    }

    public static string TenKy(DateOnly d) => $"{d.Year}-Q{(d.Month - 1) / 3 + 1}";
    public static (DateOnly Tu, DateOnly Den) KhoangQuy(int nam, int quy) => (new DateOnly(nam, (quy - 1) * 3 + 1, 1), new DateOnly(nam, (quy - 1) * 3 + 1, 1).AddMonths(3).AddDays(-1));
}

#endregion
