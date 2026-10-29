using System.Collections;
using System.ComponentModel;
using System.Windows.Threading;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.ViewModels;

/// <summary>
/// 「時刻を指定してアラーム」画面。時・分（24時間制）の2欄を受け付け、その時刻までのカウントダウンを開始する。
/// 過ぎた時刻なら翌日のその時刻にする。開始したら時刻を設定に記憶する。
/// 入力の誤りは INotifyDataErrorInfo で通知する（入力欄がピンクの枠になる）。
/// </summary>
public sealed class AlarmViewModel : ObservableObject, INotifyDataErrorInfo, ITimeFieldStepper, IDisposable
{
    /// <summary>鳴る時刻の案内（「あと ○分」）を更新する周期。</summary>
    private static readonly TimeSpan ScheduleRefreshInterval = TimeSpan.FromSeconds(1);

    private readonly AppSettings _settings;
    private readonly TimerViewModel _timer;
    private readonly DispatcherTimer _scheduleTicker;

    private string _hourText;
    private string _minuteText;
    private string _scheduleText = "";

    public AlarmViewModel(AppSettings settings, TimerViewModel timer)
    {
        _settings = settings;
        _timer = timer;

        var initial = settings.LastAlarmMinutes is { } minutes
            ? TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minutes))
            : AlarmInput.GetInitialTime(DateTime.Now);
        _hourText = initial.Hour.ToString();
        _minuteText = initial.Minute.ToString("00");

        StartCommand = new RelayCommand(Start, () => _timer.CanStartNew && TryGetTime(out _));
        _timer.PropertyChanged += OnTimerPropertyChanged;

        // 開いたまま時間が経っても「今日 / 明日」「あと ○分」が合うように更新し続ける
        _scheduleTicker = new DispatcherTimer { Interval = ScheduleRefreshInterval };
        _scheduleTicker.Tick += (_, _) => UpdateScheduleText();
        _scheduleTicker.Start();
        UpdateScheduleText();
    }

    /// <summary>開始してこの画面を閉じてよいときに発生する。</summary>
    public event EventHandler? CloseRequested;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public string HourText
    {
        get => _hourText;
        set => SetField(ref _hourText, value);
    }

    public string MinuteText
    {
        get => _minuteText;
        set => SetField(ref _minuteText, value);
    }

    /// <summary>入力が誤っている欄があるか（INotifyDataErrorInfo）。</summary>
    public bool HasErrors => GetError(nameof(HourText)) is not null || GetError(nameof(MinuteText)) is not null;

    /// <summary>鳴る時刻の案内（例：今日の 15:00 に鳴ります（あと 36分））。入力が誤っている間は空。</summary>
    public string ScheduleText
    {
        get => _scheduleText;
        private set => SetProperty(ref _scheduleText, value);
    }

    /// <summary>
    /// 開始できない理由。開始できる間は空。
    /// タイマーが動いている間は開始できないことを伝える（ホットキーで開始された場合など）。
    /// </summary>
    public string Hint => !_timer.CanStartNew ? "タイマーの動作中は開始できません" : "";

    public RelayCommand StartCommand { get; }

    public IEnumerable GetErrors(string? propertyName) =>
        GetError(propertyName) is { } error ? new[] { error } : Array.Empty<string>();

    /// <summary>↑↓キー・ホイールで、指定した欄の値を増減する。</summary>
    public void Step(string propertyName, int delta)
    {
        switch (propertyName)
        {
            case nameof(HourText):
                HourText = DurationInput.Step(HourText, delta, AlarmInput.HourMax).ToString();
                break;
            case nameof(MinuteText):
                MinuteText = DurationInput.Step(MinuteText, delta, AlarmInput.MinuteMax).ToString("00");
                break;
        }
    }

    public void Dispose()
    {
        _scheduleTicker.Stop();
        _timer.PropertyChanged -= OnTimerPropertyChanged;
    }

    private bool TryGetTime(out TimeOnly time) => AlarmInput.TryCompose(HourText, MinuteText, out time);

    private void Start()
    {
        // 待つ時間は開始する瞬間の現在時刻から求める
        if (!TryGetTime(out var time) || !_timer.StartAlarm(AlarmInput.GetDelay(DateTime.Now, time), time))
        {
            return;
        }

        _settings.LastAlarmMinutes = (int)time.ToTimeSpan().TotalMinutes;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateScheduleText()
    {
        ScheduleText = TryGetTime(out var time) ? AlarmInput.DescribeSchedule(DateTime.Now, time) : "";
    }

    private void SetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        UpdateScheduleText();
        StartCommand.RaiseCanExecuteChanged();
    }

    private string? GetError(string? propertyName) => propertyName switch
    {
        nameof(HourText) when !DurationInput.TryParseField(HourText, AlarmInput.HourMax, out _)
            => $"0〜{AlarmInput.HourMax} の整数を入力してください",
        nameof(MinuteText) when !DurationInput.TryParseField(MinuteText, AlarmInput.MinuteMax, out _)
            => $"0〜{AlarmInput.MinuteMax} の整数を入力してください",
        _ => null,
    };

    private void OnTimerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TimerViewModel.CanStartNew))
        {
            OnPropertyChanged(nameof(Hint));
            StartCommand.RaiseCanExecuteChanged();
        }
    }
}
