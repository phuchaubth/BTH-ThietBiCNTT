using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Services;

public record DongDatHang(int DongDeXuatId, decimal SoLuong, decimal DonGia);
public record DongNhanHang(int DongDonHangId, decimal SoLuong, IReadOnlyList<string> Serials);

/// <summary>Mua sắm: đề xuất (PR) → duyệt nhiều cấp → đơn hàng (PO) → nhận hàng (nhập kho / tạo tài sản).</summary>
public class MuaSamService(AppDbContext db, SoPhieuService soPhieu, ThamSoService thamSo, ThongBaoService tb, KhoService kho,
    LichSuService lichSu, UserManager<AppUser> users)
{
    private static readonly TrangThaiDeXuat[] DeXuatMo = [TrangThaiDeXuat.Nhap, TrangThaiDeXuat.ChoDuyet, TrangThaiDeXuat.DaDuyet, TrangThaiDeXuat.DatMotPhan];
    private static readonly TrangThaiDonHang[] DonMo = [TrangThaiDonHang.Nhap, TrangThaiDonHang.DaDatHang, TrangThaiDonHang.NhanMotPhan];

    private List<string> DsTen(string ma) => thamSo.Chuoi(ma).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
    public decimal NguongLanhDao => thamSo.SoLon(TS.MuaSamNguongLanhDao);

    /// <summary>Tên đăng nhập được duyệt ở một cấp.</summary>
    public async Task<List<string>> NguoiDuyetCapAsync(int cap) => cap switch
    {
        DeXuatMuaSam.CapCNTT => DsTen(TS.MuaSamNguoiDuyetCNTT) is { Count: > 0 } ds ? ds : (await users.GetUsersInRoleAsync(VaiTro.QuanTri)).Select(u => u.UserName!).ToList(),
        DeXuatMuaSam.CapVatTu => DsTen(TS.MuaSamNguoiDuyetVatTu),
        DeXuatMuaSam.CapLanhDao => (await users.GetUsersInRoleAsync(VaiTro.LanhDao)).Where(u => u.DangHoatDong).Select(u => u.UserName!).ToList(),
        _ => []
    };

    public async Task<bool> DuocDuyetAsync(DeXuatMuaSam dx, string? nguoi) =>
        nguoi is not null && dx.TrangThai == TrangThaiDeXuat.ChoDuyet
        && (await NguoiDuyetCapAsync(dx.CapHienTai)).Contains(nguoi, StringComparer.OrdinalIgnoreCase);

    // ===== Đề xuất =====
    public async Task<DeXuatMuaSam> LapDeXuatAsync(DeXuatMuaSam dx)
    {
        dx.KiemTraDong();
        await NapDonViTinh(dx);
        db.DeXuatMuaSams.Add(dx);
        await db.LuuMoiAsync(soPhieu, "DX", so => dx.SoPhieu = so);
        return dx;
    }

    private async Task NapDonViTinh(DeXuatMuaSam dx)
    {
        var ids = dx.Dongs.Where(d => d.VatTuId is not null).Select(d => d.VatTuId!.Value).ToList();
        var vt = await db.VatTus.Where(v => ids.Contains(v.Id)).ToDictionaryAsync(v => v.Id);
        foreach (var d in dx.Dongs)
        {
            if (d.Loai == LoaiHangMua.VatTu && vt.TryGetValue(d.VatTuId!.Value, out var v))
            { d.DonViTinh ??= v.DonViTinh; d.LoaiThietBiId = null; if (d.DonGiaDuKien <= 0) d.DonGiaDuKien = v.DonGia; }
            if (d.Loai == LoaiHangMua.ThietBi) { d.VatTuId = null; d.DonViTinh ??= "Cái"; }
        }
    }

    public async Task GuiDuyetAsync(DeXuatMuaSam dx, string? nguoi)
    {
        dx.GuiDuyet(NguongLanhDao, DsTen(TS.MuaSamNguoiDuyetVatTu).Count > 0);
        await db.SaveChangesAsync();
        await BaoChoDuyet(dx, nguoi);
        await db.SaveChangesAsync();
    }

    private async Task BaoChoDuyet(DeXuatMuaSam dx, string? boQua) =>
        await tb.GuiAsync(await NguoiDuyetCapAsync(dx.CapHienTai), LoaiThongBao.ChoDuyet,
            $"Đề xuất mua sắm {dx.SoPhieu} chờ duyệt ({DeXuatMuaSam.TenCap(dx.CapHienTai)})",
            $"{dx.TieuDe}\nDự toán: {dx.TongDuToan:N0} đ", $"/MuaSam/DeXuat/{dx.Id}", boQua: boQua);

    public async Task DuyetAsync(DeXuatMuaSam dx, string nguoi, string ten, bool dongY, string? yKien)
    {
        if (!await DuocDuyetAsync(dx, nguoi)) throw new DomainException($"Bạn không thuộc danh sách duyệt cấp {DeXuatMuaSam.TenCap(dx.CapHienTai)}.");
        dx.Duyet(nguoi, ten, dongY, yKien, DateTime.Now);
        await db.SaveChangesAsync();
        if (dx.TrangThai == TrangThaiDeXuat.ChoDuyet) await BaoChoDuyet(dx, nguoi);
        else
        {
            var ok = dx.TrangThai == TrangThaiDeXuat.DaDuyet;
            var nhan = new List<string?> { dx.NguoiTao };
            if (ok) nhan.AddRange(await tb.CanBoCNTTAsync());
            await tb.GuiAsync(nhan, LoaiThongBao.KetQuaDuyet, $"Đề xuất {dx.SoPhieu} {(ok ? "đã được duyệt — có thể lập đơn hàng" : "bị từ chối")}",
                yKien, $"/MuaSam/DeXuat/{dx.Id}", ok ? MucDoThongBao.ThongTin : MucDoThongBao.CanhBao, boQua: nguoi);
        }
        await db.SaveChangesAsync();
    }

    // ===== Đơn hàng =====
    public async Task<DonMuaHang> LapDonHangAsync(DonMuaHang dh, IEnumerable<DongDatHang> dongs)
    {
        var ds = dongs.Where(d => d.SoLuong > 0).ToList();
        if (ds.Count == 0) throw new DomainException("Chọn ít nhất một dòng đề xuất và nhập số lượng đặt.");
        if (!await db.NhaCungCaps.AnyAsync(n => n.Id == dh.NhaCungCapId)) throw new DomainException("Chọn nhà cung cấp.");
        var ids = ds.Select(d => d.DongDeXuatId).ToList();
        var nguon = await db.DongDeXuats.Include(d => d.VatTu).Include(d => d.LoaiThietBi).Include(d => d.DeXuatMuaSam!).ThenInclude(x => x.Dongs)
            .Where(d => ids.Contains(d.Id)).ToDictionaryAsync(d => d.Id);
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var d in ds)
        {
            var g = nguon.GetValueOrDefault(d.DongDeXuatId) ?? throw new DomainException("Dòng đề xuất không tồn tại.");
            if (!g.DeXuatMuaSam!.ChoLapDon) throw new DomainException($"Đề xuất {g.DeXuatMuaSam.SoPhieu} chưa được duyệt hoặc đã đặt đủ.");
            if (d.SoLuong > g.ConLai) throw new DomainException($"'{g.TenHang}' chỉ còn {g.ConLai:0.##} chưa đặt.");
            if (d.DonGia < 0) throw new DomainException("Đơn giá không được âm.");
            g.SoLuongDaDat += d.SoLuong;
            dh.Dongs.Add(new DongDonHang { DongDeXuatId = g.Id, Loai = g.Loai, VatTuId = g.VatTuId, LoaiThietBiId = g.LoaiThietBiId, MoTa = g.MoTa, SoLuong = d.SoLuong, DonGia = d.DonGia });
        }
        foreach (var x in nguon.Values.Select(g => g.DeXuatMuaSam!).Distinct()) x.CapNhatTheoDonHang();
        dh.KhoanMucNganSachId ??= nguon.Values.Select(g => g.DeXuatMuaSam!.KhoanMucNganSachId).FirstOrDefault(k => k is not null);
        db.DonMuaHangs.Add(dh);
        await db.LuuMoiAsync(soPhieu, "DH", so => dh.SoPhieu = so);
        await tx.CommitAsync();
        return dh;
    }

    /// <summary>Đơn ≥ ngưỡng: Lãnh đạo xác nhận; dưới ngưỡng: người duyệt cấp 1 (Trưởng phòng CNTT) hoặc Quản trị.</summary>
    public async Task<bool> DuocDatHangAsync(DonMuaHang dh, string? nguoi)
    {
        if (nguoi is null) return false;
        var u = await users.FindByNameAsync(nguoi);
        if (u is null) return false;
        if (dh.TongTien >= NguongLanhDao) return await users.IsInRoleAsync(u, VaiTro.LanhDao);
        return await users.IsInRoleAsync(u, VaiTro.QuanTri) || DsTen(TS.MuaSamNguoiDuyetCNTT).Contains(nguoi, StringComparer.OrdinalIgnoreCase);
    }

    public async Task DatHangAsync(DonMuaHang dh, string nguoi)
    {
        if (!await DuocDatHangAsync(dh, nguoi))
            throw new DomainException(dh.TongTien >= NguongLanhDao ? $"Đơn từ {NguongLanhDao:N0} đ trở lên phải do Lãnh đạo xác nhận đặt hàng." : "Bạn không có quyền xác nhận đặt hàng.");
        dh.DatHang(DateTime.Now);
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.KetQuaDuyet, $"Đơn hàng {dh.SoPhieu} đã xác nhận đặt hàng",
            $"Tổng {dh.TongTien:N0} đ · hạn giao {dh.HanGiao:dd/MM/yyyy}", $"/MuaSam/DonHang/{dh.Id}", boQua: nguoi);
        await db.SaveChangesAsync();
    }

    public async Task HuyDonAsync(DonMuaHang dh)
    {
        if (dh.TrangThai is not (TrangThaiDonHang.Nhap or TrangThaiDonHang.DaDatHang) || dh.Dongs.Any(d => d.SoLuongDaNhan > 0))
            throw new DomainException("Chỉ huỷ đơn chưa nhận hàng.");
        var ids = dh.Dongs.Where(d => d.DongDeXuatId is not null).Select(d => d.DongDeXuatId!.Value).ToList();
        var nguon = await db.DongDeXuats.Include(d => d.DeXuatMuaSam!).ThenInclude(x => x.Dongs).Where(d => ids.Contains(d.Id)).ToListAsync();
        foreach (var d in dh.Dongs)
            if (nguon.FirstOrDefault(g => g.Id == d.DongDeXuatId) is { } g) g.SoLuongDaDat = Math.Max(0, g.SoLuongDaDat - d.SoLuong);
        foreach (var x in nguon.Select(g => g.DeXuatMuaSam!).Distinct())
        {
            if (x.TrangThai == TrangThaiDeXuat.DaDatDu) x.TrangThai = TrangThaiDeXuat.DatMotPhan;
            x.CapNhatTheoDonHang();
        }
        dh.TrangThai = TrangThaiDonHang.DaHuy;
        await db.SaveChangesAsync();
    }

    // ===== Nhận hàng =====
    /// <summary>
    /// Nhận hàng (một phần hoặc đủ). Vật tư → một phiếu nhập kho (lý do Nhập mua, đơn giá gồm thuế GTGT).
    /// Thiết bị → mỗi đơn vị một hồ sơ tài sản mới (Trong kho), serial + bảo hành theo đơn hàng.
    /// </summary>
    public async Task<PhieuNhanHang> NhanHangAsync(DonMuaHang dh, int? khoId, IEnumerable<DongNhanHang> dongs, string? soHoaDon, string? bienBan, string nguoi)
    {
        if (dh.TrangThai is not (TrangThaiDonHang.DaDatHang or TrangThaiDonHang.NhanMotPhan)) throw new DomainException("Đơn hàng chưa đặt hoặc đã nhận đủ / đã huỷ.");
        var ds = dongs.Where(d => d.SoLuong > 0).ToList();
        if (ds.Count == 0) throw new DomainException("Nhập số lượng nhận cho ít nhất một dòng.");
        var now = DateTime.Now; var homNay = DateOnly.FromDateTime(now);
        var heSo = 1 + dh.ThueSuat / 100;
        var pn = new PhieuNhanHang { DonMuaHangId = dh.Id, NgayNhan = now, NguoiNhan = nguoi, SoHoaDon = soHoaDon, BienBanNghiemThu = bienBan };
        var phieuKho = new PhieuKho { LyDo = LyDoPhieuKho.NhapMua, NhaCungCapId = dh.NhaCungCapId, SoChungTu = soHoaDon ?? dh.SoPhieu,
            NguoiGiaoNhan = dh.NhaCungCap?.Ten, DienGiai = $"Nhập mua theo đơn {dh.SoPhieu}" };
        var taiSanMoi = new List<TaiSan>();
        var maDaDung = new Dictionary<string, int>();
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var n in ds)
        {
            var d = dh.Dongs.FirstOrDefault(x => x.Id == n.DongDonHangId) ?? throw new DomainException("Dòng đơn hàng không hợp lệ.");
            if (n.SoLuong > d.ConLai) throw new DomainException($"'{d.TenHang}' chỉ còn {d.ConLai:0.##} chưa nhận.");
            var gia = Math.Round(d.DonGia * heSo, 0);
            d.SoLuongDaNhan += n.SoLuong;
            pn.GiaTri += gia * n.SoLuong;
            if (d.Loai == LoaiHangMua.VatTu)
                phieuKho.ChiTiets.Add(new ChiTietPhieuKho { VatTuId = d.VatTuId!.Value, SoLuong = n.SoLuong, DonGia = gia });
            else
            {
                if (n.SoLuong != Math.Floor(n.SoLuong)) throw new DomainException("Số lượng thiết bị phải là số nguyên.");
                var serial = n.Serials.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                if (serial.Count > 0 && serial.Count != (int)n.SoLuong) throw new DomainException($"'{d.TenHang}': nhập {n.SoLuong:0} serial (hoặc để trống) — đang có {serial.Count}.");
                if (serial.Count != serial.Distinct(StringComparer.OrdinalIgnoreCase).Count()) throw new DomainException("Serial bị trùng.");
                var loai = await db.LoaiThietBis.FindAsync(d.LoaiThietBiId) ?? throw new DomainException("Loại thiết bị không tồn tại.");
                for (var i = 0; i < (int)n.SoLuong; i++)
                {
                    var ts = new TaiSan
                    {
                        MaTaiSan = await MaTaiSanMoiAsync(loai.Ma, maDaDung), Ten = d.MoTa ?? loai.Ten, LoaiThietBiId = loai.Id,
                        SoSerial = serial.ElementAtOrDefault(i), NguyenGia = gia, NgayMua = homNay, NhaCungCapId = dh.NhaCungCapId,
                        HanBaoHanh = dh.BaoHanhThang > 0 ? homNay.AddMonths(dh.BaoHanhThang) : null,
                        SoHopDong = dh.HopDong?.SoHopDong ?? dh.SoPhieu, TrangThai = TrangThaiTaiSan.TrongKho,
                        GhiChu = $"Nhận theo đơn hàng {dh.SoPhieu}" + (soHoaDon is null ? "" : $", hoá đơn {soHoaDon}")
                    };
                    taiSanMoi.Add(ts); db.TaiSans.Add(ts);
                }
            }
        }
        if (phieuKho.ChiTiets.Count > 0)
        {
            phieuKho.KhoId = khoId ?? (await kho.KhoChinhAsync()).Id;
            await kho.LapAsync(phieuKho, nguoi);
            pn.PhieuKhoId = phieuKho.Id; pn.KhoId = phieuKho.KhoId;
        }
        await db.SaveChangesAsync();
        foreach (var ts in taiSanMoi) lichSu.Ghi(ts.Id, LoaiSuKien.TaoMoi, $"Nhập mới từ đơn hàng {dh.SoPhieu} (giá {ts.NguyenGia:N0} đ)", nguoi);
        pn.TaiSanDaTao = taiSanMoi.Count == 0 ? null : string.Join(", ", taiSanMoi.Select(t => t.MaTaiSan));
        dh.PhieuNhans.Add(pn);
        dh.CapNhatTheoNhanHang();
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return pn;
    }

    /// <summary>Mã tài sản mới: {mã loại}-{yy}-{số thứ tự 4 chữ số}.</summary>
    private async Task<string> MaTaiSanMoiAsync(string maLoai, Dictionary<string, int> daDung)
    {
        var prefix = $"{maLoai}-{DateTime.Now:yy}-";
        if (!daDung.TryGetValue(prefix, out var n))
        {
            var ma = await db.TaiSans.Where(t => t.MaTaiSan.StartsWith(prefix)).Select(t => t.MaTaiSan).ToListAsync();
            n = ma.Select(m => int.TryParse(m[prefix.Length..], out var k) ? k : 0).DefaultIfEmpty(0).Max();
        }
        daDung[prefix] = ++n;
        return prefix + n.ToString("D4");
    }

    // ===== Tự động =====
    /// <summary>Số lượng vật tư đang trong quy trình mua (đề xuất chưa đặt + đơn chưa nhận).</summary>
    public async Task<Dictionary<int, decimal>> DangMuaAsync()
    {
        var pr = await db.DongDeXuats.Where(d => d.VatTuId != null && DeXuatMo.Contains(d.DeXuatMuaSam!.TrangThai))
            .Select(d => new { Id = d.VatTuId!.Value, d.SoLuong, d.SoLuongDaDat }).ToListAsync();
        var po = await db.DongDonHangs.Where(d => d.VatTuId != null && DonMo.Contains(d.DonMuaHang!.TrangThai))
            .Select(d => new { Id = d.VatTuId!.Value, d.SoLuong, d.SoLuongDaNhan }).ToListAsync();
        return pr.Select(x => (x.Id, Sl: Math.Max(0, x.SoLuong - x.SoLuongDaDat))).Concat(po.Select(x => (x.Id, Sl: Math.Max(0, x.SoLuong - x.SoLuongDaNhan))))
            .GroupBy(x => x.Id).ToDictionary(g => g.Key, g => g.Sum(x => x.Sl));
    }

    /// <summary>
    /// Lập (hoặc bổ sung vào) đề xuất nháp tự động cho vật tư dưới định mức.
    /// Gợi ý = tồn tối đa (hoặc 2× tồn tối thiểu nếu chưa khai báo) − tồn hiện tại − đang mua. Trả về đề xuất (null nếu không cần).
    /// </summary>
    public async Task<DeXuatMuaSam?> TuDongDeXuatAsync(string? nguoi = "system")
    {
        var thap = await db.VatTus.Where(v => v.TonToiThieu > 0 && v.TonKho <= v.TonToiThieu).ToListAsync();
        if (thap.Count == 0) return null;
        var dangMua = await DangMuaAsync();
        var dx = await db.DeXuatMuaSams.Include(x => x.Dongs).FirstOrDefaultAsync(x => x.TuDong && x.TrangThai == TrangThaiDeXuat.Nhap);
        var moi = dx is null;
        dx ??= new DeXuatMuaSam { TieuDe = $"Bổ sung vật tư dưới định mức ({DateTime.Now:MM/yyyy})", CanCu = CanCuDeXuat.TonThap, TuDong = true, NguoiTao = nguoi,
            LyDo = "Hệ thống tự lập từ tồn kho dưới định mức. Kiểm tra lại số lượng, đơn giá trước khi gửi duyệt." };
        var them = 0;
        foreach (var v in thap)
        {
            var muc = v.TonToiDa > 0 ? v.TonToiDa : v.TonToiThieu * 2;
            var sl = Math.Ceiling(muc - v.TonKho - dangMua.GetValueOrDefault(v.Id));
            if (sl <= 0) continue;
            dx.Dongs.Add(new DongDeXuat { Loai = LoaiHangMua.VatTu, VatTuId = v.Id, DonViTinh = v.DonViTinh, SoLuong = sl, DonGiaDuKien = v.DonGia,
                GhiChu = $"Tồn {v.TonKho:0.##} / tối thiểu {v.TonToiThieu:0.##}" });
            them++;
        }
        if (them == 0) return null;
        if (moi) await LapDeXuatAsync(dx); else await db.SaveChangesAsync();
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.VatTu, $"Đề xuất nháp {dx.SoPhieu}: {them} vật tư dưới định mức cần mua",
            "Kiểm tra và gửi duyệt.", $"/MuaSam/DeXuat/{dx.Id}", boQua: nguoi);
        await db.SaveChangesAsync();
        return dx;
    }
}

/// <summary>Dòng bản tin hằng ngày + tự lập đề xuất từ tồn thấp.</summary>
public static class MuaSamTongHop
{
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var db = (AppDbContext)sp.GetService(typeof(AppDbContext))!;
        var ts = (ThamSoService)sp.GetService(typeof(ThamSoService))!;
        var kq = new List<string>();
        if (ts.BatTat(TS.MuaSamTuDongDeXuat) && await ((MuaSamService)sp.GetService(typeof(MuaSamService))!).TuDongDeXuatAsync() is { } dx)
            kq.Add($"• Đã lập đề xuất nháp {dx.SoPhieu} cho vật tư dưới định mức");
        var d = DateOnly.FromDateTime(now);
        var tre = await db.DonMuaHangs.Where(x => (x.TrangThai == TrangThaiDonHang.DaDatHang || x.TrangThai == TrangThaiDonHang.NhanMotPhan) && x.HanGiao < d)
            .Select(x => x.SoPhieu).ToListAsync(ct);
        if (tre.Count > 0) kq.Add($"• Đơn hàng quá hạn giao: {string.Join(", ", tre)}");
        var cho = await db.DeXuatMuaSams.CountAsync(x => x.TrangThai == TrangThaiDeXuat.ChoDuyet, ct);
        if (cho > 0) kq.Add($"• {cho} đề xuất mua sắm đang chờ duyệt");
        return kq;
    }
}
