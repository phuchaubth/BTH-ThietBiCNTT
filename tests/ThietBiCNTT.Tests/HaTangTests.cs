using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Tests;

/// <summary>Quy tắc nghiệp vụ GĐ3.1 – hạ tầng CNTT theo JCI 8 (docs/JCI8-HaTangCNTT.md).</summary>
public class PhanTichPhuThuocTests
{
    // HIS(1) → SV-1(tài sản 10, không dự phòng), SW(tài sản 11, có dự phòng); LIS(2) → HIS(1) + SV-3(tài sản 12); PACS(3) → tài sản 11
    private static readonly List<CanhPhuThuoc> Canh =
    [
        new(1, 10, null, false), new(1, 11, null, true), new(2, null, 1, false), new(2, 12, null, false), new(3, 11, null, false)
    ];

    [Fact] public void TaiSan_AnhHuongTrucTiepVaGianTiep()
    {
        Assert.Equal([1, 2], PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(10, Canh).Order()); // HIS và LIS (LIS phụ thuộc HIS)
        Assert.Equal([1, 2, 3], PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(11, Canh).Order());
        Assert.Equal([2], PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(12, Canh));
        Assert.Empty(PhanTichPhuThuoc.HeThongBiAnhHuongBoiTaiSan(99, Canh));
    }

    [Fact] public void HeThong_AnhHuongKhongGomChinNo() => Assert.Equal([2], PhanTichPhuThuoc.HeThongBiAnhHuongBoiHeThong(1, Canh));

    [Theory] [InlineData(1, 1, true)] [InlineData(1, 2, true)] [InlineData(2, 1, false)] [InlineData(3, 1, false)] [InlineData(1, 3, false)]
    public void TaoVong(int heThong, int phuThuoc, bool kyVong) => Assert.Equal(kyVong, PhanTichPhuThuoc.TaoVong(heThong, phuThuoc, Canh)); // BR-HTT-03

    [Fact] public void TaoVong_GianTiep()
    {
        List<CanhPhuThuoc> c = [new(1, null, 2, false), new(2, null, 3, false)];
        Assert.True(PhanTichPhuThuoc.TaoVong(3, 1, c)); // 3→1→2→3
        Assert.False(PhanTichPhuThuoc.TaoVong(1, 3, c));
    }

    [Fact] public void DiemLoiDon_BoQuaThanhPhanCoDuPhong_DiXuyenHeThongCon()
    {
        Assert.Equal([10], PhanTichPhuThuoc.DiemLoiDon(1, Canh));       // 11 có dự phòng
        Assert.Equal([10, 12], PhanTichPhuThuoc.DiemLoiDon(2, Canh).Order()); // qua LIS → HIS → 10
    }
}

public class KeHoachDowntimeTests
{
    private static KeHoachDowntime Kh() => new() { HeThongId = 1, TieuChiKichHoat = "x", DanhBa = "d", BuocBatDau = "b", BuocKhoiPhuc = "k", QuyTrinhThayThe = "t", QuyTrinhNhapBu = "n" };
    private static readonly DateTime Now = new(2026, 11, 24, 9, 0, 0);

    [Fact] public void HeThongThuong_MotCapCntt_CoHieuLuc()
    {
        var k = Kh(); k.GuiDuyet("ktv1");
        Assert.True(k.Duyet("admin", true, false, canLanhDao: false, Now, 12));
        Assert.Equal(TrangThaiKeHoach.HieuLuc, k.TrangThai);
        Assert.Equal(new DateOnly(2027, 11, 24), k.NgayRaSoat);
    }

    [Fact] public void ToiQuanTrong_CanHaiCap_ThuTuCntt_RoiLanhDao()
    {
        var k = Kh(); k.GuiDuyet("ktv1");
        Assert.Throws<DomainException>(() => k.Duyet("ld", false, true, true, Now, 12)); // Lãnh đạo không duyệt trước
        Assert.False(k.Duyet("admin", true, false, true, Now, 12));
        Assert.Equal(TrangThaiKeHoach.ChoDuyet, k.TrangThai);
        Assert.True(k.Duyet("ld", false, true, true, Now.AddHours(1), 12));
        Assert.Equal("ld", k.NguoiDuyetLanhDao);
    }

    [Fact] public void KhongCoQuyen_HoacSaiTrangThai_BiChan()
    {
        var k = Kh(); Assert.Throws<DomainException>(() => k.Duyet("a", true, false, false, Now, 12)); // chưa gửi duyệt
        k.GuiDuyet("a"); Assert.Throws<DomainException>(() => k.Duyet("ktv", false, false, false, Now, 12));
        Assert.Throws<DomainException>(() => k.GuiDuyet("a")); // đã chờ duyệt
    }

    [Fact] public void TuChoi_VeNhap_CanLyDo_XoaDuyet()
    {
        var k = Kh(); k.GuiDuyet("ktv1"); k.Duyet("admin", true, false, true, Now, 12);
        Assert.Throws<DomainException>(() => k.TuChoi("ld", " "));
        k.TuChoi("ld", "Thiếu kênh liên lạc thay thế");
        Assert.Equal(TrangThaiKeHoach.Nhap, k.TrangThai); Assert.Null(k.NguoiDuyetCntt); Assert.Contains("kênh liên lạc", k.LyDoTuChoi);
    }

    [Fact] public void DuyetXong_RaSoat_VaCoDanhDau_FRDT13()
    {
        var k = Kh(); k.GuiDuyet("a"); k.Duyet("admin", true, false, false, Now, 12);
        var homNay = new DateOnly(2026, 12, 1);
        Assert.False(k.DenHanRaSoat(homNay));
        k.DanhDauCanRaSoat("Sau sự kiện DT-1"); k.DanhDauCanRaSoat("Sau sự kiện DT-1"); // không lặp lý do
        Assert.True(k.DenHanRaSoat(homNay)); Assert.Equal("Sau sự kiện DT-1", k.LyDoRaSoat);
        k.RaSoatXong(homNay, 12);
        Assert.False(k.CanRaSoat); Assert.False(k.DenHanRaSoat(homNay)); Assert.Equal(new DateOnly(2027, 12, 1), k.NgayRaSoat);
        Assert.True(k.DenHanRaSoat(new DateOnly(2027, 12, 2))); // đến hạn định kỳ
    }

    [Fact] public void DanhDauCanRaSoat_BoQuaKeHoachKhongHieuLuc() { var k = Kh(); k.DanhDauCanRaSoat("x"); Assert.False(k.CanRaSoat); }

    [Fact] public void PhienBanMoi_SaoChepNoiDung_VeNhap()
    {
        var k = Kh(); var m = k.TaoPhienBanMoi(2);
        Assert.Equal(2, m.PhienBan); Assert.Equal(TrangThaiKeHoach.Nhap, m.TrangThai); Assert.Equal(k.DanhBa, m.DanhBa);
    }
}

public class SuKienDowntimeTests
{
    private static readonly DateTime T0 = DateTime.Now.AddHours(-6);
    private static SuKienDowntime Ev(MucTrongYeu m = MucTrongYeu.ToiQuanTrong, int rto = 240) => new()
    {
        Ma = "DT-1", MoTa = "x", Loai = LoaiDowntime.DotXuat, TrangThai = TrangThaiSuKien.DangDienRa, BatDau = T0, RtoPhut = rto, MucTrongYeuCaoNhat = m,
        Khoas = [new() { KhoaPhongId = 1, KichHoatGiay = true }, new() { KhoaPhongId = 2, KichHoatGiay = false }]
    };

    [Fact] public void KhoiPhuc_TinhThoiLuong_VuotRto()
    {
        var s = Ev(); s.KhoiPhuc(T0.AddMinutes(300), "ktv");
        Assert.Equal(300, s.ThoiLuong(DateTime.Now)!.Value.TotalMinutes); Assert.True(s.VuotRto(DateTime.Now));
        var ok = Ev(); ok.KhoiPhuc(T0.AddMinutes(100), "ktv"); Assert.False(ok.VuotRto(DateTime.Now));
    }

    [Fact] public void KhoiPhuc_SaiThoiDiem_BiChan()
    {
        Assert.Throws<DomainException>(() => Ev().KhoiPhuc(T0.AddMinutes(-1), "k"));
        Assert.Throws<DomainException>(() => Ev().KhoiPhuc(DateTime.Now.AddHours(2), "k"));
    }

    [Fact] public void BatBuocRutKinhNghiem_TheoMucVaVuotRto()
    {
        Assert.True(Ev(MucTrongYeu.ToiQuanTrong).BatBuocRutKinhNghiem(DateTime.Now));
        Assert.True(Ev(MucTrongYeu.Cao).BatBuocRutKinhNghiem(DateTime.Now));
        var tb = Ev(MucTrongYeu.TrungBinh, rto: 1440); tb.KhoiPhuc(T0.AddMinutes(30), "k"); Assert.False(tb.BatBuocRutKinhNghiem(DateTime.Now));
        var vuot = Ev(MucTrongYeu.TrungBinh, rto: 60); vuot.KhoiPhuc(T0.AddMinutes(90), "k"); Assert.True(vuot.BatBuocRutKinhNghiem(DateTime.Now));
    }

    [Fact] public void Dong_ChiKhiDuDieuKien_BRDT03()
    {
        var s = Ev(); Assert.Throws<DomainException>(() => s.Dong("a", DateTime.Now)); // chưa khôi phục
        s.KhoiPhuc(T0.AddMinutes(60), "k");
        var ly = s.LyDoChuaDong(DateTime.Now); Assert.Equal(2, ly.Count); Assert.Contains(ly, x => x.Contains("nhập bù")); Assert.Contains(ly, x => x.Contains("rút kinh nghiệm"));
        s.Khoas[0].DaNhapBu = true; // khoa 2 không dùng giấy nên không cần
        Assert.Throws<DomainException>(() => s.Dong("a", DateTime.Now));
        s.RutKinhNghiem("nguyên nhân", null, "cải thiện", false, "a", DateTime.Now);
        s.Dong("a", DateTime.Now); Assert.Equal(TrangThaiSuKien.DaDong, s.TrangThai);
        Assert.Throws<DomainException>(() => s.Ghi("x", "a", DateTime.Now)); // đã đóng
    }

    [Fact] public void RutKinhNghiem_BatBuocNguyenNhanVaCaiThien() =>
        Assert.Throws<DomainException>(() => Ev().RutKinhNghiem(" ", "ok", "x", false, "a", DateTime.Now));

    [Fact] public void CoKeHoach_ChiTinhPhanVuotKhungLaDotXuat_BRDT04()
    {
        var b = new DateTime(2026, 11, 20, 22, 0, 0);
        var s = new SuKienDowntime { Loai = LoaiDowntime.CoKeHoach, TrangThai = TrangThaiSuKien.DaKhoiPhuc, BatDau = b, KetThuc = b.AddHours(3), DuKienBatDau = b, DuKienKetThuc = b.AddHours(2) };
        Assert.Equal(TimeSpan.FromHours(1), s.ThoiLuongDotXuat(DateTime.Now));
        s.KetThuc = b.AddMinutes(90); Assert.Equal(TimeSpan.Zero, s.ThoiLuongDotXuat(DateTime.Now));
        var dx = Ev(); dx.KhoiPhuc(T0.AddMinutes(45), "k"); Assert.Equal(TimeSpan.FromMinutes(45), dx.ThoiLuongDotXuat(DateTime.Now));
    }

    [Fact] public void LenLich_BatDauThucTe_RoiHuy()
    {
        var s = new SuKienDowntime { Loai = LoaiDowntime.CoKeHoach, TrangThai = TrangThaiSuKien.DaLenLich };
        s.BatDauThucTe(T0, "a"); Assert.Equal(TrangThaiSuKien.DangDienRa, s.TrangThai);
        Assert.Throws<DomainException>(() => s.Huy("a", DateTime.Now));
    }
}

public class KhaDungTests
{
    private static readonly DateTime Tu = new(2026, 11, 1), Den = new(2026, 11, 11); // 10 ngày = 14.400 phút

    [Fact] public void KhoangChongNhau_ChiTinhMotLan_BRDT05()
    {
        var (pct, dt) = KhaDung.Tinh(Tu, Den, [(Tu.AddHours(1), Tu.AddHours(3)), (Tu.AddHours(2), Tu.AddHours(4)), (Tu.AddDays(5), Tu.AddDays(5).AddMinutes(144))]);
        Assert.Equal(TimeSpan.FromMinutes(180 + 144), dt); Assert.Equal(97.75, pct, 2);
    }

    [Fact] public void CatTheoKy_VaKhongCoDowntime()
    {
        Assert.Equal(TimeSpan.FromHours(2), KhaDung.Tinh(Tu, Den, [(Tu.AddHours(-3), Tu.AddHours(2))]).Downtime);
        Assert.Equal(100, KhaDung.Tinh(Tu, Den, []).PhanTram);
    }

    [Fact] public void SuKienDangDienRa_TinhDenHienTai()
    {
        var s = new SuKienDowntime { Loai = LoaiDowntime.DotXuat, TrangThai = TrangThaiSuKien.DangDienRa, BatDau = Den.AddHours(-2) };
        var k = KhaDung.Khoang(s, Den, true)!.Value; Assert.Equal(Den, k.Item2);
        Assert.Null(KhaDung.Khoang(new SuKienDowntime { BatDau = Tu, KetThuc = Tu.AddHours(1), TrangThai = TrangThaiSuKien.DaHuy }, Den, false));
    }
}

public class SaoLuuTests
{
    private static TacVuSaoLuu Tv(int chuKyGio = 24) => new() { Id = 1, Ten = "t", DoiTuong = "d", ChuKyPhut = chuKyGio * 60, GioChay = new TimeOnly(1, 0) };
    private static KetQuaSaoLuuLan Kq(long id, DateTime t, KetQuaSaoLuu k) => new() { Id = id, ThoiDiem = t, KetQua = k, TacVuSaoLuuId = 1 };

    [Fact] public void Quy321_VaRpo()
    {
        var t = Tv(); Assert.False(t.Dat321);
        t.SoBanSao = 3; t.SoLoaiPhuongTien = 2; t.CoBanNgoaiCoSo = true; Assert.True(t.Dat321);
        t.CoBanNgoaiCoSo = false; Assert.False(t.Dat321);
        Assert.False(Tv(24).DapUngRpo(60)); Assert.True(Tv(1).DapUngRpo(60)); Assert.True(Tv(24).DapUngRpo(1440));
        var log15 = new TacVuSaoLuu { ChuKyPhut = 15 }; Assert.True(log15.DapUngRpo(15)); Assert.False(log15.DapUngRpo(10));
    }

    [Fact] public void LichDuKien_TheoGioChayVaChuKy()
    {
        var m = Tv(24).LichDuKien(new DateTime(2026, 11, 20, 0, 0, 0), new DateTime(2026, 11, 23, 0, 0, 0)).ToList();
        Assert.Equal([new DateTime(2026, 11, 20, 1, 0, 0), new DateTime(2026, 11, 21, 1, 0, 0), new DateTime(2026, 11, 22, 1, 0, 0)], m);
        Assert.Equal(4, Tv(6).LichDuKien(new DateTime(2026, 11, 20, 0, 0, 0), new DateTime(2026, 11, 21, 0, 0, 0)).Count());
    }

    [Fact] public void DoiChieu_ThanhCong_Loi_KhongRo_ChuaDenHan_BRSL01()
    {
        var tu = new DateTime(2026, 11, 20); var now = new DateTime(2026, 11, 24, 12, 0, 0);
        var kq = new List<KetQuaSaoLuuLan> { Kq(1, new DateTime(2026, 11, 20, 1, 10, 0), KetQuaSaoLuu.ThanhCong), Kq(2, new DateTime(2026, 11, 21, 1, 5, 0), KetQuaSaoLuu.Loi),
            Kq(3, new DateTime(2026, 11, 23, 1, 0, 0), KetQuaSaoLuu.CanhBao) };
        var moc = DoiChieuSaoLuu.Moc(Tv(), kq, tu, now.AddDays(1), now, TimeSpan.FromHours(3));
        var tt = moc.Select(m => m.TrangThai).ToList();
        Assert.Equal([TrangThaiMocSaoLuu.ThanhCong, TrangThaiMocSaoLuu.Loi, TrangThaiMocSaoLuu.KhongRo, TrangThaiMocSaoLuu.CanhBao, TrangThaiMocSaoLuu.KhongRo, TrangThaiMocSaoLuu.ChuaDenHan], tt);
        // 4 mốc thành công/cảnh báo trên 5 mốc đã đến hạn... (2 thành công/cảnh báo, 1 lỗi, 2 không rõ) = 2/5
        Assert.Equal(40.0, DoiChieuSaoLuu.TyLeThanhCong(moc));
        Assert.Equal(1, DoiChieuSaoLuu.LoiLienTiep(moc)); // mốc 24/11 không rõ; mốc 23 cảnh báo → chuỗi dừng
    }

    [Fact] public void DoiChieu_LanChayLaiSauLoi_TinhLaDaSaoLuu()
    {
        var kq = new List<KetQuaSaoLuuLan> { Kq(1, new DateTime(2026, 11, 20, 1, 5, 0), KetQuaSaoLuu.Loi), Kq(2, new DateTime(2026, 11, 20, 5, 0, 0), KetQuaSaoLuu.ThanhCong) };
        var moc = DoiChieuSaoLuu.Moc(Tv(), kq, new DateTime(2026, 11, 20), new DateTime(2026, 11, 21), new DateTime(2026, 11, 21, 12, 0, 0), TimeSpan.FromHours(3));
        Assert.Single(moc); Assert.Equal(TrangThaiMocSaoLuu.ThanhCong, moc[0].TrangThai);
    }

    [Fact] public void LichDuKien_ChuKyPhut_Log15Phut() =>
        Assert.Equal(8, new TacVuSaoLuu { ChuKyPhut = 15, GioChay = new TimeOnly(0, 0) }.LichDuKien(new DateTime(2026, 11, 20, 1, 0, 0), new DateTime(2026, 11, 20, 3, 0, 0)).Count());

    [Fact] public void LoiLienTiep_DemKhongRoVaLoi()
    {
        var now = new DateTime(2026, 11, 24, 12, 0, 0);
        var moc = DoiChieuSaoLuu.Moc(Tv(), [Kq(1, new DateTime(2026, 11, 21, 1, 0, 0), KetQuaSaoLuu.ThanhCong)], new DateTime(2026, 11, 21), now, now, TimeSpan.FromHours(3));
        Assert.Equal(3, DoiChieuSaoLuu.LoiLienTiep(moc)); // 22, 23, 24/11 không có kết quả
    }
}

public class ThuPhucHoiTests
{
    private static readonly DateTime T = new(2026, 11, 24, 10, 0, 0);
    private static LanThuPhucHoi L(double rtoPhut, double rpoPhut, bool toanVen = true)
    {
        var l = new LanThuPhucHoi { HeThongId = 1 };
        l.Ghi(1, "Full", T.AddMinutes(-rpoPhut), T, "Máy thử", T, T.AddMinutes(rtoPhut), "Mở được hồ sơ mẫu", "Điều dưỡng trưởng", toanVen);
        return l;
    }

    [Fact] public void Dat_KhiToanVenVaRtoRpoTrongMucTieu()
    {
        var l = L(200, 10); Assert.Equal(200, l.RtoDatPhut); Assert.Equal(10, l.RpoDatPhut);
        Assert.True(l.Chot(240, 15, "ktv", T)); Assert.True(l.DaChot); Assert.Empty(l.LyDoKhongDat());
    }

    [Fact] public void KhongDat_NeuVuotRtoHoacRpoHoacToanVen()
    {
        var a = L(300, 10); Assert.False(a.Chot(240, 15, "k", T)); Assert.Contains(a.LyDoKhongDat(), x => x.StartsWith("RTO"));
        var b = L(100, 30); Assert.False(b.Chot(240, 15, "k", T)); Assert.Contains(b.LyDoKhongDat(), x => x.StartsWith("RPO"));
        var c = L(100, 10, toanVen: false); Assert.False(c.Chot(240, 15, "k", T)); Assert.Contains(c.LyDoKhongDat(), x => x.Contains("toàn vẹn"));
    }

    [Fact] public void DaChot_KhongSua_ChiThemPhuLuc_BRSL03()
    {
        var l = L(100, 5); l.Chot(240, 15, "ktv", T);
        Assert.Throws<DomainException>(() => l.Ghi(1, "x", T, T, "", T, T.AddMinutes(1), "x", "x", true));
        Assert.Throws<DomainException>(() => l.Chot(240, 15, "ktv", T));
        l.ThemPhuLuc("Đính chính tên máy chủ", "admin", T); Assert.Contains("Đính chính", l.PhuLuc);
    }

    [Fact] public void ChotThieuThongTin_BiChan()
    {
        var l = new LanThuPhucHoi { HeThongId = 1 }; Assert.Throws<DomainException>(() => l.Chot(240, 15, "k", T));
        Assert.Throws<DomainException>(() => new LanThuPhucHoi().Ghi(1, "x", T.AddHours(1), T, "m", T, T.AddMinutes(5), "ok", "a", true)); // dữ liệu sau thời điểm sự cố
        Assert.Throws<DomainException>(() => new LanThuPhucHoi().Ghi(1, "x", T, T, "m", T, T.AddMinutes(-5), "ok", "a", true)); // hoàn tất trước bắt đầu
    }
}

public class DienTapTests
{
    private static readonly DateTime T = new(2026, 11, 24, 10, 0, 0);

    [Fact] public void Chot_CanBienBan_VaPhatHienKhiKhongDat()
    {
        var d = new DotDienTap { HeThongId = 1 };
        Assert.Throws<DomainException>(() => d.Chot(KetLuanDienTap.Dat, "a", T)); // chưa ghi biên bản
        d.Ghi(T, HinhThucDienTap.TrenBan, "kb", "CC", "qs", "9:00 ngừng; 9:30 khôi phục", null);
        Assert.Throws<DomainException>(() => d.Chot(KetLuanDienTap.KhongDat, "a", T)); // thiếu phát hiện
        d.PhatHien = "Chưa biết vị trí biểu mẫu giấy";
        Assert.True(d.Chot(KetLuanDienTap.KhongDat, "a", T)); // true = cần diễn tập lại
        Assert.Throws<DomainException>(() => d.Ghi(T, HinhThucDienTap.MoPhong, null, null, null, "sửa", null));
        d.ThemPhuLuc("Bổ sung", "a", T); Assert.Contains("Bổ sung", d.PhuLuc);
    }

    [Fact] public void Dat_KhongCanDienTapLai() { var d = new DotDienTap(); d.Ghi(T, HinhThucDienTap.NgungThat, null, null, null, "ok", null); Assert.False(d.Chot(KetLuanDienTap.Dat, "a", T)); }

    [Fact] public void ThayBangDowntimeThucTe_CanDaDongVaCoRutKinhNghiem_FRDT12()
    {
        var d = new DotDienTap { HeThongId = 5 };
        var s = new SuKienDowntime { Id = 9, TrangThai = TrangThaiSuKien.DaDong, NgayRutKinhNghiem = T, BatDau = T, CanCaiThien = "x", HeThongs = [new() { HeThongId = 5 }] };
        d.ThayBang(s, "admin", T); Assert.True(d.DaChot); Assert.Equal(9, d.ThayBangSuKienId);
        var khac = new DotDienTap { HeThongId = 6 }; Assert.Throws<DomainException>(() => khac.ThayBang(s, "a", T)); // không ảnh hưởng hệ thống này
        var chuaRkn = new SuKienDowntime { TrangThai = TrangThaiSuKien.DaDong, HeThongs = [new() { HeThongId = 5 }] };
        Assert.Throws<DomainException>(() => new DotDienTap { HeThongId = 5 }.ThayBang(chuaRkn, "a", T));
    }

    [Fact] public void QuaHan_ChiKhiChuaChot() { var d = new DotDienTap { HanThucHien = new DateOnly(2026, 11, 1) }; Assert.True(d.QuaHan(new DateOnly(2026, 11, 2))); d.DaChot = true; Assert.False(d.QuaHan(new DateOnly(2026, 11, 2))); }
}

public class HanhDongKhacPhucTests
{
    private static readonly DateTime T = new(2026, 11, 24, 10, 0, 0);
    private static HanhDongKhacPhuc H() => new() { Ma = "HD-1", MoTa = "m", NguoiChiuTrachNhiem = "ktv1", Han = new DateOnly(2026, 12, 10) };

    [Fact] public void VongDoi_Mo_DangLam_ChoKiemChung_DaDong()
    {
        var h = H(); h.BatDau("ktv1"); Assert.Equal(TrangThaiHanhDong.DangLam, h.TrangThai);
        Assert.Throws<DomainException>(() => h.BaoCaoXong(" ", T));
        h.BaoCaoXong("Đã thay ắc quy", T); Assert.Equal(TrangThaiHanhDong.ChoKiemChung, h.TrangThai);
        h.KiemChung("admin", true, "Test chuyển nguồn đạt 25 phút", T); Assert.Equal(TrangThaiHanhDong.DaDong, h.TrangThai);
    }

    [Fact] public void KiemChung_NguoiThucHienKhongTuKiemChung_FRHD04()
    {
        var h = H(); h.BaoCaoXong("xong", T);
        Assert.Throws<DomainException>(() => h.KiemChung("KTV1", true, "tự xác nhận", T)); // không phân biệt hoa thường
        Assert.Throws<DomainException>(() => h.KiemChung("admin", true, "", T)); // cần bằng chứng
    }

    [Fact] public void KiemChungChuaHieuQua_MoLaiVaGhiLichSu()
    {
        var h = H(); h.BaoCaoXong("xong", T); h.KiemChung("admin", false, "Pin vẫn sụt nhanh", T);
        Assert.Equal(TrangThaiHanhDong.DangLam, h.TrangThai); Assert.Null(h.NgayHoanThanh); Assert.Contains("chưa hiệu quả", h.LichSuGiaHan);
    }

    [Fact] public void GiaHan_CanLyDo_HanMoiSauHanCu_NguoiDuyetKhacNguoiLam_FRHD03()
    {
        var h = H();
        Assert.Throws<DomainException>(() => h.GiaHan(new DateOnly(2026, 12, 20), "", "admin", T));
        Assert.Throws<DomainException>(() => h.GiaHan(new DateOnly(2026, 12, 1), "chờ vật tư", "admin", T));
        Assert.Throws<DomainException>(() => h.GiaHan(new DateOnly(2026, 12, 20), "chờ vật tư", "ktv1", T));
        h.CapLeoThang = 2; h.GiaHan(new DateOnly(2026, 12, 20), "chờ vật tư", "admin", T);
        Assert.Equal(new DateOnly(2026, 12, 20), h.Han); Assert.Equal(0, h.CapLeoThang); Assert.Contains("chờ vật tư", h.LichSuGiaHan);
    }

    [Fact] public void QuaHan_ChiVoiMoVaDangLam()
    {
        var h = H(); var sau = new DateOnly(2026, 12, 11); Assert.True(h.QuaHan(sau));
        h.BaoCaoXong("x", T); Assert.False(h.QuaHan(sau));
    }
}
