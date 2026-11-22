using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD2_HopDong_BanQuyen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NgayGuiHang",
                table: "PhieuSuaChuas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayHenTra",
                table: "PhieuSuaChuas",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayNhanVe",
                table: "PhieuSuaChuas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SoPhieuHang",
                table: "PhieuSuaChuas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HopDongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoHopDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    NgayKy = table.Column<DateOnly>(type: "date", nullable: true),
                    HieuLucTu = table.Column<DateOnly>(type: "date", nullable: true),
                    HieuLucDen = table.Column<DateOnly>(type: "date", nullable: true),
                    GiaTri = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguoiPhuTrach = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HopDongs_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BanQuyenPhanMems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NhaSanXuat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    NgayMua = table.Column<DateOnly>(type: "date", nullable: true),
                    NgayHetHan = table.Column<DateOnly>(type: "date", nullable: true),
                    KhoaKichHoat = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HopDongId = table.Column<int>(type: "int", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BanQuyenPhanMems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BanQuyenPhanMems_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HopDongTaiSans",
                columns: table => new
                {
                    HopDongId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HopDongTaiSans", x => new { x.HopDongId, x.TaiSanId });
                    table.ForeignKey(
                        name: "FK_HopDongTaiSans_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HopDongTaiSans_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CapPhatBanQuyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BanQuyenPhanMemId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    NgayCap = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapPhatBanQuyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapPhatBanQuyens_BanQuyenPhanMems_BanQuyenPhanMemId",
                        column: x => x.BanQuyenPhanMemId,
                        principalTable: "BanQuyenPhanMems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CapPhatBanQuyens_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BanQuyenPhanMems_HopDongId",
                table: "BanQuyenPhanMems",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_CapPhatBanQuyens_BanQuyenPhanMemId_TaiSanId",
                table: "CapPhatBanQuyens",
                columns: new[] { "BanQuyenPhanMemId", "TaiSanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CapPhatBanQuyens_TaiSanId",
                table: "CapPhatBanQuyens",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_HieuLucDen",
                table: "HopDongs",
                column: "HieuLucDen");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_NhaCungCapId",
                table: "HopDongs",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongs_SoHopDong",
                table: "HopDongs",
                column: "SoHopDong");

            migrationBuilder.CreateIndex(
                name: "IX_HopDongTaiSans_TaiSanId",
                table: "HopDongTaiSans",
                column: "TaiSanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapPhatBanQuyens");

            migrationBuilder.DropTable(
                name: "HopDongTaiSans");

            migrationBuilder.DropTable(
                name: "BanQuyenPhanMems");

            migrationBuilder.DropTable(
                name: "HopDongs");

            migrationBuilder.DropColumn(
                name: "NgayGuiHang",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "NgayHenTra",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "NgayNhanVe",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "SoPhieuHang",
                table: "PhieuSuaChuas");
        }
    }
}
