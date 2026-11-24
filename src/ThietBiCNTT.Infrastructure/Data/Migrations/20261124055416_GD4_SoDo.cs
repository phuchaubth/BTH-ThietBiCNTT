using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD4_SoDo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SoDoMatBangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    ViTriId = table.Column<int>(type: "int", nullable: true),
                    TepAnhId = table.Column<long>(type: "bigint", nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoDoMatBangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SoDoMatBangs_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoDoMatBangs_ViTris_ViTriId",
                        column: x => x.ViTriId,
                        principalTable: "ViTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiemSoDos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoDoMatBangId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: false),
                    Y = table.Column<decimal>(type: "decimal(6,3)", precision: 6, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiemSoDos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiemSoDos_SoDoMatBangs_SoDoMatBangId",
                        column: x => x.SoDoMatBangId,
                        principalTable: "SoDoMatBangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DiemSoDos_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DiemSoDos_SoDoMatBangId_TaiSanId",
                table: "DiemSoDos",
                columns: new[] { "SoDoMatBangId", "TaiSanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiemSoDos_TaiSanId",
                table: "DiemSoDos",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDoMatBangs_KhoaPhongId",
                table: "SoDoMatBangs",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_SoDoMatBangs_ViTriId",
                table: "SoDoMatBangs",
                column: "ViTriId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiemSoDos");

            migrationBuilder.DropTable(
                name: "SoDoMatBangs");
        }
    }
}
