param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$artifactRoot = Join-Path $repoRoot 'artifacts'
$publishDir = Join-Path $artifactRoot 'publish-win-x64'
$installerDir = Join-Path $artifactRoot 'installer'
$updateZip = Join-Path $artifactRoot "QuanLyHoSo-$Version-win-x64-update.zip"
$issPath = Join-Path $repoRoot 'installer\QuanLyHoSo.iss'
$iconPath = Join-Path $repoRoot 'Assets\AppIcon.ico'

Remove-Item -LiteralPath $artifactRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
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
        -o $publishDir

    Compress-Archive -Path (Join-Path $publishDir '*') -DestinationPath $updateZip -Force

    $innoCandidates = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe"
    )
    $iscc = $innoCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($iscc) {
        & $iscc `
            /DAppVersion=$Version `
            /DPublishDir="$publishDir" `
            /DIconPath="$iconPath" `
            /O"$installerDir" `
            $issPath
    } else {
        Write-Warning 'Inno Setup 6 was not found. Update zip was created, but installer exe was skipped.'
        Write-Warning 'Install Inno Setup 6, then run this script again to create Setup.exe.'
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
