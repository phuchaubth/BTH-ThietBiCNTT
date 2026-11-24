using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Tests;

/// <summary>Quy tắc nghiệp vụ GĐ3.2 – tài liệu, an ninh mạng, rủi ro, báo cáo lãnh đạo (docs/JCI8-HaTangCNTT.md).</summary>
public class TaiLieuQuyDinhTests
{
    private static readonly DateTime Now = new(2026, 5, 1);
    private static TaiLieuQuyDinh MoiGuiDuyet() { var t = new TaiLieuQuyDinh { Ten = "CS", TrangThai = TrangThaiKeHoach.Nhap }; t.GuiDuyet("ktv"); return t; }

    [Fact] public void DuyetHaiCap_CnttTruocLanhDaoSau()
    {
        var t = MoiGuiDuyet();
        Assert.Throws<DomainException>(() => t.Duyet("ld", false, true, Now, 12)); // lãnh đạo không duyệt trước CNTT
        Assert.False(t.Duyet("tp", true, false, Now, 12));
        Assert.True(t.Duyet("ld", false, true, Now, 12));
        Assert.Equal(TrangThaiKeHoach.HieuLuc, t.TrangThai);
        Assert.Equal(new DateOnly(2027, 5, 1), t.NgayRaSoat);
    }
    [Fact] public void TuChoi_CanLyDo_VeNhap()
    {
        var t = MoiGuiDuyet();
        Assert.Throws<DomainException>(() => t.TuChoi("tp", " "));
        t.TuChoi("tp", "Thiếu phạm vi"); Assert.Equal(TrangThaiKeHoach.Nhap, t.TrangThai);
    }
    [Fact] public void DenHanRaSoat_VaRaSoatXongDoiNgay()
    {
        var t = MoiGuiDuyet(); t.Duyet("tp", true, false, Now, 12); t.Duyet("ld", false, true, Now, 1);
        Assert.False(t.DenHanRaSoat(new DateOnly(2026, 5, 15))); Assert.True(t.DenHanRaSoat(new DateOnly(2026, 6, 2)));
        t.RaSoatXong(new DateOnly(2026, 6, 2), 12); Assert.False(t.DenHanRaSoat(new DateOnly(2026, 6, 3)));
    }
    [Fact] public void KhongGuiDuyetKhiKhongPhaiNhap() { var t = MoiGuiDuyet(); Assert.Throws<DomainException>(() => t.GuiDuyet("x")); }
}

public class KenhTruyenDuLieuTests
{
    [Fact] public void NhanTinBaoMat_ThieuNhieuYeuCau() => Assert.Equal(5, new KenhTruyenDuLieu { Loai = LoaiKenh.NhanTinBaoMat }.ThieuYeuCau(false).Count);
    [Fact] public void NhanTinBaoMat_DuYeuCau() => Assert.Empty(new KenhTruyenDuLieu { Loai = LoaiKenh.NhanTinBaoMat, DangNhapRieng = true, MaHoa = true, DanhBaNguoiDuocPhep = true, BienNhanGuiDoc = true, DauThoiGian = true, BaoVeTruyCap = true }.ThieuYeuCau(false));
    [Fact] public void ThietBiDiDong_CanHuongDanVanBan() => Assert.Contains("hướng dẫn bằng văn bản", new KenhTruyenDuLieu { Loai = LoaiKenh.ThietBiDiDong, ThuHoiKhiThoiViec = true, XoaTuXa = true }.ThieuYeuCau(false));
}

public class SuCoAnNinhTests
{
    private static readonly DateTime Now = new(2026, 5, 1, 9, 0, 0);
    private static SuCoAnNinh Moi() => new() { PhatHien = Now, TrangThai = TrangThaiSuCoAn.Moi };

    [Fact] public void PhanMuc_BatBuocCoDuLieuCaNhan() => Assert.Throws<DomainException>(() => Moi().PhanMuc(MucDoAn.ThongThuong, LoaiSuCoAn.Phishing, null, "a"));
    [Fact] public void GhiBuoc_PhaiPhanMucTruoc() => Assert.Throws<DomainException>(() => Moi().GhiBuoc(BuocUngPhoAn.PhatHien, "x", true, "a", Now));
    [Fact] public void ThongThuong_CanHaiBuocDauDeDong()
    {
        var s = Moi(); s.PhanMuc(MucDoAn.ThongThuong, LoaiSuCoAn.Phishing, false, "a");
        Assert.Equal(TrangThaiSuCoAn.DangXuLy, s.TrangThai); Assert.Equal(2, s.LyDoChuaDong().Count);
        s.GhiBuoc(BuocUngPhoAn.PhatHien, "ok", true, "a", Now); s.GhiBuoc(BuocUngPhoAn.NganChan, "ok", true, "a", Now);
        s.Dong("a", Now); Assert.Equal(TrangThaiSuCoAn.DaDong, s.TrangThai);
    }
    [Fact] public void NghiemTrongCoDuLieuCaNhan_CanBaoCaoCoQuanHoacLyDo()
    {
        var s = Moi(); s.PhanMuc(MucDoAn.NghiemTrong, LoaiSuCoAn.RoRiDuLieu, true, "a");
        foreach (var b in Enum.GetValues<BuocUngPhoAn>()) s.GhiBuoc(b, "ok", true, "a", Now);
        Assert.Throws<DomainException>(() => s.Dong("a", Now));
        s.GhiBaoCao("Cơ quan A", "123/CV", new DateOnly(2026, 5, 2)); s.Dong("a", Now);
        Assert.True(s.DaBaoCaoCoQuan);
    }
    [Fact] public void HanBaoCao_ChiKhiBatThamSo()
    {
        var s = Moi(); s.PhanMuc(MucDoAn.NghiemTrong, LoaiSuCoAn.RoRiDuLieu, true, "a");
        Assert.Null(s.HanBaoCao(0)); Assert.Equal(Now.AddHours(72), s.HanBaoCao(72));
    }
    [Fact] public void KhongSuaSauKhiDong()
    {
        var s = Moi(); s.PhanMuc(MucDoAn.ThongThuong, LoaiSuCoAn.Khac, false, "a");
        s.GhiBuoc(BuocUngPhoAn.PhatHien, "ok", true, "a", Now); s.GhiBuoc(BuocUngPhoAn.NganChan, "ok", true, "a", Now); s.Dong("a", Now);
        Assert.Throws<DomainException>(() => s.PhanMuc(MucDoAn.ThongThuong, LoaiSuCoAn.Khac, false, "a"));
    }
}

public class RuiRoTests
{
    private static readonly DateTime Now = new(2026, 5, 1);
    [Theory] [InlineData(4, MucRuiRo.Thap)] [InlineData(5, MucRuiRo.TrungBinh)] [InlineData(9, MucRuiRo.TrungBinh)] [InlineData(10, MucRuiRo.Cao)] [InlineData(16, MucRuiRo.Cao)] [InlineData(17, MucRuiRo.RatCao)]
    public void MucTheoDiem(int diem, MucRuiRo muc) => Assert.Equal(muc, RuiRo.MucTheoDiem(diem));

    [Fact] public void ChamDiem_KhoangHopLe() => Assert.Throws<DomainException>(() => new RuiRo().ChamDiem(0, 3, null, null, "", "a", Now));
    [Fact] public void ChamDiem_DoiDiemCanLyDo()
    {
        var r = new RuiRo(); r.ChamDiem(3, 3, null, null, "", "a", Now);
        Assert.Throws<DomainException>(() => r.ChamDiem(4, 4, null, null, "", "a", Now));
        r.ChamDiem(4, 4, null, null, "Sự việc mới", "a", Now); Assert.Equal(2, r.LichSus.Count); Assert.Equal(16, r.Diem);
    }
    [Fact] public void TonDuKhongCaoHonBanDau() => Assert.Throws<DomainException>(() => new RuiRo().ChamDiem(2, 2, 3, 3, "", "a", Now));
    [Fact] public void DaKiemSoat_CanPhuongAn() => Assert.Throws<DomainException>(() => new RuiRo().ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 0));
    [Fact] public void GiamThieu_ConHanhDongChuaXong_ChanChuyen()
    {
        var r = new RuiRo { PhuongAn = PhuongAnRuiRo.GiamThieu };
        Assert.Throws<DomainException>(() => r.ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 2));
        r.ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 0); Assert.Equal(TrangThaiRuiRo.DaKiemSoat, r.TrangThai);
    }
    [Fact] public void ChapNhanMucCao_CanLanhDaoDuyet()
    {
        var r = new RuiRo { PhuongAn = PhuongAnRuiRo.ChapNhan, LyDoChapNhan = "Chi phí cao", NgayXemXetLai = new DateOnly(2026, 11, 1) };
        r.ChamDiem(4, 4, null, null, "", "a", Now); Assert.True(r.CanLanhDaoDuyetChapNhan);
        Assert.Throws<DomainException>(() => r.ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 0));
        r.DuyetChapNhan("ld", Now); r.ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 0);
    }
    [Fact] public void ChapNhanMucThap_KhongCanLanhDao()
    {
        var r = new RuiRo { PhuongAn = PhuongAnRuiRo.ChapNhan, LyDoChapNhan = "Thấp", NgayXemXetLai = new DateOnly(2026, 11, 1) };
        r.ChamDiem(1, 2, null, null, "", "a", Now); r.ChuyenTrangThai(TrangThaiRuiRo.DaKiemSoat, 0);
    }
}

public class DotRuiRoVaBaoCaoTests
{
    private static readonly DateTime Now = new(2026, 5, 1);
    [Fact] public void ChotDot_ChanKhiConRuiRoChuaChamLai()
    {
        var d = new DotDanhGiaRuiRo();
        Assert.Throws<DomainException>(() => d.Chot(5, 2, "a", Now));
        d.Chot(5, 0, "a", Now); Assert.True(d.DaChot); Assert.Throws<DomainException>(() => d.Chot(5, 0, "a", Now));
    }
    [Fact] public void BaoCao_GuiCanNhanXet_RoiMoiXacNhan()
    {
        var b = new BaoCaoLanhDao();
        Assert.Throws<DomainException>(() => b.XacNhanDaXem("ld", null, Now)); // chưa gửi
        Assert.Throws<DomainException>(() => b.Gui("tp", " ", null, Now));
        b.Gui("tp", "Ổn định", null, Now); Assert.Throws<DomainException>(() => b.Gui("tp", "x", null, Now));
        b.XacNhanDaXem("ld", "ok", Now); Assert.True(b.DaXem); Assert.Throws<DomainException>(() => b.XacNhanDaXem("ld", null, Now));
    }
    [Fact] public void KhoangQuy_VaTenKy()
    {
        Assert.Equal((new DateOnly(2026, 4, 1), new DateOnly(2026, 6, 30)), BaoCaoLanhDao.KhoangQuy(2026, 2));
        Assert.Equal("2026-Q3", BaoCaoLanhDao.TenKy(new DateOnly(2026, 8, 5)));
    }
    [Fact] public void NguoiGiamSat_HieuLucTheoNgay()
    {
        var g = new NguoiGiamSatCntt { NgayBatDau = new DateOnly(2026, 1, 1), NgayKetThuc = new DateOnly(2026, 6, 30) };
        Assert.True(g.HieuLuc(new DateOnly(2026, 3, 1))); Assert.False(g.HieuLuc(new DateOnly(2026, 7, 1)));
    }
}
