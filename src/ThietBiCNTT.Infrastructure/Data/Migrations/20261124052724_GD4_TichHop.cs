using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD4_TichHop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhoaApis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TienTo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    BamKhoa = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PhamVi = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    IpChoPhep = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HetHan = table.Column<DateOnly>(type: "date", nullable: true),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false),
                    LanCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpCuoi = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoaApis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyApis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoaApiId = table.Column<int>(type: "int", nullable: true),
                    TenKhoa = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Ip = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Method = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MaHttp = table.Column<int>(type: "int", nullable: false),
                    ThoiGianMs = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyApis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Webhooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SuKien = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    BiMat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Webhooks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookGuis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebhookId = table.Column<int>(type: "int", nullable: false),
                    SuKien = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    SoLan = table.Column<int>(type: "int", nullable: false),
                    ThoiGianTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuiTu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GuiLuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaHttp = table.Column<int>(type: "int", nullable: true),
                    LoiCuoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookGuis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebhookGuis_Webhooks_WebhookId",
                        column: x => x.WebhookId,
                        principalTable: "Webhooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KhoaApis_BamKhoa",
                table: "KhoaApis",
                column: "BamKhoa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyApis_ThoiGian",
                table: "NhatKyApis",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookGuis_TrangThai_GuiTu",
                table: "WebhookGuis",
                columns: new[] { "TrangThai", "GuiTu" });

            migrationBuilder.CreateIndex(
                name: "IX_WebhookGuis_WebhookId",
                table: "WebhookGuis",
                column: "WebhookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KhoaApis");

            migrationBuilder.DropTable(
                name: "NhatKyApis");

            migrationBuilder.DropTable(
                name: "WebhookGuis");

            migrationBuilder.DropTable(
                name: "Webhooks");
        }
    }
}
