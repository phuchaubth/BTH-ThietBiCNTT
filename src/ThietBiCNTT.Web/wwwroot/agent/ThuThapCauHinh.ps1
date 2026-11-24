<#
.SYNOPSIS
  Agent thu thập cấu hình máy trạm → gửi về hệ thống Quản lý TTB CNTT (POST /api/v1/agent/cauhinh).
.DESCRIPTION
  Chạy được trên Windows PowerShell 5.1 (Windows 10/11, Server 2016+), không cần cài thêm gì.
  Thu thập: hostname, serial BIOS, CPU, RAM, ổ đĩa, HĐH, IP/MAC, người đăng nhập, danh sách phần mềm (registry Uninstall).
  Mã tài sản (nếu đã dán tem) đọc từ HKLM:\SOFTWARE\BTH\ThietBi\MaTaiSan — giúp ghép chính xác với hồ sơ tài sản.
  Không thu thập dữ liệu người bệnh hay tệp của người dùng.
.PARAMETER Url
  Địa chỉ hệ thống, VD http://thietbi.bth.org.vn
.PARAMETER ApiKey
  Khoá API phạm vi "cauhinh.ghi" (Quản trị → Tích hợp & API). Nếu bỏ trống, đọc từ HKLM:\SOFTWARE\BTH\ThietBi\ApiKey.
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File ThuThapCauHinh.ps1 -Url http://thietbi.bth.org.vn -ApiKey tbk_xxx
.EXAMPLE
  powershell -File ThuThapCauHinh.ps1 -XuatFile C:\Temp\cauhinh.json   # chỉ xuất JSON để kiểm tra, không gửi
#>
[CmdletBinding()]
param(
    [string]$Url,
    [string]$ApiKey,
    [string]$XuatFile
)
$ErrorActionPreference = 'Stop'
$PhienBan = '1.0'
$RegKey = 'HKLM:\SOFTWARE\BTH\ThietBi'

function Doc-Reg([string]$ten) {
    try { (Get-ItemProperty -Path $RegKey -Name $ten -ErrorAction Stop).$ten } catch { $null }
}
if (-not $Url) { $Url = Doc-Reg 'Url' }
if (-not $ApiKey) { $ApiKey = Doc-Reg 'ApiKey' }

$cs   = Get-CimInstance Win32_ComputerSystem
$bios = Get-CimInstance Win32_BIOS
$os   = Get-CimInstance Win32_OperatingSystem
$cpu  = Get-CimInstance Win32_Processor | Select-Object -First 1
$c    = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"
$dia  = @(Get-CimInstance Win32_DiskDrive | Where-Object { $_.MediaType -notmatch 'Removable' -and $_.InterfaceType -ne 'USB' })
$net  = @(Get-CimInstance Win32_NetworkAdapterConfiguration -Filter 'IPEnabled=True' | Where-Object { $_.MACAddress })

# Loại SSD/HDD (Windows 8+); lỗi thì bỏ qua
$loai = @{}
try { Get-PhysicalDisk | ForEach-Object { $loai[[string]$_.DeviceId] = [string]$_.MediaType } } catch { }
$moTaDia = ($dia | ForEach-Object {
    $kieu = $loai[[string]$_.Index]; if (-not $kieu -or $kieu -eq 'Unspecified') { $kieu = 'Disk' }
    '{0} {1} {2:0}GB' -f $kieu, ($_.Model -replace '\s+', ' ').Trim(), ($_.Size / 1GB)
}) -join '; '

$ip  = @($net | ForEach-Object { $_.IPAddress } | Where-Object { $_ -match '^\d+\.\d+\.\d+\.\d+$' -and $_ -notlike '169.254.*' })
$mac = @($net | ForEach-Object { $_.MACAddress })

$phanMem = New-Object System.Collections.Generic.List[object]
$khoa = @('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
          'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*')
foreach ($k in $khoa) {
    Get-ItemProperty -Path $k -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -and -not $_.SystemComponent -and -not $_.ParentKeyName -and $_.DisplayName -notmatch '^(Security Update|Update for|Hotfix)' } |
        ForEach-Object { $phanMem.Add([ordered]@{ ten = [string]$_.DisplayName; phienBan = [string]$_.DisplayVersion }) }
}

$nguoi = $cs.UserName
if (-not $nguoi) { try { $nguoi = (Get-CimInstance Win32_Process -Filter "Name='explorer.exe'" | Select-Object -First 1 | Invoke-CimMethod -MethodName GetOwner).User } catch { } }

$duLieu = [ordered]@{
    hostname      = $env:COMPUTERNAME
    serialBios    = ([string]$bios.SerialNumber).Trim()
    maTaiSan      = Doc-Reg 'MaTaiSan'
    mac           = $mac
    ip            = $ip
    cpu           = ([string]$cpu.Name -replace '\s+', ' ').Trim()
    ramGb         = [math]::Round($cs.TotalPhysicalMemory / 1GB, 1)
    oCungTongGb   = [math]::Round($c.Size / 1GB, 1)
    oCungTrongGb  = [math]::Round($c.FreeSpace / 1GB, 1)
    oCung         = $moTaDia
    heDieuHanh    = '{0} ({1})' -f $os.Caption.Trim(), $os.Version
    nguoiDangNhap = $nguoi
    phienBanAgent = $PhienBan
    phanMem       = $phanMem
}
$json = $duLieu | ConvertTo-Json -Depth 4 -Compress

if ($XuatFile) { [IO.File]::WriteAllText($XuatFile, $json, (New-Object Text.UTF8Encoding $false)); Write-Output "Đã xuất $XuatFile"; return }
if (-not $Url -or -not $ApiKey) { throw 'Thiếu -Url hoặc -ApiKey (hoặc khai báo trong registry HKLM:\SOFTWARE\BTH\ThietBi).' }

[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
$than = [Text.Encoding]::UTF8.GetBytes($json)
for ($lan = 1; $lan -le 3; $lan++) {
    try {
        $kq = Invoke-RestMethod -Uri ($Url.TrimEnd('/') + '/api/v1/agent/cauhinh') -Method Post -Body $than `
              -ContentType 'application/json; charset=utf-8' -Headers @{ 'X-Api-Key' = $ApiKey } -UseBasicParsing -TimeoutSec 30
        Write-Output ("Đã gửi. Tài sản: {0} ({1}); thay đổi: {2}" -f $kq.maTaiSan, $kq.cachGhep, ($kq.thayDoi -join '; '))
        exit 0
    } catch {
        if ($lan -eq 3) { Write-Error "Gửi thất bại: $($_.Exception.Message)"; exit 1 }
        Start-Sleep -Seconds (30 * $lan + (Get-Random -Maximum 30))
    }
}
