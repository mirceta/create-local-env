# Setup-Workspace.ps1
# Opens applications and positions their windows according to workspace-config.json
# Usage: powershell -ExecutionPolicy Bypass -File Setup-Workspace.ps1 [-Config path\to\config.json] [-DryRun]

param(
    [string]$Config = (Join-Path $PSScriptRoot "workspace-config.json"),
    [switch]$DryRun
)

# Import Win32 functions for window positioning
Add-Type @"
using System;
using System.Runtime.InteropServices;
using System.Text;

public class Win32Window {
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    public const int SW_RESTORE = 9;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNORMAL = 1;
    public const int GWL_STYLE = -16;
    public const int WS_MAXIMIZE = 0x01000000;

    public static readonly IntPtr HWND_TOP = IntPtr.Zero;
    public const uint SWP_SHOWWINDOW = 0x0040;
}
"@

function Get-MainWindowForProcess {
    param([int]$ProcessId, [int]$TimeoutSeconds = 15)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $hwnd = [IntPtr]::Zero

    while ((Get-Date) -lt $deadline) {
        $windows = @()
        $callback = [Win32Window+EnumWindowsProc]{
            param($h, $l)
            $pid = 0
            [Win32Window]::GetWindowThreadProcessId($h, [ref]$pid) | Out-Null
            if ($pid -eq $ProcessId -and [Win32Window]::IsWindowVisible($h)) {
                $len = [Win32Window]::GetWindowTextLength($h)
                if ($len -gt 0) {
                    $sb = New-Object System.Text.StringBuilder ($len + 1)
                    [Win32Window]::GetWindowText($h, $sb, $sb.Capacity) | Out-Null
                    # Store in script-scope variable since we can't modify $windows from callback
                    $script:foundHwnd = $h
                    $script:foundTitle = $sb.ToString()
                }
            }
            return $true
        }

        $script:foundHwnd = [IntPtr]::Zero
        $script:foundTitle = ""
        [Win32Window]::EnumWindows($callback, [IntPtr]::Zero) | Out-Null

        if ($script:foundHwnd -ne [IntPtr]::Zero) {
            return @{ Handle = $script:foundHwnd; Title = $script:foundTitle }
        }

        Start-Sleep -Milliseconds 500
    }

    return $null
}

function Move-AppWindow {
    param(
        [IntPtr]$Handle,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height
    )

    # Restore window first (un-maximize) so we can position it freely
    [Win32Window]::ShowWindow($Handle, [Win32Window]::SW_RESTORE) | Out-Null
    Start-Sleep -Milliseconds 200

    # Move and resize
    [Win32Window]::MoveWindow($Handle, $X, $Y, $Width, $Height, $true) | Out-Null
}

# --- Main ---

if (-not (Test-Path $Config)) {
    Write-Error "Config file not found: $Config"
    exit 1
}

$config = Get-Content $Config -Raw | ConvertFrom-Json
$monitors = $config.monitors

Write-Host "=== Workspace Setup ===" -ForegroundColor Cyan
Write-Host "Config: $Config"
Write-Host ""

foreach ($app in $config.apps) {
    $monitorInfo = $monitors.($app.monitor)
    if (-not $monitorInfo) {
        Write-Warning "Unknown monitor '$($app.monitor)' for $($app.name), skipping."
        continue
    }

    # Calculate absolute screen position
    $absX = $monitorInfo.offsetX + $app.x
    $absY = $monitorInfo.offsetY + $app.y
    $width = $app.width
    $height = $app.height

    Write-Host "[$($app.name)]" -ForegroundColor Yellow
    Write-Host "  Launch: $($app.executable) $($app.arguments)"
    Write-Host "  Position: ($absX, $absY) ${width}x${height} on $($app.monitor) monitor"

    if ($DryRun) {
        Write-Host "  (dry run - skipped)" -ForegroundColor DarkGray
        Write-Host ""
        continue
    }

    # Launch the application
    try {
        $startArgs = @{ FilePath = $app.executable; PassThru = $true }
        if ($app.arguments -and $app.arguments -ne "") {
            $startArgs.ArgumentList = $app.arguments -split ' (?=(?:[^"]*"[^"]*")*[^"]*$)'
        }
        $process = Start-Process @startArgs
        Write-Host "  Started PID: $($process.Id)" -ForegroundColor Green
    }
    catch {
        Write-Warning "  Failed to launch $($app.executable): $_"
        continue
    }

    # Wait for the GUI window to appear
    $delay = if ($app.delaySeconds) { $app.delaySeconds } else { 3 }
    Write-Host "  Waiting ${delay}s for window..."
    Start-Sleep -Seconds $delay

    # Find and position the window
    $windowInfo = Get-MainWindowForProcess -ProcessId $process.Id -TimeoutSeconds 15
    if ($windowInfo) {
        Write-Host "  Found window: '$($windowInfo.Title)'" -ForegroundColor Green
        Move-AppWindow -Handle $windowInfo.Handle -X $absX -Y $absY -Width $width -Height $height
        Write-Host "  Positioned successfully." -ForegroundColor Green
    }
    else {
        # Fallback: some apps (like Chrome) spawn child processes
        # Try to find by process name instead
        Write-Host "  Main window not found by PID, searching by process name..." -ForegroundColor DarkYellow
        $procName = [System.IO.Path]::GetFileNameWithoutExtension($app.executable)
        $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue |
                 Where-Object { $_.MainWindowHandle -ne [IntPtr]::Zero } |
                 Sort-Object StartTime -Descending |
                 Select-Object -First 1

        if ($procs) {
            $handle = $procs.MainWindowHandle
            Write-Host "  Found window by process name (PID $($procs.Id))" -ForegroundColor Green
            Move-AppWindow -Handle $handle -X $absX -Y $absY -Width $width -Height $height
            Write-Host "  Positioned successfully." -ForegroundColor Green
        }
        else {
            Write-Warning "  Could not find a visible window for $($app.name)."
        }
    }
    Write-Host ""
}

Write-Host "=== Done ===" -ForegroundColor Cyan
