using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using Xunit;

namespace ThietBiCNTT.Tests;

public class VanThuTests
{
    private static readonly DateTime Now = new(2026, 11, 26, 9, 0, 0);

    private static VanBan DuThao(LoaiVanBan loai = LoaiVanBan.ToTrinh)
    {
        var vb = new VanBan { Loai = loai, TrichYeu = "V/v thử", NoiDung = "Nội dung", NoiNhan = "Ban Giám đốc" };
        vb.KhoiTaoDuThao("ktv1", Now);
        return vb;
    }

    [Theory]
    [InlineData(LoaiVanBan.CongVan, "12/CNTT")]
    [InlineData(LoaiVanBan.ToTrinh, "12/TTr-CNTT")]
    [InlineData(LoaiVanBan.QuyetDinh, "12/QĐ-CNTT")]
    [InlineData(LoaiVanBan.KeHoach, "12/KH-CNTT")]
    public void SoKyHieu_TheoNghiDinh30(LoaiVanBan loai, string kq) => Assert.Equal(kq, VanBan.TaoSoKyHieu(12, loai, " CNTT "));

    [Fact]
    public void ToTrinh_DuyetTuanTu_TraLai_TrinhLai_PhatHanh()
    {
        var vb = DuThao();
        Assert.True(vb.TheoDoiThucHien);
        Assert.Throws<DomainException>(() => vb.TrinhDuyet([], "ktv1", Now));
        vb.TrinhDuyet(["ktv2", "lanhdao", "KTV2"], "ktv1", Now);
        Assert.Equal(2, vb.CacBuocLuotNay.Count());           // bỏ trùng
        Assert.Throws<DomainException>(() => vb.Duyet("lanhdao", true, null, Now)); // không vượt bước
        Assert.Throws<DomainException>(() => vb.Duyet("ktv2", false, " ", Now));    // trả lại phải có ý kiến
        Assert.False(vb.Duyet("ktv2", false, "Bổ sung dự toán", Now));
        Assert.Equal(TrangThaiVanBan.TraLai, vb.TrangThai);
        Assert.True(vb.DuocSua);

        vb.TrinhDuyet(["ktv2", "lanhdao"], "ktv1", Now);
        Assert.Equal(2, vb.LuotGui);
        Assert.Equal("ktv2", vb.BuocDangCho!.NguoiDuyet);
        Assert.False(vb.Duyet("KTV2", true, null, Now));
        Assert.Equal("lanhdao", vb.BuocDangCho!.NguoiDuyet);
        Assert.Throws<DomainException>(() => vb.PhatHanh(1, "CNTT", Now, null, null, "admin", Now));
        Assert.True(vb.Duyet("lanhdao", true, "Đồng ý", Now));
        Assert.Equal(TrangThaiVanBan.DaDuyet, vb.TrangThai);

        vb.PhatHanh(7, "CNTT", Now.Date, "Nguyễn A", "TRƯỞNG PHÒNG", "admin", Now);
        Assert.Equal("7/TTr-CNTT", vb.SoKyHieu);
        Assert.Equal(TrangThaiVanBan.DangXuLy, vb.TrangThai);   // tờ trình → theo dõi thực hiện
        Assert.Equal("ktv1", vb.NguoiChuTri);
        Assert.Throws<DomainException>(() => vb.Huy("x", "ktv1", Now));
    }

    [Fact]
    public void CongVan_KhongTheoDoi_PhatHanhLaKetThuc()
    {
        var vb = DuThao(LoaiVanBan.CongVan);
        vb.TrinhDuyet(["admin"], "ktv1", Now);
        vb.Duyet("admin", true, null, Now);
        vb.NoiNhan = null;
        Assert.Throws<DomainException>(() => vb.PhatHanh(1, "CNTT", Now, null, null, "admin", Now)); // phải có nơi nhận
        vb.NoiNhan = "Sở Y tế";
        vb.PhatHanh(1, "CNTT", Now, null, null, "admin", Now);
        Assert.Equal(TrangThaiVanBan.DaPhatHanh, vb.TrangThai);
        Assert.True(vb.KetThuc);
        Assert.False(vb.QuaHan(Now.AddDays(30)));
    }

    [Fact]
    public void VanBanDen_VaoSo_PhanCong_TienDo_HoanThanh()
    {
        var vb = new VanBan { Huong = HuongVanBan.Den, TrichYeu = "V/v báo cáo", NgayVanBan = Now.Date.AddDays(1) };
        Assert.Throws<DomainException>(() => vb.VaoSoDen(1, "admin", Now));   // thiếu nơi gửi
        vb.CoQuanBanHanh = "Sở Y tế"; vb.NgayDen = Now;
        Assert.Throws<DomainException>(() => vb.VaoSoDen(1, "admin", Now));   // ngày VB sau ngày đến
        vb.NgayVanBan = Now.Date.AddDays(-2);
        vb.VaoSoDen(5, "admin", Now);
        Assert.Equal(TrangThaiVanBan.ChoPhanCong, vb.TrangThai);
        Assert.Equal(2026, vb.Nam);
        Assert.Throws<DomainException>(() => vb.CapNhatTienDo(10, "x", "ktv1", Now));
        Assert.Throws<DomainException>(() => vb.PhanCong("ktv1", null, Now.AddDays(-1), null, "admin", Now));
        vb.PhanCong("ktv1", "ktv2, ktv1;lanhdao", Now.AddDays(3), "Làm gấp", "admin", Now);
        Assert.Equal("ktv2,lanhdao", vb.NguoiPhoiHop);            // bỏ chủ trì khỏi phối hợp
        Assert.True(vb.LaNguoiXuLy("LANHDAO"));
        Assert.False(vb.LaNguoiXuLy("admin"));
        Assert.True(vb.SapDenHan(Now, 3));
        Assert.True(vb.QuaHan(Now.AddDays(4)));
        Assert.Throws<DomainException>(() => vb.CapNhatTienDo(120, "x", "ktv1", Now));
        vb.CapNhatTienDo(60, "Đã tổng hợp", "ktv2", Now);
        Assert.Equal(60, vb.TienDo);
        Assert.Throws<DomainException>(() => vb.HoanThanh(" ", "ktv1", Now));
        vb.HoanThanh("Đã gửi báo cáo", "ktv1", Now, 99);
        Assert.Equal(100, vb.TienDo);
        Assert.Equal(99, vb.VanBanLienQuanId);
        Assert.False(vb.QuaHan(Now.AddDays(10)));
        Assert.Equal(4, vb.NhatKys.Count);
    }
}
