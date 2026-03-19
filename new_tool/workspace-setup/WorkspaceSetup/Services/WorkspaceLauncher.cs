using System.Diagnostics;
using WorkspaceSetup.Models;

namespace WorkspaceSetup.Services;

public class LaunchProgressEventArgs(string appName, string message, bool isError = false) : EventArgs
{
    public string AppName { get; } = appName;
    public string Message { get; } = message;
    public bool IsError { get; } = isError;
}

/// <summary>Tracks a launched window so we can verify/reposition later.</summary>
public class LaunchedWindow
{
    public required AppEntry App { get; init; }
    public required int ExpectedX { get; init; }
    public required int ExpectedY { get; init; }
    public IntPtr Handle { get; set; }
    public string? Title { get; set; }
    public string ProcessName => Path.GetFileNameWithoutExtension(App.Executable);
    public bool Found => Handle != IntPtr.Zero;
}

public class WorkspaceLauncher
{
    public event EventHandler<LaunchProgressEventArgs>? Progress;

    /// <summary>Windows we launched and tracked — used for verify/reposition.</summary>
    public List<LaunchedWindow> TrackedWindows { get; } = [];

    private Dictionary<string, string> _variables = new();

    /// <summary>Replace all {{VAR_NAME}} placeholders in a string with variable values.</summary>
    private string SubstituteVariables(string input)
    {
        foreach (var (key, value) in _variables)
            input = input.Replace($"{{{{{key}}}}}", value);
        return input;
    }

    public async Task LaunchDesktopAsync(DesktopConfig desktop, Dictionary<string, MonitorInfo> monitors, Dictionary<string, string>? variables = null, CancellationToken ct = default)
    {
        TrackedWindows.Clear();
        _variables = variables ?? new();

        if (desktop.Apps.Count == 0)
        {
            Report(desktop.Name, "No apps configured.");
            return;
        }

        Report(desktop.Name, $"Launching {desktop.Apps.Count} app(s)...");

        foreach (var app in desktop.Apps)
        {
            if (ct.IsCancellationRequested) break;
            await LaunchAppAsync(app, monitors, ct);
        }

        // Final verification pass
        Report("Verify", "── Verification pass ──");
        int ok = 0, bad = 0;
        foreach (var tw in TrackedWindows)
        {
            if (!tw.Found)
            {
                Report(tw.App.Name, $"MISSING — no window found", isError: true);
                bad++;
                continue;
            }

            var rect = Win32Helper.GetWindowPosition(tw.Handle);
            if (rect == null)
            {
                Report(tw.App.Name, $"LOST — window handle no longer valid", isError: true);
                bad++;
                continue;
            }

            var r = rect.Value;
            int dx = Math.Abs(r.Left - tw.ExpectedX);
            int dy = Math.Abs(r.Top - tw.ExpectedY);
            int dw = Math.Abs(r.Width - tw.App.Width);
            int dh = Math.Abs(r.Height - tw.App.Height);

            if (dx > 10 || dy > 10 || dw > 10 || dh > 10)
            {
                Report(tw.App.Name,
                    $"WRONG POSITION — expected ({tw.ExpectedX},{tw.ExpectedY}) {tw.App.Width}x{tw.App.Height}, " +
                    $"actual ({r.Left},{r.Top}) {r.Width}x{r.Height}",
                    isError: true);
                bad++;
            }
            else
            {
                Report(tw.App.Name, $"OK at ({r.Left},{r.Top}) {r.Width}x{r.Height}");
                ok++;
            }
        }

        if (bad > 0)
            Report("Verify", $"Done: {ok} OK, {bad} problem(s). Use 'Reposition' to fix.", isError: true);
        else
            Report("Done", $"All {ok} window(s) positioned correctly.");
    }

    /// <summary>
    /// Verify all tracked windows are still where they should be.
    /// Returns list of (app, problem description) for any mismatches.
    /// </summary>
    public List<(LaunchedWindow Window, string Problem)> VerifyAll()
    {
        var problems = new List<(LaunchedWindow, string)>();

        foreach (var tw in TrackedWindows)
        {
            if (!tw.Found)
            {
                // Try to re-find by process name
                var hwnd = Win32Helper.FindWindowByProcessName(tw.ProcessName);
                if (hwnd.HasValue)
                {
                    tw.Handle = hwnd.Value;
                    tw.Title = Win32Helper.GetWindowTitle(hwnd.Value);
                }
                else
                {
                    problems.Add((tw, "NOT RUNNING — window not found"));
                    continue;
                }
            }

            var rect = Win32Helper.GetWindowPosition(tw.Handle);
            if (rect == null)
            {
                problems.Add((tw, "LOST — window handle invalid"));
                continue;
            }

            var r = rect.Value;
            int dx = Math.Abs(r.Left - tw.ExpectedX);
            int dy = Math.Abs(r.Top - tw.ExpectedY);
            int dw = Math.Abs(r.Width - tw.App.Width);
            int dh = Math.Abs(r.Height - tw.App.Height);

            if (dx > 10 || dy > 10 || dw > 10 || dh > 10)
            {
                problems.Add((tw,
                    $"WRONG — expected ({tw.ExpectedX},{tw.ExpectedY}) {tw.App.Width}x{tw.App.Height}, " +
                    $"actual ({r.Left},{r.Top}) {r.Width}x{r.Height}"));
            }
        }

        return problems;
    }

    /// <summary>
    /// Re-position all tracked windows to their expected locations.
    /// Returns count of windows successfully repositioned.
    /// </summary>
    public int RepositionAll()
    {
        int fixed_ = 0;
        foreach (var tw in TrackedWindows)
        {
            if (!tw.Found)
            {
                var hwnd = Win32Helper.FindWindowByProcessName(tw.ProcessName);
                if (hwnd.HasValue)
                {
                    tw.Handle = hwnd.Value;
                    tw.Title = Win32Helper.GetWindowTitle(hwnd.Value);
                }
                else continue;
            }

            var (ok, actual) = Win32Helper.PositionAndVerify(
                tw.Handle, tw.ExpectedX, tw.ExpectedY, tw.App.Width, tw.App.Height);

            if (ok) fixed_++;
        }
        return fixed_;
    }

    private async Task LaunchAppAsync(AppEntry app, Dictionary<string, MonitorInfo> monitors, CancellationToken ct)
    {
        if (!monitors.TryGetValue(app.Monitor, out var monitor))
        {
            Report(app.Name, $"Unknown monitor '{app.Monitor}', skipping.", isError: true);
            return;
        }

        int absX = monitor.OffsetX + app.X;
        int absY = monitor.OffsetY + app.Y;

        var tracked = new LaunchedWindow
        {
            App = app,
            ExpectedX = absX,
            ExpectedY = absY,
        };
        TrackedWindows.Add(tracked);

        var resolvedArgs = SubstituteVariables(app.Arguments);
        var resolvedExe = SubstituteVariables(app.Executable);
        Report(app.Name, $"Launching: {resolvedExe} {resolvedArgs}");

        Process process;
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = resolvedExe,
                Arguments = resolvedArgs,
                UseShellExecute = true
            };
            process = Process.Start(psi)!;
        }
        catch (Exception ex)
        {
            Report(app.Name, $"Failed to launch: {ex.Message}", isError: true);
            return;
        }

        Report(app.Name, $"PID {process.Id}, waiting {app.DelaySeconds}s for window...");
        await Task.Delay(app.DelaySeconds * 1000, ct);

        // Find the window
        IntPtr hwnd = IntPtr.Zero;
        string title = "";

        // Try by PID first
        var window = Win32Helper.FindWindowForProcess(process.Id, timeoutMs: 10000);
        if (window.HasValue)
        {
            hwnd = window.Value.Handle;
            title = window.Value.Title;
        }
        else
        {
            // Fallback: by process name (Chrome etc. spawn child processes)
            Report(app.Name, "Not found by PID, searching by process name...");
            var procName = Path.GetFileNameWithoutExtension(app.Executable);
            var found = Win32Helper.FindWindowByProcessName(procName);
            if (found.HasValue)
            {
                hwnd = found.Value;
                title = Win32Helper.GetWindowTitle(hwnd);
            }
        }

        if (hwnd == IntPtr.Zero)
        {
            Report(app.Name, "Could not find window.", isError: true);
            return;
        }

        tracked.Handle = hwnd;
        tracked.Title = title;
        Report(app.Name, $"Found: '{title}'");

        // Position and verify
        var (ok, actual) = Win32Helper.PositionAndVerify(hwnd, absX, absY, app.Width, app.Height);

        if (ok)
        {
            Report(app.Name, $"Positioned at ({absX},{absY}) {app.Width}x{app.Height}");
        }
        else
        {
            Report(app.Name,
                $"Position MISMATCH — wanted ({absX},{absY}) {app.Width}x{app.Height}, " +
                $"got ({actual.Left},{actual.Top}) {actual.Width}x{actual.Height}",
                isError: true);

            // Retry once
            Report(app.Name, "Retrying position...");
            Thread.Sleep(500);
            var (ok2, actual2) = Win32Helper.PositionAndVerify(hwnd, absX, absY, app.Width, app.Height);
            if (ok2)
                Report(app.Name, "Retry succeeded.");
            else
                Report(app.Name,
                    $"Retry failed — still at ({actual2.Left},{actual2.Top}) {actual2.Width}x{actual2.Height}",
                    isError: true);
        }
    }

    private void Report(string appName, string message, bool isError = false)
    {
        Progress?.Invoke(this, new LaunchProgressEventArgs(appName, message, isError));
    }
}
