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
        await Services.DanhMucJci.DongBoAsync(db);
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
                new KhoaPhong { Ma = "HCQT", Ten = "Phòng Hành chính quản trị" },
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
            var hcqt = await db.KhoaPhongs.FirstAsync(k => k.Ma == "HCQT");
            await TaoUser(users, "truongkhoahcqt", "Trưởng phòng Hành chính quản trị", hcqt.Id, VaiTro.TruongKhoa);
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
        // HCQT (quản lý UPS/nguồn điện phòng máy chủ) không nhận máy tính mẫu — giữ nguyên phân bố dữ liệu mẫu cũ.
        var dsKhoa = khoa.Values.Where(k => k.Ma != "HCQT").ToList();
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
        await TaoHaTangMauAsync(db, loai, khoa);
        await TaoGD32MauAsync(db, khoa);
        await TaoGD33MauAsync(db, khoa);
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
                        Dk(6, "Bản sao lưu CSDL đêm qua thành công"),
                        Dk(7, "Hệ thống chữa cháy phòng máy chủ bình thường, không báo lỗi (FMS.06.01)"), Dk(8, "Các nút ngắt điện khẩn cấp còn nhãn rõ ràng (FMS.08.00 ME5)"),
                        new MucKiemTra { ThuTu = 9, NoiDung = "Ghi chú khác", Kieu = KieuMucKiemTra.VanBan, BatBuoc = false }] },
            new MauKiemTra { Ten = "Thử UPS phòng máy chủ hằng quý", ViTri = "Phòng máy chủ – UPS", KhoaPhongId = khoa["CNTT"].Id, ChuKyGio = 2160, LaThuNguonDuPhong = true,
                MoTa = "FMS.08.01 ME3: thử nguồn thay thế ít nhất mỗi quý và lưu kết quả. Phòng CNTT quản lý và thử UPS: ngắt nguồn lưới có kiểm soát, đo thời gian lưu điện và chuyển nguồn.",
                Mucs = [So(1, "Thời gian chạy bằng ắc quy khi ngắt lưới", "phút", 10, null), So(2, "Tỷ lệ tải so với công suất danh định", "%", 30, null), So(3, "Thời gian chuyển nguồn", "mili giây", null, 10),
                        Dk(4, "Máy chủ không tắt/khởi động lại khi chuyển nguồn"), Dk(5, "Ắc quy không phồng, rò dịch; điện áp từng bình trong ngưỡng"), new MucKiemTra { ThuTu = 6, NoiDung = "Nhận xét", Kieu = KieuMucKiemTra.VanBan, BatBuoc = false }] },
            new MauKiemTra { Ten = "Thử máy phát điện dự phòng hằng quý", ViTri = "Khu kỹ thuật – máy phát điện", KhoaPhongId = khoa["HCQT"].Id, ChuKyGio = 2160, LaThuNguonDuPhong = true,
                MoTa = "FMS.08.01 ME3: Phòng Hành chính quản trị quản lý và thử máy phát. Chạy tải ≥ 30 phút ở ≥ 30% công suất danh định (không tính khởi động/làm nguội). Phòng CNTT nhận kết quả để giám sát cấp điện cho phòng máy chủ.",
                Mucs = [So(1, "Thời gian chạy tải", "phút", 30, null), So(2, "Tỷ lệ tải so với công suất danh định", "%", 30, null), So(3, "Thời gian từ mất lưới đến cấp điện trở lại", "giây", null, 30),
                        Dk(4, "Máy phát khởi động và nhận tải bình thường"), Dk(5, "Phòng máy chủ nhận điện từ máy phát khi mất lưới"), So(6, "Nhiên liệu dự trữ", "giờ chạy", 8, null),
                        new MucKiemTra { ThuTu = 7, NoiDung = "Nhận xét", Kieu = KieuMucKiemTra.VanBan, BatBuoc = false }] },
            new MauKiemTra { Ten = "Kiểm tra UPS hằng tuần", LoaiThietBiId = loai["UPS"].Id, ChuKyGio = 168,
                Mucs = [So(1, "Thời gian lưu điện ước tính", "phút", 10, null), So(2, "Tải hiện tại", "%", null, 80), Dk(3, "Không có đèn/còi cảnh báo")] },
            new MauKiemTra { Ten = "Kiểm tra máy tính đầu ca – Khoa Cấp cứu", LoaiThietBiId = loai["PC"].Id, KhoaPhongId = khoa["CC"].Id, ChuKyGio = 8,
                MoTa = "Điều dưỡng trực kiểm tra đầu mỗi ca.",
                Mucs = [Dk(1, "Khởi động & đăng nhập HIS được"), Dk(2, "Máy in phiếu / mã vạch hoạt động"), Dk(3, "Kết nối mạng ổn định")] });
        await db.SaveChangesAsync();
    }

    /// <summary>Dữ liệu mẫu phân hệ hạ tầng CNTT (GĐ3.1): hệ thống thông tin, phụ thuộc, kế hoạch downtime, sao lưu, sự kiện, hành động.</summary>
    private static async Task TaoHaTangMauAsync(AppDbContext db, Dictionary<string, LoaiThietBi> loai, Dictionary<string, KhoaPhong> khoa)
    {
        if (await db.HeThongThongTins.AnyAsync()) return;
        var now = DateTime.Now; var homNay = DateOnly.FromDateTime(now);
        TaiSan Ts(string ma, string ten, string l, string hang) => new() { MaTaiSan = ma, Ten = ten, LoaiThietBiId = loai[l].Id, Hang = hang, NguyenGia = 180_000_000, NgayMua = homNay.AddYears(-3),
            NgaySuDung = homNay.AddYears(-3), HanBaoHanh = homNay.AddMonths(8), KhoaPhongId = khoa["CNTT"].Id, TrangThai = TrangThaiTaiSan.DangSuDung, NguonVon = NguonVon.NganSach };
        var sv1 = Ts("SV-0001", "Máy chủ ứng dụng HIS", "SV", "Dell"); var sv2 = Ts("SV-0002", "Máy chủ CSDL HIS", "SV", "Dell"); var sv3 = Ts("SV-0003", "Máy chủ LIS", "SV", "HP");
        var sv4 = Ts("SV-0004", "Máy chủ PACS", "SV", "HP"); var sw1 = Ts("SW-0001", "Switch lõi tầng 3", "SW", "Cisco"); var ups1 = Ts("UPS-0101", "UPS phòng máy chủ 20kVA", "UPS", "APC");
        db.TaiSans.AddRange(sv1, sv2, sv3, sv4, sw1, ups1);
        await db.SaveChangesAsync();
        db.LichSuTaiSans.AddRange(new[] { sv1, sv2, sv3, sv4, sw1, ups1 }.Select(t => new LichSuTaiSan { TaiSanId = t.Id, LoaiSuKien = LoaiSuKien.TaoMoi, NoiDung = "Nhập dữ liệu ban đầu", ThoiGian = now, NguoiThucHien = "system" }));

        HeThongThongTin Ht(string ma, string ten, LoaiHeThong l, MucTrongYeu m, string? chuSoHuu, int? khoaChu) => new()
        {
            Ma = ma, Ten = ten, Loai = l, MucTrongYeu = m, TrangThai = TrangThaiHeThong.VanHanh, PhienBan = "1.0", NgaySuDung = homNay.AddYears(-2), ChuSoHuu = chuSoHuu, ChuSoHuuKhoaPhongId = khoaChu,
            PhuTrachChinh = "ktv1", PhuTrachDuPhong = "ktv2", NguoiTao = "system", HoTroNhaCungCap = "Kỹ sư hỗ trợ – 0900 000 000 – 24/7"
        };
        var his = Ht("HIS", "Hệ thống thông tin bệnh viện (HIS)", LoaiHeThong.LamSang, MucTrongYeu.ToiQuanTrong, "Phó Giám đốc phụ trách khám chữa bệnh", khoa["KB"].Id);
        var lis = Ht("LIS", "Hệ thống thông tin xét nghiệm (LIS)", LoaiHeThong.CanLamSang, MucTrongYeu.ToiQuanTrong, "Trưởng khoa Xét nghiệm", khoa["XN"].Id);
        var pacs = Ht("PACS", "Lưu trữ và truyền hình ảnh (PACS)", LoaiHeThong.CanLamSang, MucTrongYeu.Cao, "Trưởng khoa CĐHA", khoa["CDHA"].Id);
        var mail = Ht("EMAIL", "Thư điện tử nội bộ", LoaiHeThong.HaTangDungChung, MucTrongYeu.TrungBinh, "Trưởng phòng CNTT", khoa["CNTT"].Id);
        pacs.PhuTrachChinh = "ktv2"; pacs.PhuTrachDuPhong = "ktv1"; mail.PhuTrachDuPhong = null;
        db.HeThongThongTins.AddRange(his, lis, pacs, mail);
        await db.SaveChangesAsync();
        void Khoa(HeThongThongTin h, params string[] ma) { foreach (var m in ma) db.HeThongKhoaPhongs.Add(new HeThongKhoaPhong { HeThongId = h.Id, KhoaPhongId = khoa[m].Id, BatBuoc = true }); }
        Khoa(his, "KB", "CC", "NOI", "NGOAI", "SAN", "NHI", "DUOC", "TCKT"); Khoa(lis, "XN", "CC", "KB"); Khoa(pacs, "CDHA", "CC", "NGOAI"); Khoa(mail, "CNTT", "KHTH");
        db.PhuThuocHeThongs.AddRange(
            new PhuThuocHeThong { HeThongId = his.Id, TaiSanId = sv1.Id, VaiTro = "Máy chủ ứng dụng", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = his.Id, TaiSanId = sv2.Id, VaiTro = "Máy chủ CSDL", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = his.Id, TaiSanId = sw1.Id, VaiTro = "Switch lõi", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = his.Id, TaiSanId = ups1.Id, VaiTro = "Nguồn điện phòng máy chủ", CoDuPhong = true },
            new PhuThuocHeThong { HeThongId = lis.Id, HeThongPhuThuocId = his.Id, VaiTro = "Nhận chỉ định từ HIS", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = lis.Id, TaiSanId = sv3.Id, VaiTro = "Máy chủ LIS", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = pacs.Id, TaiSanId = sv4.Id, VaiTro = "Máy chủ PACS", CoDuPhong = false },
            new PhuThuocHeThong { HeThongId = pacs.Id, TaiSanId = sw1.Id, VaiTro = "Switch lõi", CoDuPhong = false });
        // Kế hoạch downtime: HIS hiệu lực (đã duyệt đủ cấp); PACS còn nháp; LIS chưa có (để minh hoạ cảnh báo BR-HTT-02)
        db.KeHoachDowntimes.AddRange(
            new KeHoachDowntime
            {
                HeThongId = his.Id, PhienBan = 1, TrangThai = TrangThaiKeHoach.HieuLuc, NgayHieuLuc = homNay.AddMonths(-3), NgayRaSoat = homNay.AddMonths(9), NguoiTao = "admin",
                NguoiGuiDuyet = "ktv1", NguoiDuyetCntt = "admin", NgayDuyetCntt = now.AddMonths(-3), NguoiDuyetLanhDao = "lanhdao", NgayDuyetLanhDao = now.AddMonths(-3),
                PhamVi = "HIS: tiếp đón, kê đơn, thanh toán, hồ sơ bệnh án điện tử. Ngừng ảnh hưởng toàn bộ khoa lâm sàng.",
                TieuChiKichHoat = "HIS không truy cập được quá 15 phút, hoặc CNTT thông báo ngừng.",
                DanhBa = "Trưởng phòng CNTT: 0901 000 001\nKTV trực: 0901 000 002\nNhà cung cấp HIS (24/7): 0900 000 000\nĐầu mối từng khoa: điều dưỡng trưởng. Kênh thay thế khi mạng hỏng: điện thoại nội bộ.",
                BuocBatDau = "1. KTV trực xác nhận sự cố, báo Trưởng phòng CNTT.\n2. Thông báo tất cả khoa qua điện thoại nội bộ.\n3. Quá 30 phút: các khoa dùng bộ biểu mẫu giấy.\n4. Liên hệ nhà cung cấp.",
                BuocTrongDowntime = "Cập nhật tình hình cho các khoa mỗi 30 phút. Máy trạm đọc-chỉ (read-only) tại khoa Cấp cứu.",
                BuocKhoiPhuc = "Khôi phục từ máy chủ dự phòng hoặc bản sao lưu gần nhất; kiểm tra đăng nhập, mở hồ sơ mẫu; thông báo các khoa.",
                QuyTrinhThayThe = "Dùng bộ biểu mẫu giấy đặt tại phòng điều dưỡng: phiếu đăng ký khám, y lệnh, phiếu xét nghiệm, phiếu thu. Giấy tờ cất giữ trong tủ khoá, không để lộ thông tin người bệnh.",
                QuyTrinhNhapBu = "Nhập lại có cấu trúc: thuốc đã kê, dị ứng, chỉ định; quét: phiếu ký tay; chép lại: ghi chú điều dưỡng. Điều dưỡng trưởng đối chiếu, hoàn thành trong 24 giờ.",
                TieuChiKetThuc = "HIS hoạt động ổn định 30 phút và các khoa xác nhận nhập bù."
            },
            new KeHoachDowntime
            {
                HeThongId = pacs.Id, PhienBan = 1, TrangThai = TrangThaiKeHoach.Nhap, NguoiTao = "ktv2",
                TieuChiKichHoat = "PACS không truy cập được quá 15 phút.", DanhBa = "KTV trực: 0901 000 002", BuocBatDau = "Báo khoa CĐHA, chuyển sang in phim / ghi đĩa.",
                BuocKhoiPhuc = "Khôi phục từ bản sao lưu, kiểm tra ảnh mẫu.", QuyTrinhThayThe = "In phim tạm, trả kết quả giấy.", QuyTrinhNhapBu = "Tải ảnh lên lại sau khôi phục."
            });
        // Sao lưu
        var tv1 = new TacVuSaoLuu { HeThongId = his.Id, Ten = "CSDL HIS – full hằng đêm", DoiTuong = "SQL Server HIS_DB", Loai = LoaiSaoLuu.Full, ChuKyPhut = 1440, GioChay = new TimeOnly(1, 0), PhanMem = "SQL Server Agent", NoiLuu = "NAS nội bộ",
            SoBanSao = 2, SoLoaiPhuongTien = 1, CoBanNgoaiCoSo = false, LuuGiuNgay = 30, MaHoa = true, TaiSanId = sv2.Id, NguoiTao = "system" };
        var tv2 = new TacVuSaoLuu { HeThongId = his.Id, Ten = "CSDL HIS – log mỗi 15 phút", DoiTuong = "SQL Server HIS_DB (transaction log)", Loai = LoaiSaoLuu.Log, ChuKyPhut = 15, GioChay = new TimeOnly(0, 5), PhanMem = "SQL Server Agent", NoiLuu = "NAS nội bộ",
            SoBanSao = 2, SoLoaiPhuongTien = 1, CoBanNgoaiCoSo = false, LuuGiuNgay = 14, MaHoa = true, TaiSanId = sv2.Id, NguoiTao = "system" };
        var tv3 = new TacVuSaoLuu { HeThongId = pacs.Id, Ten = "Ảnh PACS – sao lưu hằng ngày", DoiTuong = "Kho ảnh DICOM", Loai = LoaiSaoLuu.TepTin, ChuKyPhut = 1440, GioChay = new TimeOnly(2, 0), PhanMem = "Veeam", NoiLuu = "NAS + ổ rời ngoại tuyến",
            SoBanSao = 3, SoLoaiPhuongTien = 2, CoBanNgoaiCoSo = true, LuuGiuNgay = 90, MaHoa = true, TaiSanId = sv4.Id, NguoiTao = "system" };
        db.TacVuSaoLuus.AddRange(tv1, tv2, tv3);
        await db.SaveChangesAsync();
        var rnd = new Random(7);
        foreach (var tv in new[] { tv1, tv3 }) // tv2 (log 15 phút) chưa có kết quả nào: minh hoạ mốc "không rõ" và cảnh báo
            for (var d = 12; d >= 1; d--)
            {
                var t = now.Date.AddDays(-d).AddHours(tv.GioChay.Hour).AddMinutes(rnd.Next(0, 20));
                var ket = tv == tv1 && d == 4 ? KetQuaSaoLuu.Loi : tv == tv3 && d == 7 ? KetQuaSaoLuu.CanhBao : KetQuaSaoLuu.ThanhCong;
                db.KetQuaSaoLuuLans.Add(new KetQuaSaoLuuLan { TacVuSaoLuuId = tv.Id, ThoiDiem = t, KetQua = ket, DungLuongGb = Math.Round((decimal)(rnd.NextDouble() * 20 + 40), 1), ThoiLuongPhut = rnd.Next(18, 40),
                    GhiChu = ket == KetQuaSaoLuu.Loi ? "Hết dung lượng thư mục đích" : ket == KetQuaSaoLuu.CanhBao ? "Bỏ qua 3 tệp đang mở" : null, Nguon = NguonKetQua.NhapTay, NguoiGhi = "ktv1", ThoiGianGhi = t.AddHours(1) });
            }
        // Một lần thử phục hồi PACS đã chốt (đạt), HIS chưa từng thử → hệ thống nhắc / cảnh báo
        db.LanThuPhucHois.Add(new LanThuPhucHoi
        {
            HeThongId = pacs.Id, TacVuSaoLuuId = tv3.Id, HanThucHien = homNay.AddMonths(-2), BanSaoLuu = "Full " + homNay.AddMonths(-2).AddDays(-1).ToString("dd/MM/yyyy") + " – NAS", ThoiDiemDuLieu = now.AddMonths(-2).AddHours(-30), ThoiDiemSuCo = now.AddMonths(-2).AddHours(-24),
            MoiTruong = "Máy chủ thử PACS-TEST", BatDau = now.AddMonths(-2), KetThuc = now.AddMonths(-2).AddMinutes(95), KiemTraToanVen = "Mở được 5 ca mẫu, số ảnh khớp 100%.", NguoiXacNhanNghiepVu = "Trưởng khoa CĐHA",
            ToanVenDat = true, RtoMucTieu = 480, RpoMucTieu = 60, Dat = false, DaChot = true, NguoiChot = "ktv2", NgayChot = now.AddMonths(-2).AddHours(3), NguoiTao = "system"
        });
        var pc = db.LanThuPhucHois.Local.First();
        pc.Dat = pc.RpoDatPhut <= pc.RpoMucTieu && pc.RtoDatPhut <= pc.RtoMucTieu && pc.ToanVenDat;
        // Sự kiện downtime đã đóng 20 ngày trước (có rút kinh nghiệm) + hành động khắc phục quá hạn
        var skBd = now.AddDays(-20).Date.AddHours(9);
        var sk = new SuKienDowntime
        {
            Ma = $"DT-{skBd:yyMM}-0001", Loai = LoaiDowntime.DotXuat, MucDo = MucDoDowntime.NgungHoanToan, TrangThai = TrangThaiSuKien.DaDong, MoTa = "Mất điện lưới, UPS cạn pin sau 18 phút, máy chủ HIS tắt đột ngột.",
            BatDau = skBd, KetThuc = skBd.AddMinutes(55), KichHoatKeHoach = true, RtoPhut = 240, MucTrongYeuCaoNhat = MucTrongYeu.ToiQuanTrong, NguoiTao = "ktv1",
            NguyenNhanGoc = "Ắc quy UPS xuống cấp, thời gian lưu điện chỉ còn 18 phút; chưa có máy phát tự động chuyển nguồn.", DiemTot = "Các khoa chuyển sang giấy trong 10 phút.", CanCaiThien = "Thay ắc quy UPS; thử chuyển nguồn hằng tháng; bổ sung thông báo qua loa nội bộ.",
            CanSuaKeHoach = false, NgayRutKinhNghiem = skBd.AddDays(1), NguoiRutKinhNghiem = "admin", NgayDong = skBd.AddDays(2), NguoiDong = "admin"
        };
        sk.HeThongs.Add(new SuKienDowntimeHeThong { HeThongId = his.Id });
        foreach (var m in new[] { "KB", "CC", "NOI" }) sk.Khoas.Add(new SuKienDowntimeKhoa { KhoaPhongId = khoa[m].Id, KichHoatGiay = true, DaNhapBu = true, NguoiXacNhan = "truongkhoacc", ThoiDiemXacNhan = skBd.AddHours(20) });
        sk.NhatKys.Add(new NhatKyDowntime { ThoiDiem = skBd, NoiDung = "Phát hiện HIS ngừng, kích hoạt kế hoạch downtime.", NguoiGhi = "ktv1" });
        sk.NhatKys.Add(new NhatKyDowntime { ThoiDiem = skBd.AddMinutes(55), NoiDung = "Hệ thống đã khôi phục.", NguoiGhi = "ktv1" });
        db.SuKienDowntimes.Add(sk);
        await db.SaveChangesAsync();
        db.HanhDongKhacPhucs.Add(new HanhDongKhacPhuc
        {
            Ma = $"HD-{now:yyMM}-0001", Nguon = NguonHanhDong.SuKienDowntime, NguonId = sk.Id, MoTa = "Thay ắc quy UPS phòng máy chủ và thử chuyển nguồn hằng tháng.", Loai = LoaiHanhDong.KhacPhuc,
            HeThongId = his.Id, NguoiChiuTrachNhiem = "ktv1", Han = homNay.AddDays(-5), UuTien = MucDoUuTien.Cao, TrangThai = TrangThaiHanhDong.DangLam, NguoiTao = "admin"
        });
        await db.SaveChangesAsync();
    }

    /// <summary>Dữ liệu mẫu GĐ3.2: người giám sát, tài liệu, kênh dữ liệu, telehealth/AI, sự cố an ninh, rủi ro, đào tạo.</summary>
    private static async Task TaoGD32MauAsync(AppDbContext db, Dictionary<string, KhoaPhong> khoa)
    {
        if (await db.RuiRos.AnyAsync()) return;
        var now = DateTime.Now; var hn = DateOnly.FromDateTime(now); var his = await db.HeThongThongTins.FirstAsync(h => h.Ma == "HIS");
        db.NguoiGiamSatCntts.Add(new NguoiGiamSatCntt { HoTen = "Trần Minh Bảo", ChucDanh = "Phó trưởng phòng CNTT", TaiKhoan = "ktv2", SoVanBan = "QĐ-125/QĐ-BV", NgayVanBan = hn.AddMonths(-8), NgayBatDau = hn.AddMonths(-8),
            TrinhDo = "Thạc sĩ CNTT; chứng chỉ an toàn thông tin; 12 năm kinh nghiệm hệ thống thông tin bệnh viện", NguoiThayThe = "Nguyễn Văn An", NguoiTao = "admin" });
        // Tài liệu: chính sách ATTT hiệu lực; chương trình ứng phó tấn công mạng còn nháp (minh hoạ cảnh báo)
        db.TaiLieuQuyDinhs.AddRange(
            new TaiLieuQuyDinh { Ma = $"VB-{now:yyMM}-0001", Loai = LoaiTaiLieu.ChinhSachAnNinhMang, Ten = "Chính sách an ninh mạng và quản lý rủi ro mạng", PhienBan = 1, TrangThai = TrangThaiKeHoach.HieuLuc,
                NoiDung = "1. Phân loại tài sản thông tin: dữ liệu người bệnh là mức nhạy cảm cao.\n2. Kiểm soát truy cập theo vai trò, mật khẩu tối thiểu 10 ký tự, xác thực nhiều lớp với tài khoản quản trị.\n3. Quản lý bản vá: máy chủ ≤ 30 ngày, lỗ hổng nghiêm trọng ≤ 7 ngày.\n4. Chống mã độc, lọc email, kiểm thử xâm nhập hằng năm.\n5. Sao lưu 3-2-1, có bản ngoại tuyến.\n6. Khi bị tấn công thành công: cô lập máy, báo Trưởng phòng CNTT ngay, kích hoạt chương trình ứng phó, không trả tiền chuộc khi chưa có chỉ đạo.",
                NgayHieuLuc = hn.AddMonths(-4), NgayRaSoat = hn.AddMonths(8), NguoiGuiDuyet = "ktv2", NguoiDuyetCntt = "admin", NgayDuyetCntt = now.AddMonths(-4), NguoiDuyetLanhDao = "lanhdao", NgayDuyetLanhDao = now.AddMonths(-4), NguoiTao = "ktv2" },
            new TaiLieuQuyDinh { Ma = $"VB-{now:yyMM}-0002", Loai = LoaiTaiLieu.ChuongTrinhUngPhoMang, Ten = "Chương trình ứng phó tấn công mạng", PhienBan = 1, TrangThai = TrangThaiKeHoach.Nhap, NguoiTao = "ktv2",
                NoiDung = "Tác động dự kiến: mã độc mã hoá HIS làm ngừng toàn bộ khoa lâm sàng. Chiến lược duy trì chăm sóc: dùng quy trình giấy của kế hoạch downtime. Truyền thông: thông báo Lãnh đạo, các khoa, cơ quan chức năng theo quy định. Khôi phục: từ bản sao lưu ngoại tuyến." });
        // Hệ thống telehealth và công cụ AI (HCT.01.02, HCT.01.03)
        var tele = new HeThongThongTin { Ma = "TELE", Ten = "Khám bệnh từ xa (telehealth)", Loai = LoaiHeThong.LamSang, MucTrongYeu = MucTrongYeu.TrungBinh, TrangThai = TrangThaiHeThong.VanHanh, PhienBan = "1.0", LaTelehealth = true,
            ChuSoHuu = "Trưởng khoa Khám bệnh", ChuSoHuuKhoaPhongId = khoa["KB"].Id, PhuTrachChinh = "ktv1", NguoiTao = "system" };
        var ai = new HeThongThongTin { Ma = "AI-NKH", Ten = "Cảnh báo sớm nhiễm khuẩn huyết (AI)", Loai = LoaiHeThong.LamSang, MucTrongYeu = MucTrongYeu.TrungBinh, TrangThai = TrangThaiHeThong.VanHanh, PhienBan = "0.9", LaCongCuAi = true, LoaiCongCuAi = LoaiCongCuAi.DuBao,
            NgayXemXetAi = hn.AddDays(-10), ChuSoHuu = "Trưởng khoa Hồi sức tích cực", ChuSoHuuKhoaPhongId = khoa["HSTC"].Id, PhuTrachChinh = "ktv1", NguoiTao = "system" };
        db.HeThongThongTins.AddRange(tele, ai); await db.SaveChangesAsync();
        db.BuocVongDois.AddRange(new BuocVongDoi { HeThongId = ai.Id, Buoc = BuocVongDoiAi.LuaChon, NguoiPhuTrach = "BS. Lê Thị Cúc", Ngay = hn.AddMonths(-7), NoiDung = "So sánh 3 giải pháp; chọn theo độ nhạy và tích hợp HIS.", KetLuan = "Chọn giải pháp B" },
            new BuocVongDoi { HeThongId = ai.Id, Buoc = BuocVongDoiAi.TrienKhai, NguoiPhuTrach = "BS. Lê Thị Cúc", Ngay = hn.AddMonths(-6), NoiDung = "Chạy song song 4 tuần tại HSTC, đối chiếu với đánh giá của bác sĩ.", KetLuan = "Đạt, triển khai chính thức" });
        // Kênh truyền dữ liệu (HCT.01.01)
        db.KenhTruyenDuLieus.AddRange(
            new KenhTruyenDuLieu { Ten = "Nhóm Zalo điều dưỡng khoa Cấp cứu", Loai = LoaiKenh.NhanTinBaoMat, MoTa = "Trao đổi kết quả cận lâm sàng khẩn", PhamViDung = "Khoa Cấp cứu", MaHoa = true, DangNhapRieng = false, DanhBaNguoiDuocPhep = false, BienNhanGuiDoc = true, DauThoiGian = true, BaoVeTruyCap = false, NguoiTao = "ktv1" },
            new KenhTruyenDuLieu { Ten = "Cổng thông tin người bệnh", Loai = LoaiKenh.CongNguoiBenh, MoTa = "Xem kết quả, đặt lịch hẹn", HeThongId = his.Id, MaHoa = true, DangNhapRieng = true, NhatKyThaoTac = true, DongYNguoiBenh = false, NguoiTao = "ktv1" },
            new KenhTruyenDuLieu { Ten = "Giao diện HL7 LIS–HIS", Loai = LoaiKenh.GiaoDienKetNoi, HeThongId = his.Id, MaHoa = true, NguoiTao = "ktv1" });
        // Sự cố an ninh mạng: một đã đóng (phishing), một người dùng vừa báo
        var an1 = new SuCoAnNinh { Ma = $"AN-{now:yyMM}-0001", Nguon = NguonSuCoAn.NguoiDungBao, Loai = LoaiSuCoAn.Phishing, MucDo = MucDoAn.ThongThuong, LienQuanDuLieuCaNhan = false, TrangThai = TrangThaiSuCoAn.DaDong, PhatHien = now.AddDays(-21),
            MoTa = "Nhân viên phòng Tài chính nhận email giả mạo ngân hàng, đã bấm vào liên kết nhưng chưa nhập mật khẩu.", NguoiBao = "nhanvien", NguoiPhuTrach = "ktv2", NgayDong = now.AddDays(-20), NguoiDong = "ktv2", NguoiTao = "nhanvien" };
        an1.Buocs.Add(new BuocUngPho { Buoc = BuocUngPhoAn.PhatHien, NoiDung = "Xác nhận email giả mạo qua tiêu đề thư.", NguoiThucHien = "ktv2", ThoiDiem = now.AddDays(-21), HoanThanh = true });
        an1.Buocs.Add(new BuocUngPho { Buoc = BuocUngPhoAn.NganChan, NoiDung = "Chặn tên miền trên tường lửa; quét máy tính, đổi mật khẩu người dùng.", NguoiThucHien = "ktv2", ThoiDiem = now.AddDays(-21), HoanThanh = true });
        var an2 = new SuCoAnNinh { Ma = $"AN-{now:yyMM}-0002", Nguon = NguonSuCoAn.NguoiDungBao, Loai = LoaiSuCoAn.Khac, MucDo = MucDoAn.ThongThuong, TrangThai = TrangThaiSuCoAn.Moi, PhatHien = now.AddHours(-3),
            MoTa = "Máy tính khoa Cấp cứu hiện thông báo lạ yêu cầu liên hệ để mở khoá tệp.", NguoiBao = "nhanvien", NguoiTao = "nhanvien" };
        db.SuCoAnNinhs.AddRange(an1, an2);
        // Rủi ro
        RuiRo Rr(int stt, string moTa, NhomRuiRo nhom, int kn, int hq, int? tk, int? th, string? ks, PhuongAnRuiRo? pa, int xemXetThang)
        {
            var r = new RuiRo { Ma = $"RR-{now:yyMM}-{stt:D4}", MoTa = moTa, Nhom = nhom, HeThongId = his.Id, KiemSoatHienCo = ks, PhuongAn = pa, NguoiChiuTrachNhiem = "ktv2", NgayXemXetLai = hn.AddMonths(xemXetThang), NguoiTao = "ktv2" };
            r.ChamDiem(kn, hq, tk, th, "", "ktv2", now.AddMonths(-5)); return r;
        }
        db.RuiRos.AddRange(
            Rr(1, "Ắc quy UPS xuống cấp, mất điện lưới có thể làm máy chủ HIS tắt đột ngột, hỏng dữ liệu và ngừng toàn bộ khoa lâm sàng.", NhomRuiRo.DienDieuHoa, 4, 5, 2, 5, "Kiểm tra UPS hằng tuần; chưa có máy phát tự động chuyển nguồn", PhuongAnRuiRo.GiamThieu, 1),
            Rr(2, "Switch lõi không có dự phòng: hỏng thiết bị làm mất kết nối HIS, LIS, PACS.", NhomRuiRo.Mang, 3, 5, null, null, "Có cấu hình sao lưu; chưa có thiết bị dự phòng", null, 2),
            Rr(3, "Mã độc mã hoá tống tiền lây qua email làm mất dữ liệu và ngừng hệ thống.", NhomRuiRo.AnNinhMang, 3, 5, 2, 5, "Chống mã độc, lọc email, sao lưu hằng đêm (chưa có bản ngoại tuyến)", PhuongAnRuiRo.GiamThieu, 3),
            Rr(4, "Hết dung lượng thư mục sao lưu làm sao lưu đêm thất bại không được phát hiện kịp thời.", NhomRuiRo.DuLieu, 4, 3, null, null, "Cảnh báo lỗi sao lưu của hệ thống", null, 6),
            Rr(5, "Phụ thuộc một nhà cung cấp phần mềm HIS, hợp đồng hỗ trợ sắp hết hạn.", NhomRuiRo.NhaCungCap, 3, 3, null, null, "Hợp đồng bảo trì 24/7", null, 6));
        // Đào tạo
        var cc = khoa["CC"]; var dt = new BuoiDaoTao { Ngay = now.AddMonths(-2), Loai = LoaiDaoTao.Downtime, ChuDe = "Quy trình downtime HIS cho điều dưỡng khoa Cấp cứu", HeThongId = his.Id, KhoaPhongId = cc.Id, NguoiGiang = "Nguyễn Văn An", NoiDung = "Khi nào kích hoạt, dùng bộ biểu mẫu giấy, nhập bù dữ liệu sau khôi phục.", NguoiTao = "ktv1" };
        dt.ThamDus.AddRange([new() { HoTen = "Phạm Thu Hà", MaNhanVien = "NV0123", KhoaPhongId = cc.Id, ChucDanh = "Điều dưỡng", DatDanhGia = true }, new() { HoTen = "Lê Văn Hùng", MaNhanVien = "NV0145", KhoaPhongId = cc.Id, ChucDanh = "Điều dưỡng", DatDanhGia = true }, new() { HoTen = "Võ Thị Mai", MaNhanVien = "NV0167", KhoaPhongId = cc.Id, ChucDanh = "Bác sĩ", DatDanhGia = true }]);
        db.BuoiDaoTaos.AddRange(dt, new BuoiDaoTao { Ngay = now.AddMonths(-14), Loai = LoaiDaoTao.AnToanThongTin, ChuDe = "An toàn thông tin cho nhân viên toàn viện (năm trước)", NguoiGiang = "Trần Minh Bảo", NguoiTao = "ktv2" });
        await db.SaveChangesAsync();
    }

    /// <summary>Dữ liệu mẫu GĐ3.3: quản lý thay đổi, dự án xây dựng.</summary>
    private static async Task TaoGD33MauAsync(AppDbContext db, Dictionary<string, KhoaPhong> khoa)
    {
        if (await db.PhieuThayDois.AnyAsync()) return;
        var now = DateTime.Now; var hn = DateOnly.FromDateTime(now);
        var his = await db.HeThongThongTins.FirstAsync(h => h.Ma == "HIS"); var lis = await db.HeThongThongTins.FirstAsync(h => h.Ma == "LIS");
        db.ThayDoiChuanMaus.AddRange(
            new ThayDoiChuanMau { Ten = "Cập nhật bản vá hằng tháng cho máy trạm", HuongDan = "Triển khai bản vá đã kiểm thử trên nhóm thí điểm, sau đó toàn viện ngoài giờ cao điểm; ghi danh sách máy chưa vá.", KiemThu = "Mở HIS, in thử một phiếu", NguoiDuyet = "admin", NgayDuyet = now.AddMonths(-3), NguoiTao = "ktv1" },
            new ThayDoiChuanMau { Ten = "Thêm tài khoản người dùng mới theo yêu cầu của khoa", HuongDan = "Tạo tài khoản theo mẫu phân quyền của vai trò; ghi người đề nghị.", NguoiDuyet = null, NguoiTao = "ktv1" });
        // Thay đổi thường đã hoàn thành (nâng cấp HIS), có downtime và đánh giá sau triển khai
        var done = new PhieuThayDoi { Ma = $"TD-{now:yyMM}-0001", Loai = LoaiThayDoi.Thuong, Ten = "Nâng cấp HIS lên phiên bản 5.2", MoTa = "Nâng cấp phân hệ viện phí và đơn thuốc", LyDo = "Sửa lỗi in phiếu, theo khuyến cáo của nhà cung cấp", HeThongId = his.Id, MucRui = MucRuiRo.Cao, LaThayDoiLon = true,
            KeHoachThucHien = "Sao lưu toàn bộ, cài bản nâng cấp trên máy chủ ứng dụng, chạy script cơ sở dữ liệu, kiểm thử.", KeHoachKiemThu = "Chạy 10 kịch bản kiểm thử: nhập viện, kê đơn, thanh toán", PhuongAnQuayLui = "Phục hồi bản sao lưu trước nâng cấp (dự kiến 90 phút).",
            CanDowntime = true, DeXuatBatDau = now.AddDays(-40), DeXuatKetThuc = now.AddDays(-40).AddHours(3), PhienBanMoi = "5.2", NguoiDeXuat = "ktv1", NguoiThucHien = "ktv1", TrangThai = TrangThaiThayDoi.HoanThanh, NgayGuiDuyet = now.AddDays(-50),
            NguoiDuyetCntt = "admin", NgayDuyetCntt = now.AddDays(-48), NguoiDuyetChuSoHuu = "truongkhoacc", NgayDuyetChuSoHuu = now.AddDays(-47), BatDauThucTe = now.AddDays(-40), KetThucThucTe = now.AddDays(-40).AddHours(2.5),
            KetQua = "Nâng cấp thành công, downtime 2,5 giờ.", KiemThuSau = "10/10 kịch bản đạt", NguoiXacNhanNghiepVu = "Phòng Kế hoạch tổng hợp", NgayXacNhanNghiepVu = now.AddDays(-40), NguoiTao = "ktv1" };
        done.HeThongs.Add(new PhieuThayDoiHeThong { HeThongId = his.Id });
        done.DanhGiaSau = new DanhGiaSauThayDoi { Han = hn.AddDays(50) };
        // Thay đổi khẩn cấp đang thực hiện, chưa duyệt bổ sung
        var kc = new PhieuThayDoi { Ma = $"TD-{now:yyMM}-0002", Loai = LoaiThayDoi.KhanCap, Ten = "Thay switch tầng 2 khu Cấp cứu bị hỏng", LyDo = "Switch hỏng, mất mạng khu Cấp cứu", HeThongId = lis.Id, MucRui = MucRuiRo.Cao, PhuongAnQuayLui = "Gắn lại switch cũ, chuyển cáp sang switch dự phòng.",
            KeHoachThucHien = "Thay bằng switch dự phòng cùng model, nạp cấu hình đã sao lưu.", NguoiDeXuat = "ktv1", NguoiThucHien = "ktv1", TrangThai = TrangThaiThayDoi.DangThucHien, BatDauThucTe = now.AddHours(-2), HanDuyetBoSung = PhieuThayDoi.CongNgayLam(now, 2), NguoiTao = "ktv1" };
        kc.HeThongs.Add(new PhieuThayDoiHeThong { HeThongId = lis.Id });
        // Triển khai mới (công cụ AI) đang nháp, có checklist
        var moi = new PhieuThayDoi { Ma = $"TD-{now:yyMM}-0003", Loai = LoaiThayDoi.TrienKhaiMoi, Ten = "Triển khai công cụ gợi ý liều thuốc nhi khoa", MoTa = "Công cụ hỗ trợ quyết định lâm sàng tích hợp HIS", LyDo = "Giảm sai sót liều thuốc ở trẻ em", MucRui = MucRuiRo.Cao, LaThayDoiLon = true, LaCongCuHoTroQuyetDinh = true,
            KeHoachThucHien = "Chạy thử song song 4 tuần tại khoa Nhi", PhuongAnQuayLui = "Tắt tích hợp, quay về kê đơn thông thường.", NguoiDeXuat = "ktv1", NguoiTao = "ktv1" };
        moi.TaoChecklistMacDinh(); foreach (var m in moi.Checklist.Take(3)) { m.Dat = true; m.NguoiXacNhan = "ktv1"; }
        db.PhieuThayDois.AddRange(done, kc, moi);
        db.DuAnXayDungs.Add(new DuAnXayDung { Ten = "Cải tạo khu Cấp cứu giai đoạn 2", ViTri = "Tầng 1, khu A", TuNgay = hn.AddDays(20), DenNgay = hn.AddDays(110), HaTangAnhHuong = "Tuyến cáp quang tầng 1, tủ mạng khu Cấp cứu, nguồn điện tầng 1", NguoiThamGiaCntt = "Trần Minh Bảo", NgayDanhGia = hn.AddDays(-5),
            BienPhapBaoVe = "Di dời tủ mạng tạm, bọc ống bảo vệ cáp, kiểm tra mạng hằng ngày trong thi công", NhaThau = "Công ty Xây dựng ABC", NguoiTao = "ktv2" });
        // Rà soát quyền truy cập HIS: đang chờ các khoa xác nhận
        var dot = new DotRaSoatQuyen { Ma = $"RQ-{now:yyMM}-0001", HeThongId = his.Id, LyDo = "Định kỳ 6 tháng", Han = hn.AddDays(20), SoNgayKhongDangNhap = 90, NguoiTao = "ktv1" };
        TaiKhoanRaSoat Tk(string u, string? ten, string? khoaMa, string quyen, int dnNgay) => new() { TenDangNhap = u, HoTen = ten, KhoaPhongId = khoaMa is null ? null : khoa[khoaMa].Id, KhoaText = khoaMa is null ? null : khoa[khoaMa].Ten, NhomQuyen = quyen, LanDangNhapCuoi = dnNgay < 0 ? null : now.AddDays(-dnNgay), TrangThaiTk = "Hoạt động" };
        dot.NhapDanhSach([Tk("bs.hung", "Lê Văn Hùng", "CC", "Bác sĩ", 2), Tk("dd.ha", "Phạm Thu Hà", "CC", "Điều dưỡng", 5), Tk("dd.cu", "Nguyễn Văn Cũ", "CC", "Điều dưỡng", 200), Tk("bs.mai", "Võ Thị Mai", "KB", "Bác sĩ", 1), Tk("his_admin", "Quản trị HIS", null, "Quản trị hệ thống (admin)", 1), Tk("khoa.chung", null, "HSTC", "Điều dưỡng", 3)], "ktv1", now.AddDays(-3));
        db.DotRaSoatQuyens.Add(dot);
        db.ViPhamTruyCaps.Add(new ViPhamTruyCap { Nguon = NguonViPham.BaoCaoKhoa, MoTa = "Điều dưỡng dùng chung tài khoản đăng nhập HIS trong ca trực.", HeThongId = his.Id, KhoaPhongId = khoa["CC"].Id, NgayPhatHien = hn.AddDays(-12), TrangThai = TrangThaiViPham.Moi });
        // GĐ3.3c: lỗ hổng, bài tập chủ động, lưu giữ dữ liệu, hồ sơ hợp đồng dịch vụ
        db.LoHongs.AddRange(
            new LoHong { TieuDe = "Lỗ hổng thực thi mã từ xa trên máy chủ ứng dụng HIS", MaCve = "CVE-2026-1234", Nguon = NguonLoHong.KhuyenCao, Cvss = 9.1m, HeThongId = his.Id, NgayPhatHien = hn.AddDays(-3), HanVa = LoHong.TinhHan(hn.AddDays(-3), 9.1m, 7, 30, 90, 180), NguoiPhuTrach = "ktv1", NguoiTao = "ktv1" },
            new LoHong { TieuDe = "Thư viện mã hoá cũ trên máy chủ thư điện tử", MaCve = "CVE-2026-0456", Nguon = NguonLoHong.QuetLoHong, Cvss = 5.3m, HeThongId = lis.Id, NgayPhatHien = hn.AddDays(-40), HanVa = LoHong.TinhHan(hn.AddDays(-40), 5.3m, 7, 30, 90, 180), NguoiPhuTrach = "ktv2", NguoiTao = "ktv2" });
        his.LoaiDuLieuLuuGiu = "Bệnh án điện tử"; his.LuuGiuNam = 20; his.CanCuLuuGiu = "Luật Khám bệnh, chữa bệnh"; his.CachHuyDuLieu = "Huỷ theo quyết định của Hội đồng huỷ hồ sơ, lập biên bản";
        db.BaiTapChuDongs.Add(new BaiTapChuDong { Nam = hn.Year, QuyTrinh = "Ngừng hoạt động HIS do mất nguồn điện phòng máy chủ", NgayThucHien = hn.AddDays(-20), ThamGia = "CNTT, Điều dưỡng trưởng khoa Cấp cứu, HCQT", DiemCoTheHong = "UPS quá tải; máy phát chưa tự chuyển nguồn; bộ biểu mẫu giấy không đủ", HauQua = "Khoa lâm sàng phải ghi giấy, nguy cơ nhập bù sai", BienPhap = "Bổ sung công tắc chuyển nguồn tự động; tăng số lượng biểu mẫu giấy; thử mỗi quý", NguoiTao = "ktv2" });
        var hdBt = new HopDong { SoHopDong = "HĐ-BT-0001", Ten = "Bảo trì và hỗ trợ phần mềm HIS", Loai = LoaiHopDong.BaoTri, NgayKy = hn.AddMonths(-6), HieuLucTu = hn.AddMonths(-6), HieuLucDen = hn.AddMonths(6), GiaTri = 480_000_000, NguoiPhuTrach = "ktv1", PhamVi = "Bảo trì, vá lỗi, hỗ trợ 24/7 phần mềm HIS",
            ChiTieuNhaThau = "Phản hồi ≤ 2 giờ; khắc phục sự cố nghiêm trọng ≤ 8 giờ; sẵn sàng ≥ 99,5%", TanSuatBaoCao = "Báo cáo tháng bằng văn bản", NguoiTao = "ktv1" };
        db.HopDongs.Add(hdBt); await db.SaveChangesAsync(); his.HopDongId = hdBt.Id;
        await db.SaveChangesAsync();
    }
}
