param(
    [string]$Configuration = "Release",
    [switch]$IncludeUI
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
& dotnet build (Join-Path $root "QuanLyHoSo.sln") --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& (Join-Path $PSScriptRoot "run-unit.ps1") -Configuration $Configuration -SkipBuild
if (-not $?) { throw "Unit test script failed." }
& (Join-Path $PSScriptRoot "run-integration.ps1") -Configuration $Configuration -SkipBuild
if (-not $?) { throw "Integration test script failed." }

if ($IncludeUI) {
    & (Join-Path $PSScriptRoot "run-ui.ps1") -Configuration $Configuration -SkipBuild
    if (-not $?) { throw "UI test script failed." }
}
