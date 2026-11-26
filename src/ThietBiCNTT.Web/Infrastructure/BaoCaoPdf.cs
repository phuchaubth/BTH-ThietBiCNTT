using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Bảng dữ liệu của một báo cáo chuẩn — dùng chung cho xem trên web, PDF và Excel.</summary>
public record BangBaoCao(string Ma, string TieuDe, string PhuDe, string[] Cot, double[] RongCm, List<string?[]> Dong,
    string?[]? Tong = null, bool Ngang = false, int[]? CotSo = null, string? GhiChu = null);

/// <summary>Thông tin in đầu/cuối: cơ quan chủ quản, đơn vị, địa danh, các ô ký.</summary>
public record ThongTinIn(string CoQuanChuQuan, string TenDonVi, string DiaDanh, string[] ChuKy, string NguoiLap);

/// <summary>
/// Phông chữ cho PDFsharp: Times New Roman trên Windows (C:\Windows\Fonts), Liberation Serif / DejaVu Serif trên Linux,
/// hoặc thư mục cấu hình <c>BaoCao:FontDir</c> chứa times.ttf, timesbd.ttf, timesi.ttf, timesbi.ttf.
/// </summary>
public class PhongChuBaoCao : IFontResolver
{
    public const string Ho = "BaoCaoSerif";
    private readonly string[] _tep; // thường, đậm, nghiêng, đậm nghiêng

    private PhongChuBaoCao(string[] tep) => _tep = tep;

    private static readonly object Khoa = new();
    private static bool _daCai;

    public static void CaiDat(string? thuMucCauHinh)
    {
        lock (Khoa)
        {
            if (_daCai) return;
            var win = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            string[][] ung =
            [
                .. thuMucCauHinh is { Length: > 0 } d ? new[] { new[] { "times.ttf", "timesbd.ttf", "timesi.ttf", "timesbi.ttf" }.Select(f => Path.Combine(d, f)).ToArray() } : [],
                new[] { "times.ttf", "timesbd.ttf", "timesi.ttf", "timesbi.ttf" }.Select(f => Path.Combine(win, f)).ToArray(),
                new[] { "Regular", "Bold", "Italic", "BoldItalic" }.Select(f => $"/usr/share/fonts/truetype/liberation/LiberationSerif-{f}.ttf").ToArray(),
                new[] { "", "-Bold", "-Italic", "-BoldItalic" }.Select(f => $"/usr/share/fonts/truetype/dejavu/DejaVuSerif{f}.ttf").ToArray(),
            ];
            var bo = ung.FirstOrDefault(b => File.Exists(b[0]))
                ?? throw new InvalidOperationException("Không tìm thấy phông chữ để xuất PDF. Cấu hình BaoCao:FontDir trỏ tới thư mục có times.ttf (Times New Roman).");
            for (var i = 1; i < 4; i++) if (!File.Exists(bo[i])) bo[i] = bo[0];
            GlobalFontSettings.FontResolver = new PhongChuBaoCao(bo);
            _daCai = true;
        }
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(Ho + (bold ? "B" : "") + (italic ? "I" : ""));

    public byte[]? GetFont(string faceName) => File.ReadAllBytes(_tep[(faceName.EndsWith("BI") ? 3 : faceName.EndsWith("I") ? 2 : faceName.EndsWith("B") ? 1 : 0)]);
}

public static class BaoCaoChuanXuat
{
    /// <summary>PDF theo thể thức: cơ quan/đơn vị + quốc hiệu, tên báo cáo, kỳ, bảng có lặp tiêu đề, dòng tổng, địa danh ngày, các ô ký, số trang.</summary>
    public static byte[] Pdf(BangBaoCao b, ThongTinIn tt, DateTime now)
    {
        var doc = new Document();
        doc.Info.Title = b.TieuDe; doc.Info.Author = tt.TenDonVi;
        var st = doc.Styles[StyleNames.Normal]!;
        st.Font.Name = PhongChuBaoCao.Ho; st.Font.Size = 11;
        var s = doc.AddSection();
        s.PageSetup = doc.DefaultPageSetup.Clone();
        // Đặt kích thước tường minh (A4 ngang/dọc) — tránh phụ thuộc cách MigraDoc hoán đổi theo Orientation.
        s.PageSetup.Orientation = Orientation.Portrait;
        s.PageSetup.PageWidth = Unit.FromCentimeter(b.Ngang ? 29.7 : 21.0);
        s.PageSetup.PageHeight = Unit.FromCentimeter(b.Ngang ? 21.0 : 29.7);
        s.PageSetup.TopMargin = Unit.FromCentimeter(1.5); s.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
        s.PageSetup.LeftMargin = Unit.FromCentimeter(b.Ngang ? 1.5 : 2); s.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        var rongTrang = (b.Ngang ? 29.7 : 21.0) - (b.Ngang ? 3.0 : 3.5);

        // Đầu trang: cơ quan – quốc hiệu
        var dau = s.AddTable();
        dau.AddColumn(Unit.FromCentimeter(rongTrang * 0.42)); dau.AddColumn(Unit.FromCentimeter(rongTrang * 0.58));
        var r = dau.AddRow();
        var trai = r.Cells[0]; trai.Format.Alignment = ParagraphAlignment.Center;
        trai.AddParagraph(tt.CoQuanChuQuan.ToUpperInvariant()).Format.Font.Size = 11;
        var dv = trai.AddParagraph(tt.TenDonVi.ToUpperInvariant()); dv.Format.Font.Bold = true; dv.Format.Font.Size = 11;
        trai.AddParagraph("———————").Format.Font.Size = 8;
        var phai = r.Cells[1]; phai.Format.Alignment = ParagraphAlignment.Center;
        var qh = phai.AddParagraph("CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM"); qh.Format.Font.Bold = true; qh.Format.Font.Size = 11;
        var tn = phai.AddParagraph("Độc lập - Tự do - Hạnh phúc"); tn.Format.Font.Bold = true; tn.Format.Font.Size = 12;
        phai.AddParagraph("———————————").Format.Font.Size = 8;
        var ngay = phai.AddParagraph($"{tt.DiaDanh}, ngày {now:dd} tháng {now:MM} năm {now:yyyy}"); ngay.Format.Font.Italic = true;

        var td = s.AddParagraph(b.TieuDe.ToUpperInvariant());
        td.Format.Alignment = ParagraphAlignment.Center; td.Format.Font.Bold = true; td.Format.Font.Size = 14; td.Format.SpaceBefore = Unit.FromCentimeter(0.6);
        var pd = s.AddParagraph(b.PhuDe); pd.Format.Alignment = ParagraphAlignment.Center; pd.Format.Font.Italic = true; pd.Format.SpaceAfter = Unit.FromCentimeter(0.4);

        // Bảng dữ liệu — co giãn độ rộng cột vừa khổ giấy
        var tong = b.RongCm.Sum(); var heSo = tong > 0 ? rongTrang / tong : 1;
        var bang = s.AddTable();
        bang.Borders.Width = 0.5; bang.Format.Font.Size = b.Cot.Length > 9 ? 9 : 10;
        bang.LeftPadding = Unit.FromMillimeter(1); bang.RightPadding = Unit.FromMillimeter(1);
        var cotSo = (b.CotSo ?? []).ToHashSet();
        for (var i = 0; i < b.Cot.Length; i++)
        {
            var c = bang.AddColumn(Unit.FromCentimeter(b.RongCm[i] * heSo));
            c.Format.Alignment = cotSo.Contains(i) ? ParagraphAlignment.Right : ParagraphAlignment.Left;
        }
        var h = bang.AddRow(); h.HeadingFormat = true; h.Format.Font.Bold = true; h.Format.Alignment = ParagraphAlignment.Center; h.VerticalAlignment = VerticalAlignment.Center;
        h.Shading.Color = Color.FromRgb(235, 235, 235);
        for (var i = 0; i < b.Cot.Length; i++) h.Cells[i].AddParagraph(b.Cot[i]);
        foreach (var d in b.Dong)
        {
            var row = bang.AddRow();
            for (var i = 0; i < b.Cot.Length && i < d.Length; i++) row.Cells[i].AddParagraph(d[i] ?? "");
        }
        if (b.Dong.Count == 0)
        {
            var row = bang.AddRow(); row.Cells[0].MergeRight = b.Cot.Length - 1;
            var p = row.Cells[0].AddParagraph("Không có dữ liệu"); p.Format.Alignment = ParagraphAlignment.Center; p.Format.Font.Italic = true;
        }
        if (b.Tong is { } t)
        {
            var row = bang.AddRow(); row.Format.Font.Bold = true;
            for (var i = 0; i < b.Cot.Length && i < t.Length; i++) row.Cells[i].AddParagraph(t[i] ?? "");
        }
        if (!string.IsNullOrWhiteSpace(b.GhiChu))
        {
            var g = s.AddParagraph(b.GhiChu); g.Format.Font.Size = 9; g.Format.Font.Italic = true; g.Format.SpaceBefore = Unit.FromMillimeter(2);
        }

        // Chữ ký
        var ky = tt.ChuKy.Where(x => !string.IsNullOrWhiteSpace(x)).Take(4).ToArray();
        if (ky.Length > 0)
        {
            s.AddParagraph().Format.SpaceBefore = Unit.FromCentimeter(0.6);
            var kt = s.AddTable(); kt.KeepTogether = true;
            foreach (var _ in ky) kt.AddColumn(Unit.FromCentimeter(rongTrang / ky.Length));
            var kr = kt.AddRow(); kr.KeepWith = 2;
            for (var i = 0; i < ky.Length; i++) { var p = kr.Cells[i].AddParagraph(ky[i].Trim().ToUpperInvariant()); p.Format.Alignment = ParagraphAlignment.Center; p.Format.Font.Bold = true; }
            var gc = kt.AddRow();
            for (var i = 0; i < ky.Length; i++) { var p = gc.Cells[i].AddParagraph("(Ký, ghi rõ họ tên)"); p.Format.Alignment = ParagraphAlignment.Center; p.Format.Font.Italic = true; p.Format.Font.Size = 9; }
            var tenRow = kt.AddRow(); tenRow.Height = Unit.FromCentimeter(2.6); tenRow.VerticalAlignment = VerticalAlignment.Bottom;
            var tl = tenRow.Cells[0].AddParagraph(tt.NguoiLap); tl.Format.Alignment = ParagraphAlignment.Center; tl.Format.Font.Bold = true;
        }

        var ft = s.Footers.Primary.AddParagraph();
        ft.Format.Alignment = ParagraphAlignment.Right; ft.Format.Font.Size = 8;
        ft.AddText($"{b.Ma} · in lúc {now:dd/MM/yyyy HH:mm} · trang "); ft.AddPageField(); ft.AddText("/"); ft.AddNumPagesField();

        var rd = new PdfDocumentRenderer { Document = doc };
        rd.RenderDocument();
        using var ms = new MemoryStream();
        rd.PdfDocument.Save(ms, false);
        return ms.ToArray();
    }

    public static byte[] Excel(BangBaoCao b, ThongTinIn tt, DateTime now)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(b.Ma.Replace("/", "-"));
        var n = b.Cot.Length;
        ws.Cell(1, 1).Value = tt.CoQuanChuQuan.ToUpperInvariant();
        ws.Cell(2, 1).Value = tt.TenDonVi.ToUpperInvariant(); ws.Cell(2, 1).Style.Font.Bold = true;
        var c = Math.Max(n - 3, 4);
        ws.Cell(1, c).Value = "CỘNG HOÀ XÃ HỘI CHỦ NGHĨA VIỆT NAM"; ws.Cell(1, c).Style.Font.Bold = true;
        ws.Cell(2, c).Value = "Độc lập - Tự do - Hạnh phúc"; ws.Cell(2, c).Style.Font.Bold = true;
        ws.Cell(3, c).Value = $"{tt.DiaDanh}, ngày {now:dd} tháng {now:MM} năm {now:yyyy}"; ws.Cell(3, c).Style.Font.Italic = true;
        ws.Cell(5, 1).Value = b.TieuDe.ToUpperInvariant(); ws.Range(5, 1, 5, n).Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Cell(6, 1).Value = b.PhuDe; ws.Range(6, 1, 6, n).Merge().Style.Font.SetItalic().Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        for (var i = 0; i < n; i++) ws.Cell(8, i + 1).Value = b.Cot[i];
        var cotSo = (b.CotSo ?? []).ToHashSet();
        var dong = 9;
        foreach (var d in b.Dong.Concat(b.Tong is { } t ? [t] : []))
        {
            for (var i = 0; i < n && i < d.Length; i++)
            {
                var v = d[i];
                if (cotSo.Contains(i) && decimal.TryParse(v?.Replace(".", "").Replace(",", "."), System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var so))
                { ws.Cell(dong, i + 1).Value = so; ws.Cell(dong, i + 1).Style.NumberFormat.Format = "#,##0.##"; }
                else ws.Cell(dong, i + 1).Value = v ?? "";
            }
            dong++;
        }
        if (b.Tong is not null) ws.Row(dong - 1).Style.Font.Bold = true;
        global::ThietBiCNTT.Infrastructure.Services.ExcelTaiSanService.DinhDangBang(ws, 8, Math.Max(8, dong - 1), n);
        if (!string.IsNullOrWhiteSpace(b.GhiChu)) ws.Cell(dong + 1, 1).Value = b.GhiChu;
        var ky = tt.ChuKy.Where(x => !string.IsNullOrWhiteSpace(x)).Take(4).ToArray();
        for (var i = 0; i < ky.Length; i++)
        {
            var col = 1 + i * Math.Max(1, n / Math.Max(1, ky.Length));
            ws.Cell(dong + 3, col).Value = ky[i].Trim().ToUpperInvariant(); ws.Cell(dong + 3, col).Style.Font.Bold = true;
        }
        using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
    }
}
