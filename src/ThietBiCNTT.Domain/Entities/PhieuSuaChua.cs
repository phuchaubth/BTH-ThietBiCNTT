using System.ComponentModel.DataAnnotations;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Entities;

/// <summary>
/// Yêu cầu sửa chữa + Lệnh công việc (Work Request / Work Order).
/// Luồng: Mới → Đã tiếp nhận → Đang xử lý ⇄ (Chờ linh kiện | Gửi hãng) → Hoàn thành → Đã nghiệm thu.
/// </summary>
public class PhieuSuaChua : BaseEntity
{
    [Required, StringLength(20), Display(Name = "Số phiếu")] public string SoPhieu { get; set; } = "";
    [Display(Name = "Loại công việc")] public LoaiCongViec LoaiCongViec { get; set; } = LoaiCongViec.SuaChua;
    [Display(Name = "Tài sản")] public int TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Display(Name = "Khoa/phòng yêu cầu")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(100), Display(Name = "Người báo")] public string? NguoiYeuCau { get; set; }
    [StringLength(20), Display(Name = "SĐT liên hệ")] public string? DienThoaiLienHe { get; set; }
    [Required, StringLength(2000), Display(Name = "Mô tả sự cố")] public string MoTaSuCo { get; set; } = "";
    [Display(Name = "Mức độ ưu tiên")] public MucDoUuTien UuTien { get; set; } = MucDoUuTien.TrungBinh;
    [Display(Name = "Trạng thái")] public TrangThaiPhieuSuaChua TrangThai { get; set; } = TrangThaiPhieuSuaChua.Moi;

    [Display(Name = "Thời điểm báo")] public DateTime NgayYeuCau { get; set; }
    [Display(Name = "Hạn xử lý (SLA)")] public DateTime HanXuLy { get; set; }
    [Display(Name = "Thời điểm tiếp nhận")] public DateTime? NgayTiepNhan { get; set; }
    [Display(Name = "Bắt đầu xử lý")] public DateTime? NgayBatDau { get; set; }
    [Display(Name = "Thời điểm hoàn thành")] public DateTime? NgayHoanThanh { get; set; }
    [Display(Name = "Thời điểm nghiệm thu")] public DateTime? NgayNghiemThu { get; set; }
    /// <summary>QĐ-04: đang tạm dừng SLA (chờ linh kiện / gửi hãng) từ thời điểm này.</summary>
    public DateTime? BatDauTamDung { get; set; }
    /// <summary>Tổng thời gian đã tạm dừng (giây) — trừ khỏi thời gian xử lý khi tính MTTR.</summary>
    public long TongTamDungGiay { get; set; }
    // Theo dõi gửi hãng / bảo hành
    [StringLength(50), Display(Name = "Số phiếu hãng / RMA")] public string? SoPhieuHang { get; set; }
    [Display(Name = "Ngày gửi hãng")] public DateTime? NgayGuiHang { get; set; }
    [Display(Name = "Ngày hãng hẹn trả"), DataType(DataType.Date)] public DateOnly? NgayHenTra { get; set; }
    [Display(Name = "Ngày nhận về")] public DateTime? NgayNhanVe { get; set; }
    public bool TreHenTra(DateOnly homNay) => TrangThai == TrangThaiPhieuSuaChua.GuiHang && NgayHenTra is { } h && h < homNay;

    /// <summary>Đã gửi nhắc "sắp đến hạn" cho KTV.</summary>
    public bool DaNhacTruocHan { get; set; }
    /// <summary>Cấp leo thang đã thông báo khi quá hạn (0 = chưa; 1 = KTV & CNTT; 2 = trưởng phòng; 3 = lãnh đạo).</summary>
    public int CapLeoThang { get; set; }

    [StringLength(100), Display(Name = "Kỹ thuật viên")] public string? KyThuatVien { get; set; }
    [Display(Name = "Hình thức xử lý")] public HinhThucXuLy? HinhThucXuLy { get; set; }
    [Display(Name = "Đơn vị bảo hành / thuê ngoài")] public int? NhaCungCapId { get; set; }
    public NhaCungCap? NhaCungCap { get; set; }
    [StringLength(1000), Display(Name = "Nguyên nhân")] public string? NguyenNhan { get; set; }
    [StringLength(2000), Display(Name = "Biện pháp khắc phục")] public string? BienPhapKhacPhuc { get; set; }
    [Display(Name = "Nhóm nguyên nhân")] public NhomNguyenNhan? NhomNguyenNhan { get; set; }
    [Display(Name = "Hành động khắc phục")] public HanhDongKhacPhuc? HanhDongKhacPhuc { get; set; }
    [Display(Name = "Giờ công")] public decimal GioCong { get; set; }
    /// <summary>Chi phí nhân công nội bộ = giờ công × đơn giá giờ (tham số) tại thời điểm hoàn thành (FR-WO-09).</summary>
    [Display(Name = "Chi phí nhân công nội bộ (VNĐ)")] public decimal ChiPhiNhanCong { get; set; }
    // Thiết bị thay thế tạm (FR-WO-11)
    [Display(Name = "Thiết bị cho mượn tạm")] public int? ThietBiTamId { get; set; }
    public TaiSan? ThietBiTam { get; set; }
    public int? KhoaGocThietBiTamId { get; set; }
    public DateTime? NgayChoMuonTam { get; set; }
    public DateTime? NgayThuHoiTam { get; set; }
    public bool DangChoMuonTam => ThietBiTamId is not null && NgayThuHoiTam is null;
    [Display(Name = "Chi phí thuê ngoài (VNĐ)")] public decimal ChiPhiThueNgoai { get; set; }
    [StringLength(100), Display(Name = "Người nghiệm thu")] public string? NguoiNghiemThu { get; set; }
    [Range(1, 5), Display(Name = "Đánh giá (1-5)")] public int? DanhGia { get; set; }
    [StringLength(1000), Display(Name = "Ý kiến người dùng")] public string? YKienNguoiDung { get; set; }

    [Display(Name = "Kế hoạch bảo trì")] public int? KeHoachBaoTriId { get; set; }
    /// <summary>Chỉ số bộ đếm khi hoàn thành lệnh bảo trì (mốc tính chu kỳ theo chỉ số lần sau).</summary>
    [Display(Name = "Chỉ số bộ đếm khi hoàn thành")] public long? ChiSoBoDem { get; set; }
    public KeHoachBaoTri? KeHoachBaoTri { get; set; }
    public ICollection<ChiTietVatTuSuaChua> VatTus { get; set; } = [];
    public ICollection<BinhLuanPhieu> BinhLuans { get; set; } = [];
    public ICollection<GioCongPhieu> GioCongs { get; set; } = [];

    public decimal ChiPhiVatTu => VatTus.Sum(v => v.ThanhTien);
    public decimal TongChiPhi => ChiPhiVatTu + ChiPhiThueNgoai + ChiPhiNhanCong;
    public bool DaDong => TrangThai is TrangThaiPhieuSuaChua.DaNghiemThu or TrangThaiPhieuSuaChua.DaHuy;
    public bool DangTamDung => BatDauTamDung is not null;
    public bool QuaHan(DateTime now) => !DaDong && !DangTamDung && TrangThai != TrangThaiPhieuSuaChua.HoanThanh && now > HanXuLy;
    public bool HoanThanhDungHan => NgayHoanThanh is not null && NgayHoanThanh <= HanXuLy;

    /// <summary>Thời gian ngừng máy (giờ) = từ lúc báo đến lúc hoàn thành.</summary>
    public double? ThoiGianDungMayGio => NgayHoanThanh is null ? null : (NgayHoanThanh.Value - NgayYeuCau).TotalHours;
    /// <summary>Thời gian xử lý hiệu dụng (giờ) = dừng máy trừ thời gian tạm dừng chờ linh kiện/hãng.</summary>
    public double? ThoiGianXuLyGio => ThoiGianDungMayGio is { } g ? Math.Max(0, g - TongTamDungGiay / 3600.0) : null;

    /// <summary>Hàm dời hạn khi hết tạm dừng: (hạn cũ, lúc bắt đầu tạm dừng, lúc tiếp tục) → hạn mới.</summary>
    public delegate DateTime DoiHanSla(DateTime hanCu, DateTime batDauTamDung, DateTime tiepTuc);

    private void KetThucTamDung(DateTime now, DoiHanSla? doiHan)
    {
        if (BatDauTamDung is not { } s) return;
        TongTamDungGiay += (long)Math.Max(0, (now - s).TotalSeconds);
        if (doiHan is not null) HanXuLy = doiHan(HanXuLy, s, now);
        BatDauTamDung = null;
    }

    private void YeuCau(params TrangThaiPhieuSuaChua[] hopLe)
    {
        if (!hopLe.Contains(TrangThai))
            throw new DomainException($"Không thể thực hiện thao tác khi phiếu đang ở trạng thái '{TrangThai}'.");
    }

    public void TiepNhan(string kyThuatVien, HinhThucXuLy hinhThuc, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.Moi, TrangThaiPhieuSuaChua.DaTiepNhan);
        if (string.IsNullOrWhiteSpace(kyThuatVien)) throw new DomainException("Phải chọn kỹ thuật viên.");
        KyThuatVien = kyThuatVien;
        HinhThucXuLy = hinhThuc;
        NgayTiepNhan ??= now;
        TrangThai = TrangThaiPhieuSuaChua.DaTiepNhan;
    }

    public void BatDauXuLy(DateTime now, DoiHanSla? doiHan = null)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang);
        if (TrangThai == TrangThaiPhieuSuaChua.GuiHang) NgayNhanVe = now;
        KetThucTamDung(now, doiHan);
        NgayBatDau ??= now;
        TrangThai = TrangThaiPhieuSuaChua.DangXuLy;
    }

    /// <param name="dungSla">QĐ-04: true = dừng đồng hồ SLA trong thời gian chờ (tham số Sla.TamDungKhiCho).</param>
    public void TamDung(TrangThaiPhieuSuaChua lyDo, DateTime now, bool dungSla = true)
    {
        if (lyDo is not (TrangThaiPhieuSuaChua.ChoLinhKien or TrangThaiPhieuSuaChua.GuiHang))
            throw new DomainException("Lý do tạm dừng không hợp lệ.");
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang);
        if (TrangThai == lyDo) return;
        NgayBatDau ??= now;
        if (dungSla) BatDauTamDung ??= now;
        if (lyDo == TrangThaiPhieuSuaChua.GuiHang) { NgayGuiHang = now; NgayNhanVe = null; }
        TrangThai = lyDo;
    }

    public void HoanThanh(string? nguyenNhan, string? bienPhap, decimal gioCong, decimal chiPhiThueNgoai, DateTime now, DoiHanSla? doiHan = null,
        NhomNguyenNhan? nhomNguyenNhan = null, HanhDongKhacPhuc? hanhDong = null, decimal donGiaGioCong = 0)
    {
        YeuCau(TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.GuiHang, TrangThaiPhieuSuaChua.ChoLinhKien);
        if (string.IsNullOrWhiteSpace(bienPhap)) throw new DomainException("Phải nhập biện pháp khắc phục.");
        if (gioCong < 0 || chiPhiThueNgoai < 0) throw new DomainException("Giờ công / chi phí không được âm.");
        if (TrangThai == TrangThaiPhieuSuaChua.GuiHang) NgayNhanVe = now;
        KetThucTamDung(now, doiHan);
        NguyenNhan = nguyenNhan;
        BienPhapKhacPhuc = bienPhap;
        NhomNguyenNhan = nhomNguyenNhan ?? NhomNguyenNhan;
        HanhDongKhacPhuc = hanhDong ?? HanhDongKhacPhuc;
        // Có nhật ký giờ công theo KTV thì giờ công = tổng nhật ký (FR-WO-06)
        GioCong = GioCongs.Count > 0 ? GioCongs.Sum(g => g.SoGio) : gioCong;
        ChiPhiNhanCong = Math.Round(GioCong * Math.Max(0, donGiaGioCong));
        ChiPhiThueNgoai = chiPhiThueNgoai;
        NgayBatDau ??= now;
        NgayHoanThanh = now;
        TrangThai = TrangThaiPhieuSuaChua.HoanThanh;
    }

    /// <summary>
    /// QĐ-01: chỉ người báo hỏng hoặc Trưởng khoa của khoa sở hữu được nghiệm thu; KTV xử lý phiếu không tự nghiệm thu.
    /// </summary>
    public bool DuocNghiemThu(string? userName, bool laTruongKhoa, int? khoaCuaUser) =>
        !string.IsNullOrEmpty(userName) && !string.Equals(userName, KyThuatVien, StringComparison.OrdinalIgnoreCase)
        && (string.Equals(userName, NguoiTao, StringComparison.OrdinalIgnoreCase) || (laTruongKhoa && khoaCuaUser is not null && khoaCuaUser == KhoaPhongId));

    public void NghiemThu(string nguoiNghiemThu, int? danhGia, string? yKien, DateTime now)
    {
        YeuCau(TrangThaiPhieuSuaChua.HoanThanh);
        if (danhGia is < 1 or > 5) throw new DomainException("Đánh giá từ 1 đến 5.");
        NguoiNghiemThu = nguoiNghiemThu;
        DanhGia = danhGia;
        YKienNguoiDung = yKien;
        NgayNghiemThu = now;
        TrangThai = TrangThaiPhieuSuaChua.DaNghiemThu;
    }

    /// <summary>QĐ-01: phiếu hoàn thành quá N ngày không phản hồi → hệ thống tự nghiệm thu.</summary>
    public bool TuDongNghiemThu(int soNgay, DateTime now)
    {
        if (soNgay <= 0 || TrangThai != TrangThaiPhieuSuaChua.HoanThanh || NgayHoanThanh is null || NgayHoanThanh.Value.AddDays(soNgay) > now) return false;
        NguoiNghiemThu = "Hệ thống (tự động sau " + soNgay + " ngày)";
        NgayNghiemThu = now;
        TrangThai = TrangThaiPhieuSuaChua.DaNghiemThu;
        return true;
    }

    /// <summary>Người dùng không đồng ý nghiệm thu → trả lại cho KTV.</summary>
    public void TraLai(string yKien)
    {
        YeuCau(TrangThaiPhieuSuaChua.HoanThanh);
        YKienNguoiDung = yKien;
        NgayHoanThanh = null;
        TrangThai = TrangThaiPhieuSuaChua.DangXuLy;
    }

    /// <summary>Ghi giờ công một KTV (FR-WO-06): bắt đầu – kết thúc, không chồng lấn với lần ghi khác của cùng người.</summary>
    public GioCongPhieu GhiGioCong(string nguoi, DateTime batDau, DateTime ketThuc, string? ghiChu, DateTime now)
    {
        if (DaDong || TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Phiếu đã hoàn thành/đóng, không ghi thêm giờ công.");
        if (TrangThai == TrangThaiPhieuSuaChua.Moi) throw new DomainException("Phiếu chưa được tiếp nhận.");
        if (string.IsNullOrWhiteSpace(nguoi)) throw new DomainException("Chọn người thực hiện.");
        if (ketThuc <= batDau) throw new DomainException("Thời điểm kết thúc phải sau bắt đầu.");
        if ((ketThuc - batDau).TotalHours > 16) throw new DomainException("Một lần ghi tối đa 16 giờ.");
        if (ketThuc > now.AddMinutes(5)) throw new DomainException("Không ghi giờ công trong tương lai.");
        if (batDau < NgayYeuCau.AddHours(-1)) throw new DomainException("Giờ công không được trước thời điểm báo hỏng.");
        if (GioCongs.Any(g => string.Equals(g.NguoiThucHien, nguoi, StringComparison.OrdinalIgnoreCase) && g.BatDau < ketThuc && batDau < g.KetThuc))
            throw new DomainException($"{nguoi} đã có giờ công trùng khoảng thời gian này.");
        var g = new GioCongPhieu { PhieuSuaChuaId = Id, NguoiThucHien = nguoi.Trim(), BatDau = batDau, KetThuc = ketThuc, GhiChu = ghiChu?.Trim() };
        GioCongs.Add(g);
        GioCong = GioCongs.Sum(x => x.SoGio);
        return g;
    }

    /// <summary>Cho khoa mượn thiết bị dự phòng trong thời gian sửa (FR-WO-11): thiết bị phải đang trong kho.</summary>
    public void ChoMuonTam(TaiSan tam, DateTime now)
    {
        if (DaDong || TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Phiếu đã hoàn thành/đóng.");
        if (DangChoMuonTam) throw new DomainException("Phiếu đang có thiết bị cho mượn, thu hồi trước khi cho mượn thiết bị khác.");
        if (tam.Id == TaiSanId) throw new DomainException("Không thể cho mượn chính thiết bị đang sửa.");
        if (tam.TrangThai != TrangThaiTaiSan.TrongKho) throw new DomainException($"{tam.MaTaiSan} không ở trạng thái Trong kho.");
        ThietBiTamId = tam.Id; ThietBiTam = tam; KhoaGocThietBiTamId = tam.KhoaPhongId; NgayChoMuonTam = now; NgayThuHoiTam = null;
        tam.TrangThai = TrangThaiTaiSan.DangSuDung; tam.KhoaPhongId = KhoaPhongId;
    }

    public void ThuHoiTam(TaiSan tam, DateTime now)
    {
        if (!DangChoMuonTam || tam.Id != ThietBiTamId) throw new DomainException("Phiếu không có thiết bị đang cho mượn.");
        tam.TrangThai = TrangThaiTaiSan.TrongKho; tam.KhoaPhongId = KhoaGocThietBiTamId;
        NgayThuHoiTam = now;
    }

    public void Huy(string lyDo)
    {
        if (DaDong || TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Phiếu đã hoàn thành/đóng, không thể huỷ.");
        if (DangChoMuonTam) throw new DomainException("Thu hồi thiết bị cho mượn tạm trước khi huỷ phiếu.");
        BatDauTamDung = null;
        YKienNguoiDung = lyDo;
        TrangThai = TrangThaiPhieuSuaChua.DaHuy;
    }
}

/// <summary>Giờ công một lần làm việc của một KTV trên phiếu (FR-WO-06).</summary>
public class GioCongPhieu
{
    public int Id { get; set; }
    public int PhieuSuaChuaId { get; set; }
    [StringLength(100)] public string NguoiThucHien { get; set; } = "";
    public DateTime BatDau { get; set; }
    public DateTime KetThuc { get; set; }
    [StringLength(300)] public string? GhiChu { get; set; }
    public decimal SoGio => Math.Round((decimal)(KetThuc - BatDau).TotalHours, 2);
}

public class ChiTietVatTuSuaChua
{
    public int Id { get; set; }
    public int PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    /// <summary>Kho xuất & phiếu xuất kho tương ứng (truy vết FR-KHO-09).</summary>
    public int? KhoId { get; set; }
    public int? PhieuKhoId { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien => SoLuong * DonGia;
}

/// <summary>Kế hoạch bảo trì phòng ngừa (PM) theo chu kỳ ngày, áp cho một tài sản hoặc cả loại thiết bị.</summary>
public enum TrangThaiKeHoach
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ thẩm định")] ChoThamDinh,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Hiệu lực")] HieuLuc,
    [Display(Name = "Tạm dừng")] TamDung,
    [Display(Name = "Hết hiệu lực")] HetHieuLuc
}

public enum KieuChuKy
{
    [Display(Name = "Theo thời gian")] ThoiGian = 1,
    [Display(Name = "Theo chỉ số (bộ đếm)")] ChiSo,
    [Display(Name = "Thời gian hoặc chỉ số – điều kiện nào đến trước")] KetHop
}

/// <summary>
/// Kế hoạch bảo trì định kỳ (FR-PM-01..15). Theo thời gian: sinh lệnh theo lô cho mọi tài sản phạm vi ở <see cref="NgayTiepTheo"/>.
/// Theo chỉ số: sinh lệnh riêng cho tài sản có bộ đếm tăng ≥ <see cref="ChuKyChiSo"/> kể từ lần bảo trì trước.
/// Chỉ kế hoạch ở trạng thái Hiệu lực mới sinh lệnh.
/// </summary>
public class KeHoachBaoTri : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên kế hoạch")] public string Ten { get; set; } = "";
    [Display(Name = "Áp dụng cho loại thiết bị")] public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [Display(Name = "Áp dụng cho tài sản")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [Display(Name = "Chỉ tài sản của khoa")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [Display(Name = "Kiểu chu kỳ")] public KieuChuKy KieuChuKy { get; set; } = KieuChuKy.ThoiGian;
    [Range(1, 3650), Display(Name = "Chu kỳ (ngày)")] public int ChuKyNgay { get; set; } = 180;
    [Range(1, long.MaxValue), Display(Name = "Chu kỳ theo chỉ số")] public long? ChuKyChiSo { get; set; }
    [StringLength(30), Display(Name = "Đơn vị chỉ số")] public string? DonViChiSo { get; set; }
    [Display(Name = "Ngày thực hiện tiếp theo"), DataType(DataType.Date)] public DateOnly NgayTiepTheo { get; set; }
    [Display(Name = "Hiệu lực đến (tuỳ chọn)"), DataType(DataType.Date)] public DateOnly? NgayKetThuc { get; set; }
    [StringLength(4000), Display(Name = "Hướng dẫn / checklist văn bản (mỗi dòng 1 mục)")] public string? Checklist { get; set; }
    [Display(Name = "Phiếu checklist (mẫu kiểm tra)")] public int? MauKiemTraId { get; set; }
    public MauKiemTra? MauKiemTra { get; set; }
    [StringLength(100), Display(Name = "KTV phụ trách")] public string? KyThuatVien { get; set; }
    [Range(0, 1000), Display(Name = "Giờ công dự kiến / lần / thiết bị")] public decimal GioCongDuKien { get; set; } = 1;
    [Range(0, double.MaxValue), Display(Name = "Chi phí thuê ngoài dự kiến / lần / thiết bị")] public decimal ChiPhiDuKien { get; set; }
    [Display(Name = "Đang áp dụng")] public bool DangApDung { get; set; } = true;
    public TrangThaiKeHoach TrangThai { get; set; } = TrangThaiKeHoach.HieuLuc;
    [StringLength(100), Display(Name = "Người thẩm định")] public string? NguoiThamDinh { get; set; }
    [StringLength(100), Display(Name = "Người xét duyệt")] public string? NguoiDuyet { get; set; }
    public DateTime? NgayThamDinh { get; set; }
    public DateTime? NgayDuyet { get; set; }
    [StringLength(1000)] public string? YKien { get; set; }
    public List<KeHoachVatTu> VatTus { get; set; } = [];

    public bool TheoThoiGian => KieuChuKy != KieuChuKy.ChiSo;
    public bool TheoChiSo => KieuChuKy != KieuChuKy.ThoiGian && ChuKyChiSo > 0;
    public bool DenHan(DateOnly homNay) => DangApDung && NgayTiepTheo <= homNay;
    public void DayLichTiep() => NgayTiepTheo = NgayTiepTheo.AddDays(ChuKyNgay);

    /// <summary>Bộ đếm đã tăng đủ chu kỳ kể từ lần bảo trì trước (chưa có lần trước → chưa xét chỉ số).</summary>
    public bool DenHanChiSo(long? hienTai, long? lanTruoc) => TheoChiSo && hienTai is { } h && lanTruoc is { } l && h - l >= ChuKyChiSo;

    /// <summary>Các mốc thời gian dự kiến trong [tu, den] (theo chu kỳ ngày, tính từ NgayTiepTheo).</summary>
    public IEnumerable<DateOnly> CacMoc(DateOnly tu, DateOnly den)
    {
        if (!TheoThoiGian) yield break;
        var d = NgayTiepTheo;
        while (d < tu) d = d.AddDays(ChuKyNgay);
        for (; d <= den && (NgayKetThuc is null || d <= NgayKetThuc); d = d.AddDays(ChuKyNgay)) yield return d;
    }

    private void DatTrangThai(TrangThaiKeHoach t) { TrangThai = t; DangApDung = t == TrangThaiKeHoach.HieuLuc; }

    public void KiemTra()
    {
        if (LoaiThietBiId is null && TaiSanId is null) throw new DomainException("Chọn loại thiết bị hoặc một tài sản cụ thể.");
        if (KieuChuKy != KieuChuKy.ThoiGian && (ChuKyChiSo is null or <= 0)) throw new DomainException("Nhập chu kỳ theo chỉ số (VD 20.000 trang).");
        if (NgayKetThuc is { } kt && kt < NgayTiepTheo) throw new DomainException("Ngày hết hiệu lực trước ngày thực hiện đầu tiên.");
    }

    /// <summary>Gửi trình: có người thẩm định → Chờ thẩm định; không → Chờ duyệt.</summary>
    public void GuiTrinh()
    {
        if (TrangThai is not TrangThaiKeHoach.Nhap) throw new DomainException("Chỉ gửi trình kế hoạch nháp.");
        KiemTra();
        if (string.IsNullOrWhiteSpace(NguoiDuyet)) throw new DomainException("Chọn người xét duyệt.");
        YKien = null;
        DatTrangThai(string.IsNullOrWhiteSpace(NguoiThamDinh) ? TrangThaiKeHoach.ChoDuyet : TrangThaiKeHoach.ChoThamDinh);
    }

    public void ThamDinh(string nguoi, bool dongY, string? yKien, DateTime now)
    {
        if (TrangThai != TrangThaiKeHoach.ChoThamDinh) throw new DomainException("Kế hoạch không ở bước thẩm định.");
        if (!string.Equals(nguoi, NguoiThamDinh, StringComparison.OrdinalIgnoreCase)) throw new DomainException($"Chỉ {NguoiThamDinh} được thẩm định kế hoạch này.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Ghi lý do khi trả lại.");
        NgayThamDinh = now; YKien = yKien;
        DatTrangThai(dongY ? TrangThaiKeHoach.ChoDuyet : TrangThaiKeHoach.Nhap);
    }

    public void Duyet(string nguoi, bool laQuanTri, bool dongY, string? yKien, DateTime now)
    {
        if (TrangThai != TrangThaiKeHoach.ChoDuyet) throw new DomainException("Kế hoạch không ở bước xét duyệt.");
        if (!laQuanTri && !string.Equals(nguoi, NguoiDuyet, StringComparison.OrdinalIgnoreCase)) throw new DomainException($"Chỉ {NguoiDuyet} (hoặc Quản trị) được duyệt kế hoạch này.");
        if (string.Equals(nguoi, NguoiTao, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người lập không tự duyệt kế hoạch.");
        if (!dongY && string.IsNullOrWhiteSpace(yKien)) throw new DomainException("Ghi lý do khi trả lại.");
        NgayDuyet = now; YKien = yKien;
        DatTrangThai(dongY ? TrangThaiKeHoach.HieuLuc : TrangThaiKeHoach.Nhap);
    }

    public void TamDung()
    {
        if (TrangThai != TrangThaiKeHoach.HieuLuc) throw new DomainException("Chỉ tạm dừng kế hoạch đang hiệu lực.");
        DatTrangThai(TrangThaiKeHoach.TamDung);
    }

    public void TiepTuc(DateOnly homNay)
    {
        if (TrangThai != TrangThaiKeHoach.TamDung) throw new DomainException("Kế hoạch không ở trạng thái tạm dừng.");
        if (NgayKetThuc is { } kt && kt < homNay) throw new DomainException("Kế hoạch đã quá ngày hiệu lực.");
        DatTrangThai(TrangThaiKeHoach.HieuLuc);
    }

    /// <summary>Hết ngày hiệu lực → chuyển Hết hiệu lực (job nền).</summary>
    public bool KiemTraHetHan(DateOnly homNay)
    {
        if (TrangThai != TrangThaiKeHoach.HieuLuc || NgayKetThuc is not { } kt || kt >= homNay) return false;
        DatTrangThai(TrangThaiKeHoach.HetHieuLuc); return true;
    }

    /// <summary>Sửa kế hoạch đã duyệt → quay lại nháp, phải trình duyệt lại.</summary>
    public void VeNhap() => DatTrangThai(TrangThaiKeHoach.Nhap);
}

/// <summary>Vật tư tiêu hao chuẩn cho mỗi lần bảo trì một thiết bị (FR-PM-10) — dùng dự trù kho / ngân sách.</summary>
public class KeHoachVatTu
{
    public int Id { get; set; }
    public int KeHoachBaoTriId { get; set; }
    public int VatTuId { get; set; }
    public VatTu? VatTu { get; set; }
    public decimal SoLuong { get; set; }
}

/// <summary>Trao đổi / bình luận giữa người báo và KTV ngay trong phiếu.</summary>
public class BinhLuanPhieu
{
    public long Id { get; set; }
    public int PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    public DateTime ThoiGian { get; set; }
    [StringLength(100)] public string NguoiViet { get; set; } = "";
    [Required, StringLength(2000)] public string NoiDung { get; set; } = "";
    /// <summary>Bình luận do hệ thống sinh khi đổi trạng thái.</summary>
    public bool LaHeThong { get; set; }
}
