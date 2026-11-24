# Quyết định nghiệp vụ (chốt ngày 21/11/2026)

Các quyết định dưới đây **thay thế** nội dung tương ứng trong `SRS.md` v1.1 và `ThietKeHeThong.md` (Phụ lục A).

**Trạng thái (21/11/2026): cả 5 quyết định đã được áp dụng vào mã nguồn**, có unit test (`tests/ThietBiCNTT.Tests`) và kiểm thử end-to-end theo vai trò (`tests/e2e/e2e.py`).

| # | Chủ đề | Quyết định | Ảnh hưởng thiết kế / mã nguồn |
|---|---|---|---|
| QĐ-01 | Nghiệm thu phiếu sửa chữa | Chỉ **người báo hỏng** hoặc **Trưởng khoa/phòng** của khoa sở hữu thiết bị được nghiệm thu / trả lại. KTV xử lý phiếu **không** được tự nghiệm thu. Phiếu "Hoàn thành" quá **N ngày** (cấu hình, mặc định 3) không phản hồi → hệ thống tự đóng, ghi "Tự động nghiệm thu". | `PhieuSuaChuaController.NghiemThu/TraLai`: kiểm tra `NguoiTao == user` hoặc vai trò `TruongKhoa` + cùng `KhoaPhongId`; chặn `KyThuatVien == user`. Thêm job nền tự đóng phiếu. |
| QĐ-02 | Duyệt điều chuyển | **Theo giá trị tài sản.** Tổng nguyên giá phiếu < ngưỡng (cấu hình, mặc định **30 triệu đ**): 2 cấp **Trưởng khoa đi → Phòng CNTT**. ≥ ngưỡng: thêm cấp 3 **Lãnh đạo**. Người lập phiếu không được tự duyệt. | `PhieuDieuChuyen`: thêm bảng bước duyệt (`BuocDuyet`: cấp, vai trò, người, thời điểm, ý kiến), trạng thái `ChoDuyetKhoa → ChoDuyetCNTT → ChoDuyetLanhDao → DaDuyet`. Chặn tài sản đang nằm trong phiếu điều chuyển khác / đang có phiếu sửa chữa mở. Cập nhật cả `ViTriId` khi bàn giao. |
| QĐ-03 | Bảo mật dữ liệu khi thanh lý | **Tháo ổ cứng giữ lại.** Với thiết bị có lưu trữ, trước khi bàn giao máy cho đơn vị mua/huỷ, Phòng CNTT tháo ổ lưu trữ, ghi nhận **serial ổ**, người tháo, thời điểm, nơi lưu (kho CNTT) và hình thức xử lý sau đó (lưu kho / huỷ vật lý có biên bản). Chặn bước "Hoàn tất & ghi giảm" nếu còn thiết bị chưa ghi nhận tháo ổ. | `ChiTietThanhLy`: thay `DaXoaDuLieu/XacNhanXoaDuLieu` bằng `DaThaoOCung`, `SerialOCung`, `NguoiThao`, `NgayThao`, `NoiLuuOCung`, `XuLyOCung` (LuuKho/HuyVatLy), `BienBanHuyOCung`. Thêm danh sách "Ổ cứng thu hồi" để theo dõi đến khi huỷ. Cập nhật mockup màn 06. |
| QĐ-04 | Cách tính SLA | **Theo lịch trực + tạm dừng.** Khoa trọng yếu (Cấp cứu, HSTC, PTGMHS…) tính **24/7**. Khoa khác tính theo **giờ hành chính** (cấu hình, mặc định 7:00–17:00, T2–T7, trừ ngày lễ trong bảng `NgayNghi`). Đồng hồ **tạm dừng** khi phiếu ở trạng thái *Chờ linh kiện* / *Gửi hãng*. | Thêm `LichLamViec` + `NgayNghi`, service `SlaCalculator` tính hạn và thời gian đã dùng; `PhieuSuaChua` lưu `ThoiGianTamDung` (cộng dồn) và `BatDauTamDung`. MTTR/% đúng SLA dùng thời gian hiệu dụng. Mockup Kanban/Chi tiết phiếu hiển thị "Đang tạm dừng". |
| QĐ-05 | Phiên đăng nhập | **Theo vai trò.** Nhân viên / Trưởng khoa: tự đăng xuất sau **30 phút** không thao tác, không cho "Ghi nhớ đăng nhập". Cán bộ CNTT / KTV / Quản trị / Lãnh đạo: **8 giờ** trượt theo hoạt động. | Cookie `ExpireTimeSpan` đặt theo vai trò khi đăng nhập (`OnSigningIn`), bỏ checkbox ghi nhớ với vai trò khoa; kiểm tra `DangHoatDong` khi đăng nhập. |

## Việc kỹ thuật kèm theo (từ Phụ lục A – ThietKeHeThong.md) — đã xử lý

- [x] Migration khởi tạo SQL Server `KhoiTao` (`src/ThietBiCNTT.Infrastructure/Data/Migrations`).
- [x] Số phiếu không trùng khi tạo đồng thời: unique index + sinh lại số và thử lại (`LuuSoPhieu.LuuMoiAsync`).
- [x] QR: tham số `DonVi.DiaChiTruyCap` thay cho `Request.Host` (chạy sau reverse proxy).
- [x] Dashboard giới hạn số liệu theo khoa với vai trò khoa; ẩn menu không có quyền.
- [x] Hiển thị số/ngày theo vi-VN (14.200.000 đ), đọc số trong form theo dấu chấm (input type=number).
- [x] Chặn điều chuyển tài sản đang nằm trong phiếu điều chuyển khác hoặc đang có phiếu sửa chữa mở; cập nhật vị trí khi bàn giao.

## Danh mục tham số hệ thống (Quản trị → Tham số hệ thống)

Mọi thông số nghiệp vụ ở trên đều **không gán cứng trong mã**: lưu ở bảng `ThamSoHeThongs`, Quản trị chỉnh tại màn hình *Tham số hệ thống* (có kiểm tra kiểu/khoảng giá trị, nút khôi phục mặc định, ghi nhật ký), hiệu lực ngay (cache 10 phút, xoá khi lưu). Ngày nghỉ lễ khai báo ở bảng `NgayNghis` cùng màn hình. Chỉ cấu hình kỹ thuật (chuỗi kết nối CSDL, nhà cung cấp CSDL) nằm trong `appsettings.json`.

| Nhóm | Mã | Tham số | Kiểu | Mặc định |
|---|---|---|---|---|
| Thông tin đơn vị | `DonVi.CoQuanChuQuan` | Cơ quan chủ quản | Văn bản | SỞ Y TẾ .................. |
| Thông tin đơn vị | `DonVi.TenBenhVien` | Tên bệnh viện | Văn bản | BỆNH VIỆN .................. |
| Thông tin đơn vị | `DonVi.TenPhongCNTT` | Tên đơn vị quản lý | Văn bản | PHÒNG CÔNG NGHỆ THÔNG TIN |
| Thông tin đơn vị | `DonVi.DiaChiTruyCap` | Địa chỉ truy cập hệ thống | Văn bản | (trống) |
| SLA sửa chữa | `Sla.GioKhanCap` | Hạn xử lý mức Khẩn cấp | Số thực | 2 giờ |
| SLA sửa chữa | `Sla.GioCao` | Hạn xử lý mức Cao | Số thực | 8 giờ |
| SLA sửa chữa | `Sla.GioTrungBinh` | Hạn xử lý mức Trung bình | Số thực | 24 giờ |
| SLA sửa chữa | `Sla.GioThap` | Hạn xử lý mức Thấp | Số thực | 72 giờ |
| SLA sửa chữa | `Sla.HeSoKhoaTrongYeu` | Hệ số rút ngắn cho khoa trọng yếu | Số thực | 0.5 × hạn |
| SLA sửa chữa | `Sla.KhoaTrongYeu24h` | Khoa trọng yếu tính SLA 24/7 | Bật/Tắt | true |
| SLA sửa chữa | `Sla.GioBatDau` | Giờ hành chính bắt đầu | Giờ | 07:00 |
| SLA sửa chữa | `Sla.GioKetThuc` | Giờ hành chính kết thúc | Giờ | 17:00 |
| SLA sửa chữa | `Sla.NgayLamViec` | Ngày làm việc trong tuần | Thứ trong tuần | 2,3,4,5,6,7 |
| SLA sửa chữa | `Sla.TamDungKhiCho` | Tạm dừng SLA khi chờ linh kiện / gửi hãng | Bật/Tắt | true |
| SLA sửa chữa | `Sla.NhacTruocPhut` | Nhắc KTV trước khi đến hạn | Số nguyên | 30 phút |
| Nghiệm thu | `NghiemThu.TuDongSauNgay` | Tự động nghiệm thu sau | Số nguyên | 3 ngày |
| Điều chuyển | `DieuChuyen.NguongDuyetLanhDao` | Ngưỡng giá trị cần Lãnh đạo duyệt | Số nguyên | 30000000 đ |
| Bảo trì định kỳ | `BaoTri.SinhLenhTruocNgay` | Sinh lệnh bảo trì trước ngày đến hạn | Số nguyên | 0 ngày |
| Bảo trì định kỳ | `BaoTri.HanHoanThanhNgay` | Thời hạn hoàn thành lệnh bảo trì | Số nguyên | 7 ngày |
| Bảo trì định kỳ | `BaoTri.ChuKyQuetGio` | Chu kỳ quét kế hoạch đến hạn | Số nguyên | 6 giờ |
| Bảo trì định kỳ | `BaoTri.CanhBaoDenHanNgay` | Hiển thị cảnh báo kế hoạch sắp đến hạn | Số nguyên | 7 ngày |
| Tài sản | `TaiSan.CanhBaoBaoHanhNgay` | Cảnh báo sắp hết bảo hành trước | Số nguyên | 60 ngày |
| Tài sản | `TaiSan.NguongChiPhiSuaPhanTram` | Ngưỡng chi phí sửa chữa / nguyên giá | Số nguyên | 50 % |
| Tài sản | `TaiSan.TuoiThoMacDinhNam` | Tuổi thọ mặc định khi tạo loại thiết bị | Số nguyên | 5 năm |
| Tài sản | `HopDong.CanhBaoTruocNgay` | Cảnh báo hợp đồng / bản quyền sắp hết hạn trước | Số nguyên | 60 ngày |
| Kho vật tư | `Kho.ChoXuatAm` | Cho phép xuất kho vượt tồn | Bật/Tắt | false |
| Mua sắm | `MuaSam.NguoiDuyetCNTT` | Người duyệt cấp 1 (Trưởng phòng CNTT) — tên đăng nhập, cách nhau dấu phẩy | Văn bản | admin |
| Mua sắm | `MuaSam.NguoiDuyetVatTu` | Người duyệt cấp 2 (Phòng Vật tư - TTB); trống = bỏ qua cấp | Văn bản | (trống) |
| Mua sắm | `MuaSam.NguongDuyetLanhDao` | Ngưỡng giá trị đề xuất / đơn hàng cần Lãnh đạo | Số nguyên | 50000000 đ |
| Mua sắm | `MuaSam.TuDongDeXuatTonThap` | Tự lập đề xuất nháp khi vật tư dưới định mức (hằng ngày) | Bật/Tắt | true |
| Ngân sách | `NganSach.NguongCanhBaoPhanTram` | Cảnh báo khi khoản mục đã dùng (cam kết + thực chi) đạt | Số nguyên | 80 % dự toán |
| Ngân sách | `NganSach.ChanVuotDuToan` | Chặn duyệt đề xuất vượt dự toán còn lại (tắt = chỉ cảnh báo) | Bật/Tắt | false |
| API & tích hợp | `Api.Bat` | Bật API mở /api/v1 | Bật/Tắt | true |
| API & tích hợp | `Api.GioiHanMoiPhut` | Giới hạn số lời gọi mỗi khoá | Số nguyên | 120 lần/phút |
| API & tích hợp | `Api.LuuNhatKyNgay` | Lưu nhật ký gọi API / webhook đã gửi | Số nguyên | 90 ngày |
| Agent cấu hình | `Agent.ChuanRamGb` | Chuẩn HIS: RAM tối thiểu (0 = không xét) | Số thực | 8 GB |
| Agent cấu hình | `Agent.ChuanOCungTrongGb` | Chuẩn HIS: ổ C còn trống tối thiểu | Số thực | 20 GB |
| Agent cấu hình | `Agent.ChuanHeDieuHanh` | Chuẩn HIS: hệ điều hành được phép | Văn bản | Windows 10, Windows 11 |
| Agent cấu hình | `Agent.ChuanPhanMem` | Chuẩn HIS: phần mềm bắt buộc | Văn bản | (trống) |
| Agent cấu hình | `Agent.CanhBaoKhongGuiNgay` | Cảnh báo máy không gửi cấu hình sau | Số nguyên | 7 ngày |
| Agent cấu hình | `Agent.GhiLichSuDoiIP` | Ghi lịch sử khi chỉ đổi IP | Bật/Tắt | false |
| Agent cấu hình | `Agent.TuGhepTheoHostname` | Tự ghép theo hostname / MAC | Bật/Tắt | true |
| Ngân sách | `NganSach.BatBuocKhoanMuc` | Bắt buộc chọn khoản mục ngân sách khi gửi duyệt đề xuất | Bật/Tắt | false |
| Thanh lý | `ThanhLy.BatBuocThaoOCung` | Bắt buộc tháo ổ cứng trước khi hoàn tất | Bật/Tắt | true |
| Bảo mật | `BaoMat.PhienKhoaPhut` | Tự đăng xuất (Nhân viên, Trưởng khoa) sau | Số nguyên | 30 phút không thao tác |
| Bảo mật | `BaoMat.PhienCNTTGio` | Thời gian giữ phiên (CNTT, KTV, Lãnh đạo) | Số nguyên | 8 giờ |
| Bảo mật | `BaoMat.ChoGhiNhoVoiKhoa` | Cho 'Ghi nhớ đăng nhập' với tài khoản khoa | Bật/Tắt | false |
| Thông báo | `ThongBao.EmailBat` | Gửi thông báo qua email | Bật/Tắt | false |
| Thông báo | `ThongBao.SmtpHost` | Máy chủ SMTP | Văn bản | (trống) |
| Thông báo | `ThongBao.SmtpPort` | Cổng SMTP | Số nguyên | 587 |
| Thông báo | `ThongBao.SmtpBaoMat` | Mã hoá kết nối SMTP (TLS) | Bật/Tắt | true |
| Thông báo | `ThongBao.SmtpUser` | Tài khoản đăng nhập SMTP | Văn bản | (trống) |
| Thông báo | `ThongBao.EmailGui` | Địa chỉ email gửi đi | Văn bản | (trống) |
| Thông báo | `ThongBao.TelegramBat` | Gửi thông báo qua Telegram | Bật/Tắt | false |
| Thông báo | `ThongBao.TelegramNhomCNTT` | Chat ID nhóm Telegram Phòng CNTT | Văn bản | (trống) |
| Thông báo | `ThongBao.GioTongHop` | Giờ gửi bản tin tổng hợp hằng ngày | Giờ | 07:30 |
| Thông báo | `LeoThang.Cap2SauPhut` | Leo thang cấp 2 sau khi quá hạn | Số nguyên | 60 phút |
| Thông báo | `LeoThang.Cap2NguoiNhan` | Người nhận leo thang cấp 2 | Văn bản | admin |
| Thông báo | `LeoThang.Cap3SauPhut` | Leo thang cấp 3 (Lãnh đạo) sau khi quá hạn | Số nguyên | 240 phút |

Thêm tham số mới: khai báo mã trong lớp `TS` và một dòng trong `ThamSoMacDinh.DanhSach()` (`ThietBiCNTT.Infrastructure/Services/ThamSoService.cs`); lần khởi động sau hệ thống tự thêm vào CSDL, giữ nguyên các giá trị đã chỉnh.
