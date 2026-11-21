using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;

namespace ThietBiCNTT.Domain.Common;

/// <summary>
/// Tính hạn xử lý theo QĐ-04: khoa trọng yếu tính 24/7; khoa khác chỉ tính giờ hành chính
/// (khung giờ, các thứ làm việc, trừ ngày nghỉ lễ) — tất cả lấy từ tham số hệ thống.
/// </summary>
public static class SlaCalculator
{
    public static TimeSpan ThoiLuong(MucDoUuTien uuTien, bool khoaTrongYeu, SlaThamSo ts)
    {
        var gio = uuTien switch
        {
            MucDoUuTien.KhanCap => ts.GioKhanCap,
            MucDoUuTien.Cao => ts.GioCao,
            MucDoUuTien.TrungBinh => ts.GioTrungBinh,
            _ => ts.GioThap
        };
        if (khoaTrongYeu) gio *= ts.HeSoKhoaTrongYeu;
        return TimeSpan.FromMinutes((double)Math.Max(15, gio * 60)); // tối thiểu 15 phút
    }

    public static DateTime TinhHan(DateTime batDau, MucDoUuTien uuTien, bool khoaTrongYeu, SlaThamSo ts)
    {
        var thoiLuong = ThoiLuong(uuTien, khoaTrongYeu, ts);
        return khoaTrongYeu && ts.KhoaTrongYeu24h ? batDau + thoiLuong : CongGioLamViec(batDau, thoiLuong, ts);
    }

    /// <summary>Cộng một khoảng thời gian chỉ tính trong giờ làm việc.</summary>
    public static DateTime CongGioLamViec(DateTime tu, TimeSpan conLai, SlaThamSo ts)
    {
        if (ts.GioKetThuc <= ts.GioBatDau || ts.NgayLamViec.Count == 0) return tu + conLai; // cấu hình không hợp lệ → 24/7
        var t = tu;
        for (var vong = 0; vong < 3660 && conLai > TimeSpan.Zero; vong++)
        {
            var ngay = DateOnly.FromDateTime(t);
            var moCua = ngay.ToDateTime(ts.GioBatDau);
            var dongCua = ngay.ToDateTime(ts.GioKetThuc);
            if (!LaNgayLamViec(ngay, ts) || t >= dongCua) { t = ngay.AddDays(1).ToDateTime(ts.GioBatDau); continue; }
            if (t < moCua) t = moCua;
            var trongNgay = dongCua - t;
            if (conLai <= trongNgay) return t + conLai;
            conLai -= trongNgay;
            t = ngay.AddDays(1).ToDateTime(ts.GioBatDau);
        }
        return t;
    }

    public static bool LaNgayLamViec(DateOnly ngay, SlaThamSo ts) =>
        ts.NgayLamViec.Contains(ngay.DayOfWeek) && !ts.NgayNghi.Contains(ngay);
}
