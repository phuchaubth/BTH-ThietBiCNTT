using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_SaoLuuChuKyPhut : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChuKyGio",
                table: "TacVuSaoLuus",
                newName: "ChuKyPhut");
            // Giá trị cũ tính bằng giờ → quy đổi sang phút.
            migrationBuilder.Sql("UPDATE TacVuSaoLuus SET ChuKyPhut = ChuKyPhut * 60");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE TacVuSaoLuus SET ChuKyPhut = CASE WHEN ChuKyPhut / 60 < 1 THEN 1 ELSE ChuKyPhut / 60 END");
            migrationBuilder.RenameColumn(
                name: "ChuKyPhut",
                table: "TacVuSaoLuus",
                newName: "ChuKyGio");
        }
    }
}
