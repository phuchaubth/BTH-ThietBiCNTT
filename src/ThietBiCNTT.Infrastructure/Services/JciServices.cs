using Microsoft.EntityFrameworkCore;
using ThietBiCNTT.Domain.Entities;
using ThietBiCNTT.Domain.Enums;
using ThietBiCNTT.Infrastructure.Data;

namespace ThietBiCNTT.Infrastructure.Services;

/// <summary>FR-JC-01, FR-JC-08: danh mục tiêu chuẩn JCI 8 mặc định (đến mức tiêu chuẩn; số ME theo bản JCI 8).</summary>
public static class DanhMucJci
{
    private static TieuChuanJci T(string chuong, string ma, int me, string tomTat, string chucNang, bool tuDong = true) =>
        new() { Chuong = chuong, Ma = ma, SoMe = me, TomTat = tomTat, ChucNang = chucNang, CoQuyTacTuDong = tuDong };

    public static IReadOnlyList<TieuChuanJci> MacDinh() =>
    [
        T("HCT", "HCT.01.00", 4, "Lãnh đạo chỉ định người đủ năng lực giám sát công nghệ thông tin y tế và các quy trình liên quan", "Người giám sát CNTT y tế (FR-HTT-12)"),
        T("HCT", "HCT.01.01", 5, "Truyền dữ liệu người bệnh qua điện tử, thiết bị di động: chất lượng, pháp luật, bảo mật, riêng tư", "Kênh truyền dữ liệu (FR-HTT-09), hướng dẫn thiết bị di động"),
        T("HCT", "HCT.01.02", 4, "Telehealth: hướng dẫn bảo vệ dữ liệu và thông tin người bệnh", "Cờ telehealth và hướng dẫn (FR-HTT-13)"),
        T("HCT", "HCT.01.03", 3, "Công cụ hỗ trợ quyết định lâm sàng / AI: lựa chọn, triển khai, giám sát, cải tiến", "Vòng đời công cụ AI (FR-HTT-13)"),
        T("HCT", "HCT.01.04", 6, "Xây dựng, duy trì và thử nghiệm chương trình ứng phó downtime có kế hoạch và đột xuất", "Kế hoạch downtime, sự kiện, diễn tập (FR-DT)"),
        T("HCT", "HCT.01.05", 3, "Quy trình, thủ tục về an ninh mạng và quản lý rủi ro mạng", "An ninh mạng (FR-AN), chương trình ứng phó, rủi ro"),
        T("MOI", "MOI.01.00", 3, "Lập kế hoạch quản lý thông tin, chọn quy trình đáp ứng nhu cầu người dùng dữ liệu", "Danh mục hệ thống thông tin (FR-HTT-01..05)"),
        T("MOI", "MOI.01.01", 6, "Bảo mật, riêng tư, toàn vẹn thông tin; kiểm soát truy cập theo vai trò", "Rà soát quyền truy cập, giám sát tuân thủ (FR-TC)"),
        T("MOI", "MOI.01.02", 5, "Bảo vệ thông tin trước mất, đánh cắp, phá huỷ, mã độc tống tiền, tấn công mạng; đánh giá rủi ro ATTT hằng năm", "Sao lưu, thử phục hồi, sự cố an ninh, đánh giá rủi ro"),
        T("MOI", "MOI.01.03", 3, "Xác định thời gian lưu giữ dữ liệu và cách huỷ", "Thời gian lưu giữ dữ liệu (FR-HTT-14)"),
        T("MOI", "MOI.01.04", 4, "Đào tạo về hệ thống thông tin, an toàn thông tin; đào tạo an ninh mạng hằng năm", "Đào tạo (FR-DT-09)"),
        T("MOI", "MOI.02.00", 4, "Quản lý văn bản, chính sách, chương trình thống nhất", "Tài liệu có phiên bản và phê duyệt"),
        T("MOI", "MOI.02.01", 4, "Lãnh đạo xem xét, phê duyệt chính sách, quy trình", "Tài liệu duyệt hai cấp"),
        T("MOI", "MOI.02.03", 3, "Phổ biến thông tin kịp thời, đúng định dạng", "Bộ downtime của khoa, thông báo downtime"),
        T("FMS", "FMS.01.01", 3, "Người đủ trình độ giám sát; chương trình được rà soát, sửa đổi ít nhất hằng năm", "Chương trình tiện ích, rà soát hằng năm (FR-HTT-15)"),
        T("FMS", "FMS.02.00", 4, "Đánh giá rủi ro hằng năm (có hệ thống tiện ích), ưu tiên, hiệu quả, báo cáo cơ quan quản trị", "Đợt đánh giá rủi ro (FR-RR-05)"),
        T("FMS", "FMS.08.00", 5, "Chương trình quản lý tiện ích (gồm hệ thống thông tin, liên lạc, dữ liệu); danh mục và chu kỳ bảo trì", "Tiện ích CNTT (FR-HTT-15)"),
        T("FMS", "FMS.08.01", 3, "Nguồn điện thay thế luôn sẵn sàng; thử ít nhất mỗi quý và lưu kết quả", "Thử nguồn dự phòng (FR-HTT-16)"),
        T("FMS", "FMS.09.00", 4, "Chương trình ứng phó khẩn cấp và thảm hoạ; đánh giá hằng năm", "Chương trình của Phòng HCQT; tham chiếu downtime", false),
        T("FMS", "FMS.10.00", 3, "Đánh giá rủi ro trước xây dựng / bảo trì ảnh hưởng chăm sóc; giám sát nhà thầu", "Quản lý thay đổi, PCRA, dự án xây dựng (FR-TD)"),
        T("GLD", "GLD.04.00", 7, "Chương trình chất lượng và an toàn người bệnh; định nghĩa sự kiện an toàn, phân tích sự cố nghiêm trọng", "Cờ an toàn người bệnh trong downtime", false),
        T("GLD", "GLD.04.01", 3, "Báo cáo chất lượng và an toàn bằng văn bản cho cơ quan quản trị ít nhất hằng quý", "Báo cáo lãnh đạo (FR-JC-04)"),
        T("GLD", "GLD.05.00", 6, "Xem xét, giám sát hợp đồng; chỉ tiêu chất lượng nhà thầu báo cáo", "Hợp đồng (SRS 4.6), FR-JC-07, FR-JC-09"),
        T("GLD", "GLD.06.00", 4, "Trưởng bộ phận đủ trình độ; đề xuất nguồn lực; tài liệu dịch vụ hiện tại và dự kiến", "Hồ sơ phạm vi dịch vụ Phòng CNTT (FR-JC-10)"),
        T("QPS", "QPS.03.04", 6, "Phân tích sâu khi có sự cố hoặc xu hướng bất lợi; báo cáo rủi ro", "Rút kinh nghiệm downtime, sự cố an ninh"),
        T("QPS", "QPS.04.00", 4, "Cải tiến có dữ liệu chứng minh hiệu quả, bền vững", "Hành động khắc phục có kiểm chứng (mục 5)"),
        T("QPS", "QPS.04.01", 6, "Bài tập giảm thiểu rủi ro chủ động ít nhất mỗi năm", "Bài tập rủi ro chủ động (FR-RR-09)"),
        T("SQE", "SQE.01.07", 7, "Đào tạo liên tục; nhu cầu từ dữ liệu; ghi hoàn thành vào hồ sơ từng người", "Đào tạo (FR-DT-09)"),
    ];

    /// <summary>Thêm các tiêu chuẩn còn thiếu; không ghi đè ghi chú của Phòng QLCL.</summary>
    public static async Task DongBoAsync(AppDbContext db)
    {
        var co = (await db.TieuChuanJcis.Select(t => t.Ma).ToListAsync()).ToHashSet();
        foreach (var t in MacDinh().Where(t => !co.Contains(t.Ma))) db.TieuChuanJcis.Add(t);
        // Cập nhật phần do hệ thống quản lý (chức năng, số ME, cờ tự động) nhưng giữ GhiChu
        foreach (var t in await db.TieuChuanJcis.ToListAsync())
            if (MacDinh().FirstOrDefault(x => x.Ma == t.Ma) is { } m) { t.ChucNang = m.ChucNang; t.SoMe = m.SoMe; t.CoQuyTacTuDong = m.CoQuyTacTuDong; if (string.IsNullOrWhiteSpace(t.TomTat)) t.TomTat = m.TomTat; }
        await db.SaveChangesAsync();
    }
}

/// <summary>FR-JC-02: tự đánh giá bằng chứng cho từng tiêu chuẩn, tính từ dữ liệu của hệ thống.</summary>
public class BangChungJciService(AppDbContext db, ThamSoService thamSo, HaTangService haTang)
{
    private static KetQuaBangChung R(MucBangChung m, string s) => new(m, s);
    private static KetQuaBangChung Tong(int dat, int tong, string chiTiet) =>
        tong == 0 ? R(MucBangChung.KhongApDung, "Không có đối tượng áp dụng") : R(dat == tong ? MucBangChung.Du : dat == 0 ? MucBangChung.Thieu : MucBangChung.MotPhan, chiTiet);
    private static readonly KetQuaBangChung ChuaTuDong = R(MucBangChung.ChuaTuDong, "Chưa có quy tắc tự động — người phụ trách tự đánh giá");

    public async Task<Dictionary<string, KetQuaBangChung>> TinhTatCaAsync(DateTime now)
    {
        var d = DateOnly.FromDateTime(now); var kq = new Dictionary<string, KetQuaBangChung>();
        var hts = await db.HeThongThongTins.AsNoTracking().Include(h => h.KhoaPhongs).Where(h => h.TrangThai == TrangThaiHeThong.VanHanh).ToListAsync();
        var trongYeu = hts.Where(h => h.LaTrongYeu).ToList();
        var taiLieu = await db.TaiLieuQuyDinhs.AsNoTracking().Where(t => t.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync();
        bool CoTl(LoaiTaiLieu l, int? htId = null) => taiLieu.Any(t => t.Loai == l && (htId is null || t.HeThongId == null || t.HeThongId == htId));
        var thang12 = now.AddMonths(-12);
        var keHoach = await db.KeHoachDowntimes.AsNoTracking().Where(k => k.TrangThai == TrangThaiKeHoach.HieuLuc).ToListAsync();
        var dienTap = await db.DotDienTaps.AsNoTracking().Where(x => x.DaChot).ToListAsync();
        var gs = await db.NguoiGiamSatCntts.AsNoTracking().ToListAsync();
        var phucHoi = await db.LanThuPhucHois.AsNoTracking().Where(l => l.DaChot && l.Dat == true).ToListAsync();
        var tacVu = await db.TacVuSaoLuus.AsNoTracking().Where(t => t.DangApDung).ToListAsync();

        // --- HCT ---
        kq["HCT.01.00"] = gs.Any(x => x.HieuLuc(d)) ? R(MucBangChung.Du, "Có người được chỉ định còn hiệu lực") : R(MucBangChung.Thieu, "Chưa có người giám sát CNTT y tế được chỉ định còn hiệu lực");
        var kenhs = await db.KenhTruyenDuLieus.AsNoTracking().Where(k => k.DangSuDung).ToListAsync(); var coHdDd = CoTl(LoaiTaiLieu.HuongDanThietBiDiDong);
        var kenhDat = kenhs.Count(k => k.ThieuYeuCau(coHdDd).Count == 0);
        kq["HCT.01.01"] = kenhs.Count == 0 ? R(MucBangChung.Thieu, "Chưa đăng ký kênh truyền dữ liệu nào") : !coHdDd ? R(MucBangChung.MotPhan, $"{kenhDat}/{kenhs.Count} kênh đạt checklist; chưa có hướng dẫn thiết bị di động hiệu lực")
            : Tong(kenhDat, kenhs.Count, $"{kenhDat}/{kenhs.Count} kênh đạt checklist ME");
        var tele = hts.Where(h => h.LaTelehealth).ToList();
        kq["HCT.01.02"] = Tong(tele.Count(h => CoTl(LoaiTaiLieu.HuongDanTelehealth, h.Id)), tele.Count, $"{tele.Count(h => CoTl(LoaiTaiLieu.HuongDanTelehealth, h.Id))}/{tele.Count} hệ thống telehealth có hướng dẫn bảo vệ dữ liệu hiệu lực");
        var ai = hts.Where(h => h.LaCongCuAi).ToList(); var buoc = await db.BuocVongDois.AsNoTracking().ToListAsync();
        bool AiDu(HeThongThongTin h) => CoTl(LoaiTaiLieu.HuongDanAi, h.Id) && buoc.Any(b => b.HeThongId == h.Id && b.Buoc == BuocVongDoiAi.LuaChon) && buoc.Any(b => b.HeThongId == h.Id && b.Buoc == BuocVongDoiAi.TrienKhai)
            && buoc.Any(b => b.HeThongId == h.Id && b.Buoc == BuocVongDoiAi.GiamSat) && !(h.NgayXemXetAi is { } x && x < d);
        kq["HCT.01.03"] = Tong(ai.Count(AiDu), ai.Count, $"{ai.Count(AiDu)}/{ai.Count} công cụ AI đủ hướng dẫn, các bước lựa chọn–triển khai–giám sát và còn trong hạn xem xét");
        // HCT.01.04: kế hoạch hiệu lực + diễn tập trong 12 tháng (hoặc downtime thực tế đã debrief thay thế)
        int coKh = trongYeu.Count(h => keHoach.Any(k => k.HeThongId == h.Id)), daThu = trongYeu.Count(h => dienTap.Any(x => x.HeThongId == h.Id && x.LoaiThuNghiem == LoaiThuNghiem.Downtime && x.KetLuan != KetLuanDienTap.KhongDat && (x.NgayChot ?? DateTime.MinValue) >= thang12));
        kq["HCT.01.04"] = trongYeu.Count == 0 ? R(MucBangChung.KhongApDung, "Không có hệ thống trọng yếu") : coKh == 0 ? R(MucBangChung.Thieu, $"0/{trongYeu.Count} hệ thống trọng yếu có kế hoạch downtime hiệu lực")
            : R(coKh == trongYeu.Count && daThu == trongYeu.Count ? MucBangChung.Du : MucBangChung.MotPhan, $"Kế hoạch hiệu lực {coKh}/{trongYeu.Count}; đã diễn tập đạt trong 12 tháng {daThu}/{trongYeu.Count}");
        var thangUng = thamSo.SoNguyen(TS.JciThuUngPhoMangThang);
        bool coCs = CoTl(LoaiTaiLieu.ChinhSachAnNinhMang), coUp = CoTl(LoaiTaiLieu.ChuongTrinhUngPhoMang);
        bool daThuMang = dienTap.Any(x => x.LoaiThuNghiem == LoaiThuNghiem.UngPhoTanCong && (x.NgayChot ?? DateTime.MinValue) >= now.AddMonths(-thangUng));
        var diemAn = (coCs ? 1 : 0) + (coUp ? 1 : 0) + (daThuMang ? 1 : 0);
        kq["HCT.01.05"] = R(diemAn == 3 ? MucBangChung.Du : diemAn == 0 ? MucBangChung.Thieu : MucBangChung.MotPhan, $"Chính sách an ninh mạng {(coCs ? "✓" : "✗")}; chương trình ứng phó {(coUp ? "✓" : "✗")}; đã thử trong {thangUng} tháng {(daThuMang ? "✓" : "✗")}");
        // --- MOI ---
        kq["MOI.01.00"] = Tong(hts.Count(h => h.HoSoThieu().Count == 0), hts.Count, $"{hts.Count(h => h.HoSoThieu().Count == 0)}/{hts.Count} hệ thống có chủ sở hữu và người phụ trách");
        var tqt = thamSo.SoNguyen(TS.JciRaSoatQuyenToiQuanTrongThang); var cao = thamSo.SoNguyen(TS.JciRaSoatQuyenCaoThang);
        var dotQ = await db.DotRaSoatQuyens.AsNoTracking().Where(x => x.TrangThai == TrangThaiRaSoat.DaDong).ToListAsync();
        int qOk = trongYeu.Count(h => { var ck = DotRaSoatQuyen.ChuKyThang(h.MucTrongYeu, tqt, cao); return ck > 0 && dotQ.Any(x => x.HeThongId == h.Id && (x.NgayDong ?? DateTime.MinValue) >= now.AddMonths(-ck)); });
        var viPhamMo = await db.ViPhamTruyCaps.AsNoTracking().CountAsync(v => v.TrangThai != TrangThaiViPham.DaDong);
        kq["MOI.01.01"] = trongYeu.Count == 0 ? R(MucBangChung.KhongApDung, "Không có hệ thống trọng yếu") : R(qOk == trongYeu.Count && viPhamMo == 0 ? MucBangChung.Du : qOk == 0 ? MucBangChung.Thieu : MucBangChung.MotPhan, $"Rà soát quyền đúng chu kỳ {qOk}/{trongYeu.Count} hệ thống trọng yếu; {viPhamMo} vi phạm truy cập chưa đóng");
        var dotRr = await db.DotDanhGiaRuiRos.AsNoTracking().Where(x => x.DaChot).OrderByDescending(x => x.NgayChot).ToListAsync();
        bool rrNam = dotRr.Any(x => (x.NgayChot ?? DateTime.MinValue) >= thang12);
        int coBk = trongYeu.Count(h => tacVu.Any(t => t.HeThongId == h.Id));
        int phOk = trongYeu.Count(h => { var ck = thamSo.MucTieu(h.MucTrongYeu).ChuKyThuPhucHoiThang; return ck <= 0 || phucHoi.Any(l => l.HeThongId == h.Id && (l.NgayChot ?? DateTime.MinValue) >= now.AddMonths(-ck)); });
        var thanhPhan02 = new[] { trongYeu.Count == 0 || coBk == trongYeu.Count, trongYeu.Count == 0 || phOk == trongYeu.Count, rrNam, coCs };
        kq["MOI.01.02"] = R(thanhPhan02.All(x => x) ? MucBangChung.Du : thanhPhan02.Any(x => x) ? MucBangChung.MotPhan : MucBangChung.Thieu,
            $"Sao lưu khai báo {coBk}/{trongYeu.Count}; thử phục hồi Đạt trong chu kỳ {phOk}/{trongYeu.Count}; đánh giá rủi ro ATTT năm {(rrNam ? "✓" : "✗")}; chính sách khi bị tấn công {(coCs ? "✓" : "✗")}");
        int coLuu = trongYeu.Count(h => h.LuuGiuNam is not null && !string.IsNullOrWhiteSpace(h.CanCuLuuGiu) && !string.IsNullOrWhiteSpace(h.CachHuyDuLieu));
        kq["MOI.01.03"] = Tong(coLuu, trongYeu.Count, $"{coLuu}/{trongYeu.Count} hệ thống trọng yếu khai báo thời gian lưu giữ, căn cứ pháp lý và cách huỷ dữ liệu");
        var thangDt = thamSo.SoNguyen(TS.JciDaoTaoTrongThang);
        var attt = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).Where(b => b.Loai == LoaiDaoTao.AnToanThongTin && b.Ngay >= now.AddMonths(-thangDt)).ToListAsync();
        kq["MOI.01.04"] = attt.Count == 0 ? R(MucBangChung.Thieu, $"Chưa có đào tạo an toàn thông tin / an ninh mạng trong {thangDt} tháng") : R(attt.Sum(b => b.ThamDus.Count) > 0 ? MucBangChung.Du : MucBangChung.MotPhan, $"{attt.Count} buổi đào tạo ATTT, {attt.Sum(b => b.ThamDus.Count)} lượt tham dự");
        var cacLoai = new[] { LoaiTaiLieu.ChinhSachAnNinhMang, LoaiTaiLieu.ChuongTrinhUngPhoMang, LoaiTaiLieu.ChuongTrinhTienIch, LoaiTaiLieu.HuongDanThietBiDiDong };
        var coLoai = cacLoai.Count(l => CoTl(l)); var quaHanRs = taiLieu.Count(t => t.DenHanRaSoat(d));
        kq["MOI.02.00"] = kq["MOI.02.01"] = R(coLoai == cacLoai.Length && quaHanRs == 0 ? MucBangChung.Du : coLoai == 0 ? MucBangChung.Thieu : MucBangChung.MotPhan, $"{coLoai}/{cacLoai.Length} loại tài liệu bắt buộc có bản hiệu lực; {quaHanRs} tài liệu quá hạn rà soát");
        kq["MOI.02.03"] = Tong(coKh, trongYeu.Count, $"{coKh}/{trongYeu.Count} hệ thống trọng yếu có kế hoạch downtime để lập bộ downtime cho khoa");
        // --- FMS ---
        var ctTi = taiLieu.FirstOrDefault(t => t.Loai == LoaiTaiLieu.ChuongTrinhTienIch);
        kq["FMS.01.01"] = ctTi is null ? R(MucBangChung.Thieu, "Chưa có chương trình quản lý tiện ích hiệu lực") : R(ctTi.DenHanRaSoat(d) ? MucBangChung.MotPhan : MucBangChung.Du, ctTi.DenHanRaSoat(d) ? "Chương trình quá hạn rà soát" : "Chương trình hiệu lực, còn trong hạn rà soát");
        var dotGanNhat = dotRr.FirstOrDefault(x => (x.NgayChot ?? DateTime.MinValue) >= thang12);
        kq["FMS.02.00"] = dotGanNhat is null ? R(MucBangChung.Thieu, "Chưa có đợt đánh giá rủi ro đã chốt trong 12 tháng") : R(dotGanNhat.NguoiDuyet is not null && dotGanNhat.NgayGuiCoQuanQuanTri is not null ? MucBangChung.Du : MucBangChung.MotPhan,
            $"Đợt {dotGanNhat.Nam} đã chốt; Lãnh đạo duyệt {(dotGanNhat.NguoiDuyet is not null ? "✓" : "✗")}; gửi cơ quan quản trị {(dotGanNhat.NgayGuiCoQuanQuanTri is not null ? "✓" : "✗")}");
        var canh = await haTang.CanhAsync(); var tsIds = canh.Where(c => c.TaiSanId is not null).Select(c => c.TaiSanId!.Value).Distinct().ToList();
        var taiSans = await db.TaiSans.AsNoTracking().Where(t => tsIds.Contains(t.Id) && t.TrangThai != TrangThaiTaiSan.DaThanhLy).ToListAsync();
        var kh = await db.KeHoachBaoTris.AsNoTracking().Where(k => k.DangApDung).ToListAsync();
        int coPm = taiSans.Count(t => kh.Any(k => k.TaiSanId == t.Id || (k.TaiSanId == null && k.LoaiThietBiId == t.LoaiThietBiId)));
        kq["FMS.08.00"] = ctTi is null ? R(MucBangChung.Thieu, "Chưa có chương trình quản lý tiện ích hiệu lực") : R(coPm == taiSans.Count ? MucBangChung.Du : MucBangChung.MotPhan, $"Chương trình hiệu lực; {coPm}/{taiSans.Count} thành phần trong danh mục có kế hoạch bảo trì");
        var thangNg = thamSo.SoNguyen(TS.JciThuNguonDuPhongThang);
        var maus = await db.MauKiemTras.AsNoTracking().Where(m => m.LaThuNguonDuPhong && m.DangApDung).ToListAsync();
        var lanCuoi = (await db.LanKiemTras.AsNoTracking().Where(l => maus.Select(m => m.Id).Contains(l.MauKiemTraId)).ToListAsync()).GroupBy(l => l.MauKiemTraId).ToDictionary(g => g.Key, g => g.Max(x => x.ThoiGian));
        int dungHan = maus.Count(m => lanCuoi.TryGetValue(m.Id, out var t) && t >= now.AddMonths(-thangNg));
        kq["FMS.08.01"] = maus.Count == 0 ? R(MucBangChung.Thieu, "Chưa có phiếu thử nguồn điện dự phòng") : Tong(dungHan, maus.Count, $"{dungHan}/{maus.Count} nguồn dự phòng đã thử trong {thangNg} tháng gần nhất");
        kq["FMS.09.00"] = ChuaTuDong;
        var tdCs = await db.PhieuThayDois.AsNoTracking().Include(x => x.MucPcras).Where(x => x.AnhHuongChamSoc && x.NgayTao >= thang12).ToListAsync();
        var duAn = await db.DuAnXayDungs.AsNoTracking().Where(x => !x.DaHoanThanh).ToListAsync();
        int pcraDu = tdCs.Count(x => x.ThieuDieuKien().Count == 0), duAnThieu = duAn.Count(x => x.ThieuThongTin);
        kq["FMS.10.00"] = tdCs.Count == 0 && duAn.Count == 0 ? R(MucBangChung.KhongApDung, "Không có thay đổi ảnh hưởng chăm sóc / dự án xây dựng trong 12 tháng")
            : R(pcraDu == tdCs.Count && duAnThieu == 0 ? MucBangChung.Du : MucBangChung.MotPhan, $"PCRA đầy đủ {pcraDu}/{tdCs.Count} thay đổi ảnh hưởng chăm sóc; {duAn.Count - duAnThieu}/{duAn.Count} dự án xây dựng có đánh giá và biện pháp bảo vệ hạ tầng");
        // --- GLD ---
        kq["GLD.04.00"] = ChuaTuDong;
        var bc = await db.BaoCaoLanhDaos.AsNoTracking().OrderByDescending(b => b.TuNgay).ToListAsync();
        var quyTruoc = BaoCaoLanhDao.TenKy(d.AddMonths(-3)); var bcQ = bc.FirstOrDefault(b => b.Ky == quyTruoc);
        kq["GLD.04.01"] = bcQ is null ? R(MucBangChung.Thieu, $"Chưa có báo cáo quý {quyTruoc}") : !bcQ.DaGui ? R(MucBangChung.MotPhan, $"Báo cáo quý {quyTruoc} chưa gửi") : R(bcQ.DaXem ? MucBangChung.Du : MucBangChung.MotPhan, bcQ.DaXem ? $"Báo cáo quý {quyTruoc} đã gửi và được Lãnh đạo xác nhận" : $"Báo cáo quý {quyTruoc} đã gửi, chờ Lãnh đạo xác nhận đã xem");
        var hd = await db.HopDongs.AsNoTracking().Where(h => (h.Loai == LoaiHopDong.BaoTri || h.Loai == LoaiHopDong.ThueNgoai) && (h.HieuLucDen == null || h.HieuLucDen >= d)).ToListAsync();
        int coPhamVi = hd.Count(h => !string.IsNullOrWhiteSpace(h.PhamVi));
        var dgNam = await db.DanhGiaNhaCungCaps.AsNoTracking().Where(x => x.Nam == now.Year && x.NgayDanhGia != null).Select(x => x.HopDongId).ToListAsync();
        int duHs = hd.Count(h => h.ThieuGld05().Count == 0), daDg = hd.Count(h => dgNam.Contains(h.Id));
        kq["GLD.05.00"] = hd.Count > 0 ? R(duHs == hd.Count && daDg == hd.Count ? MucBangChung.Du : duHs == 0 && daDg == 0 ? MucBangChung.Thieu : MucBangChung.MotPhan, $"{duHs}/{hd.Count} hợp đồng dịch vụ CNTT đủ hồ sơ GLD.05.00; {daDg}/{hd.Count} đã đánh giá nhà cung cấp năm {now.Year}") : hd.Count == 0 ? R(MucBangChung.Thieu, "Chưa có hợp đồng bảo trì/thuê ngoài nào") : R(coPhamVi == hd.Count ? MucBangChung.MotPhan : MucBangChung.Thieu, $"{coPhamVi}/{hd.Count} hợp đồng có mô tả phạm vi; chỉ tiêu chất lượng nhà thầu báo cáo (ME5–ME6) chưa theo dõi trong hệ thống");
        var pvDv = taiLieu.Any(t => t.Loai == LoaiTaiLieu.PhamViDichVu); var coGs = gs.Any(x => x.HieuLuc(d));
        kq["GLD.06.00"] = R(pvDv && coGs ? MucBangChung.Du : pvDv || coGs ? MucBangChung.MotPhan : MucBangChung.Thieu, $"Người phụ trách được chỉ định {(coGs ? "✓" : "✗")}; hồ sơ phạm vi dịch vụ hiệu lực {(pvDv ? "✓" : "✗")}");
        // --- QPS ---
        var suKien = await db.SuKienDowntimes.AsNoTracking().Where(s => s.BatDau >= thang12 && (s.TrangThai == TrangThaiSuKien.DaKhoiPhuc || s.TrangThai == TrangThaiSuKien.DaDong)).ToListAsync();
        var canRkn = suKien.Where(s => s.BatBuocRutKinhNghiem(now)).ToList();
        kq["QPS.03.04"] = Tong(canRkn.Count(s => s.DaRutKinhNghiem), canRkn.Count, $"{canRkn.Count(s => s.DaRutKinhNghiem)}/{canRkn.Count} sự kiện downtime bắt buộc đã rút kinh nghiệm");
        var capa = await db.HanhDongKhacPhucs.AsNoTracking().ToListAsync();
        var quaHanCapa = capa.Count(h => h.QuaHan(d)); var dong = capa.Count(h => h.TrangThai == TrangThaiHanhDong.DaDong);
        kq["QPS.04.00"] = capa.Count == 0 ? R(MucBangChung.KhongApDung, "Chưa có hành động khắc phục nào") : R(quaHanCapa == 0 ? MucBangChung.Du : MucBangChung.MotPhan, $"{dong}/{capa.Count} hành động đã đóng sau kiểm chứng; {quaHanCapa} quá hạn");
        var btcd = await db.BaiTapChuDongs.AsNoTracking().Where(x => x.DaChot && x.NgayChot >= thang12).CountAsync();
        kq["QPS.04.01"] = btcd > 0 ? R(MucBangChung.Du, $"{btcd} bài tập giảm thiểu rủi ro chủ động đã chốt trong 12 tháng") : R(MucBangChung.Thieu, "Chưa có bài tập giảm thiểu rủi ro chủ động trong 12 tháng");
        // --- SQE ---
        var theoNv = await db.ThamDuDaoTaos.AsNoTracking().CountAsync(t => t.MaNhanVien != null); var tong = await db.ThamDuDaoTaos.AsNoTracking().CountAsync();
        kq["SQE.01.07"] = tong == 0 ? R(MucBangChung.Thieu, "Chưa ghi nhận người tham dự đào tạo nào") : R(theoNv == tong ? MucBangChung.Du : MucBangChung.MotPhan, $"{theoNv}/{tong} lượt tham dự có mã nhân viên để lưu hồ sơ nhân sự");
        return kq;
    }
}

public record ChiSoJci(string Nhom, string Ten, string GiaTri, string MucTieu, bool? Dat, string? GhiChu = null);

/// <summary>FR-JC-03: bộ chỉ số hạ tầng CNTT (bảng 4.8.2).</summary>
public class ChiSoJciService(AppDbContext db, ThamSoService thamSo, HaTangService haTang)
{
    public async Task<List<ChiSoJci>> TinhAsync(DateOnly tu, DateOnly den, DateTime now)
    {
        var ds = new List<ChiSoJci>(); var t0 = tu.ToDateTime(TimeOnly.MinValue); var t1 = den.AddDays(1).ToDateTime(TimeOnly.MinValue); var kyDen = t1 > now ? now : t1; var dTu = tu; var dDen = den;
        var hts = await db.HeThongThongTins.AsNoTracking().Include(h => h.KhoaPhongs).Where(h => h.TrangThai == TrangThaiHeThong.VanHanh).ToListAsync();
        var tqt = hts.Where(h => h.MucTrongYeu == MucTrongYeu.ToiQuanTrong).ToList(); var trongYeu = hts.Where(h => h.LaTrongYeu).ToList();
        var skAll = await db.SuKienDowntimes.AsNoTracking().Include(s => s.HeThongs).Where(s => s.BatDau != null && s.TrangThai != TrangThaiSuKien.DaHuy && (s.KetThuc == null || s.KetThuc >= t0) && s.BatDau < t1).ToListAsync();
        // 1. Khả dụng từng hệ thống Tối quan trọng (chỉ downtime đột xuất và phần vượt khung có kế hoạch — BR-DT-05)
        foreach (var h in tqt)
        {
            var khoang = skAll.Where(s => s.HeThongs.Any(x => x.HeThongId == h.Id)).Select(s => KhaDung.Khoang(s, now, true)).Where(x => x is not null).Select(x => x!.Value);
            var (pct, ngung) = KhaDung.Tinh(t0, kyDen, khoang);
            ds.Add(new ChiSoJci("HCT.01.04", $"Khả dụng {h.Ma}", pct.ToString("0.###") + "%", "≥ 99,5%", pct >= 99.5, ngung == TimeSpan.Zero ? "không có downtime đột xuất" : $"ngừng {(int)ngung.TotalMinutes} phút"));
        }
        // 2. Downtime đột xuất vượt RTO
        var vuot = skAll.Count(s => s.Loai == LoaiDowntime.DotXuat && s.BatDau >= t0 && s.VuotRto(now));
        ds.Add(new ChiSoJci("HCT.01.04", "Số downtime đột xuất vượt RTO", vuot.ToString(), "0", vuot == 0, $"{skAll.Count(s => s.Loai == LoaiDowntime.DotXuat && s.BatDau >= t0)} sự kiện đột xuất trong kỳ"));
        // 3. Tỷ lệ sao lưu thành công (BR-SL-01)
        int moc = 0, ok = 0;
        foreach (var tv in await db.TacVuSaoLuus.AsNoTracking().Include(t => t.HeThong).Where(t => t.DangApDung && t.HeThong!.TrangThai == TrangThaiHeThong.VanHanh).ToListAsync())
        {
            var m = (await haTang.MocSaoLuuAsync(tv, t0, kyDen, now)).Where(x => x.TrangThai != TrangThaiMocSaoLuu.ChuaDenHan).ToList();
            moc += m.Count; ok += m.Count(x => x.TrangThai is TrangThaiMocSaoLuu.ThanhCong or TrangThaiMocSaoLuu.CanhBao);
        }
        ds.Add(new ChiSoJci("MOI.01.02", "Tỷ lệ sao lưu thành công", moc == 0 ? "—" : (ok * 100.0 / moc).ToString("0.#") + "%", "≥ 98%", moc == 0 ? null : ok * 100.0 / moc >= 98, $"{ok}/{moc} mốc theo lịch"));
        // 4. Hệ thống trọng yếu có thử phục hồi Đạt trong chu kỳ
        var ph = await db.LanThuPhucHois.AsNoTracking().Where(l => l.DaChot && l.Dat == true).ToListAsync();
        int phDat = trongYeu.Count(h => { var ck = thamSo.MucTieu(h.MucTrongYeu).ChuKyThuPhucHoiThang; return ck <= 0 || ph.Any(l => l.HeThongId == h.Id && (l.NgayChot ?? DateTime.MinValue) >= kyDen.AddMonths(-ck)); });
        ds.Add(new ChiSoJci("MOI.01.02", "Hệ thống trọng yếu có thử phục hồi Đạt trong chu kỳ", trongYeu.Count == 0 ? "—" : $"{phDat}/{trongYeu.Count}", "100%", trongYeu.Count == 0 ? null : phDat == trongYeu.Count));
        // 5. Diễn tập đúng hạn (đợt có hạn trong kỳ)
        var dotTrongKy = await db.DotDienTaps.AsNoTracking().Where(d => d.HanThucHien >= dTu && d.HanThucHien <= dDen).ToListAsync();
        int dtDung = dotTrongKy.Count(d => d.DaChot && DateOnly.FromDateTime(d.NgayChot!.Value) <= d.HanThucHien);
        ds.Add(new ChiSoJci("HCT.01.04", "Diễn tập / thử nghiệm hoàn thành đúng hạn", dotTrongKy.Count == 0 ? "—" : $"{dtDung}/{dotTrongKy.Count}", "100%", dotTrongKy.Count == 0 ? null : dtDung == dotTrongKy.Count, dotTrongKy.Count == 0 ? "không có đợt đến hạn trong kỳ" : null));
        // 6. Khoa phụ thuộc hệ thống Tối quan trọng được đào tạo downtime
        var thangDt = thamSo.SoNguyen(TS.JciDaoTaoTrongThang);
        var buoi = await db.BuoiDaoTaos.AsNoTracking().Include(b => b.ThamDus).Where(b => b.Ngay >= kyDen.AddMonths(-thangDt) && b.Ngay < t1 && b.Loai == LoaiDaoTao.Downtime).ToListAsync();
        var cap = tqt.SelectMany(h => h.KhoaPhongs.Where(k => k.BatBuoc).Select(k => (h.Id, k.KhoaPhongId))).ToList();
        int capDat = cap.Count(c => buoi.Any(b => (b.HeThongId == null || b.HeThongId == c.Id) && (b.KhoaPhongId == c.KhoaPhongId || b.ThamDus.Any(t => t.KhoaPhongId == c.KhoaPhongId))));
        ds.Add(new ChiSoJci("MOI.01.04", "Khoa phụ thuộc HTTT Tối quan trọng đã được đào tạo downtime", cap.Count == 0 ? "—" : $"{capDat}/{cap.Count} ({capDat * 100 / cap.Count}%)", "≥ 90%", cap.Count == 0 ? null : capDat * 100.0 / cap.Count >= 90));
        // 7. Rủi ro Cao / Rất cao đang mở
        var cao = await db.RuiRos.AsNoTracking().Where(r => r.TrangThai == TrangThaiRuiRo.Mo || r.TrangThai == TrangThaiRuiRo.DangXuLy).ToListAsync();
        var nCao = cao.Count(r => r.Muc >= MucRuiRo.Cao);
        ds.Add(new ChiSoJci("FMS.02.00", "Rủi ro Cao / Rất cao đang mở", nCao.ToString(), "giảm theo quý", null, $"tổng {cao.Count} rủi ro đang mở"));
        // 8. CAPA đúng hạn
        var capa = await db.HanhDongKhacPhucs.AsNoTracking().Where(h => h.Han >= dTu && h.Han <= dDen).ToListAsync();
        int capaDung = capa.Count(h => h.TrangThai == TrangThaiHanhDong.DaDong && h.NgayHoanThanh is { } n && DateOnly.FromDateTime(n) <= h.Han);
        ds.Add(new ChiSoJci("QPS.04.00", "Hành động khắc phục hoàn thành đúng hạn", capa.Count == 0 ? "—" : $"{capaDung}/{capa.Count} ({capaDung * 100 / capa.Count}%)", "≥ 90%", capa.Count == 0 ? null : capaDung * 100.0 / capa.Count >= 90));
        // 9–11. Chưa có phân hệ
        foreach (var (ten, mt) in new[] { ("Thay đổi thất bại / quay lui", "≤ 5%"), ("Rà soát quyền truy cập đúng hạn", "100%"), ("Thời gian thu hồi tài khoản nghỉ việc (trung vị)", "≤ 1 ngày làm việc") })
            ds.Add(new ChiSoJci("GĐ3.3", ten, "chưa có phân hệ", mt, null));
        // 12. Hoàn thành kiểm tra phòng máy chủ
        var mau = await db.MauKiemTras.AsNoTracking().Where(m => m.DangApDung && m.ChuKyGio <= 24 && m.ViTri != null && m.ViTri.Contains("Phòng máy chủ")).ToListAsync();
        if (mau.Count > 0)
        {
            var ngay = (den.DayNumber - tu.DayNumber + 1);
            var lan = await db.LanKiemTras.AsNoTracking().Where(l => mau.Select(m => m.Id).Contains(l.MauKiemTraId) && l.ThoiGian >= t0 && l.ThoiGian < t1).Select(l => l.ThoiGian.Date).Distinct().CountAsync();
            var can = Math.Min(ngay, Math.Max(1, (int)Math.Ceiling((kyDen - t0).TotalDays)));
            ds.Add(new ChiSoJci("FMS.08.00", "Kiểm tra phòng máy chủ hằng ngày đã thực hiện", $"{lan}/{can} ngày", "100%", lan >= can));
        }
        return ds;
    }
}
