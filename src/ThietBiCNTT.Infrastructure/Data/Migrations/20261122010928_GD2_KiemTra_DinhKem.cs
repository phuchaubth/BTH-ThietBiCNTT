using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD2_KiemTra_DinhKem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MauKiemTras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    ViTri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChuKyGio = table.Column<int>(type: "int", nullable: false),
                    TuTaoPhieu = table.Column<bool>(type: "bit", nullable: false),
                    DangApDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MauKiemTras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MauKiemTras_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MauKiemTras_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MauKiemTras_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TepDinhKems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoiTuong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DoiTuongId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenFile = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TenLuu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    KichThuoc = table.Column<long>(type: "bigint", nullable: false),
                    NguoiTai = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDinhKems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LanKiemTras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MauKiemTraId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    NguoiKiem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CoBatThuong = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LanKiemTras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LanKiemTras_MauKiemTras_MauKiemTraId",
                        column: x => x.MauKiemTraId,
                        principalTable: "MauKiemTras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LanKiemTras_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LanKiemTras_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MucKiemTras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MauKiemTraId = table.Column<int>(type: "int", nullable: false),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Kieu = table.Column<int>(type: "int", nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NguongMin = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    NguongMax = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    BatBuoc = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucKiemTras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MucKiemTras_MauKiemTras_MauKiemTraId",
                        column: x => x.MauKiemTraId,
                        principalTable: "MauKiemTras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaKiemTras",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LanKiemTraId = table.Column<long>(type: "bigint", nullable: false),
                    MucKiemTraId = table.Column<int>(type: "int", nullable: false),
                    NoiDungMuc = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Dat = table.Column<bool>(type: "bit", nullable: true),
                    GiaTriSo = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    GiaTriText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BatThuong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaKiemTras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KetQuaKiemTras_LanKiemTras_LanKiemTraId",
                        column: x => x.LanKiemTraId,
                        principalTable: "LanKiemTras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaKiemTras_LanKiemTraId",
                table: "KetQuaKiemTras",
                column: "LanKiemTraId");

            migrationBuilder.CreateIndex(
                name: "IX_LanKiemTras_MauKiemTraId_TaiSanId_ThoiGian",
                table: "LanKiemTras",
                columns: new[] { "MauKiemTraId", "TaiSanId", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_LanKiemTras_PhieuSuaChuaId",
                table: "LanKiemTras",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_LanKiemTras_TaiSanId",
                table: "LanKiemTras",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_MauKiemTras_KhoaPhongId",
                table: "MauKiemTras",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_MauKiemTras_LoaiThietBiId",
                table: "MauKiemTras",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_MauKiemTras_TaiSanId",
                table: "MauKiemTras",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_MucKiemTras_MauKiemTraId",
                table: "MucKiemTras",
                column: "MauKiemTraId");

            migrationBuilder.CreateIndex(
                name: "IX_TepDinhKems_DoiTuong_DoiTuongId",
                table: "TepDinhKems",
                columns: new[] { "DoiTuong", "DoiTuongId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KetQuaKiemTras");

            migrationBuilder.DropTable(
                name: "MucKiemTras");

            migrationBuilder.DropTable(
                name: "TepDinhKems");

            migrationBuilder.DropTable(
                name: "LanKiemTras");

            migrationBuilder.DropTable(
                name: "MauKiemTras");
        }
    }
}
