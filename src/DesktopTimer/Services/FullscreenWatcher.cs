using System.Text;
using System.Windows.Threading;
using DesktopTimer.Models;
using static DesktopTimer.Interop.NativeMethods;

namespace DesktopTimer.Services;

/// <summary>全画面判定に使う、フォアグラウンドウィンドウの情報。</summary>
/// <param name="WindowRect">フォアグラウンドウィンドウの矩形。</param>
/// <param name="WindowMonitor">フォアグラウンドウィンドウがあるモニター。</param>
/// <param name="TimerMonitor">タイマーがあるモニター。</param>
/// <param name="ClassName">ウィンドウクラス名。</param>
/// <param name="IsShellWindow">デスクトップ（シェル）のウィンドウか。</param>
/// <param name="IsOwnProcess">このアプリのウィンドウか。</param>
/// <param name="IsMaximizedWithCaption">タイトルバー付きで最大化された通常のウィンドウか。</param>
public sealed record ForegroundWindowInfo(
    ScreenRect WindowRect,
    MonitorInfo WindowMonitor,
    MonitorInfo TimerMonitor,
    string ClassName,
    bool IsShellWindow,
    bool IsOwnProcess,
    bool IsMaximizedWithCaption);

/// <summary>
/// 全画面アプリの検出（仕様書 8.2）。1秒周期でフォアグラウンドウィンドウを調べ、
/// タイマーと同じモニターの全域を覆っていれば全画面とみなす。
/// </summary>
public sealed class FullscreenWatcher : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    /// <summary>デスクトップのウィンドウクラス。全画面扱いしない。</summary>
    private static readonly string[] DesktopClassNames = ["Progman", "WorkerW"];

    private readonly Func<IntPtr> _getTimerHandle;
    private readonly DispatcherTimer _pollTimer;
    private readonly uint _ownProcessId = (uint)Environment.ProcessId;

    /// <param name="getTimerHandle">タイマーウィンドウのハンドル（どのモニターにあるかの判定に使う）。</param>
    public FullscreenWatcher(Func<IntPtr> getTimerHandle)
    {
        _getTimerHandle = getTimerHandle;
        _pollTimer = new DispatcherTimer { Interval = PollInterval };
        _pollTimer.Tick += (_, _) => Check();
    }

    /// <summary>タイマーと同じモニターで全画面アプリが前面にあるか。</summary>
    public bool IsFullscreenActive { get; private set; }

    /// <summary><see cref="IsFullscreenActive"/> が変わったときに発生する。</summary>
    public event EventHandler? FullscreenChanged;

    /// <summary>監視を開始 / 停止する。停止すると全画面ではない扱いに戻す。</summary>
    public bool IsEnabled
    {
        get => _pollTimer.IsEnabled;
        set
        {
            _pollTimer.IsEnabled = value;
            if (value)
            {
                Check();
            }
            else
            {
                SetFullscreen(false);
            }
        }
    }

    /// <summary>フォアグラウンドウィンドウが全画面アプリかどうかの判定。</summary>
    public static bool IsFullscreen(ForegroundWindowInfo info)
    {
        if (info.IsOwnProcess || info.IsShellWindow || DesktopClassNames.Contains(info.ClassName))
        {
            return false;
        }

        // タスクバーを自動的に隠す設定だと、普通に最大化しただけでもモニター全域を覆うので除外する
        if (info.IsMaximizedWithCaption)
        {
            return false;
        }

        return info.WindowMonitor.DeviceName == info.TimerMonitor.DeviceName
            && info.WindowRect.Contains(info.WindowMonitor.Bounds);
    }

    public void Dispose() => _pollTimer.Stop();

    private void Check()
    {
        var info = GetForegroundWindowInfo();
        SetFullscreen(info is not null && IsFullscreen(info));
    }

    private void SetFullscreen(bool value)
    {
        if (IsFullscreenActive == value)
        {
            return;
        }

        IsFullscreenActive = value;
        FullscreenChanged?.Invoke(this, EventArgs.Empty);
    }

    private ForegroundWindowInfo? GetForegroundWindowInfo()
    {
        var foreground = GetForegroundWindow();
        var timerHandle = _getTimerHandle();
        if (foreground == IntPtr.Zero || timerHandle == IntPtr.Zero)
        {
            return null;
        }

        var windowRect = MonitorHelper.GetWindowBounds(foreground);
        var windowMonitor = MonitorHelper.GetMonitorOf(foreground);
        var timerMonitor = MonitorHelper.GetMonitorOf(timerHandle);
        if (windowRect is null || windowMonitor is null || timerMonitor is null)
        {
            return null;
        }

        var className = new StringBuilder(256);
        GetClassName(foreground, className, className.Capacity);
        GetWindowThreadProcessId(foreground, out var processId);
        var style = (uint)GetWindowLongPtr(foreground, GWL_STYLE).ToInt64();

        return new ForegroundWindowInfo(
            windowRect.Value,
            windowMonitor,
            timerMonitor,
            className.ToString(),
            IsShellWindow: foreground == GetShellWindow(),
            IsOwnProcess: processId == _ownProcessId,
            IsMaximizedWithCaption: IsZoomed(foreground) && (style & WS_CAPTION) == WS_CAPTION);
    }
}
