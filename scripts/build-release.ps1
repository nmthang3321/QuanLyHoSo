param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$artifactRoot = Join-Path $repoRoot 'artifacts'
$clientPublishDir = Join-Path $artifactRoot 'publish-client-win-x64'
$serverPublishDir = Join-Path $artifactRoot 'publish-server-win-x64'
$installerDir = Join-Path $artifactRoot 'installer'
$clientUpdateZip = Join-Path $artifactRoot "QuanLyHoSo-Client-$Version-win-x64-update.zip"
$serverPackageZip = Join-Path $artifactRoot "QuanLyHoSo-Server-$Version-win-x64.zip"
$clientIssPath = Join-Path $repoRoot 'installer\QuanLyHoSo.Client.iss'
$serverIssPath = Join-Path $repoRoot 'installer\QuanLyHoSo.Server.iss'
$iconPath = Join-Path $repoRoot 'Assets\AppIcon.ico'

if (-not $artifactRoot.StartsWith($repoRoot.Path, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Artifact path is outside the repository: $artifactRoot"
}

Remove-Item -LiteralPath $artifactRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $clientPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $serverPublishDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

Push-Location $repoRoot
try {
    dotnet publish .\QuanLyHoSo.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:Version=$Version `
        /p:AssemblyVersion=$Version.0 `
        /p:FileVersion=$Version.0 `
        -o $clientPublishDir

    dotnet publish .\QuanLyHoSo.Server\QuanLyHoSo.Server.csproj `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:Version=$Version `
        /p:AssemblyVersion=$Version.0 `
        /p:FileVersion=$Version.0 `
        -o $serverPublishDir

    Compress-Archive -Path (Join-Path $clientPublishDir '*') -DestinationPath $clientUpdateZip -Force
    Compress-Archive -Path (Join-Path $serverPublishDir '*') -DestinationPath $serverPackageZip -Force

    $innoCandidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:LOCALAPPDATA}\Programs\Inno Setup 6\ISCC.exe"
    )
    $iscc = $innoCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

    if (-not $iscc) {
        throw 'Inno Setup 6 was not found. Install Inno Setup 6, then run this script again.'
    }

    & $iscc `
        /DAppVersion=$Version `
        /DPublishDir="$clientPublishDir" `
        /DIconPath="$iconPath" `
        /O"$installerDir" `
        $clientIssPath
    if ($LASTEXITCODE -ne 0) {
        throw "Client installer compilation failed with exit code $LASTEXITCODE."
    }

    & $iscc `
        /DAppVersion=$Version `
        /DPublishDir="$serverPublishDir" `
        /DIconPath="$iconPath" `
        /O"$installerDir" `
        $serverIssPath
    if ($LASTEXITCODE -ne 0) {
        throw "Server installer compilation failed with exit code $LASTEXITCODE."
    }

    Write-Host ''
    Write-Host 'Release artifacts:'
    Get-ChildItem -LiteralPath $artifactRoot -Recurse -File |
        Where-Object { $_.Extension -in '.zip', '.exe' } |
        Select-Object FullName, Length |
        Format-Table -AutoSize
} finally {
    Pop-Location
}
