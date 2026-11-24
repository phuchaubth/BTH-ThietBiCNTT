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
    // GĐ3.1 – Hạ tầng CNTT theo JCI 8 (bảng 4.1.3 và các thời hạn trong docs/JCI8-HaTangCNTT.md)
    public const string JciRto1 = "Jci.RtoToiQuanTrongPhut";
    public const string JciRto2 = "Jci.RtoCaoPhut";
    public const string JciRto3 = "Jci.RtoTrungBinhPhut";
    public const string JciRto4 = "Jci.RtoThapPhut";
    public const string JciRpo1 = "Jci.RpoToiQuanTrongPhut";
    public const string JciRpo2 = "Jci.RpoCaoPhut";
    public const string JciRpo3 = "Jci.RpoTrungBinhPhut";
    public const string JciRpo4 = "Jci.RpoThapPhut";
    public const string JciDienTap1 = "Jci.DienTapToiQuanTrongThang";
    public const string JciDienTap2 = "Jci.DienTapCaoThang";
    public const string JciDienTap3 = "Jci.DienTapTrungBinhThang";
    public const string JciDienTap4 = "Jci.DienTapThapThang";
    public const string JciPhucHoi1 = "Jci.ThuPhucHoiToiQuanTrongThang";
    public const string JciPhucHoi2 = "Jci.ThuPhucHoiCaoThang";
    public const string JciPhucHoi3 = "Jci.ThuPhucHoiTrungBinhThang";
    public const string JciPhucHoi4 = "Jci.ThuPhucHoiThapThang";
    public const string JciRaSoatKeHoachThang = "Jci.RaSoatKeHoachThang";
    public const string JciThongBaoTruocGio = "Jci.ThongBaoDowntimeTruocGio";
    public const string JciNhacDienTapTruocNgay = "Jci.NhacDienTapTruocNgay";
    public const string JciDienTapLaiNgay = "Jci.DienTapLaiTrongNgay";
    public const string JciLeoThangDienTap2Ngay = "Jci.LeoThangDienTapCap2Ngay";
    public const string JciLeoThangDienTap3Ngay = "Jci.LeoThangDienTapCap3Ngay";
    public const string JciNhacPhucHoiTruocNgay = "Jci.NhacThuPhucHoiTruocNgay";
    public const string JciPhucHoiLaiNgay = "Jci.ThuPhucHoiLaiTrongNgay";
    public const string JciSaoLuuChoGio = "Jci.SaoLuuKhoangChoGio";
    public const string JciSaoLuuLoiTaoPhieu = "Jci.SaoLuuLoiLienTiepTaoPhieu";
    public const string JciHanhDongNhacTruocNgay = "Jci.HanhDongNhacTruocNgay";
    public const string JciHanhDongMacDinhNgay = "Jci.HanhDongHanMacDinhNgay";
    public const string JciNguoiDuyetCntt = "Jci.NguoiDuyetCntt";
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

        T(200, "JCI 8 – RTO/RPO", TS.JciRto1, "RTO mức Tối quan trọng", KieuThamSo.SoNguyen, "240", "phút", 1, 100000, "Thời gian tối đa được phép để khôi phục hệ thống (bảng 4.1.3). Hệ thống có thể ghi đè riêng."),
        T(201, "JCI 8 – RTO/RPO", TS.JciRpo1, "RPO mức Tối quan trọng", KieuThamSo.SoNguyen, "15", "phút", 1, 100000, "Lượng dữ liệu tối đa được phép mất, tính theo thời gian."),
        T(202, "JCI 8 – RTO/RPO", TS.JciRto2, "RTO mức Cao", KieuThamSo.SoNguyen, "480", "phút", 1, 100000),
        T(203, "JCI 8 – RTO/RPO", TS.JciRpo2, "RPO mức Cao", KieuThamSo.SoNguyen, "60", "phút", 1, 100000),
        T(204, "JCI 8 – RTO/RPO", TS.JciRto3, "RTO mức Trung bình", KieuThamSo.SoNguyen, "1440", "phút", 1, 100000),
        T(205, "JCI 8 – RTO/RPO", TS.JciRpo3, "RPO mức Trung bình", KieuThamSo.SoNguyen, "1440", "phút", 1, 100000),
        T(206, "JCI 8 – RTO/RPO", TS.JciRto4, "RTO mức Thấp", KieuThamSo.SoNguyen, "4320", "phút", 1, 100000),
        T(207, "JCI 8 – RTO/RPO", TS.JciRpo4, "RPO mức Thấp", KieuThamSo.SoNguyen, "1440", "phút", 1, 100000),

        T(209, "JCI 8 – Downtime", TS.JciNguoiDuyetCntt, "Người duyệt bước Trưởng phòng CNTT", KieuThamSo.VanBan, "admin", moTa: "Tên đăng nhập, cách nhau bởi dấu phẩy. Duyệt kế hoạch downtime (FR-DT-02), xác nhận dùng downtime thực tế thay diễn tập (FR-DT-12), duyệt gia hạn hành động khắc phục."),
        T(210, "JCI 8 – Downtime", TS.JciDienTap1, "Chu kỳ diễn tập downtime – Tối quan trọng", KieuThamSo.SoNguyen, "6", "tháng", 0, 60, "HCT.01.04 ME1: thử nghiệm chương trình ít nhất mỗi năm (tối đa 12 tháng). 0 = không bắt buộc."),
        T(211, "JCI 8 – Downtime", TS.JciDienTap2, "Chu kỳ diễn tập downtime – Cao", KieuThamSo.SoNguyen, "12", "tháng", 0, 60),
        T(212, "JCI 8 – Downtime", TS.JciDienTap3, "Chu kỳ diễn tập downtime – Trung bình", KieuThamSo.SoNguyen, "12", "tháng", 0, 60),
        T(213, "JCI 8 – Downtime", TS.JciDienTap4, "Chu kỳ diễn tập downtime – Thấp", KieuThamSo.SoNguyen, "12", "tháng", 0, 60),
        T(214, "JCI 8 – Downtime", TS.JciRaSoatKeHoachThang, "Rà soát lại kế hoạch downtime sau", KieuThamSo.SoNguyen, "12", "tháng", 1, 60, "FR-DT-02: đến hạn sẽ nhắc người phụ trách kỹ thuật."),
        T(215, "JCI 8 – Downtime", TS.JciThongBaoTruocGio, "Thông báo downtime có kế hoạch tới khoa trước", KieuThamSo.SoNguyen, "48", "giờ", 1, 720, "FR-DT-05. Hệ thống nhắc lại trước 1 giờ."),
        T(216, "JCI 8 – Downtime", TS.JciNhacDienTapTruocNgay, "Nhắc diễn tập trước hạn", KieuThamSo.SoNguyen, "30", "ngày", 0, 180, "FR-DT-10: đợt diễn tập được tạo trước hạn số ngày này."),
        T(217, "JCI 8 – Downtime", TS.JciDienTapLaiNgay, "Diễn tập lại khi Không đạt trong", KieuThamSo.SoNguyen, "90", "ngày", 7, 365, "FR-DT-12."),
        T(218, "JCI 8 – Downtime", TS.JciLeoThangDienTap2Ngay, "Diễn tập quá hạn: báo Trưởng phòng CNTT sau", KieuThamSo.SoNguyen, "7", "ngày", 0, 365, "BR-DT-06. Người nhận = Người nhận leo thang cấp 2 (nhóm Thông báo)."),
        T(219, "JCI 8 – Downtime", TS.JciLeoThangDienTap3Ngay, "Diễn tập quá hạn: báo Lãnh đạo sau", KieuThamSo.SoNguyen, "30", "ngày", 0, 365, "BR-DT-06."),

        T(230, "JCI 8 – Sao lưu", TS.JciPhucHoi1, "Chu kỳ thử phục hồi – Tối quan trọng", KieuThamSo.SoNguyen, "3", "tháng", 0, 60, "Intent HCT.01.04: sao lưu đơn giản thử ít nhất mỗi quý và sau thay đổi lớn của hệ thống sao lưu. 0 = không bắt buộc."),
        T(231, "JCI 8 – Sao lưu", TS.JciPhucHoi2, "Chu kỳ thử phục hồi – Cao", KieuThamSo.SoNguyen, "3", "tháng", 0, 60),
        T(232, "JCI 8 – Sao lưu", TS.JciPhucHoi3, "Chu kỳ thử phục hồi – Trung bình", KieuThamSo.SoNguyen, "6", "tháng", 0, 60),
        T(233, "JCI 8 – Sao lưu", TS.JciPhucHoi4, "Chu kỳ thử phục hồi – Thấp", KieuThamSo.SoNguyen, "12", "tháng", 0, 60),
        T(234, "JCI 8 – Sao lưu", TS.JciNhacPhucHoiTruocNgay, "Nhắc thử phục hồi trước hạn", KieuThamSo.SoNguyen, "14", "ngày", 0, 180, "FR-SL-05."),
        T(235, "JCI 8 – Sao lưu", TS.JciPhucHoiLaiNgay, "Thử phục hồi lại khi Không đạt trong", KieuThamSo.SoNguyen, "30", "ngày", 1, 365, "FR-SL-07."),
        T(236, "JCI 8 – Sao lưu", TS.JciSaoLuuChoGio, "Khoảng chờ kết quả sao lưu sau giờ dự kiến", KieuThamSo.SoNguyen, "3", "giờ", 1, 72, "FR-SL-04: quá khoảng này chưa có kết quả → 'Không rõ' và cảnh báo."),
        T(237, "JCI 8 – Sao lưu", TS.JciSaoLuuLoiTaoPhieu, "Tự tạo phiếu sửa chữa khi lỗi liên tiếp (hệ thống Tối quan trọng)", KieuThamSo.SoNguyen, "2", "lần", 0, 20, "FR-SL-04. 0 = tắt."),

        T(240, "JCI 8 – Hành động khắc phục", TS.JciHanhDongMacDinhNgay, "Hạn mặc định của hành động khắc phục", KieuThamSo.SoNguyen, "30", "ngày", 1, 365),
        T(241, "JCI 8 – Hành động khắc phục", TS.JciHanhDongNhacTruocNgay, "Nhắc trước hạn", KieuThamSo.SoNguyen, "3", "ngày", 0, 60, "FR-HD-03. Quá hạn: leo thang người nhận cấp 2 sau 7 ngày, Lãnh đạo sau 30 ngày (theo BR-DT-06)."),

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

    /// <summary>Bảng 4.1.3: RTO/RPO mặc định, chu kỳ diễn tập & thử phục hồi theo mức trọng yếu.</summary>
    public MucTieuTheoMuc MucTieu(MucTrongYeu m) => m switch
    {
        MucTrongYeu.ToiQuanTrong => new(SoNguyen(TS.JciRto1), SoNguyen(TS.JciRpo1), SoNguyen(TS.JciDienTap1), SoNguyen(TS.JciPhucHoi1)),
        MucTrongYeu.Cao => new(SoNguyen(TS.JciRto2), SoNguyen(TS.JciRpo2), SoNguyen(TS.JciDienTap2), SoNguyen(TS.JciPhucHoi2)),
        MucTrongYeu.TrungBinh => new(SoNguyen(TS.JciRto3), SoNguyen(TS.JciRpo3), SoNguyen(TS.JciDienTap3), SoNguyen(TS.JciPhucHoi3)),
        _ => new(SoNguyen(TS.JciRto4), SoNguyen(TS.JciRpo4), SoNguyen(TS.JciDienTap4), SoNguyen(TS.JciPhucHoi4)),
    };

    public int Rto(HeThongThongTin h) => h.RtoPhut ?? MucTieu(h.MucTrongYeu).RtoPhut;
    public int Rpo(HeThongThongTin h) => h.RpoPhut ?? MucTieu(h.MucTrongYeu).RpoPhut;

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
