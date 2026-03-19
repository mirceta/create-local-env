# Install-LoginTask.ps1
# Creates a Windows Scheduled Task to run Setup-Workspace.ps1 at user login.
# Run this once (as admin is optional but recommended).
# Usage: powershell -ExecutionPolicy Bypass -File Install-LoginTask.ps1

param(
    [string]$TaskName = "SetupWorkspace",
    [switch]$Remove
)

$scriptPath = Join-Path $PSScriptRoot "Setup-Workspace.ps1"

if ($Remove) {
    Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Host "Removed scheduled task '$TaskName'." -ForegroundColor Yellow
    exit 0
}

if (-not (Test-Path $scriptPath)) {
    Write-Error "Setup-Workspace.ps1 not found at: $scriptPath"
    exit 1
}

# Add a startup delay so the desktop is fully loaded
$action = New-ScheduledTaskAction `
    -Execute "powershell.exe" `
    -Argument "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$scriptPath`"" `
    -WorkingDirectory $PSScriptRoot

$trigger = New-ScheduledTaskTrigger -AtLogOn
$trigger.Delay = "PT10S"  # 10-second delay after login

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable

# Register for current user
$principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited

Register-ScheduledTask `
    -TaskName $TaskName `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -Principal $principal `
    -Force | Out-Null

Write-Host "Scheduled task '$TaskName' created." -ForegroundColor Green
Write-Host "It will run Setup-Workspace.ps1 at every login (10s delay)." -ForegroundColor Cyan
Write-Host ""
Write-Host "To remove: powershell -File Install-LoginTask.ps1 -Remove" -ForegroundColor DarkGray
