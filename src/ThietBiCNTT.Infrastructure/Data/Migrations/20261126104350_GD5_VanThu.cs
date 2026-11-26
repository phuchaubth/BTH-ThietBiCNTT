using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD5_VanThu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MauVanBans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    TrichYeu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DangDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauVanBans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VanBans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Huong = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    SoDen = table.Column<int>(type: "int", nullable: true),
                    SoDi = table.Column<int>(type: "int", nullable: true),
                    SoKyHieu = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    NgayVanBan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayDen = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CoQuanBanHanh = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NoiNhan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TrichYeu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoKhan = table.Column<int>(type: "int", nullable: false),
                    NguoiKy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ChucVuNguoiKy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HanXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NguoiSoan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiChuTri = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiPhoiHop = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    YKienChiDao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TienDo = table.Column<int>(type: "int", nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayPhatHanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TheoDoiThucHien = table.Column<bool>(type: "bit", nullable: false),
                    VanBanLienQuanId = table.Column<int>(type: "int", nullable: true),
                    DeXuatMuaSamId = table.Column<int>(type: "int", nullable: true),
                    LuotGui = table.Column<int>(type: "int", nullable: false),
                    BuocHienTai = table.Column<int>(type: "int", nullable: false),
                    NgayNhacCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoHuy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VanBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VanBans_DeXuatMuaSams_DeXuatMuaSamId",
                        column: x => x.DeXuatMuaSamId,
                        principalTable: "DeXuatMuaSams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_VanBans_VanBans_VanBanLienQuanId",
                        column: x => x.VanBanLienQuanId,
                        principalTable: "VanBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocDuyetVanBans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VanBanId = table.Column<int>(type: "int", nullable: false),
                    LuotGui = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DongY = table.Column<bool>(type: "bit", nullable: true),
                    YKien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocDuyetVanBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuocDuyetVanBans_VanBans_VanBanId",
                        column: x => x.VanBanId,
                        principalTable: "VanBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyVanBans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VanBanId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TienDo = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyVanBans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhatKyVanBans_VanBans_VanBanId",
                        column: x => x.VanBanId,
                        principalTable: "VanBans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuocDuyetVanBans_NguoiDuyet_DongY",
                table: "BuocDuyetVanBans",
                columns: new[] { "NguoiDuyet", "DongY" });

            migrationBuilder.CreateIndex(
                name: "IX_BuocDuyetVanBans_VanBanId",
                table: "BuocDuyetVanBans",
                column: "VanBanId");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyVanBans_VanBanId",
                table: "NhatKyVanBans",
                column: "VanBanId");

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_DeXuatMuaSamId",
                table: "VanBans",
                column: "DeXuatMuaSamId");

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_Huong_TrangThai",
                table: "VanBans",
                columns: new[] { "Huong", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_Nam_SoDen",
                table: "VanBans",
                columns: new[] { "Nam", "SoDen" },
                unique: true,
                filter: "[SoDen] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_Nam_SoDi",
                table: "VanBans",
                columns: new[] { "Nam", "SoDi" },
                unique: true,
                filter: "[SoDi] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_NguoiChuTri",
                table: "VanBans",
                column: "NguoiChuTri");

            migrationBuilder.CreateIndex(
                name: "IX_VanBans_VanBanLienQuanId",
                table: "VanBans",
                column: "VanBanLienQuanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuocDuyetVanBans");

            migrationBuilder.DropTable(
                name: "MauVanBans");

            migrationBuilder.DropTable(
                name: "NhatKyVanBans");

            migrationBuilder.DropTable(
                name: "VanBans");
        }
    }
}
