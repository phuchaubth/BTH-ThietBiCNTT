using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_HaTangCNTT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HeThongThongTins",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    MucTrongYeu = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    PhienBan = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NgaySuDung = table.Column<DateOnly>(type: "date", nullable: true),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    HopDongId = table.Column<int>(type: "int", nullable: true),
                    ChuSoHuuKhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    ChuSoHuu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhuTrachChinh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhuTrachDuPhong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HoTroNhaCungCap = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RtoPhut = table.Column<int>(type: "int", nullable: true),
                    RpoPhut = table.Column<int>(type: "int", nullable: true),
                    CapDoAttt = table.Column<int>(type: "int", nullable: true),
                    SoQdCapDo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayQdCapDo = table.Column<DateOnly>(type: "date", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeThongThongTins", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeThongThongTins_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeThongThongTins_KhoaPhongs_ChuSoHuuKhoaPhongId",
                        column: x => x.ChuSoHuuKhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HeThongThongTins_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuKienDowntimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    MucDo = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuKienBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuKienKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KichHoatKeHoach = table.Column<bool>(type: "bit", nullable: false),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: true),
                    RtoPhut = table.Column<int>(type: "int", nullable: true),
                    MucTrongYeuCaoNhat = table.Column<int>(type: "int", nullable: true),
                    DaThongBaoTruoc = table.Column<bool>(type: "bit", nullable: false),
                    DaNhacTruoc1Gio = table.Column<bool>(type: "bit", nullable: false),
                    NguyenNhanGoc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DiemTot = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanCaiThien = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CanSuaKeHoach = table.Column<bool>(type: "bit", nullable: false),
                    NgayRutKinhNghiem = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiRutKinhNghiem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuKienDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuKienDowntimes_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HanhDongKhacPhucs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    NguonId = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    NguoiChiuTrachNhiem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Han = table.Column<DateOnly>(type: "date", nullable: false),
                    UuTien = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    KetQuaThucHien = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BangChungHieuQua = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguoiKiemChung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayKiemChung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LichSuGiaHan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    DaNhacTruocHan = table.Column<bool>(type: "bit", nullable: false),
                    CapLeoThang = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HanhDongKhacPhucs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HanhDongKhacPhucs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HeThongKhoaPhongs",
                columns: table => new
                {
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: false),
                    BatBuoc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeThongKhoaPhongs", x => new { x.HeThongId, x.KhoaPhongId });
                    table.ForeignKey(
                        name: "FK_HeThongKhoaPhongs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HeThongKhoaPhongs_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeHoachDowntimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    PhienBan = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TieuChiKichHoat = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DanhBa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    BuocBatDau = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    BuocTrongDowntime = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    BuocKhoiPhuc = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    QuyTrinhThayThe = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    QuyTrinhNhapBu = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TieuChiKetThuc = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayHieuLuc = table.Column<DateOnly>(type: "date", nullable: true),
                    NgayRaSoat = table.Column<DateOnly>(type: "date", nullable: true),
                    NguoiGuiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiDuyetCntt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetCntt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDuyetLanhDao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetLanhDao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeHoachDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeHoachDowntimes_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhuThuocHeThongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    HeThongPhuThuocId = table.Column<int>(type: "int", nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoDuPhong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhuThuocHeThongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhuThuocHeThongs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhuThuocHeThongs_HeThongThongTins_HeThongPhuThuocId",
                        column: x => x.HeThongPhuThuocId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PhuThuocHeThongs_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TacVuSaoLuus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DoiTuong = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    ChuKyGio = table.Column<int>(type: "int", nullable: false),
                    GioChay = table.Column<TimeOnly>(type: "time", nullable: false),
                    PhanMem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NoiLuu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SoBanSao = table.Column<int>(type: "int", nullable: false),
                    SoLoaiPhuongTien = table.Column<int>(type: "int", nullable: false),
                    CoBanNgoaiCoSo = table.Column<bool>(type: "bit", nullable: false),
                    LuuGiuNgay = table.Column<int>(type: "int", nullable: false),
                    MaHoa = table.Column<bool>(type: "bit", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    DangApDung = table.Column<bool>(type: "bit", nullable: false),
                    SoLanLoiDaCanhBao = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TacVuSaoLuus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TacVuSaoLuus_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TacVuSaoLuus_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyDowntimes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuKienDowntimeId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NguoiGhi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyDowntimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhatKyDowntimes_SuKienDowntimes_SuKienDowntimeId",
                        column: x => x.SuKienDowntimeId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuKienDowntimeHeThongs",
                columns: table => new
                {
                    SuKienDowntimeId = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuKienDowntimeHeThongs", x => new { x.SuKienDowntimeId, x.HeThongId });
                    table.ForeignKey(
                        name: "FK_SuKienDowntimeHeThongs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuKienDowntimeHeThongs_SuKienDowntimes_SuKienDowntimeId",
                        column: x => x.SuKienDowntimeId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SuKienDowntimeKhoas",
                columns: table => new
                {
                    SuKienDowntimeId = table.Column<int>(type: "int", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: false),
                    KichHoatGiay = table.Column<bool>(type: "bit", nullable: false),
                    DaNhapBu = table.Column<bool>(type: "bit", nullable: false),
                    NguoiXacNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThoiDiemXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuKienDowntimeKhoas", x => new { x.SuKienDowntimeId, x.KhoaPhongId });
                    table.ForeignKey(
                        name: "FK_SuKienDowntimeKhoas_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuKienDowntimeKhoas_SuKienDowntimes_SuKienDowntimeId",
                        column: x => x.SuKienDowntimeId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DotDienTaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    KeHoachDowntimeId = table.Column<int>(type: "int", nullable: true),
                    HanThucHien = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayThucHien = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HinhThuc = table.Column<int>(type: "int", nullable: false),
                    KichBan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KhoaThamGia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiQuanSat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DienBien = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    PhatHien = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KetLuan = table.Column<int>(type: "int", nullable: true),
                    ThayBangSuKienId = table.Column<int>(type: "int", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DaChot = table.Column<bool>(type: "bit", nullable: false),
                    NguoiChot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayChot = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhuLuc = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CapNhacQuaHan = table.Column<int>(type: "int", nullable: false),
                    DaNhacTruoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotDienTaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotDienTaps_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DotDienTaps_KeHoachDowntimes_KeHoachDowntimeId",
                        column: x => x.KeHoachDowntimeId,
                        principalTable: "KeHoachDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DotDienTaps_SuKienDowntimes_ThayBangSuKienId",
                        column: x => x.ThayBangSuKienId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaSaoLuuLans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TacVuSaoLuuId = table.Column<int>(type: "int", nullable: false),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KetQua = table.Column<int>(type: "int", nullable: false),
                    DungLuongGb = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThoiLuongPhut = table.Column<int>(type: "int", nullable: true),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiGhi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThoiGianGhi = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaSaoLuuLans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KetQuaSaoLuuLans_TacVuSaoLuus_TacVuSaoLuuId",
                        column: x => x.TacVuSaoLuuId,
                        principalTable: "TacVuSaoLuus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LanThuPhucHois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    HanThucHien = table.Column<DateOnly>(type: "date", nullable: false),
                    TacVuSaoLuuId = table.Column<int>(type: "int", nullable: true),
                    BanSaoLuu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ThoiDiemDuLieu = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ThoiDiemSuCo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MoiTruong = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KiemTraToanVen = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguoiXacNhanNghiepVu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToanVenDat = table.Column<bool>(type: "bit", nullable: false),
                    RtoMucTieu = table.Column<int>(type: "int", nullable: true),
                    RpoMucTieu = table.Column<int>(type: "int", nullable: true),
                    Dat = table.Column<bool>(type: "bit", nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DaChot = table.Column<bool>(type: "bit", nullable: false),
                    NguoiChot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayChot = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhuLuc = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CapNhacQuaHan = table.Column<int>(type: "int", nullable: false),
                    DaNhacTruoc = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanThuPhucHois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanThuPhucHois_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LanThuPhucHois_TacVuSaoLuus_TacVuSaoLuuId",
                        column: x => x.TacVuSaoLuuId,
                        principalTable: "TacVuSaoLuus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotDienTaps_HeThongId_DaChot",
                table: "DotDienTaps",
                columns: new[] { "HeThongId", "DaChot" });

            migrationBuilder.CreateIndex(
                name: "IX_DotDienTaps_KeHoachDowntimeId",
                table: "DotDienTaps",
                column: "KeHoachDowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDienTaps_ThayBangSuKienId",
                table: "DotDienTaps",
                column: "ThayBangSuKienId");

            migrationBuilder.CreateIndex(
                name: "IX_HanhDongKhacPhucs_HeThongId",
                table: "HanhDongKhacPhucs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_HanhDongKhacPhucs_Ma",
                table: "HanhDongKhacPhucs",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HanhDongKhacPhucs_Nguon_NguonId",
                table: "HanhDongKhacPhucs",
                columns: new[] { "Nguon", "NguonId" });

            migrationBuilder.CreateIndex(
                name: "IX_HanhDongKhacPhucs_TrangThai_Han",
                table: "HanhDongKhacPhucs",
                columns: new[] { "TrangThai", "Han" });

            migrationBuilder.CreateIndex(
                name: "IX_HeThongKhoaPhongs_KhoaPhongId",
                table: "HeThongKhoaPhongs",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_HeThongThongTins_ChuSoHuuKhoaPhongId",
                table: "HeThongThongTins",
                column: "ChuSoHuuKhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_HeThongThongTins_HopDongId",
                table: "HeThongThongTins",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_HeThongThongTins_Ma",
                table: "HeThongThongTins",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HeThongThongTins_NhaCungCapId",
                table: "HeThongThongTins",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachDowntimes_HeThongId_PhienBan",
                table: "KeHoachDowntimes",
                columns: new[] { "HeThongId", "PhienBan" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaSaoLuuLans_TacVuSaoLuuId_ThoiDiem",
                table: "KetQuaSaoLuuLans",
                columns: new[] { "TacVuSaoLuuId", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_LanThuPhucHois_HeThongId_DaChot",
                table: "LanThuPhucHois",
                columns: new[] { "HeThongId", "DaChot" });

            migrationBuilder.CreateIndex(
                name: "IX_LanThuPhucHois_TacVuSaoLuuId",
                table: "LanThuPhucHois",
                column: "TacVuSaoLuuId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyDowntimes_SuKienDowntimeId",
                table: "NhatKyDowntimes",
                column: "SuKienDowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhuThuocHeThongs_HeThongId",
                table: "PhuThuocHeThongs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhuThuocHeThongs_HeThongPhuThuocId",
                table: "PhuThuocHeThongs",
                column: "HeThongPhuThuocId");

            migrationBuilder.CreateIndex(
                name: "IX_PhuThuocHeThongs_TaiSanId",
                table: "PhuThuocHeThongs",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_SuKienDowntimeHeThongs_HeThongId",
                table: "SuKienDowntimeHeThongs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_SuKienDowntimeKhoas_KhoaPhongId",
                table: "SuKienDowntimeKhoas",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_SuKienDowntimes_Ma",
                table: "SuKienDowntimes",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuKienDowntimes_PhieuSuaChuaId",
                table: "SuKienDowntimes",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_SuKienDowntimes_TrangThai_BatDau",
                table: "SuKienDowntimes",
                columns: new[] { "TrangThai", "BatDau" });

            migrationBuilder.CreateIndex(
                name: "IX_TacVuSaoLuus_HeThongId",
                table: "TacVuSaoLuus",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_TacVuSaoLuus_TaiSanId",
                table: "TacVuSaoLuus",
                column: "TaiSanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DotDienTaps");

            migrationBuilder.DropTable(
                name: "HanhDongKhacPhucs");

            migrationBuilder.DropTable(
                name: "HeThongKhoaPhongs");

            migrationBuilder.DropTable(
                name: "KetQuaSaoLuuLans");

            migrationBuilder.DropTable(
                name: "LanThuPhucHois");

            migrationBuilder.DropTable(
                name: "NhatKyDowntimes");

            migrationBuilder.DropTable(
                name: "PhuThuocHeThongs");

            migrationBuilder.DropTable(
                name: "SuKienDowntimeHeThongs");

            migrationBuilder.DropTable(
                name: "SuKienDowntimeKhoas");

            migrationBuilder.DropTable(
                name: "KeHoachDowntimes");

            migrationBuilder.DropTable(
                name: "TacVuSaoLuus");

            migrationBuilder.DropTable(
                name: "SuKienDowntimes");

            migrationBuilder.DropTable(
                name: "HeThongThongTins");
        }
    }
}
