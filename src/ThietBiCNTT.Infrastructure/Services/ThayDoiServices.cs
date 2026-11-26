using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>GĐ3.3 – nghiệp vụ quản lý thay đổi (mục 4.5).</summary>
public class ThayDoiService(AppDbContext db, HaTangService haTang, LichSuService lichSu, ThongBaoService tb, SoPhieuService soPhieu, ThamSoService thamSo)
{
    /// <summary>Tập hệ thống bị tác động trực tiếp.</summary>
    public static List<int> HeThongIds(PhieuThayDoi p) => p.HeThongs.Select(h => h.HeThongId).Concat(p.HeThongId is { } i ? [i] : []).Distinct().ToList();

    public async Task<bool> CoToiQuanTrongAsync(PhieuThayDoi p)
    {
        var ids = HeThongIds(p);
        return ids.Count > 0 && await db.HeThongThongTins.AnyAsync(h => ids.Contains(h.Id) && h.MucTrongYeu == MucTrongYeu.ToiQuanTrong);
    }

    /// <summary>FR-TD-04: các thay đổi khác trùng khung giờ trên cùng hệ thống hoặc chuỗi phụ thuộc.</summary>
    public async Task<List<PhieuThayDoi>> TrungLichAsync(PhieuThayDoi p)
    {
        if (p.DeXuatBatDau is not { } b || p.DeXuatKetThuc is not { } k) return [];
        var canh = await haTang.CanhAsync();
        async Task<HashSet<int>> TapAnhHuong(IEnumerable<int> ids)
        {
            var kq = ids.ToHashSet();
            foreach (var id in ids.ToList()) foreach (var x in PhanTichPhuThuoc.HeThongBiAnhHuongBoiHeThong(id, canh)) kq.Add(x);
            return await Task.FromResult(kq);
        }
        var cua = await TapAnhHuong(HeThongIds(p));
        if (cua.Count == 0) return [];
        var khac = await db.PhieuThayDois.AsNoTracking().Include(x => x.HeThongs)
            .Where(x => x.Id != p.Id && x.DeXuatBatDau != null && x.DeXuatKetThuc != null && (x.TrangThai == TrangThaiThayDoi.ChoDuyet || x.TrangThai == TrangThaiThayDoi.DaDuyet || x.TrangThai == TrangThaiThayDoi.DangThucHien))
            .ToListAsync();
        var kq2 = new List<PhieuThayDoi>();
        foreach (var x in khac)
            if (PhieuThayDoi.GiaoNhau(b, k, x.DeXuatBatDau!.Value, x.DeXuatKetThuc!.Value) && (await TapAnhHuong(HeThongIds(x))).Overlaps(cua)) kq2.Add(x);
        return kq2;
    }

    /// <summary>FR-TD-03: thay đổi cần downtime → tạo downtime có kế hoạch và thông báo khoa.</summary>
    public async Task<SuKienDowntime> TaoDowntimeAsync(PhieuThayDoi p, string nguoi)
    {
        if (!p.CanDowntime) throw new DomainException("Phiếu không đánh dấu cần downtime.");
        if (p.SuKienDowntimeId is not null) throw new DomainException("Phiếu đã có sự kiện downtime.");
        if (p.TrangThai is not (TrangThaiThayDoi.DaDuyet or TrangThaiThayDoi.DangThucHien)) throw new DomainException("Chỉ tạo downtime cho phiếu đã duyệt hoặc đang thực hiện.");
        if (p.DeXuatBatDau is not { } b || p.DeXuatKetThuc is not { } k || k <= b) throw new DomainException("Khung giờ đề xuất không hợp lệ.");
        var ids = HeThongIds(p);
        if (ids.Count == 0) throw new DomainException("Chọn hệ thống bị tác động.");
        var now = DateTime.Now;
        var s = new SuKienDowntime { Loai = LoaiDowntime.CoKeHoach, MucDo = MucDoDowntime.NgungHoanToan, TrangThai = TrangThaiSuKien.DaLenLich, MoTa = $"Theo phiếu thay đổi {p.Ma}: {p.Ten}", DuKienBatDau = b, DuKienKetThuc = k, NguoiTao = nguoi };
        await haTang.GanHeThongAsync(s, ids);
        s.NhatKys.Add(new NhatKyDowntime { ThoiDiem = now, NguoiGhi = nguoi, NoiDung = $"Đăng ký downtime có kế hoạch từ phiếu thay đổi {p.Ma}." });
        db.SuKienDowntimes.Add(s);
        await db.LuuMoiAsync(soPhieu, "DT", so => s.Ma = so);
        p.SuKienDowntimeId = s.Id;
        var nhan = new List<string?>(await tb.CanBoCNTTAsync());
        foreach (var kh in s.Khoas) nhan.AddRange(await tb.TruongKhoaAsync(kh.KhoaPhongId));
        await tb.GuiAsync(nhan, LoaiThongBao.Downtime, $"Lịch downtime {p.Ten}: {b:HH:mm dd/MM} – {k:HH:mm dd/MM}", s.MoTa, $"/SuKienDowntime/Details/{s.Id}", boQua: nguoi);
        await db.SaveChangesAsync();
        return s;
    }

    /// <summary>FR-TD-07, FR-SL-09, FR-TD-09: sau khi ghi kết quả Hoàn thành.</summary>
    public async Task<List<string>> HoanTatAsync(PhieuThayDoi p, string nguoi)
    {
        var kq = new List<string>();
        if (p.TrangThai != TrangThaiThayDoi.HoanThanh) return kq;
        var now = DateTime.Now;
        if (p.TaiSanId is { } tsId)
        {
            lichSu.Ghi(tsId, LoaiSuKien.CapNhat, $"Thay đổi {p.Ma} hoàn thành: {p.Ten}", nguoi); kq.Add("đã ghi lịch sử tài sản");
        }
        var chinh = p.HeThongId ?? p.HeThongs.Select(h => (int?)h.HeThongId).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(p.PhienBanMoi) && chinh is { } hid && await db.HeThongThongTins.FirstOrDefaultAsync(h => h.Id == hid) is { } ht)
        {
            ht.PhienBan = p.PhienBanMoi.Trim(); kq.Add($"cập nhật phiên bản {ht.Ma} = {ht.PhienBan}");
        }
        if (p.Loai == LoaiThayDoi.TrienKhaiMoi && p.HeThongMoiId is { } moi && await db.HeThongThongTins.FirstOrDefaultAsync(h => h.Id == moi) is { } hm && hm.TrangThai == TrangThaiHeThong.DangTrienKhai)
        {
            hm.TrangThai = TrangThaiHeThong.VanHanh; hm.NgaySuDung ??= DateOnly.FromDateTime(now); kq.Add($"hệ thống {hm.Ma} chuyển sang Vận hành");
        }
        if ((p.LaThayDoiLon || p.Loai == LoaiThayDoi.TrienKhaiMoi) && p.DanhGiaSau is null)
        {
            p.DanhGiaSau = new DanhGiaSauThayDoi { Han = DateOnly.FromDateTime(now).AddDays(thamSo.SoNguyen(TS.JciDanhGiaSauTrienKhaiNgay)) };
            kq.Add($"lên lịch đánh giá sau triển khai {p.DanhGiaSau.Han:dd/MM/yyyy}");
        }
        if (p.LaHeThongSaoLuu && p.LanThuPhucHoiId is null)
        {
            var hs = HeThongIds(p);
            var h0 = hs.Count > 0 ? hs[0] : (int?)null;
            if (h0 is { } hh)
            {
                var l = new LanThuPhucHoi { HeThongId = hh, HanThucHien = DateOnly.FromDateTime(now).AddDays(14), LyDo = $"Thử phục hồi ngoài lịch sau thay đổi hệ thống sao lưu {p.Ma}", NguoiTao = nguoi };
                db.LanThuPhucHois.Add(l); await db.SaveChangesAsync(); p.LanThuPhucHoiId = l.Id; kq.Add("tạo lần thử phục hồi ngoài lịch (hạn 14 ngày)");
            }
        }
        return kq;
    }

    /// <summary>FR-TD-06: khi hoàn tất checklist → tạo bản ghi HTTT mới trạng thái Đang triển khai.</summary>
    public async Task<HeThongThongTin> TaoHeThongMoiAsync(PhieuThayDoi p, string ma, string nguoi)
    {
        if (p.Loai != LoaiThayDoi.TrienKhaiMoi) throw new DomainException("Chỉ áp dụng cho thay đổi loại Triển khai mới.");
        if (p.HeThongMoiId is not null) throw new DomainException("Đã tạo bản ghi hệ thống mới.");
        if (p.Checklist.Count == 0 || p.Checklist.Any(m => !m.Dat)) throw new DomainException("Hoàn tất checklist trước khi tạo bản ghi hệ thống.");
        ma = (ma ?? "").Trim().ToUpperInvariant();
        if (ma.Length == 0) throw new DomainException("Nhập mã hệ thống.");
        if (await db.HeThongThongTins.AnyAsync(h => h.Ma == ma)) throw new DomainException("Mã hệ thống đã tồn tại.");
        var h = new HeThongThongTin { Ma = ma, Ten = p.Ten, MoTa = p.MoTa, TrangThai = TrangThaiHeThong.DangTrienKhai, LaCongCuAi = p.LaCongCuHoTroQuyetDinh, PhuTrachChinh = p.NguoiDeXuat, NguoiTao = nguoi };
        db.HeThongThongTins.Add(h); await db.SaveChangesAsync();
        p.HeThongMoiId = h.Id; p.HeThongId ??= h.Id;
        return h;
    }
}
