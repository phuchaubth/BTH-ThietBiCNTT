using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Tự gửi báo cáo định kỳ qua email (FR-BC-13): dựng xlsx, lưu vào thư mục đính kèm, xếp hàng email kèm tệp.</summary>
public class BaoCaoDinhKyService(AppDbContext db, ThamSoService ts, BaoCaoService kpi, NganSachService ns, TcoService tco,
    BaoCaoTuyBienService tuyBien, TepService tep, ThongBaoService tb)
{
    /// <summary>Gửi các lịch đến hạn (gọi mỗi 5 phút; mỗi kỳ chỉ gửi một lần, sau giờ bản tin tổng hợp).</summary>
    public async Task<int> ChayAsync(DateTime now, CancellationToken ct = default)
    {
        if (TimeOnly.FromDateTime(now) < ts.Gio(TS.GioTongHop)) return 0;
        var n = 0;
        foreach (var l in await db.LichGuiBaoCaos.Include(x => x.BaoCaoMau).Where(x => x.DangBat).ToListAsync(ct))
        {
            if (LichGuiBaoCao.KyDenHan(l.ChuKy, now) is not { } ky || l.KyDaGui == ky.Ky) continue;
            await GuiAsync(l, ky, ct); n++;
        }
        return n;
    }

    public async Task GuiAsync(LichGuiBaoCao l, (string Ky, DateTime Tu, DateTime Den) ky, CancellationToken ct = default)
    {
        var now = DateTime.Now;
        try
        {
            var (byteArr, tenTep, tomTat) = await DungAsync(l, ky.Tu, ky.Den);
            var thuMuc = Path.Combine(tep.ThuMucGoc, "baocao", now.ToString("yyyyMM"));
            Directory.CreateDirectory(thuMuc);
            var duongDan = Path.Combine(thuMuc, $"{Guid.NewGuid():N}.xlsx");
            await File.WriteAllBytesAsync(duongDan, byteArr, ct);

            // Người nhận: tên đăng nhập → email tài khoản (+ thông báo trong ứng dụng); hoặc địa chỉ email trực tiếp
            var dsTen = l.DsNguoiNhan.Where(x => !x.Contains('@')).ToList();
            var users = await db.Users.Where(u => dsTen.Contains(u.UserName!) && u.DangHoatDong).ToListAsync(ct);
            var emails = l.DsNguoiNhan.Where(x => x.Contains('@')).Concat(users.Where(u => !string.IsNullOrWhiteSpace(u.Email)).Select(u => u.Email!))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var tieuDe = $"[Báo cáo định kỳ] {l.Ten} – kỳ {ky.Ky}";
            var noiDung = $"<div style=\"font-family:Segoe UI,Arial,sans-serif;font-size:14px\"><p>Kính gửi,</p><p>Hệ thống Quản lý TTB CNTT gửi <b>{System.Net.WebUtility.HtmlEncode(l.Ten)}</b> "
                + $"(số liệu {ky.Tu:dd/MM/yyyy} – {ky.Den.AddDays(-1):dd/MM/yyyy}).</p><p style=\"white-space:pre-line\">{System.Net.WebUtility.HtmlEncode(tomTat)}</p>"
                + "<p>Chi tiết trong tệp Excel đính kèm.</p><p style=\"color:#777;font-size:12px\">Email tự động — vui lòng không trả lời.</p></div>";
            var emailBat = ts.BatTat(TS.EmailBat);
            if (emailBat)
                foreach (var e in emails)
                    db.TinNhanChos.Add(new TinNhanCho { Kenh = KenhGui.Email, DiaChi = e, TieuDe = tieuDe, NoiDung = noiDung, ThoiGianTao = now, GuiTu = now,
                        TepDinhKem = duongDan, TenTepDinhKem = tenTep });
            await tb.GuiAsync(users.Select(u => u.UserName), LoaiThongBao.HeThong, tieuDe, tomTat, $"/LichGuiBaoCao/Tep/{l.Id}", chiTrongUngDung: true); // email đã gửi kèm tệp ở trên
            l.KyDaGui = ky.Ky; l.LanGuiCuoi = now; l.TepCuoi = duongDan;
            l.KetQuaCuoi = emailBat ? $"Đã xếp hàng gửi {emails.Count} email ({tenTep})" : $"Đã lập {tenTep} — email đang tắt (Tham số → Thông báo), chỉ gửi thông báo trong ứng dụng";
        }
        catch (Exception ex) when (ex is DomainException or InvalidOperationException or IOException)
        {
            l.KyDaGui = ky.Ky; l.LanGuiCuoi = now; l.KetQuaCuoi = "Lỗi: " + ex.Message;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<(byte[] Tep, string Ten, string TomTat)> DungAsync(LichGuiBaoCao l, DateTime tu, DateTime den)
    {
        var tenBv = ts.Chuoi(TS.TenBenhVien);
        switch (l.Loai)
        {
            case LoaiBaoCaoDinhKy.KpiVanHanh:
            {
                var r = await kpi.LapAsync(tu, den.AddDays(-1), null);
                return (BaoCaoExcel.Kpi(r), $"KPI_{tu:yyyyMMdd}_{den.AddDays(-1):yyyyMMdd}.xlsx",
                    $"Số phiếu: {r.Tong.SoPhieu} · đã xử lý: {r.Tong.DaXong} · đúng SLA: {(r.Tong.DaXong == 0 ? "—" : $"{r.Tong.TyLeDungHan:0.#}%")}"
                    + $" · MTTR: {(r.Tong.MttrGio is { } m ? $"{m:0.#} giờ" : "—")} · chi phí: {r.Tong.ChiPhi:N0} đ");
            }
            case LoaiBaoCaoDinhKy.NganSach:
            {
                var ds = await ns.SuDungAsync(tu.Year);
                return (BaoCaoExcel.NganSach(ds, tu.Year, tenBv), $"NganSach_{tu.Year}.xlsx",
                    $"Dự toán {ds.Sum(x => x.DuToan):N0} đ · cam kết {ds.Sum(x => x.CamKet):N0} đ · thực chi {ds.Sum(x => x.ThucChi):N0} đ · còn lại {ds.Sum(x => x.ConLai):N0} đ");
            }
            case LoaiBaoCaoDinhKy.Tco:
            {
                var a = await tco.LapAsync(NhomTco.TaiSan); var b = await tco.LapAsync(NhomTco.Loai); var c = await tco.LapAsync(NhomTco.Model);
                return (BaoCaoExcel.Tco(a, b, c, "Toàn viện"), $"TCO_{den:yyyyMMdd}.xlsx",
                    $"{a.Count} tài sản · TCO {a.Sum(x => x.Tong):N0} đ · chi phí vận hành {a.Sum(x => x.VanHanh):N0} đ");
            }
            default:
            {
                var mau = l.BaoCaoMau ?? await db.BaoCaoMaus.FindAsync(l.BaoCaoMauId) ?? throw new DomainException("Mẫu báo cáo đã bị xoá.");
                var kq = await tuyBien.ChayAsync(BaoCaoTuyBienService.Doc(mau.CauHinh), (tu, den));
                return (BaoCaoExcel.TuyBien(kq, mau.Ten), $"{TenTep(mau.Ten)}_{den:yyyyMMdd}.xlsx", $"{mau.Ten}: {kq.TongBanGhi} bản ghi · {kq.MoTaLoc}");
            }
        }
    }

    public static string TenTep(string s) => new string(s.Normalize(System.Text.NormalizationForm.FormD)
        .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray())
        .Replace('đ', 'd').Replace('Đ', 'D').Replace(' ', '_') is var t && t.Length > 40 ? t[..40] : t;
}
