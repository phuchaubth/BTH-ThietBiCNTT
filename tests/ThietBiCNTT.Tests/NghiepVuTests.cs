using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Tests;

public class SlaTests
{
    private static readonly SlaThamSo Ts = SlaThamSo.MacDinh; // 2/8/24/72h, hệ số 0.5, 7:00–17:00, T2–T7

    [Fact] // Thứ Bảy 21/11/2026 16:00, khẩn cấp khoa thường: 1h hôm nay + CN nghỉ + 1h sáng T2
    public void KhanCap_KhoaThuong_QuaNgayNghi() =>
        Assert.Equal(new DateTime(2026, 11, 23, 8, 0, 0), SlaCalculator.TinhHan(new DateTime(2026, 11, 21, 16, 0, 0), MucDoUuTien.KhanCap, false, Ts));

    [Fact] // Khoa trọng yếu 24/7, hạn = 2h × 0.5 = 1h
    public void KhanCap_KhoaTrongYeu_24h() =>
        Assert.Equal(new DateTime(2026, 11, 21, 17, 0, 0), SlaCalculator.TinhHan(new DateTime(2026, 11, 21, 16, 0, 0), MucDoUuTien.KhanCap, true, Ts));

    [Fact] // Báo ngoài giờ (22:00 T3) → bắt đầu tính từ 7:00 T4
    public void BaoNgoaiGio_TinhTuSangHomSau() =>
        Assert.Equal(new DateTime(2026, 11, 18, 15, 0, 0), SlaCalculator.TinhHan(new DateTime(2026, 11, 17, 22, 0, 0), MucDoUuTien.Cao, false, Ts));

    [Fact]
    public void NgayLe_KhongTinh()
    {
        var ts = Ts with { NgayNghi = new HashSet<DateOnly> { new(2026, 11, 18) } };
        // T3 16:00 + 2h: 1h T3, T4 nghỉ lễ, 1h T5
        Assert.Equal(new DateTime(2026, 11, 19, 8, 0, 0), SlaCalculator.TinhHan(new DateTime(2026, 11, 17, 16, 0, 0), MucDoUuTien.KhanCap, false, ts));
    }

    [Fact]
    public void GioLamViecGiua_TruNgoaiGio() =>
        Assert.Equal(TimeSpan.FromHours(11), SlaCalculator.GioLamViecGiua(new DateTime(2026, 11, 17, 8, 0, 0), new DateTime(2026, 11, 18, 9, 0, 0), Ts));

    [Fact] // Tạm dừng lúc 9:00 khi hạn là 15:00 (còn 6h làm việc), tiếp tục 10:00 hôm sau → hạn 16:00 hôm sau
    public void TamDung_DoiHan_GioHanhChinh() =>
        Assert.Equal(new DateTime(2026, 11, 18, 16, 0, 0),
            SlaCalculator.DoiHanSauTamDung(new DateTime(2026, 11, 17, 15, 0, 0), new DateTime(2026, 11, 17, 9, 0, 0), new DateTime(2026, 11, 18, 10, 0, 0), false, Ts));
}

public class PhieuSuaChuaTests
{
    private static PhieuSuaChua Phieu() => new()
    {
        SoPhieu = "SC-1", TaiSanId = 1, KhoaPhongId = 7, NguoiTao = "nhanvien", MoTaSuCo = "Hỏng",
        NgayYeuCau = new DateTime(2026, 11, 17, 8, 0, 0), HanXuLy = new DateTime(2026, 11, 17, 16, 0, 0)
    };

    [Fact]
    public void TamDung_KhongQuaHan_VaDoiHanKhiTiepTuc()
    {
        var p = Phieu();
        p.TiepNhan("ktv1", HinhThucXuLy.TuSua, new DateTime(2026, 11, 17, 8, 10, 0));
        p.TamDung(TrangThaiPhieuSuaChua.ChoLinhKien, new DateTime(2026, 11, 17, 10, 0, 0));
        Assert.False(p.QuaHan(new DateTime(2026, 11, 20, 0, 0, 0)));
        p.BatDauXuLy(new DateTime(2026, 11, 17, 12, 0, 0), (han, bd, tiep) => han + (tiep - bd));
        Assert.Equal(new DateTime(2026, 11, 17, 18, 0, 0), p.HanXuLy);
        Assert.Equal(7200, p.TongTamDungGiay);
        Assert.False(p.DangTamDung);
    }

    [Fact]
    public void NghiemThu_ChiNguoiBaoHoacTruongKhoa_KhongPhaiKtv()
    {
        var p = Phieu();
        p.TiepNhan("ktv1", HinhThucXuLy.TuSua, DateTime.Now);
        Assert.True(p.DuocNghiemThu("nhanvien", false, 7));
        Assert.True(p.DuocNghiemThu("truongkhoacc", true, 7));
        Assert.False(p.DuocNghiemThu("truongkhoakhac", true, 9));
        Assert.False(p.DuocNghiemThu("ktv1", false, 1));
        Assert.False(p.DuocNghiemThu("nguoikhac", false, 7));
    }

    [Fact]
    public void TuDongNghiemThu_SauNNgay()
    {
        var p = Phieu();
        p.TiepNhan("ktv1", HinhThucXuLy.TuSua, p.NgayYeuCau);
        p.HoanThanh(null, "Đã sửa", 1, 0, new DateTime(2026, 11, 17, 10, 0, 0));
        Assert.False(p.TuDongNghiemThu(3, new DateTime(2026, 11, 19, 10, 0, 0)));
        Assert.True(p.TuDongNghiemThu(3, new DateTime(2026, 11, 20, 10, 0, 0)));
        Assert.Equal(TrangThaiPhieuSuaChua.DaNghiemThu, p.TrangThai);
    }

    [Fact]
    public void HoanThanh_BatBuocBienPhap() =>
        Assert.Throws<DomainException>(() => { var p = Phieu(); p.TiepNhan("ktv1", HinhThucXuLy.TuSua, DateTime.Now); p.HoanThanh(null, " ", 0, 0, DateTime.Now); });
}

public class DieuChuyenTests
{
    private static PhieuDieuChuyen Phieu(decimal gia, int? tuKhoa = 1) => new()
    {
        SoPhieu = "DC-1", TuKhoaPhongId = tuKhoa, DenKhoaPhongId = 2, LyDo = "x", NguoiTao = "nguoilap",
        ChiTiets = [new ChiTietDieuChuyen { TaiSanId = 1, TaiSan = new TaiSan { Id = 1, MaTaiSan = "PC-1", NguyenGia = gia, KhoaPhongId = tuKhoa } }]
    };

    [Fact]
    public void DuoiNguong_HaiCap()
    {
        var p = Phieu(15_000_000);
        p.GuiDuyet(30_000_000);
        Assert.Equal([1, 2], p.CacCap());
        p.Duyet("tk", "TK", true, null, DateTime.Now);
        Assert.Equal(PhieuDieuChuyen.CapCNTT, p.CapHienTai);
        p.Duyet("cntt", "CNTT", true, null, DateTime.Now);
        Assert.Equal(TrangThaiDieuChuyen.DaDuyet, p.TrangThai);
    }

    [Fact]
    public void TuNguongTroLen_BaCap_VaBanGiao()
    {
        var p = Phieu(30_000_000);
        p.GuiDuyet(30_000_000);
        Assert.Equal([1, 2, 3], p.CacCap());
        p.Duyet("tk", "TK", true, null, DateTime.Now);
        p.Duyet("cntt", "CNTT", true, null, DateTime.Now);
        Assert.Equal(TrangThaiDieuChuyen.ChoDuyet, p.TrangThai);
        p.Duyet("ld", "LĐ", true, null, DateTime.Now);
        p.BanGiao(DateTime.Now);
        Assert.Equal(2, p.ChiTiets.First().TaiSan!.KhoaPhongId);
    }

    [Fact]
    public void TuKho_BoQuaCapTruongKhoa()
    {
        var p = Phieu(1_000_000, tuKhoa: null);
        p.GuiDuyet(30_000_000);
        Assert.Equal(PhieuDieuChuyen.CapCNTT, p.CapHienTai);
    }

    [Fact]
    public void KhongTuDuyet_VaTuChoiPhaiCoLyDo()
    {
        var p = Phieu(1_000_000);
        p.GuiDuyet(30_000_000);
        Assert.Throws<DomainException>(() => p.Duyet("nguoilap", "", true, null, DateTime.Now));
        Assert.Throws<DomainException>(() => p.Duyet("tk", "", false, null, DateTime.Now));
        p.Duyet("tk", "", false, "Không đồng ý", DateTime.Now);
        Assert.Equal(TrangThaiDieuChuyen.TuChoi, p.TrangThai);
    }
}

public class ThanhLyTests
{
    private static DeXuatThanhLy DeXuat()
    {
        var loai = new LoaiThietBi { CoLuuTruDuLieu = true };
        var d = new DeXuatThanhLy { SoPhieu = "TL-1", LyDo = "x",
            ChiTiets = [new ChiTietThanhLy { Id = 1, TaiSan = new TaiSan { MaTaiSan = "PC-9", LoaiThietBi = loai } }] };
        d.GuiHoiDong(); d.DanhGia("HĐ", "Đồng ý", true, DateTime.Now); d.PheDuyet("215/QĐ-BV", new DateOnly(2026, 11, 17), HinhThucThanhLy.BanTrucTiep);
        return d;
    }

    [Fact]
    public void ChanHoanTat_KhiChuaThaoOCung()
    {
        var d = DeXuat();
        Assert.Throws<DomainException>(() => d.HoanTat(0, DateTime.Now));
        d.ChiTiets.First().GhiNhanThaoOCung("WD-123", "Kho CNTT", "ktv1", DateTime.Now);
        d.HoanTat(100_000, DateTime.Now);
        Assert.Equal(TrangThaiTaiSan.DaThanhLy, d.ChiTiets.First().TaiSan!.TrangThai);
    }

    [Fact]
    public void ThamSoTatBatBuoc_ChoPhepHoanTat()
    {
        var d = DeXuat();
        d.HoanTat(0, DateTime.Now, batBuocThaoOCung: false);
        Assert.Equal(TrangThaiThanhLy.DaThanhLy, d.TrangThai);
    }
}

public class KiemKeTests
{
    private static (DotKiemKe Dot, TaiSan A, TaiSan B) Dot()
    {
        var a = new TaiSan { Id = 1, MaTaiSan = "PC-1", KhoaPhongId = 10 };
        var b = new TaiSan { Id = 2, MaTaiSan = "PC-2", KhoaPhongId = 20 };
        var d = new DotKiemKe { Ma = "KK-1", Ten = "x" };
        d.ChupSoSach([a, b]);
        return (d, a, b);
    }

    [Fact]
    public void Quet_Khop_SaiViTri_Thua_VaKhoaSo()
    {
        var (d, a, b) = Dot();
        Assert.Equal(KetQuaKiemKe.Khop, d.GhiNhan("pc-1", a, 10, null, "u", DateTime.Now).ChiTiet.KetQua);
        Assert.True(d.GhiNhan("PC-1", a, 10, null, "u", DateTime.Now).DaKiemTruoc);
        var la = new TaiSan { Id = 3, MaTaiSan = "PC-3", KhoaPhongId = 30 };
        Assert.Equal(KetQuaKiemKe.Thua, d.GhiNhan("PC-3", la, 10, null, "u", DateTime.Now).ChiTiet.KetQua);
        Assert.Equal(KetQuaKiemKe.Thua, d.GhiNhan("XYZ", null, 10, null, "u", DateTime.Now).ChiTiet.KetQua);
        d.KhoaSo(DateTime.Now);
        var tk = d.ThongKe();
        Assert.Equal((1, 0, 2, 1), (tk[KetQuaKiemKe.Khop], tk[KetQuaKiemKe.SaiViTri], tk[KetQuaKiemKe.Thua], tk[KetQuaKiemKe.Thieu]));
        Assert.Throws<DomainException>(() => d.GhiNhan("PC-2", b, 20, null, "u", DateTime.Now));
    }

    [Fact]
    public void Quet_SaiViTri_VaGioiHanPhamViKhoa()
    {
        var (d, _, b) = Dot();
        Assert.Equal(KetQuaKiemKe.SaiViTri, d.GhiNhan("PC-2", b, 10, null, "u", DateTime.Now).ChiTiet.KetQua);
        var dk = new DotKiemKe { Ma = "KK-2", Ten = "x", KhoaPhongId = 10 };
        Assert.Throws<DomainException>(() => dk.GhiNhan("PC-2", b, 20, null, "u", DateTime.Now));
    }

    [Theory]
    [InlineData("https://thietbi.bth.org.vn/qr/PC-0012", "PC-0012")]
    [InlineData("http://10.0.0.5/qr/pc%2D0012?x=1", "PC-0012")]
    [InlineData("  mh-0004 ", "MH-0004")]
    public void TachMa_TuNoiDungQr(string vao, string ra) => Assert.Equal(ra, ThietBiCNTT.Web.Controllers.KiemKeController.TachMa(vao));
}

public class HangDoiTests
{
    [Fact]
    public void LoiThuLaiLuyThua_RoiDanhDauLoi()
    {
        var now = new DateTime(2026, 11, 22, 8, 0, 0);
        var t = new TinNhanCho { Kenh = KenhGui.Email, DiaChi = "a@b.vn", GuiTu = now };
        t.DanhDauLoi("x", now); Assert.Equal(now.AddMinutes(1), t.GuiTu);
        t.DanhDauLoi("x", now); Assert.Equal(now.AddMinutes(2), t.GuiTu);
        t.DanhDauLoi("x", now); t.DanhDauLoi("x", now);
        Assert.Equal(TrangThaiGui.ChoGui, t.TrangThai);
        t.DanhDauLoi("x", now);
        Assert.Equal(TrangThaiGui.Loi, t.TrangThai);
        t.GuiLai(now);
        Assert.Equal((TrangThaiGui.ChoGui, 0), (t.TrangThai, t.SoLanThu));
    }
}

public class KiemTraTests
{
    private static MauKiemTra Mau() => new()
    {
        Id = 1, Ten = "Phòng máy chủ",
        Mucs = [new MucKiemTra { Id = 1, ThuTu = 1, NoiDung = "Nhiệt độ", Kieu = KieuMucKiemTra.So, DonVi = "°C", NguongMin = 18, NguongMax = 27 },
                new MucKiemTra { Id = 2, ThuTu = 2, NoiDung = "Điều hoà", Kieu = KieuMucKiemTra.DatKhongDat },
                new MucKiemTra { Id = 3, ThuTu = 3, NoiDung = "Ghi chú", Kieu = KieuMucKiemTra.VanBan, BatBuoc = false }]
    };

    [Fact]
    public void TrongNguong_Dat_NgoaiNguong_BatThuong()
    {
        var ok = Mau().ChamDiem(new Dictionary<int, string?> { [1] = "22,5", [2] = "true" }, null, "u", DateTime.Now, null);
        Assert.False(ok.CoBatThuong);
        var nong = Mau().ChamDiem(new Dictionary<int, string?> { [1] = "29", [2] = "true" }, null, "u", DateTime.Now, null);
        Assert.True(nong.CoBatThuong);
        Assert.Equal("Nhiệt độ: 29 °C", nong.TomTatBatThuong());
        var hong = Mau().ChamDiem(new Dictionary<int, string?> { [1] = "20", [2] = "false" }, null, "u", DateTime.Now, null);
        Assert.Equal("Điều hoà: Không đạt", hong.TomTatBatThuong());
    }

    [Fact]
    public void ThieuMucBatBuoc_HoacSoSai_BaoLoi()
    {
        Assert.Throws<DomainException>(() => Mau().ChamDiem(new Dictionary<int, string?> { [1] = "20" }, null, "u", DateTime.Now, null));
        Assert.Throws<DomainException>(() => Mau().ChamDiem(new Dictionary<int, string?> { [1] = "abc", [2] = "true" }, null, "u", DateTime.Now, null));
        var m = Mau(); m.LoaiThietBiId = 5;
        Assert.Throws<DomainException>(() => m.ChamDiem(new Dictionary<int, string?> { [1] = "20", [2] = "true" }, null, "u", DateTime.Now, null));
    }
}

public class BanQuyenHopDongTests
{
    [Fact]
    public void CapPhat_KhongVuotSoLuong_KhongTrung()
    {
        var b = new BanQuyenPhanMem { Ten = "Office", SoLuong = 2 };
        TaiSan T(int id) => new() { Id = id, MaTaiSan = "PC-" + id };
        b.CapPhat(T(1), "u", DateTime.Now); b.CapPhat(T(2), "u", DateTime.Now);
        Assert.Throws<DomainException>(() => b.CapPhat(T(3), "u", DateTime.Now));
        Assert.Throws<DomainException>(() => new BanQuyenPhanMem { SoLuong = 5, CapPhats = [new CapPhatBanQuyen { TaiSanId = 1 }] }.CapPhat(T(1), "u", DateTime.Now));
        var mienPhi = new BanQuyenPhanMem { Loai = LoaiGiayPhep.MienPhi, SoLuong = 0 };
        mienPhi.CapPhat(T(9), "u", DateTime.Now);
        Assert.Equal(1, mienPhi.DaDung);
    }

    [Fact]
    public void HopDong_HieuLuc()
    {
        var d = new DateOnly(2026, 11, 22);
        var h = new HopDong { HieuLucTu = new(2026, 1, 1), HieuLucDen = new(2026, 12, 15) };
        Assert.True(h.ConHieuLuc(d)); Assert.True(h.SapHetHan(d, 30)); Assert.False(h.SapHetHan(d, 10)); Assert.False(h.DaHetHan(d));
    }

    [Fact]
    public void GuiHang_GhiNgayGui_NhanVe_VaTreHen()
    {
        var p = new PhieuSuaChua { SoPhieu = "SC", NgayYeuCau = DateTime.Now, HanXuLy = DateTime.Now.AddHours(8) };
        p.TiepNhan("ktv", HinhThucXuLy.BaoHanh, DateTime.Now);
        p.TamDung(TrangThaiPhieuSuaChua.GuiHang, DateTime.Now);
        p.NgayHenTra = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        Assert.NotNull(p.NgayGuiHang); Assert.True(p.TreHenTra(DateOnly.FromDateTime(DateTime.Today)));
        p.BatDauXuLy(DateTime.Now);
        Assert.NotNull(p.NgayNhanVe); Assert.False(p.TreHenTra(DateOnly.FromDateTime(DateTime.Today)));
    }
}
