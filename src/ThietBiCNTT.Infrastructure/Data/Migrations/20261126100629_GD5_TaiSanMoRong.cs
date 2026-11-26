using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD5_TaiSanMoRong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TruongTuyChinhs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kieu = table.Column<int>(type: "int", nullable: false),
                    LuaChon = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BatBuoc = table.Column<bool>(type: "bit", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DangDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TruongTuyChinhs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TruongTuyChinhs_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GiaTriTuyChinhs",
                columns: table => new
                {
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    TruongTuyChinhId = table.Column<int>(type: "int", nullable: false),
                    GiaTri = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiaTriTuyChinhs", x => new { x.TaiSanId, x.TruongTuyChinhId });
                    table.ForeignKey(
                        name: "FK_GiaTriTuyChinhs_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GiaTriTuyChinhs_TruongTuyChinhs_TruongTuyChinhId",
                        column: x => x.TruongTuyChinhId,
                        principalTable: "TruongTuyChinhs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiaTriTuyChinhs_TruongTuyChinhId_GiaTri",
                table: "GiaTriTuyChinhs",
                columns: new[] { "TruongTuyChinhId", "GiaTri" });

            migrationBuilder.CreateIndex(
                name: "IX_TruongTuyChinhs_LoaiThietBiId",
                table: "TruongTuyChinhs",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_TruongTuyChinhs_Ma",
                table: "TruongTuyChinhs",
                column: "Ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiaTriTuyChinhs");

            migrationBuilder.DropTable(
                name: "TruongTuyChinhs");
        }
    }
}
