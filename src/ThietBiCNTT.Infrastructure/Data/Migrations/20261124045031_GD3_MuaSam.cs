using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_MuaSam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeXuatMuaSams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CanCu = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: true),
                    KhoanMucNganSachId = table.Column<int>(type: "int", nullable: true),
                    TuDong = table.Column<bool>(type: "bit", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    CapHienTai = table.Column<int>(type: "int", nullable: false),
                    CapCuoi = table.Column<int>(type: "int", nullable: false),
                    LuotGui = table.Column<int>(type: "int", nullable: false),
                    CoCapVatTu = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeXuatMuaSams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeXuatMuaSams_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonMuaHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: false),
                    HopDongId = table.Column<int>(type: "int", nullable: true),
                    CanCu = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HanGiao = table.Column<DateOnly>(type: "date", nullable: true),
                    ThueSuat = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BaoHanhThang = table.Column<int>(type: "int", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayDatHang = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KhoanMucNganSachId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonMuaHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DonMuaHangs_HopDongs_HopDongId",
                        column: x => x.HopDongId,
                        principalTable: "HopDongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonMuaHangs_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocDuyetMuaSams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeXuatMuaSamId = table.Column<int>(type: "int", nullable: false),
                    Cap = table.Column<int>(type: "int", nullable: false),
                    LuotGui = table.Column<int>(type: "int", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TenNguoiDuyet = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DongY = table.Column<bool>(type: "bit", nullable: false),
                    YKien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BuocDuyetMuaSams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuocDuyetMuaSams_DeXuatMuaSams_DeXuatMuaSamId",
                        column: x => x.DeXuatMuaSamId,
                        principalTable: "DeXuatMuaSams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DongDeXuats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeXuatMuaSamId = table.Column<int>(type: "int", nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: true),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGiaDuKien = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    SoLuongDaDat = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongDeXuats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DongDeXuats_DeXuatMuaSams_DeXuatMuaSamId",
                        column: x => x.DeXuatMuaSamId,
                        principalTable: "DeXuatMuaSams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DongDeXuats_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongDeXuats_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuNhanHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonMuaHangId = table.Column<int>(type: "int", nullable: false),
                    NgayNhan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SoHoaDon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BienBanNghiemThu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KhoId = table.Column<int>(type: "int", nullable: true),
                    PhieuKhoId = table.Column<int>(type: "int", nullable: true),
                    GiaTri = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    TaiSanDaTao = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhanHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuNhanHangs_DonMuaHangs_DonMuaHangId",
                        column: x => x.DonMuaHangId,
                        principalTable: "DonMuaHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhieuNhanHangs_PhieuKhos_PhieuKhoId",
                        column: x => x.PhieuKhoId,
                        principalTable: "PhieuKhos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DongDonHangs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonMuaHangId = table.Column<int>(type: "int", nullable: false),
                    DongDeXuatId = table.Column<int>(type: "int", nullable: true),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: true),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    MoTa = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    SoLuongDaNhan = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongDonHangs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DongDonHangs_DonMuaHangs_DonMuaHangId",
                        column: x => x.DonMuaHangId,
                        principalTable: "DonMuaHangs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DongDonHangs_DongDeXuats_DongDeXuatId",
                        column: x => x.DongDeXuatId,
                        principalTable: "DongDeXuats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongDonHangs_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongDonHangs_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BuocDuyetMuaSams_DeXuatMuaSamId",
                table: "BuocDuyetMuaSams",
                column: "DeXuatMuaSamId");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatMuaSams_PhieuSuaChuaId",
                table: "DeXuatMuaSams",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatMuaSams_SoPhieu",
                table: "DeXuatMuaSams",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatMuaSams_TrangThai_CapHienTai",
                table: "DeXuatMuaSams",
                columns: new[] { "TrangThai", "CapHienTai" });

            migrationBuilder.CreateIndex(
                name: "IX_DongDeXuats_DeXuatMuaSamId",
                table: "DongDeXuats",
                column: "DeXuatMuaSamId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDeXuats_LoaiThietBiId",
                table: "DongDeXuats",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDeXuats_VatTuId",
                table: "DongDeXuats",
                column: "VatTuId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDonHangs_DongDeXuatId",
                table: "DongDonHangs",
                column: "DongDeXuatId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDonHangs_DonMuaHangId",
                table: "DongDonHangs",
                column: "DonMuaHangId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDonHangs_LoaiThietBiId",
                table: "DongDonHangs",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_DongDonHangs_VatTuId",
                table: "DongDonHangs",
                column: "VatTuId");

            migrationBuilder.CreateIndex(
                name: "IX_DonMuaHangs_HopDongId",
                table: "DonMuaHangs",
                column: "HopDongId");

            migrationBuilder.CreateIndex(
                name: "IX_DonMuaHangs_NhaCungCapId",
                table: "DonMuaHangs",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_DonMuaHangs_SoPhieu",
                table: "DonMuaHangs",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonMuaHangs_TrangThai_HanGiao",
                table: "DonMuaHangs",
                columns: new[] { "TrangThai", "HanGiao" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhanHangs_DonMuaHangId",
                table: "PhieuNhanHangs",
                column: "DonMuaHangId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhanHangs_PhieuKhoId",
                table: "PhieuNhanHangs",
                column: "PhieuKhoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BuocDuyetMuaSams");

            migrationBuilder.DropTable(
                name: "DongDonHangs");

            migrationBuilder.DropTable(
                name: "PhieuNhanHangs");

            migrationBuilder.DropTable(
                name: "DongDeXuats");

            migrationBuilder.DropTable(
                name: "DonMuaHangs");

            migrationBuilder.DropTable(
                name: "DeXuatMuaSams");
        }
    }
}
