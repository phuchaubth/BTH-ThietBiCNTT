using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD2_KiemKe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotKiemKes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayKhoaSo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    ToKiemKe = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotKiemKes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotKiemKes_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietKiemKes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotKiemKeId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    MaQuet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KhoaSoSachId = table.Column<int>(type: "int", nullable: true),
                    KhoaThucTeId = table.Column<int>(type: "int", nullable: true),
                    KetQua = table.Column<int>(type: "int", nullable: false),
                    TinhTrangThucTe = table.Column<int>(type: "int", nullable: true),
                    NguoiKiem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThoiGianKiem = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietKiemKes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKes_DotKiemKes_DotKiemKeId",
                        column: x => x.DotKiemKeId,
                        principalTable: "DotKiemKes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKes_KhoaPhongs_KhoaSoSachId",
                        column: x => x.KhoaSoSachId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKes_KhoaPhongs_KhoaThucTeId",
                        column: x => x.KhoaThucTeId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKes_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKes_DotKiemKeId_KetQua",
                table: "ChiTietKiemKes",
                columns: new[] { "DotKiemKeId", "KetQua" });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKes_DotKiemKeId_TaiSanId",
                table: "ChiTietKiemKes",
                columns: new[] { "DotKiemKeId", "TaiSanId" });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKes_KhoaSoSachId",
                table: "ChiTietKiemKes",
                column: "KhoaSoSachId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKes_KhoaThucTeId",
                table: "ChiTietKiemKes",
                column: "KhoaThucTeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKes_TaiSanId",
                table: "ChiTietKiemKes",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_DotKiemKes_KhoaPhongId",
                table: "DotKiemKes",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_DotKiemKes_Ma",
                table: "DotKiemKes",
                column: "Ma",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietKiemKes");

            migrationBuilder.DropTable(
                name: "DotKiemKes");
        }
    }
}
