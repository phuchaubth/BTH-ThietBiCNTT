using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;

namespace ThietBiCNTT.Tests;

/// <summary>Quy tắc nghiệp vụ GĐ3.3 – quản lý thay đổi, rà soát quyền, lỗ hổng, bài tập chủ động, nhà cung cấp.</summary>
public class PhieuThayDoiTests
{
    private static readonly DateTime Now = new(2026, 5, 4, 9, 0, 0); // thứ hai
    private static PhieuThayDoi Thuong() => new() { Loai = LoaiThayDoi.Thuong, Ten = "Nâng cấp", KeHoachThucHien = "k", PhuongAnQuayLui = "q" };

    [Fact] public void BuocDuyet_TheoLoai()
    {
        Assert.Empty(new PhieuThayDoi { Loai = LoaiThayDoi.Chuan }.BuocCanDuyet(true));
        Assert.Equal([BuocDuyetThayDoi.Cntt], Thuong().BuocCanDuyet(false));
        Assert.Equal([BuocDuyetThayDoi.Cntt, BuocDuyetThayDoi.ChuSoHuu], Thuong().BuocCanDuyet(true));
        Assert.Equal([BuocDuyetThayDoi.Cntt, BuocDuyetThayDoi.LanhDao], new PhieuThayDoi { Loai = LoaiThayDoi.TrienKhaiMoi }.BuocCanDuyet(false));
    }
    [Fact] public void ThuongThieuQuayLui_KhongGuiDuyet()
    {
        var p = Thuong(); p.PhuongAnQuayLui = null;
        Assert.Throws<DomainException>(() => p.GuiDuyet("a", Now));
    }
    [Fact] public void ChuanKhongCanQuayLui_VaBatDauTrucTiep()
    {
        var p = new PhieuThayDoi { Loai = LoaiThayDoi.Chuan, Ten = "Vá" };
        Assert.Empty(p.ThieuDieuKien()); p.BatDau("a", Now); Assert.Equal(TrangThaiThayDoi.DangThucHien, p.TrangThai);
    }
    [Fact] public void ThuongChuaDuyet_KhongBatDau() => Assert.Throws<DomainException>(() => Thuong().BatDau("a", Now));
    [Fact] public void DuyetDungThuTuRoiMoiBatDau()
    {
        var p = Thuong(); p.GuiDuyet("a", Now);
        Assert.Throws<DomainException>(() => p.Duyet(BuocDuyetThayDoi.ChuSoHuu, "x", true, Now));
        Assert.False(p.Duyet(BuocDuyetThayDoi.Cntt, "tp", true, Now));
        Assert.True(p.Duyet(BuocDuyetThayDoi.ChuSoHuu, "csh", true, Now));
        Assert.Equal(TrangThaiThayDoi.DaDuyet, p.TrangThai);
        p.BatDau("a", Now); Assert.Equal(TrangThaiThayDoi.DangThucHien, p.TrangThai);
    }
    [Fact] public void KhanCap_BatDauNgay_HanDuyetBoSungHaiNgayLam()
    {
        var p = new PhieuThayDoi { Loai = LoaiThayDoi.KhanCap, LyDo = "Hỏng", PhuongAnQuayLui = "q" };
        p.BatDau("a", new DateTime(2026, 5, 8, 10, 0, 0)); // thứ sáu
        Assert.Equal(new DateTime(2026, 5, 12, 10, 0, 0), p.HanDuyetBoSung); // thứ ba
        Assert.True(p.QuaHanDuyetBoSung(new DateTime(2026, 5, 13)));
        Assert.Throws<DomainException>(() => p.GhiKetQua(TrangThaiThayDoi.HoanThanh, "ok", "đạt", false, null, "a", Now)); // chưa duyệt bổ sung
    }
    [Fact] public void CongNgayLam_BoQuaCuoiTuan() => Assert.Equal(new DateTime(2026, 5, 11), PhieuThayDoi.CongNgayLam(new DateTime(2026, 5, 8), 1));
    [Fact] public void ThayDoiLon_CanXacNhanNghiepVu()
    {
        var p = Thuong(); p.LaThayDoiLon = true; p.TrangThai = TrangThaiThayDoi.DangThucHien;
        Assert.Throws<DomainException>(() => p.GhiKetQua(TrangThaiThayDoi.HoanThanh, "ok", "đạt", false, null, "a", Now));
        p.GhiKetQua(TrangThaiThayDoi.HoanThanh, "ok", "đạt", true, "Khoa Dược", "a", Now);
        Assert.True(p.NenTaoHanhDong); // thành công có vấn đề
    }
    [Fact] public void QuayLui_GoiYHanhDong() { var p = Thuong(); p.TrangThai = TrangThaiThayDoi.DangThucHien; p.GhiKetQua(TrangThaiThayDoi.DaQuayLui, "Lỗi", null, false, null, "a", Now); Assert.True(p.NenTaoHanhDong); }
    [Fact] public void Pcra_ChanKhiConMucChuaGhi()
    {
        var p = Thuong(); p.AnhHuongChamSoc = true; foreach (var n in Enum.GetValues<NhomPcra>()) p.MucPcras.Add(new MucPcra { Nhom = n });
        Assert.Contains(p.ThieuDieuKien(), x => x.Contains("PCRA"));
        foreach (var m in p.MucPcras) m.NoiDung = "ok"; p.BienPhapAnToan = "giấy"; p.NguoiPhuTrachKhoa = "dd";
        Assert.Empty(p.ThieuDieuKien());
    }
    [Fact] public void TrienKhaiMoi_CanChecklistDat()
    {
        var p = new PhieuThayDoi { Loai = LoaiThayDoi.TrienKhaiMoi, KeHoachThucHien = "k", PhuongAnQuayLui = "q", LaCongCuHoTroQuyetDinh = true };
        Assert.Contains(p.ThieuDieuKien(), x => x.Contains("checklist"));
        p.TaoChecklistMacDinh(); Assert.Equal(11, p.Checklist.Count); // 8 mặc định + 3 AI
        foreach (var m in p.Checklist) m.Dat = true; Assert.Empty(p.ThieuDieuKien());
    }
    [Fact] public void GiaoNhauKhungGio()
    {
        var a = new DateTime(2026, 5, 4, 22, 0, 0);
        Assert.True(PhieuThayDoi.GiaoNhau(a, a.AddHours(2), a.AddHours(1), a.AddHours(3)));
        Assert.False(PhieuThayDoi.GiaoNhau(a, a.AddHours(2), a.AddHours(2), a.AddHours(3)));
    }
    [Fact] public void DanhGiaSau_CanKetLuanVaAnToan()
    {
        var d = new DanhGiaSauThayDoi();
        Assert.Throws<DomainException>(() => d.Ghi(null, null, null, null, null, "tốt", "a", Now));
        d.Ghi("dễ", "đúng", "ổn", "không sai sót", "tốt", "Ổn định", "a", Now); Assert.True(d.DaDanhGia);
    }
}

public class RaSoatQuyenTests
{
    private static readonly DateTime Now = new(2026, 5, 4);
    private static TaiKhoanRaSoat Tk(string u, string? ten = "A", int? khoa = 1, string? quyen = "Bác sĩ", DateTime? dn = null) => new() { TenDangNhap = u, HoTen = ten, KhoaPhongId = khoa, NhomQuyen = quyen, LanDangNhapCuoi = dn ?? Now.AddDays(-2) };

    [Fact] public void TuPhatHien_LauKhongDangNhap_QuanTri_DungChung_KhongKhoa()
    {
        var a = Tk("u1", dn: Now.AddDays(-200)); a.PhatHien(90, Now); Assert.True(a.KhongDangNhapLau);
        var b = Tk("his_admin", quyen: "Quản trị hệ thống (admin)"); b.PhatHien(90, Now); Assert.True(b.QuyenQuanTri);
        var c = Tk("khoa.chung", ten: null); c.PhatHien(90, Now); Assert.True(c.DungChung);
        var d = Tk("u4", khoa: null); d.PhatHien(90, Now); Assert.True(d.KhongThuocKhoa);
        var e = Tk("u5"); e.PhatHien(90, Now); Assert.False(e.CoDauHieu);
    }
    [Fact] public void NhapDanhSach_ChiMotLan_VaKhongRong()
    {
        var d = new DotRaSoatQuyen();
        Assert.Throws<DomainException>(() => d.NhapDanhSach([], "a", Now));
        d.NhapDanhSach([Tk("u1")], "a", Now); Assert.Equal(TrangThaiRaSoat.DangXacNhan, d.TrangThai);
        Assert.Throws<DomainException>(() => d.NhapDanhSach([Tk("u2")], "a", Now));
    }
    [Fact] public void LuongXacNhan_ThucHien_Dong()
    {
        var d = new DotRaSoatQuyen { HeThong = new HeThongThongTin { Ma = "HIS" } };
        var t1 = Tk("u1"); var t2 = Tk("u2"); d.NhapDanhSach([t1, t2], "a", Now);
        Assert.Throws<DomainException>(() => d.ChuyenThucHien()); // chưa xác nhận
        t1.XacNhan(QuyetDinhTaiKhoan.ThuHoi, null, null, "tk", Now); t2.XacNhan(QuyetDinhTaiKhoan.Giu, null, null, "tk", Now);
        d.ChuyenThucHien();
        Assert.Throws<DomainException>(() => d.Dong("a", Now)); // chưa thực hiện thu hồi
        t1.DaThucHienTrenHeThong("cntt", Now); d.Dong("a", Now);
        Assert.Contains("Thu hồi: 1", d.BienBan);
    }
    [Fact] public void DoiQuyen_CanQuyenMoi() { var t = Tk("u1"); new DotRaSoatQuyen().NhapDanhSach([t], "a", Now); Assert.Throws<DomainException>(() => t.XacNhan(QuyetDinhTaiKhoan.DoiQuyen, "", null, "tk", Now)); }
    [Fact] public void GiuTaiKhoanQuanTri_CanLyDo()
    {
        var t = Tk("his_admin", quyen: "admin"); var d = new DotRaSoatQuyen(); d.NhapDanhSach([t], "a", Now);
        Assert.Throws<DomainException>(() => t.XacNhan(QuyetDinhTaiKhoan.Giu, null, null, "tk", Now));
        t.XacNhan(QuyetDinhTaiKhoan.Giu, null, "Cần cho vận hành", "tk", Now);
    }
    [Fact] public void ChuKyTheoMucTrongYeu() { Assert.Equal(6, DotRaSoatQuyen.ChuKyThang(MucTrongYeu.ToiQuanTrong, 6, 12)); Assert.Equal(12, DotRaSoatQuyen.ChuKyThang(MucTrongYeu.Cao, 6, 12)); Assert.Equal(0, DotRaSoatQuyen.ChuKyThang(MucTrongYeu.Thap, 6, 12)); }
    [Fact] public void ViPham_CanHanhDongMoiDong()
    {
        var v = new ViPhamTruyCap(); Assert.Throws<DomainException>(() => v.GhiHanhDong("a", " ", true, Now));
        v.GhiHanhDong("a", "Nhắc nhở, khoá tài khoản", true, Now); Assert.Equal(TrangThaiViPham.DaDong, v.TrangThai); Assert.Throws<DomainException>(() => v.GhiHanhDong("a", "x", false, Now));
    }
}

public class LoHongVaKhacTests
{
    private static readonly DateTime Now = new(2026, 5, 4);
    [Theory] [InlineData(9.8, MucLoHong.NghiemTrong)] [InlineData(7.0, MucLoHong.Cao)] [InlineData(5.5, MucLoHong.TrungBinh)] [InlineData(2.0, MucLoHong.Thap)]
    public void MucTheoCvss(double c, MucLoHong m) => Assert.Equal(m, LoHong.MucTheoCvss((decimal)c));
    [Fact] public void HanVaTheoMuc() => Assert.Equal(new DateOnly(2026, 5, 11), LoHong.TinhHan(new DateOnly(2026, 5, 4), 9.5m, 7, 30, 90, 180));
    [Fact] public void ChapNhanRuiRo_CanLyDo()
    {
        var l = new LoHong { Cvss = 8 }; Assert.Throws<DomainException>(() => l.ChapNhanRuiRo(" ", "a", Now));
        l.ChapNhanRuiRo("Hệ thống sắp thay", "a", Now); Assert.False(l.DangMo); Assert.Throws<DomainException>(() => l.DaVaXong("a", Now));
    }
    [Fact] public void QuaHan() { var l = new LoHong { Cvss = 8, HanVa = new DateOnly(2026, 5, 1) }; Assert.True(l.QuaHan(new DateOnly(2026, 5, 4))); l.DaVaXong("a", Now); Assert.False(l.QuaHan(new DateOnly(2026, 5, 4))); }
    [Fact] public void BaiTapChuDong_DienDuMoiChot()
    {
        var b = new BaiTapChuDong { QuyTrinh = "Ngừng HIS" };
        Assert.Throws<DomainException>(() => b.Chot("a", Now));
        b.DiemCoTheHong = "x"; b.HauQua = "y"; b.BienPhap = "z"; b.Chot("a", Now); Assert.Throws<DomainException>(() => b.Chot("a", Now));
    }
    [Fact] public void DanhGiaNhaCungCap_CanNguoiGiamSatVaTruongBoPhan()
    {
        var d = new DanhGiaNhaCungCap();
        Assert.Throws<DomainException>(() => d.Chot(Now)); d.NguoiGiamSat = "a"; Assert.Throws<DomainException>(() => d.Chot(Now));
        d.TruongBoPhan = "b"; d.KetLuan = KetLuanNhaCungCap.CanhBao; Assert.Throws<DomainException>(() => d.Chot(Now)); // thiếu nhận xét
        d.NhanXet = "Chậm"; d.Chot(Now); Assert.NotNull(d.NgayDanhGia);
    }
    [Fact] public void HopDongThieuGld05() { var h = new HopDong { Loai = LoaiHopDong.BaoTri, PhamVi = "x" }; Assert.Equal(4, h.ThieuGld05().Count); Assert.True(h.LaDichVuCntt); }
    [Fact] public void DuAnXayDung_ThieuThongTin() { var d = new DuAnXayDung { Ten = "A" }; Assert.True(d.ThieuThongTin); d.NguoiThamGiaCntt = "x"; d.NgayDanhGia = new DateOnly(2026, 5, 1); d.BienPhapBaoVe = "y"; Assert.False(d.ThieuThongTin); }
}
