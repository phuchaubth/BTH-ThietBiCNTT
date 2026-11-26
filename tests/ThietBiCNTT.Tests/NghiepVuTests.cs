using Microsoft.Extensions.Configuration;
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

public class KhoTests
{
    [Fact]
    public void GiaBinhQuanGiaQuyen()
    {
        var v = new VatTu { TonKho = 4, DonGia = 650_000 };
        v.CapNhatGiaBinhQuan(10, 750_000);
        Assert.Equal(721_429, v.DonGia);
        var moi = new VatTu { TonKho = 0, DonGia = 0 };
        moi.CapNhatGiaBinhQuan(5, 100_000);
        Assert.Equal(100_000, moi.DonGia);
    }

    [Theory]
    [InlineData(LyDoPhieuKho.NhapMua, LoaiPhieuKho.Nhap)]
    [InlineData(LyDoPhieuKho.XuatSuaChua, LoaiPhieuKho.Xuat)]
    [InlineData(LyDoPhieuKho.ChuyenKho, LoaiPhieuKho.Chuyen)]
    [InlineData(LyDoPhieuKho.KiemKe, LoaiPhieuKho.DieuChinh)]
    public void LoaiPhieuTheoLyDo(LyDoPhieuKho lyDo, LoaiPhieuKho loai) => Assert.Equal(loai, PhieuKho.LoaiCua(lyDo));

    [Fact]
    public void PhieuKho_KiemTraHopLe()
    {
        PhieuKho P(LyDoPhieuKho l, params ChiTietPhieuKho[] c) => new() { LyDo = l, Loai = PhieuKho.LoaiCua(l), KhoId = 1, ChiTiets = c.ToList() };
        Assert.Throws<DomainException>(() => P(LyDoPhieuKho.NhapMua).KiemTraHopLe());
        Assert.Throws<DomainException>(() => P(LyDoPhieuKho.NhapMua, new ChiTietPhieuKho { VatTuId = 1, SoLuong = -1 }).KiemTraHopLe());
        Assert.Throws<DomainException>(() => P(LyDoPhieuKho.ChuyenKho, new ChiTietPhieuKho { VatTuId = 1, SoLuong = 1 }).KiemTraHopLe());
        Assert.Throws<DomainException>(() => P(LyDoPhieuKho.NhapMua, new ChiTietPhieuKho { VatTuId = 1, SoLuong = 1 }, new ChiTietPhieuKho { VatTuId = 1, SoLuong = 2 }).KiemTraHopLe());
        P(LyDoPhieuKho.KiemKe, new ChiTietPhieuKho { VatTuId = 1, SoLuong = -3 }).KiemTraHopLe();
    }
}

public class MuaSamTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 9, 0, 0);
    private static DeXuatMuaSam Dx(decimal donGia, int sl = 2) => new()
    {
        NguoiTao = "ktv1", TieuDe = "Mua",
        Dongs = [new DongDeXuat { Loai = LoaiHangMua.VatTu, VatTuId = 1, SoLuong = sl, DonGiaDuKien = donGia }]
    };

    [Fact]
    public void DuoiNguong_KhongCapVatTu_MotCap()
    {
        var dx = Dx(1_000_000);
        dx.GuiDuyet(50_000_000, coCapVatTu: false);
        Assert.Equal([DeXuatMuaSam.CapCNTT], dx.CacCap());
        dx.Duyet("admin", "Admin", true, null, Now);
        Assert.Equal(TrangThaiDeXuat.DaDuyet, dx.TrangThai);
    }

    [Fact]
    public void TuNguong_BaCap_CoVatTu()
    {
        var dx = Dx(30_000_000);
        dx.GuiDuyet(50_000_000, coCapVatTu: true);
        Assert.Equal([1, 2, 3], dx.CacCap());
        dx.Duyet("admin", "A", true, null, Now);
        Assert.Equal(DeXuatMuaSam.CapVatTu, dx.CapHienTai);
        Assert.Throws<DomainException>(() => dx.Duyet("admin", "A", true, null, Now)); // một người không duyệt hai cấp
        dx.Duyet("vattu", "V", true, null, Now);
        dx.Duyet("lanhdao", "L", true, null, Now);
        Assert.Equal(TrangThaiDeXuat.DaDuyet, dx.TrangThai);
    }

    [Fact]
    public void TuNguong_KhongCapVatTu_BoQuaCap2()
    {
        var dx = Dx(30_000_000);
        dx.GuiDuyet(50_000_000, coCapVatTu: false);
        Assert.Equal([1, 3], dx.CacCap());
        dx.Duyet("admin", "A", true, null, Now);
        Assert.Equal(DeXuatMuaSam.CapLanhDao, dx.CapHienTai);
    }

    [Fact]
    public void KhongTuDuyet_TuChoiCanLyDo_GuiLai()
    {
        var dx = Dx(100_000);
        Assert.Throws<DomainException>(() => new DeXuatMuaSam { NguoiTao = "a" }.GuiDuyet(1, false)); // không có dòng
        dx.GuiDuyet(50_000_000, false);
        Assert.Throws<DomainException>(() => dx.Duyet("ktv1", "K", true, null, Now));
        Assert.Throws<DomainException>(() => dx.Duyet("admin", "A", false, " ", Now));
        dx.Duyet("admin", "A", false, "Chưa cần", Now);
        Assert.Equal(TrangThaiDeXuat.TuChoi, dx.TrangThai);
        dx.GuiDuyet(50_000_000, false);              // gửi lại sau khi sửa
        dx.Duyet("admin", "A", true, null, Now);     // lượt mới — được duyệt lại
        Assert.Equal(TrangThaiDeXuat.DaDuyet, dx.TrangThai);
    }

    [Fact]
    public void TrangThaiTheoDatHang_VaNhanHang()
    {
        var dx = Dx(100_000, 4);
        dx.GuiDuyet(50_000_000, false); dx.Duyet("admin", "A", true, null, Now);
        dx.Dongs[0].SoLuongDaDat = 1; dx.CapNhatTheoDonHang();
        Assert.Equal(TrangThaiDeXuat.DatMotPhan, dx.TrangThai);
        dx.Dongs[0].SoLuongDaDat = 4; dx.CapNhatTheoDonHang();
        Assert.Equal(TrangThaiDeXuat.DaDatDu, dx.TrangThai);

        var dh = new DonMuaHang { ThueSuat = 10, HanGiao = new DateOnly(2026, 11, 20), Dongs = [new DongDonHang { SoLuong = 4, DonGia = 100_000 }] };
        Assert.Equal(440_000, dh.TongTien);
        Assert.False(dh.TreHan(new DateOnly(2026, 11, 24))); // nháp không tính trễ
        dh.DatHang(Now);
        Assert.True(dh.TreHan(new DateOnly(2026, 11, 24)));
        dh.Dongs[0].SoLuongDaNhan = 1; dh.CapNhatTheoNhanHang();
        Assert.Equal(TrangThaiDonHang.NhanMotPhan, dh.TrangThai);
        dh.Dongs[0].SoLuongDaNhan = 4; dh.CapNhatTheoNhanHang();
        Assert.Equal(TrangThaiDonHang.DaNhanDu, dh.TrangThai);
        Assert.False(dh.TreHan(new DateOnly(2026, 11, 24)));
    }

    [Fact]
    public void DongThietBi_PhaiCoLoaiVaMoTa()
    {
        var dx = new DeXuatMuaSam { Dongs = [new DongDeXuat { Loai = LoaiHangMua.ThietBi, SoLuong = 1, LoaiThietBiId = 1 }] };
        Assert.Throws<DomainException>(dx.KiemTraDong);
        dx.Dongs[0].MoTa = "PC Core i5";
        dx.KiemTraDong();
    }
}

public class NganSachTests
{
    private static readonly DateTime Now = new(2026, 11, 24, 9, 0, 0);

    [Fact]
    public void PhanBoQuy_PhaiBangDuToan_VaKhongTuDuyet()
    {
        var k = new KhoanMucNganSach { Ma = "VT", DuToan = 100, Quy1 = 40, Quy2 = 40, NguoiTao = "ktv1" };
        Assert.Throws<DomainException>(() => k.Duyet("lanhdao", Now)); // 80 ≠ 100
        k.Quy3 = 20;
        Assert.Throws<DomainException>(() => k.Duyet("ktv1", Now));
        k.Duyet("lanhdao", Now);
        Assert.True(k.DungDuoc);
    }

    [Fact]
    public void DieuChinh_CanDuyet_KhongAm_KhongTuDuyet()
    {
        var k = new KhoanMucNganSach { DuToan = 100, NguoiTao = "a" };
        Assert.Throws<DomainException>(() => k.DeNghiDieuChinh(10, "x", "a", Now)); // chưa duyệt
        k.Duyet("b", Now);
        Assert.Throws<DomainException>(() => k.DeNghiDieuChinh(-150, "giảm", "a", Now));
        Assert.Throws<DomainException>(() => k.DeNghiDieuChinh(50, " ", "a", Now));
        var d = k.DeNghiDieuChinh(50, "Bổ sung", "a", Now);
        Assert.Equal(100, k.DuToanHieuLuc); // chưa duyệt thì chưa có hiệu lực
        Assert.Throws<DomainException>(() => k.DuyetDieuChinh(d, true, "a", Now));
        k.DuyetDieuChinh(d, true, "b", Now);
        Assert.Equal(150, k.DuToanHieuLuc);
        Assert.Throws<DomainException>(() => k.DuyetDieuChinh(d, true, "b", Now));
        k.KhoaSo();
        Assert.Throws<DomainException>(() => k.DeNghiDieuChinh(1, "x", "a", Now));
    }

    [Fact]
    public void SuDung_ConLai_VaTyLe()
    {
        var k = new KhoanMucNganSach { DuToan = 1000 };
        var sd = new SuDungNganSach(k, 100, 200, 300, 50, 50);
        Assert.Equal(300, sd.CamKet);
        Assert.Equal(400, sd.ThucChi);
        Assert.Equal(300, sd.ConLai);
        Assert.Equal(70, sd.TyLe);
        Assert.Equal(999, new SuDungNganSach(new KhoanMucNganSach(), 0, 0, 1, 0, 0).TyLe);
    }
}

public class TichHopTests
{
    [Theory]
    [InlineData(null, "8.8.8.8", true)]
    [InlineData("10.10.0.0/16", "10.10.5.7", true)]
    [InlineData("10.10.0.0/16", "10.11.0.1", false)]
    [InlineData("10.10.1.5, 192.168.1.0/24", "192.168.1.200", true)]
    [InlineData("10.10.1.5", "10.10.1.6", false)]
    [InlineData("10.0.0.0/8", "::ffff:10.1.2.3", true)]
    [InlineData("10.10.1.128/25", "10.10.1.100", false)]
    public void IpChoPhep_CidrVaDiaChiDon(string? ds, string ip, bool ok) =>
        Assert.Equal(ok, new KhoaApi { IpChoPhep = ds }.IpHopLe(System.Net.IPAddress.Parse(ip)));

    [Fact]
    public void KiemTraDanhSachIp_BaoLoi() 
    {
        Assert.Throws<DomainException>(() => KhoaApi.KiemTraDanhSachIp("10.0.0.0/99"));
        Assert.Throws<DomainException>(() => KhoaApi.KiemTraDanhSachIp("abc"));
        Assert.Equal("10.0.0.1, 10.1.0.0/16", KhoaApi.KiemTraDanhSachIp(" 10.0.0.1 ,10.1.0.0/16 "));
    }

    [Fact]
    public void KhoaHetHan_VaThuHoi()
    {
        var h = new DateOnly(2026, 11, 24);
        Assert.True(new KhoaApi { HetHan = h }.CoHieuLuc(h));
        Assert.False(new KhoaApi { HetHan = h.AddDays(-1) }.CoHieuLuc(h));
        Assert.False(new KhoaApi { DangHoatDong = false }.CoHieuLuc(h));
    }

    [Fact]
    public void Webhook_ThuLaiLuyThua_ChuKyHmac()
    {
        var now = new DateTime(2026, 11, 24, 9, 0, 0);
        var g = new WebhookGui();
        g.GhiLoi("timeout", null, now); Assert.Equal(now.AddMinutes(1), g.GuiTu);
        g.GhiLoi("500", 500, now); Assert.Equal(now.AddMinutes(2), g.GuiTu);
        for (var i = 0; i < 6; i++) g.GhiLoi("x", 500, now);
        Assert.Equal(TrangThaiGuiWebhook.Loi, g.TrangThai);
        // Vector kiểm chứng HMAC-SHA256 (RFC 4231 test case 2)
        Assert.Equal("sha256=5bdcc146bf60754e6a042426089575c75a003f089d2739839dec58b964ec3843",
            ThietBiCNTT.Infrastructure.Services.TichHopService.Ky("Jefe", "what do ya want for nothing?"));
        Assert.True(new Webhook { SuKien = "phieu.tao,phieu.dong" }.Nhan("phieu.dong"));
        Assert.False(new Webhook { SuKien = "phieu.tao", DangHoatDong = false }.Nhan("phieu.tao"));
    }
}

public class AgentTests
{
    [Fact]
    public void DoiChieuChuanHis()
    {
        var c = new ChuanCauHinh(8, 20, ["Windows 10", "Windows 11"], ["HIS Client"]);
        var m = new MayTramAgent { RamGb = 8, OCungTrongGb = 15, HeDieuHanh = "Microsoft Windows 11 Pro (10.0.22631)", PhanMem = "HIS Client 5|5.2\nZalo|24" };
        var kq = m.DoiChieu(c);
        Assert.Equal(4, kq.Count);
        Assert.True(kq[0].Dat); Assert.False(kq[1].Dat); Assert.True(kq[2].Dat); Assert.True(kq[3].Dat);
        Assert.False(new MayTramAgent { HeDieuHanh = "Windows 7" }.DoiChieu(c)[2].Dat);
        Assert.Empty(m.DoiChieu(new ChuanCauHinh(0, 0, [], [])));
    }

    [Fact]
    public void SoSanhPhanMem_TheoTen_KhongPhanBietHoaThuong()
    {
        var (them, bo) = MayTramAgent.SoSanhPhanMem(["Zalo|23", "UltraViewer|6"], ["zalo|24", "AnyDesk|8"]);
        Assert.Equal(["AnyDesk"], them);
        Assert.Equal(["UltraViewer"], bo);
    }

    [Theory]
    [InlineData("00:1a:2b:3c:4d:5e", "00-1A-2B-3C-4D-5E")]
    [InlineData("001A2B3C4D5E", "00-1A-2B-3C-4D-5E")]
    public void ChuanHoaMac(string vao, string ra) => Assert.Equal(ra, ThietBiCNTT.Infrastructure.Services.AgentService.ChuanMac(vao));

    [Fact]
    public void KhongGui_TheoSoNgay()
    {
        var now = new DateTime(2026, 11, 24);
        Assert.True(new MayTramAgent { LanCuoi = now.AddDays(-8) }.KhongGui(now, 7));
        Assert.False(new MayTramAgent { LanCuoi = now.AddDays(-8) }.KhongGui(now, 0));
    }
}

public class DiDongTests
{
    [Fact]
    public void ThoiDiemQuet_DungGioMayQuet_NeuHopLy()
    {
        var now = new DateTime(2026, 11, 24, 10, 0, 0);
        long Ms(DateTime t) => new DateTimeOffset(t).ToUnixTimeMilliseconds();
        Assert.Equal(now, ThietBiCNTT.Web.Controllers.KiemKeController.ThoiDiemQuet(null, now));
        Assert.Equal(now.AddHours(-2), ThietBiCNTT.Web.Controllers.KiemKeController.ThoiDiemQuet(Ms(now.AddHours(-2)), now));
        Assert.Equal(now, ThietBiCNTT.Web.Controllers.KiemKeController.ThoiDiemQuet(Ms(now.AddDays(-8)), now));   // quá cũ
        Assert.Equal(now, ThietBiCNTT.Web.Controllers.KiemKeController.ThoiDiemQuet(Ms(now.AddHours(1)), now));   // giờ máy sai (tương lai)
        Assert.Equal(now, ThietBiCNTT.Web.Controllers.KiemKeController.ThoiDiemQuet(Ms(now.AddMinutes(2)), now)); // lệch nhẹ → kẹp về hiện tại
    }
}

public class BaoCaoDinhKyTests
{
    [Fact]
    public void KyDenHan_ThangVaTuan()
    {
        var k = LichGuiBaoCao.KyDenHan(ChuKyGui.Thang, new DateTime(2026, 12, 1, 8, 0, 0))!.Value;
        Assert.Equal("2026-11", k.Ky); Assert.Equal(new DateTime(2026, 11, 1), k.Tu); Assert.Equal(new DateTime(2026, 12, 1), k.Den);
        Assert.Null(LichGuiBaoCao.KyDenHan(ChuKyGui.Thang, new DateTime(2026, 12, 2)));
        var t = LichGuiBaoCao.KyDenHan(ChuKyGui.Tuan, new DateTime(2026, 11, 23, 9, 0, 0))!.Value; // thứ Hai
        Assert.Equal(new DateTime(2026, 11, 16), t.Tu); Assert.Equal(new DateTime(2026, 11, 23), t.Den); Assert.Equal("2026-W47", t.Ky);
        Assert.Null(LichGuiBaoCao.KyDenHan(ChuKyGui.Tuan, new DateTime(2026, 11, 24)));
        var g = LichGuiBaoCao.KyGanNhat(ChuKyGui.Tuan, new DateTime(2026, 11, 26)); // thứ Năm → tuần trước đã trọn
        Assert.Equal(new DateTime(2026, 11, 16), g.Tu); Assert.Equal(new DateTime(2026, 11, 23), g.Den);
        Assert.Equal("2026-10", LichGuiBaoCao.KyGanNhat(ChuKyGui.Thang, new DateTime(2026, 11, 26)).Ky);
    }

    [Fact]
    public void LichGui_KiemTra()
    {
        Assert.Throws<DomainException>(() => new LichGuiBaoCao { NguoiNhan = " , " }.KiemTra());
        Assert.Throws<DomainException>(() => new LichGuiBaoCao { NguoiNhan = "lanhdao", Loai = LoaiBaoCaoDinhKy.TuyBien }.KiemTra());
        Assert.Equal(["lanhdao", "a@b.vn"], new LichGuiBaoCao { NguoiNhan = "lanhdao; a@b.vn\n" }.DsNguoiNhan);
    }

    [Fact]
    public void Tco_TongVaChiSo()
    {
        var x = new ThietBiCNTT.Infrastructure.Services.DongTco("PC", "PC-1", null, null, null, 1, 10_000_000, 3_000_000, 500_000, 1_000_000, 500_000, 2, 2.5);
        Assert.Equal(5_000_000, x.VanHanh); Assert.Equal(15_000_000, x.Tong); Assert.Equal(6_000_000, x.MoiNam); Assert.Equal(30, x.TyLeSua);
    }
}

public class BaoMatKeToanTests
{
    [Fact]
    public void KyKeToan_MacDinhThangTruoc_DenTinhCaNgay()
    {
        var (t, d) = ThietBiCNTT.Web.Controllers.KeToanController.Ky(new DateTime(2026, 10, 1), new DateTime(2026, 10, 31));
        Assert.Equal(new DateTime(2026, 10, 1), t); Assert.Equal(new DateTime(2026, 11, 1), d);
        var (t2, d2) = ThietBiCNTT.Web.Controllers.KeToanController.Ky(null, null);
        Assert.Equal(1, t2.Day); Assert.Equal(t2.AddMonths(1), d2);
        var (t3, d3) = ThietBiCNTT.Web.Controllers.KeToanController.Ky(new DateTime(2026, 5, 10), new DateTime(2026, 5, 1)); // đảo ngược
        Assert.Equal(new DateTime(2026, 5, 1), t3); Assert.Equal(new DateTime(2026, 5, 11), d3);
    }

    [Fact]
    public async Task Ldap_ChuaCauHinh_BaoLoi_MatKhauRong_TuChoi()
    {
        var log = Microsoft.Extensions.Logging.Abstractions.NullLogger<ThietBiCNTT.Infrastructure.Identity.LdapService>.Instance;
        var khong = new ThietBiCNTT.Infrastructure.Identity.LdapService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), log);
        await Assert.ThrowsAsync<DomainException>(() => khong.KiemTraAsync("u", "p"));
        var cfg = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ldap:Server"] = "dc.invalid", ["Ldap:TenMien"] = "bth.local" }).Build();
        var co = new ThietBiCNTT.Infrastructure.Identity.LdapService(cfg, log);
        Assert.False(await co.KiemTraAsync("u", ""));          // bind ẩn danh phải bị chặn trước khi gọi máy chủ
        Assert.Equal("u@bth.local", co.Upn("u")); Assert.Equal("a@x.vn", co.Upn("a@x.vn"));
    }
}

public class TaiSanMoRongTests
{
    [Theory]
    [InlineData(KieuTruong.So, "1.234,5", "1234.5")]
    [InlineData(KieuTruong.So, "1500", "1500")]
    [InlineData(KieuTruong.So, "2,5", "2.5")]
    [InlineData(KieuTruong.So, "1.500", "1500")]
    [InlineData(KieuTruong.So, "1.500.000", "1500000")]
    [InlineData(KieuTruong.So, "1.5", "1.5")]
    [InlineData(KieuTruong.So, "2.50", "2.5")]
    [InlineData(KieuTruong.Ngay, "05/03/2026", "2026-03-05")]
    [InlineData(KieuTruong.Ngay, "2026-03-05", "2026-03-05")]
    [InlineData(KieuTruong.CoKhong, "Có", "true")]
    [InlineData(KieuTruong.CoKhong, "0", "false")]
    [InlineData(KieuTruong.DanhSach, "online", "Online")]
    [InlineData(KieuTruong.VanBan, "  P.204 ", "P.204")]
    public void ChuanHoaGiaTri(KieuTruong kieu, string vao, string ra) =>
        Assert.Equal(ra, new TruongTuyChinh { Ten = "x", Kieu = kieu, LuaChon = "Offline; Online" }.ChuanHoa(vao));

    [Fact]
    public void ChuanHoa_BaoLoi_VaBatBuoc()
    {
        Assert.Throws<DomainException>(() => new TruongTuyChinh { Ten = "x", Kieu = KieuTruong.So }.ChuanHoa("abc"));
        Assert.Throws<DomainException>(() => new TruongTuyChinh { Ten = "x", Kieu = KieuTruong.Ngay }.ChuanHoa("32/13/2026"));
        Assert.Throws<DomainException>(() => new TruongTuyChinh { Ten = "x", Kieu = KieuTruong.DanhSach, LuaChon = "A;B" }.ChuanHoa("C"));
        Assert.Throws<DomainException>(() => new TruongTuyChinh { Ten = "x", BatBuoc = true }.ChuanHoa(" "));
        Assert.Null(new TruongTuyChinh { Ten = "x" }.ChuanHoa(""));
        Assert.Throws<DomainException>(() => new TruongTuyChinh { Ten = "x", Kieu = KieuTruong.DanhSach }.KiemTraDinhNghia());
        Assert.Equal("05/03/2026", new TruongTuyChinh { Kieu = KieuTruong.Ngay }.HienThi("2026-03-05"));
        Assert.True(new TruongTuyChinh { LoaiThietBiId = 3 }.ApDung(3));
        Assert.False(new TruongTuyChinh { LoaiThietBiId = 3 }.ApDung(4));
        Assert.False(new TruongTuyChinh { DangDung = false }.ApDung(3));
    }

    [Fact]
    public void MauMa_KiemTraVaDung()
    {
        Assert.Null(ThietBiCNTT.Infrastructure.Services.MaTaiSanService.KiemTraMau("{LOAI}-{KHOA}-{NAM}-{STT:5}"));
        Assert.NotNull(ThietBiCNTT.Infrastructure.Services.MaTaiSanService.KiemTraMau("{LOAI}-{NAM}"));          // thiếu STT
        Assert.NotNull(ThietBiCNTT.Infrastructure.Services.MaTaiSanService.KiemTraMau("{LOAI}-{STT}-{STT}"));    // 2 STT
        Assert.NotNull(ThietBiCNTT.Infrastructure.Services.MaTaiSanService.KiemTraMau("{XYZ}-{STT}"));           // thành phần lạ
        var (dau, so, cuoi) = ThietBiCNTT.Infrastructure.Services.MaTaiSanService.Dung("{LOAI}-{KHOA}-{NAM}-{STT:5}", "pc", null, new DateTime(2026, 5, 1));
        Assert.Equal("PC-KHO-2026-", dau); Assert.Equal(5, so); Assert.Equal("", cuoi);
        var b = ThietBiCNTT.Infrastructure.Services.MaTaiSanService.Dung("TB{NAM2}/{STT}/{LOAI}", "UPS", "CC", new DateTime(2026, 5, 1));
        Assert.Equal(("TB26/", 4, "/UPS"), b);
    }
}
