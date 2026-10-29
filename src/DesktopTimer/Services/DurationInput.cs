namespace DesktopTimer.Services;

/// <summary>
/// 「時間を指定して開始」の入力（時・分・秒の3欄）の解釈。UI に依存しない純粋な計算だけを置く。
/// </summary>
public static class DurationInput
{
    public const int HoursMax = 23;
    public const int MinutesMax = 59;
    public const int SecondsMax = 59;

    /// <summary>指定できる最長の時間（23:59:59）。</summary>
    public static TimeSpan MaxDuration { get; } = new(HoursMax, MinutesMax, SecondsMax);

    /// <summary>
    /// 1欄の入力を 0〜max の整数として読む。空欄は 0 とみなす。
    /// </summary>
    public static bool TryParseField(string? text, int max, out int value)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            value = 0;
            return true;
        }

        // 全角数字も受け付ける（IME がオンのまま入力されることがあるため）
        var normalized = text.Trim().Normalize(System.Text.NormalizationForm.FormKC);
        if (normalized.All(char.IsAsciiDigit) && int.TryParse(normalized, out value) && value <= max)
        {
            return true;
        }

        value = 0;
        return false;
    }

    /// <summary>
    /// 3欄から時間を組み立てる。いずれかの欄が不正、または合計が 0 なら false。
    /// </summary>
    public static bool TryCompose(string? hours, string? minutes, string? seconds, out TimeSpan duration)
    {
        if (TryParseField(hours, HoursMax, out var h)
            && TryParseField(minutes, MinutesMax, out var m)
            && TryParseField(seconds, SecondsMax, out var s)
            && (h, m, s) != (0, 0, 0))
        {
            duration = new TimeSpan(h, m, s);
            return true;
        }

        duration = TimeSpan.Zero;
        return false;
    }

    /// <summary>時間を3欄の値に分ける（入力欄の初期値用）。範囲外は 1秒〜23:59:59 に収める。</summary>
    public static (int Hours, int Minutes, int Seconds) Split(TimeSpan duration)
    {
        var clamped = duration < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1)
            : duration > MaxDuration ? MaxDuration
            : duration;
        return (clamped.Hours, clamped.Minutes, clamped.Seconds);
    }

    /// <summary>
    /// ↑↓キー・ホイールでの増減。現在の値に delta を足して 0〜max に収める（繰り上がりはしない）。
    /// 現在の入力が不正なら 0 から数える。
    /// </summary>
    public static int Step(string? text, int delta, int max)
    {
        var current = TryParseField(text, max, out var value) ? value : 0;
        return Math.Clamp(current + delta, 0, max);
    }
}
