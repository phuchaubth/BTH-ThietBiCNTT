using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class GD3_3a_ThayDoi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuThayDois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HeThongId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    MucRui = table.Column<int>(type: "int", nullable: false),
                    LaThayDoiLon = table.Column<bool>(type: "bit", nullable: false),
                    KeHoachThucHien = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    KeHoachKiemThu = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    PhuongAnQuayLui = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    CanDowntime = table.Column<bool>(type: "bit", nullable: false),
                    DeXuatBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeXuatKetThuc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LaHeThongSaoLuu = table.Column<bool>(type: "bit", nullable: false),
                    LaCongCuHoTroQuyetDinh = table.Column<bool>(type: "bit", nullable: false),
                    NguoiDeXuat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiDuyetCntt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetCntt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDuyetChuSoHuu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetChuSoHuu = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDuyetLanhDao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyetLanhDao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LyDoTuChoi = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayGuiDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HanDuyetBoSung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DaLeoThang = table.Column<bool>(type: "bit", nullable: false),
                    BatDauThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KetThucThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KetQua = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    KiemThuSau = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CoVanDe = table.Column<bool>(type: "bit", nullable: false),
                    NguoiXacNhanNghiepVu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayXacNhanNghiepVu = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PhienBanMoi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SuKienDowntimeId = table.Column<int>(type: "int", nullable: true),
                    HeThongMoiId = table.Column<int>(type: "int", nullable: true),
                    LanThuPhucHoiId = table.Column<int>(type: "int", nullable: true),
                    AnhHuongChamSoc = table.Column<bool>(type: "bit", nullable: false),
                    BienPhapAnToan = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguoiPhuTrachKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NhaThau = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    GiamSatNhaThau = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuThayDois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuThayDois_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuThayDois_SuKienDowntimes_SuKienDowntimeId",
                        column: x => x.SuKienDowntimeId,
                        principalTable: "SuKienDowntimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuThayDois_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThayDoiChuanMaus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HuongDan = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: false),
                    KiemThu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    HoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NguoiDuyet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayDuyet = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThayDoiChuanMaus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DanhGiaSauThayDois",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuThayDoiId = table.Column<int>(type: "int", nullable: false),
                    Han = table.Column<DateOnly>(type: "date", nullable: false),
                    DaNhac = table.Column<bool>(type: "bit", nullable: false),
                    DeDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DungMucDich = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TichHop = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AnToanNguoiBenh = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TacDongNhanVien = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KetLuan = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiDanhGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGiaSauThayDois", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DanhGiaSauThayDois_PhieuThayDois_PhieuThayDoiId",
                        column: x => x.PhieuThayDoiId,
                        principalTable: "PhieuThayDois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DuAnXayDungs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ViTri = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    TuNgay = table.Column<DateOnly>(type: "date", nullable: true),
                    DenNgay = table.Column<DateOnly>(type: "date", nullable: true),
                    HaTangAnhHuong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NguoiThamGiaCntt = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NgayDanhGia = table.Column<DateOnly>(type: "date", nullable: true),
                    BienPhapBaoVe = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NhaThau = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    KetQuaGiamSat = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PhieuThayDoiId = table.Column<int>(type: "int", nullable: true),
                    DaHoanThanh = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuAnXayDungs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DuAnXayDungs_PhieuThayDois_PhieuThayDoiId",
                        column: x => x.PhieuThayDoiId,
                        principalTable: "PhieuThayDois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MucKiemTraThayDoi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuThayDoiId = table.Column<int>(type: "int", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Dat = table.Column<bool>(type: "bit", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NguoiXacNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucKiemTraThayDoi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MucKiemTraThayDoi_PhieuThayDois_PhieuThayDoiId",
                        column: x => x.PhieuThayDoiId,
                        principalTable: "PhieuThayDois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MucPcra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuThayDoiId = table.Column<int>(type: "int", nullable: false),
                    Nhom = table.Column<int>(type: "int", nullable: false),
                    ApDung = table.Column<bool>(type: "bit", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucPcra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MucPcra_PhieuThayDois_PhieuThayDoiId",
                        column: x => x.PhieuThayDoiId,
                        principalTable: "PhieuThayDois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuThayDoiHeThong",
                columns: table => new
                {
                    PhieuThayDoiId = table.Column<int>(type: "int", nullable: false),
                    HeThongId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuThayDoiHeThong", x => new { x.PhieuThayDoiId, x.HeThongId });
                    table.ForeignKey(
                        name: "FK_PhieuThayDoiHeThong_HeThongThongTins_HeThongId",
                        column: x => x.HeThongId,
                        principalTable: "HeThongThongTins",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuThayDoiHeThong_PhieuThayDois_PhieuThayDoiId",
                        column: x => x.PhieuThayDoiId,
                        principalTable: "PhieuThayDois",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DanhGiaSauThayDois_PhieuThayDoiId",
                table: "DanhGiaSauThayDois",
                column: "PhieuThayDoiId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DuAnXayDungs_PhieuThayDoiId",
                table: "DuAnXayDungs",
                column: "PhieuThayDoiId");

            migrationBuilder.CreateIndex(
                name: "IX_MucKiemTraThayDoi_PhieuThayDoiId",
                table: "MucKiemTraThayDoi",
                column: "PhieuThayDoiId");

            migrationBuilder.CreateIndex(
                name: "IX_MucPcra_PhieuThayDoiId",
                table: "MucPcra",
                column: "PhieuThayDoiId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDoiHeThong_HeThongId",
                table: "PhieuThayDoiHeThong",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDois_HeThongId",
                table: "PhieuThayDois",
                column: "HeThongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDois_Ma",
                table: "PhieuThayDois",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDois_SuKienDowntimeId",
                table: "PhieuThayDois",
                column: "SuKienDowntimeId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDois_TaiSanId",
                table: "PhieuThayDois",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuThayDois_TrangThai",
                table: "PhieuThayDois",
                column: "TrangThai");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGiaSauThayDois");

            migrationBuilder.DropTable(
                name: "DuAnXayDungs");

            migrationBuilder.DropTable(
                name: "MucKiemTraThayDoi");

            migrationBuilder.DropTable(
                name: "MucPcra");

            migrationBuilder.DropTable(
                name: "PhieuThayDoiHeThong");

            migrationBuilder.DropTable(
                name: "ThayDoiChuanMaus");

            migrationBuilder.DropTable(
                name: "PhieuThayDois");
        }
    }
}
