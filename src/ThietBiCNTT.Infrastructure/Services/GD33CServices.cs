using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>FR-RR-03: gợi ý rủi ro từ dữ liệu; FR-RR-07: nhắc xem xét lại rủi ro cao; FR-AN-07: nhắc lỗ hổng quá hạn.</summary>
public class GoiYRuiRoService(AppDbContext db, HaTangService haTang, ThongBaoService tb, ThamSoService thamSo, SoPhieuService soPhieu)
{
    public async Task<int> SinhAsync(DateOnly homNay, CancellationToken ct = default)
    {
        var co = (await db.GoiYRuiRos.Select(g => g.Khoa).ToListAsync(ct)).ToHashSet();
        var moi = new List<GoiYRuiRo>();
        void Them(string khoa, string nd, NhomRuiRo nhom, int? ht, string? dl) { if (co.Add(khoa)) moi.Add(new GoiYRuiRo { Khoa = khoa, NoiDung = nd, Nhom = nhom, HeThongId = ht, DuLieu = dl }); }

        var canh = await haTang.CanhAsync(ct);
        var trongYeu = await db.HeThongThongTins.AsNoTracking().Include(h => h.HopDong).Where(h => h.TrangThai == TrangThaiHeThong.VanHanh && (h.MucTrongYeu == MucTrongYeu.ToiQuanTrong || h.MucTrongYeu == MucTrongYeu.Cao)).ToListAsync(ct);
        foreach (var h in trongYeu)
        {
            var spof = PhanTichPhuThuoc.DiemLoiDon(h.Id, canh);
            if (spof.Count > 0) Them($"SPOF:{h.Ma}", $"Hệ thống {h.Ma} có {spof.Count} điểm lỗi đơn (tài sản phụ thuộc không có dự phòng): một thiết bị hỏng có thể làm ngừng hệ thống.", NhomRuiRo.HaTangVatLy, h.Id, $"{spof.Count} tài sản");
            var tsIds = canh.Where(c => c.HeThongId == h.Id && c.TaiSanId != null).Select(c => c.TaiSanId!.Value).Distinct().ToList();
            var hetBh = await db.TaiSans.AsNoTracking().Where(t => tsIds.Contains(t.Id) && t.HanBaoHanh != null && t.HanBaoHanh < homNay).Select(t => t.MaTaiSan).ToListAsync(ct);
            if (hetBh.Count > 0) Them($"BH:{h.Ma}", $"{hetBh.Count} tài sản phục vụ hệ thống {h.Ma} đã hết bảo hành: nguy cơ chậm sửa chữa, thiếu phụ tùng.", NhomRuiRo.PhanCung, h.Id, string.Join(", ", hetBh.Take(8)));
            if (h.HopDong is { } hd && hd.SapHetHan(homNay, 60)) Them($"HD:{hd.Id}:{hd.HieuLucDen:yyyyMMdd}", $"Hợp đồng hỗ trợ {hd.SoHopDong} của hệ thống {h.Ma} sắp hết hạn {hd.HieuLucDen:dd/MM/yyyy}: nguy cơ gián đoạn hỗ trợ.", NhomRuiRo.NhaCungCap, h.Id, hd.SoHopDong);
        }
        var nam = homNay.AddMonths(-12).ToDateTime(TimeOnly.MinValue);
        foreach (var l in await db.LanThuPhucHois.AsNoTracking().Include(x => x.HeThong).Where(x => x.DaChot && x.Dat == false && x.NgayChot >= nam).ToListAsync(ct))
            Them($"PH:{l.Id}", $"Thử phục hồi {l.HeThong?.Ma} ngày {l.HanThucHien:dd/MM/yyyy} không đạt: chưa chắc phục hồi được dữ liệu khi sự cố.", NhomRuiRo.DuLieu, l.HeThongId, null);
        var thang = DateTime.Now.AddDays(-30);
        foreach (var tv in await db.TacVuSaoLuus.AsNoTracking().Include(t => t.HeThong).Where(t => t.DangApDung && t.KetQuas.Count(k => k.KetQua == KetQuaSaoLuu.Loi && k.ThoiDiem >= thang) >= 3).ToListAsync(ct))
            Them($"SL:{tv.Id}", $"Tác vụ sao lưu \"{tv.Ten}\" ({tv.HeThong?.Ma}) lỗi từ 3 lần trong 30 ngày: dữ liệu có thể không có bản sao lưu dùng được.", NhomRuiRo.DuLieu, tv.HeThongId, null);
        foreach (var lh in await db.LoHongs.AsNoTracking().Where(x => (x.TrangThai == TrangThaiLoHong.Mo || x.TrangThai == TrangThaiLoHong.DangVa) && x.HanVa < homNay && x.Cvss >= 7).ToListAsync(ct))
            Them($"LH:{lh.Id}", $"Lỗ hổng {lh.MaCve ?? lh.TieuDe} (CVSS {lh.Cvss}) quá hạn vá: nguy cơ bị khai thác.", NhomRuiRo.AnNinhMang, lh.HeThongId, null);
        if (moi.Count == 0) return 0;
        db.GoiYRuiRos.AddRange(moi); await db.SaveChangesAsync(ct);
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.RuiRo, $"Có {moi.Count} gợi ý rủi ro mới từ dữ liệu hệ thống", "Xem và chấp nhận hoặc bỏ qua tại Sổ rủi ro › Gợi ý.", "/GoiYRuiRo");
        return moi.Count;
    }

    /// <summary>Chấp nhận gợi ý → tạo rủi ro nháp cần chấm điểm.</summary>
    public async Task<RuiRo> ChapNhanAsync(int id, string nguoi, string nguoiPhuTrach)
    {
        var g = await db.GoiYRuiRos.FirstOrDefaultAsync(x => x.Id == id) ?? throw new DomainException("Không tìm thấy gợi ý.");
        if (g.TrangThai != TrangThaiGoiY.Moi) throw new DomainException("Gợi ý đã được xử lý.");
        var r = new RuiRo { MoTa = g.NoiDung, Nhom = g.Nhom, HeThongId = g.HeThongId, NguoiChiuTrachNhiem = nguoiPhuTrach, NguoiTao = nguoi, KiemSoatHienCo = "Chưa đánh giá (tạo từ gợi ý hệ thống)", NgayXemXetLai = DateOnly.FromDateTime(DateTime.Today).AddDays(thamSo.SoNguyen(TS.JciXemXetRuiRoCaoNgay)) };
        r.ChamDiem(3, 3, null, null, "", nguoi, DateTime.Now);
        db.RuiRos.Add(r);
        await db.LuuMoiAsync(soPhieu, "RR", so => r.Ma = so);
        g.TrangThai = TrangThaiGoiY.DaChapNhan; g.RuiRoId = r.Id; g.NguoiXuLy = nguoi; g.NgayXuLy = DateTime.Now;
        await db.SaveChangesAsync();
        return r;
    }

    /// <summary>FR-RR-07 và FR-AN-07: nhắc người phụ trách.</summary>
    public async Task NhacAsync(DateTime now, DateOnly homNay, CancellationToken ct = default)
    {
        var ngay = thamSo.SoNguyen(TS.JciXemXetRuiRoCaoNgay);
        foreach (var r in await db.RuiRos.Where(x => (x.TrangThai == TrangThaiRuiRo.Mo || x.TrangThai == TrangThaiRuiRo.DangXuLy) && x.NguoiChiuTrachNhiem != null).Include(x => x.LichSus).ToListAsync(ct))
        {
            if (r.Muc < MucRuiRo.Cao) continue;
            var cuoi = r.LichSus.Count > 0 ? r.LichSus.Max(l => l.NgayDanhGia) : r.NgayTao;
            if (cuoi.AddDays(ngay) > now) continue;
            var td = $"Xem xét lại rủi ro {r.Ma} ({r.Muc.TenHienThi()})";
            if (!await db.ThongBaos.AnyAsync(t => t.TieuDe == td && t.ThoiGian >= now.AddDays(-14), ct)) await tb.GuiAsync([r.NguoiChiuTrachNhiem], LoaiThongBao.RuiRo, td, $"FR-RR-07: rủi ro mức Cao trở lên xem xét lại mỗi {ngay} ngày; lần chấm cuối {cuoi:dd/MM/yyyy}.", $"/RuiRo/Details/{r.Id}", MucDoThongBao.CanhBao);
        }
        var cntt = await tb.CanBoCNTTAsync();
        foreach (var l in await db.LoHongs.Where(x => (x.TrangThai == TrangThaiLoHong.Mo || x.TrangThai == TrangThaiLoHong.DangVa) && !x.DaNhacQuaHan && x.HanVa < homNay).ToListAsync(ct))
        {
            l.DaNhacQuaHan = true;
            await tb.GuiAsync(l.NguoiPhuTrach is null ? cntt : [l.NguoiPhuTrach], LoaiThongBao.AnNinhMang, $"[Quá hạn vá] Lỗ hổng {l.MaCve ?? l.TieuDe} (CVSS {l.Cvss})", $"Hạn {l.HanVa:dd/MM/yyyy}.", $"/LoHong/Details/{l.Id}", l.Muc >= MucLoHong.Cao ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao);
        }
        await db.SaveChangesAsync(ct);
    }
}
