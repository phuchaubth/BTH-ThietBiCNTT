using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>
/// Phiếu điều chuyển — QĐ-02: duyệt nhiều cấp theo giá trị.
/// Cấp 1 Trưởng khoa đi → Cấp 2 Phòng CNTT → (nếu tổng nguyên giá ≥ ngưỡng) Cấp 3 Lãnh đạo.
/// Tài sản từ Kho CNTT (không có khoa đi) bỏ qua cấp 1.
/// </summary>
public class PhieuDieuChuyen : BaseEntity
{
    public const int CapTruongKhoa = 1, CapCNTT = 2, CapLanhDao = 3;

    [Required, StringLength(20), Display(Name = "Số phiếu")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Từ khoa/phòng")] public int? TuKhoaPhongId { get; set; }
    public KhoaPhong? TuKhoaPhong { get; set; }
    [Display(Name = "Đến khoa/phòng")] public int DenKhoaPhongId { get; set; }
    public KhoaPhong? DenKhoaPhong { get; set; }
    [Display(Name = "Vị trí mới")] public int? DenViTriId { get; set; }
    public ViTri? DenViTri { get; set; }
    [StringLength(100), Display(Name = "Người nhận")] public string? NguoiNhan { get; set; }
    [Required, StringLength(1000), Display(Name = "Lý do điều chuyển")] public string LyDo { get; set; } = "";
    [Display(Name = "Trạng thái")] public TrangThaiDieuChuyen TrangThai { get; set; } = TrangThaiDieuChuyen.Nhap;
    [Display(Name = "Tổng nguyên giá")] public decimal TongNguyenGia { get; set; }
    /// <summary>Cấp đang chờ duyệt (1..3); 0 khi chưa gửi / đã kết thúc.</summary>
    public int CapHienTai { get; set; }
    /// <summary>Cấp cuối cùng phải duyệt (2 hoặc 3).</summary>
    public int CapCuoi { get; set; }
    [Display(Name = "Ngày bàn giao")] public DateTime? NgayBanGiao { get; set; }
    public ICollection<ChiTietDieuChuyen> ChiTiets { get; set; } = [];
    public ICollection<BuocDuyetDieuChuyen> BuocDuyets { get; set; } = [];

    public static string TenCap(int cap) => cap switch
    {
        CapTruongKhoa => "Trưởng khoa/phòng đi",
        CapCNTT => "Phòng CNTT",
        CapLanhDao => "Lãnh đạo bệnh viện",
        _ => ""
    };

    /// <summary>Các cấp phải duyệt theo thứ tự.</summary>
    public IEnumerable<int> CacCap() =>
        Enumerable.Range(1, CapCuoi).Where(c => c != CapTruongKhoa || TuKhoaPhongId is not null);

    public void GuiDuyet(decimal nguongLanhDao)
    {
        if (TrangThai is not (TrangThaiDieuChuyen.Nhap or TrangThaiDieuChuyen.TuChoi)) throw new DomainException("Chỉ gửi duyệt phiếu nháp hoặc bị từ chối.");
        if (ChiTiets.Count == 0) throw new DomainException("Phiếu chưa có tài sản nào.");
        if (TuKhoaPhongId == DenKhoaPhongId) throw new DomainException("Khoa đi và khoa đến phải khác nhau.");
        if (ChiTiets.Any(c => c.TaiSan is null)) throw new DomainException("Thiếu thông tin tài sản.");
        TongNguyenGia = ChiTiets.Sum(c => c.TaiSan!.NguyenGia);
        CapCuoi = TongNguyenGia >= nguongLanhDao ? CapLanhDao : CapCNTT;
        CapHienTai = CacCap().First();
        TrangThai = TrangThaiDieuChuyen.ChoDuyet;
    }

    /// <summary>Duyệt cấp hiện tại. Kiểm tra quyền theo cấp do tầng ứng dụng thực hiện; ở đây chặn tự duyệt và duyệt trùng.</summary>
    public void Duyet(string nguoiDuyet, string tenHienThi, bool dongY, string? yKien, DateTime now)
    {
        if (TrangThai != TrangThaiDieuChuyen.ChoDuyet) throw new DomainException("Phiếu không ở trạng thái chờ duyệt.");
        if (string.Equals(nguoiDuyet, NguoiTao, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người lập phiếu không được tự duyệt.");
        if (BuocDuyets.Any(b => b.DongY && string.Equals(b.NguoiDuyet, nguoiDuyet, StringComparison.OrdinalIgnoreCase) && b.LuotGui == LuotGui))
            throw new DomainException("Bạn đã duyệt một cấp của phiếu này.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Phải ghi lý do khi từ chối.");
        BuocDuyets.Add(new BuocDuyetDieuChuyen { Cap = CapHienTai, NguoiDuyet = nguoiDuyet, TenNguoiDuyet = tenHienThi, DongY = dongY, YKien = yKien, ThoiGian = now, LuotGui = LuotGui });
        if (!dongY) { TrangThai = TrangThaiDieuChuyen.TuChoi; CapHienTai = 0; LuotGui++; return; }
        var tiep = CacCap().Where(c => c > CapHienTai).DefaultIfEmpty(0).First();
        if (tiep == 0) { TrangThai = TrangThaiDieuChuyen.DaDuyet; CapHienTai = 0; }
        else CapHienTai = tiep;
    }

    /// <summary>Số lần gửi duyệt (tăng khi bị từ chối) — để phân biệt các bước duyệt của mỗi lượt.</summary>
    public int LuotGui { get; set; } = 1;

    /// <summary>Bàn giao: cập nhật khoa/phòng, vị trí, người dùng của từng tài sản.</summary>
    public void BanGiao(DateTime now)
    {
        if (TrangThai != TrangThaiDieuChuyen.DaDuyet) throw new DomainException("Phiếu chưa được duyệt đủ các cấp.");
        foreach (var ct in ChiTiets)
        {
            var ts = ct.TaiSan ?? throw new DomainException("Thiếu thông tin tài sản.");
            if (ts.DaThanhLy || ts.TrangThai == TrangThaiTaiSan.ChoThanhLy) throw new DomainException($"Tài sản {ts.MaTaiSan} đang/đã thanh lý.");
            ts.KhoaPhongId = DenKhoaPhongId;
            ts.ViTriId = DenViTriId;
            ts.NguoiSuDung = NguoiNhan;
            if (ts.TrangThai == TrangThaiTaiSan.TrongKho) ts.TrangThai = TrangThaiTaiSan.DangSuDung;
        }
        NgayBanGiao = now;
        TrangThai = TrangThaiDieuChuyen.DaBanGiao;
    }

    public bool DangMo => TrangThai is TrangThaiDieuChuyen.Nhap or TrangThaiDieuChuyen.ChoDuyet or TrangThaiDieuChuyen.DaDuyet;
}

public class ChiTietDieuChuyen
{
    public int Id { get; set; }
    public int PhieuDieuChuyenId { get; set; }
    public PhieuDieuChuyen? PhieuDieuChuyen { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(500)] public string? TinhTrang { get; set; }
}

public class BuocDuyetDieuChuyen
{
    public int Id { get; set; }
    public int PhieuDieuChuyenId { get; set; }
    public PhieuDieuChuyen? PhieuDieuChuyen { get; set; }
    public int Cap { get; set; }
    public int LuotGui { get; set; }
    [StringLength(100)] public string NguoiDuyet { get; set; } = "";
    [StringLength(200)] public string? TenNguoiDuyet { get; set; }
    public bool DongY { get; set; }
    [StringLength(1000)] public string? YKien { get; set; }
    public DateTime ThoiGian { get; set; }
}

public class DeXuatThanhLy : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số đề xuất")] public string SoPhieu { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Lý do thanh lý")] public string LyDo { get; set; } = "";
    [Display(Name = "Trạng thái")] public TrangThaiThanhLy TrangThai { get; set; } = TrangThaiThanhLy.Nhap;
    [StringLength(500), Display(Name = "Thành phần hội đồng")] public string? HoiDong { get; set; }
    [StringLength(2000), Display(Name = "Kết luận hội đồng")] public string? KetLuanHoiDong { get; set; }
    [Display(Name = "Ngày đánh giá")] public DateTime? NgayDanhGia { get; set; }
    [StringLength(50), Display(Name = "Số quyết định")] public string? SoQuyetDinh { get; set; }
    [Display(Name = "Ngày quyết định"), DataType(DataType.Date)] public DateOnly? NgayQuyetDinh { get; set; }
    [Display(Name = "Hình thức thanh lý")] public HinhThucThanhLy? HinhThuc { get; set; }
    [Display(Name = "Giá trị thu hồi (VNĐ)")] public decimal GiaTriThuHoi { get; set; }
    [Display(Name = "Ngày hoàn tất")] public DateTime? NgayHoanTat { get; set; }
    public ICollection<ChiTietThanhLy> ChiTiets { get; set; } = [];

    public void GuiHoiDong()
    {
        if (TrangThai is not (TrangThaiThanhLy.Nhap or TrangThaiThanhLy.TuChoi)) throw new DomainException("Chỉ gửi đề xuất ở trạng thái nháp.");
        if (ChiTiets.Count == 0) throw new DomainException("Đề xuất chưa có tài sản.");
        foreach (var ct in ChiTiets) if (ct.TaiSan is { } ts) ts.TrangThai = TrangThaiTaiSan.ChoThanhLy;
        TrangThai = TrangThaiThanhLy.ChoHoiDong;
    }

    public void DanhGia(string hoiDong, string ketLuan, bool dongY, DateTime now)
    {
        if (TrangThai != TrangThaiThanhLy.ChoHoiDong) throw new DomainException("Đề xuất không ở bước hội đồng đánh giá.");
        HoiDong = hoiDong; KetLuanHoiDong = ketLuan; NgayDanhGia = now;
        if (dongY) TrangThai = TrangThaiThanhLy.DaDanhGia;
        else TuChoi();
    }

    public void PheDuyet(string soQuyetDinh, DateOnly ngayQuyetDinh, HinhThucThanhLy hinhThuc)
    {
        if (TrangThai != TrangThaiThanhLy.DaDanhGia) throw new DomainException("Hội đồng chưa đánh giá.");
        if (string.IsNullOrWhiteSpace(soQuyetDinh)) throw new DomainException("Phải nhập số quyết định.");
        SoQuyetDinh = soQuyetDinh; NgayQuyetDinh = ngayQuyetDinh; HinhThuc = hinhThuc;
        TrangThai = TrangThaiThanhLy.DaPheDuyet;
    }

    /// <summary>Thiết bị có lưu trữ dữ liệu nhưng chưa ghi nhận tháo ổ cứng (QĐ-03).</summary>
    public IEnumerable<ChiTietThanhLy> ChuaThaoOCung() =>
        ChiTiets.Where(c => c.TaiSan?.LoaiThietBi?.CoLuuTruDuLieu == true && !c.DaThaoOCung);

    /// <summary>Hoàn tất: QĐ-03 bắt buộc đã tháo ổ cứng với thiết bị có lưu trữ (tham số ThanhLy.BatBuocThaoOCung).</summary>
    public void HoanTat(decimal giaTriThuHoi, DateTime now, bool batBuocThaoOCung = true)
    {
        if (TrangThai != TrangThaiThanhLy.DaPheDuyet) throw new DomainException("Chưa có quyết định thanh lý.");
        if (giaTriThuHoi < 0) throw new DomainException("Giá trị thu hồi không được âm.");
        var chua = ChuaThaoOCung().Select(c => c.TaiSan!.MaTaiSan).ToList();
        if (batBuocThaoOCung && chua.Count > 0) throw new DomainException("Chưa ghi nhận tháo ổ cứng cho: " + string.Join(", ", chua));
        foreach (var ct in ChiTiets) if (ct.TaiSan is { } ts) ts.TrangThai = TrangThaiTaiSan.DaThanhLy;
        GiaTriThuHoi = giaTriThuHoi; NgayHoanTat = now;
        TrangThai = TrangThaiThanhLy.DaThanhLy;
    }

    private void TuChoi()
    {
        foreach (var ct in ChiTiets) if (ct.TaiSan is { } ts && ts.TrangThai == TrangThaiTaiSan.ChoThanhLy) ts.TrangThai = TrangThaiTaiSan.Hong;
        TrangThai = TrangThaiThanhLy.TuChoi;
    }
}

public enum XuLyOCung
{
    [Display(Name = "Lưu kho CNTT")] LuuKho = 1,
    [Display(Name = "Huỷ vật lý (có biên bản)")] HuyVatLy
}

public class ChiTietThanhLy
{
    public int Id { get; set; }
    public int DeXuatThanhLyId { get; set; }
    public DeXuatThanhLy? DeXuatThanhLy { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(500), Display(Name = "Tình trạng kỹ thuật")] public string? TinhTrang { get; set; }
    [Display(Name = "Giá trị còn lại (VNĐ)")] public decimal GiaTriConLai { get; set; }

    // QĐ-03: tháo ổ cứng giữ lại trước khi giao máy
    [Display(Name = "Đã tháo ổ cứng")] public bool DaThaoOCung { get; set; }
    [StringLength(200), Display(Name = "Serial ổ cứng")] public string? SerialOCung { get; set; }
    [StringLength(100), Display(Name = "Người tháo")] public string? NguoiThao { get; set; }
    [Display(Name = "Ngày tháo")] public DateTime? NgayThao { get; set; }
    [StringLength(200), Display(Name = "Nơi lưu ổ cứng")] public string? NoiLuuOCung { get; set; }
    [Display(Name = "Xử lý ổ cứng")] public XuLyOCung? XuLyOCung { get; set; }
    [StringLength(100), Display(Name = "Số biên bản huỷ")] public string? BienBanHuy { get; set; }
    [Display(Name = "Ngày huỷ ổ")] public DateTime? NgayHuyOCung { get; set; }

    public void GhiNhanThaoOCung(string serial, string noiLuu, string nguoi, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(serial)) throw new DomainException($"Nhập serial ổ cứng của {TaiSan?.MaTaiSan}.");
        if (DaThaoOCung) throw new DomainException($"{TaiSan?.MaTaiSan} đã ghi nhận tháo ổ.");
        DaThaoOCung = true; SerialOCung = serial.Trim(); NoiLuuOCung = string.IsNullOrWhiteSpace(noiLuu) ? "Kho CNTT" : noiLuu.Trim();
        NguoiThao = nguoi; NgayThao = now; XuLyOCung = Entities.XuLyOCung.LuuKho;
    }

    public void HuyOCung(string soBienBan, DateTime now)
    {
        if (!DaThaoOCung) throw new DomainException("Chưa tháo ổ cứng.");
        if (string.IsNullOrWhiteSpace(soBienBan)) throw new DomainException("Nhập số biên bản huỷ.");
        XuLyOCung = Entities.XuLyOCung.HuyVatLy; BienBanHuy = soBienBan.Trim(); NgayHuyOCung = now;
    }
}
