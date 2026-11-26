using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD6_VongDoiTaiSan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeXuatThanhLyId",
                table: "PhieuKhos",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DenKhoaPhongId",
                table: "PhieuDieuChuyens",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "DonViNhanNgoai",
                table: "PhieuDieuChuyens",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NgayQuyetDinh",
                table: "PhieuDieuChuyens",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RaNgoai",
                table: "PhieuDieuChuyens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SoQuyetDinh",
                table: "PhieuDieuChuyens",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeXuatThanhLyId",
                table: "PhieuKhos");

            migrationBuilder.DropColumn(
                name: "DonViNhanNgoai",
                table: "PhieuDieuChuyens");

            migrationBuilder.DropColumn(
                name: "NgayQuyetDinh",
                table: "PhieuDieuChuyens");

            migrationBuilder.DropColumn(
                name: "RaNgoai",
                table: "PhieuDieuChuyens");

            migrationBuilder.DropColumn(
                name: "SoQuyetDinh",
                table: "PhieuDieuChuyens");

            migrationBuilder.AlterColumn<int>(
                name: "DenKhoaPhongId",
                table: "PhieuDieuChuyens",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
