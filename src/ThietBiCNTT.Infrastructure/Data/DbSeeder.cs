using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, UserManager<AppUser> users, RoleManager<IdentityRole> roles, bool duLieuMau, string? matKhauAdmin = null)
    {
        await Services.ThamSoService.DongBoAsync(db);
        foreach (var r in VaiTro.TatCa)
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

        if (!await db.KhoaPhongs.AnyAsync())
        {
            db.KhoaPhongs.AddRange(
                new KhoaPhong { Ma = "CNTT", Ten = "Phòng Công nghệ thông tin" },
                new KhoaPhong { Ma = "KHTH", Ten = "Phòng Kế hoạch tổng hợp" },
                new KhoaPhong { Ma = "TCKT", Ten = "Phòng Tài chính kế toán" },
                new KhoaPhong { Ma = "VTTB", Ten = "Phòng Vật tư thiết bị y tế" },
                new KhoaPhong { Ma = "TCCB", Ten = "Phòng Tổ chức cán bộ" },
                new KhoaPhong { Ma = "KB", Ten = "Khoa Khám bệnh" },
                new KhoaPhong { Ma = "CC", Ten = "Khoa Cấp cứu", LaKhoaTrongYeu = true },
                new KhoaPhong { Ma = "HSTC", Ten = "Khoa Hồi sức tích cực - Chống độc", LaKhoaTrongYeu = true },
                new KhoaPhong { Ma = "PTGM", Ten = "Khoa Phẫu thuật - Gây mê hồi sức", LaKhoaTrongYeu = true },
                new KhoaPhong { Ma = "NOI", Ten = "Khoa Nội tổng hợp" },
                new KhoaPhong { Ma = "NGOAI", Ten = "Khoa Ngoại tổng hợp" },
                new KhoaPhong { Ma = "SAN", Ten = "Khoa Phụ sản" },
                new KhoaPhong { Ma = "NHI", Ten = "Khoa Nhi" },
                new KhoaPhong { Ma = "XN", Ten = "Khoa Xét nghiệm" },
                new KhoaPhong { Ma = "CDHA", Ten = "Khoa Chẩn đoán hình ảnh" },
                new KhoaPhong { Ma = "DUOC", Ten = "Khoa Dược" });
            db.LoaiThietBis.AddRange(
                new LoaiThietBi { Ma = "PC", Ten = "Máy tính để bàn", Nhom = NhomThietBi.MayTinhDeBan, CoLuuTruDuLieu = true, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "LT", Ten = "Máy tính xách tay", Nhom = NhomThietBi.MayTinhXachTay, CoLuuTruDuLieu = true, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "SV", Ten = "Máy chủ", Nhom = NhomThietBi.MayChu, CoLuuTruDuLieu = true, TuoiThoNam = 7 },
                new LoaiThietBi { Ma = "PRN", Ten = "Máy in laser", Nhom = NhomThietBi.MayIn, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "SCN", Ten = "Máy scan", Nhom = NhomThietBi.MayIn, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "SW", Ten = "Switch mạng", Nhom = NhomThietBi.ThietBiMang, TuoiThoNam = 7 },
                new LoaiThietBi { Ma = "AP", Ten = "Wifi Access Point", Nhom = NhomThietBi.ThietBiMang, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "MH", Ten = "Màn hình", Nhom = NhomThietBi.ManHinh, TuoiThoNam = 6 },
                new LoaiThietBi { Ma = "UPS", Ten = "Bộ lưu điện UPS", Nhom = NhomThietBi.Ups, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "CAM", Ten = "Camera IP", Nhom = NhomThietBi.Camera, TuoiThoNam = 5 },
                new LoaiThietBi { Ma = "BCR", Ten = "Máy đọc mã vạch", Nhom = NhomThietBi.NgoaiVi, TuoiThoNam = 4 });
            db.NhaCungCaps.AddRange(
                new NhaCungCap { Ten = "Công ty TNHH Tin học Minh Phát", MaSoThue = "0301234567", DienThoai = "028 3838 1111", NguoiLienHe = "Anh Tuấn" },
                new NhaCungCap { Ten = "Trung tâm bảo hành Dell Việt Nam", DienThoai = "1800 545 455", LaDonViBaoHanh = true },
                new NhaCungCap { Ten = "Trung tâm bảo hành HP Việt Nam", DienThoai = "1800 588 868", LaDonViBaoHanh = true },
                new NhaCungCap { Ten = "Công ty CP Giải pháp mạng An Khang", MaSoThue = "0109876543", DienThoai = "024 3333 2222", NguoiLienHe = "Chị Lan" });
            db.VatTus.AddRange(
                new VatTu { Ma = "RAM-D4-8G", Ten = "RAM DDR4 8GB 3200", DonViTinh = "Thanh", TonKho = 12, TonToiThieu = 5, DonGia = 450_000 },
                new VatTu { Ma = "SSD-256", Ten = "SSD 256GB SATA", DonViTinh = "Cái", TonKho = 4, TonToiThieu = 5, DonGia = 650_000 },
                new VatTu { Ma = "PSU-450", Ten = "Nguồn máy tính 450W", DonViTinh = "Cái", TonKho = 3, TonToiThieu = 2, DonGia = 550_000 },
                new VatTu { Ma = "MUC-HP26A", Ten = "Hộp mực HP 26A", DonViTinh = "Hộp", TonKho = 20, TonToiThieu = 10, DonGia = 380_000 },
                new VatTu { Ma = "TRONG-HP", Ten = "Trống máy in HP", DonViTinh = "Cái", TonKho = 1, TonToiThieu = 3, DonGia = 290_000 },
                new VatTu { Ma = "BAT-UPS", Ten = "Ắc quy UPS 12V-9Ah", DonViTinh = "Bình", TonKho = 8, TonToiThieu = 4, DonGia = 420_000 },
                new VatTu { Ma = "KB-USB", Ten = "Bàn phím USB", DonViTinh = "Cái", TonKho = 15, TonToiThieu = 5, DonGia = 120_000 },
                new VatTu { Ma = "MS-USB", Ten = "Chuột USB", DonViTinh = "Cái", TonKho = 18, TonToiThieu = 5, DonGia = 80_000 });
            await db.SaveChangesAsync();
        }

        var cntt = await db.KhoaPhongs.FirstAsync(k => k.Ma == "CNTT");
        var cc = await db.KhoaPhongs.FirstAsync(k => k.Ma == "CC");
        // Mật khẩu admin lần đầu: lấy từ cấu hình Seed:AdminPassword (chỉ dùng khi tài khoản chưa tồn tại).
        await TaoUser(users, "admin", "Quản trị hệ thống", cntt.Id, VaiTro.QuanTri, matKhauAdmin);
        if (duLieuMau)
        {
            await TaoUser(users, "ktv1", "Nguyễn Văn An (KTV)", cntt.Id, VaiTro.CanBoCNTT);
            await TaoUser(users, "ktv2", "Trần Minh Bảo (KTV)", cntt.Id, VaiTro.CanBoCNTT);
            await TaoUser(users, "truongkhoacc", "BS. Lê Thị Cúc", cc.Id, VaiTro.TruongKhoa);
            await TaoUser(users, "lanhdao", "Phó Giám đốc", null, VaiTro.LanhDao);
            await TaoUser(users, "nhanvien", "ĐD. Phạm Thu Hà", cc.Id, VaiTro.NhanVien);
            await TaoDuLieuMau(db);
        }
    }

    private static async Task TaoUser(UserManager<AppUser> users, string userName, string hoTen, int? khoaId, string role, string? matKhau = null)
    {
        if (await users.FindByNameAsync(userName) is not null) return;
        var u = new AppUser { UserName = userName, Email = $"{userName}@benhvien.local", EmailConfirmed = true, HoTen = hoTen, KhoaPhongId = khoaId };
        // Mật khẩu mặc định — BẮT BUỘC đổi khi triển khai thật.
        var rs = await users.CreateAsync(u, string.IsNullOrWhiteSpace(matKhau) ? "Abc@12345" : matKhau);
        if (!rs.Succeeded) throw new InvalidOperationException($"Không tạo được tài khoản {userName}: " + string.Join("; ", rs.Errors.Select(e => e.Description)));
        await users.AddToRoleAsync(u, role);
    }

    private static async Task TaoDuLieuMau(AppDbContext db)
    {
        if (await db.TaiSans.AnyAsync()) return;
        var khoa = await db.KhoaPhongs.ToDictionaryAsync(k => k.Ma);
        var loai = await db.LoaiThietBis.ToDictionaryAsync(l => l.Ma);
        var ncc = await db.NhaCungCaps.ToListAsync();
        var rnd = new Random(42);
        var homNay = DateOnly.FromDateTime(DateTime.Today);
        var dsKhoa = khoa.Values.ToList();
        var mau = new (string Loai, string Ten, string Hang, string Model, decimal Gia)[]
        {
            ("PC", "Máy tính Dell OptiPlex 7010", "Dell", "OptiPlex 7010", 15_500_000),
            ("PC", "Máy tính HP ProDesk 400 G7", "HP", "ProDesk 400 G7", 13_900_000),
            ("PC", "Máy tính Lenovo ThinkCentre M70s", "Lenovo", "M70s", 14_200_000),
            ("LT", "Laptop Dell Latitude 3440", "Dell", "Latitude 3440", 18_900_000),
            ("PRN", "Máy in HP LaserJet Pro M404dn", "HP", "M404dn", 7_800_000),
            ("PRN", "Máy in Canon LBP 2900", "Canon", "LBP 2900", 3_900_000),
            ("MH", "Màn hình Dell 22 inch", "Dell", "E2223HN", 2_900_000),
            ("UPS", "UPS Santak 1000VA", "Santak", "Blazer 1000", 2_400_000),
            ("BCR", "Máy đọc mã vạch Zebra", "Zebra", "DS2208", 2_100_000),
        };
        var dsTs = new List<TaiSan>();
        var stt = new Dictionary<string, int>();
        for (var i = 0; i < 60; i++)
        {
            // 3 máy đầu: máy tính Khoa Cấp cứu (để có dữ liệu cho phiếu kiểm tra đầu ca)
            var m = i < 3 ? mau[0] : mau[rnd.Next(mau.Length)];
            var k = i < 3 ? khoa["CC"] : dsKhoa[rnd.Next(dsKhoa.Count)];
            stt[m.Loai] = stt.GetValueOrDefault(m.Loai) + 1;
            var ngayMua = homNay.AddDays(-rnd.Next(60, 365 * 8));
            var ts = new TaiSan
            {
                MaTaiSan = $"{m.Loai}-{stt[m.Loai]:D4}", Ten = m.Ten, LoaiThietBiId = loai[m.Loai].Id,
                Hang = m.Hang, Model = m.Model, SoSerial = $"SN{rnd.Next(10_000_000, 99_999_999)}",
                NguyenGia = m.Gia, NgayMua = ngayMua, NgaySuDung = ngayMua.AddDays(7),
                HanBaoHanh = ngayMua.AddYears(3), NhaCungCapId = ncc[rnd.Next(ncc.Count)].Id,
                KhoaPhongId = k.Id, TrangThai = rnd.Next(10) switch { 0 => TrangThaiTaiSan.Hong, 1 => TrangThaiTaiSan.DangSuaChua, _ => TrangThaiTaiSan.DangSuDung },
                NguonVon = NguonVon.NganSach,
            };
            if (m.Loai is "PC" or "LT")
                ts.CauHinh = new CauHinh
                {
                    CPU = rnd.Next(2) == 0 ? "Intel Core i5-12500" : "Intel Core i3-10100",
                    RAM = rnd.Next(2) == 0 ? "8GB DDR4" : "16GB DDR4",
                    OCung = "SSD 256GB", HeDieuHanh = "Windows 11 Pro", BanQuyen = "Windows OEM",
                    DiaChiIP = $"10.10.{rnd.Next(1, 20)}.{rnd.Next(2, 250)}",
                    DiaChiMAC = string.Join(":", Enumerable.Range(0, 6).Select(_ => rnd.Next(256).ToString("X2"))),
                    Hostname = $"{k.Ma}-{m.Loai}{stt[m.Loai]:D2}", PhanMem = "HIS client, Office 2021, Unikey",
                    NgayCapNhat = DateTime.Now
                };
            if (m.Loai == "PRN") ts.ChiSoBoDem = rnd.Next(5_000, 120_000);
            dsTs.Add(ts);
        }
        db.TaiSans.AddRange(dsTs);
        await db.SaveChangesAsync();
        db.LichSuTaiSans.AddRange(dsTs.Select(t => new LichSuTaiSan
            { TaiSanId = t.Id, LoaiSuKien = LoaiSuKien.TaoMoi, NoiDung = "Nhập dữ liệu ban đầu", ThoiGian = DateTime.Now, NguoiThucHien = "system" }));

        var loi = new[] { "Máy không lên nguồn", "Máy chạy rất chậm, treo khi mở HIS", "Máy in kẹt giấy liên tục",
                          "Không vào được mạng", "Màn hình bị sọc", "Máy in in mờ", "UPS kêu bíp liên tục" };
        var now = DateTime.Now;
        var n = 0;
        foreach (var t in dsTs.Where(t => t.TrangThai != TrangThaiTaiSan.DangSuDung).Concat(dsTs.Take(25)))
        {
            n++;
            // Phiếu còn mở (thiết bị hỏng/đang sửa) là phiếu gần đây; phiếu đã xong rải đều 11 tháng.
            var ngay = t.TrangThai == TrangThaiTaiSan.DangSuDung
                ? now.AddDays(-rnd.Next(1, 330)).AddHours(-rnd.Next(0, 10))
                : now.AddHours(-rnd.Next(1, 30));
            var uuTien = (MucDoUuTien)rnd.Next(1, 5);
            var trongYeu = dsKhoa.First(k => k.Id == t.KhoaPhongId).LaKhoaTrongYeu;
            var p = new PhieuSuaChua
            {
                SoPhieu = $"SC-{ngay:yyMM}-{n:D4}", TaiSanId = t.Id, KhoaPhongId = t.KhoaPhongId,
                NguoiYeuCau = "Điều dưỡng trưởng", MoTaSuCo = loi[rnd.Next(loi.Length)], UuTien = uuTien,
                NgayYeuCau = ngay, HanXuLy = ThietBiCNTT.Domain.Common.SlaCalculator.TinhHan(ngay, uuTien, trongYeu, SlaThamSo.MacDinh),
                NguoiTao = "nhanvien",
            };
            if (t.TrangThai == TrangThaiTaiSan.DangSuDung)
            {
                p.TiepNhan("ktv1", HinhThucXuLy.TuSua, ngay.AddMinutes(rnd.Next(10, 120)));
                p.HoanThanh("Linh kiện hỏng / lỗi phần mềm", "Đã thay thế linh kiện, cài đặt lại", rnd.Next(1, 4), 0, ngay.AddHours(rnd.Next(1, 30)));
                if (rnd.Next(3) > 0) p.NghiemThu("Điều dưỡng trưởng", rnd.Next(3, 6), null, p.NgayHoanThanh!.Value.AddHours(2));
            }
            else if (t.TrangThai == TrangThaiTaiSan.DangSuaChua)
                p.TiepNhan("ktv2", HinhThucXuLy.TuSua, ngay.AddMinutes(30));
            db.PhieuSuaChuas.Add(p);
        }
        db.KeHoachBaoTris.AddRange(
            new KeHoachBaoTri { Ten = "Vệ sinh máy tính định kỳ 6 tháng", LoaiThietBiId = loai["PC"].Id, ChuKyNgay = 180, NgayTiepTheo = homNay.AddDays(5),
                Checklist = "Vệ sinh bụi, tra keo tản nhiệt\nKiểm tra quạt, nguồn\nCập nhật Windows, diệt virus\nKiểm tra dung lượng ổ cứng\nKiểm tra kết nối HIS", KyThuatVien = "ktv1" },
            new KeHoachBaoTri { Ten = "Bảo dưỡng máy in 3 tháng", LoaiThietBiId = loai["PRN"].Id, ChuKyNgay = 90, NgayTiepTheo = homNay.AddDays(-2),
                Checklist = "Vệ sinh trục kéo giấy\nKiểm tra trống, lô sấy\nIn trang thử", KyThuatVien = "ktv2" },
            new KeHoachBaoTri { Ten = "Kiểm tra ắc quy UPS hằng năm", LoaiThietBiId = loai["UPS"].Id, ChuKyNgay = 365, NgayTiepTheo = homNay.AddDays(30),
                Checklist = "Đo điện áp ắc quy\nTest chuyển nguồn\nGhi nhận thời gian lưu điện", KyThuatVien = "ktv1" });
        await db.SaveChangesAsync();
        await TaoMauKiemTraMau(db, loai, khoa);
    }

    private static async Task TaoMauKiemTraMau(AppDbContext db, Dictionary<string, LoaiThietBi> loai, Dictionary<string, KhoaPhong> khoa)
    {
        if (await db.MauKiemTras.AnyAsync()) return;
        static MucKiemTra Dk(int i, string nd) => new() { ThuTu = i, NoiDung = nd, Kieu = KieuMucKiemTra.DatKhongDat };
        static MucKiemTra So(int i, string nd, string dv, decimal? min, decimal? max) => new() { ThuTu = i, NoiDung = nd, Kieu = KieuMucKiemTra.So, DonVi = dv, NguongMin = min, NguongMax = max };
        db.MauKiemTras.AddRange(
            new MauKiemTra { Ten = "Kiểm tra phòng máy chủ hằng ngày", ViTri = "Phòng máy chủ – tầng 3 nhà A", KhoaPhongId = khoa["CNTT"].Id, ChuKyGio = 24,
                MoTa = "Thực hiện đầu giờ sáng. Nhiệt độ/độ ẩm đọc trên nhiệt kế treo tường.",
                Mucs = [So(1, "Nhiệt độ phòng", "°C", 18, 27), So(2, "Độ ẩm", "%", 40, 60), Dk(3, "Điều hoà hoạt động bình thường"),
                        Dk(4, "UPS không báo lỗi / không chạy bằng ắc quy"), Dk(5, "Không có đèn báo lỗi trên máy chủ, thiết bị lưu trữ"),
                        Dk(6, "Bản sao lưu CSDL đêm qua thành công"), new MucKiemTra { ThuTu = 7, NoiDung = "Ghi chú khác", Kieu = KieuMucKiemTra.VanBan, BatBuoc = false }] },
            new MauKiemTra { Ten = "Kiểm tra UPS hằng tuần", LoaiThietBiId = loai["UPS"].Id, ChuKyGio = 168,
                Mucs = [So(1, "Thời gian lưu điện ước tính", "phút", 10, null), So(2, "Tải hiện tại", "%", null, 80), Dk(3, "Không có đèn/còi cảnh báo")] },
            new MauKiemTra { Ten = "Kiểm tra máy tính đầu ca – Khoa Cấp cứu", LoaiThietBiId = loai["PC"].Id, KhoaPhongId = khoa["CC"].Id, ChuKyGio = 8,
                MoTa = "Điều dưỡng trực kiểm tra đầu mỗi ca.",
                Mucs = [Dk(1, "Khởi động & đăng nhập HIS được"), Dk(2, "Máy in phiếu / mã vạch hoạt động"), Dk(3, "Kết nối mạng ổn định")] });
        await db.SaveChangesAsync();
    }
}
