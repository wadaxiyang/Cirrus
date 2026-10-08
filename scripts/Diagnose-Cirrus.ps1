# Cirrus portable preview startup diagnostics (run from extracted release folder).
# Review the generated report for personal file paths before sharing it.
$ErrorActionPreference = "Continue"
$report = Join-Path $PSScriptRoot "Cirrus-startup-diagnostics.txt"
$exe = Join-Path $PSScriptRoot "Cirrus.exe"
$start = Get-Date
$lines = [System.Collections.Generic.List[string]]::new()

$lines.Add("Cirrus preview startup diagnostics")
$lines.Add("Collected: $($start.ToString('o'))")
$lines.Add("OS: $([Environment]::OSVersion.VersionString)")
$lines.Add("Architecture: $([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture)")
$lines.Add("Application path: $exe")
$lines.Add("Cirrus.exe exists: $(Test-Path -LiteralPath $exe)")
$lines.Add("WinUI runtime DLL found: $([bool](Get-ChildItem -LiteralPath $PSScriptRoot -Filter 'Microsoft.UI.Xaml.dll' -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1))")
$lines.Add("")

if (Test-Path -LiteralPath $exe) {
    try {
        $proc = Start-Process -FilePath $exe -WorkingDirectory $PSScriptRoot -PassThru -ErrorAction Stop
        Start-Sleep -Seconds 8
        $proc.Refresh()
        if ($proc.HasExited) {
            $lines.Add("Process exited during startup. Exit code: $($proc.ExitCode) (0x$('{0:X8}' -f ($proc.ExitCode -band 0xffffffff)))")
        } else {
            $lines.Add("Process still running after 8 seconds (PID $($proc.Id)). Check whether a window is visible.")
            # Do not terminate the app if it starts successfully on the user's computer.
        }
    } catch {
        $lines.Add("Start-Process error: $($_.Exception.Message)")
    }
} else {
    $lines.Add("ERROR: Cirrus.exe not found. Extract the entire ZIP first.")
}

$lines.Add("")
$lines.Add("Recent Cirrus-related Application event logs:")
try {
    $events = Get-WinEvent -FilterHashtable @{LogName = "Application"; StartTime = $start.AddSeconds(-5)} -MaxEvents 200 -ErrorAction Stop |
        Where-Object {
            $_.ProviderName -in @("Application Error", ".NET Runtime", "Windows Error Reporting", "Application Hang") -and
            $_.Message -match "Cirrus|WindowsAppSDK|Microsoft.UI.Xaml|WinUI|0x80073D54"
        } | Select-Object -First 10
    if ($events) {
        foreach ($event in $events) {
            $lines.Add("[$($event.TimeCreated)] [$($event.ProviderName)] Event $($event.Id)")
            $lines.Add($event.Message)
            $lines.Add("")
        }
    } else {
        $lines.Add("No matching Application events found.")
    }
} catch {
    $lines.Add("Could not read Application events: $($_.Exception.Message)")
}

$lines | Set-Content -LiteralPath $report -Encoding UTF8
Write-Host "Report saved: $report"
Write-Host "Review the report for personal paths or account information before sharing it."
Get-Content -LiteralPath $report
