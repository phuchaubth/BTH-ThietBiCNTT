using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Nghiệp vụ kho: lập & ghi sổ phiếu kho, tồn theo kho, thẻ kho, giá bình quân.</summary>
public class KhoService(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb)
{
    public static string TienTo(LoaiPhieuKho l) => l switch
    { LoaiPhieuKho.Nhap => "NK", LoaiPhieuKho.Xuat => "XK", LoaiPhieuKho.Chuyen => "CK", _ => "DC-K" };

    public async Task<Kho> KhoChinhAsync() =>
        await db.Khos.Where(k => k.DangHoatDong).OrderBy(k => k.Loai).ThenBy(k => k.Id).FirstOrDefaultAsync()
        ?? throw new DomainException("Chưa có kho nào — tạo kho trong Danh mục kho.");

    public async Task<decimal> TonAsync(int khoId, int vatTuId) =>
        await db.TonKhoVatTus.Where(t => t.KhoId == khoId && t.VatTuId == vatTuId).Select(t => (decimal?)t.SoLuong).FirstOrDefaultAsync() ?? 0;

    /// <summary>
    /// Lập phiếu (gán số, lưu). Nhập/xuất/chuyển ghi sổ ngay; điều chỉnh (kiểm kê kho) chờ Quản trị duyệt.
    /// </summary>
    public async Task<PhieuKho> LapAsync(PhieuKho p, string? nguoi, bool ghiSoNgay = true)
    {
        p.Loai = PhieuKho.LoaiCua(p.LyDo);
        p.KiemTraHopLe();
        await using var tx = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync() : null;
        db.PhieuKhos.Add(p);
        await db.LuuMoiAsync(soPhieu, TienTo(p.Loai), so => p.SoPhieu = so);
        if (p.Loai != LoaiPhieuKho.DieuChinh && ghiSoNgay) await GhiSoAsync(p, nguoi);
        if (tx is not null) await tx.CommitAsync();
        return p;
    }

    public async Task DuyetAsync(PhieuKho p, string nguoi)
    {
        if (p.TrangThai != TrangThaiPhieuKho.ChoDuyet) throw new DomainException("Phiếu không ở trạng thái chờ duyệt.");
        if (string.Equals(p.NguoiTao, nguoi, StringComparison.OrdinalIgnoreCase)) throw new DomainException("Người lập phiếu không tự duyệt.");
        p.NguoiDuyet = nguoi;
        await using var tx = await db.Database.BeginTransactionAsync();
        await GhiSoAsync(p, nguoi);
        await tx.CommitAsync();
    }

    private async Task GhiSoAsync(PhieuKho p, string? nguoi)
    {
        if (p.TrangThai == TrangThaiPhieuKho.DaGhiSo) throw new DomainException("Phiếu đã ghi sổ.");
        var now = DateTime.Now;
        var choAm = thamSo.BatTat(TS.KhoChoXuatAm);
        var ids = p.ChiTiets.Select(c => c.VatTuId).ToList();
        var vatTus = await db.VatTus.Where(v => ids.Contains(v.Id)).ToDictionaryAsync(v => v.Id);
        var dien = p.DienGiai ?? EnumTen.TenHienThi(p.LyDo);
        foreach (var c in p.ChiTiets)
        {
            var v = vatTus.GetValueOrDefault(c.VatTuId) ?? throw new DomainException("Vật tư không tồn tại.");
            switch (p.Loai)
            {
                case LoaiPhieuKho.Nhap:
                    if (c.DonGia <= 0) c.DonGia = v.DonGia;
                    v.CapNhatGiaBinhQuan(c.SoLuong, c.DonGia);
                    await BienDong(p, p.KhoId, v, c.SoLuong, c.DonGia, dien, now);
                    break;
                case LoaiPhieuKho.Xuat:
                    c.DonGia = v.DonGia;
                    await KiemTraDuTon(p.KhoId, v, c.SoLuong, choAm);
                    await BienDong(p, p.KhoId, v, -c.SoLuong, c.DonGia, dien, now);
                    break;
                case LoaiPhieuKho.Chuyen:
                    c.DonGia = v.DonGia;
                    await KiemTraDuTon(p.KhoId, v, c.SoLuong, choAm);
                    await BienDong(p, p.KhoId, v, -c.SoLuong, c.DonGia, dien, now);
                    await BienDong(p, p.KhoDenId!.Value, v, c.SoLuong, c.DonGia, dien, now);
                    break;
                case LoaiPhieuKho.DieuChinh:
                    if (c.DonGia <= 0) c.DonGia = v.DonGia;
                    if (c.SoLuong < 0) await KiemTraDuTon(p.KhoId, v, -c.SoLuong, false);
                    await BienDong(p, p.KhoId, v, c.SoLuong, c.DonGia, dien, now);
                    break;
            }
        }
        p.TrangThai = TrangThaiPhieuKho.DaGhiSo;
        p.NgayGhiSo = now;
        await db.SaveChangesAsync();
        foreach (var v in vatTus.Values.Where(v => v.DuoiTonToiThieu && p.Loai is LoaiPhieuKho.Xuat or LoaiPhieuKho.DieuChinh))
            await tb.VatTuDuoiDinhMucAsync(v);
        await db.SaveChangesAsync();
    }

    private async Task KiemTraDuTon(int khoId, VatTu v, decimal soLuong, bool choAm)
    {
        var ton = await TonAsync(khoId, v.Id);
        if (!choAm && soLuong > ton) throw new DomainException($"'{v.Ten}' không đủ tồn tại kho (còn {ton:0.##} {v.DonViTinh}).");
    }

    /// <summary>Cộng/trừ tồn theo kho và tổng tồn vật tư, ghi một dòng thẻ kho.</summary>
    private async Task BienDong(PhieuKho p, int khoId, VatTu v, decimal delta, decimal donGia, string dien, DateTime now)
    {
        var t = await db.TonKhoVatTus.FirstOrDefaultAsync(x => x.KhoId == khoId && x.VatTuId == v.Id)
                ?? db.TonKhoVatTus.Local.FirstOrDefault(x => x.KhoId == khoId && x.VatTuId == v.Id);
        if (t is null) { t = new TonKhoVatTu { KhoId = khoId, VatTuId = v.Id }; db.TonKhoVatTus.Add(t); }
        t.SoLuong += delta;
        v.TonKho += p.Loai == LoaiPhieuKho.Chuyen ? 0 : delta;
        db.TheKhos.Add(new TheKho { KhoId = khoId, VatTuId = v.Id, PhieuKho = p, ThoiGian = now, Nhap = delta > 0 ? delta : 0, Xuat = delta < 0 ? -delta : 0,
            TonSau = t.SoLuong, DonGia = donGia, DienGiai = $"{p.SoPhieu} · {dien}" });
    }

    /// <summary>Đồng bộ dữ liệu cũ (trước khi có nhiều kho): tạo kho chính, đưa tổng tồn hiện có vào kho chính.</summary>
    public static async Task DongBoAsync(AppDbContext db)
    {
        if (!await db.Khos.AnyAsync())
        {
            db.Khos.Add(new Kho { Ma = "KHO-CNTT", Ten = "Kho chính Phòng CNTT", Loai = LoaiKho.Chinh });
            await db.SaveChangesAsync();
        }
        var chinh = await db.Khos.OrderBy(k => k.Loai).ThenBy(k => k.Id).FirstAsync();
        var coTon = await db.TonKhoVatTus.Select(t => t.VatTuId).Distinct().ToListAsync();
        var thieu = await db.VatTus.Where(v => v.TonKho != 0 && !coTon.Contains(v.Id)).ToListAsync();
        foreach (var v in thieu) db.TonKhoVatTus.Add(new TonKhoVatTu { KhoId = chinh.Id, VatTuId = v.Id, SoLuong = v.TonKho });
        if (thieu.Count > 0) await db.SaveChangesAsync();
    }
}
