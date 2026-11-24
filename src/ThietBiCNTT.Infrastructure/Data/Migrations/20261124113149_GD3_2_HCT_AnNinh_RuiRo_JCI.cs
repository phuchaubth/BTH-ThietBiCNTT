using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_2_HCT_AnNinh_RuiRo_JCI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LaThuNguonDuPhong",
                table: "MauKiemTras",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LaCongCuAi",
                table: "HeThongThongTins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LaTelehealth",
                table: "HeThongThongTins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LoaiCongCuAi",
                table: "HeThongThongTins",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayXemXetAi",
                table: "HeThongThongTins",
                type: "date",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HeThongId",
                table: "DotDienTaps",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "LoaiThuNghiem",
                table: "DotDienTaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TaiLieuId",
                table: "DotDienTaps",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaoCaoLanhDaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ky = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TuNgay = table.Column<DateOnly>(type: "date", nullable: false),
                    DenNgay = table.Column<DateOnly>(type: "date", nullable: false),
                    Snapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SuCoNghiemTrong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiGui = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiXem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayXem = table.Column<DateTime>(type: "datetime2", nullable: true),
                    YKienChiDao = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SoLanNhac = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoLanhDaos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BuocVongDois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    Buoc = table.Column<int>(type: "int", nullable: false),
                    NguoiPhuTrach = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KetLuan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocVongDois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuocVongDois_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuoiDaoTaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ngay = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    ChuDe = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NguoiGiang = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuoiDaoTaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuoiDaoTaos_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BuoiDaoTaos_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DotDanhGiaRuiRos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    ChuTri = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ThanhPhan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DaChot = table.Column<bool>(type: "bit", nullable: false),
                    NgayChot = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiChot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiGuiCoQuanQuanTri = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayGuiCoQuanQuanTri = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HanhDongCuaCoQuanQuanTri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BaoCao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotDanhGiaRuiRos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KenhTruyenDuLieus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhamViDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MaHoa = table.Column<bool>(type: "bit", nullable: false),
                    DangNhapRieng = table.Column<bool>(type: "bit", nullable: false),
                    DanhBaNguoiDuocPhep = table.Column<bool>(type: "bit", nullable: false),
                    BienNhanGuiDoc = table.Column<bool>(type: "bit", nullable: false),
                    DauThoiGian = table.Column<bool>(type: "bit", nullable: false),
                    BaoVeTruyCap = table.Column<bool>(type: "bit", nullable: false),
                    ThuHoiKhiThoiViec = table.Column<bool>(type: "bit", nullable: false),
                    XoaTuXa = table.Column<bool>(type: "bit", nullable: false),
                    NhatKyThaoTac = table.Column<bool>(type: "bit", nullable: false),
                    DongYNguoiBenh = table.Column<bool>(type: "bit", nullable: false),
                    DangSuDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KenhTruyenDuLieus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KenhTruyenDuLieus_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiGiamSatCntts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ChucDanh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TaiKhoan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoVanBan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayVanBan = table.Column<DateOnly>(type: "date", nullable: false),
                    TrinhDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                    NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: true),
                    NguoiThayThe = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiGiamSatCntts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuiRos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Nhom = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    KiemSoatHienCo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    KhaNang = table.Column<int>(type: "int", nullable: false),
                    HauQua = table.Column<int>(type: "int", nullable: false),
                    KhaNangTonDu = table.Column<int>(type: "int", nullable: true),
                    HauQuaTonDu = table.Column<int>(type: "int", nullable: true),
                    PhuongAn = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NguoiChiuTrachNhiem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayXemXetLai = table.Column<DateOnly>(type: "date", nullable: true),
                    LyDoChapNhan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDuyetChapNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetChapNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuiRos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RuiRos_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RuiRos_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuCoAnNinhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    MucDo = table.Column<int>(type: "int", nullable: false),
                    LienQuanDuLieuCaNhan = table.Column<bool>(type: "bit", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    PhatHien = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    NguoiBao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiPhuTrach = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    DaBaoCaoCoQuan = table.Column<bool>(type: "bit", nullable: false),
                    NgayBaoCao = table.Column<DateOnly>(type: "date", nullable: true),
                    CoQuan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SoVanBan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LyDoKhongBaoCao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguyenNhanGoc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BaiHoc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SuKienDowntimeId = table.Column<int>(type: "int", nullable: true),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DaThongBaoLanhDao = table.Column<bool>(type: "bit", nullable: false),
                    DaNhacBaoCao = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuCoAnNinhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuCoAnNinhs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuCoAnNinhs_SuKienDowntimes_SuKienDowntimeId",
                        column: x => x.SuKienDowntimeId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuCoAnNinhs_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiLieuQuyDinhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    PhienBan = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    NgayHieuLuc = table.Column<DateOnly>(type: "date", nullable: true),
                    NgayRaSoat = table.Column<DateOnly>(type: "date", nullable: true),
                    NguoiGuiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiDuyetCntt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetCntt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDuyetLanhDao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetLanhDao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DaNhacRaSoat = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuQuyDinhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiLieuQuyDinhs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TieuChuanJcis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Chuong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TomTat = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    SoMe = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChucNang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CoQuyTacTuDong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TieuChuanJcis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThamDuDaoTaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuoiDaoTaoId = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MaNhanVien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    ChucDanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DatDanhGia = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThamDuDaoTaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThamDuDaoTaos_BuoiDaoTaos_BuoiDaoTaoId",
                        column: x => x.BuoiDaoTaoId,
                        principalTable: "BuoiDaoTaos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThamDuDaoTaos_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuDiemRuiRos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuiRoId = table.Column<int>(type: "int", nullable: false),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KhaNang = table.Column<int>(type: "int", nullable: false),
                    HauQua = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDanhGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DotDanhGiaRuiRoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuDiemRuiRos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuDiemRuiRos_RuiRos_RuiRoId",
                        column: x => x.RuiRoId,
                        principalTable: "RuiRos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BuocUngPhos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuCoAnNinhId = table.Column<int>(type: "int", nullable: false),
                    Buoc = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThoiDiem = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HoanThanh = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocUngPhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuocUngPhos_SuCoAnNinhs_SuCoAnNinhId",
                        column: x => x.SuCoAnNinhId,
                        principalTable: "SuCoAnNinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaTieuChuanJcis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuChuanJciId = table.Column<int>(type: "int", nullable: false),
                    DanhGia = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiDanhGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaTieuChuanJcis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGiaTieuChuanJcis_TieuChuanJcis_TieuChuanJciId",
                        column: x => x.TieuChuanJciId,
                        principalTable: "TieuChuanJcis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotDienTaps_TaiLieuId",
                table: "DotDienTaps",
                column: "TaiLieuId");

            migrationBuilder.CreateIndex(
                name: "IX_BaoCaoLanhDaos_Ky",
                table: "BaoCaoLanhDaos",
                column: "Ky");

            migrationBuilder.CreateIndex(
                name: "IX_BuocUngPhos_SuCoAnNinhId",
                table: "BuocUngPhos",
                column: "SuCoAnNinhId");

            migrationBuilder.CreateIndex(
                name: "IX_BuocVongDois_HeThongId_Buoc",
                table: "BuocVongDois",
                columns: new[] { "HeThongId", "Buoc" });

            migrationBuilder.CreateIndex(
                name: "IX_BuoiDaoTaos_HeThongId",
                table: "BuoiDaoTaos",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiDaoTaos_KhoaPhongId",
                table: "BuoiDaoTaos",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_BuoiDaoTaos_Ngay",
                table: "BuoiDaoTaos",
                column: "Ngay");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaTieuChuanJcis_TieuChuanJciId",
                table: "DanhGiaTieuChuanJcis",
                column: "TieuChuanJciId");

            migrationBuilder.CreateIndex(
                name: "IX_DotDanhGiaRuiRos_Nam",
                table: "DotDanhGiaRuiRos",
                column: "Nam");

            migrationBuilder.CreateIndex(
                name: "IX_KenhTruyenDuLieus_HeThongId",
                table: "KenhTruyenDuLieus",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuDiemRuiRos_RuiRoId",
                table: "LichSuDiemRuiRos",
                column: "RuiRoId");

            migrationBuilder.CreateIndex(
                name: "IX_RuiRos_HeThongId",
                table: "RuiRos",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_RuiRos_Ma",
                table: "RuiRos",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RuiRos_TaiSanId",
                table: "RuiRos",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_RuiRos_TrangThai_Nhom",
                table: "RuiRos",
                columns: new[] { "TrangThai", "Nhom" });

            migrationBuilder.CreateIndex(
                name: "IX_SuCoAnNinhs_HeThongId",
                table: "SuCoAnNinhs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_SuCoAnNinhs_Ma",
                table: "SuCoAnNinhs",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SuCoAnNinhs_SuKienDowntimeId",
                table: "SuCoAnNinhs",
                column: "SuKienDowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_SuCoAnNinhs_TaiSanId",
                table: "SuCoAnNinhs",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_SuCoAnNinhs_TrangThai_MucDo",
                table: "SuCoAnNinhs",
                columns: new[] { "TrangThai", "MucDo" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuQuyDinhs_HeThongId",
                table: "TaiLieuQuyDinhs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuQuyDinhs_Loai_TrangThai",
                table: "TaiLieuQuyDinhs",
                columns: new[] { "Loai", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuQuyDinhs_Ma",
                table: "TaiLieuQuyDinhs",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThamDuDaoTaos_BuoiDaoTaoId",
                table: "ThamDuDaoTaos",
                column: "BuoiDaoTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThamDuDaoTaos_KhoaPhongId",
                table: "ThamDuDaoTaos",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_TieuChuanJcis_Ma",
                table: "TieuChuanJcis",
                column: "Ma",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DotDienTaps_TaiLieuQuyDinhs_TaiLieuId",
                table: "DotDienTaps",
                column: "TaiLieuId",
                principalTable: "TaiLieuQuyDinhs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DotDienTaps_TaiLieuQuyDinhs_TaiLieuId",
                table: "DotDienTaps");

            migrationBuilder.DropTable(
                name: "BaoCaoLanhDaos");

            migrationBuilder.DropTable(
                name: "BuocUngPhos");

            migrationBuilder.DropTable(
                name: "BuocVongDois");

            migrationBuilder.DropTable(
                name: "DanhGiaTieuChuanJcis");

            migrationBuilder.DropTable(
                name: "DotDanhGiaRuiRos");

            migrationBuilder.DropTable(
                name: "KenhTruyenDuLieus");

            migrationBuilder.DropTable(
                name: "LichSuDiemRuiRos");

            migrationBuilder.DropTable(
                name: "NguoiGiamSatCntts");

            migrationBuilder.DropTable(
                name: "TaiLieuQuyDinhs");

            migrationBuilder.DropTable(
                name: "ThamDuDaoTaos");

            migrationBuilder.DropTable(
                name: "SuCoAnNinhs");

            migrationBuilder.DropTable(
                name: "TieuChuanJcis");

            migrationBuilder.DropTable(
                name: "RuiRos");

            migrationBuilder.DropTable(
                name: "BuoiDaoTaos");

            migrationBuilder.DropIndex(
                name: "IX_DotDienTaps_TaiLieuId",
                table: "DotDienTaps");

            migrationBuilder.DropColumn(
                name: "LaThuNguonDuPhong",
                table: "MauKiemTras");

            migrationBuilder.DropColumn(
                name: "LaCongCuAi",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "LaTelehealth",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "LoaiCongCuAi",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "NgayXemXetAi",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "LoaiThuNghiem",
                table: "DotDienTaps");

            migrationBuilder.DropColumn(
                name: "TaiLieuId",
                table: "DotDienTaps");

            migrationBuilder.AlterColumn<int>(
                name: "HeThongId",
                table: "DotDienTaps",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
