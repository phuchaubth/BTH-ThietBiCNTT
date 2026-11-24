using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD4_Agent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MayTramAgents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Hostname = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: false),
                    SerialBios = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Mac = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ip = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaTaiSanKhaiBao = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    Cpu = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RamGb = table.Column<decimal>(type: "decimal(8,1)", precision: 8, scale: 1, nullable: true),
                    OCungTongGb = table.Column<decimal>(type: "decimal(10,1)", precision: 10, scale: 1, nullable: true),
                    OCungTrongGb = table.Column<decimal>(type: "decimal(10,1)", precision: 10, scale: 1, nullable: true),
                    HeDieuHanh = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NguoiDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhienBanAgent = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PhanMem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LanDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LanCuoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpGui = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    SoLanGui = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MayTramAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MayTramAgents_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MayTramAgents_Hostname",
                table: "MayTramAgents",
                column: "Hostname");

            migrationBuilder.CreateIndex(
                name: "IX_MayTramAgents_SerialBios",
                table: "MayTramAgents",
                column: "SerialBios");

            migrationBuilder.CreateIndex(
                name: "IX_MayTramAgents_TaiSanId",
                table: "MayTramAgents",
                column: "TaiSanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MayTramAgents");
        }
    }
}
