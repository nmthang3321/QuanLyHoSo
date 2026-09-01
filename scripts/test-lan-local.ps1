param(
    [string]$AdminServerUrl = "http://localhost:5055",
    [string]$BuildOutput = ".lan-test-build"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "QuanLyHoSo.csproj"
$runId = "run-{0:yyyyMMddHHmmss}" -f (Get-Date)
$outputPath = Join-Path (Join-Path $repoRoot $BuildOutput) $runId
$exePath = Join-Path $outputPath "QuanLyHoSo.exe"
$settingsFolder = Join-Path $env:LOCALAPPDATA "QuanLyHoSo\Settings"
$settingsPath = Join-Path $settingsFolder "path-settings.json"
$backupPath = Join-Path $settingsFolder ("path-settings.before-lan-test.{0:yyyyMMddHHmmss}.json" -f (Get-Date))
$databasePath = Join-Path $env:LOCALAPPDATA "QuanLyHoSo\Data\quanlyhoso.db"
$logFolder = Join-Path $env:LOCALAPPDATA "QuanLyHoSo\Logs"

function Write-SettingsFile {
    param(
        [string]$Mode,
        [string]$MachineName
    )

    $settings = [ordered]@{
        DatabasePath = $databasePath
        LogFolder = $logFolder
        DataAccessMode = $Mode
        AdminMachineName = $MachineName
        AdminServerUrl = $AdminServerUrl.TrimEnd("/")
    }

    New-Item -ItemType Directory -Force -Path $settingsFolder | Out-Null
    $settings | ConvertTo-Json -Depth 5 | Set-Content -Path $settingsPath -Encoding UTF8
}

function Wait-AdminServer {
    $healthUrl = $AdminServerUrl.TrimEnd("/") + "/api/health"
    $deadline = (Get-Date).AddSeconds(20)

    while ((Get-Date) -lt $deadline) {
        try {
            Invoke-RestMethod -Method Post -Uri $healthUrl -ContentType "application/json" -Body "{}" | Out-Null
            return
        }
        catch {
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Khong ket noi duoc admin server tai $healthUrl. Hay kiem tra app admin co chay duoc khong."
}

Push-Location $repoRoot
try {
    Write-Host "Building app..." -ForegroundColor Cyan
    dotnet build $projectPath -o $outputPath

    if (!(Test-Path $exePath)) {
        throw "Khong tim thay file app: $exePath"
    }

    New-Item -ItemType Directory -Force -Path $settingsFolder | Out-Null
    if (Test-Path $settingsPath) {
        Copy-Item -Path $settingsPath -Destination $backupPath -Force
        Write-Host "Backed up settings: $backupPath" -ForegroundColor DarkGray
    }

    Write-SettingsFile -Mode "AdminHost" -MachineName $env:COMPUTERNAME
    Write-Host "Starting ADMIN app..." -ForegroundColor Cyan
    $adminProcess = Start-Process -FilePath $exePath -WorkingDirectory $outputPath -PassThru

    Write-Host "Waiting for admin API: $AdminServerUrl" -ForegroundColor Cyan
    Wait-AdminServer

    Write-SettingsFile -Mode "Client" -MachineName $env:COMPUTERNAME
    Write-Host "Starting CLIENT app..." -ForegroundColor Cyan
    $clientProcess = Start-Process -FilePath $exePath -WorkingDirectory $outputPath -PassThru

    Write-Host ""
    Write-Host "LAN local test is running." -ForegroundColor Green
    Write-Host "ADMIN pid : $($adminProcess.Id)"
    Write-Host "CLIENT pid: $($clientProcess.Id)"
    Write-Host ""
    Write-Host "Close both app windows when done. This script will restore path-settings.json."

    Wait-Process -Id $adminProcess.Id, $clientProcess.Id -ErrorAction SilentlyContinue
}
finally {
    if (Test-Path $backupPath) {
        Copy-Item -Path $backupPath -Destination $settingsPath -Force
        Write-Host "Restored original settings." -ForegroundColor Yellow
    }
    Pop-Location
}
