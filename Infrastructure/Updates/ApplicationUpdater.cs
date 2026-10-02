using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace QuanLyHoSo.Infrastructure.Updates
{
    public static class ApplicationUpdater
    {
        public static void Start(
            string packagePath,
            bool requireAdministrator,
            Action shutdownApplication,
            IReadOnlyList<string> restartArguments = null)
        {
            if (string.IsNullOrWhiteSpace(packagePath) || !File.Exists(packagePath))
            {
                throw new FileNotFoundException("Không tìm thấy gói cập nhật.", packagePath);
            }

            var currentProcess = Process.GetCurrentProcess();
            var executablePath = currentProcess.MainModule?.FileName
                ?? throw new InvalidOperationException("Không xác định được file ứng dụng đang chạy.");
            var installDirectory = AppContext.BaseDirectory;
            var scriptPath = Path.Combine(Path.GetTempPath(), $"QuanLyHoSo_Update_{Guid.NewGuid():N}.ps1");
            var logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuanLyHoSo",
                "Logs");

            File.WriteAllText(
                scriptPath,
                BuildUpdaterScript(
                    currentProcess.Id,
                    packagePath,
                    installDirectory,
                    executablePath,
                    restartArguments ?? Array.Empty<string>(),
                    logFolder,
                    scriptPath),
                Encoding.UTF8);

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File {QuoteProcessArgument(scriptPath)}",
                UseShellExecute = requireAdministrator,
                CreateNoWindow = !requireAdministrator,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            if (requireAdministrator)
            {
                startInfo.Verb = "runas";
            }

            Process.Start(startInfo);
            shutdownApplication?.Invoke();
        }

        internal static string BuildUpdaterScript(
            int processId,
            string packagePath,
            string installDirectory,
            string executablePath,
            IReadOnlyList<string> restartArguments,
            string logFolder,
            string scriptPath)
        {
            var argumentArray = string.Join(", ", (restartArguments ?? Array.Empty<string>())
                .Select(QuotePowerShellString));

            return $@"
$ErrorActionPreference = 'Stop'
$processId = {processId}
$packagePath = {QuotePowerShellString(packagePath)}
$installDir = {QuotePowerShellString(installDirectory)}
$exePath = {QuotePowerShellString(executablePath)}
$restartArguments = @({argumentArray})
$logFolder = {QuotePowerShellString(logFolder)}
$scriptPath = {QuotePowerShellString(scriptPath)}
$extractDir = Join-Path ([System.IO.Path]::GetTempPath()) ('QuanLyHoSo_Update_' + [System.Guid]::NewGuid().ToString('N'))
$logPath = Join-Path $logFolder ('update-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')

New-Item -ItemType Directory -Path $logFolder -Force | Out-Null
try {{
    Wait-Process -Id $processId -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 700
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
    Expand-Archive -LiteralPath $packagePath -DestinationPath $extractDir -Force

    $sourceDir = $extractDir
    $children = @(Get-ChildItem -LiteralPath $extractDir)
    $directories = @($children | Where-Object {{ $_.PSIsContainer }})
    $files = @($children | Where-Object {{ -not $_.PSIsContainer }})
    if ($directories.Count -eq 1 -and $files.Count -eq 0) {{
        $sourceDir = $directories[0].FullName
    }}

    Copy-Item -Path (Join-Path $sourceDir '*') -Destination $installDir -Recurse -Force
    if ($restartArguments.Count -gt 0) {{
        Start-Process -FilePath $exePath -ArgumentList $restartArguments
    }} else {{
        Start-Process -FilePath $exePath
    }}
}} catch {{
    ($_ | Out-String) | Set-Content -LiteralPath $logPath -Encoding UTF8
    try {{
        if ($restartArguments.Count -gt 0) {{
            Start-Process -FilePath $exePath -ArgumentList $restartArguments
        }} else {{
            Start-Process -FilePath $exePath
        }}
    }} catch {{
        ($_ | Out-String) | Add-Content -LiteralPath $logPath -Encoding UTF8
    }}
}} finally {{
    Remove-Item -LiteralPath $extractDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $scriptPath -Force -ErrorAction SilentlyContinue
}}
";
        }

        private static string QuotePowerShellString(string value)
        {
            return $"'{(value ?? string.Empty).Replace("'", "''")}'";
        }

        private static string QuoteProcessArgument(string value)
        {
            return $"\"{(value ?? string.Empty).Replace("\"", "\\\"")}\"";
        }
    }
}
