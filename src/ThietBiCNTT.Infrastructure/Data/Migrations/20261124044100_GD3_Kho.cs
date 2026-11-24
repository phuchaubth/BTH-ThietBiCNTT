using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_Kho : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nhom",
                table: "VatTus",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuyCach",
                table: "VatTus",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TonToiDa",
                table: "VatTus",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "KhoId",
                table: "ChiTietVatTuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhieuKhoId",
                table: "ChiTietVatTuSuaChuas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Khos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    ThuKho = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViTri = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Khos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhieuKhos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<int>(type: "int", nullable: false),
                    KhoId = table.Column<int>(type: "int", nullable: false),
                    KhoDenId = table.Column<int>(type: "int", nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: true),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    SoChungTu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NguoiGiaoNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DienGiai = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayGhiSo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuKhos_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhos_Khos_KhoDenId",
                        column: x => x.KhoDenId,
                        principalTable: "Khos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhos_Khos_KhoId",
                        column: x => x.KhoId,
                        principalTable: "Khos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhos_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKhos_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TonKhoVatTus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoId = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKhoVatTus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TonKhoVatTus_Khos_KhoId",
                        column: x => x.KhoId,
                        principalTable: "Khos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TonKhoVatTus_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuKhos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuKhoId = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    TonSoSach = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKhos_PhieuKhos_PhieuKhoId",
                        column: x => x.PhieuKhoId,
                        principalTable: "PhieuKhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuKhos_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TheKhos",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhoId = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: false),
                    PhieuKhoId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Nhap = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Xuat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TonSau = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    DienGiai = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheKhos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TheKhos_Khos_KhoId",
                        column: x => x.KhoId,
                        principalTable: "Khos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheKhos_PhieuKhos_PhieuKhoId",
                        column: x => x.PhieuKhoId,
                        principalTable: "PhieuKhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TheKhos_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKhos_PhieuKhoId",
                table: "ChiTietPhieuKhos",
                column: "PhieuKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuKhos_VatTuId",
                table: "ChiTietPhieuKhos",
                column: "VatTuId");

            migrationBuilder.CreateIndex(
                name: "IX_Khos_Ma",
                table: "Khos",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_KhoaPhongId",
                table: "PhieuKhos",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_KhoDenId",
                table: "PhieuKhos",
                column: "KhoDenId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_KhoId",
                table: "PhieuKhos",
                column: "KhoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_NhaCungCapId",
                table: "PhieuKhos",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_PhieuSuaChuaId",
                table: "PhieuKhos",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKhos_SoPhieu",
                table: "PhieuKhos",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TheKhos_KhoId_VatTuId_ThoiGian",
                table: "TheKhos",
                columns: new[] { "KhoId", "VatTuId", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_TheKhos_PhieuKhoId",
                table: "TheKhos",
                column: "PhieuKhoId");

            migrationBuilder.CreateIndex(
                name: "IX_TheKhos_VatTuId",
                table: "TheKhos",
                column: "VatTuId");

            migrationBuilder.CreateIndex(
                name: "IX_TonKhoVatTus_KhoId_VatTuId",
                table: "TonKhoVatTus",
                columns: new[] { "KhoId", "VatTuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TonKhoVatTus_VatTuId",
                table: "TonKhoVatTus",
                column: "VatTuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuKhos");

            migrationBuilder.DropTable(
                name: "TheKhos");

            migrationBuilder.DropTable(
                name: "TonKhoVatTus");

            migrationBuilder.DropTable(
                name: "PhieuKhos");

            migrationBuilder.DropTable(
                name: "Khos");

            migrationBuilder.DropColumn(
                name: "Nhom",
                table: "VatTus");

            migrationBuilder.DropColumn(
                name: "QuyCach",
                table: "VatTus");

            migrationBuilder.DropColumn(
                name: "TonToiDa",
                table: "VatTus");

            migrationBuilder.DropColumn(
                name: "KhoId",
                table: "ChiTietVatTuSuaChuas");

            migrationBuilder.DropColumn(
                name: "PhieuKhoId",
                table: "ChiTietVatTuSuaChuas");
        }
    }
}
