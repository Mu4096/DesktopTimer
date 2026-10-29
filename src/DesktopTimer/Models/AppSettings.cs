using System.Windows.Input;
using DesktopTimer.Services;
using DesktopTimer.ViewModels;

namespace DesktopTimer.Models;

/// <summary>
/// アプリの設定（仕様書 10.1）。変更は PropertyChanged で通知し、即時反映・即時保存する。
/// </summary>
public sealed class AppSettings : ObservableObject
{
    public const int PresetMinutesMin = 1;
    public const int PresetMinutesMax = 180;
    public const int VolumePercentMin = 0;
    public const int VolumePercentMax = 100;
    public const int AutoStopSecondsMin = 5;
    public const int AutoStopSecondsMax = 600;
    public const int WarningMinutesMin = 0;
    public const int WarningMinutesMax = 10;
    public const int OpacityPercentMin = 30;
    public const int OpacityPercentMax = 100;
    public const int ScalePercentMin = 50;
    public const int ScalePercentMax = 300;
    public const int LastCustomSecondsMin = 1;
    public const int LastCustomSecondsMax = 23 * 3600 + 59 * 60 + 59;
    public const int LastAlarmMinutesMin = 0;
    public const int LastAlarmMinutesMax = 23 * 60 + 59;

    private int _preset1Minutes = 10;
    private int _preset2Minutes = 30;
    private string _soundId = SoundCatalog.DefaultSoundId;
    private int _volumePercent = 80;
    private int _autoStopSeconds = 60;
    private bool _toastEnabled = true;
    private int _warningMinutes = 1;
    private int _opacityPercent = 100;
    private int _scalePercent = 100;
    private bool _hideOnFullscreen = true;
    private bool _autoStart = true;
    private HotkeySetting _preset1Hotkey = new(ModifierKeys.Control | ModifierKeys.Alt, Key.D1);
    private HotkeySetting _preset2Hotkey = new(ModifierKeys.Control | ModifierKeys.Alt, Key.D2);
    private HotkeySetting _cancelHotkey = new(ModifierKeys.Control | ModifierKeys.Alt, Key.D0);
    private WindowPosition? _windowPosition;
    private int _lastCustomSeconds = 5 * 60;
    private int? _lastAlarmMinutes;

    /// <summary>プリセット1の時間（分）。</summary>
    public int Preset1Minutes
    {
        get => _preset1Minutes;
        set => SetProperty(ref _preset1Minutes, value);
    }

    /// <summary>プリセット2の時間（分）。</summary>
    public int Preset2Minutes
    {
        get => _preset2Minutes;
        set => SetProperty(ref _preset2Minutes, value);
    }

    /// <summary>タイムアップ音の ID（<see cref="SoundCatalog"/> 参照）。</summary>
    public string SoundId
    {
        get => _soundId;
        set => SetProperty(ref _soundId, value);
    }

    /// <summary>音量（%）。0 で無音。</summary>
    public int VolumePercent
    {
        get => _volumePercent;
        set => SetProperty(ref _volumePercent, value);
    }

    /// <summary>音の自動停止時間（秒）。</summary>
    public int AutoStopSeconds
    {
        get => _autoStopSeconds;
        set => SetProperty(ref _autoStopSeconds, value);
    }

    /// <summary>タイムアップ時のトースト通知。</summary>
    public bool ToastEnabled
    {
        get => _toastEnabled;
        set => SetProperty(ref _toastEnabled, value);
    }

    /// <summary>警告色に変わる残り時間（分）。0 で無効。</summary>
    public int WarningMinutes
    {
        get => _warningMinutes;
        set => SetProperty(ref _warningMinutes, value);
    }

    /// <summary>ウィンドウの不透明度（%）。</summary>
    public int OpacityPercent
    {
        get => _opacityPercent;
        set => SetProperty(ref _opacityPercent, value);
    }

    /// <summary>ウィンドウのサイズ（基準サイズに対する %）。</summary>
    public int ScalePercent
    {
        get => _scalePercent;
        set => SetProperty(ref _scalePercent, value);
    }

    /// <summary>全画面アプリの表示中にタイマーを隠す。</summary>
    public bool HideOnFullscreen
    {
        get => _hideOnFullscreen;
        set => SetProperty(ref _hideOnFullscreen, value);
    }

    /// <summary>Windows 起動時に自動起動する。</summary>
    public bool AutoStart
    {
        get => _autoStart;
        set => SetProperty(ref _autoStart, value);
    }

    public HotkeySetting Preset1Hotkey
    {
        get => _preset1Hotkey;
        set => SetProperty(ref _preset1Hotkey, value);
    }

    public HotkeySetting Preset2Hotkey
    {
        get => _preset2Hotkey;
        set => SetProperty(ref _preset2Hotkey, value);
    }

    /// <summary>キャンセル / TimeUp 解除のホットキー。</summary>
    public HotkeySetting CancelHotkey
    {
        get => _cancelHotkey;
        set => SetProperty(ref _cancelHotkey, value);
    }

    /// <summary>ウィンドウ位置。null ならプライマリモニター右下（デフォルト位置）。</summary>
    public WindowPosition? WindowPosition
    {
        get => _windowPosition;
        set => SetProperty(ref _windowPosition, value);
    }

    /// <summary>
    /// 「時間を指定して開始」で前回指定した時間（秒）。次に開いた時の初期値にする。
    /// 設定画面には出さず、「すべてデフォルトに戻す」の対象にもしない（ウィンドウ位置と同じく操作の記憶のため）。
    /// </summary>
    public int LastCustomSeconds
    {
        get => _lastCustomSeconds;
        set => SetProperty(ref _lastCustomSeconds, value);
    }

    /// <summary>
    /// 「時刻を指定してアラーム」で前回指定した時刻（0:00 からの分）。次に開いた時の初期値にする。
    /// null なら未指定（初期値は開いた時刻の次の正時）。扱いは <see cref="LastCustomSeconds"/> と同じ。
    /// </summary>
    public int? LastAlarmMinutes
    {
        get => _lastAlarmMinutes;
        set => SetProperty(ref _lastAlarmMinutes, value);
    }

    /// <summary>操作に割り当てられたホットキー。</summary>
    public HotkeySetting GetHotkey(HotkeyAction action) => action switch
    {
        HotkeyAction.StartPreset1 => Preset1Hotkey,
        HotkeyAction.StartPreset2 => Preset2Hotkey,
        HotkeyAction.Cancel => CancelHotkey,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    /// <summary>操作にホットキーを割り当てる。</summary>
    public void SetHotkey(HotkeyAction action, HotkeySetting hotkey)
    {
        switch (action)
        {
            case HotkeyAction.StartPreset1:
                Preset1Hotkey = hotkey;
                break;
            case HotkeyAction.StartPreset2:
                Preset2Hotkey = hotkey;
                break;
            case HotkeyAction.Cancel:
                CancelHotkey = hotkey;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    /// <summary>ホットキーの設定プロパティ名かどうか。</summary>
    public static bool IsHotkeyProperty(string? propertyName) =>
        propertyName is nameof(Preset1Hotkey) or nameof(Preset2Hotkey) or nameof(CancelHotkey);

    /// <summary>
    /// 範囲外の値や欠けた値をデフォルト・範囲内に直す。ファイルから読み込んだ直後に呼ぶ。
    /// </summary>
    public void Normalize()
    {
        var defaults = new AppSettings();

        Preset1Minutes = Math.Clamp(Preset1Minutes, PresetMinutesMin, PresetMinutesMax);
        Preset2Minutes = Math.Clamp(Preset2Minutes, PresetMinutesMin, PresetMinutesMax);
        VolumePercent = Math.Clamp(VolumePercent, VolumePercentMin, VolumePercentMax);
        AutoStopSeconds = Math.Clamp(AutoStopSeconds, AutoStopSecondsMin, AutoStopSecondsMax);
        WarningMinutes = Math.Clamp(WarningMinutes, WarningMinutesMin, WarningMinutesMax);
        OpacityPercent = Math.Clamp(OpacityPercent, OpacityPercentMin, OpacityPercentMax);
        ScalePercent = Math.Clamp(ScalePercent, ScalePercentMin, ScalePercentMax);
        LastCustomSeconds = Math.Clamp(LastCustomSeconds, LastCustomSecondsMin, LastCustomSecondsMax);
        if (LastAlarmMinutes is { } alarmMinutes)
        {
            LastAlarmMinutes = Math.Clamp(alarmMinutes, LastAlarmMinutesMin, LastAlarmMinutesMax);
        }

        // JSON に null が書かれていた場合の補完
        if (string.IsNullOrWhiteSpace(SoundId))
        {
            SoundId = defaults.SoundId;
        }

        Preset1Hotkey ??= defaults.Preset1Hotkey;
        Preset2Hotkey ??= defaults.Preset2Hotkey;
        CancelHotkey ??= defaults.CancelHotkey;
    }

    /// <summary>
    /// 設定項目をすべてデフォルトに戻す。ウィンドウ位置は「位置をリセット」で別に戻せるので維持する。
    /// 前回指定した時間・時刻（LastCustomSeconds / LastAlarmMinutes）も設定項目ではないので維持する。
    /// </summary>
    public void ResetToDefaults()
    {
        var defaults = new AppSettings();

        Preset1Minutes = defaults.Preset1Minutes;
        Preset2Minutes = defaults.Preset2Minutes;
        SoundId = defaults.SoundId;
        VolumePercent = defaults.VolumePercent;
        AutoStopSeconds = defaults.AutoStopSeconds;
        ToastEnabled = defaults.ToastEnabled;
        WarningMinutes = defaults.WarningMinutes;
        OpacityPercent = defaults.OpacityPercent;
        ScalePercent = defaults.ScalePercent;
        HideOnFullscreen = defaults.HideOnFullscreen;
        AutoStart = defaults.AutoStart;
        Preset1Hotkey = defaults.Preset1Hotkey;
        Preset2Hotkey = defaults.Preset2Hotkey;
        CancelHotkey = defaults.CancelHotkey;
    }
}

/// <summary>ホットキー（修飾キー＋キー）。Key が None なら割り当てなし。</summary>
public sealed record HotkeySetting(ModifierKeys Modifiers, Key Key)
{
    /// <summary>割り当てなし。</summary>
    public static HotkeySetting None { get; } = new(ModifierKeys.None, Key.None);

    /// <summary>表示用の文字列（例：Ctrl + Alt + 1）。</summary>
    public string ToDisplayString()
    {
        if (Key == Key.None)
        {
            return "なし";
        }

        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(Key switch
        {
            >= Key.D0 and <= Key.D9 => ((int)(Key - Key.D0)).ToString(),
            >= Key.NumPad0 and <= Key.NumPad9 => $"Num {(int)(Key - Key.NumPad0)}",
            _ => Key.ToString(),
        });

        return string.Join(" + ", parts);
    }
}

/// <summary>
/// ウィンドウ位置（仕様書 8.3）。左上のスクリーン座標（物理ピクセル）と、そのとき置かれていたモニターの識別情報。
/// </summary>
/// <param name="MonitorDeviceName">モニターの識別情報（例：\\.\DISPLAY2）。</param>
/// <param name="MonitorLeft">保存時のそのモニターの左端（並べ替えられた時に同じモニター上へ追従させるため）。</param>
/// <param name="MonitorTop">保存時のそのモニターの上端。</param>
public sealed record WindowPosition(
    int X, int Y, string? MonitorDeviceName = null, int? MonitorLeft = null, int? MonitorTop = null);
