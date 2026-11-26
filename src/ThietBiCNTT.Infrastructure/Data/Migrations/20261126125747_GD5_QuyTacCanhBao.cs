using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD5_QuyTacCanhBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuyTacCanhBaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DoiTuong = table.Column<int>(type: "int", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    UuTienTu = table.Column<int>(type: "int", nullable: true),
                    NguoiNhan = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VaiTroNhan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GuiNguoiPhuTrach = table.Column<bool>(type: "bit", nullable: false),
                    GuiTruongKhoa = table.Column<bool>(type: "bit", nullable: false),
                    MucDo = table.Column<int>(type: "int", nullable: false),
                    ChiTrongUngDung = table.Column<bool>(type: "bit", nullable: false),
                    NhacLaiSauGio = table.Column<int>(type: "int", nullable: false),
                    HanhDong = table.Column<int>(type: "int", nullable: false),
                    DangBat = table.Column<bool>(type: "bit", nullable: false),
                    LanChayCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoLanGui = table.Column<int>(type: "int", nullable: false),
                    LoiCuoi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuyTacCanhBaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuyTacCanhBaos_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_QuyTacCanhBaos_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CanhBaoDaGuis",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuyTacCanhBaoId = table.Column<int>(type: "int", nullable: false),
                    DoiTuongId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LanDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LanCuoi = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SoLan = table.Column<int>(type: "int", nullable: false),
                    DangKhop = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CanhBaoDaGuis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CanhBaoDaGuis_QuyTacCanhBaos_QuyTacCanhBaoId",
                        column: x => x.QuyTacCanhBaoId,
                        principalTable: "QuyTacCanhBaos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DieuKienCanhBaos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuyTacCanhBaoId = table.Column<int>(type: "int", nullable: false),
                    Truong = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    ToanTu = table.Column<int>(type: "int", nullable: false),
                    GiaTri = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DieuKienCanhBaos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DieuKienCanhBaos_QuyTacCanhBaos_QuyTacCanhBaoId",
                        column: x => x.QuyTacCanhBaoId,
                        principalTable: "QuyTacCanhBaos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CanhBaoDaGuis_QuyTacCanhBaoId_DoiTuongId",
                table: "CanhBaoDaGuis",
                columns: new[] { "QuyTacCanhBaoId", "DoiTuongId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DieuKienCanhBaos_QuyTacCanhBaoId",
                table: "DieuKienCanhBaos",
                column: "QuyTacCanhBaoId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyTacCanhBaos_KhoaPhongId",
                table: "QuyTacCanhBaos",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_QuyTacCanhBaos_LoaiThietBiId",
                table: "QuyTacCanhBaos",
                column: "LoaiThietBiId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CanhBaoDaGuis");

            migrationBuilder.DropTable(
                name: "DieuKienCanhBaos");

            migrationBuilder.DropTable(
                name: "QuyTacCanhBaos");
        }
    }
}
