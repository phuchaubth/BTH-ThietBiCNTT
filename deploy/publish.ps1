<#
.SYNOPSIS
  Đóng gói bản cài đặt để chép lên máy chủ IIS.
.DESCRIPTION
  Chạy trên máy phát triển (đã cài .NET 10 SDK), tại thư mục gốc repo:
      powershell -ExecutionPolicy Bypass -File deploy\publish.ps1
  Kết quả: thư mục .\out\ThietBiCNTT_<ngày-giờ>\app và file .zip cùng tên, kèm script SQL migration (idempotent).
#>
param(
    [string]$OutRoot = ".\out",
    [switch]$KhongTaoSql
)
$ErrorActionPreference = "Stop"
$stamp = Get-Date -Format "yyyyMMdd_HHmm"
$dest  = Join-Path $OutRoot "ThietBiCNTT_$stamp"
$app   = Join-Path $dest "app"

Write-Host "==> Kiểm thử quy tắc nghiệp vụ" -ForegroundColor Cyan
dotnet test tests\ThietBiCNTT.Tests -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "Unit test thất bại — dừng đóng gói." }

Write-Host "==> Publish (Release, win-x64, framework-dependent)" -ForegroundColor Cyan
dotnet publish src\ThietBiCNTT.Web -c Release -r win-x64 --self-contained false -o $app --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish thất bại." }

# Không mang cấu hình môi trường dev/demo lên máy chủ
Remove-Item (Join-Path $app "appsettings.Development.json"), (Join-Path $app "appsettings.Demo.json") -ErrorAction SilentlyContinue

if (-not $KhongTaoSql) {
    Write-Host "==> Xuất script SQL migration (idempotent)" -ForegroundColor Cyan
    dotnet tool update --global dotnet-ef | Out-Null
    dotnet ef migrations script --idempotent -p src\ThietBiCNTT.Infrastructure -o (Join-Path $dest "migration-idempotent.sql")
}

Copy-Item deploy\appsettings.Production.mau.json $dest
Copy-Item deploy\caidat-iis.ps1, deploy\capnhat.ps1 $dest
Copy-Item deploy\sql -Destination $dest -Recurse

Compress-Archive -Path "$dest\*" -DestinationPath "$dest.zip" -Force
Write-Host "Xong: $dest.zip" -ForegroundColor Green
