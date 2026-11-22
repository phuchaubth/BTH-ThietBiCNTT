using System.ComponentModel.DataAnnotations;

namespace ThietBiCNTT.Domain.Entities;

public enum LoaiThongBao
{
    [Display(Name = "Yêu cầu sửa chữa mới")] PhieuMoi = 1,
    [Display(Name = "Được giao việc")] GiaoViec,
    [Display(Name = "Chờ nghiệm thu")] ChoNghiemThu,
    [Display(Name = "Bị trả lại")] TraLai,
    [Display(Name = "Trao đổi mới")] BinhLuan,
    [Display(Name = "Sắp đến hạn SLA")] SapDenHan,
    [Display(Name = "Quá hạn SLA – leo thang")] LeoThang,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Kết quả duyệt")] KetQuaDuyet,
    [Display(Name = "Lệnh bảo trì định kỳ")] BaoTri,
    [Display(Name = "Vật tư dưới định mức")] VatTu,
    [Display(Name = "Kiểm tra bất thường")] KiemTraBatThuong,
    [Display(Name = "Tổng hợp hằng ngày")] TongHopNgay,
    [Display(Name = "Hệ thống")] HeThong
}

public enum MucDoThongBao { ThongTin = 1, CanhBao, KhanCap }

/// <summary>Thông báo trong ứng dụng (chuông).</summary>
public class ThongBao
{
    public long Id { get; set; }
    [StringLength(100)] public string NguoiNhan { get; set; } = "";
    public LoaiThongBao Loai { get; set; }
    public MucDoThongBao MucDo { get; set; } = MucDoThongBao.ThongTin;
    [StringLength(300)] public string TieuDe { get; set; } = "";
    [StringLength(2000)] public string? NoiDung { get; set; }
    [StringLength(300)] public string? Link { get; set; }
    public DateTime ThoiGian { get; set; }
    public bool DaDoc { get; set; }
    public DateTime? ThoiGianDoc { get; set; }
}

public enum KenhGui
{
    [Display(Name = "Email")] Email = 1,
    [Display(Name = "Telegram")] Telegram
}

public enum TrangThaiGui
{
    [Display(Name = "Chờ gửi")] ChoGui = 1,
    [Display(Name = "Đã gửi")] DaGui,
    [Display(Name = "Lỗi")] Loi
}

/// <summary>Hàng đợi gửi ra ngoài (outbox): email, Telegram — gửi nền, tự thử lại khi lỗi.</summary>
public class TinNhanCho
{
    public const int SoLanThuToiDa = 5;
    public long Id { get; set; }
    public KenhGui Kenh { get; set; }
    [StringLength(200)] public string DiaChi { get; set; } = "";
    [StringLength(300)] public string TieuDe { get; set; } = "";
    [StringLength(4000)] public string NoiDung { get; set; } = "";
    public TrangThaiGui TrangThai { get; set; } = TrangThaiGui.ChoGui;
    public int SoLanThu { get; set; }
    [StringLength(1000)] public string? LoiCuoi { get; set; }
    public DateTime ThoiGianTao { get; set; }
    public DateTime GuiTu { get; set; }
    public DateTime? ThoiGianGui { get; set; }

    public void DanhDauDaGui(DateTime now) { TrangThai = TrangThaiGui.DaGui; ThoiGianGui = now; LoiCuoi = null; }

    /// <summary>Lỗi: thử lại sau 1, 2, 4, 8 phút; quá số lần → Lỗi (Quản trị gửi lại bằng tay).</summary>
    public void DanhDauLoi(string loi, DateTime now)
    {
        SoLanThu++;
        LoiCuoi = loi.Length > 1000 ? loi[..1000] : loi;
        if (SoLanThu >= SoLanThuToiDa) TrangThai = TrangThaiGui.Loi;
        else GuiTu = now.AddMinutes(Math.Pow(2, SoLanThu - 1));
    }

    public void GuiLai(DateTime now) { TrangThai = TrangThaiGui.ChoGui; SoLanThu = 0; GuiTu = now; }
}
