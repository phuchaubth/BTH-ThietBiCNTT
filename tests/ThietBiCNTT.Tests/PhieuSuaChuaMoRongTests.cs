using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using Xunit;

namespace ThietBiCNTT.Tests;

public class PhieuSuaChuaMoRongTests
{
    private static readonly DateTime T0 = new(2026, 11, 17, 8, 0, 0);
    private static PhieuSuaChua Phieu()
    {
        var p = new PhieuSuaChua { SoPhieu = "SC-1", TaiSanId = 1, KhoaPhongId = 7, MoTaSuCo = "Hỏng", NgayYeuCau = T0, HanXuLy = T0.AddHours(8) };
        p.TiepNhan("ktv1", HinhThucXuLy.TuSua, T0.AddMinutes(10));
        return p;
    }

    [Fact]
    public void GioCong_TheoKtv_KhongChongLan_TongVaoPhieu()
    {
        var p = Phieu();
        var now = T0.AddHours(10);
        p.GhiGioCong("ktv1", T0.AddHours(1), T0.AddHours(2.5), null, now);
        p.GhiGioCong("ktv2", T0.AddHours(1), T0.AddHours(2), "hỗ trợ", now);       // người khác cùng giờ: được
        Assert.Throws<DomainException>(() => p.GhiGioCong("KTV1", T0.AddHours(2), T0.AddHours(3), null, now)); // chồng lấn
        Assert.Throws<DomainException>(() => p.GhiGioCong("ktv1", T0.AddHours(4), T0.AddHours(3), null, now)); // kết thúc trước bắt đầu
        Assert.Throws<DomainException>(() => p.GhiGioCong("ktv1", T0.AddHours(9), T0.AddHours(11), null, now)); // tương lai
        Assert.Equal(2.5m, p.GioCong);
        p.HoanThanh(null, "Đã sửa", 99, 0, now, null, NhomNguyenNhan.PhanCung, HanhDongKhacPhuc.ThayLinhKien, 100_000);
        Assert.Equal(2.5m, p.GioCong);                  // lấy theo nhật ký, bỏ qua số nhập tay
        Assert.Equal(250_000m, p.ChiPhiNhanCong);
        Assert.Equal(250_000m, p.TongChiPhi);
        Assert.Equal(NhomNguyenNhan.PhanCung, p.NhomNguyenNhan);
        Assert.Throws<DomainException>(() => p.GhiGioCong("ktv1", T0.AddHours(5), T0.AddHours(6), null, now));
    }

    [Fact]
    public void ChoMuonTam_ChiThietBiTrongKho_ThuHoiVeKhoGoc()
    {
        var p = Phieu();
        var tam = new TaiSan { Id = 9, MaTaiSan = "PC-9", TrangThai = TrangThaiTaiSan.DangSuDung, KhoaPhongId = 1 };
        Assert.Throws<DomainException>(() => p.ChoMuonTam(tam, T0));
        tam.TrangThai = TrangThaiTaiSan.TrongKho;
        Assert.Throws<DomainException>(() => p.ChoMuonTam(new TaiSan { Id = 1, MaTaiSan = "X", TrangThai = TrangThaiTaiSan.TrongKho }, T0));
        p.ChoMuonTam(tam, T0.AddHours(1));
        Assert.True(p.DangChoMuonTam);
        Assert.Equal(TrangThaiTaiSan.DangSuDung, tam.TrangThai);
        Assert.Equal(7, tam.KhoaPhongId);
        Assert.Throws<DomainException>(() => p.Huy("x"));
        p.ThuHoiTam(tam, T0.AddHours(5));
        Assert.False(p.DangChoMuonTam);
        Assert.Equal(TrangThaiTaiSan.TrongKho, tam.TrangThai);
        Assert.Equal(1, tam.KhoaPhongId);
        Assert.Throws<DomainException>(() => p.ThuHoiTam(tam, T0.AddHours(6)));
    }
}

public class NhacTenTests
{
    [Fact]
    public void NhacTen_BoEmailVaTrung() =>
        Assert.Equal(["ktv2", "truong.khoa"], BinhLuanPhieu.NhacTen("Nhờ @ktv2 và @truong.khoa. Gửi a@b.vn, @KTV2 lần nữa"));
}
