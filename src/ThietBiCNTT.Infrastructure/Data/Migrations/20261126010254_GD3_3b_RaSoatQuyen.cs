using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_3b_RaSoatQuyen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DotRaSoatQuyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    Han = table.Column<DateOnly>(type: "date", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SoNgayKhongDangNhap = table.Column<int>(type: "int", nullable: false),
                    DaNhacQuaHan = table.Column<bool>(type: "bit", nullable: false),
                    NgayNhapDanhSach = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BienBan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DotRaSoatQuyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DotRaSoatQuyens_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThongBaoNghiViecs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NgayHieuLuc = table.Column<DateOnly>(type: "date", nullable: false),
                    KhoaMoi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Han = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaLeoThang = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaoNghiViecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ThongBaoNghiViecs_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViPhamTruyCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nguon = table.Column<int>(type: "int", nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NgayPhatHien = table.Column<DateOnly>(type: "date", nullable: false),
                    NghiemTrong = table.Column<bool>(type: "bit", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NguoiXuLy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayDong = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SuCoAnNinhId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViPhamTruyCaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViPhamTruyCaps_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViPhamTruyCaps_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoanRaSoats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DotRaSoatQuyenId = table.Column<int>(type: "int", nullable: false),
                    TenDangNhap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KhoaText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NhomQuyen = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LanDangNhapCuoi = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TrangThaiTk = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KhongDangNhapLau = table.Column<bool>(type: "bit", nullable: false),
                    DungChung = table.Column<bool>(type: "bit", nullable: false),
                    QuyenQuanTri = table.Column<bool>(type: "bit", nullable: false),
                    KhongThuocKhoa = table.Column<bool>(type: "bit", nullable: false),
                    QuyetDinh = table.Column<int>(type: "int", nullable: false),
                    QuyenMoi = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NguoiXacNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayXacNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaThucHien = table.Column<bool>(type: "bit", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayThucHien = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoanRaSoats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiKhoanRaSoats_DotRaSoatQuyens_DotRaSoatQuyenId",
                        column: x => x.DotRaSoatQuyenId,
                        principalTable: "DotRaSoatQuyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaiKhoanRaSoats_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ViecThuHoiQuyen",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThongBaoNghiViecId = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    NoiDung = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    DaThucHien = table.Column<bool>(type: "bit", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayThucHien = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViecThuHoiQuyen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViecThuHoiQuyen_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViecThuHoiQuyen_ThongBaoNghiViecs_ThongBaoNghiViecId",
                        column: x => x.ThongBaoNghiViecId,
                        principalTable: "ThongBaoNghiViecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DotRaSoatQuyens_HeThongId",
                table: "DotRaSoatQuyens",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_DotRaSoatQuyens_Ma",
                table: "DotRaSoatQuyens",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanRaSoats_DotRaSoatQuyenId",
                table: "TaiKhoanRaSoats",
                column: "DotRaSoatQuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanRaSoats_KhoaPhongId",
                table: "TaiKhoanRaSoats",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoanRaSoats_TenDangNhap",
                table: "TaiKhoanRaSoats",
                column: "TenDangNhap");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoNghiViecs_KhoaPhongId",
                table: "ThongBaoNghiViecs",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_ViecThuHoiQuyen_HeThongId",
                table: "ViecThuHoiQuyen",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_ViecThuHoiQuyen_ThongBaoNghiViecId",
                table: "ViecThuHoiQuyen",
                column: "ThongBaoNghiViecId");

            migrationBuilder.CreateIndex(
                name: "IX_ViPhamTruyCaps_HeThongId",
                table: "ViPhamTruyCaps",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_ViPhamTruyCaps_KhoaPhongId",
                table: "ViPhamTruyCaps",
                column: "KhoaPhongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiKhoanRaSoats");

            migrationBuilder.DropTable(
                name: "ViecThuHoiQuyen");

            migrationBuilder.DropTable(
                name: "ViPhamTruyCaps");

            migrationBuilder.DropTable(
                name: "DotRaSoatQuyens");

            migrationBuilder.DropTable(
                name: "ThongBaoNghiViecs");
        }
    }
}
