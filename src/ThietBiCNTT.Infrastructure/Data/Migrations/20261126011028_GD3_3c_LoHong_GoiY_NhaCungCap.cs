using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_3c_LoHong_GoiY_NhaCungCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CachXuLyKhongDat",
                table: "HopDongs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChiTieuNhaThau",
                table: "HopDongs",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeHoachLienTuc",
                table: "HopDongs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TanSuatBaoCao",
                table: "HopDongs",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CachHuyDuLieu",
                table: "HeThongThongTins",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CanCuLuuGiu",
                table: "HeThongThongTins",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiDuLieuLuuGiu",
                table: "HeThongThongTins",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LuuGiuNam",
                table: "HeThongThongTins",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaiTapChuDongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    QuyTrinh = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    NgayThucHien = table.Column<DateOnly>(type: "date", nullable: false),
                    DiemCoTheHong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HauQua = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BienPhap = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ThamGia = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DotDienTapId = table.Column<int>(type: "int", nullable: true),
                    DaChot = table.Column<bool>(type: "bit", nullable: false),
                    NguoiChot = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayChot = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiTapChuDongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BaiTapChuDongs_DotDienTaps_DotDienTapId",
                        column: x => x.DotDienTapId,
                        principalTable: "DotDienTaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaNhaCungCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HopDongId = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    DiemPhanHoi = table.Column<int>(type: "int", nullable: false),
                    DiemChatLuong = table.Column<int>(type: "int", nullable: false),
                    DiemTuanThu = table.Column<int>(type: "int", nullable: false),
                    DiemBaoMat = table.Column<int>(type: "int", nullable: false),
                    DuLieuThamChieu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiGiamSat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TruongBoPhan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KetLuan = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaNhaCungCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGiaNhaCungCaps_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GoiYRuiRos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Khoa = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Nhom = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    DuLieu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    RuiRoId = table.Column<int>(type: "int", nullable: true),
                    NguoiXuLy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoiYRuiRos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoiYRuiRos_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoHongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaCve = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    Cvss = table.Column<decimal>(type: "decimal(3,1)", precision: 3, scale: 1, nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    NgayPhatHien = table.Column<DateOnly>(type: "date", nullable: false),
                    HanVa = table.Column<DateOnly>(type: "date", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NguoiPhuTrach = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayXong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiXong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DaNhacQuaHan = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoHongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoHongs_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoHongs_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BaiTapChuDongs_DotDienTapId",
                table: "BaiTapChuDongs",
                column: "DotDienTapId");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaNhaCungCaps_HopDongId_Nam",
                table: "DanhGiaNhaCungCaps",
                columns: new[] { "HopDongId", "Nam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoiYRuiRos_HeThongId",
                table: "GoiYRuiRos",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_GoiYRuiRos_Khoa",
                table: "GoiYRuiRos",
                column: "Khoa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoHongs_HeThongId",
                table: "LoHongs",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_LoHongs_TaiSanId",
                table: "LoHongs",
                column: "TaiSanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BaiTapChuDongs");

            migrationBuilder.DropTable(
                name: "DanhGiaNhaCungCaps");

            migrationBuilder.DropTable(
                name: "GoiYRuiRos");

            migrationBuilder.DropTable(
                name: "LoHongs");

            migrationBuilder.DropColumn(
                name: "CachXuLyKhongDat",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "ChiTieuNhaThau",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "KeHoachLienTuc",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "TanSuatBaoCao",
                table: "HopDongs");

            migrationBuilder.DropColumn(
                name: "CachHuyDuLieu",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "CanCuLuuGiu",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "LoaiDuLieuLuuGiu",
                table: "HeThongThongTins");

            migrationBuilder.DropColumn(
                name: "LuuGiuNam",
                table: "HeThongThongTins");
        }
    }
}
