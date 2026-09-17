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
$customerPdfName = "QuanLyHoSo_TaiLieu_KhachHang_$Version.pdf"
$customerPdf = Join-Path $repoRoot "doc\$customerPdfName"
$clientIssPath = Join-Path $repoRoot 'installer\QuanLyHoSo.Client.iss'
$serverIssPath = Join-Path $repoRoot 'installer\QuanLyHoSo.Server.iss'
$iconPath = Join-Path $repoRoot 'Assets\AppIcon.ico'

if ([System.IO.Path]::GetFullPath($artifactRoot) -ne [System.IO.Path]::GetFullPath((Join-Path $repoRoot.Path 'artifacts'))) {
    throw "Artifact path is outside the repository: $artifactRoot"
}

if (-not (Test-Path -LiteralPath $customerPdf -PathType Leaf)) {
    throw "Missing customer PDF for version ${Version}: $customerPdf"
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
    if ($LASTEXITCODE -ne 0) { throw "Client publish failed: $LASTEXITCODE" }

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
    if ($LASTEXITCODE -ne 0) { throw "Server publish failed: $LASTEXITCODE" }

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

    Copy-Item -LiteralPath $customerPdf -Destination (Join-Path $installerDir $customerPdfName)
    $assetNames = @(
        "QuanLyHoSo-Server-Setup-$Version-win-x64.exe",
        "QuanLyHoSo-Client-Setup-$Version-win-x64.exe",
        $customerPdfName
    )
    $checksums = foreach ($assetName in $assetNames) {
        $hash = Get-FileHash -LiteralPath (Join-Path $installerDir $assetName) -Algorithm SHA256
        '{0}  {1}' -f $hash.Hash.ToLowerInvariant(), $assetName
    }
    $checksums | Set-Content -LiteralPath (Join-Path $installerDir 'SHA256.txt') -Encoding ASCII

    Write-Host ''
    Write-Host 'Release artifacts:'
    Get-ChildItem -LiteralPath $installerDir -File |
        Select-Object FullName, Length |
        Format-Table -AutoSize
} finally {
    Pop-Location
}
