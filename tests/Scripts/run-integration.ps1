param([string]$Configuration = "Release", [switch]$SkipBuild)
. (Join-Path $PSScriptRoot "common.ps1") -Configuration $Configuration -SkipBuild:$SkipBuild
$report = Initialize-TestRun "Integration"
Invoke-TestProject "tests\QuanLyHoSo.IntegrationTests\QuanLyHoSo.IntegrationTests.csproj" $report ""
