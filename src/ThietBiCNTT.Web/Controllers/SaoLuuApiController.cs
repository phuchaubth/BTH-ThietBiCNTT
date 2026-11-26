using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Infrastructure.Data;
using ThietBiCNTT.Infrastructure.Services;

namespace ThietBiCNTT.Web.Controllers;

public record KetQuaSaoLuuApi(int TacVuId, string KetQua, DateTime? ThoiDiem, decimal? DungLuongGb, int? ThoiLuongPhut, string? GhiChu);

/// <summary>FR-SL-03: nhận kết quả sao lưu tự động từ phần mềm sao lưu / script (khoá API trong tham số hệ thống).</summary>
[ApiController, AllowAnonymous, IgnoreAntiforgeryToken, Route("api/saoluu")]
public class SaoLuuApiController(AppDbContext db, ThamSoService thamSo) : ControllerBase
{
    [HttpPost("ketqua")]
    public async Task<IActionResult> KetQua([FromBody] KetQuaSaoLuuApi body)
    {
        var khoa = thamSo.Chuoi(TS.JciSaoLuuApiKey);
        if (string.IsNullOrWhiteSpace(khoa) || khoa.Length < 24) return StatusCode(503, new { loi = "API chưa được bật (chưa đặt khoá ≥ 24 ký tự)." });
        var gui = Request.Headers["X-Api-Key"].ToString();
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(Encoding.UTF8.GetBytes(gui)), SHA256.HashData(Encoding.UTF8.GetBytes(khoa)))) return Unauthorized(new { loi = "Khoá API không đúng." });
        var tv = await db.TacVuSaoLuus.FirstOrDefaultAsync(t => t.Id == body.TacVuId && t.DangApDung);
        if (tv is null) return NotFound(new { loi = "Không tìm thấy tác vụ sao lưu đang áp dụng." });
        var kq = body.KetQua?.Trim().ToLowerInvariant() switch { "thanhcong" or "success" or "ok" => KetQuaSaoLuu.ThanhCong, "canhbao" or "warning" => KetQuaSaoLuu.CanhBao, "loi" or "error" or "failed" => KetQuaSaoLuu.Loi, _ => (KetQuaSaoLuu?)null };
        if (kq is null) return BadRequest(new { loi = "ketQua phải là ThanhCong, CanhBao hoặc Loi." });
        var t = body.ThoiDiem ?? DateTime.Now;
        if (t > DateTime.Now.AddMinutes(5)) return BadRequest(new { loi = "thoiDiem ở tương lai." });
        if (kq != KetQuaSaoLuu.ThanhCong && string.IsNullOrWhiteSpace(body.GhiChu)) return BadRequest(new { loi = "Kết quả cảnh báo / lỗi cần ghiChu." });
        if (await db.KetQuaSaoLuuLans.AnyAsync(k => k.TacVuSaoLuuId == tv.Id && k.ThoiDiem == t && k.Nguon == NguonKetQua.Api)) return Conflict(new { loi = "Đã nhận kết quả này." });
        db.KetQuaSaoLuuLans.Add(new KetQuaSaoLuuLan { TacVuSaoLuuId = tv.Id, ThoiDiem = t, KetQua = kq.Value, DungLuongGb = body.DungLuongGb, ThoiLuongPhut = body.ThoiLuongPhut, GhiChu = body.GhiChu?.Trim(), Nguon = NguonKetQua.Api, NguoiGhi = "api", ThoiGianGhi = DateTime.Now });
        await db.SaveChangesAsync();
        return Ok(new { ketQua = kq.ToString() });
    }
}
