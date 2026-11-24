using System.Net.Http.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>
/// Tạo thông báo trong ứng dụng + xếp hàng email/Telegram theo cài đặt của từng người dùng.
/// Chỉ thêm vào DbContext — người gọi tự SaveChanges (cùng giao dịch với nghiệp vụ).
/// </summary>
public class ThongBaoService(AppDbContext db, ThamSoService thamSo, UserManager<AppUser> users)
{
    private string DiaChiGoc => thamSo.Chuoi(TS.DiaChiTruyCap).TrimEnd('/');

    public async Task GuiAsync(IEnumerable<string?> nguoiNhan, LoaiThongBao loai, string tieuDe, string? noiDung, string? link,
        MucDoThongBao mucDo = MucDoThongBao.ThongTin, bool guiNhomCNTT = false, string? boQua = null)
    {
        var ten = nguoiNhan.Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, boQua, StringComparison.OrdinalIgnoreCase))
            .Select(x => x!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var ds = await db.Users.Where(u => ten.Contains(u.UserName!) && u.DangHoatDong).ToListAsync();
        var now = DateTime.Now;
        var email = thamSo.BatTat(TS.EmailBat);
        var tele = thamSo.BatTat(TS.TelegramBat);
        var url = link is null ? null : (DiaChiGoc.Length > 0 ? DiaChiGoc + link : link);
        foreach (var u in ds)
        {
            db.ThongBaos.Add(new ThongBao { NguoiNhan = u.UserName!, Loai = loai, MucDo = mucDo, TieuDe = tieuDe, NoiDung = noiDung, Link = link, ThoiGian = now });
            if (email && u.NhanEmail && !string.IsNullOrWhiteSpace(u.Email))
                db.TinNhanChos.Add(TaoTin(KenhGui.Email, u.Email!, tieuDe, NoiDungEmail(tieuDe, noiDung, url, u.HoTen), now));
            if (tele && u.NhanTelegram && !string.IsNullOrWhiteSpace(u.TelegramChatId))
                db.TinNhanChos.Add(TaoTin(KenhGui.Telegram, u.TelegramChatId!, tieuDe, NoiDungTelegram(tieuDe, noiDung, url, mucDo), now));
        }
        var nhom = thamSo.Chuoi(TS.TelegramNhomCNTT);
        if (guiNhomCNTT && tele && nhom.Length > 0)
            db.TinNhanChos.Add(TaoTin(KenhGui.Telegram, nhom, tieuDe, NoiDungTelegram(tieuDe, noiDung, url, mucDo), now));
    }

    public async Task GuiVaiTroAsync(string vaiTro, LoaiThongBao loai, string tieuDe, string? noiDung, string? link,
        MucDoThongBao mucDo = MucDoThongBao.ThongTin, bool guiNhomCNTT = false, string? boQua = null) =>
        await GuiAsync((await users.GetUsersInRoleAsync(vaiTro)).Select(u => u.UserName), loai, tieuDe, noiDung, link, mucDo, guiNhomCNTT, boQua);

    public async Task<List<string>> CanBoCNTTAsync() =>
        (await users.GetUsersInRoleAsync(VaiTro.CanBoCNTT)).Concat(await users.GetUsersInRoleAsync(VaiTro.QuanTri))
            .Where(u => u.DangHoatDong).Select(u => u.UserName!).Distinct().ToList();

    public async Task<List<string>> TruongKhoaAsync(int? khoaId) => khoaId is null ? []
        : (await users.GetUsersInRoleAsync(VaiTro.TruongKhoa)).Where(u => u.KhoaPhongId == khoaId && u.DangHoatDong).Select(u => u.UserName!).ToList();

    private static TinNhanCho TaoTin(KenhGui k, string dc, string td, string nd, DateTime now) =>
        new() { Kenh = k, DiaChi = dc, TieuDe = td.Length > 300 ? td[..300] : td, NoiDung = nd.Length > 4000 ? nd[..4000] : nd, ThoiGianTao = now, GuiTu = now };

    private static string NoiDungEmail(string td, string? nd, string? url, string ten) =>
        $"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px\"><p>Chào {E(ten)},</p><p><b>{E(td)}</b></p>"
        + (nd is null ? "" : $"<p style=\"white-space:pre-line\">{E(nd)}</p>")
        + (url is null ? "" : $"<p><a href=\"{E(url)}\">Mở trong hệ thống Quản lý TTB CNTT</a></p>")
        + "<p style=\"color:#777;font-size:12px\">Email tự động từ hệ thống Quản lý Trang thiết bị CNTT — vui lòng không trả lời.</p></div>";

    private static string NoiDungTelegram(string td, string? nd, string? url, MucDoThongBao m) =>
        (m == MucDoThongBao.KhanCap ? "🔴 " : m == MucDoThongBao.CanhBao ? "🟠 " : "🔵 ") + $"<b>{E(td)}</b>"
        + (nd is null ? "" : "\n" + E(nd)) + (url is null ? "" : $"\n<a href=\"{E(url)}\">Mở chi tiết</a>");

    /// <summary>Chỉ thoát ký tự đặc biệt HTML (giữ nguyên chữ tiếng Việt có dấu).</summary>
    private static string E(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    // ===== Các sự kiện nghiệp vụ =====
    public async Task PhieuMoiAsync(PhieuSuaChua p, string? khoa, string? thietBi) =>
        await GuiAsync(await CanBoCNTTAsync(), LoaiThongBao.PhieuMoi, $"[{p.UuTien.TenHienThi()}] {p.SoPhieu}: {MotDong(p.MoTaSuCo)}",
            $"{thietBi} · {khoa}\nNgười báo: {p.NguoiYeuCau}\nHạn xử lý: {p.HanXuLy:HH:mm dd/MM}", $"/PhieuSuaChua/Details/{p.Id}",
            p.UuTien >= MucDoUuTien.Cao ? MucDoThongBao.CanhBao : MucDoThongBao.ThongTin, guiNhomCNTT: true, boQua: p.NguoiTao);

    public Task GiaoViecAsync(PhieuSuaChua p, string? nguoiGiao) =>
        GuiAsync([p.KyThuatVien], LoaiThongBao.GiaoViec, $"Bạn được giao {p.SoPhieu}: {MotDong(p.MoTaSuCo)}",
            $"Ưu tiên {p.UuTien.TenHienThi()} · hạn {p.HanXuLy:HH:mm dd/MM}", $"/PhieuSuaChua/Details/{p.Id}", boQua: nguoiGiao);

    public async Task ChoNghiemThuAsync(PhieuSuaChua p) =>
        await GuiAsync(new[] { p.NguoiTao }.Concat(await TruongKhoaAsync(p.KhoaPhongId)), LoaiThongBao.ChoNghiemThu,
            $"{p.SoPhieu} đã xử lý xong — mời nghiệm thu", p.BienPhapKhacPhuc, $"/PhieuSuaChua/Details/{p.Id}");

    public Task TraLaiAsync(PhieuSuaChua p, string? nguoi) =>
        GuiAsync([p.KyThuatVien], LoaiThongBao.TraLai, $"{p.SoPhieu} bị trả lại — chưa đạt", p.YKienNguoiDung, $"/PhieuSuaChua/Details/{p.Id}", MucDoThongBao.CanhBao, boQua: nguoi);

    public Task BinhLuanAsync(PhieuSuaChua p, string nguoiViet, string noiDung) =>
        GuiAsync([p.NguoiTao, p.KyThuatVien], LoaiThongBao.BinhLuan, $"{nguoiViet} trao đổi trong {p.SoPhieu}", noiDung, $"/PhieuSuaChua/Details/{p.Id}#trao-doi", boQua: nguoiViet);

    public async Task VatTuDuoiDinhMucAsync(VatTu v) =>
        await GuiAsync(await CanBoCNTTAsync(), LoaiThongBao.VatTu, $"Vật tư {v.Ma} dưới định mức (còn {v.TonKho:0.##} {v.DonViTinh})",
            $"{v.Ten} — tồn tối thiểu {v.TonToiThieu:0.##}. Cần lập đề xuất mua sắm.", "/Kho?duoiDinhMuc=true", MucDoThongBao.CanhBao);

    private static string MotDong(string s) { var x = s.Split('\n')[0]; return x.Length > 80 ? x[..80] + "…" : x; }
}

public static class EnumTen
{
    public static string TenHienThi(this Enum e) => ExcelTaiSanService.Ten(e);
}

// ===================== Gửi ra ngoài =====================

public interface IKenhGui
{
    KenhGui Kenh { get; }
    Task GuiAsync(TinNhanCho tin, CancellationToken ct);
}

public class EmailSender(ThamSoService thamSo, IConfiguration config) : IKenhGui
{
    public KenhGui Kenh => KenhGui.Email;
    public async Task GuiAsync(TinNhanCho tin, CancellationToken ct)
    {
        var host = thamSo.Chuoi(TS.SmtpHost);
        var tu = thamSo.Chuoi(TS.EmailGui);
        if (host.Length == 0 || tu.Length == 0) throw new InvalidOperationException("Chưa cấu hình máy chủ SMTP / email gửi đi (Tham số → Thông báo).");
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress("Quản lý TTB CNTT", tu));
        msg.To.Add(MailboxAddress.Parse(tin.DiaChi));
        msg.Subject = tin.TieuDe;
        msg.Body = new TextPart("html") { Text = tin.NoiDung };
        using var smtp = new SmtpClient { Timeout = 20000 };
        var port = thamSo.SoNguyen(TS.SmtpPort);
        await smtp.ConnectAsync(host, port, thamSo.BatTat(TS.SmtpBaoMat)
            ? (port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls) : SecureSocketOptions.None, ct);
        var user = thamSo.Chuoi(TS.SmtpUser);
        if (user.Length > 0) await smtp.AuthenticateAsync(user, config["ThongBao:SmtpPassword"] ?? "", ct);
        await smtp.SendAsync(msg, ct);
        await smtp.DisconnectAsync(true, ct);
    }
}

public class TelegramSender(IHttpClientFactory http, IConfiguration config) : IKenhGui
{
    public KenhGui Kenh => KenhGui.Telegram;
    public async Task GuiAsync(TinNhanCho tin, CancellationToken ct)
    {
        var token = config["ThongBao:TelegramBotToken"];
        if (string.IsNullOrWhiteSpace(token)) throw new InvalidOperationException("Chưa cấu hình ThongBao:TelegramBotToken trong appsettings.");
        var goc = (config["ThongBao:TelegramApiBase"] ?? "https://api.telegram.org").TrimEnd('/');
        var res = await http.CreateClient("telegram").PostAsJsonAsync($"{goc}/bot{token}/sendMessage",
            new { chat_id = tin.DiaChi, text = tin.NoiDung, parse_mode = "HTML", disable_web_page_preview = true }, ct);
        if (!res.IsSuccessStatusCode) throw new InvalidOperationException($"Telegram trả về {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync(ct)}");
    }
}

/// <summary>Xử lý hàng đợi gửi: lấy tin đến hạn, gửi qua kênh tương ứng, ghi kết quả / lịch thử lại.</summary>
public class HangDoiGuiService(AppDbContext db, IEnumerable<IKenhGui> kenhs, ILogger<HangDoiGuiService> log)
{
    public async Task<int> XuLyAsync(CancellationToken ct = default)
    {
        var now = DateTime.Now;
        var ds = await db.TinNhanChos.Where(t => t.TrangThai == TrangThaiGui.ChoGui && t.GuiTu <= now).OrderBy(t => t.Id).Take(50).ToListAsync(ct);
        var map = kenhs.ToDictionary(k => k.Kenh);
        foreach (var t in ds)
        {
            try { await map[t.Kenh].GuiAsync(t, ct); t.DanhDauDaGui(DateTime.Now); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                t.DanhDauLoi(ex.Message, DateTime.Now);
                log.LogWarning("Gửi {Kenh} tới {DiaChi} lỗi (lần {N}): {Loi}", t.Kenh, t.DiaChi, t.SoLanThu, ex.Message);
            }
        }
        if (ds.Count > 0) await db.SaveChangesAsync(ct);
        return ds.Count;
    }
}

/// <summary>Cảnh báo định kỳ: nhắc sắp đến hạn SLA, leo thang 3 cấp khi quá hạn, bản tin tổng hợp hằng ngày.</summary>
public class CanhBaoService(AppDbContext db, ThongBaoService tb, ThamSoService thamSo, UserManager<AppUser> users, IServiceProvider sp)
{
    private static readonly TrangThaiPhieuSuaChua[] DangMo =
        [TrangThaiPhieuSuaChua.Moi, TrangThaiPhieuSuaChua.DaTiepNhan, TrangThaiPhieuSuaChua.DangXuLy, TrangThaiPhieuSuaChua.ChoLinhKien, TrangThaiPhieuSuaChua.GuiHang];

    public async Task ChayAsync(DateTime now, CancellationToken ct = default)
    {
        await NhacTruocHanAsync(now, ct);
        await LeoThangAsync(now, ct);
        await TongHopNgayAsync(now, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task NhacTruocHanAsync(DateTime now, CancellationToken ct)
    {
        var phut = thamSo.SoNguyen(TS.SlaNhacTruocPhut);
        if (phut <= 0) return;
        var moc = now.AddMinutes(phut);
        var ds = await db.PhieuSuaChuas.Where(p => DangMo.Contains(p.TrangThai) && p.BatDauTamDung == null && !p.DaNhacTruocHan
            && p.HanXuLy > now && p.HanXuLy <= moc).ToListAsync(ct);
        foreach (var p in ds)
        {
            p.DaNhacTruocHan = true;
            var nhan = p.KyThuatVien is null ? await tb.CanBoCNTTAsync() : [p.KyThuatVien];
            await tb.GuiAsync(nhan, LoaiThongBao.SapDenHan, $"{p.SoPhieu} sắp đến hạn SLA ({p.HanXuLy:HH:mm})",
                p.MoTaSuCo, $"/PhieuSuaChua/Details/{p.Id}", MucDoThongBao.CanhBao);
        }
    }

    private async Task LeoThangAsync(DateTime now, CancellationToken ct)
    {
        var cap2 = thamSo.SoNguyen(TS.LeoThangCap2SauPhut);
        var cap3 = thamSo.SoNguyen(TS.LeoThangCap3SauPhut);
        var ds = await db.PhieuSuaChuas.Include(p => p.KhoaPhong).Include(p => p.TaiSan)
            .Where(p => DangMo.Contains(p.TrangThai) && p.BatDauTamDung == null && p.HanXuLy < now && p.CapLeoThang < 3).ToListAsync(ct);
        foreach (var p in ds)
        {
            var tre = now - p.HanXuLy;
            var trongYeu = p.UuTien == MucDoUuTien.KhanCap || p.KhoaPhong?.LaKhoaTrongYeu == true;
            int capMoi = cap3 > 0 && trongYeu && tre.TotalMinutes >= cap3 ? 3 : cap2 > 0 && tre.TotalMinutes >= cap2 ? 2 : 1;
            if (capMoi <= p.CapLeoThang) continue;
            var nhan = new List<string?>();
            if (p.CapLeoThang < 1) { nhan.Add(p.KyThuatVien); nhan.AddRange(await tb.CanBoCNTTAsync()); }
            if (capMoi >= 2 && p.CapLeoThang < 2) nhan.AddRange(thamSo.Chuoi(TS.LeoThangCap2NguoiNhan).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            if (capMoi >= 3) nhan.AddRange((await users.GetUsersInRoleAsync(VaiTro.LanhDao)).Select(u => u.UserName));
            p.CapLeoThang = capMoi;
            var tg = tre.TotalHours >= 1 ? $"{(int)tre.TotalHours} giờ {tre.Minutes} phút" : $"{(int)tre.TotalMinutes} phút";
            await tb.GuiAsync(nhan, LoaiThongBao.LeoThang, $"[Leo thang cấp {capMoi}] {p.SoPhieu} quá hạn SLA {tg}",
                $"{p.TaiSan?.MaTaiSan} · {p.KhoaPhong?.Ten} · {p.UuTien.TenHienThi()}\n{p.MoTaSuCo}\nKTV: {p.KyThuatVien ?? "chưa giao"} · Trạng thái: {p.TrangThai.TenHienThi()}",
                $"/PhieuSuaChua/Details/{p.Id}", capMoi >= 2 ? MucDoThongBao.KhanCap : MucDoThongBao.CanhBao, guiNhomCNTT: true);
        }
    }

    /// <summary>Bản tin hằng ngày cho CNTT — chạy một lần/ngày sau giờ cấu hình.</summary>
    private async Task TongHopNgayAsync(DateTime now, CancellationToken ct)
    {
        if (TimeOnly.FromDateTime(now) < thamSo.Gio(TS.GioTongHop)) return;
        var homNay = now.Date;
        if (await db.ThongBaos.AnyAsync(t => t.Loai == LoaiThongBao.TongHopNgay && t.ThoiGian >= homNay, ct)) return;
        var d = DateOnly.FromDateTime(now);
        var dong = new List<string>();
        var quaHan = await db.PhieuSuaChuas.CountAsync(p => DangMo.Contains(p.TrangThai) && p.BatDauTamDung == null && p.HanXuLy < now, ct);
        var moi = await db.PhieuSuaChuas.CountAsync(p => p.TrangThai == TrangThaiPhieuSuaChua.Moi, ct);
        var choNt = await db.PhieuSuaChuas.CountAsync(p => p.TrangThai == TrangThaiPhieuSuaChua.HoanThanh, ct);
        var bh = await db.TaiSans.CountAsync(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.HanBaoHanh >= d && t.HanBaoHanh <= d.AddDays(thamSo.SoNguyen(TS.CanhBaoBaoHanhNgay)), ct);
        var vt = await db.VatTus.Where(v => v.TonKho <= v.TonToiThieu).Select(v => v.Ma).ToListAsync(ct);
        var pm = await db.KeHoachBaoTris.CountAsync(k => k.DangApDung && k.NgayTiepTheo <= d.AddDays(thamSo.SoNguyen(TS.BaoTriCanhBaoNgay)), ct);
        if (quaHan > 0) dong.Add($"• {quaHan} phiếu đang quá hạn SLA");
        if (moi > 0) dong.Add($"• {moi} yêu cầu mới chưa tiếp nhận");
        if (choNt > 0) dong.Add($"• {choNt} phiếu chờ khoa nghiệm thu");
        if (bh > 0) dong.Add($"• {bh} thiết bị sắp hết bảo hành");
        if (vt.Count > 0) dong.Add($"• Vật tư dưới định mức: {string.Join(", ", vt)}");
        if (pm > 0) dong.Add($"• {pm} kế hoạch bảo trì đến hạn");
        foreach (var x in TongHopBoSung) dong.AddRange(await x(sp, now, ct));
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.TongHopNgay, $"Tổng hợp ngày {now:dd/MM/yyyy}",
            dong.Count == 0 ? "Không có việc tồn đọng. 👍" : string.Join("\n", dong), "/", dong.Count == 0 ? MucDoThongBao.ThongTin : MucDoThongBao.CanhBao);
    }

    /// <summary>Phân hệ khác (hợp đồng, bản quyền, kiểm tra…) bổ sung dòng vào bản tin hằng ngày.</summary>
    public static readonly List<Func<IServiceProvider, DateTime, CancellationToken, Task<IEnumerable<string>>>> TongHopBoSung = [];
}
