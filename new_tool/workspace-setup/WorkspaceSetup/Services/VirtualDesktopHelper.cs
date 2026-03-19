using System.Runtime.InteropServices;

namespace WorkspaceSetup.Services;

/// <summary>
/// Manages Windows 10 virtual desktops.
///
/// Two strategies for keeping a window on all desktops:
///   1. COM Pin (undocumented, build-specific) — tried first
///   2. Desktop Following (documented IVirtualDesktopManager) — reliable fallback
///      Uses a timer to detect desktop switches and moves the window to follow.
/// </summary>
public static class VirtualDesktopHelper
{
    private static bool _initialized;
    private static IVirtualDesktopManagerInternal? _managerInternal;
    private static IVirtualDesktopManager? _manager;
    private static IApplicationViewCollection? _viewCollection;
    private static IVirtualDesktopPinnedApps? _pinnedApps;
    private static System.Windows.Forms.Timer? _followTimer;

    public static string? LastError { get; private set; }

    private static bool EnsureInitialized()
    {
        if (_initialized) return _manager != null;
        _initialized = true;

        try
        {
            // Documented API — always try this
            var managerType = Type.GetTypeFromCLSID(CLSID_VirtualDesktopManager);
            if (managerType != null)
                _manager = (IVirtualDesktopManager)Activator.CreateInstance(managerType)!;
        }
        catch (Exception ex)
        {
            LastError = $"IVirtualDesktopManager init: {ex.Message}";
        }

        try
        {
            // Undocumented APIs — best-effort
            var shellType = Type.GetTypeFromCLSID(CLSID_ImmersiveShell);
            if (shellType != null)
            {
                var shell = (IServiceProvider10)Activator.CreateInstance(shellType)!;

                var clsid = CLSID_VirtualDesktopManagerInternal;
                var iid = typeof(IVirtualDesktopManagerInternal).GUID;
                _managerInternal = (IVirtualDesktopManagerInternal)shell.QueryService(ref clsid, ref iid);

                var viewGuid = typeof(IApplicationViewCollection).GUID;
                _viewCollection = (IApplicationViewCollection)shell.QueryService(ref viewGuid, ref viewGuid);

                var pinnedClsid = CLSID_VirtualDesktopPinnedApps;
                var pinnedIid = typeof(IVirtualDesktopPinnedApps).GUID;
                _pinnedApps = (IVirtualDesktopPinnedApps)shell.QueryService(ref pinnedClsid, ref pinnedIid);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Undocumented VD init failed (non-fatal): {ex.Message}");
            // Non-fatal — we can still use the documented API fallback
        }

        return _manager != null;
    }

    /// <summary>
    /// Pin a window so it appears on ALL virtual desktops.
    /// Tries undocumented COM pin first, falls back to desktop-following via timer.
    /// </summary>
    public static bool PinWindow(IntPtr hwnd)
    {
        if (!EnsureInitialized())
        {
            LastError = "Virtual desktop APIs not available on this OS";
            return false;
        }

        // Strategy 1: Try undocumented COM pin
        if (_viewCollection != null && _pinnedApps != null)
        {
            try
            {
                int hr = _viewCollection.GetViewForHwnd(hwnd, out var view);
                if (hr >= 0 && view != null)
                {
                    _pinnedApps.PinView(view);
                    _pinnedApps.IsViewPinned(view, out bool pinned);
                    if (pinned)
                    {
                        LastError = null;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"COM pin failed (trying fallback): {ex.Message}");
            }
        }

        // Strategy 2: Desktop-following via documented API
        if (_manager != null)
        {
            StartDesktopFollowing(hwnd);
            LastError = null;
            return true;
        }

        LastError = "Both pin strategies failed";
        return false;
    }

    /// <summary>
    /// Start a timer that detects virtual desktop switches and moves the window
    /// to the current desktop. Uses only the documented IVirtualDesktopManager API.
    /// </summary>
    public static void StartDesktopFollowing(IntPtr hwnd)
    {
        StopDesktopFollowing();

        _followTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _followTimer.Tick += (_, _) =>
        {
            try
            {
                if (_manager == null) return;

                int hr = _manager.IsWindowOnCurrentVirtualDesktop(hwnd, out bool onCurrent);
                if (hr < 0) return; // API call failed, skip this tick

                if (!onCurrent)
                {
                    // Window is on a different desktop — move it to the current one.
                    // To get the current desktop's GUID, create a tiny temp window
                    // (new windows are always created on the active desktop).
                    var tempHwnd = CreateWindowExW(
                        0, "Static", "", 0,
                        0, 0, 1, 1,
                        IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                    if (tempHwnd != IntPtr.Zero)
                    {
                        try
                        {
                            var desktopId = _manager.GetWindowDesktopId(tempHwnd);
                            if (desktopId != Guid.Empty)
                            {
                                _manager.MoveWindowToDesktop(hwnd, ref desktopId);
                            }
                        }
                        finally
                        {
                            DestroyWindow(tempHwnd);
                        }
                    }
                }
            }
            catch { /* swallow — timer will retry next tick */ }
        };
        _followTimer.Start();
    }

    public static void StopDesktopFollowing()
    {
        _followTimer?.Stop();
        _followTimer?.Dispose();
        _followTimer = null;
    }

    /// <summary>Unpin a window / stop following.</summary>
    public static bool UnpinWindow(IntPtr hwnd)
    {
        StopDesktopFollowing();

        if (_viewCollection != null && _pinnedApps != null)
        {
            try
            {
                int hr = _viewCollection.GetViewForHwnd(hwnd, out var view);
                if (hr >= 0 && view != null)
                    _pinnedApps.UnpinView(view);
            }
            catch { }
        }
        return true;
    }

    public static bool IsSupported => EnsureInitialized();

    // ── Other desktop management methods ──

    public static int GetDesktopCount()
    {
        if (!EnsureInitialized() || _managerInternal == null) return 1;
        try { return _managerInternal.GetCount(); }
        catch { return 1; }
    }

    public static bool EnsureDesktopCount(int count)
    {
        if (!EnsureInitialized() || _managerInternal == null) return false;
        try
        {
            int current = _managerInternal.GetCount();
            while (current < count)
            {
                _managerInternal.CreateDesktop();
                current++;
            }
            return true;
        }
        catch { return false; }
    }

    public static bool SwitchToDesktop(int index)
    {
        if (!EnsureInitialized() || _managerInternal == null) return false;
        try
        {
            _managerInternal.GetDesktops(out var desktops);
            desktops.GetCount(out int count);
            if (index < 0 || index >= count)
            {
                Marshal.ReleaseComObject(desktops);
                return false;
            }

            var iid = typeof(IVirtualDesktop).GUID;
            desktops.GetAt(index, ref iid, out var obj);
            var desktop = (IVirtualDesktop)obj;
            _managerInternal.SwitchDesktop(desktop);
            Marshal.ReleaseComObject(desktops);
            return true;
        }
        catch { return false; }
    }

    public static int GetCurrentDesktopIndex()
    {
        if (!EnsureInitialized() || _managerInternal == null) return 0;
        try
        {
            var current = _managerInternal.GetCurrentDesktop();
            var currentId = current.GetId();
            _managerInternal.GetDesktops(out var desktops);
            desktops.GetCount(out int count);
            var iid = typeof(IVirtualDesktop).GUID;

            for (int i = 0; i < count; i++)
            {
                desktops.GetAt(i, ref iid, out var obj);
                var d = (IVirtualDesktop)obj;
                if (d.GetId() == currentId)
                {
                    Marshal.ReleaseComObject(desktops);
                    return i;
                }
            }
            Marshal.ReleaseComObject(desktops);
            return 0;
        }
        catch { return 0; }
    }

    // ═══════════════════════════════════════════
    //  Win32 helpers for desktop following
    // ═══════════════════════════════════════════

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateWindowExW(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight,
        IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(IntPtr hwnd);

    // ═══════════════════════════════════════════
    //  COM Interface Definitions
    // ═══════════════════════════════════════════

    private static readonly Guid CLSID_ImmersiveShell =
        new("C2F03A33-21F5-47FA-B4BB-156362A2F239");
    private static readonly Guid CLSID_VirtualDesktopManagerInternal =
        new("C5E0CDCA-7B6E-41B2-9FC4-D93975CC467B");
    private static readonly Guid CLSID_VirtualDesktopManager =
        new("AA509086-5CA9-4C25-8F95-589D3C07B48A");
    private static readonly Guid CLSID_VirtualDesktopPinnedApps =
        new("B5A399E7-1C87-46B8-88E9-FC5747B171BD");

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("6D5140C1-7436-11CE-8034-00AA006009FA")]
    private interface IServiceProvider10
    {
        [return: MarshalAs(UnmanagedType.IUnknown)]
        object QueryService(ref Guid guidService, ref Guid riid);
    }

    // ── Documented API (stable across Windows versions) ──

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("A5CD92FF-29BE-454C-8D04-D82879FB3F1B")]
    private interface IVirtualDesktopManager
    {
        [PreserveSig] int IsWindowOnCurrentVirtualDesktop(IntPtr topLevelWindow, out bool onCurrent);
        Guid GetWindowDesktopId(IntPtr topLevelWindow);
        void MoveWindowToDesktop(IntPtr topLevelWindow, ref Guid desktopId);
    }

    // ── Undocumented APIs (Build 17763 / Server 2019) ──

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("FF72FFDD-BE7E-43FC-9C03-AD81681E88E4")]
    private interface IVirtualDesktop
    {
        [PreserveSig] int IsViewVisible([MarshalAs(UnmanagedType.IUnknown)] object view, out bool visible);
        Guid GetId();
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("F31574D6-B682-4CDC-BD56-1827860ABEC6")]
    private interface IVirtualDesktopManagerInternal
    {
        int GetCount();
        void MoveViewToDesktop([MarshalAs(UnmanagedType.IUnknown)] object view, IVirtualDesktop desktop);
        [PreserveSig] int CanViewMoveDesktops([MarshalAs(UnmanagedType.IUnknown)] object view, out bool canMove);
        IVirtualDesktop GetCurrentDesktop();
        void GetDesktops(out IObjectArray desktops);
        [PreserveSig]
        int GetAdjacentDesktop(IVirtualDesktop from, int direction, out IVirtualDesktop desktop);
        void SwitchDesktop(IVirtualDesktop desktop);
        IVirtualDesktop CreateDesktop();
        void RemoveDesktop(IVirtualDesktop desktop, IVirtualDesktop fallback);
        IVirtualDesktop FindDesktop(ref Guid desktopId);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("1841C6D7-4F9D-42C0-AF41-8747538F10E5")]
    private interface IApplicationViewCollection
    {
        [PreserveSig] int GetViews(out IObjectArray array);
        [PreserveSig] int GetViewsByZOrder(out IObjectArray array);
        [PreserveSig] int GetViewsByAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] string id, out IObjectArray array);
        [PreserveSig] int GetViewForHwnd(IntPtr hwnd, [MarshalAs(UnmanagedType.IUnknown)] out object view);
        [PreserveSig] int GetViewForApplication([MarshalAs(UnmanagedType.IUnknown)] object application, [MarshalAs(UnmanagedType.IUnknown)] out object view);
        [PreserveSig] int GetViewForAppUserModelId([MarshalAs(UnmanagedType.LPWStr)] string id, [MarshalAs(UnmanagedType.IUnknown)] out object view);
        [PreserveSig] int GetViewInFocus(out IntPtr view);
        [PreserveSig] int Unknown1(out IntPtr view);
        void RefreshCollection();
        [PreserveSig] int RegisterForApplicationViewChanges([MarshalAs(UnmanagedType.IUnknown)] object listener, out int cookie);
        [PreserveSig] int UnregisterForApplicationViewChanges(int cookie);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("4CE81583-1E4C-4632-A621-07A53543148F")]
    private interface IVirtualDesktopPinnedApps
    {
        [PreserveSig] int IsViewPinned([MarshalAs(UnmanagedType.IUnknown)] object view, out bool pinned);
        void PinView([MarshalAs(UnmanagedType.IUnknown)] object view);
        void UnpinView([MarshalAs(UnmanagedType.IUnknown)] object view);
        [PreserveSig] int IsAppIdPinned([MarshalAs(UnmanagedType.LPWStr)] string appId, out bool pinned);
        void PinAppID([MarshalAs(UnmanagedType.LPWStr)] string appId);
        void UnpinAppID([MarshalAs(UnmanagedType.LPWStr)] string appId);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("92CA9DCD-5622-4BBA-A805-5E9F541BD8C9")]
    private interface IObjectArray
    {
        void GetCount(out int count);
        void GetAt(int index, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out object obj);
    }
}
