using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_NganSach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KhoanMucNganSachs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Nam = table.Column<int>(type: "int", nullable: false),
                    NhomChi = table.Column<int>(type: "int", nullable: false),
                    DuToan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    Quy1 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: true),
                    Quy2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: true),
                    Quy3 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: true),
                    Quy4 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: true),
                    GomChiPhiSuaNgoai = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoanMucNganSachs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChiNganSachs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoanMucNganSachId = table.Column<int>(type: "int", nullable: false),
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    SoChungTu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HopDongId = table.Column<int>(type: "int", nullable: true),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: true),
                    NguoiNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiNganSachs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiNganSachs_KhoanMucNganSachs_KhoanMucNganSachId",
                        column: x => x.KhoanMucNganSachId,
                        principalTable: "KhoanMucNganSachs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DieuChinhNganSachs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoanMucNganSachId = table.Column<int>(type: "int", nullable: false),
                    SoTien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NguoiDeNghi = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgayDeNghi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaDuyet = table.Column<bool>(type: "bit", nullable: false),
                    TuChoi = table.Column<bool>(type: "bit", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuChinhNganSachs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DieuChinhNganSachs_KhoanMucNganSachs_KhoanMucNganSachId",
                        column: x => x.KhoanMucNganSachId,
                        principalTable: "KhoanMucNganSachs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DonMuaHangs_KhoanMucNganSachId",
                table: "DonMuaHangs",
                column: "KhoanMucNganSachId");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatMuaSams_KhoanMucNganSachId",
                table: "DeXuatMuaSams",
                column: "KhoanMucNganSachId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiNganSachs_KhoanMucNganSachId_Ngay",
                table: "ChiNganSachs",
                columns: new[] { "KhoanMucNganSachId", "Ngay" });

            migrationBuilder.CreateIndex(
                name: "IX_DieuChinhNganSachs_KhoanMucNganSachId",
                table: "DieuChinhNganSachs",
                column: "KhoanMucNganSachId");

            migrationBuilder.CreateIndex(
                name: "IX_KhoanMucNganSachs_Nam_Ma",
                table: "KhoanMucNganSachs",
                columns: new[] { "Nam", "Ma" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DeXuatMuaSams_KhoanMucNganSachs_KhoanMucNganSachId",
                table: "DeXuatMuaSams",
                column: "KhoanMucNganSachId",
                principalTable: "KhoanMucNganSachs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DonMuaHangs_KhoanMucNganSachs_KhoanMucNganSachId",
                table: "DonMuaHangs",
                column: "KhoanMucNganSachId",
                principalTable: "KhoanMucNganSachs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DeXuatMuaSams_KhoanMucNganSachs_KhoanMucNganSachId",
                table: "DeXuatMuaSams");

            migrationBuilder.DropForeignKey(
                name: "FK_DonMuaHangs_KhoanMucNganSachs_KhoanMucNganSachId",
                table: "DonMuaHangs");

            migrationBuilder.DropTable(
                name: "ChiNganSachs");

            migrationBuilder.DropTable(
                name: "DieuChinhNganSachs");

            migrationBuilder.DropTable(
                name: "KhoanMucNganSachs");

            migrationBuilder.DropIndex(
                name: "IX_DonMuaHangs_KhoanMucNganSachId",
                table: "DonMuaHangs");

            migrationBuilder.DropIndex(
                name: "IX_DeXuatMuaSams_KhoanMucNganSachId",
                table: "DeXuatMuaSams");
        }
    }
}
