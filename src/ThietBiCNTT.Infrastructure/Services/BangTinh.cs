using ClosedXML.Excel;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Đọc bảng từ tệp .xlsx hoặc .csv (dùng cho nhập lỗ hổng từ công cụ quét).</summary>
public static class BangTinh
{
    public static async Task<List<string?[]>> DocAsync(Stream s, string tenTep)
    {
        var raw = new List<string?[]>();
        if (tenTep.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            using var wb = new XLWorkbook(s); var ws = wb.Worksheets.First();
            var cuoi = ws.LastRowUsed()?.RowNumber() ?? 0; var cot = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
            for (var r = 1; r <= cuoi; r++)
                raw.Add(Enumerable.Range(1, cot).Select(c => { var cell = ws.Cell(r, c); return cell.DataType == XLDataType.DateTime ? cell.GetDateTime().ToString("yyyy-MM-dd") : cell.GetString(); }).Cast<string?>().ToArray());
            return raw;
        }
        using var rd = new StreamReader(s);
        string? line; var first = true; char sep = ',';
        while ((line = await rd.ReadLineAsync()) is not null)
        {
            if (first) { sep = line.Count(c => c == ';') > line.Count(c => c == ',') ? ';' : line.Contains('\t') ? '\t' : ','; first = false; }
            var kq = new List<string?>(); var sb = new System.Text.StringBuilder(); var ngoac = false;
            foreach (var ch in line) { if (ch == '"') ngoac = !ngoac; else if (ch == sep && !ngoac) { kq.Add(sb.ToString()); sb.Clear(); } else sb.Append(ch); }
            kq.Add(sb.ToString()); raw.Add(kq.ToArray());
        }
        return raw;
    }

    /// <summary>Chỉ số cột theo danh sách tên chấp nhận (không phân biệt hoa thường), -1 nếu không có.</summary>
    public static int[] Cot(string?[] tieuDe, params string[][] ten)
    {
        var h = tieuDe.Select(x => (x ?? "").Trim().ToLowerInvariant()).ToList();
        return ten.Select(t => h.FindIndex(x => t.Contains(x))).ToArray();
    }
}
