using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class FullscreenDetectionTests
{
    private static readonly MonitorInfo Main = new(
        @"\\.\DISPLAY1", new ScreenRect(0, 0, 1920, 1080), new ScreenRect(0, 0, 1920, 1032), 1.0, IsPrimary: true);

    private static readonly MonitorInfo Sub = new(
        @"\\.\DISPLAY2", new ScreenRect(-1920, 0, 0, 1080), new ScreenRect(-1920, 0, 0, 1032), 1.0, IsPrimary: false);

    /// <summary>メインモニター全域を覆う、タイマーと同じモニターの全画面アプリ。</summary>
    private static ForegroundWindowInfo FullscreenApp() => new(
        WindowRect: Main.Bounds,
        WindowMonitor: Main,
        TimerMonitor: Main,
        ClassName: "UnityWndClass",
        IsShellWindow: false,
        IsOwnProcess: false,
        IsMaximizedWithCaption: false);

    [Fact]
    public void 同じモニター全域を覆うウィンドウは全画面()
    {
        Assert.True(FullscreenWatcher.IsFullscreen(FullscreenApp()));
    }

    [Fact]
    public void モニターより大きいウィンドウも全画面()
    {
        Assert.True(FullscreenWatcher.IsFullscreen(FullscreenApp() with { WindowRect = new ScreenRect(-8, -8, 1928, 1088) }));
    }

    [Fact]
    public void 作業領域だけの大きさなら全画面ではない()
    {
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { WindowRect = Main.WorkArea }));
    }

    [Fact]
    public void 別のモニターの全画面アプリは対象外()
    {
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { WindowRect = Sub.Bounds, WindowMonitor = Sub }));
    }

    [Theory]
    [InlineData("Progman")]
    [InlineData("WorkerW")]
    public void デスクトップは全画面扱いしない(string className)
    {
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { ClassName = className }));
    }

    [Fact]
    public void シェルウィンドウと自分のウィンドウは全画面扱いしない()
    {
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { IsShellWindow = true }));
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { IsOwnProcess = true }));
    }

    [Fact]
    public void タイトルバー付きの最大化ウィンドウは全画面扱いしない()
    {
        // タスクバーを自動的に隠す設定では、最大化でもモニター全域を覆う
        Assert.False(FullscreenWatcher.IsFullscreen(FullscreenApp() with { IsMaximizedWithCaption = true }));
    }
}
