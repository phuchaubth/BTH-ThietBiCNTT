using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThietBiCNTT.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeXuatThanhLys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    HoiDong = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    KetLuanHoiDong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SoQuyetDinh = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NgayQuyetDinh = table.Column<DateOnly>(type: "date", nullable: true),
                    HinhThuc = table.Column<int>(type: "int", nullable: true),
                    GiaTriThuHoi = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    NgayHoanTat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeXuatThanhLys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KhoaPhongs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TruongKhoa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LaKhoaTrongYeu = table.Column<bool>(type: "bit", nullable: false),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KhoaPhongs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoaiThietBis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nhom = table.Column<int>(type: "int", nullable: false),
                    CoLuuTruDuLieu = table.Column<bool>(type: "bit", nullable: false),
                    TuoiThoNam = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoaiThietBis", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NgayNghis",
                columns: table => new
                {
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NgayNghis", x => x.Ngay);
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    MaSoThue = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguoiLienHe = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LaDonViBaoHanh = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NhatKyHeThongs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HanhDong = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DoiTuong = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoiTuongId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ChiTiet = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKyHeThongs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ThamSoHeThongs",
                columns: table => new
                {
                    Ma = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nhom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Kieu = table.Column<int>(type: "int", nullable: false),
                    GiaTri = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    GiaTriMacDinh = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DonVi = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ToiThieu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ToiDa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ThuTu = table.Column<int>(type: "int", nullable: false),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThamSoHeThongs", x => x.Ma);
                });

            migrationBuilder.CreateTable(
                name: "VatTus",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ma = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    DonViTinh = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TonKho = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TonToiThieu = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatTus", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    DangHoatDong = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ViTris",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Loai = table.Column<int>(type: "int", nullable: false),
                    ChaId = table.Column<int>(type: "int", nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViTris", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ViTris_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViTris_ViTris_ChaId",
                        column: x => x.ChaId,
                        principalTable: "ViTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuDieuChuyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TuKhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    DenKhoaPhongId = table.Column<int>(type: "int", nullable: false),
                    DenViTriId = table.Column<int>(type: "int", nullable: true),
                    NguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LyDo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    TongNguyenGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    CapHienTai = table.Column<int>(type: "int", nullable: false),
                    CapCuoi = table.Column<int>(type: "int", nullable: false),
                    NgayBanGiao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LuotGui = table.Column<int>(type: "int", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuDieuChuyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyens_KhoaPhongs_DenKhoaPhongId",
                        column: x => x.DenKhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyens_KhoaPhongs_TuKhoaPhongId",
                        column: x => x.TuKhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyens_ViTris_DenViTriId",
                        column: x => x.DenViTriId,
                        principalTable: "ViTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaiSans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiSan = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Ten = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: false),
                    Hang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoSerial = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NguonVon = table.Column<int>(type: "int", nullable: false),
                    NguyenGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    NgayMua = table.Column<DateOnly>(type: "date", nullable: true),
                    NgaySuDung = table.Column<DateOnly>(type: "date", nullable: true),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    HanBaoHanh = table.Column<DateOnly>(type: "date", nullable: true),
                    SoHopDong = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    ViTriId = table.Column<int>(type: "int", nullable: true),
                    NguoiSuDung = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaiSanChaId = table.Column<int>(type: "int", nullable: true),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    ChiSoBoDem = table.Column<long>(type: "bigint", nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiSans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaiSans_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiSans_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiSans_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiSans_TaiSans_TaiSanChaId",
                        column: x => x.TaiSanChaId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaiSans_ViTris_ViTriId",
                        column: x => x.ViTriId,
                        principalTable: "ViTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BuocDuyetDieuChuyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuDieuChuyenId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BuocDuyetDieuChuyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BuocDuyetDieuChuyens_PhieuDieuChuyens_PhieuDieuChuyenId",
                        column: x => x.PhieuDieuChuyenId,
                        principalTable: "PhieuDieuChuyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CauHinhs",
                columns: table => new
                {
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    CPU = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    RAM = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OCung = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    HeDieuHanh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BanQuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DiaChiIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    DiaChiMAC = table.Column<string>(type: "nvarchar(17)", maxLength: 17, nullable: true),
                    Hostname = table.Column<string>(type: "nvarchar(63)", maxLength: 63, nullable: true),
                    PhanMem = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauHinhs", x => x.TaiSanId);
                    table.ForeignKey(
                        name: "FK_CauHinhs_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDieuChuyens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuDieuChuyenId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDieuChuyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietDieuChuyens_PhieuDieuChuyens_PhieuDieuChuyenId",
                        column: x => x.PhieuDieuChuyenId,
                        principalTable: "PhieuDieuChuyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietDieuChuyens_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietThanhLys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DeXuatThanhLyId = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GiaTriConLai = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    DaThaoOCung = table.Column<bool>(type: "bit", nullable: false),
                    SerialOCung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NguoiThao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayThao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoiLuuOCung = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    XuLyOCung = table.Column<int>(type: "int", nullable: true),
                    BienBanHuy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NgayHuyOCung = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietThanhLys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietThanhLys_DeXuatThanhLys_DeXuatThanhLyId",
                        column: x => x.DeXuatThanhLyId,
                        principalTable: "DeXuatThanhLys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietThanhLys_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KeHoachBaoTris",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ten = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LoaiThietBiId = table.Column<int>(type: "int", nullable: true),
                    TaiSanId = table.Column<int>(type: "int", nullable: true),
                    ChuKyNgay = table.Column<int>(type: "int", nullable: false),
                    NgayTiepTheo = table.Column<DateOnly>(type: "date", nullable: false),
                    Checklist = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    KyThuatVien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DangApDung = table.Column<bool>(type: "bit", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeHoachBaoTris", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KeHoachBaoTris_LoaiThietBis_LoaiThietBiId",
                        column: x => x.LoaiThietBiId,
                        principalTable: "LoaiThietBis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KeHoachBaoTris_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LichSuTaiSans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LoaiSuKien = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NguoiThucHien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuTaiSans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LichSuTaiSans_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuSuaChuas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoPhieu = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LoaiCongViec = table.Column<int>(type: "int", nullable: false),
                    TaiSanId = table.Column<int>(type: "int", nullable: false),
                    KhoaPhongId = table.Column<int>(type: "int", nullable: true),
                    NguoiYeuCau = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DienThoaiLienHe = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MoTaSuCo = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    UuTien = table.Column<int>(type: "int", nullable: false),
                    TrangThai = table.Column<int>(type: "int", nullable: false),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanXuLy = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NgayTiepNhan = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayBatDau = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayHoanThanh = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayNghiemThu = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BatDauTamDung = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TongTamDungGiay = table.Column<long>(type: "bigint", nullable: false),
                    KyThuatVien = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HinhThucXuLy = table.Column<int>(type: "int", nullable: true),
                    NhaCungCapId = table.Column<int>(type: "int", nullable: true),
                    NguyenNhan = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BienPhapKhacPhuc = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GioCong = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ChiPhiThueNgoai = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false),
                    NguoiNghiemThu = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DanhGia = table.Column<int>(type: "int", nullable: true),
                    YKienNguoiDung = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    KeHoachBaoTriId = table.Column<int>(type: "int", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiTao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NguoiCapNhat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuSuaChuas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhieuSuaChuas_KeHoachBaoTris_KeHoachBaoTriId",
                        column: x => x.KeHoachBaoTriId,
                        principalTable: "KeHoachBaoTris",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuSuaChuas_KhoaPhongs_KhoaPhongId",
                        column: x => x.KhoaPhongId,
                        principalTable: "KhoaPhongs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuSuaChuas_NhaCungCaps_NhaCungCapId",
                        column: x => x.NhaCungCapId,
                        principalTable: "NhaCungCaps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuSuaChuas_TaiSans_TaiSanId",
                        column: x => x.TaiSanId,
                        principalTable: "TaiSans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BinhLuanPhieus",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NguoiViet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LaHeThong = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BinhLuanPhieus", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BinhLuanPhieus_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietVatTuSuaChuas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuSuaChuaId = table.Column<int>(type: "int", nullable: false),
                    VatTuId = table.Column<int>(type: "int", nullable: false),
                    SoLuong = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,2)", precision: 18, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietVatTuSuaChuas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChiTietVatTuSuaChuas_PhieuSuaChuas_PhieuSuaChuaId",
                        column: x => x.PhieuSuaChuaId,
                        principalTable: "PhieuSuaChuas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietVatTuSuaChuas_VatTus_VatTuId",
                        column: x => x.VatTuId,
                        principalTable: "VatTus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_KhoaPhongId",
                table: "AspNetUsers",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BinhLuanPhieus_PhieuSuaChuaId",
                table: "BinhLuanPhieus",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_BuocDuyetDieuChuyens_PhieuDieuChuyenId",
                table: "BuocDuyetDieuChuyens",
                column: "PhieuDieuChuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_CauHinhs_DiaChiIP",
                table: "CauHinhs",
                column: "DiaChiIP");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDieuChuyens_PhieuDieuChuyenId",
                table: "ChiTietDieuChuyens",
                column: "PhieuDieuChuyenId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDieuChuyens_TaiSanId",
                table: "ChiTietDieuChuyens",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhLys_DeXuatThanhLyId",
                table: "ChiTietThanhLys",
                column: "DeXuatThanhLyId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhLys_SerialOCung",
                table: "ChiTietThanhLys",
                column: "SerialOCung");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietThanhLys_TaiSanId",
                table: "ChiTietThanhLys",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietVatTuSuaChuas_PhieuSuaChuaId",
                table: "ChiTietVatTuSuaChuas",
                column: "PhieuSuaChuaId");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietVatTuSuaChuas_VatTuId",
                table: "ChiTietVatTuSuaChuas",
                column: "VatTuId");

            migrationBuilder.CreateIndex(
                name: "IX_DeXuatThanhLys_SoPhieu",
                table: "DeXuatThanhLys",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachBaoTris_LoaiThietBiId",
                table: "KeHoachBaoTris",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_KeHoachBaoTris_TaiSanId",
                table: "KeHoachBaoTris",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_KhoaPhongs_Ma",
                table: "KhoaPhongs",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichSuTaiSans_TaiSanId_ThoiGian",
                table: "LichSuTaiSans",
                columns: new[] { "TaiSanId", "ThoiGian" });

            migrationBuilder.CreateIndex(
                name: "IX_LoaiThietBis_Ma",
                table: "LoaiThietBis",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NhatKyHeThongs_ThoiGian",
                table: "NhatKyHeThongs",
                column: "ThoiGian");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyens_DenKhoaPhongId",
                table: "PhieuDieuChuyens",
                column: "DenKhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyens_DenViTriId",
                table: "PhieuDieuChuyens",
                column: "DenViTriId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyens_SoPhieu",
                table: "PhieuDieuChuyens",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyens_TrangThai_CapHienTai",
                table: "PhieuDieuChuyens",
                columns: new[] { "TrangThai", "CapHienTai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyens_TuKhoaPhongId",
                table: "PhieuDieuChuyens",
                column: "TuKhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_KeHoachBaoTriId",
                table: "PhieuSuaChuas",
                column: "KeHoachBaoTriId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_KhoaPhongId",
                table: "PhieuSuaChuas",
                column: "KhoaPhongId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_NhaCungCapId",
                table: "PhieuSuaChuas",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_SoPhieu",
                table: "PhieuSuaChuas",
                column: "SoPhieu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_TaiSanId",
                table: "PhieuSuaChuas",
                column: "TaiSanId");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuSuaChuas_TrangThai_HanXuLy",
                table: "PhieuSuaChuas",
                columns: new[] { "TrangThai", "HanXuLy" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_KhoaPhongId_TrangThai",
                table: "TaiSans",
                columns: new[] { "KhoaPhongId", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_LoaiThietBiId",
                table: "TaiSans",
                column: "LoaiThietBiId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_MaTaiSan",
                table: "TaiSans",
                column: "MaTaiSan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_NhaCungCapId",
                table: "TaiSans",
                column: "NhaCungCapId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_SoSerial",
                table: "TaiSans",
                column: "SoSerial");

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_TaiSanChaId",
                table: "TaiSans",
                column: "TaiSanChaId");

            migrationBuilder.CreateIndex(
                name: "IX_TaiSans_ViTriId",
                table: "TaiSans",
                column: "ViTriId");

            migrationBuilder.CreateIndex(
                name: "IX_VatTus_Ma",
                table: "VatTus",
                column: "Ma",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ViTris_ChaId",
                table: "ViTris",
                column: "ChaId");

            migrationBuilder.CreateIndex(
                name: "IX_ViTris_KhoaPhongId",
                table: "ViTris",
                column: "KhoaPhongId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BinhLuanPhieus");

            migrationBuilder.DropTable(
                name: "BuocDuyetDieuChuyens");

            migrationBuilder.DropTable(
                name: "CauHinhs");

            migrationBuilder.DropTable(
                name: "ChiTietDieuChuyens");

            migrationBuilder.DropTable(
                name: "ChiTietThanhLys");

            migrationBuilder.DropTable(
                name: "ChiTietVatTuSuaChuas");

            migrationBuilder.DropTable(
                name: "LichSuTaiSans");

            migrationBuilder.DropTable(
                name: "NgayNghis");

            migrationBuilder.DropTable(
                name: "NhatKyHeThongs");

            migrationBuilder.DropTable(
                name: "ThamSoHeThongs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "PhieuDieuChuyens");

            migrationBuilder.DropTable(
                name: "DeXuatThanhLys");

            migrationBuilder.DropTable(
                name: "PhieuSuaChuas");

            migrationBuilder.DropTable(
                name: "VatTus");

            migrationBuilder.DropTable(
                name: "KeHoachBaoTris");

            migrationBuilder.DropTable(
                name: "TaiSans");

            migrationBuilder.DropTable(
                name: "LoaiThietBis");

            migrationBuilder.DropTable(
                name: "NhaCungCaps");

            migrationBuilder.DropTable(
                name: "ViTris");

            migrationBuilder.DropTable(
                name: "KhoaPhongs");
        }
    }
}
