param([string]$Configuration = "Release", [switch]$SkipBuild)
. (Join-Path $PSScriptRoot "common.ps1") -Configuration $Configuration -SkipBuild:$SkipBuild
$report = Initialize-TestRun "Regression"
Invoke-TestProject "tests\QuanLyHoSo.UnitTests\QuanLyHoSo.UnitTests.csproj" $report "" -CollectCoverage
Invoke-TestProject "tests\QuanLyHoSo.IntegrationTests\QuanLyHoSo.IntegrationTests.csproj" $report "" -CollectCoverage
