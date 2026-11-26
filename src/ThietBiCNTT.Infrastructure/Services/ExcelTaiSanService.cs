using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Một dòng đọc được từ file Excel nhập tài sản.</summary>
public class DongNhapTaiSan
{
    public int SoDong { get; set; }
    public TaiSan TaiSan { get; set; } = new();
    public CauHinh? CauHinh { get; set; }
    public List<string> Loi { get; } = [];
    public List<string> CanhBao { get; } = [];
    /// <summary>Mã đã tồn tại trong CSDL → cập nhật (nếu người dùng chọn) thay vì thêm mới.</summary>
    public int? IdDaCo { get; set; }
    /// <summary>Giá trị trường tuỳ chỉnh đã chuẩn hoá (Id trường → giá trị).</summary>
    public Dictionary<int, string?> TuyChinh { get; } = [];
    public bool HopLe => Loi.Count == 0;
}

public record KetQuaNhap(int ThemMoi, int CapNhat, int BoQua);

/// <summary>Nhập / xuất danh sách tài sản bằng Excel (.xlsx).</summary>
public class ExcelTaiSanService(AppDbContext db, LichSuService lichSu, MaTaiSanService maTs, TruongTuyChinhService tuyChinh)
{
    // Thứ tự cột của file mẫu. (*) = bắt buộc.
    private static readonly (string Ten, string GhiChu)[] Cot =
    [
        ("Mã tài sản (*)", "Duy nhất, VD PC-0123. Để trống = hệ thống tự sinh theo mẫu mã (nếu bật tham số)"),
        ("Tên tài sản (*)", ""),
        ("Mã loại thiết bị (*)", "Theo sheet DanhMuc, VD PC, LT, PRN"),
        ("Hãng", ""), ("Model", ""), ("Số serial", ""),
        ("Mã khoa/phòng", "Theo sheet DanhMuc; để trống = Kho CNTT"),
        ("Người sử dụng", ""),
        ("Nguồn vốn", "Ngân sách nhà nước / Quỹ phát triển sự nghiệp / Tài trợ / Viện trợ / Xã hội hoá / Khác"),
        ("Nguyên giá (VNĐ)", "Số, không dấu chấm phân cách"),
        ("Ngày mua", "dd/MM/yyyy"), ("Ngày đưa vào sử dụng", "dd/MM/yyyy"), ("Hạn bảo hành", "dd/MM/yyyy"),
        ("Nhà cung cấp", "Tên đúng như danh mục (không bắt buộc)"),
        ("Số hợp đồng / hoá đơn", ""),
        ("Trạng thái", "Trong kho / Đang sử dụng / Đang sửa chữa / Hỏng (để trống = Đang sử dụng)"),
        ("CPU", ""), ("RAM", ""), ("Ổ cứng", ""), ("Hệ điều hành", ""), ("Bản quyền", ""),
        ("Địa chỉ IP", ""), ("Địa chỉ MAC", ""), ("Hostname", ""), ("Phần mềm", ""), ("Ghi chú", ""),
    ];

    private static readonly string[] DinhDangNgay = ["dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "yyyy-MM-dd", "d/M/yy"];

    // ---------- File mẫu ----------
    public async Task<byte[]> TaoFileMauAsync()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("TaiSan");
        for (var i = 0; i < Cot.Length; i++)
        {
            var c = ws.Cell(1, i + 1);
            c.Value = Cot[i].Ten;
            c.Style.Font.Bold = true;
            c.Style.Fill.BackgroundColor = Cot[i].Ten.Contains("(*)") ? XLColor.FromHtml("#FDE2E1") : XLColor.FromHtml("#DCE8F7");
            if (!string.IsNullOrEmpty(Cot[i].GhiChu)) c.CreateComment().AddText(Cot[i].GhiChu);
        }
        // Trường tuỳ chỉnh: thêm cột sau các cột chuẩn, tiêu đề "Tên [Mã]"
        var tt = await tuyChinh.DangDungAsync();
        for (var i = 0; i < tt.Count; i++)
        {
            var t = tt[i]; var c = ws.Cell(1, Cot.Length + 1 + i);
            c.Value = $"{t.Ten}{(t.BatBuoc ? " (*)" : "")} [{t.Ma}]";
            c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8F5E9");
            c.CreateComment().AddText($"Trường bổ sung · {ExcelTaiSanService.Ten(t.Kieu)}" + (t.LoaiThietBi is null ? "" : $" · chỉ loại {t.LoaiThietBi.Ma}")
                + (t.Kieu == KieuTruong.DanhSach ? $" · chọn: {string.Join(" / ", t.DsLuaChon)}" : t.Kieu == KieuTruong.Ngay ? " · dd/MM/yyyy" : t.Kieu == KieuTruong.CoKhong ? " · Có/Không" : ""));
            if (t.Kieu == KieuTruong.DanhSach && t.DsLuaChon.Count > 0)
                ws.Range(2, Cot.Length + 1 + i, 5001, Cot.Length + 1 + i).CreateDataValidation().List("\"" + string.Join(",", t.DsLuaChon) + "\"", true);
        }
        // Dòng ví dụ
        object[] viDu = ["PC-0999", "Máy tính Dell OptiPlex 7010", "PC", "Dell", "OptiPlex 7010", "SN123456", "KB", "BS. Nguyễn Văn A",
            "Ngân sách nhà nước", 15500000, "15/03/2024", "20/03/2024", "15/03/2027", "", "HĐ 12/2024", "Đang sử dụng",
            "Intel Core i5-12500", "16GB DDR4", "SSD 256GB", "Windows 11 Pro", "OEM", "10.10.2.50", "3C:52:82:1A:0F:7E", "KB-PC50", "HIS, Office", "Dòng ví dụ — xoá trước khi nhập"];
        for (var i = 0; i < viDu.Length; i++) ws.Cell(2, i + 1).Value = XLCellValue.FromObject(viDu[i]);
        ws.Row(2).Style.Font.Italic = true;
        ws.Row(2).Style.Font.FontColor = XLColor.Gray;
        ws.SheetView.FreezeRows(1);
        ws.Columns().AdjustToContents(1, 2, 10, 40);

        // Sheet danh mục để tra mã
        var dm = wb.AddWorksheet("DanhMuc");
        dm.Cell(1, 1).Value = "Mã loại"; dm.Cell(1, 2).Value = "Tên loại";
        dm.Cell(1, 4).Value = "Mã khoa"; dm.Cell(1, 5).Value = "Tên khoa/phòng";
        dm.Cell(1, 7).Value = "Nhà cung cấp"; dm.Cell(1, 9).Value = "Trạng thái"; dm.Cell(1, 10).Value = "Nguồn vốn";
        dm.Row(1).Style.Font.Bold = true;
        var r = 2; foreach (var l in await db.LoaiThietBis.OrderBy(x => x.Ma).ToListAsync()) { dm.Cell(r, 1).Value = l.Ma; dm.Cell(r++, 2).Value = l.Ten; }
        var nLoai = r - 1;
        r = 2; foreach (var k in await db.KhoaPhongs.Where(x => x.DangHoatDong).OrderBy(x => x.Ma).ToListAsync()) { dm.Cell(r, 4).Value = k.Ma; dm.Cell(r++, 5).Value = k.Ten; }
        var nKhoa = r - 1;
        r = 2; foreach (var n in await db.NhaCungCaps.OrderBy(x => x.Ten).ToListAsync()) dm.Cell(r++, 7).Value = n.Ten;
        var nNcc = r - 1;
        var tts = new[] { TrangThaiTaiSan.TrongKho, TrangThaiTaiSan.DangSuDung, TrangThaiTaiSan.DangSuaChua, TrangThaiTaiSan.Hong };
        r = 2; foreach (var t in tts) dm.Cell(r++, 9).Value = Ten(t);
        r = 2; foreach (var v in Enum.GetValues<NguonVon>()) dm.Cell(r++, 10).Value = Ten(v);
        dm.Columns().AdjustToContents();

        // Danh sách chọn (data validation) cho các cột mã
        void Chon(int cot, string vung) => ws.Range(2, cot, 2000, cot).CreateDataValidation().List(vung, true);
        if (nLoai >= 2) Chon(3, $"DanhMuc!$A$2:$A${nLoai}");
        if (nKhoa >= 2) Chon(7, $"DanhMuc!$D$2:$D${nKhoa}");
        Chon(9, $"DanhMuc!$J$2:$J${1 + Enum.GetValues<NguonVon>().Length}");
        if (nNcc >= 2) ws.Range(2, 14, 2000, 14).CreateDataValidation().List($"DanhMuc!$G$2:$G${nNcc}", false);
        Chon(16, $"DanhMuc!$I$2:$I${1 + tts.Length}");

        var hd = wb.AddWorksheet("HuongDan");
        string[] dongHd =
        [
            "HƯỚNG DẪN NHẬP TÀI SẢN",
            "1. Nhập dữ liệu ở sheet TaiSan, mỗi dòng một tài sản, bắt đầu từ dòng 2 (xoá dòng ví dụ).",
            "2. Cột tô đỏ (*) bắt buộc. Mã loại, mã khoa chọn từ danh sách (tra ở sheet DanhMuc).",
            "3. Ngày nhập dạng dd/MM/yyyy hoặc định dạng ngày của Excel. Nguyên giá nhập số.",
            "4. Tải file lên: hệ thống kiểm tra từng dòng và hiển thị lỗi trước khi nhập — chưa có gì được ghi vào CSDL cho đến khi bấm Xác nhận.",
            "5. Mã tài sản đã tồn tại: mặc định bỏ qua; chọn 'Cập nhật tài sản đã có' để ghi đè thông tin (không đổi khoa/phòng — khoa chỉ đổi qua phiếu điều chuyển).",
            "6. Tối đa 5.000 dòng mỗi lần.",
        ];
        for (var i = 0; i < dongHd.Length; i++) hd.Cell(i + 1, 1).Value = dongHd[i];
        hd.Cell(1, 1).Style.Font.Bold = true; hd.Column(1).Width = 120;

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ---------- Đọc & kiểm tra ----------
    public async Task<List<DongNhapTaiSan>> DocAsync(Stream file)
    {
        using var wb = new XLWorkbook(file);
        var ws = wb.Worksheets.FirstOrDefault(w => w.Name == "TaiSan") ?? wb.Worksheet(1);
        var loai = await db.LoaiThietBis.ToDictionaryAsync(x => x.Ma.ToUpperInvariant());
        var khoa = await db.KhoaPhongs.ToDictionaryAsync(x => x.Ma.ToUpperInvariant());
        var ncc = (await db.NhaCungCaps.ToListAsync()).GroupBy(x => x.Ten.Trim().ToLowerInvariant()).ToDictionary(g => g.Key, g => g.First());
        var maDaCo = await db.TaiSans.ToDictionaryAsync(x => x.MaTaiSan.ToUpperInvariant(), x => x.Id);
        var serialDaCo = (await db.TaiSans.Where(x => x.SoSerial != null).Select(x => new { x.SoSerial, x.MaTaiSan }).ToListAsync())
            .GroupBy(x => x.SoSerial!.ToUpperInvariant()).ToDictionary(g => g.Key, g => g.First().MaTaiSan);
        var trongFile = new Dictionary<string, int>();
        var ds = new List<DongNhapTaiSan>();
        var tuSinh = maTs.TuSinh;
        // Cột trường tuỳ chỉnh nhận theo "[Mã]" ở tiêu đề (thứ tự cột tuỳ ý)
        var truong = await tuyChinh.DangDungAsync();
        var cotTuyChinh = new Dictionary<int, TruongTuyChinh>();
        var cotCuoi = ws.Row(1).LastCellUsed()?.Address.ColumnNumber ?? Cot.Length;
        for (var c = Cot.Length + 1; c <= cotCuoi; c++)
        {
            var hd = ws.Cell(1, c).GetString();
            var m = System.Text.RegularExpressions.Regex.Match(hd, @"\[([A-Za-z][A-Za-z0-9_]*)\]\s*$");
            var t = truong.FirstOrDefault(x => m.Success ? string.Equals(x.Ma, m.Groups[1].Value, StringComparison.OrdinalIgnoreCase) : string.Equals(x.Ten, hd.Trim(), StringComparison.OrdinalIgnoreCase));
            if (t is not null) cotTuyChinh[c] = t;
        }

        var dongCuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
        if (dongCuoi > 5001) throw new InvalidOperationException("File có quá 5.000 dòng — hãy chia nhỏ.");
        for (var r = 2; r <= dongCuoi; r++)
        {
            var row = ws.Row(r);
            string S(int c) => row.Cell(c).GetFormattedString().Trim();
            if (Enumerable.Range(1, Math.Max(Cot.Length, cotCuoi)).All(c => string.IsNullOrWhiteSpace(S(c)))) continue;
            var d = new DongNhapTaiSan { SoDong = r };
            var t = d.TaiSan;

            t.MaTaiSan = S(1).ToUpperInvariant();
            t.Ten = S(2);
            if (t.MaTaiSan.Length == 0) { if (tuSinh) d.CanhBao.Add("Mã sẽ tự sinh khi nhập"); else d.Loi.Add("Thiếu mã tài sản"); }
            else if (t.MaTaiSan.Length > 30) d.Loi.Add("Mã tài sản quá 30 ký tự");
            if (t.Ten.Length == 0) d.Loi.Add("Thiếu tên tài sản");
            if (t.MaTaiSan.Length > 0)
            {
                if (trongFile.TryGetValue(t.MaTaiSan, out var dongTrung)) d.Loi.Add($"Trùng mã với dòng {dongTrung}");
                else trongFile[t.MaTaiSan] = r;
                if (maDaCo.TryGetValue(t.MaTaiSan, out var id)) d.IdDaCo = id;
            }
            if (loai.TryGetValue(S(3).ToUpperInvariant(), out var l)) t.LoaiThietBiId = l.Id;
            else d.Loi.Add(S(3).Length == 0 ? "Thiếu mã loại thiết bị" : $"Mã loại '{S(3)}' không có trong danh mục");
            t.Hang = Null(S(4)); t.Model = Null(S(5)); t.SoSerial = Null(S(6));
            if (t.SoSerial is { } sn && serialDaCo.TryGetValue(sn.ToUpperInvariant(), out var maCoSerial) && maCoSerial != t.MaTaiSan)
                d.CanhBao.Add($"Serial trùng với tài sản {maCoSerial}");
            if (S(7).Length > 0)
            {
                if (khoa.TryGetValue(S(7).ToUpperInvariant(), out var k)) t.KhoaPhongId = k.Id;
                else d.Loi.Add($"Mã khoa '{S(7)}' không có trong danh mục");
            }
            t.NguoiSuDung = Null(S(8));
            t.NguonVon = S(9).Length == 0 ? NguonVon.NganSach : TimEnum<NguonVon>(S(9)) ?? Ghi(d, $"Nguồn vốn '{S(9)}' không hợp lệ", NguonVon.Khac);
            if (S(10).Length > 0)
            {
                if (DocSo(row.Cell(10)) is { } gia && gia >= 0) t.NguyenGia = gia;
                else d.Loi.Add($"Nguyên giá '{S(10)}' không phải số");
            }
            t.NgayMua = DocNgay(row.Cell(11), d, "Ngày mua");
            t.NgaySuDung = DocNgay(row.Cell(12), d, "Ngày đưa vào sử dụng");
            t.HanBaoHanh = DocNgay(row.Cell(13), d, "Hạn bảo hành");
            if (S(14).Length > 0)
            {
                if (ncc.TryGetValue(S(14).ToLowerInvariant(), out var n)) t.NhaCungCapId = n.Id;
                else d.CanhBao.Add($"Nhà cung cấp '{S(14)}' chưa có trong danh mục — bỏ qua");
            }
            t.SoHopDong = Null(S(15));
            t.TrangThai = S(16).Length == 0 ? (t.KhoaPhongId is null ? TrangThaiTaiSan.TrongKho : TrangThaiTaiSan.DangSuDung)
                : TimEnum<TrangThaiTaiSan>(S(16)) is { } tt && tt is not (TrangThaiTaiSan.ChoThanhLy or TrangThaiTaiSan.DaThanhLy) ? tt
                : Ghi(d, $"Trạng thái '{S(16)}' không hợp lệ", TrangThaiTaiSan.DangSuDung);
            var ch = new CauHinh { CPU = Null(S(17)), RAM = Null(S(18)), OCung = Null(S(19)), HeDieuHanh = Null(S(20)), BanQuyen = Null(S(21)),
                DiaChiIP = Null(S(22)), DiaChiMAC = Null(S(23)), Hostname = Null(S(24)), PhanMem = Null(S(25)) };
            if (ch.DiaChiIP is { } ip && !System.Net.IPAddress.TryParse(ip, out _)) d.Loi.Add($"Địa chỉ IP '{ip}' không hợp lệ");
            if (CauHinh.CacTruong.Any(f => typeof(CauHinh).GetProperty(f)!.GetValue(ch) is string)) d.CauHinh = ch;
            t.GhiChu = Null(S(26));
            if (t.GhiChu?.Contains("Dòng ví dụ") == true) d.Loi.Add("Dòng ví dụ của file mẫu — hãy xoá");
            if (t.NgaySuDung < t.NgayMua) d.CanhBao.Add("Ngày sử dụng trước ngày mua");
            if (t.LoaiThietBiId != 0)
            {
                foreach (var (c, tr) in cotTuyChinh.Where(x => x.Value.ApDung(t.LoaiThietBiId)))
                {
                    var raw = row.Cell(c).DataType == XLDataType.DateTime ? row.Cell(c).GetDateTime().ToString("dd/MM/yyyy") : S(c);
                    try { d.TuyChinh[tr.Id] = tr.ChuanHoa(raw); } catch (ThietBiCNTT.Domain.Common.DomainException ex) { d.Loi.Add(ex.Message); }
                }
                if (d.IdDaCo is null)
                    foreach (var tr in truong.Where(x => x.BatBuoc && x.ApDung(t.LoaiThietBiId) && !cotTuyChinh.ContainsValue(x))) d.Loi.Add($"Thiếu cột bắt buộc '{tr.Ten} [{tr.Ma}]'");
            }
            ds.Add(d);
        }
        return ds;
    }

    // ---------- Ghi vào CSDL ----------
    public async Task<KetQuaNhap> NhapAsync(List<DongNhapTaiSan> dong, bool capNhatDaCo, string? nguoi)
    {
        if (dong.Any(d => !d.HopLe)) throw new InvalidOperationException("Còn dòng lỗi — sửa file và tải lại.");
        int them = 0, sua = 0, bo = 0;
        var truong = await tuyChinh.DangDungAsync();
        var daCap = new Dictionary<string, int>();
        await using var tx = await db.Database.BeginTransactionAsync();
        foreach (var d in dong)
        {
            if (d.IdDaCo is { } id)
            {
                if (!capNhatDaCo) { bo++; continue; }
                var ts = await db.TaiSans.Include(x => x.CauHinh).FirstAsync(x => x.Id == id);
                if (ts.DaThanhLy) { bo++; continue; }
                var khoaCu = ts.KhoaPhongId; var ttCu = ts.TrangThai;
                var moi = d.TaiSan;
                ts.Ten = moi.Ten; ts.LoaiThietBiId = moi.LoaiThietBiId; ts.Hang = moi.Hang; ts.Model = moi.Model; ts.SoSerial = moi.SoSerial;
                ts.NguoiSuDung = moi.NguoiSuDung; ts.NguonVon = moi.NguonVon; ts.NguyenGia = moi.NguyenGia; ts.NgayMua = moi.NgayMua;
                ts.NgaySuDung = moi.NgaySuDung; ts.HanBaoHanh = moi.HanBaoHanh; ts.NhaCungCapId = moi.NhaCungCapId ?? ts.NhaCungCapId;
                ts.SoHopDong = moi.SoHopDong; ts.GhiChu = moi.GhiChu;
                if (ttCu != TrangThaiTaiSan.ChoThanhLy) ts.TrangThai = moi.TrangThai;
                if (khoaCu is null) ts.KhoaPhongId = moi.KhoaPhongId; // chỉ gán khoa cho tài sản đang ở kho
                if (d.CauHinh is not null) lichSu.CapNhatCauHinh(ts, d.CauHinh, nguoi);
                lichSu.Ghi(ts.Id, LoaiSuKien.CapNhat, "Cập nhật từ file Excel", nguoi);
                if (d.TuyChinh.Count > 0) await tuyChinh.GhiAsync(ts.Id, truong, d.TuyChinh, nguoi);
                sua++;
            }
            else
            {
                var ts = d.TaiSan;
                if (ts.MaTaiSan.Length == 0) ts.MaTaiSan = await maTs.TaoAsync(ts.LoaiThietBiId, ts.KhoaPhongId, daCap);
                if (d.CauHinh is not null) { d.CauHinh.NgayCapNhat = DateTime.Now; ts.CauHinh = d.CauHinh; }
                db.TaiSans.Add(ts);
                await db.SaveChangesAsync();
                lichSu.Ghi(ts.Id, LoaiSuKien.TaoMoi, "Nhập từ file Excel", nguoi);
                if (d.TuyChinh.Count > 0) await tuyChinh.GhiAsync(ts.Id, truong, d.TuyChinh, nguoi, ghiLichSu: false);
                them++;
            }
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new KetQuaNhap(them, sua, bo);
    }

    // ---------- Xuất ----------
    public static byte[] XuatTaiSan(IEnumerable<TaiSan> ds, string tieuDe, IReadOnlyList<TruongTuyChinh>? truong = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("TaiSan");
        ws.Cell(1, 1).Value = tieuDe; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = "Xuất lúc " + DateTime.Now.ToString("HH:mm dd/MM/yyyy");
        string[] hdr = ["Mã TS", "Tên", "Loại", "Hãng", "Model", "Serial", "Khoa/phòng", "Người SD", "Trạng thái", "Nguồn vốn",
            "Nguyên giá", "Ngày mua", "Ngày SD", "Hạn BH", "Nhà cung cấp", "Số HĐ", "CPU", "RAM", "Ổ cứng", "HĐH", "IP", "MAC", "Hostname",
            .. (truong ?? []).Select(t => $"{t.Ten} [{t.Ma}]")];
        for (var i = 0; i < hdr.Length; i++) ws.Cell(4, i + 1).Value = hdr[i];
        var r = 5;
        foreach (var t in ds)
        {
            object?[] v = [t.MaTaiSan, t.Ten, t.LoaiThietBi?.Ten, t.Hang, t.Model, t.SoSerial, t.KhoaPhong?.Ten ?? "Kho CNTT", t.NguoiSuDung,
                Ten(t.TrangThai), Ten(t.NguonVon), t.NguyenGia, t.NgayMua?.ToDateTime(TimeOnly.MinValue), t.NgaySuDung?.ToDateTime(TimeOnly.MinValue),
                t.HanBaoHanh?.ToDateTime(TimeOnly.MinValue), t.NhaCungCap?.Ten, t.SoHopDong,
                t.CauHinh?.CPU, t.CauHinh?.RAM, t.CauHinh?.OCung, t.CauHinh?.HeDieuHanh, t.CauHinh?.DiaChiIP, t.CauHinh?.DiaChiMAC, t.CauHinh?.Hostname,
                .. (truong ?? []).Select(tr => tr.GiaTriCoKieu(t.GiaTriTuyChinhs.FirstOrDefault(g => g.TruongTuyChinhId == tr.Id)?.GiaTri) is DateOnly dd ? dd.ToDateTime(TimeOnly.MinValue) : tr.GiaTriCoKieu(t.GiaTriTuyChinhs.FirstOrDefault(g => g.TruongTuyChinhId == tr.Id)?.GiaTri))];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = XLCellValue.FromObject(v[i]);
            r++;
        }
        DinhDangBang(ws, 4, r - 1, hdr.Length);
        ws.Column(11).Style.NumberFormat.Format = "#,##0";
        foreach (var c in new[] { 12, 13, 14 }) ws.Column(c).Style.DateFormat.Format = "dd/MM/yyyy";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    /// <summary>Định dạng bảng dữ liệu: tiêu đề in đậm, kẻ khung, lọc, cố định dòng tiêu đề, độ rộng cột.</summary>
    public static void DinhDangBang(IXLWorksheet ws, int dongTieuDe, int dongCuoi, int soCot)
    {
        var hdr = ws.Range(dongTieuDe, 1, dongTieuDe, soCot);
        hdr.Style.Font.Bold = true; hdr.Style.Fill.BackgroundColor = XLColor.FromHtml("#DCE8F7");
        var bang = ws.Range(dongTieuDe, 1, Math.Max(dongTieuDe, dongCuoi), soCot);
        bang.Style.Border.OutsideBorder = XLBorderStyleValues.Thin; bang.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        bang.SetAutoFilter();
        ws.SheetView.FreezeRows(dongTieuDe);
        ws.Columns(1, soCot).AdjustToContents(dongTieuDe, Math.Max(dongTieuDe, dongCuoi), 8, 45);
    }

    // ---------- tiện ích ----------
    private static string? Null(string s) => s.Length == 0 ? null : s;
    private static T Ghi<T>(DongNhapTaiSan d, string loi, T macDinh) { d.Loi.Add(loi); return macDinh; }

    public static string Ten(Enum e) =>
        e.GetType().GetMember(e.ToString()).FirstOrDefault()?
            .GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false)
            .OfType<System.ComponentModel.DataAnnotations.DisplayAttribute>().FirstOrDefault()?.GetName() ?? e.ToString();

    private static T? TimEnum<T>(string s) where T : struct, Enum
    {
        var k = BoDau(s);
        foreach (var v in Enum.GetValues<T>())
            if (BoDau(Ten(v)) == k || BoDau(v.ToString()) == k) return v;
        return null;
    }

    private static string BoDau(string s)
    {
        var n = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(System.Text.NormalizationForm.FormD);
        return new string(n.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(c) && c != '/').ToArray());
    }

    private static decimal? DocSo(IXLCell c)
    {
        if (c.DataType == XLDataType.Number) return (decimal)c.GetDouble();
        var s = c.GetString().Trim().Replace(" ", "").Replace("đ", "");
        if (s.Count(ch => ch == '.') > 1 || (s.Contains('.') && s.Contains(','))) s = s.Replace(".", "").Replace(",", ".");
        else if (s.Contains(',') && s.Split(',')[1].Length == 3) s = s.Replace(",", "");
        return decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : null;
    }

    private static DateOnly? DocNgay(IXLCell c, DongNhapTaiSan d, string ten)
    {
        if (c.IsEmpty()) return null;
        if (c.DataType == XLDataType.DateTime) return DateOnly.FromDateTime(c.GetDateTime());
        var s = c.GetFormattedString().Trim();
        if (DateTime.TryParseExact(s, DinhDangNgay, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)) return DateOnly.FromDateTime(dt);
        if (c.DataType == XLDataType.Number && c.GetDouble() is > 20000 and < 80000) return DateOnly.FromDateTime(DateTime.FromOADate(c.GetDouble()));
        d.Loi.Add($"{ten} '{s}' không đúng dạng dd/MM/yyyy");
        return null;
    }
}
