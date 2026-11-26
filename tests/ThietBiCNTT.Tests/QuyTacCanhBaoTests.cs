using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using Xunit;

namespace ThietBiCNTT.Tests;

public class QuyTacCanhBaoTests
{
    private static readonly DateOnly HomNay = new(2026, 11, 26);
    private static bool K(ToanTuCanhBao t, string? g, object? v) => new DieuKienCanhBao { Truong = "x", ToanTu = t, GiaTri = g }.Khop(v, HomNay);

    [Fact]
    public void SoSanhSo_ChapNhanDinhDangViet()
    {
        Assert.True(K(ToanTuCanhBao.Lon, "27", 27.5m));
        Assert.False(K(ToanTuCanhBao.Lon, "27", 27m));
        Assert.True(K(ToanTuCanhBao.LonBang, "27,5", 27.5m));
        Assert.True(K(ToanTuCanhBao.Lon, "1.000.000", 15_500_000m));
        Assert.False(K(ToanTuCanhBao.Lon, "1.000.000", 900_000m));
        Assert.True(K(ToanTuCanhBao.Bang, "3", 3));
        Assert.True(K(ToanTuCanhBao.Lon, "100", "250"));    // trường bổ sung lưu chuỗi số
        Assert.False(K(ToanTuCanhBao.Lon, "10", null));
    }

    [Fact]
    public void Chuoi_Bool_Trong()
    {
        Assert.True(K(ToanTuCanhBao.Bang, "đang sử dụng", "Đang sử dụng"));
        Assert.True(K(ToanTuCanhBao.Chua, "nhiệt", "Nhiệt độ phòng"));
        Assert.True(K(ToanTuCanhBao.KhongChua, "UPS", "Máy in"));
        Assert.True(K(ToanTuCanhBao.Bang, "true", true));
        Assert.True(K(ToanTuCanhBao.Bang, "có", true));
        Assert.True(K(ToanTuCanhBao.Khac, "true", false));
        Assert.True(K(ToanTuCanhBao.Trong, null, "  "));
        Assert.True(K(ToanTuCanhBao.KhongTrong, null, "PC-1"));
    }

    [Fact]
    public void Ngay_TrongVong_QuaMoc()
    {
        Assert.True(K(ToanTuCanhBao.TrongVongNgay, "30", HomNay.AddDays(30)));
        Assert.False(K(ToanTuCanhBao.TrongVongNgay, "30", HomNay.AddDays(31)));
        Assert.False(K(ToanTuCanhBao.TrongVongNgay, "30", HomNay.AddDays(-1)));
        Assert.True(K(ToanTuCanhBao.QuaNgay, "2", HomNay.AddDays(-2)));
        Assert.False(K(ToanTuCanhBao.QuaNgay, "2", HomNay.AddDays(-1)));
        Assert.True(K(ToanTuCanhBao.QuaNgay, "0", new DateTime(2026, 11, 26, 8, 0, 0)));
        Assert.True(K(ToanTuCanhBao.Nho, "01/12/2026", HomNay));
    }

    [Fact]
    public void QuyTac_AND_VaKiemTraHopLe()
    {
        var q = new QuyTacCanhBao { Ten = "t", DoiTuong = DoiTuongCanhBao.KetQuaKiemTra, NguoiNhan = "admin",
            DieuKiens = [new() { Truong = "muc", ToanTu = ToanTuCanhBao.Chua, GiaTri = "Nhiệt độ" }, new() { Truong = "gia_tri_so", ToanTu = ToanTuCanhBao.Lon, GiaTri = "27" }] };
        q.KiemTra();
        Assert.True(q.Khop(new Dictionary<string, object?> { ["muc"] = "Nhiệt độ phòng", ["gia_tri_so"] = 30m }, HomNay));
        Assert.False(q.Khop(new Dictionary<string, object?> { ["muc"] = "Độ ẩm", ["gia_tri_so"] = 60m }, HomNay));
        q.HanhDong = HanhDongCanhBao.NangUuTien;
        Assert.Throws<DomainException>(q.KiemTra);
        q.HanhDong = HanhDongCanhBao.KhongCo; q.NguoiNhan = null;
        Assert.Throws<DomainException>(q.KiemTra);
        q.NguoiNhan = "admin"; q.DieuKiens[1].GiaTri = "";
        Assert.Throws<DomainException>(q.KiemTra);
    }
}
