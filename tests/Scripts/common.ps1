param(
    [string]$Configuration = "Release",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$script:RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$script:SolutionPath = Join-Path $script:RepositoryRoot "QuanLyHoSo.sln"
$script:ReportsPath = Join-Path $script:RepositoryRoot "tests\Reports"
$script:TestConfiguration = $Configuration

function Initialize-TestRun {
    param([string]$SuiteName)

    New-Item -ItemType Directory -Force -Path $script:ReportsPath | Out-Null
    $reportsRoot = [System.IO.Path]::GetFullPath($script:ReportsPath).TrimEnd('\') + '\'
    $suiteReportPath = [System.IO.Path]::GetFullPath((Join-Path $script:ReportsPath $SuiteName))
    if (-not $suiteReportPath.StartsWith($reportsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean a report path outside tests/Reports: $suiteReportPath"
    }
    if (Test-Path -LiteralPath $suiteReportPath) {
        Remove-Item -LiteralPath $suiteReportPath -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $suiteReportPath | Out-Null

    if (-not $SkipBuild) {
        & dotnet build $script:SolutionPath --configuration $script:TestConfiguration | Out-Host
        if ($LASTEXITCODE -ne 0) { throw "Solution build failed with exit code $LASTEXITCODE." }
    }

    return $suiteReportPath
}

function Invoke-TestProject {
    param(
        [string]$Project,
        [string]$ReportPath,
        [string]$Filter,
        [switch]$CollectCoverage
    )

    $arguments = @(
        "test",
        (Join-Path $script:RepositoryRoot $Project),
        "--configuration", $script:TestConfiguration,
        "--no-build",
        "--logger", "trx",
        "--results-directory", $ReportPath
    )
    if (-not [string]::IsNullOrWhiteSpace($Filter)) {
        $arguments += @("--filter", $Filter)
    }
    if ($CollectCoverage) {
        $arguments += @("--collect", "XPlat Code Coverage")
    }

    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { throw "Test project failed with exit code ${LASTEXITCODE}: $Project" }
}
