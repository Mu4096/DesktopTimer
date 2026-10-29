using System.Collections;
using System.ComponentModel;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.ViewModels;

/// <summary>
/// 「時間を指定して開始」画面。時・分・秒の3欄を受け付け、開始したら指定時間を設定に記憶する。
/// 入力の誤りは INotifyDataErrorInfo で通知する（入力欄がピンクの枠になる）。
/// </summary>
public sealed class CustomTimerViewModel : ObservableObject, INotifyDataErrorInfo, ITimeFieldStepper, IDisposable
{
    private readonly AppSettings _settings;
    private readonly TimerViewModel _timer;

    private string _hoursText;
    private string _minutesText;
    private string _secondsText;

    public CustomTimerViewModel(AppSettings settings, TimerViewModel timer)
    {
        _settings = settings;
        _timer = timer;

        var (h, m, s) = DurationInput.Split(TimeSpan.FromSeconds(settings.LastCustomSeconds));
        _hoursText = h.ToString();
        _minutesText = m.ToString();
        _secondsText = s.ToString("00");

        StartCommand = new RelayCommand(Start, () => _timer.CanStartNew && TryGetDuration(out _));
        _timer.PropertyChanged += OnTimerPropertyChanged;
    }

    /// <summary>開始してこの画面を閉じてよいときに発生する。</summary>
    public event EventHandler? CloseRequested;

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public string HoursText
    {
        get => _hoursText;
        set => SetField(ref _hoursText, value);
    }

    public string MinutesText
    {
        get => _minutesText;
        set => SetField(ref _minutesText, value);
    }

    public string SecondsText
    {
        get => _secondsText;
        set => SetField(ref _secondsText, value);
    }

    /// <summary>入力が誤っている欄があるか（INotifyDataErrorInfo）。</summary>
    public bool HasErrors => GetError(nameof(HoursText)) is not null
        || GetError(nameof(MinutesText)) is not null
        || GetError(nameof(SecondsText)) is not null;

    /// <summary>
    /// 開始ボタンの下に出す案内。入力が正しい間は空。
    /// タイマーが動いている間は開始できないことも伝える（ホットキーで開始された場合など）。
    /// </summary>
    public string Hint => !_timer.CanStartNew ? "タイマーの動作中は開始できません"
        : HasErrors ? ""
        : !TryGetDuration(out _) ? "1秒以上を指定してください"
        : "";

    public RelayCommand StartCommand { get; }

    public IEnumerable GetErrors(string? propertyName) =>
        GetError(propertyName) is { } error ? new[] { error } : Array.Empty<string>();

    /// <summary>↑↓キー・ホイールで、指定した欄の値を増減する。</summary>
    public void Step(string propertyName, int delta)
    {
        switch (propertyName)
        {
            case nameof(HoursText):
                HoursText = DurationInput.Step(HoursText, delta, DurationInput.HoursMax).ToString();
                break;
            case nameof(MinutesText):
                MinutesText = DurationInput.Step(MinutesText, delta, DurationInput.MinutesMax).ToString();
                break;
            case nameof(SecondsText):
                SecondsText = DurationInput.Step(SecondsText, delta, DurationInput.SecondsMax).ToString("00");
                break;
        }
    }

    public void Dispose()
    {
        _timer.PropertyChanged -= OnTimerPropertyChanged;
    }

    private bool TryGetDuration(out TimeSpan duration) =>
        DurationInput.TryCompose(HoursText, MinutesText, SecondsText, out duration);

    private void Start()
    {
        if (!TryGetDuration(out var duration) || !_timer.StartCustom(duration))
        {
            return;
        }

        _settings.LastCustomSeconds = (int)duration.TotalSeconds;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void SetField(ref string field, string value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(Hint));
        StartCommand.RaiseCanExecuteChanged();
    }

    private string? GetError(string? propertyName) => propertyName switch
    {
        nameof(HoursText) when !DurationInput.TryParseField(HoursText, DurationInput.HoursMax, out _)
            => $"0〜{DurationInput.HoursMax} の整数を入力してください",
        nameof(MinutesText) when !DurationInput.TryParseField(MinutesText, DurationInput.MinutesMax, out _)
            => $"0〜{DurationInput.MinutesMax} の整数を入力してください",
        nameof(SecondsText) when !DurationInput.TryParseField(SecondsText, DurationInput.SecondsMax, out _)
            => $"0〜{DurationInput.SecondsMax} の整数を入力してください",
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
