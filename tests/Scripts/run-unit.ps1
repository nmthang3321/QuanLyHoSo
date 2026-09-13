param([string]$Configuration = "Release", [switch]$SkipBuild)
. (Join-Path $PSScriptRoot "common.ps1") -Configuration $Configuration -SkipBuild:$SkipBuild
$report = Initialize-TestRun "Unit"
Invoke-TestProject "tests\QuanLyHoSo.UnitTests\QuanLyHoSo.UnitTests.csproj" $report "Category=Unit"
