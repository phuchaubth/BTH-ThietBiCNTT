using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Identity;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

/// <summary>Máy trạm có agent: ghép tài sản, đối chiếu chuẩn HIS, phần mềm, lịch sử thay đổi.</summary>
[Authorize(Roles = VaiTro.NhomCNTT)]
public class AgentController(AppDbContext db, AgentService agent, ThamSoService thamSo, IWebHostEnvironment env) : Controller
{
    public async Task<IActionResult> Index(string? loc, string? q)
    {
        var ds = await db.MayTramAgents.AsNoTracking().Include(m => m.TaiSan).ThenInclude(t => t!.KhoaPhong).OrderBy(m => m.Hostname).ToListAsync();
        var chuan = agent.Chuan(); var now = DateTime.Now; var ngay = thamSo.SoNguyen(TS.AgentKhongGuiNgay);
        ViewBag.TongSo = ds.Count;
        ViewBag.ChuaGhep = ds.Count(m => m.TaiSanId == null);
        ViewBag.KhongGui = ds.Count(m => m.KhongGui(now, ngay));
        ViewBag.KhongDat = ds.Count(m => m.DoiChieu(chuan).Any(x => !x.Dat));
        if (!string.IsNullOrWhiteSpace(q)) ds = ds.Where(m => m.Hostname.Contains(q, StringComparison.OrdinalIgnoreCase) || (m.Ip ?? "").Contains(q) || (m.NguoiDangNhap ?? "").Contains(q, StringComparison.OrdinalIgnoreCase)
            || (m.TaiSan?.MaTaiSan ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) || m.DsPhanMem.Any(p => p.Contains(q, StringComparison.OrdinalIgnoreCase))).ToList();
        ds = loc switch
        {
            "chuaghep" => ds.Where(m => m.TaiSanId == null).ToList(),
            "khonggui" => ds.Where(m => m.KhongGui(now, ngay)).ToList(),
            "khongdat" => ds.Where(m => m.DoiChieu(chuan).Any(x => !x.Dat)).ToList(),
            _ => ds
        };
        ViewBag.Chuan = chuan; ViewBag.Ngay = ngay; ViewData["loc"] = loc; ViewData["q"] = q;
        return View(ds);
    }

    public async Task<IActionResult> ChiTiet(int id)
    {
        var m = await db.MayTramAgents.AsNoTracking().Include(x => x.TaiSan).ThenInclude(t => t!.CauHinh).FirstOrDefaultAsync(x => x.Id == id);
        if (m is null) return NotFound();
        ViewBag.Chuan = agent.Chuan();
        ViewBag.LichSu = m.TaiSanId is null ? new List<LichSuTaiSan>() : await db.LichSuTaiSans.AsNoTracking()
            .Where(l => l.TaiSanId == m.TaiSanId && l.LoaiSuKien == LoaiSuKien.CauHinh).OrderByDescending(l => l.ThoiGian).Take(50).ToListAsync();
        return View(m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ghep(int id, string maTaiSan)
    {
        try { await agent.GhepAsync(id, maTaiSan ?? "", User.Identity!.Name!); TempData["ok"] = $"Đã ghép với {maTaiSan}."; }
        catch (DomainException ex) { TempData["err"] = ex.Message; }
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BoGhep(int id)
    {
        var m = await db.MayTramAgents.FindAsync(id);
        if (m is null) return NotFound();
        m.TaiSanId = null; await db.SaveChangesAsync();
        TempData["ok"] = "Đã bỏ ghép. Lần gửi sau agent sẽ không tự ghép lại nếu không khớp mã / serial.";
        return RedirectToAction(nameof(ChiTiet), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = VaiTro.QuanTri)]
    public async Task<IActionResult> Xoa(int id)
    {
        var m = await db.MayTramAgents.FindAsync(id);
        if (m is null) return NotFound();
        db.MayTramAgents.Remove(m); await db.SaveChangesAsync();
        TempData["ok"] = $"Đã xoá máy {m.Hostname} khỏi danh sách agent.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Tải script agent (PowerShell) để cài qua GPO.</summary>
    public IActionResult TaiScript(string ten = "ThuThapCauHinh.ps1")
    {
        if (ten is not ("ThuThapCauHinh.ps1" or "CaiDatAgent.ps1")) return NotFound();
        var f = env.WebRootFileProvider.GetFileInfo("agent/" + ten);
        return f.Exists ? File(f.CreateReadStream(), "text/plain; charset=utf-8", ten) : NotFound();
    }
}
