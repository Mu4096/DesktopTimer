using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>
/// タイマーの状態遷移と時間計算を担う。UI には依存しない。
/// 残り時間・経過時間は開始時刻との差分から求め、タイマーイベントの回数は数えない（仕様書 11章）。
/// </summary>
public sealed class TimerService
{
    private readonly ITimeSource _clock;
    private TimeSpan _startedAt;
    private TimeSpan _timeUpAt;
    private TimeSpan _stoppedElapsed;

    public TimerService(ITimeSource clock)
    {
        _clock = clock;
    }

    public TimerState State { get; private set; } = TimerState.Idle;

    /// <summary>今回の計り方。Idle では CountDown。</summary>
    public TimerMode Mode { get; private set; } = TimerMode.CountDown;

    /// <summary>今回のカウントダウンの長さ。Idle とカウントアップでは 0。</summary>
    public TimeSpan Duration { get; private set; }

    /// <summary>
    /// アラームとして開始した場合の、鳴らす時刻（表示・通知の文言用）。それ以外では null。
    /// アラームは指定時刻までのカウントダウンとして計る（Mode は CountDown）。
    /// </summary>
    public TimeOnly? AlarmTime { get; private set; }

    /// <summary>経過時間。カウントダウンでは Duration を超えない。</summary>
    public TimeSpan Elapsed => State switch
    {
        TimerState.Running when Mode == TimerMode.CountUp => _clock.Now - _startedAt,
        TimerState.Running => Min(_clock.Now - _startedAt, Duration),
        TimerState.TimeUp => Duration,
        TimerState.Stopped => _stoppedElapsed,
        _ => TimeSpan.Zero,
    };

    /// <summary>残り時間。負にはならない。カウントアップでは 0。</summary>
    public TimeSpan Remaining => Mode == TimerMode.CountUp ? TimeSpan.Zero : Duration - Elapsed;

    /// <summary>カウントダウンの経過割合（0〜1）。カウントアップでは 0。</summary>
    public double Progress => Duration > TimeSpan.Zero ? Elapsed / Duration : 0;

    /// <summary>TimeUp になってからの経過時間（音の自動停止などに使う）。TimeUp 以外では 0。</summary>
    public TimeSpan TimeSinceTimeUp => State == TimerState.TimeUp ? _clock.Now - _timeUpAt : TimeSpan.Zero;

    /// <summary>状態が変わったときに発生する。</summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// カウントダウンを開始する。Idle / TimeUp / Stopped から開始でき、Running 中は無視する（仕様書 3章）。
    /// </summary>
    /// <param name="alarmTime">アラームとして開始する場合の鳴らす時刻。duration はその時刻までの時間を渡す。</param>
    /// <returns>開始した場合 true。</returns>
    public bool Start(TimeSpan duration, TimeOnly? alarmTime = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(duration, TimeSpan.Zero);

        if (State == TimerState.Running)
        {
            return false;
        }

        Mode = TimerMode.CountDown;
        Duration = duration;
        AlarmTime = alarmTime;
        _startedAt = _clock.Now;
        SetState(TimerState.Running);
        return true;
    }

    /// <summary>
    /// カウントアップを開始する。開始できる状態は <see cref="Start"/> と同じ。
    /// </summary>
    /// <returns>開始した場合 true。</returns>
    public bool StartCountUp()
    {
        if (State == TimerState.Running)
        {
            return false;
        }

        Mode = TimerMode.CountUp;
        Duration = TimeSpan.Zero;
        AlarmTime = null;
        _startedAt = _clock.Now;
        SetState(TimerState.Running);
        return true;
    }

    /// <summary>
    /// カウントアップを止め、止めた時点の経過時間を保ったまま Stopped にする。
    /// カウントダウン中は何もしない（カウントダウンは Cancel で止める）。
    /// </summary>
    /// <returns>止めた場合 true。</returns>
    public bool Stop()
    {
        if (State != TimerState.Running || Mode != TimerMode.CountUp)
        {
            return false;
        }

        _stoppedElapsed = _clock.Now - _startedAt;
        SetState(TimerState.Stopped);
        return true;
    }

    /// <summary>
    /// キャンセル（Running → Idle）、TimeUp の解除（TimeUp → Idle）、止めたカウントアップを閉じる（Stopped → Idle）。
    /// </summary>
    /// <returns>Idle に戻した場合 true。もともと Idle なら false。</returns>
    public bool Cancel()
    {
        if (State == TimerState.Idle)
        {
            return false;
        }

        Mode = TimerMode.CountDown;
        Duration = TimeSpan.Zero;
        AlarmTime = null;
        _stoppedElapsed = TimeSpan.Zero;
        SetState(TimerState.Idle);
        return true;
    }

    /// <summary>
    /// 現在時刻を見て、カウントダウンの残りが 0 なら TimeUp に遷移させる。表示更新のたびに呼ぶ。
    /// スリープ復帰後に呼ばれた場合も、すでに 0 を過ぎていればここで TimeUp になる。
    /// </summary>
    public void Update()
    {
        if (State == TimerState.Running && Mode == TimerMode.CountDown && _clock.Now - _startedAt >= Duration)
        {
            _timeUpAt = _clock.Now;
            SetState(TimerState.TimeUp);
        }
    }

    private void SetState(TimerState state)
    {
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;
}
