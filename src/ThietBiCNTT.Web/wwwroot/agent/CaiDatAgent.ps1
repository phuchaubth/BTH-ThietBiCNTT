<#
.SYNOPSIS
  Cài agent thu thập cấu hình trên máy trạm: chép script, ghi cấu hình registry, tạo Scheduled Task chạy dưới SYSTEM
  (khi khởi động máy + hằng ngày lúc 10:00, trễ ngẫu nhiên tối đa 60 phút để không dồn tải).
.DESCRIPTION
  Dùng làm Computer Startup Script trong GPO, hoặc chạy tay với quyền Administrator.
  Đặt ThuThapCauHinh.ps1 cùng thư mục với script này (VD \\bth.local\NETLOGON\agent\).
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File \\bth.local\NETLOGON\agent\CaiDatAgent.ps1 -Url http://thietbi.bth.org.vn -ApiKey tbk_xxx
.EXAMPLE
  ... -MaTaiSan PC-0123     # khai báo mã tài sản (theo tem QR) cho máy này
.EXAMPLE
  ... -GoCaiDat             # gỡ agent
#>
[CmdletBinding()]
param(
    [string]$Url,
    [string]$ApiKey,
    [string]$MaTaiSan,
    [string]$Gio = '10:00',
    [switch]$GoCaiDat
)
$ErrorActionPreference = 'Stop'
$ThuMuc = Join-Path $env:ProgramData 'BTH\ThietBiAgent'
$RegKey = 'HKLM:\SOFTWARE\BTH\ThietBi'
$TenTask = 'BTH - Thu thap cau hinh may tram'

if ($GoCaiDat) {
    Unregister-ScheduledTask -TaskName $TenTask -Confirm:$false -ErrorAction SilentlyContinue
    Remove-Item $ThuMuc -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item $RegKey -Recurse -Force -ErrorAction SilentlyContinue
    Write-Output 'Đã gỡ agent.'; return
}
if (-not $Url -or -not $ApiKey) { throw 'Cần -Url và -ApiKey.' }

New-Item -ItemType Directory -Path $ThuMuc -Force | Out-Null
Copy-Item (Join-Path $PSScriptRoot 'ThuThapCauHinh.ps1') $ThuMuc -Force
# Chỉ SYSTEM và Administrators đọc được thư mục / registry chứa khoá API
icacls $ThuMuc /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' | Out-Null

New-Item -Path $RegKey -Force | Out-Null
Set-ItemProperty -Path $RegKey -Name Url -Value $Url
Set-ItemProperty -Path $RegKey -Name ApiKey -Value $ApiKey
if ($MaTaiSan) { Set-ItemProperty -Path $RegKey -Name MaTaiSan -Value $MaTaiSan }
$acl = Get-Acl $RegKey
$acl.SetAccessRuleProtection($true, $false)
foreach ($ai in 'NT AUTHORITY\SYSTEM', 'BUILTIN\Administrators') {
    $acl.AddAccessRule((New-Object System.Security.AccessControl.RegistryAccessRule($ai, 'FullControl', 'ContainerInherit,ObjectInherit', 'None', 'Allow')))
}
Set-Acl -Path $RegKey -AclObject $acl

$hanhDong = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument "-NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File `"$ThuMuc\ThuThapCauHinh.ps1`""
$khoiDong = New-ScheduledTaskTrigger -AtStartup; $khoiDong.Delay = 'PT10M'
$hangNgay = New-ScheduledTaskTrigger -Daily -At $Gio -RandomDelay (New-TimeSpan -Minutes 60)
$caiDat   = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 15)
$chinh    = New-ScheduledTaskPrincipal -UserId 'SYSTEM' -LogonType ServiceAccount -RunLevel Highest
Register-ScheduledTask -TaskName $TenTask -Action $hanhDong -Trigger $khoiDong, $hangNgay -Settings $caiDat -Principal $chinh -Force | Out-Null
Start-ScheduledTask -TaskName $TenTask
Write-Output "Đã cài agent ($ThuMuc). Task: $TenTask — chạy lần đầu ngay bây giờ."
