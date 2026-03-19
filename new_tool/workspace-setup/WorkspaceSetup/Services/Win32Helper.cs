using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace WorkspaceSetup.Services;

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left, Top, Right, Bottom;
    public int Width => Right - Left;
    public int Height => Bottom - Top;
    public override string ToString() => $"({Left},{Top}) {Width}x{Height}";
}

public record WindowPlacement(IntPtr Handle, string Title, string ProcessName, RECT Rect);

public static partial class Win32Helper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height,
        [MarshalAs(UnmanagedType.Bool)] bool repaint);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    private static partial int GetWindowTextLengthW(IntPtr hWnd);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int GetWindowTextW(IntPtr hWnd, [Out] char[] lpString, int nMaxCount);

    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetForegroundWindow(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(IntPtr hWnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsZoomed(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    /// <summary>Get the current position and size of a window.</summary>
    public static RECT? GetWindowPosition(IntPtr hwnd)
    {
        if (GetWindowRect(hwnd, out var rect))
            return rect;
        return null;
    }

    /// <summary>Get the window title.</summary>
    public static string GetWindowTitle(IntPtr hwnd)
    {
        int len = GetWindowTextLengthW(hwnd);
        if (len <= 0) return "";
        var buf = new char[len + 1];
        GetWindowTextW(hwnd, buf, buf.Length);
        return new string(buf, 0, len);
    }

    /// <summary>
    /// Position a window and verify it landed where we asked.
    /// Returns (success, actualRect) so the caller can report mismatches.
    /// </summary>
    public static (bool Ok, RECT Actual) PositionAndVerify(IntPtr hwnd, int x, int y, int width, int height, int tolerance = 10)
    {
        // Restore if minimized/maximized — these states ignore MoveWindow
        if (IsIconic(hwnd) || IsZoomed(hwnd))
        {
            ShowWindow(hwnd, SW_RESTORE);
            Thread.Sleep(300);
        }

        bool moved = MoveWindow(hwnd, x, y, width, height, true);
        Thread.Sleep(200);

        // Read back actual position
        GetWindowRect(hwnd, out var actual);

        bool xOk = Math.Abs(actual.Left - x) <= tolerance;
        bool yOk = Math.Abs(actual.Top - y) <= tolerance;
        bool wOk = Math.Abs(actual.Width - width) <= tolerance;
        bool hOk = Math.Abs(actual.Height - height) <= tolerance;

        return (moved && xOk && yOk && wOk && hOk, actual);
    }

    public static void PositionWindow(IntPtr hwnd, int x, int y, int width, int height)
    {
        ShowWindow(hwnd, SW_RESTORE);
        Thread.Sleep(200);
        MoveWindow(hwnd, x, y, width, height, true);
    }

    public static (IntPtr Handle, string Title)? FindWindowForProcess(int processId, int timeoutMs = 15000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;

        while (Environment.TickCount64 < deadline)
        {
            IntPtr found = IntPtr.Zero;
            string foundTitle = "";

            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == processId && IsWindowVisible(hWnd))
                {
                    int len = GetWindowTextLengthW(hWnd);
                    if (len > 0)
                    {
                        var buf = new char[len + 1];
                        GetWindowTextW(hWnd, buf, buf.Length);
                        found = hWnd;
                        foundTitle = new string(buf, 0, len);
                        return false; // stop enumerating
                    }
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
                return (found, foundTitle);

            Thread.Sleep(500);
        }

        return null;
    }

    public static IntPtr? FindWindowByProcessName(string processName)
    {
        var procs = Process.GetProcessesByName(processName)
            .Where(p => p.MainWindowHandle != IntPtr.Zero)
            .OrderByDescending(p => p.StartTime)
            .FirstOrDefault();

        return procs?.MainWindowHandle;
    }

    /// <summary>
    /// Bring a window to the foreground: restore if minimized, then set as foreground window.
    /// </summary>
    public static void BringToFront(IntPtr hwnd)
    {
        ShowWindow(hwnd, SW_RESTORE);
        SetForegroundWindow(hwnd);
    }

    /// <summary>
    /// Find all visible windows for a given process name and bring them to front.
    /// Returns the number of windows brought forward.
    /// </summary>
    public static int BringAllToFrontByProcessName(string processName)
    {
        var handles = new List<IntPtr>();
        var procs = Process.GetProcessesByName(processName);

        foreach (var proc in procs)
        {
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == proc.Id && IsWindowVisible(hWnd))
                {
                    int len = GetWindowTextLengthW(hWnd);
                    if (len > 0)
                        handles.Add(hWnd);
                }
                return true;
            }, IntPtr.Zero);
        }

        foreach (var hwnd in handles)
            BringToFront(hwnd);

        return handles.Count;
    }
}
