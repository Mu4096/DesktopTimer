using System.ComponentModel;
using System.Windows;
using DesktopTimer.Models;
using DesktopTimer.Services;
using DesktopTimer.ViewModels;
using DesktopTimer.Views;

namespace DesktopTimer;

public partial class App : Application
{
    /// <summary>多重起動防止用（仕様書 9章）。同じユーザーのセッション内で1つだけ。</summary>
    private const string SingleInstanceMutexName = @"Local\DesktopTimer.SingleInstance";

    /// <summary>2つ目の起動から、既存のインスタンスへ「表示して」と伝えるためのイベント。</summary>
    private const string ShowExistingEventName = @"Local\DesktopTimer.ShowExisting";

    private Mutex? _singleInstanceMutex;
    private bool _ownsSingleInstanceMutex;
    private EventWaitHandle? _showExistingEvent;
    private RegisteredWaitHandle? _showExistingWait;

    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private TimerService? _timer;
    private SoundService? _sound;
    private HotkeyService? _hotkeys;
    private UserSoundLibrary? _userSounds;
    private TrayIconService? _tray;
    private TimerViewModel? _timerViewModel;
    private TimerWindow? _timerWindow;
    private FullscreenWatcher? _fullscreen;

    /// <summary>
    /// ユーザーがタイマーを表示したいか（トレイの表示/非表示、右クリックメニューの「非表示にする」で切り替える）。
    /// 実際の表示は、これに加えて全画面アプリによる自動非表示も考慮して決める。
    /// </summary>
    private bool _userWantsTimerVisible = true;
    private SettingsWindow? _settingsWindow;
    private CustomTimerWindow? _customTimerWindow;
    private AlarmWindow? _alarmWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!TryBecomeSingleInstance())
        {
            // 既存のインスタンスを表示状態にして、こちらは何もせず終了する
            Shutdown();
            return;
        }

        _settingsService = new SettingsService(SettingsService.DefaultDirectoryPath);
        _settings = _settingsService.Load();
        _settingsService.EnableAutoSave(_settings);
        _settings.PropertyChanged += OnSettingsChanged;
        AutoStartService.Apply(_settings.AutoStart);

        _timer = new TimerService(new SystemTimeSource());
        _timer.StateChanged += (_, _) => OnTimerStateChanged();
        _sound = new SoundService();
        _userSounds = new UserSoundLibrary(UserSoundLibrary.DefaultDirectoryPath);
        // TimerService のイベント購読で生存し続ける
        _ = new AlarmController(_timer, _sound, _settings, _userSounds);

        var presetUnit = TimeSpan.FromMinutes(1);
#if DEBUG
        // 動作確認用：--short を付けて起動するとプリセットを「分」ではなく「秒」として扱う
        if (e.Args.Contains("--short"))
        {
            presetUnit = TimeSpan.FromSeconds(1);
        }
#endif

        _timerViewModel = new TimerViewModel(_timer, _settings, presetUnit);

        _hotkeys = new HotkeyService(_settings);
        _hotkeys.Pressed += (_, action) => OnHotkeyPressed(action);

        _timerWindow = new TimerWindow(_settings) { DataContext = _timerViewModel };
        _timerWindow.SettingsRequested += (_, _) => ShowSettings();
        _timerWindow.CustomTimerRequested += (_, _) => ShowCustomTimer();
        _timerWindow.AlarmRequested += (_, _) => ShowAlarm();
        _timerWindow.HideRequested += (_, _) => HideTimerWindow();

        _tray = new TrayIconService(
            isTimerVisible: () => _userWantsTimerVisible,
            canCancel: () => _timerViewModel.CancelCommand.CanExecute(null),
            cancelLabel: () => _timerViewModel.CancelLabel);
        _tray.ToggleVisibilityRequested += (_, _) => ToggleTimerVisibility();
        _tray.CancelRequested += (_, _) => _timerViewModel.CancelCommand.Execute(null);
        _tray.SettingsRequested += (_, _) => ShowSettings();
        _tray.ResetPositionRequested += (_, _) => _timerWindow.ResetPosition();
        _tray.ExitRequested += (_, _) => Shutdown();
        _tray.NotificationClicked += (_, _) => ShowTimerWindow();

        MainWindow = _timerWindow;
        MainWindow.Show();

        // 全画面アプリの検出（タイマーのハンドルが必要なので表示後に開始する）
        _fullscreen = new FullscreenWatcher(() => _timerWindow.Handle);
        _fullscreen.FullscreenChanged += (_, _) => UpdateTimerVisibility();
        _fullscreen.IsEnabled = _settings.HideOnFullscreen;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _settingsService?.Flush();
        _fullscreen?.Dispose();
        _tray?.Dispose();
        _hotkeys?.Dispose();
        _sound?.Dispose();

        _showExistingWait?.Unregister(null);
        _showExistingEvent?.Dispose();
        if (_ownsSingleInstanceMutex)
        {
            // 2つ目として起動した場合は所有していないので解放しない
            _singleInstanceMutex!.ReleaseMutex();
        }

        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// 最初のインスタンスなら true。すでに起動中なら、既存のインスタンスに表示を依頼して false。
    /// </summary>
    private bool TryBecomeSingleInstance()
    {
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            if (EventWaitHandle.TryOpenExisting(ShowExistingEventName, out var showExisting))
            {
                using (showExisting)
                {
                    showExisting.Set();
                }
            }

            return false;
        }

        _ownsSingleInstanceMutex = true;

        // 2つ目の起動からの合図を待ち受け、来たらタイマーを表示する
        _showExistingEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowExistingEventName);
        _showExistingWait = ThreadPool.RegisterWaitForSingleObject(
            _showExistingEvent,
            (_, _) => Dispatcher.BeginInvoke(ShowTimerWindow),
            state: null,
            Timeout.Infinite,
            executeOnlyOnce: false);
        return true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.AutoStart):
                AutoStartService.Apply(_settings!.AutoStart);
                break;
            case nameof(AppSettings.HideOnFullscreen):
                if (_fullscreen is not null)
                {
                    _fullscreen.IsEnabled = _settings!.HideOnFullscreen;
                }

                break;
        }
    }

    /// <summary>TimeUp になったら通知を出す（トレイから非表示にしていても出す）。</summary>
    private void OnTimerStateChanged()
    {
        if (_timer!.State == TimerState.TimeUp && _settings!.ToastEnabled)
        {
            var message = _timer.AlarmTime is { } alarmTime
                ? $"{TimeFormat.ClockLabel(alarmTime)} のアラームです。"
                : $"{TimeFormat.DurationLabel(_timer.Duration)}のタイマーが終了しました。";
            _tray?.ShowNotification("時間です", message);
        }
    }

    /// <summary>
    /// ホットキーの操作を実行する。Running 中のプリセット開始は、コマンドが無効なので無視される（仕様書 3章）。
    /// </summary>
    private void OnHotkeyPressed(HotkeyAction action)
    {
        var command = action switch
        {
            HotkeyAction.StartPreset1 => _timerViewModel!.StartPreset1Command,
            HotkeyAction.StartPreset2 => _timerViewModel!.StartPreset2Command,
            _ => _timerViewModel!.CancelCommand,
        };
        command.Execute(null);
    }

    private void ToggleTimerVisibility()
    {
        _userWantsTimerVisible = !_userWantsTimerVisible;
        UpdateTimerVisibility();
    }

    private void ShowTimerWindow()
    {
        _userWantsTimerVisible = true;
        UpdateTimerVisibility();
    }

    private void HideTimerWindow()
    {
        _userWantsTimerVisible = false;
        UpdateTimerVisibility();
    }

    /// <summary>
    /// タイマーの表示/非表示を反映する。ユーザーが非表示にしているか、全画面アプリの表示中は隠す（仕様書 8.2）。
    /// 全画面中に TimeUp になっても強制表示はしない（音と通知は AlarmController / OnTimerStateChanged で通常どおり出る）。
    /// </summary>
    private void UpdateTimerVisibility()
    {
        if (_timerWindow is null)
        {
            return;
        }

        var visible = _userWantsTimerVisible && _fullscreen is not { IsFullscreenActive: true };
        if (visible)
        {
            _timerWindow.Show();
        }
        else
        {
            _timerWindow.Hide();
        }
    }

    /// <summary>「時間を指定して開始」の小窓をタイマーの近くに開く。すでに開いていれば前面に出す。</summary>
    private void ShowCustomTimer()
    {
        if (_customTimerWindow is not null)
        {
            _customTimerWindow.Activate();
            return;
        }

        var viewModel = new CustomTimerViewModel(_settings!, _timerViewModel!);
        _customTimerWindow = new CustomTimerWindow(viewModel, _timerWindow!.Handle);
        _customTimerWindow.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _customTimerWindow = null;
        };
        _customTimerWindow.Show();
        _customTimerWindow.Activate();
    }

    /// <summary>「時刻を指定してアラーム」の小窓をタイマーの近くに開く。すでに開いていれば前面に出す。</summary>
    private void ShowAlarm()
    {
        if (_alarmWindow is not null)
        {
            _alarmWindow.Activate();
            return;
        }

        var viewModel = new AlarmViewModel(_settings!, _timerViewModel!);
        _alarmWindow = new AlarmWindow(viewModel, _timerWindow!.Handle);
        _alarmWindow.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _alarmWindow = null;
        };
        _alarmWindow.Show();
        _alarmWindow.Activate();
    }

    /// <summary>設定画面を開く。すでに開いていれば前面に出す。</summary>
    private void ShowSettings()
    {
        if (_settingsWindow is not null)
        {
            if (_settingsWindow.WindowState == WindowState.Minimized)
            {
                _settingsWindow.WindowState = WindowState.Normal;
            }

            _settingsWindow.Activate();
            return;
        }

        var viewModel = new SettingsViewModel(_settings!, _hotkeys!, _userSounds!);
        _settingsWindow = new SettingsWindow { DataContext = viewModel };
        _settingsWindow.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _settingsWindow = null;
        };
        _settingsWindow.Show();
    }
}
