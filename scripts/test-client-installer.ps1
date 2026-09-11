param(
    [Parameter(Mandatory = $true)]
    [string]$InstallerPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resolvedInstaller = (Resolve-Path -LiteralPath $InstallerPath).Path
$testInstallDir = Join-Path $repoRoot 'artifacts\install-test-client'
$settingsPath = Join-Path $env:LOCALAPPDATA 'QuanLyHoSo\Settings\path-settings.json'
$settingsFolder = Split-Path -Parent $settingsPath
$backupPath = Join-Path $env:TEMP 'QuanLyHoSo-path-settings-before-installer-test.json'
$hadSettings = Test-Path -LiteralPath $settingsPath
$serverJob = $null

if (-not $testInstallDir.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Test install path is outside the repository: $testInstallDir"
}

try {
    if ($hadSettings) {
        Copy-Item -LiteralPath $settingsPath -Destination $backupPath -Force
    }

    $serverJob = Start-Job -ScriptBlock {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 51234)
        $listener.Start()
        try {
            $client = $listener.AcceptTcpClient()
            $stream = $client.GetStream()
            $buffer = New-Object byte[] 4096
            [void]$stream.Read($buffer, 0, $buffer.Length)
            $body = '{"ok":true}'
            $response = "HTTP/1.1 200 OK`r`nContent-Type: application/json`r`nContent-Length: $($body.Length)`r`nConnection: close`r`n`r`n$body"
            $bytes = [Text.Encoding]::ASCII.GetBytes($response)
            $stream.Write($bytes, 0, $bytes.Length)
            $stream.Dispose()
            $client.Dispose()
        } finally {
            $listener.Stop()
        }
    }

    Start-Sleep -Milliseconds 500
    $install = Start-Process -FilePath $resolvedInstaller -ArgumentList @(
        '/VERYSILENT',
        '/SUPPRESSMSGBOXES',
        '/NORESTART',
        "/DIR=$testInstallDir",
        '/MERGETASKS=!desktopicon',
        '/SERVERURL=http://127.0.0.1:51234'
    ) -WindowStyle Hidden -Wait -PassThru

    if ($install.ExitCode -ne 0) {
        throw "Client installer returned exit code $($install.ExitCode)."
    }

    $installedExe = Join-Path $testInstallDir 'QuanLyHoSo.exe'
    if (-not (Test-Path -LiteralPath $installedExe)) {
        throw "Installed executable was not found: $installedExe"
    }

    $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    if ($settings.DataAccessMode -ne 'Client' -or $settings.AdminServerUrl -ne 'http://127.0.0.1:51234') {
        throw 'The client installer did not write the expected server settings.'
    }

    [pscustomobject]@{
        SetupExitCode = $install.ExitCode
        InstalledExe = $installedExe
        DataAccessMode = $settings.DataAccessMode
        AdminServerUrl = $settings.AdminServerUrl
    }
} finally {
    if ($serverJob) {
        Stop-Job $serverJob -ErrorAction SilentlyContinue
        Remove-Job $serverJob -Force -ErrorAction SilentlyContinue
    }

    $uninstaller = Join-Path $testInstallDir 'unins000.exe'
    if (Test-Path -LiteralPath $uninstaller) {
        Start-Process -FilePath $uninstaller -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART' -WindowStyle Hidden -Wait
    }

    if ($hadSettings -and (Test-Path -LiteralPath $backupPath)) {
        New-Item -ItemType Directory -Path $settingsFolder -Force | Out-Null
        Copy-Item -LiteralPath $backupPath -Destination $settingsPath -Force
        Remove-Item -LiteralPath $backupPath -Force
    } elseif (-not $hadSettings -and (Test-Path -LiteralPath $settingsPath)) {
        Remove-Item -LiteralPath $settingsPath -Force
    }
}
