using System.Windows.Threading;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.ViewModels;

/// <summary>タイマーウィンドウの表示用プロパティとコマンド。</summary>
public sealed class TimerViewModel : ObservableObject
{
    /// <summary>表示更新の周期（仕様書 11章）。</summary>
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(200);

    private readonly TimerService _timer;
    private readonly DispatcherTimer _ticker;
    private readonly AppSettings _settings;
    private readonly TimeSpan _presetUnit;

    private string _timeText = "";
    private string _subText = "";
    private double _progress;
    private double _minuteProgress;
    private bool _isWarning;

    /// <param name="presetUnit">プリセット・警告時間の設定値1あたりの時間。通常は1分（動作確認用に1秒にできる）。</param>
    public TimerViewModel(TimerService timer, AppSettings settings, TimeSpan presetUnit)
    {
        _timer = timer;
        _settings = settings;
        _presetUnit = presetUnit;

        _ticker = new DispatcherTimer(DispatcherPriority.Normal) { Interval = UpdateInterval };
        _ticker.Tick += (_, _) => Refresh();

        StartPreset1Command = new RelayCommand(() => _timer.Start(Preset1), () => CanStartNew);
        StartPreset2Command = new RelayCommand(() => _timer.Start(Preset2), () => CanStartNew);
        StartCountUpCommand = new RelayCommand(() => _timer.StartCountUp(), () => CanStartNew);
        CancelCommand = new RelayCommand(CancelOrStop, () => State != TimerState.Idle);
        DismissCommand = new RelayCommand(() => _timer.Cancel(), () => State is TimerState.TimeUp or TimerState.Stopped);

        _timer.StateChanged += (_, _) => OnStateChanged();
        _settings.PropertyChanged += (_, e) => OnSettingsChanged(e.PropertyName);
        Refresh();
    }

    public TimerState State => _timer.State;

    public bool IsIdle => State == TimerState.Idle;

    public bool IsTimeUp => State == TimerState.TimeUp;

    /// <summary>止めたカウントアップの結果を表示中か。</summary>
    public bool IsStopped => State == TimerState.Stopped;

    /// <summary>カウントアップの表示（計測中・停止後）か。</summary>
    public bool IsCountUp => State != TimerState.Idle && _timer.Mode == TimerMode.CountUp;

    /// <summary>アラーム（指定時刻までのカウントダウン）の表示（待機中・TimeUp）か。</summary>
    public bool IsAlarm => State != TimerState.Idle && _timer.AlarmTime is not null;

    /// <summary>カウントアップを計測中か（キャンセル操作が「停止」になる）。</summary>
    public bool IsCountingUp => State == TimerState.Running && _timer.Mode == TimerMode.CountUp;

    /// <summary>時間の表示（Running / TimeUp / Stopped）を出すかどうか。</summary>
    public bool IsTimeDisplayVisible => State != TimerState.Idle;

    /// <summary>新しく開始できるか（Running 中以外）。プリセット・時間指定・アラーム・カウントアップの開始で共通。</summary>
    public bool CanStartNew => State != TimerState.Running;

    /// <summary>キャンセル操作の文言。カウントアップの計測中は結果を残して止めるので「停止」。</summary>
    public string CancelLabel => IsCountingUp ? "停止" : "キャンセル";

    public string Preset1Label => TimeFormat.PresetLabel(Preset1);

    public string Preset2Label => TimeFormat.PresetLabel(Preset2);

    private TimeSpan Preset1 => _settings.Preset1Minutes * _presetUnit;

    private TimeSpan Preset2 => _settings.Preset2Minutes * _presetUnit;

    /// <summary>警告色に変わる残り時間。0 なら警告しない。</summary>
    private TimeSpan WarningThreshold => _settings.WarningMinutes * _presetUnit;

    /// <summary>大きく出す時間。カウントダウンは残り時間、カウントアップは経過時間。</summary>
    public string TimeText
    {
        get => _timeText;
        private set => SetProperty(ref _timeText, value);
    }

    /// <summary>時間の下の小さい文字。例：経過 02:18 / 10:00、7:30 にアラーム、カウントアップ中</summary>
    public string SubText
    {
        get => _subText;
        private set => SetProperty(ref _subText, value);
    }

    /// <summary>カウントダウンの経過割合（0〜1）。</summary>
    public double Progress
    {
        get => _progress;
        private set => SetProperty(ref _progress, value);
    }

    /// <summary>カウントアップで、今の1分のうち経過した割合（0〜1）。進捗バーが1分ごとに満ちる。</summary>
    public double MinuteProgress
    {
        get => _minuteProgress;
        private set => SetProperty(ref _minuteProgress, value);
    }

    /// <summary>
    /// 警告色で表示するか（仕様書 4.7）。残り時間が警告閾値以下になったカウントダウン中と、TimeUp 中。
    /// </summary>
    public bool IsWarning
    {
        get => _isWarning;
        private set => SetProperty(ref _isWarning, value);
    }

    /// <summary>プリセット1を開始（Running 中以外で有効）。</summary>
    public RelayCommand StartPreset1Command { get; }

    /// <summary>プリセット2を開始（Running 中以外で有効）。</summary>
    public RelayCommand StartPreset2Command { get; }

    /// <summary>カウントアップを開始（Running 中以外で有効）。</summary>
    public RelayCommand StartCountUpCommand { get; }

    /// <summary>
    /// キャンセル / TimeUp 解除 / カウントアップの停止（Idle 以外で有効）。
    /// カウントアップの計測中は Idle に戻さず、結果を表示したまま止める。
    /// </summary>
    public RelayCommand CancelCommand { get; }

    /// <summary>
    /// ウィンドウの左クリックによる TimeUp 解除・止めたカウントアップを閉じる操作。
    /// TimeUp / Stopped でのみ有効（Running 中のクリックは何もしない）。
    /// </summary>
    public RelayCommand DismissCommand { get; }

    /// <summary>指定した時間でカウントダウンを開始する。Running 中なら何もせず false。</summary>
    public bool StartCustom(TimeSpan duration) => CanStartNew && _timer.Start(duration);

    /// <summary>
    /// アラームを開始する（alarmTime までの delay のカウントダウン）。Running 中なら何もせず false。
    /// </summary>
    public bool StartAlarm(TimeSpan delay, TimeOnly alarmTime) => CanStartNew && _timer.Start(delay, alarmTime);

    private void CancelOrStop()
    {
        if (IsCountingUp)
        {
            _timer.Stop();
        }
        else
        {
            _timer.Cancel();
        }
    }

    private void OnSettingsChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(AppSettings.Preset1Minutes):
                OnPropertyChanged(nameof(Preset1Label));
                break;
            case nameof(AppSettings.Preset2Minutes):
                OnPropertyChanged(nameof(Preset2Label));
                break;
            case nameof(AppSettings.WarningMinutes):
                Refresh();
                break;
        }
    }

    private void OnStateChanged()
    {
        // TimeUp・Stopped 中は表示が変わらない（点滅はビュー側のアニメーション）ので、更新は Running 中だけ回す
        _ticker.IsEnabled = State == TimerState.Running;

        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsTimeUp));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(IsCountUp));
        OnPropertyChanged(nameof(IsAlarm));
        OnPropertyChanged(nameof(IsCountingUp));
        OnPropertyChanged(nameof(IsTimeDisplayVisible));
        OnPropertyChanged(nameof(CanStartNew));
        OnPropertyChanged(nameof(CancelLabel));
        StartPreset1Command.RaiseCanExecuteChanged();
        StartPreset2Command.RaiseCanExecuteChanged();
        StartCountUpCommand.RaiseCanExecuteChanged();
        CancelCommand.RaiseCanExecuteChanged();
        DismissCommand.RaiseCanExecuteChanged();

        Refresh();
    }

    private void Refresh()
    {
        // ここで TimeUp に遷移した場合は OnStateChanged から再入して表示が更新される
        _timer.Update();

        if (_timer.Mode == TimerMode.CountUp)
        {
            var elapsed = _timer.Elapsed;
            TimeText = TimeFormat.Format(elapsed);
            SubText = "カウントアップ中";
            Progress = 0;
            MinuteProgress = (double)(elapsed.Ticks % TimeSpan.TicksPerMinute) / TimeSpan.TicksPerMinute;
            IsWarning = false;
            return;
        }

        TimeText = TimeFormat.FormatRemaining(_timer.Remaining);
        SubText = _timer.AlarmTime is { } alarmTime
            ? $"{TimeFormat.ClockLabel(alarmTime)} にアラーム"
            : $"経過 {TimeFormat.Format(_timer.Elapsed)} / {TimeFormat.Format(_timer.Duration)}";
        Progress = _timer.Progress;
        MinuteProgress = 0;
        IsWarning = State switch
        {
            TimerState.TimeUp => true,
            TimerState.Running => WarningThreshold > TimeSpan.Zero && _timer.Remaining <= WarningThreshold,
            _ => false,
        };
    }
}
