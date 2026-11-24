using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Common;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>Ngân sách CNTT: tính cam kết / thực chi theo khoản mục từ chứng từ mua sắm, sửa chữa và chi ghi tay.</summary>
public class NganSachService(AppDbContext db, ThamSoService thamSo)
{
    private static readonly TrangThaiDeXuat[] DeXuatCamKet = [TrangThaiDeXuat.DaDuyet, TrangThaiDeXuat.DatMotPhan];
    private static readonly TrangThaiDonHang[] DonCamKet = [TrangThaiDonHang.Nhap, TrangThaiDonHang.DaDatHang, TrangThaiDonHang.NhanMotPhan];

    public async Task<List<SuDungNganSach>> SuDungAsync(int? nam = null, int? khoanMucId = null)
    {
        var q = db.KhoanMucNganSachs.Include(k => k.DieuChinhs).Include(k => k.KhoanChis).AsNoTracking().AsQueryable();
        if (nam is not null) q = q.Where(k => k.Nam == nam);
        if (khoanMucId is not null) q = q.Where(k => k.Id == khoanMucId);
        var kms = await q.OrderBy(k => k.Nam).ThenBy(k => k.Ma).ToListAsync();
        if (kms.Count == 0) return [];
        var ids = kms.Select(k => k.Id).ToList();
        // Tổng hợp trên bộ nhớ (SQLite không cộng được decimal phía CSDL; số dòng mỗi năm nhỏ).
        var pr = await db.DongDeXuats.Where(d => ids.Contains(d.DeXuatMuaSam!.KhoanMucNganSachId!.Value) && DeXuatCamKet.Contains(d.DeXuatMuaSam.TrangThai))
            .Select(d => new { Km = d.DeXuatMuaSam!.KhoanMucNganSachId!.Value, d.SoLuong, d.SoLuongDaDat, d.DonGiaDuKien }).ToListAsync();
        var po = await db.DongDonHangs.Where(d => ids.Contains(d.DonMuaHang!.KhoanMucNganSachId!.Value) && DonCamKet.Contains(d.DonMuaHang.TrangThai))
            .Select(d => new { Km = d.DonMuaHang!.KhoanMucNganSachId!.Value, d.SoLuong, d.SoLuongDaNhan, d.DonGia, d.DonMuaHang.ThueSuat }).ToListAsync();
        var nh = await db.PhieuNhanHangs.Where(p => ids.Contains(p.DonMuaHang!.KhoanMucNganSachId!.Value))
            .Select(p => new { Km = p.DonMuaHang!.KhoanMucNganSachId!.Value, p.GiaTri }).ToListAsync();
        var gom = kms.Where(k => k.GomChiPhiSuaNgoai).Select(k => k.Nam).Distinct().ToList();
        var sn = await db.PhieuSuaChuas.Where(p => gom.Count > 0 && p.NgayHoanThanh != null && gom.Contains(p.NgayHoanThanh.Value.Year) && p.ChiPhiThueNgoai > 0)
            .Select(p => new { Nam = p.NgayHoanThanh!.Value.Year, p.ChiPhiThueNgoai }).ToListAsync();
        return kms.Select(k => new SuDungNganSach(k,
            pr.Where(x => x.Km == k.Id).Sum(x => Math.Max(0, x.SoLuong - x.SoLuongDaDat) * x.DonGiaDuKien),
            po.Where(x => x.Km == k.Id).Sum(x => Math.Round(Math.Max(0, x.SoLuong - x.SoLuongDaNhan) * x.DonGia * (1 + x.ThueSuat / 100), 0)),
            nh.Where(x => x.Km == k.Id).Sum(x => x.GiaTri),
            // Một năm có thể có nhiều khoản mục bật cờ — chỉ khoản mục đầu tiên (theo mã) nhận chi phí, tránh tính trùng.
            k.GomChiPhiSuaNgoai && kms.First(x => x.Nam == k.Nam && x.GomChiPhiSuaNgoai).Id == k.Id ? sn.Where(x => x.Nam == k.Nam).Sum(x => x.ChiPhiThueNgoai) : 0,
            k.KhoanChis.Sum(c => c.SoTien))).ToList();
    }

    public async Task<SuDungNganSach?> MotAsync(int khoanMucId) => (await SuDungAsync(khoanMucId: khoanMucId)).FirstOrDefault();

    public Task<List<KhoanMucNganSach>> DungDuocAsync(int? nam = null)
    {
        var n = nam ?? DateTime.Today.Year;
        return db.KhoanMucNganSachs.AsNoTracking().Where(k => k.TrangThai == TrangThaiNganSach.DaDuyet && k.Nam >= n - 1 && k.Nam <= n + 1)
            .OrderByDescending(k => k.Nam).ThenBy(k => k.Ma).ToListAsync();
    }

    /// <summary>
    /// Kiểm tra đề xuất với ngân sách khi gửi/duyệt. Trả về cảnh báo (null nếu ổn).
    /// Ném lỗi khi bật tham số chặn vượt dự toán, hoặc bắt buộc khoản mục mà chưa chọn.
    /// </summary>
    public async Task<string?> KiemTraDeXuatAsync(DeXuatMuaSam dx, bool chan)
    {
        if (dx.KhoanMucNganSachId is null)
        {
            if (thamSo.BatTat(TS.NganSachBatBuocKhoanMuc)) throw new DomainException("Chọn khoản mục ngân sách cho đề xuất.");
            return null;
        }
        var sd = await MotAsync(dx.KhoanMucNganSachId.Value) ?? throw new DomainException("Khoản mục ngân sách không tồn tại.");
        if (!sd.KhoanMuc.DungDuoc) throw new DomainException($"Khoản mục {sd.KhoanMuc.Ma} chưa được duyệt hoặc đã khoá sổ.");
        // Đề xuất đang chờ duyệt chưa nằm trong cam kết; đề xuất đã duyệt thì đã tính rồi.
        var conLai = sd.ConLai + (DeXuatCamKet.Contains(dx.TrangThai) ? dx.Dongs.Sum(d => d.ConLai * d.DonGiaDuKien) : 0);
        var can = dx.Dongs.Sum(d => d.ConLai * d.DonGiaDuKien);
        if (can <= conLai) return null;
        var msg = $"Vượt ngân sách: khoản mục {sd.KhoanMuc.Ma} còn {conLai:N0} đ, đề xuất cần {can:N0} đ.";
        if (chan && thamSo.BatTat(TS.NganSachChanVuot)) throw new DomainException(msg + " (Hệ thống đang chặn duyệt vượt dự toán — đề nghị điều chỉnh ngân sách trước.)");
        return msg;
    }

    public async Task ThemChiAsync(int khoanMucId, ChiNganSach c, string nguoi)
    {
        var k = await db.KhoanMucNganSachs.FindAsync(khoanMucId) ?? throw new DomainException("Không tìm thấy khoản mục.");
        if (!k.DungDuoc) throw new DomainException("Chỉ ghi chi cho khoản mục đã duyệt, chưa khoá sổ.");
        if (c.SoTien <= 0) throw new DomainException("Số tiền phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(c.NoiDung)) throw new DomainException("Nhập nội dung chi.");
        if (c.Ngay.Year != k.Nam) throw new DomainException($"Ngày chi phải thuộc năm {k.Nam}.");
        c.KhoanMucNganSachId = k.Id; c.NguoiNhap = nguoi; c.ThoiGian = DateTime.Now;
        db.ChiNganSachs.Add(c);
        await db.SaveChangesAsync();
    }
}

/// <summary>Dòng bản tin hằng ngày: khoản mục chạm ngưỡng cảnh báo / vượt dự toán.</summary>
public static class NganSachTongHop
{
    public static async Task<IEnumerable<string>> DongAsync(IServiceProvider sp, DateTime now, CancellationToken ct)
    {
        var svc = (NganSachService)sp.GetService(typeof(NganSachService))!;
        var ts = (ThamSoService)sp.GetService(typeof(ThamSoService))!;
        var nguong = ts.SoNguyen(TS.NganSachNguongCanhBao);
        var ds = (await svc.SuDungAsync(now.Year)).Where(x => x.KhoanMuc.TrangThai == TrangThaiNganSach.DaDuyet).ToList();
        var kq = new List<string>();
        var vuot = ds.Where(x => x.TyLe >= 100).Select(x => $"{x.KhoanMuc.Ma} ({x.TyLe:0.#}%)").ToList();
        var gan = ds.Where(x => x.TyLe >= nguong && x.TyLe < 100).Select(x => $"{x.KhoanMuc.Ma} ({x.TyLe:0.#}%)").ToList();
        if (vuot.Count > 0) kq.Add($"• Ngân sách đã dùng hết / vượt: {string.Join(", ", vuot)}");
        if (gan.Count > 0) kq.Add($"• Ngân sách đã dùng ≥ {nguong}%: {string.Join(", ", gan)}");
        return kq;
    }
}
