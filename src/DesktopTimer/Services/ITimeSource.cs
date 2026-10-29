namespace DesktopTimer.Services;

/// <summary>
/// 単調増加する時刻の取得元。テストで時刻を差し替えられるように抽象化している。
/// </summary>
public interface ITimeSource
{
    /// <summary>任意の基準点からの経過時間。</summary>
    TimeSpan Now { get; }
}

/// <summary>
/// GetTickCount64 ベースの時刻。システム時計の変更に影響されず、
/// スリープ・休止状態の間も進むため、復帰後も実時間で残り時間を計算できる（仕様書 11章）。
/// </summary>
public sealed class SystemTimeSource : ITimeSource
{
    public TimeSpan Now => TimeSpan.FromMilliseconds(Environment.TickCount64);
}
