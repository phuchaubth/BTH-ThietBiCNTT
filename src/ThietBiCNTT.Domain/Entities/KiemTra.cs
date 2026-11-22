using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ThietBiCNTT.Domain.Common;

namespace ThietBiCNTT.Domain.Entities;

public enum KieuMucKiemTra
{
    [Display(Name = "Đạt / Không đạt")] DatKhongDat = 1,
    [Display(Name = "Số đo (có ngưỡng)")] So,
    [Display(Name = "Ghi chú văn bản")] VanBan
}

/// <summary>
/// Mẫu phiếu kiểm tra. Đối tượng: một tài sản cụ thể, hoặc mọi tài sản đang dùng của một loại (có thể giới hạn trong một khoa),
/// hoặc không gắn tài sản (kiểm tra theo vị trí — VD phòng máy chủ).
/// </summary>
public class MauKiemTra : BaseEntity
{
    [Required, StringLength(200), Display(Name = "Tên phiếu kiểm tra")] public string Ten { get; set; } = "";
    [StringLength(1000), Display(Name = "Hướng dẫn / mô tả")] public string? MoTa { get; set; }
    [Display(Name = "Loại thiết bị áp dụng")] public int? LoaiThietBiId { get; set; }
    public LoaiThietBi? LoaiThietBi { get; set; }
    [Display(Name = "Tài sản cụ thể")] public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    /// <summary>Khoa thực hiện / phạm vi: giới hạn tài sản theo khoa và cho phép người của khoa ghi kiểm tra.</summary>
    [Display(Name = "Khoa/phòng thực hiện")] public int? KhoaPhongId { get; set; }
    public KhoaPhong? KhoaPhong { get; set; }
    [StringLength(200), Display(Name = "Vị trí kiểm tra (nếu không gắn tài sản)")] public string? ViTri { get; set; }
    [Range(1, 8760), Display(Name = "Chu kỳ (giờ)")] public int ChuKyGio { get; set; } = 24;
    [Display(Name = "Bất thường tự tạo phiếu sửa chữa")] public bool TuTaoPhieu { get; set; } = true;
    [Display(Name = "Đang áp dụng")] public bool DangApDung { get; set; } = true;
    public List<MucKiemTra> Mucs { get; set; } = [];

    public bool KhongGanTaiSan => TaiSanId is null && LoaiThietBiId is null;

    public static string TenChuKy(int gio) => gio switch
    {
        8 => "Mỗi ca (8 giờ)", 12 => "12 giờ", 24 => "Hằng ngày", 168 => "Hằng tuần", 720 => "Hằng tháng",
        _ => gio % 24 == 0 ? $"{gio / 24} ngày" : $"{gio} giờ"
    };

    /// <summary>Chấm một lần kiểm tra theo các mục của mẫu. <paramref name="nhap"/>: Id mục → giá trị người dùng nhập.</summary>
    public LanKiemTra ChamDiem(IReadOnlyDictionary<int, string?> nhap, int? taiSanId, string nguoi, DateTime now, string? ghiChu)
    {
        if (!DangApDung) throw new DomainException("Phiếu kiểm tra đã ngừng áp dụng.");
        if (!KhongGanTaiSan && taiSanId is null) throw new DomainException("Chọn thiết bị được kiểm tra.");
        var lan = new LanKiemTra { MauKiemTraId = Id, TaiSanId = taiSanId, NguoiKiem = nguoi, ThoiGian = now, GhiChu = ghiChu };
        var thieu = new List<string>();
        foreach (var m in Mucs.OrderBy(m => m.ThuTu))
        {
            var v = nhap.GetValueOrDefault(m.Id)?.Trim();
            if (string.IsNullOrEmpty(v)) { if (m.BatBuoc) thieu.Add(m.NoiDung); continue; }
            lan.KetQuas.Add(m.Cham(v));
        }
        if (thieu.Count > 0) throw new DomainException("Chưa nhập: " + string.Join("; ", thieu));
        lan.CoBatThuong = lan.KetQuas.Any(k => k.BatThuong);
        return lan;
    }
}

public class MucKiemTra
{
    public int Id { get; set; }
    public int MauKiemTraId { get; set; }
    public MauKiemTra? MauKiemTra { get; set; }
    public int ThuTu { get; set; }
    [Required, StringLength(300)] public string NoiDung { get; set; } = "";
    public KieuMucKiemTra Kieu { get; set; } = KieuMucKiemTra.DatKhongDat;
    [StringLength(20)] public string? DonVi { get; set; }
    public decimal? NguongMin { get; set; }
    public decimal? NguongMax { get; set; }
    public bool BatBuoc { get; set; } = true;

    public string MoTaNguong => Kieu != KieuMucKiemTra.So ? "" :
        (NguongMin, NguongMax) switch
        {
            ({ } a, { } b) => $"{a:0.##}–{b:0.##} {DonVi}",
            ({ } a, null) => $"≥ {a:0.##} {DonVi}",
            (null, { } b) => $"≤ {b:0.##} {DonVi}",
            _ => DonVi ?? ""
        };

    public KetQuaKiemTra Cham(string giaTri)
    {
        var kq = new KetQuaKiemTra { MucKiemTraId = Id, NoiDungMuc = NoiDung, DonVi = DonVi };
        switch (Kieu)
        {
            case KieuMucKiemTra.DatKhongDat:
                kq.Dat = giaTri is "true" or "Dat" or "1";
                kq.BatThuong = kq.Dat == false;
                break;
            case KieuMucKiemTra.So:
                if (!decimal.TryParse(giaTri.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var so))
                    throw new DomainException($"'{NoiDung}': '{giaTri}' không phải số.");
                kq.GiaTriSo = so;
                kq.BatThuong = (NguongMin is { } a && so < a) || (NguongMax is { } b && so > b);
                kq.Dat = !kq.BatThuong;
                break;
            default:
                kq.GiaTriText = giaTri.Length > 1000 ? giaTri[..1000] : giaTri;
                break;
        }
        return kq;
    }
}

public class LanKiemTra
{
    public long Id { get; set; }
    public int MauKiemTraId { get; set; }
    public MauKiemTra? MauKiemTra { get; set; }
    public int? TaiSanId { get; set; }
    public TaiSan? TaiSan { get; set; }
    [StringLength(100)] public string NguoiKiem { get; set; } = "";
    public DateTime ThoiGian { get; set; }
    public bool CoBatThuong { get; set; }
    [StringLength(1000)] public string? GhiChu { get; set; }
    /// <summary>Phiếu sửa chữa được tạo / liên kết khi có bất thường.</summary>
    public int? PhieuSuaChuaId { get; set; }
    public PhieuSuaChua? PhieuSuaChua { get; set; }
    public List<KetQuaKiemTra> KetQuas { get; set; } = [];

    public string TomTatBatThuong() => string.Join("; ", KetQuas.Where(k => k.BatThuong).Select(k =>
        k.GiaTriSo is { } s ? $"{k.NoiDungMuc}: {s:0.##} {k.DonVi}".Trim() : $"{k.NoiDungMuc}: Không đạt"));
}

public class KetQuaKiemTra
{
    public long Id { get; set; }
    public long LanKiemTraId { get; set; }
    public LanKiemTra? LanKiemTra { get; set; }
    public int MucKiemTraId { get; set; }
    /// <summary>Lưu lại nội dung mục tại thời điểm kiểm tra (mẫu có thể sửa sau).</summary>
    [StringLength(300)] public string NoiDungMuc { get; set; } = "";
    [StringLength(20)] public string? DonVi { get; set; }
    public bool? Dat { get; set; }
    public decimal? GiaTriSo { get; set; }
    [StringLength(1000)] public string? GiaTriText { get; set; }
    public bool BatThuong { get; set; }
}

/// <summary>Tệp đính kèm (ảnh, PDF) gắn với phiếu sửa chữa, lần kiểm tra, tài sản…</summary>
public class TepDinhKem
{
    public long Id { get; set; }
    [StringLength(50)] public string DoiTuong { get; set; } = "";
    [StringLength(50)] public string DoiTuongId { get; set; } = "";
    [StringLength(255)] public string TenFile { get; set; } = "";
    [StringLength(100)] public string TenLuu { get; set; } = "";
    [StringLength(100)] public string ContentType { get; set; } = "";
    public long KichThuoc { get; set; }
    [StringLength(100)] public string? NguoiTai { get; set; }
    public DateTime ThoiGian { get; set; }
    public bool LaAnh => ContentType.StartsWith("image/");
}
