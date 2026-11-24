using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD4_BaoCao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TenTepDinhKem",
                table: "TinNhanChos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TepDinhKem",
                table: "TinNhanChos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BaoCaoMaus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    CauHinh = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChiaSe = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaoCaoMaus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LichGuiBaoCaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    BaoCaoMauId = table.Column<int>(type: "int", nullable: true),
                    ChuKy = table.Column<int>(type: "int", nullable: false),
                    NguoiNhan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DangBat = table.Column<bool>(type: "bit", nullable: false),
                    KyDaGui = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LanGuiCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TepCuoi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    KetQuaCuoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichGuiBaoCaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichGuiBaoCaos_BaoCaoMaus_BaoCaoMauId",
                        column: x => x.BaoCaoMauId,
                        principalTable: "BaoCaoMaus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichGuiBaoCaos_BaoCaoMauId",
                table: "LichGuiBaoCaos",
                column: "BaoCaoMauId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichGuiBaoCaos");

            migrationBuilder.DropTable(
                name: "BaoCaoMaus");

            migrationBuilder.DropColumn(
                name: "TenTepDinhKem",
                table: "TinNhanChos");

            migrationBuilder.DropColumn(
                name: "TepDinhKem",
                table: "TinNhanChos");
        }
    }
}
