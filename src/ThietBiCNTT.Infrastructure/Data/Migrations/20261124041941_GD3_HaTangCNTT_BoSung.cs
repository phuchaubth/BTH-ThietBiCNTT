using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_HaTangCNTT_BoSung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AnhHuongAnToan",
                table: "SuKienDowntimes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanRaSoat",
                table: "KeHoachDowntimes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DaNhacRaSoat",
                table: "KeHoachDowntimes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LyDoRaSoat",
                table: "KeHoachDowntimes",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnhHuongAnToan",
                table: "SuKienDowntimes");

            migrationBuilder.DropColumn(
                name: "CanRaSoat",
                table: "KeHoachDowntimes");

            migrationBuilder.DropColumn(
                name: "DaNhacRaSoat",
                table: "KeHoachDowntimes");

            migrationBuilder.DropColumn(
                name: "LyDoRaSoat",
                table: "KeHoachDowntimes");
        }
    }
}
