using System.ComponentModel.DataAnnotations;

namespace ThietBiCNTT.Domain.Enums;

public enum NhomThietBi
{
    [Display(Name = "Máy tính để bàn")] MayTinhDeBan = 1,
    [Display(Name = "Máy tính xách tay")] MayTinhXachTay,
    [Display(Name = "Máy chủ")] MayChu,
    [Display(Name = "Máy in / Scan")] MayIn,
    [Display(Name = "Thiết bị mạng")] ThietBiMang,
    [Display(Name = "Màn hình")] ManHinh,
    [Display(Name = "UPS / Nguồn")] Ups,
    [Display(Name = "Camera")] Camera,
    [Display(Name = "Thiết bị ngoại vi")] NgoaiVi,
    [Display(Name = "Khác")] Khac
}

public enum TrangThaiTaiSan
{
    [Display(Name = "Trong kho")] TrongKho = 1,
    [Display(Name = "Đang sử dụng")] DangSuDung,
    [Display(Name = "Đang sửa chữa")] DangSuaChua,
    [Display(Name = "Hỏng")] Hong,
    [Display(Name = "Chờ thanh lý")] ChoThanhLy,
    [Display(Name = "Đã thanh lý")] DaThanhLy
}

public enum NguonVon
{
    [Display(Name = "Ngân sách nhà nước")] NganSach = 1,
    [Display(Name = "Quỹ phát triển sự nghiệp")] QuyPhatTrien,
    [Display(Name = "Tài trợ / Viện trợ")] TaiTro,
    [Display(Name = "Xã hội hoá")] XaHoiHoa,
    [Display(Name = "Khác")] Khac
}

public enum LoaiViTri
{
    [Display(Name = "Toà nhà")] ToaNha = 1,
    [Display(Name = "Tầng")] Tang,
    [Display(Name = "Phòng")] Phong
}

public enum MucDoUuTien
{
    [Display(Name = "Thấp")] Thap = 1,
    [Display(Name = "Trung bình")] TrungBinh,
    [Display(Name = "Cao")] Cao,
    [Display(Name = "Khẩn cấp")] KhanCap
}

public enum LoaiCongViec
{
    [Display(Name = "Sửa chữa đột xuất")] SuaChua = 1,
    [Display(Name = "Bảo trì định kỳ")] BaoTriDinhKy
}

public enum TrangThaiPhieuSuaChua
{
    [Display(Name = "Mới")] Moi = 1,
    [Display(Name = "Đã tiếp nhận")] DaTiepNhan,
    [Display(Name = "Đang xử lý")] DangXuLy,
    [Display(Name = "Chờ linh kiện")] ChoLinhKien,
    [Display(Name = "Gửi hãng / Bảo hành")] GuiHang,
    [Display(Name = "Hoàn thành")] HoanThanh,
    [Display(Name = "Đã nghiệm thu")] DaNghiemThu,
    [Display(Name = "Đã huỷ")] DaHuy
}

public enum HinhThucXuLy
{
    [Display(Name = "Tự sửa (Phòng CNTT)")] TuSua = 1,
    [Display(Name = "Bảo hành hãng")] BaoHanh,
    [Display(Name = "Thuê ngoài")] ThueNgoai
}

public enum TrangThaiDieuChuyen
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ duyệt")] ChoDuyet,
    [Display(Name = "Đã duyệt")] DaDuyet,
    [Display(Name = "Từ chối")] TuChoi,
    [Display(Name = "Đã bàn giao")] DaBanGiao
}

public enum TrangThaiThanhLy
{
    [Display(Name = "Nháp")] Nhap = 1,
    [Display(Name = "Chờ hội đồng đánh giá")] ChoHoiDong,
    [Display(Name = "Hội đồng đã đánh giá")] DaDanhGia,
    [Display(Name = "Đã có quyết định")] DaPheDuyet,
    [Display(Name = "Đã thanh lý")] DaThanhLy,
    [Display(Name = "Từ chối")] TuChoi
}

public enum HinhThucThanhLy
{
    [Display(Name = "Bán đấu giá")] BanDauGia = 1,
    [Display(Name = "Bán trực tiếp")] BanTrucTiep,
    [Display(Name = "Phá dỡ / Huỷ")] PhaDo,
    [Display(Name = "Điều chuyển đơn vị khác")] DieuChuyenNgoai
}

public enum LoaiSuKien
{
    [Display(Name = "Tạo mới")] TaoMoi = 1,
    [Display(Name = "Cập nhật")] CapNhat,
    [Display(Name = "Thay đổi cấu hình")] CauHinh,
    [Display(Name = "Sửa chữa")] SuaChua,
    [Display(Name = "Bảo trì")] BaoTri,
    [Display(Name = "Điều chuyển")] DieuChuyen,
    [Display(Name = "Thanh lý")] ThanhLy
}
