param([string]$Configuration = "Release", [switch]$SkipBuild)
. (Join-Path $PSScriptRoot "common.ps1") -Configuration $Configuration -SkipBuild:$SkipBuild
$report = Initialize-TestRun "UI"
$env:QUANLYHOSO_UI_EXE = Join-Path $RepositoryRoot "bin\$Configuration\net5.0-windows\QuanLyHoSo.exe"
try {
    Invoke-TestProject "tests\QuanLyHoSo.UITests\QuanLyHoSo.UITests.csproj" $report "Category=UI"
}
finally {
    Remove-Item Env:\QUANLYHOSO_UI_EXE -ErrorAction SilentlyContinue
}
