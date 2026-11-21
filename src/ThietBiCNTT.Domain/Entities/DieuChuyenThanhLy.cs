using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

public class PhieuDieuChuyen : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số phiếu")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Từ khoa/phòng")] public int? TuKhoaPhongId { get; set; }
    public KhoaPhong? TuKhoaPhong { get; set; }
    [Display(Name = "Đến khoa/phòng")] public int DenKhoaPhongId { get; set; }
    public KhoaPhong? DenKhoaPhong { get; set; }
    [StringLength(100), Display(Name = "Người nhận")] public string? NguoiNhan { get; set; }
    [Required, StringLength(1000), Display(Name = "Lý do điều chuyển")] public string LyDo { get; set; } = "";
    [Display(Name = "Trạng thái")] public TrangThaiDieuChuyen TrangThai { get; set; } = TrangThaiDieuChuyen.Nhap;
    [StringLength(100), Display(Name = "Người duyệt")] public string? NguoiDuyet { get; set; }
    [Display(Name = "Ngày duyệt")] public DateTime? NgayDuyet { get; set; }
    [StringLength(1000), Display(Name = "Ý kiến duyệt")] public string? YKienDuyet { get; set; }
    [Display(Name = "Ngày bàn giao")] public DateTime? NgayBanGiao { get; set; }
    public ICollection<ChiTietDieuChuyen> ChiTiets { get; set; } = [];

    public void GuiDuyet()
    {
        if (TrangThai is not (TrangThaiDieuChuyen.Nhap or TrangThaiDieuChuyen.TuChoi)) throw new DomainException("Chỉ gửi duyệt phiếu nháp/bị từ chối.");
        if (ChiTiets.Count == 0) throw new DomainException("Phiếu chưa có tài sản nào.");
        if (TuKhoaPhongId == DenKhoaPhongId) throw new DomainException("Khoa đi và khoa đến phải khác nhau.");
        TrangThai = TrangThaiDieuChuyen.ChoDuyet;
    }

    public void Duyet(string nguoiDuyet, bool dongY, string? yKien, DateTime now)
    {
        if (TrangThai != TrangThaiDieuChuyen.ChoDuyet) throw new DomainException("Phiếu không ở trạng thái chờ duyệt.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Phải ghi lý do khi từ chối.");
        NguoiDuyet = nguoiDuyet; NgayDuyet = now; YKienDuyet = yKien;
        TrangThai = dongY ? TrangThaiDieuChuyen.DaDuyet : TrangThaiDieuChuyen.TuChoi;
    }

    /// <summary>Bàn giao: cập nhật khoa/phòng của từng tài sản.</summary>
    public void BanGiao(DateTime now)
    {
        if (TrangThai != TrangThaiDieuChuyen.DaDuyet) throw new DomainException("Phiếu chưa được duyệt.");
        foreach (var ct in ChiTiets)
        {
            var ts = ct.TaiSan ?? throw new DomainException("Thiếu thông tin tài sản.");
            if (ts.DaThanhLy) throw new DomainException($"Tài sản {ts.MaTaiSan} đã thanh lý.");
            ts.KhoaPhongId = DenKhoaPhongId;
            ts.NguoiSuDung = NguoiNhan;
            if (ts.TrangThai == TrangThaiTaiSan.TrongKho) ts.TrangThai = TrangThaiTaiSan.DangSuDung;
        }
        NgayBanGiao = now;
        TrangThai = TrangThaiDieuChuyen.DaBanGiao;
    }
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

    /// <summary>Hoàn tất: bắt buộc đã xoá dữ liệu với thiết bị có lưu trữ (bảo mật dữ liệu bệnh nhân).</summary>
    public void HoanTat(decimal giaTriThuHoi, DateTime now)
    {
        if (TrangThai != TrangThaiThanhLy.DaPheDuyet) throw new DomainException("Chưa có quyết định thanh lý.");
        var chuaXoa = ChiTiets.Where(c => c.TaiSan?.LoaiThietBi?.CoLuuTruDuLieu == true && !c.DaXoaDuLieu)
                              .Select(c => c.TaiSan!.MaTaiSan).ToList();
        if (chuaXoa.Count > 0) throw new DomainException("Chưa xác nhận xoá dữ liệu cho: " + string.Join(", ", chuaXoa));
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

public class ChiTietThanhLy
{
    public int Id { get; set; }
    public int DeXuatThanhLyId { get; set; }
    public DeXuatThanhLy? DeXuatThanhLy { get; set; }
    public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(500), Display(Name = "Tình trạng kỹ thuật")] public string? TinhTrang { get; set; }
    [Display(Name = "Giá trị còn lại (VNĐ)")] public decimal GiaTriConLai { get; set; }
    [Display(Name = "Đã xoá dữ liệu")] public bool DaXoaDuLieu { get; set; }
    [StringLength(200), Display(Name = "Phương pháp xoá / người xác nhận")] public string? XacNhanXoaDuLieu { get; set; }
}
