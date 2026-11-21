<#
.SYNOPSIS
  Cập nhật phiên bản mới (giữ nguyên cấu hình appsettings.Production.json, khoá, log).
.DESCRIPTION
  Chạy "Run as Administrator" trong thư mục giải nén gói cài đặt MỚI:
      powershell -ExecutionPolicy Bypass -File .\capnhat.ps1
  Trình tự: sao lưu CSDL → đưa site vào bảo trì (app_offline.htm) → sao lưu thư mục app → chép bản mới → mở lại.
#>
param(
    [string]$Root        = "D:\ThietBiCNTT",
    [string]$AppPoolName = "ThietBiCNTT",
    [string]$SqlServer   = "localhost",
    [string]$Database    = "ThietBiCNTT",
    [switch]$BoQuaSaoLuuCsdl
)
$ErrorActionPreference = "Stop"
$appDir = Join-Path $Root "app"
$stamp  = Get-Date -Format "yyyyMMdd_HHmm"

if (-not $BoQuaSaoLuuCsdl) {
    Write-Host "==> Sao lưu CSDL trước khi cập nhật" -ForegroundColor Cyan
    $bak = Join-Path $Root "backup\${Database}_truoc-capnhat_$stamp.bak"
    sqlcmd -S $SqlServer -E -b -Q "BACKUP DATABASE [$Database] TO DISK = N'$bak' WITH COPY_ONLY, COMPRESSION, INIT"
    if ($LASTEXITCODE -ne 0) { throw "Sao lưu CSDL thất bại — dừng cập nhật. (Dùng -BoQuaSaoLuuCsdl nếu đã sao lưu bằng cách khác.)" }
}

Write-Host "==> Đưa ứng dụng vào chế độ bảo trì" -ForegroundColor Cyan
Set-Content -Path (Join-Path $appDir "app_offline.htm") -Encoding UTF8 -Value '<!doctype html><meta charset="utf-8"><title>Bảo trì</title><div style="font-family:sans-serif;text-align:center;margin-top:20vh"><h2>Hệ thống đang được nâng cấp</h2><p>Vui lòng quay lại sau vài phút. Cần hỗ trợ gấp: liên hệ Phòng CNTT.</p></div>'
Start-Sleep -Seconds 5

Write-Host "==> Sao lưu bản đang chạy" -ForegroundColor Cyan
Compress-Archive -Path "$appDir\*" -DestinationPath (Join-Path $Root "backup\app_$stamp.zip") -Force

Write-Host "==> Chép bản mới (giữ appsettings.Production.json)" -ForegroundColor Cyan
Get-ChildItem (Join-Path $PSScriptRoot "app") | Where-Object Name -ne "appsettings.Production.json" |
    Copy-Item -Destination $appDir -Recurse -Force

Remove-Item (Join-Path $appDir "app_offline.htm")
Restart-WebAppPool $AppPoolName
Write-Host "Đã cập nhật. Ứng dụng tự áp migration CSDL khi khởi động (nếu Database:AutoMigrate = true)." -ForegroundColor Green
Write-Host "Khôi phục nếu lỗi: giải nén backup\app_$stamp.zip vào $appDir và restore file .bak." -ForegroundColor Yellow
