using System.Runtime.InteropServices;
using DesktopTimer.Models;
using static DesktopTimer.Interop.NativeMethods;

namespace DesktopTimer.Services;

/// <summary>
/// モニター情報の取得とウィンドウの移動（Win32）。位置の計算は <see cref="PlacementCalculator"/> で行う。
/// モニターごとに DPI が異なりうるため、座標はすべて物理ピクセル（Win32 のスクリーン座標）で扱う。
/// </summary>
public static class MonitorHelper
{
    /// <summary>接続中のすべてのモニター。</summary>
    public static IReadOnlyList<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            if (GetMonitorInfo(hMonitor) is { } info)
            {
                monitors.Add(info);
            }

            return true;
        }, IntPtr.Zero);
        return monitors;
    }

    /// <summary>ウィンドウが（主に）表示されているモニター。非表示のウィンドウでも位置から求まる。</summary>
    public static MonitorInfo? GetMonitorOf(IntPtr hwnd) => GetMonitorInfo(MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST));

    /// <summary>ウィンドウの矩形（物理ピクセル）。</summary>
    public static ScreenRect? GetWindowBounds(IntPtr hwnd) =>
        GetWindowRect(hwnd, out var rect) ? new ScreenRect(rect.Left, rect.Top, rect.Right, rect.Bottom) : null;

    /// <summary>
    /// ウィンドウの左上を指定位置へ移動する（サイズは変えない）。
    /// DPI の異なるモニターへ移ると WPF がサイズを変え、その際に位置がずれることがあるので、ずれていたら置き直す。
    /// </summary>
    public static void MoveTo(IntPtr hwnd, int x, int y)
    {
        SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        if (GetWindowBounds(hwnd) is { } bounds && (bounds.Left != x || bounds.Top != y))
        {
            SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
        }
    }

    private static MonitorInfo? GetMonitorInfo(IntPtr hMonitor)
    {
        var info = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
        if (hMonitor == IntPtr.Zero || !GetMonitorInfoEx(hMonitor, ref info))
        {
            return null;
        }

        var scale = GetDpiForMonitor(hMonitor, MDT_EFFECTIVE_DPI, out var dpiX, out _) == 0 ? dpiX / 96.0 : 1.0;
        return new MonitorInfo(
            info.szDevice,
            ToScreenRect(info.rcMonitor),
            ToScreenRect(info.rcWork),
            scale,
            (info.dwFlags & MONITORINFOF_PRIMARY) != 0);
    }

    private static ScreenRect ToScreenRect(RECT rect) => new(rect.Left, rect.Top, rect.Right, rect.Bottom);
}
