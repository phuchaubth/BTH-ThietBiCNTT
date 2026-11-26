using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Lưu / đọc tệp đính kèm trên đĩa (thư mục cấu hình DinhKem:Path).</summary>
public class TepService(AppDbContext db, IConfiguration config, IHostEnvironment env)
{
    public const long KichThuocToiDa = 10 * 1024 * 1024;
    private static readonly Dictionary<string, string> LoaiChoPhep = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp", [".pdf"] = "application/pdf",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".doc"] = "application/msword", [".xls"] = "application/vnd.ms-excel"
    };

    public string ThuMucGoc => Directory.CreateDirectory(config["DinhKem:Path"] is { Length: > 0 } p ? p
        : Path.Combine(env.ContentRootPath, "App_Data", "dinhkem")).FullName;

    public async Task<TepDinhKem> LuuAsync(string doiTuong, string doiTuongId, string tenFile, Stream noiDung, long kichThuoc, string? nguoi)
    {
        var ext = Path.GetExtension(tenFile);
        if (!LoaiChoPhep.TryGetValue(ext, out var ct)) throw new DomainException($"'{tenFile}': chỉ nhận ảnh JPG/PNG/WEBP, PDF hoặc Word/Excel.");
        if (kichThuoc <= 0 || kichThuoc > KichThuocToiDa) throw new DomainException($"'{tenFile}': tối đa 10 MB.");
        using var ms = new MemoryStream();
        await noiDung.CopyToAsync(ms);
        if (!DungDinhDang(ms.GetBuffer().AsSpan(0, (int)Math.Min(ms.Length, 16)), ct)) throw new DomainException($"'{tenFile}': nội dung không đúng định dạng {ext}.");
        var thang = DateTime.Now.ToString("yyyyMM");
        var tenLuu = $"{thang}/{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        Directory.CreateDirectory(Path.Combine(ThuMucGoc, thang));
        ms.Position = 0;
        await using (var fs = File.Create(Path.Combine(ThuMucGoc, tenLuu))) await ms.CopyToAsync(fs);
        var tep = new TepDinhKem { DoiTuong = doiTuong, DoiTuongId = doiTuongId, TenFile = Path.GetFileName(tenFile), TenLuu = tenLuu,
            ContentType = ct, KichThuoc = ms.Length, NguoiTai = nguoi, ThoiGian = DateTime.Now };
        db.TepDinhKems.Add(tep);
        return tep;
    }

    /// <summary>Kiểm tra "magic bytes" — không tin phần mở rộng tên file.</summary>
    private static bool DungDinhDang(ReadOnlySpan<byte> b, string ct) => ct switch
    {
        "image/jpeg" => b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
        "image/png" => b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
        "image/webp" => b.Length >= 12 && b[..4].SequenceEqual("RIFF"u8) && b[8..12].SequenceEqual("WEBP"u8),
        "application/pdf" => b.Length >= 5 && b[..5].SequenceEqual("%PDF-"u8),
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" or "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            => b.Length >= 4 && b[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
        "application/msword" or "application/vnd.ms-excel" => b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
        _ => false
    };

    public string DuongDan(TepDinhKem t)
    {
        var p = Path.GetFullPath(Path.Combine(ThuMucGoc, t.TenLuu));
        if (!p.StartsWith(ThuMucGoc, StringComparison.Ordinal)) throw new InvalidOperationException("Đường dẫn không hợp lệ.");
        return p;
    }

    public Task<List<TepDinhKem>> DanhSachAsync(string doiTuong, string doiTuongId) =>
        db.TepDinhKems.AsNoTracking().Where(t => t.DoiTuong == doiTuong && t.DoiTuongId == doiTuongId).OrderBy(t => t.Id).ToListAsync();
}

public static class KiemTraTongHop
{
    /// <summary>Dòng bản tin hằng ngày: số lượt kiểm tra quá hạn.</summary>
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var svc = (KiemTraService)sp.GetService(typeof(KiemTraService))!;
        var qua = (await svc.DanhSachViecAsync(now, chiDenHan: true)).Count(v => v.QuaHan);
        return qua > 0 ? [$"• {qua} lượt kiểm tra thiết bị quá hạn"] : [];
    }
}

public record ViecKiemTra(MauKiemTra Mau, TaiSan? TaiSan, DateTime? LanCuoi, DateTime HanTiep, bool QuaHan);

/// <summary>Kiểm tra thiết bị: danh sách đến hạn, ghi kết quả, xử lý bất thường.</summary>
public class KiemTraService(AppDbContext db, ThongBaoService tb, SoPhieuService soPhieu, ThamSoService thamSo, LichSuService lichSu)
{
    /// <summary>Các đối tượng của một mẫu: tài sản cụ thể / mọi tài sản đang dùng của loại (trong khoa) / [null] nếu kiểm tra theo vị trí.</summary>
    public async Task<List<TaiSan?>> DoiTuongAsync(MauKiemTra m)
    {
        if (m.KhongGanTaiSan) return [null];
        var q = db.TaiSans.AsNoTracking().Include(t => t.KhoaPhong).Where(t => t.TrangThai != TrangThaiTaiSan.DaThanhLy && t.TrangThai != TrangThaiTaiSan.ChoThanhLy);
        q = m.TaiSanId is { } id ? q.Where(t => t.Id == id)
            : q.Where(t => t.LoaiThietBiId == m.LoaiThietBiId && t.TrangThai != TrangThaiTaiSan.TrongKho && (m.KhoaPhongId == null || t.KhoaPhongId == m.KhoaPhongId));
        return (await q.OrderBy(t => t.MaTaiSan).ToListAsync()).Cast<TaiSan?>().ToList();
    }

    /// <summary>Danh sách việc kiểm tra, sắp theo hạn. <paramref name="khoaId"/> lọc mẫu theo khoa thực hiện.</summary>
    public async Task<List<ViecKiemTra>> DanhSachViecAsync(DateTime now, int? khoaId = null, bool chiDenHan = false)
    {
        var maus = await db.MauKiemTras.AsNoTracking().Include(m => m.KhoaPhong).Where(m => m.DangApDung && !m.DungChoBaoTri && (khoaId == null || m.KhoaPhongId == khoaId)).ToListAsync();
        var lanCuoi = (await db.LanKiemTras.GroupBy(l => new { l.MauKiemTraId, l.TaiSanId })
                .Select(g => new { g.Key.MauKiemTraId, g.Key.TaiSanId, Max = g.Max(x => x.ThoiGian) }).ToListAsync())
            .ToDictionary(x => (x.MauKiemTraId, x.TaiSanId), x => x.Max);
        var kq = new List<ViecKiemTra>();
        foreach (var m in maus)
            foreach (var t in await DoiTuongAsync(m))
            {
                var cuoi = lanCuoi.TryGetValue((m.Id, t?.Id), out var c) ? c : (DateTime?)null;
                var han = (cuoi ?? m.NgayTao).AddHours(m.ChuKyGio);
                if (cuoi is null && m.NgayTao.AddHours(m.ChuKyGio) > now) han = now; // mẫu mới: cần kiểm ngay lần đầu
                if (!chiDenHan || han <= now) kq.Add(new ViecKiemTra(m, t, cuoi, han, han < now.AddMinutes(-30)));
            }
        return kq.OrderBy(v => v.HanTiep).ToList();
    }

    public async Task<LanKiemTra> GhiAsync(int mauId, int? taiSanId, IReadOnlyDictionary<int, string?> nhap, string nguoi, string? ghiChu, int? phieuBaoTriId = null)
    {
        var mau = await db.MauKiemTras.Include(m => m.Mucs).FirstOrDefaultAsync(m => m.Id == mauId) ?? throw new DomainException("Không tìm thấy phiếu kiểm tra.");
        PhieuSuaChua? pm = null;
        if (phieuBaoTriId is { } pid)
        {
            // Checklist của lệnh bảo trì: đúng thiết bị, đúng mẫu của kế hoạch, lệnh còn mở
            pm = await db.PhieuSuaChuas.Include(p => p.KeHoachBaoTri).FirstOrDefaultAsync(p => p.Id == pid)
                 ?? throw new DomainException("Không tìm thấy lệnh bảo trì.");
            if (pm.LoaiCongViec != LoaiCongViec.BaoTriDinhKy || pm.KeHoachBaoTri?.MauKiemTraId != mauId || pm.TaiSanId != taiSanId)
                throw new DomainException("Checklist không khớp với lệnh bảo trì.");
            if (pm.DaDong || pm.TrangThai == TrangThaiPhieuSuaChua.HoanThanh) throw new DomainException("Lệnh bảo trì đã hoàn thành.");
        }
        else if (taiSanId is not null && !(await DoiTuongAsync(mau)).Any(t => t?.Id == taiSanId)) throw new DomainException("Thiết bị không thuộc phạm vi phiếu kiểm tra này.");
        var now = DateTime.Now;
        var lan = mau.ChamDiem(nhap, mau.KhongGanTaiSan ? null : taiSanId, nguoi, now, ghiChu);
        lan.PhieuBaoTriId = pm?.Id;
        db.LanKiemTras.Add(lan);
        await db.SaveChangesAsync();
        if (pm is not null)
        {
            db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = pm.Id, ThoiGian = now, NguoiViet = nguoi, LaHeThong = true,
                NoiDung = $"Đã thực hiện checklist '{mau.Ten}': " + (lan.CoBatThuong ? "có mục không đạt — " + lan.TomTatBatThuong() : "tất cả đạt") });
            if (pm.TrangThai == TrangThaiPhieuSuaChua.DaTiepNhan) pm.BatDauXuLy(now);
            await db.SaveChangesAsync();
        }
        if (lan.CoBatThuong) await XuLyBatThuongAsync(mau, lan, nguoi);
        return lan;
    }

    private async Task XuLyBatThuongAsync(MauKiemTra mau, LanKiemTra lan, string nguoi)
    {
        var tomTat = lan.TomTatBatThuong();
        var ts = lan.TaiSanId is { } id ? await db.TaiSans.Include(t => t.KhoaPhong).FirstAsync(t => t.Id == id) : null;
        if (ts is not null && mau.TuTaoPhieu)
        {
            var mo = await db.PhieuSuaChuas.FirstOrDefaultAsync(p => p.TaiSanId == ts.Id && p.LoaiCongViec == LoaiCongViec.SuaChua
                && p.TrangThai != TrangThaiPhieuSuaChua.DaNghiemThu && p.TrangThai != TrangThaiPhieuSuaChua.DaHuy && p.TrangThai != TrangThaiPhieuSuaChua.HoanThanh);
            if (mo is not null)
            {
                // Đã có phiếu đang mở → gắn vào, không tạo trùng
                lan.PhieuSuaChuaId = mo.Id;
                db.BinhLuanPhieus.Add(new BinhLuanPhieu { PhieuSuaChuaId = mo.Id, ThoiGian = DateTime.Now, NguoiViet = nguoi, NoiDung = $"[Kiểm tra '{mau.Ten}'] Bất thường: {tomTat}" });
            }
            else
            {
                var now = DateTime.Now;
                var p = new PhieuSuaChua
                {
                    TaiSanId = ts.Id, KhoaPhongId = ts.KhoaPhongId, NguoiYeuCau = nguoi, NguoiTao = nguoi,
                    MoTaSuCo = $"[Kiểm tra bất thường – {mau.Ten}] {tomTat}" + (lan.GhiChu is null ? "" : $"\nGhi chú: {lan.GhiChu}"),
                    UuTien = MucDoUuTien.Cao, NgayYeuCau = now, HanXuLy = thamSo.TinhHanSla(now, MucDoUuTien.Cao, ts.KhoaPhong?.LaKhoaTrongYeu == true),
                };
                db.PhieuSuaChuas.Add(p);
                await db.LuuMoiAsync(soPhieu, "SC", so => p.SoPhieu = so);
                lan.PhieuSuaChuaId = p.Id;
                lichSu.Ghi(ts.Id, LoaiSuKien.SuaChua, $"Kiểm tra bất thường → tự tạo {p.SoPhieu}: {tomTat}", nguoi);
                await tb.PhieuMoiAsync(p, ts.KhoaPhong?.Ten, $"{ts.MaTaiSan} {ts.Ten}");
            }
        }
        await tb.GuiAsync(await tb.CanBoCNTTAsync(), LoaiThongBao.KiemTraBatThuong,
            $"Kiểm tra bất thường: {mau.Ten}" + (ts is null ? (mau.ViTri is null ? "" : $" · {mau.ViTri}") : $" · {ts.MaTaiSan}"),
            tomTat + $"\nNgười kiểm: {nguoi}", $"/KiemTra/Lan/{lan.Id}", MucDoThongBao.CanhBao, guiNhomCNTT: true, boQua: nguoi);
        await db.SaveChangesAsync();
    }
}
