using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Mã các tham số hệ thống. Thêm tham số mới: khai báo ở đây + trong <see cref="ThamSoMacDinh.DanhSach"/>.</summary>
public static class TS
{
    // Thông tin đơn vị (in biểu mẫu, tem QR)
    public const string TenBenhVien = "DonVi.TenBenhVien";
    public const string CoQuanChuQuan = "DonVi.CoQuanChuQuan";
    public const string TenPhongCNTT = "DonVi.TenPhongCNTT";
    public const string DiaChiTruyCap = "DonVi.DiaChiTruyCap";
    // SLA
    public const string SlaKhanCap = "Sla.GioKhanCap";
    public const string SlaCao = "Sla.GioCao";
    public const string SlaTrungBinh = "Sla.GioTrungBinh";
    public const string SlaThap = "Sla.GioThap";
    public const string SlaHeSoKhoaTrongYeu = "Sla.HeSoKhoaTrongYeu";
    public const string SlaKhoaTrongYeu24h = "Sla.KhoaTrongYeu24h";
    public const string SlaGioBatDau = "Sla.GioBatDau";
    public const string SlaGioKetThuc = "Sla.GioKetThuc";
    public const string SlaNgayLamViec = "Sla.NgayLamViec";
    public const string SlaTamDungKhiCho = "Sla.TamDungKhiCho";
    public const string SlaNhacTruocPhut = "Sla.NhacTruocPhut";
    // Nghiệm thu
    public const string NghiemThuTuDongSauNgay = "NghiemThu.TuDongSauNgay";
    // Điều chuyển
    public const string DieuChuyenNguongLanhDao = "DieuChuyen.NguongDuyetLanhDao";
    // Bảo trì định kỳ
    public const string BaoTriSinhTruocNgay = "BaoTri.SinhLenhTruocNgay";
    public const string BaoTriHanHoanThanhNgay = "BaoTri.HanHoanThanhNgay";
    public const string BaoTriChuKyQuetGio = "BaoTri.ChuKyQuetGio";
    public const string BaoTriCanhBaoNgay = "BaoTri.CanhBaoDenHanNgay";
    // Tài sản
    public const string CanhBaoBaoHanhNgay = "TaiSan.CanhBaoBaoHanhNgay";
    public const string NguongChiPhiSuaPhanTram = "TaiSan.NguongChiPhiSuaPhanTram";
    public const string TuoiThoMacDinhNam = "TaiSan.TuoiThoMacDinhNam";
    public const string HopDongCanhBaoNgay = "HopDong.CanhBaoTruocNgay";
    // Kho
    public const string KhoChoXuatAm = "Kho.ChoXuatAm";
    // Mua sắm
    public const string MuaSamNguoiDuyetCNTT = "MuaSam.NguoiDuyetCNTT";
    public const string MuaSamNguoiDuyetVatTu = "MuaSam.NguoiDuyetVatTu";
    public const string MuaSamNguongLanhDao = "MuaSam.NguongDuyetLanhDao";
    public const string MuaSamTuDongDeXuat = "MuaSam.TuDongDeXuatTonThap";
    // Thanh lý
    public const string ThanhLyBatBuocThaoOCung = "ThanhLy.BatBuocThaoOCung";
    // Thông báo & cảnh báo
    public const string EmailBat = "ThongBao.EmailBat";
    public const string SmtpHost = "ThongBao.SmtpHost";
    public const string SmtpPort = "ThongBao.SmtpPort";
    public const string SmtpBaoMat = "ThongBao.SmtpBaoMat";
    public const string SmtpUser = "ThongBao.SmtpUser";
    public const string EmailGui = "ThongBao.EmailGui";
    public const string TelegramBat = "ThongBao.TelegramBat";
    public const string TelegramNhomCNTT = "ThongBao.TelegramNhomCNTT";
    public const string GioTongHop = "ThongBao.GioTongHop";
    public const string LeoThangCap2SauPhut = "LeoThang.Cap2SauPhut";
    public const string LeoThangCap2NguoiNhan = "LeoThang.Cap2NguoiNhan";
    public const string LeoThangCap3SauPhut = "LeoThang.Cap3SauPhut";
    // Bảo mật
    public const string PhienKhoaPhut = "BaoMat.PhienKhoaPhut";
    public const string PhienCNTTGio = "BaoMat.PhienCNTTGio";
    public const string ChoGhiNhoVoiKhoa = "BaoMat.ChoGhiNhoVoiKhoa";
}

public static class ThamSoMacDinh
{
    private static ThamSoHeThong T(int thuTu, string nhom, string ma, string ten, KieuThamSo kieu, string macDinh,
        string? donVi = null, decimal? min = null, decimal? max = null, string? moTa = null) =>
        new() { ThuTu = thuTu, Nhom = nhom, Ma = ma, Ten = ten, Kieu = kieu, GiaTri = macDinh, GiaTriMacDinh = macDinh, DonVi = donVi, ToiThieu = min, ToiDa = max, MoTa = moTa };

    public static IReadOnlyList<ThamSoHeThong> DanhSach() =>
    [
        T(10, "Thông tin đơn vị", TS.CoQuanChuQuan, "Cơ quan chủ quản", KieuThamSo.VanBan, "SỞ Y TẾ ..................", moTa: "In ở góc trái các biên bản."),
        T(11, "Thông tin đơn vị", TS.TenBenhVien, "Tên bệnh viện", KieuThamSo.VanBan, "BỆNH VIỆN ..................", moTa: "In trên biên bản và tem QR."),
        T(12, "Thông tin đơn vị", TS.TenPhongCNTT, "Tên đơn vị quản lý", KieuThamSo.VanBan, "PHÒNG CÔNG NGHỆ THÔNG TIN"),
        T(13, "Thông tin đơn vị", TS.DiaChiTruyCap, "Địa chỉ truy cập hệ thống", KieuThamSo.VanBan, "", moTa: "VD http://thietbi.benhvien.local — dùng để tạo link trong mã QR. Để trống = lấy theo địa chỉ đang truy cập."),

        T(20, "SLA sửa chữa", TS.SlaKhanCap, "Hạn xử lý mức Khẩn cấp", KieuThamSo.SoThuc, "2", "giờ", 0.25m, 720),
        T(21, "SLA sửa chữa", TS.SlaCao, "Hạn xử lý mức Cao", KieuThamSo.SoThuc, "8", "giờ", 0.25m, 720),
        T(22, "SLA sửa chữa", TS.SlaTrungBinh, "Hạn xử lý mức Trung bình", KieuThamSo.SoThuc, "24", "giờ", 0.25m, 720),
        T(23, "SLA sửa chữa", TS.SlaThap, "Hạn xử lý mức Thấp", KieuThamSo.SoThuc, "72", "giờ", 0.25m, 2160),
        T(24, "SLA sửa chữa", TS.SlaHeSoKhoaTrongYeu, "Hệ số rút ngắn cho khoa trọng yếu", KieuThamSo.SoThuc, "0.5", "× hạn", 0.1m, 1, "0.5 = khoa trọng yếu được xử lý trong một nửa thời gian."),
        T(25, "SLA sửa chữa", TS.SlaKhoaTrongYeu24h, "Khoa trọng yếu tính SLA 24/7", KieuThamSo.BatTat, "true", moTa: "Bật: Cấp cứu, HSTC, Phòng mổ… tính cả đêm/ngày nghỉ. Khoa khác luôn tính theo giờ hành chính."),
        T(26, "SLA sửa chữa", TS.SlaGioBatDau, "Giờ hành chính bắt đầu", KieuThamSo.Gio, "07:00"),
        T(27, "SLA sửa chữa", TS.SlaGioKetThuc, "Giờ hành chính kết thúc", KieuThamSo.Gio, "17:00"),
        T(28, "SLA sửa chữa", TS.SlaNgayLamViec, "Ngày làm việc trong tuần", KieuThamSo.ThuTrongTuan, "2,3,4,5,6,7", moTa: "Ngày nghỉ lễ khai báo ở bảng Ngày nghỉ bên dưới."),
        T(29, "SLA sửa chữa", TS.SlaTamDungKhiCho, "Tạm dừng SLA khi chờ linh kiện / gửi hãng", KieuThamSo.BatTat, "true"),
        T(30, "SLA sửa chữa", TS.SlaNhacTruocPhut, "Nhắc KTV trước khi đến hạn", KieuThamSo.SoNguyen, "30", "phút", 0, 1440),

        T(40, "Nghiệm thu", TS.NghiemThuTuDongSauNgay, "Tự động nghiệm thu sau", KieuThamSo.SoNguyen, "3", "ngày", 0, 30, "Phiếu đã hoàn thành mà khoa không phản hồi sau số ngày này sẽ tự đóng. 0 = tắt."),

        T(50, "Điều chuyển", TS.DieuChuyenNguongLanhDao, "Ngưỡng giá trị cần Lãnh đạo duyệt", KieuThamSo.SoNguyen, "30000000", "đ", 0, null, "Tổng nguyên giá phiếu ≥ ngưỡng: Trưởng khoa → P.CNTT → Lãnh đạo. Dưới ngưỡng: Trưởng khoa → P.CNTT."),

        T(60, "Bảo trì định kỳ", TS.BaoTriSinhTruocNgay, "Sinh lệnh bảo trì trước ngày đến hạn", KieuThamSo.SoNguyen, "0", "ngày", 0, 60),
        T(61, "Bảo trì định kỳ", TS.BaoTriHanHoanThanhNgay, "Thời hạn hoàn thành lệnh bảo trì", KieuThamSo.SoNguyen, "7", "ngày", 1, 90),
        T(62, "Bảo trì định kỳ", TS.BaoTriChuKyQuetGio, "Chu kỳ quét kế hoạch đến hạn", KieuThamSo.SoNguyen, "6", "giờ", 1, 24),
        T(63, "Bảo trì định kỳ", TS.BaoTriCanhBaoNgay, "Hiển thị cảnh báo kế hoạch sắp đến hạn", KieuThamSo.SoNguyen, "7", "ngày", 0, 60),

        T(70, "Tài sản", TS.CanhBaoBaoHanhNgay, "Cảnh báo sắp hết bảo hành trước", KieuThamSo.SoNguyen, "60", "ngày", 0, 365),
        T(71, "Tài sản", TS.NguongChiPhiSuaPhanTram, "Ngưỡng chi phí sửa chữa / nguyên giá", KieuThamSo.SoNguyen, "50", "%", 1, 500, "Vượt ngưỡng → gợi ý thay mới/thanh lý."),
        T(73, "Tài sản", TS.HopDongCanhBaoNgay, "Cảnh báo hợp đồng / bản quyền sắp hết hạn trước", KieuThamSo.SoNguyen, "60", "ngày", 0, 365),
        T(72, "Tài sản", TS.TuoiThoMacDinhNam, "Tuổi thọ mặc định khi tạo loại thiết bị", KieuThamSo.SoNguyen, "5", "năm", 1, 50),

        T(80, "Kho vật tư", TS.KhoChoXuatAm, "Cho phép xuất kho vượt tồn", KieuThamSo.BatTat, "false"),

        T(84, "Mua sắm", TS.MuaSamNguoiDuyetCNTT, "Người duyệt cấp 1 (Trưởng phòng CNTT)", KieuThamSo.VanBan, "admin", moTa: "Tên đăng nhập, cách nhau bởi dấu phẩy. Cũng là người xác nhận đặt hàng dưới ngưỡng Lãnh đạo."),
        T(85, "Mua sắm", TS.MuaSamNguoiDuyetVatTu, "Người duyệt cấp 2 (Phòng Vật tư - TTB)", KieuThamSo.VanBan, "", moTa: "Để trống = bỏ qua cấp này."),
        T(86, "Mua sắm", TS.MuaSamNguongLanhDao, "Ngưỡng giá trị cần Lãnh đạo duyệt", KieuThamSo.SoNguyen, "50000000", "đ", 0, null, "Đề xuất / đơn hàng có tổng giá trị ≥ ngưỡng phải qua Lãnh đạo (vai trò Lãnh đạo)."),
        T(87, "Mua sắm", TS.MuaSamTuDongDeXuat, "Tự lập đề xuất nháp khi vật tư dưới định mức", KieuThamSo.BatTat, "true", moTa: "Hằng ngày (cùng giờ bản tin tổng hợp). Số lượng gợi ý = tồn tối đa − tồn hiện tại − đang đặt."),

        T(90, "Thanh lý", TS.ThanhLyBatBuocThaoOCung, "Bắt buộc tháo ổ cứng trước khi hoàn tất", KieuThamSo.BatTat, "true", moTa: "Áp dụng cho loại thiết bị có lưu trữ dữ liệu (QĐ-03)."),

        T(110, "Thông báo", TS.EmailBat, "Gửi thông báo qua email", KieuThamSo.BatTat, "false", moTa: "Mật khẩu SMTP đặt trong appsettings.Production.json (ThongBao:SmtpPassword), không lưu trong CSDL."),
        T(111, "Thông báo", TS.SmtpHost, "Máy chủ SMTP", KieuThamSo.VanBan, "", moTa: "VD mail.bth.org.vn hoặc smtp.gmail.com"),
        T(112, "Thông báo", TS.SmtpPort, "Cổng SMTP", KieuThamSo.SoNguyen, "587", null, 1, 65535),
        T(113, "Thông báo", TS.SmtpBaoMat, "Mã hoá kết nối SMTP (TLS)", KieuThamSo.BatTat, "true", moTa: "Bật: STARTTLS (cổng 587) hoặc SSL (cổng 465). Tắt chỉ khi máy chủ mail nội bộ không hỗ trợ."),
        T(114, "Thông báo", TS.SmtpUser, "Tài khoản đăng nhập SMTP", KieuThamSo.VanBan, ""),
        T(115, "Thông báo", TS.EmailGui, "Địa chỉ email gửi đi", KieuThamSo.VanBan, "", moTa: "VD thietbi@bth.org.vn"),
        T(120, "Thông báo", TS.TelegramBat, "Gửi thông báo qua Telegram", KieuThamSo.BatTat, "false", moTa: "Token bot đặt trong appsettings.Production.json (ThongBao:TelegramBotToken)."),
        T(121, "Thông báo", TS.TelegramNhomCNTT, "Chat ID nhóm Telegram Phòng CNTT", KieuThamSo.VanBan, "", moTa: "Nhóm nhận: yêu cầu mới, quá hạn SLA, kiểm tra bất thường. VD -1001234567890"),
        T(122, "Thông báo", TS.GioTongHop, "Giờ gửi bản tin tổng hợp hằng ngày", KieuThamSo.Gio, "07:30", moTa: "Gửi cho CNTT: phiếu quá hạn, bảo hành/hợp đồng sắp hết, vật tư dưới định mức, bảo trì đến hạn."),
        T(130, "Thông báo", TS.LeoThangCap2SauPhut, "Leo thang cấp 2 sau khi quá hạn", KieuThamSo.SoNguyen, "60", "phút", 0, 10080, moTa: "Cấp 1 (ngay khi quá hạn): KTV + toàn bộ Cán bộ CNTT. Cấp 2: người nhận dưới đây. 0 = tắt."),
        T(131, "Thông báo", TS.LeoThangCap2NguoiNhan, "Người nhận leo thang cấp 2", KieuThamSo.VanBan, "admin", moTa: "Tên đăng nhập (Trưởng phòng CNTT…), cách nhau bởi dấu phẩy."),
        T(132, "Thông báo", TS.LeoThangCap3SauPhut, "Leo thang cấp 3 (Lãnh đạo) sau khi quá hạn", KieuThamSo.SoNguyen, "240", "phút", 0, 43200, moTa: "Chỉ áp dụng phiếu Khẩn cấp hoặc của khoa trọng yếu. Gửi cho các tài khoản vai trò Lãnh đạo. 0 = tắt."),

        T(100, "Bảo mật", TS.PhienKhoaPhut, "Tự đăng xuất (Nhân viên, Trưởng khoa) sau", KieuThamSo.SoNguyen, "30", "phút không thao tác", 5, 480),
        T(101, "Bảo mật", TS.PhienCNTTGio, "Thời gian giữ phiên (CNTT, KTV, Lãnh đạo)", KieuThamSo.SoNguyen, "8", "giờ", 1, 24),
        T(102, "Bảo mật", TS.ChoGhiNhoVoiKhoa, "Cho 'Ghi nhớ đăng nhập' với tài khoản khoa", KieuThamSo.BatTat, "false", moTa: "Nên tắt vì máy ở khoa dùng chung."),
    ];
}

/// <summary>Đọc/ghi tham số hệ thống, có cache bộ nhớ (xoá cache khi lưu).</summary>
public class ThamSoService(AppDbContext db, IMemoryCache cache)
{
    private const string CacheKey = "ThamSoHeThong";
    private const string CacheNgayNghi = "NgayNghi";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private Dictionary<string, string> TatCa() =>
        cache.GetOrCreate(CacheKey, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            var luu = db.ThamSoHeThongs.AsNoTracking().ToDictionary(x => x.Ma, x => x.GiaTri);
            foreach (var d in ThamSoMacDinh.DanhSach()) luu.TryAdd(d.Ma, d.GiaTriMacDinh);
            return luu;
        })!;

    public string Chuoi(string ma) => TatCa().GetValueOrDefault(ma) ?? "";
    public int SoNguyen(string ma) => int.TryParse(Chuoi(ma), NumberStyles.Integer, Inv, out var v) ? v : int.Parse(MacDinh(ma), Inv);
    public long SoLon(string ma) => long.TryParse(Chuoi(ma), NumberStyles.Integer, Inv, out var v) ? v : long.Parse(MacDinh(ma), Inv);
    public decimal SoThuc(string ma) => decimal.TryParse(Chuoi(ma), NumberStyles.Number, Inv, out var v) ? v : decimal.Parse(MacDinh(ma), Inv);
    public bool BatTat(string ma) => bool.TryParse(Chuoi(ma), out var v) ? v : bool.Parse(MacDinh(ma));
    public TimeOnly Gio(string ma) => TimeOnly.TryParseExact(Chuoi(ma), "HH:mm", Inv, DateTimeStyles.None, out var v) ? v : TimeOnly.ParseExact(MacDinh(ma), "HH:mm", Inv);
    public HashSet<DayOfWeek> ThuTrongTuan(string ma) =>
        Chuoi(ma).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n is >= 2 and <= 8)
            .Select(n => n == 8 ? DayOfWeek.Sunday : (DayOfWeek)(n - 1)).ToHashSet();

    private static string MacDinh(string ma) => ThamSoMacDinh.DanhSach().First(x => x.Ma == ma).GiaTriMacDinh;

    public HashSet<DateOnly> NgayNghi() =>
        cache.GetOrCreate(CacheNgayNghi, e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return db.NgayNghis.AsNoTracking().Select(x => x.Ngay).ToHashSet();
        })!;

    public SlaThamSo Sla() => new(
        SoThuc(TS.SlaKhanCap), SoThuc(TS.SlaCao), SoThuc(TS.SlaTrungBinh), SoThuc(TS.SlaThap),
        SoThuc(TS.SlaHeSoKhoaTrongYeu), BatTat(TS.SlaKhoaTrongYeu24h),
        Gio(TS.SlaGioBatDau), Gio(TS.SlaGioKetThuc), ThuTrongTuan(TS.SlaNgayLamViec), NgayNghi());

    public DateTime TinhHanSla(DateTime batDau, Domain.Enums.MucDoUuTien uuTien, bool khoaTrongYeu) =>
        SlaCalculator.TinhHan(batDau, uuTien, khoaTrongYeu, Sla());

    /// <summary>Kiểm tra và chuẩn hoá giá trị theo kiểu; trả về thông báo lỗi hoặc null.</summary>
    public static string? KiemTra(ThamSoHeThong dn, ref string giaTri)
    {
        giaTri = (giaTri ?? "").Trim();
        switch (dn.Kieu)
        {
            case KieuThamSo.SoNguyen:
                giaTri = giaTri.Replace(".", "").Replace(",", "").Replace(" ", "");
                if (!long.TryParse(giaTri, NumberStyles.Integer, Inv, out var n)) return "phải là số nguyên";
                if (dn.ToiThieu is { } a && n < a || dn.ToiDa is { } b && n > b) return $"phải trong khoảng {dn.ToiThieu:0.##} – {dn.ToiDa:0.##}";
                break;
            case KieuThamSo.SoThuc:
                giaTri = giaTri.Replace(",", ".");
                if (!decimal.TryParse(giaTri, NumberStyles.Number, Inv, out var d)) return "phải là số";
                if (dn.ToiThieu is { } a2 && d < a2 || dn.ToiDa is { } b2 && d > b2) return $"phải trong khoảng {dn.ToiThieu:0.##} – {dn.ToiDa:0.##}";
                giaTri = d.ToString(Inv);
                break;
            case KieuThamSo.Gio:
                if (!TimeOnly.TryParse(giaTri, Inv, out var g)) return "phải có dạng HH:mm";
                giaTri = g.ToString("HH:mm", Inv);
                break;
            case KieuThamSo.BatTat:
                giaTri = (giaTri is "true" or "on" or "True") ? "true" : "false";
                break;
            case KieuThamSo.ThuTrongTuan:
                var ds = giaTri.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().OrderBy(x => x).ToList();
                if (ds.Count == 0 || ds.Any(x => !int.TryParse(x, out var t) || t < 2 || t > 8)) return "chọn ít nhất một ngày";
                giaTri = string.Join(",", ds);
                break;
        }
        return null;
    }

    public async Task<List<string>> LuuAsync(Dictionary<string, string> giaTris, string? nguoi)
    {
        var loi = new List<string>();
        var dsLuu = await db.ThamSoHeThongs.ToDictionaryAsync(x => x.Ma);
        foreach (var dn in ThamSoMacDinh.DanhSach())
        {
            if (!giaTris.TryGetValue(dn.Ma, out var v)) continue;
            if (KiemTra(dn, ref v) is { } e) { loi.Add($"{dn.Ten}: {e}"); continue; }
            var row = dsLuu[dn.Ma];
            if (row.GiaTri == v) continue;
            row.GiaTri = v; row.NgayCapNhat = DateTime.Now; row.NguoiCapNhat = nguoi;
            db.NhatKyHeThongs.Add(new NhatKyHeThong { ThoiGian = DateTime.Now, NguoiDung = nguoi, HanhDong = "Sua", DoiTuong = nameof(ThamSoHeThong), DoiTuongId = dn.Ma, ChiTiet = $"→ {v}" });
        }
        if (loi.Count == 0) { await db.SaveChangesAsync(); XoaCache(); }
        return loi;
    }

    public void XoaCache() { cache.Remove(CacheKey); cache.Remove(CacheNgayNghi); }

    /// <summary>Đồng bộ danh mục tham số: thêm tham số mới (giữ giá trị đã chỉnh), cập nhật tên/mô tả/giới hạn.</summary>
    public static async Task DongBoAsync(AppDbContext db)
    {
        var co = await db.ThamSoHeThongs.ToDictionaryAsync(x => x.Ma);
        foreach (var dn in ThamSoMacDinh.DanhSach())
        {
            if (co.TryGetValue(dn.Ma, out var r))
            {
                r.Nhom = dn.Nhom; r.Ten = dn.Ten; r.MoTa = dn.MoTa; r.Kieu = dn.Kieu; r.DonVi = dn.DonVi;
                r.ToiThieu = dn.ToiThieu; r.ToiDa = dn.ToiDa; r.ThuTu = dn.ThuTu; r.GiaTriMacDinh = dn.GiaTriMacDinh;
            }
            else db.ThamSoHeThongs.Add(dn);
        }
        await db.SaveChangesAsync();
    }
}
