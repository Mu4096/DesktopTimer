namespace DesktopTimer.Services;

/// <summary>
/// 「時刻を指定してアラーム」の入力（時・分の2欄、24時間制）の解釈と、鳴る時刻の計算。
/// UI に依存しない純粋な計算だけを置く。欄の読み方・増減は <see cref="DurationInput"/> と共通。
/// </summary>
public static class AlarmInput
{
    public const int HourMax = 23;
    public const int MinuteMax = 59;

    /// <summary>2欄から時刻を組み立てる。いずれかの欄が不正なら false。0:00 も指定できる。</summary>
    public static bool TryCompose(string? hour, string? minute, out TimeOnly time)
    {
        if (DurationInput.TryParseField(hour, HourMax, out var h)
            && DurationInput.TryParseField(minute, MinuteMax, out var m))
        {
            time = new TimeOnly(h, m);
            return true;
        }

        time = default;
        return false;
    }

    /// <summary>
    /// now の後で最初に time になるまでの時間。今日のその時刻を過ぎていれば（ちょうど同じ時刻も含む）翌日の時刻にする。
    /// 結果は 0 より大きく 24時間以下。
    /// </summary>
    public static TimeSpan GetDelay(DateTime now, TimeOnly time)
    {
        var target = now.Date + time.ToTimeSpan();
        if (target <= now)
        {
            target = target.AddDays(1);
        }

        return target - now;
    }

    /// <summary>
    /// 鳴る時刻の案内。例：「今日の 15:00 に鳴ります（あと 36分）」「明日の 7:30 に鳴ります（あと 16時間21分）」。
    /// 残りは分単位に切り上げる（1分未満でも「あと 1分」と表し、0分にはしない）。
    /// </summary>
    public static string DescribeSchedule(DateTime now, TimeOnly time)
    {
        var delay = GetDelay(now, time);
        var day = (now + delay).Date == now.Date ? "今日" : "明日";
        var ceiledMinutes = (delay.Ticks + TimeSpan.TicksPerMinute - 1) / TimeSpan.TicksPerMinute;
        return $"{day}の {TimeFormat.ClockLabel(time)} に鳴ります（あと {TimeFormat.DurationLabel(TimeSpan.FromMinutes(ceiledMinutes))}）";
    }

    /// <summary>初めて開いた時の初期値。now の次の正時（例：14:23 → 15:00、23:10 → 0:00）。</summary>
    public static TimeOnly GetInitialTime(DateTime now) => new((now.Hour + 1) % 24, 0);
}
