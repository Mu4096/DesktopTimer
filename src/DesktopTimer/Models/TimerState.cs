namespace DesktopTimer.Models;

/// <summary>タイマーの状態（仕様書 3章）。</summary>
public enum TimerState
{
    /// <summary>待機中。プリセットボタンを表示する。</summary>
    Idle,

    /// <summary>計測中（カウントダウン / カウントアップ）。</summary>
    Running,

    /// <summary>時間切れ（カウントダウンのみ）。0 を点滅表示し、音を鳴らす。</summary>
    TimeUp,

    /// <summary>カウントアップを止めた状態。止めた時点の経過時間を表示し続ける。</summary>
    Stopped,
}

/// <summary>タイマーの計り方。</summary>
public enum TimerMode
{
    /// <summary>指定した時間から 0 まで減らす。</summary>
    CountDown,

    /// <summary>0 から経過時間を増やす（終わりはなく、止めるまで続く）。</summary>
    CountUp,
}
