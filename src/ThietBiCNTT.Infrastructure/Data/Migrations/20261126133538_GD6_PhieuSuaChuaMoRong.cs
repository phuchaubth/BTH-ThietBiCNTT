using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD6_PhieuSuaChuaMoRong : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ChiPhiNhanCong",
                table: "PhieuSuaChuas",
                type: "decimal(18,0)",
                precision: 18,
                scale: 0,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "HanhDongKhacPhuc",
                table: "PhieuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KhoaGocThietBiTamId",
                table: "PhieuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayChoMuonTam",
                table: "PhieuSuaChuas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayThuHoiTam",
                table: "PhieuSuaChuas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NhomNguyenNhan",
                table: "PhieuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThietBiTamId",
                table: "PhieuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GioCongPhieus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GioCongPhieus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GioCongPhieus_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_ThietBiTamId",
                table: "PhieuSuaChuas",
                column: "ThietBiTamId");

            migrationBuilder.CreateIndex(
                name: "IX_GioCongPhieus_NguoiThucHien_BatDau",
                table: "GioCongPhieus",
                columns: new[] { "NguoiThucHien", "BatDau" });

            migrationBuilder.CreateIndex(
                name: "IX_GioCongPhieus_PhieuSuaChuaId",
                table: "GioCongPhieus",
                column: "PhieuSuaChuaId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuSuaChuas_TaiSans_ThietBiTamId",
                table: "PhieuSuaChuas",
                column: "ThietBiTamId",
                principalTable: "TaiSans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhieuSuaChuas_TaiSans_ThietBiTamId",
                table: "PhieuSuaChuas");

            migrationBuilder.DropTable(
                name: "GioCongPhieus");

            migrationBuilder.DropIndex(
                name: "IX_PhieuSuaChuas_ThietBiTamId",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "ChiPhiNhanCong",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "HanhDongKhacPhuc",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "KhoaGocThietBiTamId",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "NgayChoMuonTam",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "NgayThuHoiTam",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "NhomNguyenNhan",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "ThietBiTamId",
                table: "PhieuSuaChuas");
        }
    }
}
