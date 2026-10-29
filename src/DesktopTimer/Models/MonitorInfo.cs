namespace DesktopTimer.Models;

/// <summary>スクリーン座標（物理ピクセル）の矩形。Right / Bottom は含まない。</summary>
public readonly record struct ScreenRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public bool Contains(int x, int y) => x >= Left && x < Right && y >= Top && y < Bottom;

    /// <summary>other が完全にこの矩形の中に収まっているか。</summary>
    public bool Contains(ScreenRect other) =>
        other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;
}

/// <summary>モニター1台の情報。</summary>
/// <param name="DeviceName">識別情報（例：\\.\DISPLAY1）。</param>
/// <param name="Bounds">モニター全体。</param>
/// <param name="WorkArea">作業領域（タスクバーを除いた部分）。</param>
/// <param name="Scale">表示スケール（96 DPI = 1.0）。</param>
public sealed record MonitorInfo(string DeviceName, ScreenRect Bounds, ScreenRect WorkArea, double Scale, bool IsPrimary);
