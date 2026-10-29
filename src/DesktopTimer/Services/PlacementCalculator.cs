using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>
/// ウィンドウ位置の計算（仕様書 4.1, 8.3）。Win32 に依存しない純粋な計算だけを置き、テストできるようにしている。
/// 座標はすべて物理ピクセル。ウィンドウの大きさは DIP で受け取り、置き先モニターのスケールで換算する。
/// </summary>
public static class PlacementCalculator
{
    /// <summary>作業領域の端からの余白（DIP）。</summary>
    public const double EdgeMarginDip = 16;

    /// <summary>モニターに置いた時のウィンドウの矩形。</summary>
    public static ScreenRect GetWindowRect(int x, int y, double widthDip, double heightDip, MonitorInfo monitor) =>
        new(x, y, x + (int)Math.Round(widthDip * monitor.Scale), y + (int)Math.Round(heightDip * monitor.Scale));

    /// <summary>デフォルト位置：プライマリモニターの作業領域の右下隅から 16DIP 内側。</summary>
    public static (int X, int Y) GetDefaultPosition(IReadOnlyList<MonitorInfo> monitors, double widthDip, double heightDip)
    {
        var primary = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
        var margin = (int)Math.Round(EdgeMarginDip * primary.Scale);
        var width = (int)Math.Round(widthDip * primary.Scale);
        var height = (int)Math.Round(heightDip * primary.Scale);
        return (primary.WorkArea.Right - width - margin, primary.WorkArea.Bottom - height - margin);
    }

    /// <summary>
    /// 保存位置を今のモニター構成で解決する。どのモニターの作業領域にも収まらなければ null（デフォルト位置にする）。
    /// </summary>
    public static (int X, int Y)? ResolveSavedPosition(
        WindowPosition saved, IReadOnlyList<MonitorInfo> monitors, double widthDip, double heightDip)
    {
        // 保存時と同じモニターがあれば、そのモニター上の同じ位置にする（ディスプレイ設定で並べ替えられても追従する）
        var sameMonitor = monitors.FirstOrDefault(m => m.DeviceName == saved.MonitorDeviceName);
        if (sameMonitor is not null && saved.MonitorLeft is { } savedLeft && saved.MonitorTop is { } savedTop)
        {
            var x = saved.X - savedLeft + sameMonitor.Bounds.Left;
            var y = saved.Y - savedTop + sameMonitor.Bounds.Top;
            if (FitsInWorkArea(x, y, widthDip, heightDip, sameMonitor))
            {
                return (x, y);
            }
        }

        // 識別情報で見つからない（古い設定・モニター交換など）場合は座標のまま判定する
        var monitorAtPoint = FindMonitorAt(saved.X, saved.Y, monitors);
        if (monitorAtPoint is not null && FitsInWorkArea(saved.X, saved.Y, widthDip, heightDip, monitorAtPoint))
        {
            return (saved.X, saved.Y);
        }

        return null;
    }

    /// <summary>タイマーと、その近くに出す小窓との間隔（DIP）。</summary>
    public const double PopupGapDip = 8;

    /// <summary>
    /// タイマーの近くに出す小窓（「時間を指定して開始」など）の位置。
    /// タイマーの上に右端をそろえて置き、上に入らなければ下、どちらにも入らなければ作業領域内に収める。
    /// </summary>
    /// <param name="anchor">タイマーの矩形。</param>
    /// <param name="monitor">タイマーがあるモニター。小窓の大きさ・間隔はこのモニターのスケールで換算する。</param>
    public static (int X, int Y) GetPopupPosition(ScreenRect anchor, MonitorInfo monitor, double widthDip, double heightDip)
    {
        var work = monitor.WorkArea;
        var gap = (int)Math.Round(PopupGapDip * monitor.Scale);
        var width = (int)Math.Round(widthDip * monitor.Scale);
        var height = (int)Math.Round(heightDip * monitor.Scale);

        var x = Math.Clamp(anchor.Right - width, work.Left, Math.Max(work.Left, work.Right - width));

        var above = anchor.Top - gap - height;
        var below = anchor.Bottom + gap;
        var y = above >= work.Top ? above
            : below + height <= work.Bottom ? below
            : Math.Clamp(above, work.Top, Math.Max(work.Top, work.Bottom - height));
        return (x, y);
    }

    /// <summary>ウィンドウの矩形が、いずれかのモニターの作業領域に完全に収まっているか。</summary>
    public static bool FitsInAnyWorkArea(ScreenRect windowRect, IReadOnlyList<MonitorInfo> monitors) =>
        monitors.Any(m => m.WorkArea.Contains(windowRect));

    /// <summary>点を含むモニター。</summary>
    public static MonitorInfo? FindMonitorAt(int x, int y, IReadOnlyList<MonitorInfo> monitors) =>
        monitors.FirstOrDefault(m => m.Bounds.Contains(x, y));

    private static bool FitsInWorkArea(int x, int y, double widthDip, double heightDip, MonitorInfo monitor) =>
        monitor.WorkArea.Contains(GetWindowRect(x, y, widthDip, heightDip, monitor));
}
