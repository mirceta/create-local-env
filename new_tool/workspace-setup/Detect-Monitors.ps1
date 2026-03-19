# Detect-Monitors.ps1
# Helper: prints your monitor layout so you can fill in workspace-config.json correctly.
# Usage: powershell -ExecutionPolicy Bypass -File Detect-Monitors.ps1

Add-Type -AssemblyName System.Windows.Forms

Write-Host "=== Monitor Layout ===" -ForegroundColor Cyan
Write-Host ""

$screens = [System.Windows.Forms.Screen]::AllScreens | Sort-Object { $_.Bounds.X }

foreach ($i in 0..($screens.Count - 1)) {
    $s = $screens[$i]
    $b = $s.Bounds
    $w = $s.WorkingArea
    $primary = if ($s.Primary) { " (PRIMARY)" } else { "" }

    Write-Host "Monitor $($i + 1)$primary" -ForegroundColor Yellow
    Write-Host "  Device:       $($s.DeviceName)"
    Write-Host "  Full bounds:  X=$($b.X), Y=$($b.Y), Width=$($b.Width), Height=$($b.Height)"
    Write-Host "  Working area: X=$($w.X), Y=$($w.Y), Width=$($w.Width), Height=$($w.Height)"
    Write-Host ""
    Write-Host "  Config entry:" -ForegroundColor Green
    Write-Host "    `"monitor$($i + 1)`": { `"offsetX`": $($b.X), `"offsetY`": $($b.Y), `"width`": $($b.Width), `"height`": $($b.Height) }"
    Write-Host ""
}

Write-Host "Copy the config entries above into your workspace-config.json `"monitors`" section." -ForegroundColor Cyan
