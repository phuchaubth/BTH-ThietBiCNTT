<#
.SYNOPSIS
  Cài đặt lần đầu Quản lý TTB CNTT trên IIS (Windows Server 2019/2022/2025).
.DESCRIPTION
  Chạy PowerShell "Run as Administrator" trên máy chủ web, trong thư mục giải nén gói cài đặt:
      powershell -ExecutionPolicy Bypass -File .\caidat-iis.ps1 -CertThumbprint <thumbprint chứng chỉ thietbi.bth.org.vn>
  Script:
    1. Bật IIS + các tính năng cần thiết (kể cả Application Initialization).
    2. Tạo thư mục D:\ThietBiCNTT\{app,keys,logs,backup}, chép ứng dụng, phân quyền cho app pool.
    3. Tạo app pool (No Managed Code, AlwaysRunning, không idle timeout, recycle 02:00).
    4. Tạo website + binding HTTP (và HTTPS nếu truyền -CertThumbprint), bật preload.
  KHÔNG cài .NET Hosting Bundle (tải riêng — xem tài liệu mục 3.2) và KHÔNG tạo CSDL (xem sql\01-tao-csdl.sql).
#>
param(
    [string]$HostName      = "thietbi.bth.org.vn",
    [string]$Root          = "D:\ThietBiCNTT",
    [string]$SiteName      = "ThietBiCNTT",
    [string]$AppPoolName   = "ThietBiCNTT",
    [int]   $HttpPort      = 80,
    [string]$CertThumbprint = "",                      # chứng chỉ trong LocalMachine\My để bật HTTPS
    [string]$PoolUser = "", [string]$PoolPassword = "" # tuỳ chọn: chạy app pool bằng tài khoản domain (SQL ở máy khác)
)
$ErrorActionPreference = "Stop"

Write-Host "==> 1. Tính năng IIS" -ForegroundColor Cyan
$features = "Web-Server","Web-Default-Doc","Web-Http-Errors","Web-Static-Content","Web-Http-Logging",
            "Web-Stat-Compression","Web-Dyn-Compression","Web-Filtering","Web-AppInit","Web-Mgmt-Console","Web-Scripting-Tools"
if (Get-Command Install-WindowsFeature -ErrorAction SilentlyContinue) { Install-WindowsFeature $features | Out-Null }
else { Enable-WindowsOptionalFeature -Online -All -FeatureName IIS-WebServerRole,IIS-ApplicationInit,IIS-ManagementConsole | Out-Null }
Import-Module WebAdministration

$hostingBundle = Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\IIS Extensions\IIS AspNetCore Module V2" -ErrorAction SilentlyContinue
if (-not $hostingBundle) { Write-Warning "Chưa thấy ASP.NET Core Module V2 — hãy cài '.NET 10 Hosting Bundle' rồi chạy lại 'iisreset'." }

Write-Host "==> 2. Thư mục & chép ứng dụng" -ForegroundColor Cyan
foreach ($d in "app","keys","logs","backup") { New-Item -ItemType Directory -Force -Path (Join-Path $Root $d) | Out-Null }
Copy-Item -Path (Join-Path $PSScriptRoot "sql") -Destination $Root -Recurse -Force   # script sao lưu / khôi phục
$appDir = Join-Path $Root "app"
Copy-Item -Path (Join-Path $PSScriptRoot "app\*") -Destination $appDir -Recurse -Force
$cfg = Join-Path $appDir "appsettings.Production.json"
if (-not (Test-Path $cfg)) {
    Copy-Item (Join-Path $PSScriptRoot "appsettings.Production.mau.json") $cfg
    Write-Warning "Đã tạo $cfg từ file mẫu — MỞ RA SỬA chuỗi kết nối, KeysPath, mật khẩu admin trước khi truy cập."
}

Write-Host "==> 3. App pool $AppPoolName" -ForegroundColor Cyan
if (-not (Test-Path "IIS:\AppPools\$AppPoolName")) { New-WebAppPool -Name $AppPoolName | Out-Null }
$pool = "IIS:\AppPools\$AppPoolName"
Set-ItemProperty $pool managedRuntimeVersion ""                       # No Managed Code
Set-ItemProperty $pool managedPipelineMode Integrated
Set-ItemProperty $pool startMode AlwaysRunning                        # job nền (bảo trì định kỳ, tự nghiệm thu) luôn chạy
Set-ItemProperty $pool processModel.idleTimeout ([TimeSpan]::Zero)
Set-ItemProperty $pool processModel.loadUserProfile $true
Set-ItemProperty $pool recycling.periodicRestart.time ([TimeSpan]::Zero)
Clear-ItemProperty $pool -Name recycling.periodicRestart.schedule
New-ItemProperty $pool -Name recycling.periodicRestart.schedule -Value @{value="02:00:00"} | Out-Null
if ($PoolUser) {
    Set-ItemProperty $pool processModel -Value @{userName=$PoolUser; password=$PoolPassword; identityType=3}
    $identity = $PoolUser
} else {
    Set-ItemProperty $pool processModel.identityType ApplicationPoolIdentity
    $identity = "IIS AppPool\$AppPoolName"
}

Write-Host "==> Phân quyền thư mục cho $identity" -ForegroundColor Cyan
icacls $appDir /grant "${identity}:(OI)(CI)RX" /T /Q | Out-Null
icacls (Join-Path $Root "keys") /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null
icacls (Join-Path $Root "logs") /grant "${identity}:(OI)(CI)M" /T /Q | Out-Null
icacls $cfg /inheritance:r /grant "Administrators:F" "SYSTEM:F" "${identity}:R" /Q | Out-Null   # file chứa chuỗi kết nối

Write-Host "==> 4. Website $SiteName" -ForegroundColor Cyan
if (-not (Test-Path "IIS:\Sites\$SiteName")) {
    New-Website -Name $SiteName -PhysicalPath $appDir -ApplicationPool $AppPoolName -HostHeader $HostName -Port $HttpPort | Out-Null
} else {
    Set-ItemProperty "IIS:\Sites\$SiteName" physicalPath $appDir
    Set-ItemProperty "IIS:\Sites\$SiteName" applicationPool $AppPoolName
}
Set-ItemProperty "IIS:\Sites\$SiteName" applicationDefaults.preloadEnabled $true
Set-WebConfigurationProperty -PSPath "IIS:\Sites\$SiteName" -Filter "system.webServer/directoryBrowse" -Name enabled -Value $false

if ($CertThumbprint) {
    if (-not (Get-WebBinding -Name $SiteName -Protocol https)) {
        New-WebBinding -Name $SiteName -Protocol https -Port 443 -HostHeader $HostName -SslFlags 1
    }
    (Get-WebBinding -Name $SiteName -Protocol https).AddSslCertificate($CertThumbprint, "My")
}

Start-WebAppPool $AppPoolName -ErrorAction SilentlyContinue
Start-Website $SiteName -ErrorAction SilentlyContinue
Write-Host "Xong. Truy cập: http://$HostName  (kiểm tra theo checklist mục 7 của tài liệu)" -ForegroundColor Green
Write-Host "Danh tính app pool cần quyền CSDL: $identity" -ForegroundColor Yellow
