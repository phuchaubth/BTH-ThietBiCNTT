using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD5_BaoTriDayDu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChiSoBoDem",
                table: "PhieuSuaChuas",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DungChoBaoTri",
                table: "MauKiemTras",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ChuKyBaoTriNgay",
                table: "LoaiThietBis",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PhieuBaoTriId",
                table: "LanKiemTras",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ChiPhiDuKien",
                table: "KeHoachBaoTris",
                type: "decimal(18,2)",
                precision: 18,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "ChuKyChiSo",
                table: "KeHoachBaoTris",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DonViChiSo",
                table: "KeHoachBaoTris",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GioCongDuKien",
                table: "KeHoachBaoTris",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<int>(
                name: "KhoaPhongId",
                table: "KeHoachBaoTris",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KieuChuKy",
                table: "KeHoachBaoTris",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MauKiemTraId",
                table: "KeHoachBaoTris",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayDuyet",
                table: "KeHoachBaoTris",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayKetThuc",
                table: "KeHoachBaoTris",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayThamDinh",
                table: "KeHoachBaoTris",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyet",
                table: "KeHoachBaoTris",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiThamDinh",
                table: "KeHoachBaoTris",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "KeHoachBaoTris",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "YKien",
                table: "KeHoachBaoTris",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KeHoachVatTus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KeHoachBaoTriId = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeHoachVatTus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeHoachVatTus_KeHoachBaoTris_KeHoachBaoTriId",
                        column: x => x.KeHoachBaoTriId,
                        principalTable: "KeHoachBaoTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KeHoachVatTus_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LanKiemTras_PhieuBaoTriId",
                table: "LanKiemTras",
                column: "PhieuBaoTriId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachBaoTris_KhoaPhongId",
                table: "KeHoachBaoTris",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachBaoTris_MauKiemTraId",
                table: "KeHoachBaoTris",
                column: "MauKiemTraId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachBaoTris_TrangThai_NgayTiepTheo",
                table: "KeHoachBaoTris",
                columns: new[] { "TrangThai", "NgayTiepTheo" });

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVatTus_KeHoachBaoTriId",
                table: "KeHoachVatTus",
                column: "KeHoachBaoTriId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachVatTus_VatTuId",
                table: "KeHoachVatTus",
                column: "VatTuId");

            migrationBuilder.AddForeignKey(
                name: "FK_KeHoachBaoTris_KhoaPhongs_KhoaPhongId",
                table: "KeHoachBaoTris",
                column: "KhoaPhongId",
                principalTable: "KhoaPhongs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KeHoachBaoTris_MauKiemTras_MauKiemTraId",
                table: "KeHoachBaoTris",
                column: "MauKiemTraId",
                principalTable: "MauKiemTras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // Kế hoạch đang tắt trước khi có quy trình duyệt → Tạm dừng (các kế hoạch khác mặc định Hiệu lực)
            migrationBuilder.Sql("UPDATE KeHoachBaoTris SET TrangThai = 5 WHERE DangApDung = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KeHoachBaoTris_KhoaPhongs_KhoaPhongId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropForeignKey(
                name: "FK_KeHoachBaoTris_MauKiemTras_MauKiemTraId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropTable(
                name: "KeHoachVatTus");

            migrationBuilder.DropIndex(
                name: "IX_LanKiemTras_PhieuBaoTriId",
                table: "LanKiemTras");

            migrationBuilder.DropIndex(
                name: "IX_KeHoachBaoTris_KhoaPhongId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropIndex(
                name: "IX_KeHoachBaoTris_MauKiemTraId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropIndex(
                name: "IX_KeHoachBaoTris_TrangThai_NgayTiepTheo",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "ChiSoBoDem",
                table: "PhieuSuaChuas");

            migrationBuilder.DropColumn(
                name: "DungChoBaoTri",
                table: "MauKiemTras");

            migrationBuilder.DropColumn(
                name: "ChuKyBaoTriNgay",
                table: "LoaiThietBis");

            migrationBuilder.DropColumn(
                name: "PhieuBaoTriId",
                table: "LanKiemTras");

            migrationBuilder.DropColumn(
                name: "ChiPhiDuKien",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "ChuKyChiSo",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "DonViChiSo",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "GioCongDuKien",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "KhoaPhongId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "KieuChuKy",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "MauKiemTraId",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "NgayDuyet",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "NgayKetThuc",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "NgayThamDinh",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "NguoiDuyet",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "NguoiThamDinh",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "KeHoachBaoTris");

            migrationBuilder.DropColumn(
                name: "YKien",
                table: "KeHoachBaoTris");
        }
    }
}
