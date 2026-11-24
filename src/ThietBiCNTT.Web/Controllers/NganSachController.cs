using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;
using ThietBiCNTT.Web.Infrastructure;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Ngân sách CNTT: dự toán theo khoản mục, điều chỉnh có duyệt, theo dõi cam kết / thực chi, báo cáo.</summary>
[Authorize(Roles = VaiTro.NhomCNTT + "," + VaiTro.LanhDao)]
public class NganSachController(AppDbContext db, NganSachService ns, ThamSoService thamSo) : Controller
{
    public async Task<IActionResult> Index(int? nam)
    {
        var n = nam ?? DateTime.Today.Year;
        ViewData["nam"] = n;
        ViewBag.DsNam = (await db.KhoanMucNganSachs.Select(k => k.Nam).Distinct().ToListAsync()).Append(n).Append(DateTime.Today.Year).Distinct().OrderByDescending(x => x).ToList();
        ViewBag.Nguong = thamSo.SoNguyen(TS.NganSachNguongCanhBao);
        return View(await ns.SuDungAsync(n));
    }

    [Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Sua(int? id, int? nam)
    {
        var k = id is null ? new KhoanMucNganSach { Nam = nam ?? DateTime.Today.Year } : await db.KhoanMucNganSachs.FindAsync(id);
        if (k is null) return NotFound();
        if (k.TrangThai != TrangThaiNganSach.DuThao) { TempData["err"] = "Khoản mục đã duyệt — dùng điều chỉnh dự toán."; return RedirectToAction(nameof(ChiTiet), new { id }); }
        return View(k);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Sua(KhoanMucNganSach input)
    {
        if (!ModelState.IsValid) return View(input);
        try
        {
            input.KiemTraPhanBo();
            if (await db.KhoanMucNganSachs.AnyAsync(k => k.Nam == input.Nam && k.Ma == input.Ma && k.Id != input.Id)) throw new DomainException($"Mã {input.Ma} đã có trong năm {input.Nam}.");
            KhoanMucNganSach k;
            if (input.Id == 0) { k = new KhoanMucNganSach(); db.KhoanMucNganSachs.Add(k); }
            else
            {
                k = await db.KhoanMucNganSachs.FindAsync(input.Id) ?? throw new DomainException("Không tìm thấy khoản mục.");
                if (k.TrangThai != TrangThaiNganSach.DuThao) throw new DomainException("Khoản mục đã duyệt — dùng điều chỉnh dự toán.");
            }
            k.Ma = input.Ma.Trim().ToUpperInvariant(); k.Ten = input.Ten; k.Nam = input.Nam; k.NhomChi = input.NhomChi; k.DuToan = input.DuToan;
            k.Quy1 = input.Quy1; k.Quy2 = input.Quy2; k.Quy3 = input.Quy3; k.Quy4 = input.Quy4; k.GomChiPhiSuaNgoai = input.GomChiPhiSuaNgoai; k.GhiChu = input.GhiChu;
            await db.SaveChangesAsync();
            TempData["ok"] = $"Đã lưu khoản mục {k.Ma} (dự thảo) — chờ Lãnh đạo/Quản trị duyệt.";
            return RedirectToAction(nameof(ChiTiet), new { id = k.Id });
        }
        catch (DomainException ex) { ModelState.AddModelError("", ex.Message); return View(input); }
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var sd = await ns.MotAsync(id);
        if (sd is null) return NotFound();
        ViewBag.DeXuats = await db.DeXuatMuaSams.AsNoTracking().Include(x => x.Dongs).Where(x => x.KhoanMucNganSachId == id && x.TrangThai != TrangThaiDeXuat.DaHuy)
            .OrderByDescending(x => x.Id).ToListAsync();
        ViewBag.DonHangs = await db.DonMuaHangs.AsNoTracking().Include(x => x.Dongs).Include(x => x.NhaCungCap).Include(x => x.PhieuNhans)
            .Where(x => x.KhoanMucNganSachId == id && x.TrangThai != TrangThaiDonHang.DaHuy).OrderByDescending(x => x.Id).ToListAsync();
        ViewBag.SuaNgoai = sd.ThucChiSuaNgoai > 0
            ? await db.PhieuSuaChuas.AsNoTracking().Include(p => p.TaiSan).Where(p => p.NgayHoanThanh != null && p.NgayHoanThanh.Value.Year == sd.KhoanMuc.Nam && p.ChiPhiThueNgoai > 0)
                .OrderByDescending(p => p.NgayHoanThanh).ToListAsync()
            : new List<PhieuSuaChua>();
        ViewBag.Nguong = thamSo.SoNguyen(TS.NganSachNguongCanhBao);
        return View(sd);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet)]
    public async Task<IActionResult> Duyet(int id)
    {
        var k = await db.KhoanMucNganSachs.FindAsync(id);
        if (k is null) return NotFound();
        try { k.Duyet(User.Identity!.Name!, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = $"Đã duyệt dự toán {k.Ma}: {k.DuToan:N0} đ."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet)]
    public async Task<IActionResult> KhoaSo(int id)
    {
        var k = await db.KhoanMucNganSachs.FindAsync(id);
        if (k is null) return NotFound();
        try { k.KhoaSo(); await db.SaveChangesAsync(); TempData["ok"] = $"Đã khoá sổ {k.Ma}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> Xoa(int id)
    {
        var k = await db.KhoanMucNganSachs.FindAsync(id);
        if (k is null) return NotFound();
        if (k.TrangThai != TrangThaiNganSach.DuThao || await db.DeXuatMuaSams.AnyAsync(x => x.KhoanMucNganSachId == id) || await db.DonMuaHangs.AnyAsync(x => x.KhoanMucNganSachId == id))
        { TempData["err"] = "Chỉ xoá khoản mục dự thảo chưa được sử dụng."; return RedirectToAction(nameof(ChiTiet), new { id }); }
        db.KhoanMucNganSachs.Remove(k); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã xoá khoản mục {k.Ma}.";
        return RedirectToAction(nameof(Index), new { nam = k.Nam });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DieuChinh(int id, decimal soTien, bool giam, string? lyDo)
    {
        var k = await db.KhoanMucNganSachs.Include(x => x.DieuChinhs).FirstOrDefaultAsync(x => x.Id == id);
        if (k is null) return NotFound();
        try
        {
            k.DeNghiDieuChinh(giam ? -Math.Abs(soTien) : Math.Abs(soTien), lyDo ?? "", User.Identity!.Name!, DateTime.Now);
            await db.SaveChangesAsync();
            TempData["ok"] = "Đã gửi đề nghị điều chỉnh — chờ Lãnh đạo/Quản trị khác duyệt.";
        }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomDuyet)]
    public async Task<IActionResult> DuyetDieuChinh(int id, int dieuChinhId, bool dongY)
    {
        var k = await db.KhoanMucNganSachs.Include(x => x.DieuChinhs).FirstOrDefaultAsync(x => x.Id == id);
        var d = k?.DieuChinhs.FirstOrDefault(x => x.Id == dieuChinhId);
        if (k is null || d is null) return NotFound();
        try { k.DuyetDieuChinh(d, dongY, User.Identity!.Name!, DateTime.Now); await db.SaveChangesAsync(); TempData["ok"] = dongY ? $"Đã duyệt điều chỉnh {d.SoTien:+#,0;-#,0} đ." : "Đã từ chối điều chỉnh."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> ThemChi(int id, DateOnly ngay, decimal soTien, string? soChungTu, string? noiDung)
    {
        try { await ns.ThemChiAsync(id, new ChiNganSach { Ngay = ngay, SoTien = soTien, SoChungTu = soChungTu, NoiDung = noiDung ?? "" }, User.Identity!.Name!); TempData["ok"] = "Đã ghi khoản chi."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.NhomCNTT)]
    public async Task<IActionResult> XoaChi(int id, int chiId)
    {
        var c = await db.ChiNganSachs.FirstOrDefaultAsync(x => x.Id == chiId && x.KhoanMucNganSachId == id);
        var k = await db.KhoanMucNganSachs.FindAsync(id);
        if (c is null || k is null) return NotFound();
        if (!k.DungDuoc) TempData["err"] = "Khoản mục đã khoá sổ.";
        else { db.ChiNganSachs.Remove(c); await db.SaveChangesAsync(); TempData["ok"] = "Đã xoá khoản chi."; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    /// <summary>Báo cáo dự toán – thực hiện theo khoản mục (xlsx).</summary>
    public async Task<IActionResult> Xuat(int nam)
    {
        var ds = await ns.SuDungAsync(nam);
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("DuToanThucHien");
        ws.Cell(1, 1).Value = $"BÁO CÁO DỰ TOÁN – THỰC HIỆN NGÂN SÁCH CNTT NĂM {nam}"; ws.Cell(1, 1).Style.Font.Bold = true; ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = $"{thamSo.Chuoi(TS.TenBenhVien)} · lập ngày {DateTime.Now:dd/MM/yyyy HH:mm}";
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
        using var ms = new MemoryStream(); wb.SaveAs(ms);
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"NganSach_{nam}.xlsx");
    }
}
