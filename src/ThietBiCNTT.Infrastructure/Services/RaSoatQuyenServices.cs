using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Một dòng trong tệp danh sách tài khoản (FR-TC-02) kèm kết quả kiểm tra.</summary>
public record DongTaiKhoan(int Dong, string TenDangNhap, string? HoTen, string? Khoa, string? NhomQuyen, DateTime? LanDangNhapCuoi, string? TrangThai, int? KhoaPhongId, List<string> Loi);

/// <summary>GĐ3.3 – rà soát quyền truy cập (mục 4.7).</summary>
public class RaSoatQuyenService(AppDbContext db, SoPhieuService soPhieu, ThongBaoService tb, ThamSoService thamSo)
{
    static readonly string[][] TenCot =
    [
        ["tên đăng nhập", "ten dang nhap", "username", "user", "tài khoản", "tai khoan", "account"],
        ["họ tên", "ho ten", "họ và tên", "name", "fullname", "full name"],
        ["khoa", "khoa/phòng", "khoa phòng", "phòng", "department", "dept"],
        ["nhóm quyền", "nhom quyen", "quyền", "quyen", "vai trò", "vai tro", "role", "group"],
        ["lần đăng nhập cuối", "lan dang nhap cuoi", "đăng nhập cuối", "last login", "lastlogin", "last logon"],
        ["trạng thái", "trang thai", "status"]
    ];

    static string Chuan(string s) => s.Trim().ToLowerInvariant();

    /// <summary>Đọc .xlsx hoặc .csv; kiểm tra từng dòng.</summary>
    public async Task<List<DongTaiKhoan>> DocAsync(Stream s, string tenTep)
    {
        var raw = new List<string?[]>();
        if (tenTep.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var wb = new XLWorkbook(s); var ws = wb.Worksheets.First();
            var cuoi = ws.LastRowUsed()?.RowNumber() ?? 0; var cot = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var r = 1; r <= cuoi; r++)
                raw.Add(Enumerable.Range(1, cot).Select(c => { var cell = ws.Cell(r, c); return cell.DataType == XLDataType.DateTime ? cell.GetDateTime().ToString("yyyy-MM-dd HH:mm") : cell.GetString(); }).Cast<string?>().ToArray());
        }
        else
        {
            using var rd = new StreamReader(s);
            string? line; var first = true; char sep = ',';
            while ((line = await rd.ReadLineAsync()) is not null)
            {
                if (first) { sep = line.Count(c => c == ';') > line.Count(c => c == ',') ? ';' : line.Contains('\t') ? '\t' : ','; first = false; }
                raw.Add(TachCsv(line, sep));
            }
        }
        if (raw.Count < 2) throw new DomainException("Tệp không có dòng dữ liệu.");
        var tieuDe = raw[0].Select(x => Chuan(x ?? "")).ToList();
        var idx = TenCot.Select(ten => tieuDe.FindIndex(h => ten.Contains(h))).ToArray();
        if (idx[0] < 0) throw new DomainException("Không tìm thấy cột \"Tên đăng nhập\" ở dòng đầu tiên.");
        var khoas = await db.KhoaPhongs.AsNoTracking().ToListAsync();
        var kq = new List<DongTaiKhoan>(); var daCo = new HashSet<string>();
        for (var i = 1; i < raw.Count; i++)
        {
            string? Lay(int c) => idx[c] >= 0 && idx[c] < raw[i].Length ? raw[i][idx[c]]?.Trim() : null;
            if (raw[i].All(x => string.IsNullOrWhiteSpace(x))) continue;
            var loi = new List<string>(); var user = Lay(0) ?? "";
            if (user.Length == 0) loi.Add("thiếu tên đăng nhập");
            else if (!daCo.Add(user.ToLowerInvariant())) loi.Add("trùng tên đăng nhập trong tệp");
            DateTime? dn = null; var ds = Lay(4);
            if (!string.IsNullOrWhiteSpace(ds)) { if (DateTime.TryParse(ds, new CultureInfo("vi-VN"), DateTimeStyles.None, out var d1) || DateTime.TryParse(ds, CultureInfo.InvariantCulture, DateTimeStyles.None, out d1)) dn = d1; else loi.Add($"ngày đăng nhập cuối không đọc được ({ds})"); }
            var khoaText = Lay(2); int? kid = null;
            if (!string.IsNullOrWhiteSpace(khoaText)) { var k = khoas.FirstOrDefault(x => Chuan(x.Ma) == Chuan(khoaText) || Chuan(x.Ten) == Chuan(khoaText)); if (k is null) loi.Add($"không khớp khoa \"{khoaText}\""); else kid = k.Id; }
            kq.Add(new DongTaiKhoan(i + 1, user, Lay(1), khoaText, Lay(3), dn, Lay(5), kid, loi));
        }
        return kq;
    }

    static string?[] TachCsv(string line, char sep)
    {
        var kq = new List<string?>(); var sb = new System.Text.StringBuilder(); var trongNgoac = false;
        foreach (var ch in line)
        {
            if (ch == '"') trongNgoac = !trongNgoac;
            else if (ch == sep && !trongNgoac) { kq.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(ch);
        }
        kq.Add(sb.ToString()); return kq.ToArray();
    }

    public async Task<DotRaSoatQuyen> TaoDotAsync(int heThongId, string lyDo, string nguoi)
    {
        var h = await db.HeThongThongTins.FirstOrDefaultAsync(x => x.Id == heThongId) ?? throw new DomainException("Không tìm thấy hệ thống.");
        if (await db.DotRaSoatQuyens.AnyAsync(d => d.HeThongId == heThongId && d.TrangThai != TrangThaiRaSoat.DaDong)) throw new DomainException($"Hệ thống {h.Ma} đã có đợt rà soát chưa đóng.");
        var d = new DotRaSoatQuyen { HeThongId = heThongId, LyDo = lyDo, Han = DateOnly.FromDateTime(DateTime.Today).AddDays(thamSo.SoNguyen(TS.JciRaSoatQuyenHanNgay)), SoNgayKhongDangNhap = thamSo.SoNguyen(TS.JciTaiKhoanKhongDangNhapNgay), NguoiTao = nguoi };
        db.DotRaSoatQuyens.Add(d);
        await db.LuuMoiAsync(soPhieu, "RQ", so => d.Ma = so);
        return d;
    }

    /// <summary>FR-TC-01: sinh đợt rà soát cho hệ thống Tối quan trọng / Cao khi đến chu kỳ.</summary>
    public async Task<int> SinhDotAsync(CancellationToken ct = default)
    {
        var dem = 0; var now = DateTime.Now;
        var tqt = thamSo.SoNguyen(TS.JciRaSoatQuyenToiQuanTrongThang); var cao = thamSo.SoNguyen(TS.JciRaSoatQuyenCaoThang);
        foreach (var h in await db.HeThongThongTins.Where(x => x.TrangThai == TrangThaiHeThong.VanHanh && (x.MucTrongYeu == MucTrongYeu.ToiQuanTrong || x.MucTrongYeu == MucTrongYeu.Cao)).ToListAsync(ct))
        {
            if (await db.DotRaSoatQuyens.AnyAsync(d => d.HeThongId == h.Id && d.TrangThai != TrangThaiRaSoat.DaDong, ct)) continue;
            var chuKy = DotRaSoatQuyen.ChuKyThang(h.MucTrongYeu, tqt, cao); if (chuKy <= 0) continue;
            var cuoi = await db.DotRaSoatQuyens.Where(d => d.HeThongId == h.Id).MaxAsync(d => (DateTime?)d.NgayTao, ct);
            if (cuoi is { } c && c.AddMonths(chuKy) > now) continue;
            var d2 = await TaoDotAsync(h.Id, "Định kỳ theo chu kỳ rà soát", "system");
            var nhan = new List<string?> { h.PhuTrachChinh }; if (nhan[0] is null) nhan = new List<string?>(await tb.CanBoCNTTAsync());
            await tb.GuiAsync(nhan, LoaiThongBao.RaSoatQuyen, $"Đến kỳ rà soát quyền truy cập {h.Ma} ({d2.Ma})", $"Xuất danh sách tài khoản từ hệ thống, nhập vào đợt rà soát. Hạn {d2.Han:dd/MM/yyyy}.", $"/RaSoatQuyen/Details/{d2.Id}");
            dem++;
        }
        return dem;
    }

    /// <summary>FR-TC-06: tạo việc thu hồi trên mọi hệ thống người đó có tài khoản theo đợt rà soát gần nhất.</summary>
    public async Task<ThongBaoNghiViec> GhiNghiViecAsync(ThongBaoNghiViec t, string nguoi)
    {
        if (string.IsNullOrWhiteSpace(t.HoTen)) throw new DomainException("Nhập họ tên.");
        var now = DateTime.Now;
        t.Han = PhieuThayDoi.CongNgayLam(t.NgayHieuLuc.ToDateTime(TimeOnly.MinValue) < now.Date ? now : t.NgayHieuLuc.ToDateTime(new TimeOnly(17, 0)), thamSo.SoNguyen(TS.JciThuHoiNghiViecNgayLam));
        var user = t.TenDangNhap?.Trim().ToLowerInvariant();
        var hoTen = t.HoTen.Trim().ToLowerInvariant();
        var gan = await db.TaiKhoanRaSoats.AsNoTracking().Include(x => x.DotRaSoatQuyen).ThenInclude(d => d!.HeThong)
            .Where(x => (user != null && x.TenDangNhap.ToLower() == user) || (x.HoTen != null && x.HoTen.ToLower() == hoTen)).ToListAsync();
        foreach (var g in gan.GroupBy(x => x.DotRaSoatQuyen!.HeThongId).Select(g => g.OrderByDescending(x => x.DotRaSoatQuyen!.NgayTao).First()))
            t.Viecs.Add(new ViecThuHoiQuyen { HeThongId = g.DotRaSoatQuyen!.HeThongId, NoiDung = $"{(t.Loai == LoaiNghiViec.NghiViec ? "Thu hồi" : "Rà soát / đổi quyền")} tài khoản {g.TenDangNhap} trên {g.DotRaSoatQuyen.HeThong?.Ma}" });
        t.Viecs.Add(new ViecThuHoiQuyen { NoiDung = "Thu hồi thiết bị di động, thẻ, chìa khoá và tài khoản không nằm trong danh sách rà soát (HCT.01.01 ME2)" });
        t.NguoiTao = nguoi; db.ThongBaoNghiViecs.Add(t);
        await db.SaveChangesAsync();
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.RaSoatQuyen, $"{t.Loai.TenHienThi()}: thu hồi quyền của {t.HoTen} trước {t.Han:dd/MM/yyyy}", $"{t.Viecs.Count} việc cần thực hiện.", $"/NghiViec/Details/{t.Id}", MucDoThongBao.CanhBao);
        return t;
    }
}
