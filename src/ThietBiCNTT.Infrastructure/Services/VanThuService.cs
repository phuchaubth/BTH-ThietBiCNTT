using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Văn thư Phòng CNTT: sổ văn bản đến/đi, trình duyệt tờ trình, phân công, theo dõi tiến độ, nhắc hạn.</summary>
public class VanThuService(AppDbContext db, ThamSoService thamSo, ThongBaoService thongBao, UserManager<AppUser> users)
{
    public static string TenEnum(Enum e) =>
        e.GetType().GetMember(e.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
            .OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault()?.GetName() ?? e.ToString();
    private static string Link(VanBan vb) => $"/VanThu/ChiTiet/{vb.Id}";
    private List<string> Ds(string ma) => thamSo.Chuoi(ma).Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

    public List<string> NguoiVanThu => Ds(TS.VanThuNguoiVanThu);
    public List<string> NguoiPhanCong => Ds(TS.VanThuNguoiPhanCong);
    public List<string> NguoiDuyetMacDinh => Ds(TS.VanThuNguoiDuyet);

    public bool LaVanThu(string? u, bool quanTri) => quanTri || (u is not null && NguoiVanThu.Contains(u, StringComparer.OrdinalIgnoreCase));
    public bool LaPhanCong(string? u, bool quanTri) => quanTri || (u is not null && NguoiPhanCong.Contains(u, StringComparer.OrdinalIgnoreCase));

    /// <summary>Tài khoản được chọn làm người xử lý / người duyệt: CNTT, Quản trị, Lãnh đạo đang hoạt động.</summary>
    public async Task<List<AppUser>> CanBoAsync()
    {
        var ds = new List<AppUser>();
        foreach (var r in new[] { VaiTro.QuanTri, VaiTro.CanBoCNTT, VaiTro.LanhDao }) ds.AddRange(await users.GetUsersInRoleAsync(r));
        return ds.Where(u => u.DangHoatDong).DistinctBy(u => u.Id).OrderBy(u => u.UserName).ToList();
    }

    public int HanGoiY(DoKhanVanBan doKhan)
    {
        var n = thamSo.SoNguyen(TS.VanThuHanMacDinh);
        return doKhan == DoKhanVanBan.Thuong ? n : Math.Max(1, n / 3);
    }

    /// <summary>Lưu với số tự tăng theo năm: nếu hai người cùng vào sổ (trùng unique index) thì lấy số kế tiếp.</summary>
    private async Task LuuCoSoAsync(Func<Task<int>> soTiep, Action<int> gan)
    {
        for (var lan = 1; ; lan++)
        {
            gan(await soTiep());
            try { await db.SaveChangesAsync(); return; }
            catch (DbUpdateException) when (lan < 5) { await Task.Delay(Random.Shared.Next(20, 120)); }
        }
    }

    private async Task<int> SoDenTiepAsync(int nam) => (await db.VanBans.Where(x => x.Nam == nam && x.SoDen != null).MaxAsync(x => x.SoDen) ?? 0) + 1;
    private async Task<int> SoDiTiepAsync(int nam) => (await db.VanBans.Where(x => x.Nam == nam && x.SoDi != null).MaxAsync(x => x.SoDi) ?? 0) + 1;

    public async Task VaoSoDenAsync(VanBan vb, string nguoi)
    {
        var now = DateTime.Now;
        vb.Huong = HuongVanBan.Den;
        var nam = (vb.NgayDen ?? now).Year;
        var lan = 0;
        db.VanBans.Add(vb);
        await LuuCoSoAsync(() => SoDenTiepAsync(nam), so =>
        {
            if (lan++ == 0) vb.VaoSoDen(so, nguoi, now);
            else { vb.SoDen = so; vb.NhatKys[^1].NoiDung = $"Vào sổ văn bản đến số {so}/{vb.Nam}"; }
        });
        await thongBao.GuiAsync(NguoiPhanCong, LoaiThongBao.ChoDuyet, $"Văn bản đến số {vb.SoDen}: chờ phân công",
            $"{vb.CoQuanBanHanh} — {vb.SoKyHieu}\n{vb.TrichYeu}", Link(vb),
            vb.DoKhan == DoKhanVanBan.Thuong ? MucDoThongBao.ThongTin : MucDoThongBao.CanhBao, boQua: nguoi);
        await db.SaveChangesAsync();
    }

    public async Task TaoDuThaoAsync(VanBan vb, string nguoi)
    {
        vb.KhoiTaoDuThao(nguoi, DateTime.Now);
        db.VanBans.Add(vb);
        await db.SaveChangesAsync();
    }

    public async Task PhanCongAsync(VanBan vb, string chuTri, string? phoiHop, DateTime? han, string? yKien, string nguoi)
    {
        var hopLe = (await CanBoAsync()).Select(u => u.UserName!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var x in new[] { chuTri }.Concat((phoiHop ?? "").Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            if (!string.IsNullOrWhiteSpace(x) && !hopLe.Contains(x)) throw new DomainException($"Tài khoản '{x}' không phải cán bộ CNTT/Lãnh đạo đang hoạt động.");
        vb.PhanCong(chuTri.Trim(), phoiHop, han, yKien, nguoi, DateTime.Now);
        var han2 = vb.HanXuLy is null ? "" : $" · hạn {vb.HanXuLy:dd/MM/yyyy}";
        await thongBao.GuiAsync([vb.NguoiChuTri], LoaiThongBao.GiaoViec, $"Bạn chủ trì xử lý văn bản {vb.SoHienThi}{han2}",
            $"{vb.TrichYeu}{(vb.YKienChiDao is null ? "" : $"\nÝ kiến chỉ đạo: {vb.YKienChiDao}")}", Link(vb), boQua: nguoi);
        await thongBao.GuiAsync(vb.NguoiPhoiHopDs, LoaiThongBao.GiaoViec, $"Bạn phối hợp xử lý văn bản {vb.SoHienThi}{han2}",
            $"{vb.TrichYeu}\nChủ trì: {vb.NguoiChuTri}", Link(vb), boQua: nguoi);
        await db.SaveChangesAsync();
    }

    public async Task CapNhatTienDoAsync(VanBan vb, int tienDo, string noiDung, string nguoi)
    {
        vb.CapNhatTienDo(tienDo, noiDung, nguoi, DateTime.Now);
        var nhan = (vb.Huong == HuongVanBan.Den ? NguoiPhanCong : [vb.NguoiSoan ?? ""]).Append(vb.NguoiChuTri);
        await thongBao.GuiAsync(nhan, LoaiThongBao.BinhLuan, $"Tiến độ {tienDo}% — văn bản {vb.SoHienThi}", $"{nguoi}: {noiDung}", Link(vb),
            boQua: nguoi, chiTrongUngDung: true);
        await db.SaveChangesAsync();
    }

    public async Task HoanThanhAsync(VanBan vb, string ketQua, int? vanBanTraLoiId, string nguoi)
    {
        if (vanBanTraLoiId is { } t && !await db.VanBans.AnyAsync(x => x.Id == t && x.Huong == HuongVanBan.Di))
            throw new DomainException("Văn bản trả lời phải là văn bản đi trong sổ.");
        vb.HoanThanh(ketQua, nguoi, DateTime.Now, vanBanTraLoiId);
        var nhan = (vb.Huong == HuongVanBan.Den ? NguoiPhanCong : [vb.NguoiSoan ?? ""]).Concat(vb.NguoiPhoiHopDs).Append(vb.NguoiChuTri);
        await thongBao.GuiAsync(nhan, LoaiThongBao.KetQuaDuyet, $"Đã hoàn thành văn bản {vb.SoHienThi}", $"{vb.TrichYeu}\nKết quả: {vb.KetQua}", Link(vb), boQua: nguoi);
        await db.SaveChangesAsync();
    }

    public async Task TrinhDuyetAsync(VanBan vb, IReadOnlyList<string> nguoiDuyet, string nguoi)
    {
        var hopLe = (await CanBoAsync()).Select(u => u.UserName!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var x in nguoiDuyet.Where(x => !string.IsNullOrWhiteSpace(x)))
            if (!hopLe.Contains(x.Trim())) throw new DomainException($"Tài khoản '{x}' không được duyệt văn bản (cần là Quản trị, CNTT hoặc Lãnh đạo đang hoạt động).");
        vb.TrinhDuyet(nguoiDuyet, nguoi, DateTime.Now);
        await BaoNguoiDuyetAsync(vb, nguoi);
        await db.SaveChangesAsync();
    }

    private Task BaoNguoiDuyetAsync(VanBan vb, string? boQua) =>
        thongBao.GuiAsync([vb.BuocDangCho!.NguoiDuyet], LoaiThongBao.ChoDuyet, $"{TenEnum(vb.Loai)} chờ bạn duyệt: {vb.TrichYeu}",
            $"Người soạn: {vb.NguoiSoan} · bước {vb.BuocHienTai}/{vb.CacBuocLuotNay.Count()}", Link(vb),
            vb.DoKhan == DoKhanVanBan.Thuong ? MucDoThongBao.ThongTin : MucDoThongBao.CanhBao, boQua: boQua);

    public async Task DuyetAsync(VanBan vb, string nguoi, bool dongY, string? yKien)
    {
        var xong = vb.Duyet(nguoi, dongY, yKien, DateTime.Now);
        if (!dongY)
            await thongBao.GuiAsync([vb.NguoiSoan], LoaiThongBao.TraLai, $"{TenEnum(vb.Loai)} bị trả lại: {vb.TrichYeu}", $"{nguoi}: {yKien}", Link(vb), MucDoThongBao.CanhBao);
        else if (xong)
        {
            await thongBao.GuiAsync([vb.NguoiSoan], LoaiThongBao.KetQuaDuyet, $"{TenEnum(vb.Loai)} đã được duyệt: {vb.TrichYeu}", "Chuyển văn thư cấp số, phát hành.", Link(vb), boQua: nguoi);
            await thongBao.GuiAsync(NguoiVanThu, LoaiThongBao.ChoDuyet, $"Cấp số phát hành: {vb.TrichYeu}", $"{TenEnum(vb.Loai)} của {vb.NguoiSoan} đã duyệt xong.", Link(vb), boQua: nguoi);
        }
        else await BaoNguoiDuyetAsync(vb, nguoi);
        await db.SaveChangesAsync();
    }

    public async Task PhatHanhAsync(VanBan vb, DateTime ngay, string? nguoiKy, string? chucVu, string nguoi)
    {
        var now = DateTime.Now;
        if (ngay.Date > now.Date) throw new DomainException("Ngày ban hành không được sau hôm nay.");
        var donVi = thamSo.Chuoi(TS.VanThuKyHieu);
        var lan = 0;
        await LuuCoSoAsync(() => SoDiTiepAsync(ngay.Year), so =>
        {
            if (lan++ == 0) vb.PhatHanh(so, donVi, ngay, nguoiKy, chucVu, nguoi, now);
            else
            {
                vb.SoDi = so; vb.SoKyHieu = VanBan.TaoSoKyHieu(so, vb.Loai, donVi);
                vb.NhatKys[^1].NoiDung = $"Cấp số {vb.SoKyHieu} ngày {ngay:dd/MM/yyyy}, phát hành tới: {vb.NoiNhan}";
            }
        });
        await thongBao.GuiAsync([vb.NguoiSoan], LoaiThongBao.KetQuaDuyet, $"Đã phát hành {vb.SoKyHieu}: {vb.TrichYeu}",
            vb.TheoDoiThucHien ? "Văn bản chuyển sang theo dõi thực hiện — cập nhật tiến độ đến khi hoàn thành." : null, Link(vb), boQua: nguoi);
        await db.SaveChangesAsync();
    }

    /// <summary>Điền nội dung mẫu: {{NGAY}}, {{DIA_DANH}}, {{DON_VI}}, {{BENH_VIEN}}, {{NGUOI_SOAN}}, {{TRICH_YEU}}, {{DE_XUAT}}.</summary>
    public string DienMau(string mau, string? trichYeu, string? nguoiSoan, DeXuatMuaSam? dx, DateTime now)
    {
        var vi = CultureInfo.GetCultureInfo("vi-VN");
        var deXuat = dx is null ? "" : string.Join("\n", dx.Dongs.Select((d, i) =>
                $"{i + 1}. {(d.Loai == LoaiHangMua.VatTu ? d.VatTu?.Ten : d.LoaiThietBi?.Ten + " — " + d.MoTa)}: {d.SoLuong.ToString("0.##", vi)} {d.DonViTinh} × {d.DonGiaDuKien.ToString("N0", vi)} đ = {d.ThanhTien.ToString("N0", vi)} đ"))
            + $"\nTổng dự toán: {dx.TongDuToan.ToString("N0", vi)} đ (đề xuất {dx.SoPhieu}{(string.IsNullOrEmpty(dx.LyDo) ? "" : $"; lý do: {dx.LyDo}")}).";
        return mau.Replace("{{NGAY}}", $"ngày {now:dd} tháng {now:MM} năm {now:yyyy}")
            .Replace("{{DIA_DANH}}", thamSo.Chuoi(TS.VanThuDiaDanh))
            .Replace("{{DON_VI}}", thamSo.Chuoi(TS.TenPhongCNTT))
            .Replace("{{BENH_VIEN}}", thamSo.Chuoi(TS.TenBenhVien))
            .Replace("{{NGUOI_SOAN}}", nguoiSoan ?? "")
            .Replace("{{TRICH_YEU}}", trichYeu ?? "")
            .Replace("{{DE_XUAT}}", deXuat);
    }

    /// <summary>
    /// Nhắc hạn: mỗi văn bản chưa kết thúc, sắp đến hạn (≤ N ngày) hoặc quá hạn → nhắc người đang phải làm,
    /// tối đa một lần/ngày, sau giờ bản tin tổng hợp.
    /// </summary>
    public async Task<int> NhacHanAsync(DateTime now, CancellationToken ct = default)
    {
        if (TimeOnly.FromDateTime(now) < thamSo.Gio(TS.GioTongHop)) return 0;
        var homNay = now.Date;
        var gioiHan = homNay.AddDays(thamSo.SoNguyen(TS.VanThuNhacTruocNgay) + 1);
        var mo = new[] { TrangThaiVanBan.DuThao, TrangThaiVanBan.ChoDuyet, TrangThaiVanBan.TraLai, TrangThaiVanBan.DaDuyet, TrangThaiVanBan.ChoPhanCong, TrangThaiVanBan.DangXuLy };
        var ds = await db.VanBans.Include(x => x.BuocDuyets)
            .Where(x => mo.Contains(x.TrangThai) && x.HanXuLy != null && x.HanXuLy < gioiHan && (x.NgayNhacCuoi == null || x.NgayNhacCuoi < homNay))
            .ToListAsync(ct);
        foreach (var vb in ds)
        {
            var quaHan = vb.QuaHan(now);
            IEnumerable<string?> nhan = vb.TrangThai switch
            {
                TrangThaiVanBan.ChoDuyet => [vb.BuocDangCho?.NguoiDuyet],
                TrangThaiVanBan.ChoPhanCong => NguoiPhanCong,
                TrangThaiVanBan.DaDuyet => NguoiVanThu,
                TrangThaiVanBan.DangXuLy => vb.NguoiPhoiHopDs.Prepend(vb.NguoiChuTri),
                _ => [vb.NguoiSoan]
            };
            if (quaHan && vb.Huong == HuongVanBan.Den) nhan = nhan.Concat(NguoiPhanCong);
            var con = (vb.HanXuLy!.Value.Date - homNay).Days;
            await thongBao.GuiAsync(nhan, LoaiThongBao.SapDenHan,
                quaHan ? $"Quá hạn {-con} ngày: văn bản {vb.SoHienThi}" : con == 0 ? $"Đến hạn hôm nay: văn bản {vb.SoHienThi}" : $"Còn {con} ngày: văn bản {vb.SoHienThi}",
                $"{vb.TrichYeu}\nTrạng thái: {TenEnum(vb.TrangThai)} · hạn {vb.HanXuLy:dd/MM/yyyy}", Link(vb),
                quaHan ? MucDoThongBao.CanhBao : MucDoThongBao.ThongTin);
            vb.NgayNhacCuoi = now;
        }
        if (ds.Count > 0) await db.SaveChangesAsync(ct);
        return ds.Count;
    }
}

public static class VanThuTongHop
{
    /// <summary>Dòng bản tin hằng ngày: văn bản quá hạn / chờ phân công / chờ cấp số.</summary>
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var db = (AppDbContext)sp.GetService(typeof(AppDbContext))!;
        var homNay = now.Date;
        var kq = new List<string>();
        var mo = new[] { TrangThaiVanBan.DuThao, TrangThaiVanBan.ChoDuyet, TrangThaiVanBan.TraLai, TrangThaiVanBan.DaDuyet, TrangThaiVanBan.ChoPhanCong, TrangThaiVanBan.DangXuLy };
        var qua = await db.VanBans.CountAsync(x => mo.Contains(x.TrangThai) && x.HanXuLy < homNay, ct);
        if (qua > 0) kq.Add($"• {qua} văn bản quá hạn xử lý");
        var cho = await db.VanBans.CountAsync(x => x.TrangThai == TrangThaiVanBan.ChoPhanCong, ct);
        if (cho > 0) kq.Add($"• {cho} văn bản đến chờ phân công");
        var so = await db.VanBans.CountAsync(x => x.TrangThai == TrangThaiVanBan.DaDuyet, ct);
        if (so > 0) kq.Add($"• {so} văn bản đã duyệt chờ cấp số phát hành");
        return kq;
    }
}
