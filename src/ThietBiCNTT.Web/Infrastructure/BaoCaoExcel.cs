using ClosedXML.Excel;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Infrastructure;

/// <summary>Dựng file Excel cho các báo cáo — dùng chung cho tải về và gửi định kỳ.</summary>
public static class BaoCaoExcel
{
    private static byte[] Luu(XLWorkbook wb) { using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray(); }

    private static void Sheet(XLWorkbook wb, string ten, string tieuDe, string phu, string[] cot, IEnumerable<object?[]> dong, Action<IXLWorksheet>? dinhDang = null)
    {
        var ws = wb.AddWorksheet(ten);
        ws.Cell(1, 1).Value = tieuDe; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = phu;
        for (var i = 0; i < cot.Length; i++) ws.Cell(4, i + 1).Value = cot[i];
        var n = 5;
        foreach (var x in dong) { for (var i = 0; i < x.Length; i++) ws.Cell(n, i + 1).Value = XLCellValue.FromObject(x[i] is DateOnly d ? d.ToDateTime(TimeOnly.MinValue) : x[i]); n++; }
        ExcelTaiSanService.DinhDangBang(ws, 4, Math.Max(4, n - 1), cot.Length);
        dinhDang?.Invoke(ws);
    }

    public static byte[] Kpi(BaoCaoKetQua r)
    {
        using var wb = new XLWorkbook();
        var phu = $"Kỳ báo cáo: {r.Tu:dd/MM/yyyy} – {r.Den:dd/MM/yyyy} · {r.PhamVi}";
        static object? G(double? v) => v is null ? null : Math.Round(v.Value, 1);
        string[] cotSla = ["Nhóm", "Số phiếu", "Đã xử lý", "Đúng SLA", "% đúng SLA", "MTTR (giờ)", "Dừng máy (giờ)", "Chi phí (đ)"];
        object?[] DongSla(DongSla x) => [x.Nhom, x.SoPhieu, x.DaXong, x.DungHan, Math.Round(x.TyLeDungHan, 1), G(x.MttrGio), Math.Round(x.DungMayGio, 1), x.ChiPhi];
        void Tien(IXLWorksheet ws) => ws.Column(8).Style.NumberFormat.Format = "#,##0";
        Sheet(wb, "TheoThang", "SLA & MTTR THEO THÁNG", phu, cotSla, r.TheoThang.Append(r.Tong).Select(DongSla), Tien);
        Sheet(wb, "TheoKhoa", "SLA & MTTR THEO KHOA/PHÒNG", phu, cotSla, r.TheoKhoa.Select(DongSla), Tien);
        Sheet(wb, "TheoUuTien", "SLA THEO MỨC ƯU TIÊN", phu, cotSla, r.TheoUuTien.Select(DongSla), Tien);
        Sheet(wb, "KhoaTrongYeu", "THỜI GIAN DỪNG MÁY – KHOA TRỌNG YẾU", phu, cotSla, r.DungMayKhoaTrongYeu.Select(DongSla), Tien);
        Sheet(wb, "KyThuatVien", "NĂNG SUẤT KỸ THUẬT VIÊN", phu, ["Kỹ thuật viên", "Số phiếu", "Sửa chữa đã xong", "Đúng SLA", "% đúng SLA", "MTTR (giờ)", "Giờ công", "Đánh giá TB (1-5)", "Lệnh bảo trì"],
            r.TheoKtv.Select(x => new object?[] { x.KyThuatVien, x.SoPhieu, x.DaXong, x.DungHan, Math.Round(x.TyLeDungHan, 1), G(x.MttrGio), x.GioCong, G(x.DanhGiaTb), x.BaoTri }));
        Sheet(wb, "ThietBiHongNhieu", "THIẾT BỊ HỎNG NHIỀU NHẤT (TOP 20)", phu, ["Mã TS", "Tên", "Khoa", "Loại", "Số lần hỏng", "Dừng máy (giờ)", "Chi phí SC (đ)", "MTBF (ngày)", "Nguyên giá (đ)"],
            r.ThietBiHongNhieu.Select(x => new object?[] { x.MaTaiSan, x.Ten, x.Khoa, x.Loai, x.SoLanHong, Math.Round(x.DungMayGio, 1), x.ChiPhi, x.MtbfNgay, x.NguyenGia }),
            ws => { ws.Column(7).Style.NumberFormat.Format = "#,##0"; ws.Column(9).Style.NumberFormat.Format = "#,##0"; });
        string[] cotCp = ["Nhóm", "Số phiếu", "Vật tư (đ)", "Thuê ngoài (đ)", "Tổng (đ)"];
        Sheet(wb, "ChiPhiTheoKhoa", "CHI PHÍ SỬA CHỮA, BẢO TRÌ THEO KHOA", phu, cotCp, r.ChiPhiTheoKhoa.Select(x => new object?[] { x.Nhom, x.SoPhieu, x.VatTu, x.ThueNgoai, x.Tong }),
            ws => ws.Columns(3, 5).Style.NumberFormat.Format = "#,##0");
        Sheet(wb, "ChiPhiTheoLoai", "CHI PHÍ SỬA CHỮA, BẢO TRÌ THEO LOẠI THIẾT BỊ", phu, cotCp, r.ChiPhiTheoLoai.Select(x => new object?[] { x.Nhom, x.SoPhieu, x.VatTu, x.ThueNgoai, x.Tong }),
            ws => ws.Columns(3, 5).Style.NumberFormat.Format = "#,##0");
        return Luu(wb);
    }

    public static byte[] NganSach(List<SuDungNganSach> ds, int nam, string tenBenhVien)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("DuToanThucHien");
        ws.Cell(1, 1).Value = $"BÁO CÁO DỰ TOÁN – THỰC HIỆN NGÂN SÁCH CNTT NĂM {nam}"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = $"{tenBenhVien} · lập ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
        string[] cot = ["Mã", "Khoản mục", "Nhóm chi", "Trạng thái", "Dự toán duyệt", "Điều chỉnh", "Dự toán hiệu lực", "Cam kết (đề xuất)", "Cam kết (đơn hàng)",
            "Thực chi (nhận hàng)", "Thực chi (sửa ngoài)", "Thực chi (khác)", "Tổng thực chi", "Còn lại", "% đã dùng"];
        for (var i = 0; i < cot.Length; i++) ws.Cell(4, i + 1).Value = cot[i];
        var r = 5;
        foreach (var x in ds)
        {
            object?[] v = [x.KhoanMuc.Ma, x.KhoanMuc.Ten, x.KhoanMuc.NhomChi.Ten(), x.KhoanMuc.TrangThai.Ten(), x.KhoanMuc.DuToan, x.KhoanMuc.TongDieuChinh, x.DuToan,
                x.CamKetDeXuat, x.CamKetDonHang, x.ThucChiNhanHang, x.ThucChiSuaNgoai, x.ThucChiKhac, x.ThucChi, x.ConLai, x.TyLe / 100];
            for (var i = 0; i < v.Length; i++) ws.Cell(r, i + 1).Value = XLCellValue.FromObject(v[i]);
            if (x.TyLe >= 100) ws.Range(r, 1, r, cot.Length).Style.Font.FontColor = XLColor.Red;
            r++;
        }
        if (ds.Count > 0)
        {
            ws.Cell(r, 2).Value = "TỔNG CỘNG"; ws.Cell(r, 2).Style.Font.Bold = true;
            for (var c = 5; c <= 14; c++) { ws.Cell(r, c).FormulaA1 = $"SUM({ws.Cell(5, c).Address}:{ws.Cell(r - 1, c).Address})"; ws.Cell(r, c).Style.Font.Bold = true; }
            ws.Cell(r, 15).FormulaA1 = $"IF(G{r}=0,0,(I{r}+H{r}+M{r})/G{r})";
            r++;
        }
        ExcelTaiSanService.DinhDangBang(ws, 4, Math.Max(4, r - 1), cot.Length);
        ws.Columns(5, 14).Style.NumberFormat.Format = "#,##0";
        ws.Column(15).Style.NumberFormat.Format = "0.0%";
        return Luu(wb);
    }

    public static byte[] Tco(List<DongTco> theoTaiSan, List<DongTco> theoLoai, List<DongTco> theoModel, string phamVi)
    {
        using var wb = new XLWorkbook();
        var phu = $"{phamVi} · lập ngày {DateTime.Now:dd/MM/yyyy}. TCO = nguyên giá + sửa chữa + bảo trì định kỳ + hợp đồng bảo trì phân bổ + bản quyền phân bổ.";
        string[] cotNhom = ["Nhóm", "Số tài sản", "Nguyên giá", "Sửa chữa", "Bảo trì định kỳ", "Hợp đồng bảo trì", "Bản quyền", "Chi phí vận hành", "TCO", "Số lần hỏng", "Tuổi TB (năm)", "TCO/năm/tài sản", "% sửa/nguyên giá"];
        object?[] Nhom(DongTco x) => [x.Nhom, x.SoTaiSan, x.NguyenGia, x.SuaChua, x.BaoTri, x.HopDong, x.BanQuyen, x.VanHanh, x.Tong, x.SoLanHong, x.NamSuDung, x.MoiNam, x.TyLeSua];
        void Tien(IXLWorksheet ws) { ws.Columns(3, 9).Style.NumberFormat.Format = "#,##0"; ws.Column(12).Style.NumberFormat.Format = "#,##0"; }
        Sheet(wb, "TheoLoai", "TCO THEO LOẠI THIẾT BỊ", phu, cotNhom, theoLoai.Select(Nhom), Tien);
        Sheet(wb, "TheoModel", "TCO THEO MODEL", phu, cotNhom, theoModel.Select(Nhom), Tien);
        Sheet(wb, "TheoTaiSan", "TCO TỪNG TÀI SẢN", phu, ["Mã TS", "Loại", "Model", "Khoa", "Nguyên giá", "Sửa chữa", "Bảo trì định kỳ", "Hợp đồng bảo trì", "Bản quyền", "Chi phí vận hành", "TCO", "Số lần hỏng", "Tuổi (năm)", "TCO/năm", "% sửa/nguyên giá"],
            theoTaiSan.Select(x => new object?[] { x.MaTaiSan, x.Loai, x.Model, x.Khoa, x.NguyenGia, x.SuaChua, x.BaoTri, x.HopDong, x.BanQuyen, x.VanHanh, x.Tong, x.SoLanHong, x.NamSuDung, x.MoiNam, x.TyLeSua }),
            ws => { ws.Columns(5, 11).Style.NumberFormat.Format = "#,##0"; ws.Column(14).Style.NumberFormat.Format = "#,##0"; });
        return Luu(wb);
    }

    public static byte[] TuyBien(KetQuaBaoCao kq, string ten)
    {
        using var wb = new XLWorkbook();
        Sheet(wb, "BaoCao", ten.ToUpperInvariant(), $"{kq.MoTaLoc} · {kq.TongBanGhi} bản ghi · lập ngày {DateTime.Now:dd/MM/yyyy HH:mm}",
            kq.Cot.Select(c => c.Ten).ToArray(), kq.Dong, ws =>
            {
                for (var i = 0; i < kq.Cot.Count; i++)
                    if (kq.Cot[i].LaSo) ws.Column(i + 1).Style.NumberFormat.Format = "#,##0.##";
                    else if (kq.Dong.FirstOrDefault(r => r[i] is not null)?[i] is DateTime or DateOnly) ws.Column(i + 1).Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
            });
        return Luu(wb);
    }
}
