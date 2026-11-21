# Quyết định nghiệp vụ (chốt ngày 21/11/2026)

Các quyết định dưới đây **thay thế** nội dung tương ứng trong `SRS.md` v1.1 và `ThietKeHeThong.md` (Phụ lục A). Sẽ được đưa vào SRS v1.2 và áp dụng khi hoàn thiện mã nguồn.

| # | Chủ đề | Quyết định | Ảnh hưởng thiết kế / mã nguồn |
|---|---|---|---|
| QĐ-01 | Nghiệm thu phiếu sửa chữa | Chỉ **người báo hỏng** hoặc **Trưởng khoa/phòng** của khoa sở hữu thiết bị được nghiệm thu / trả lại. KTV xử lý phiếu **không** được tự nghiệm thu. Phiếu "Hoàn thành" quá **N ngày** (cấu hình, mặc định 3) không phản hồi → hệ thống tự đóng, ghi "Tự động nghiệm thu". | `PhieuSuaChuaController.NghiemThu/TraLai`: kiểm tra `NguoiTao == user` hoặc vai trò `TruongKhoa` + cùng `KhoaPhongId`; chặn `KyThuatVien == user`. Thêm job nền tự đóng phiếu. |
| QĐ-02 | Duyệt điều chuyển | **Theo giá trị tài sản.** Tổng nguyên giá phiếu < ngưỡng (cấu hình, mặc định **30 triệu đ**): 2 cấp **Trưởng khoa đi → Phòng CNTT**. ≥ ngưỡng: thêm cấp 3 **Lãnh đạo**. Người lập phiếu không được tự duyệt. | `PhieuDieuChuyen`: thêm bảng bước duyệt (`BuocDuyet`: cấp, vai trò, người, thời điểm, ý kiến), trạng thái `ChoDuyetKhoa → ChoDuyetCNTT → ChoDuyetLanhDao → DaDuyet`. Chặn tài sản đang nằm trong phiếu điều chuyển khác / đang có phiếu sửa chữa mở. Cập nhật cả `ViTriId` khi bàn giao. |
| QĐ-03 | Bảo mật dữ liệu khi thanh lý | **Tháo ổ cứng giữ lại.** Với thiết bị có lưu trữ, trước khi bàn giao máy cho đơn vị mua/huỷ, Phòng CNTT tháo ổ lưu trữ, ghi nhận **serial ổ**, người tháo, thời điểm, nơi lưu (kho CNTT) và hình thức xử lý sau đó (lưu kho / huỷ vật lý có biên bản). Chặn bước "Hoàn tất & ghi giảm" nếu còn thiết bị chưa ghi nhận tháo ổ. | `ChiTietThanhLy`: thay `DaXoaDuLieu/XacNhanXoaDuLieu` bằng `DaThaoOCung`, `SerialOCung`, `NguoiThao`, `NgayThao`, `NoiLuuOCung`, `XuLyOCung` (LuuKho/HuyVatLy), `BienBanHuyOCung`. Thêm danh sách "Ổ cứng thu hồi" để theo dõi đến khi huỷ. Cập nhật mockup màn 06. |
| QĐ-04 | Cách tính SLA | **Theo lịch trực + tạm dừng.** Khoa trọng yếu (Cấp cứu, HSTC, PTGMHS…) tính **24/7**. Khoa khác tính theo **giờ hành chính** (cấu hình, mặc định 7:00–17:00, T2–T7, trừ ngày lễ trong bảng `NgayNghi`). Đồng hồ **tạm dừng** khi phiếu ở trạng thái *Chờ linh kiện* / *Gửi hãng*. | Thêm `LichLamViec` + `NgayNghi`, service `SlaCalculator` tính hạn và thời gian đã dùng; `PhieuSuaChua` lưu `ThoiGianTamDung` (cộng dồn) và `BatDauTamDung`. MTTR/% đúng SLA dùng thời gian hiệu dụng. Mockup Kanban/Chi tiết phiếu hiển thị "Đang tạm dừng". |
| QĐ-05 | Phiên đăng nhập | **Theo vai trò.** Nhân viên / Trưởng khoa: tự đăng xuất sau **30 phút** không thao tác, không cho "Ghi nhớ đăng nhập". Cán bộ CNTT / KTV / Quản trị / Lãnh đạo: **8 giờ** trượt theo hoạt động. | Cookie `ExpireTimeSpan` đặt theo vai trò khi đăng nhập (`OnSigningIn`), bỏ checkbox ghi nhớ với vai trò khoa; kiểm tra `DangHoatDong` khi đăng nhập. |

## Việc kỹ thuật kèm theo (từ Phụ lục A – ThietKeHeThong.md)

- Tạo migration khởi tạo cho SQL Server (`KhoiTao`).
- Số phiếu: dùng `SEQUENCE` SQL Server / khoá ghi để không trùng khi tạo đồng thời.
- QR: cấu hình `App:PublicBaseUrl` thay vì `Request.Host` (chạy sau reverse proxy).
- Dashboard: giới hạn số liệu theo khoa với vai trò khoa; ẩn menu không có quyền.
