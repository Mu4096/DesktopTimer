namespace DesktopTimer.Services;

/// <summary>時間表示の書式（仕様書 4.2）。</summary>
public static class TimeFormat
{
    /// <summary>
    /// 秒未満を切り捨てて表示する。60分未満は mm:ss、60分以上は h:mm:ss。
    /// 経過時間の表示に使う。
    /// </summary>
    public static string Format(TimeSpan time)
    {
        var totalSeconds = Math.Max(0, time.Ticks / TimeSpan.TicksPerSecond);
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds / 60 % 60;
        var seconds = totalSeconds % 60;

        return hours > 0
            ? $"{hours}:{minutes:00}:{seconds:00}"
            : $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 秒未満を切り上げて表示する。残り時間の表示に使う。
    /// 開始直後は設定時間そのもの（例：10:00）が表示され、00:00 になるのは時間切れの瞬間だけになる。
    /// </summary>
    public static string FormatRemaining(TimeSpan time)
    {
        var ticks = Math.Max(0, time.Ticks);
        var ceiledSeconds = (ticks + TimeSpan.TicksPerSecond - 1) / TimeSpan.TicksPerSecond;
        return Format(TimeSpan.FromSeconds(ceiledSeconds));
    }

    /// <summary>プリセットボタンのラベル（例：10分、90分）。分で割り切れない場合は秒で表す。</summary>
    public static string PresetLabel(TimeSpan duration)
    {
        return duration.Ticks % TimeSpan.TicksPerMinute == 0
            ? $"{(long)duration.TotalMinutes}分"
            : $"{(long)duration.TotalSeconds}秒";
    }

    /// <summary>時刻の表示（例：7:30、23:05）。アラームの時刻に使う。時は0埋めしない。</summary>
    public static string ClockLabel(TimeOnly time) => $"{time.Hour}:{time.Minute:00}";

    /// <summary>
    /// 時間の長さを文章向けに表す（例：25分、1時間30分、1分30秒）。0 の単位は省く。通知の文言に使う。
    /// </summary>
    public static string DurationLabel(TimeSpan duration)
    {
        var totalSeconds = Math.Max(0, duration.Ticks / TimeSpan.TicksPerSecond);
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds / 60 % 60;
        var seconds = totalSeconds % 60;

        var label = (hours > 0 ? $"{hours}時間" : "")
            + (minutes > 0 ? $"{minutes}分" : "")
            + (seconds > 0 ? $"{seconds}秒" : "");
        return label.Length > 0 ? label : "0秒";
    }
}
