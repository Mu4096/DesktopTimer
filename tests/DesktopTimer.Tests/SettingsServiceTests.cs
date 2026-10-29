using System.IO;
using System.Windows.Input;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DesktopTimerTests", Guid.NewGuid().ToString("N"));
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _service = new SettingsService(_directory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void ファイルが無ければデフォルト値()
    {
        var settings = _service.Load();

        Assert.Equal(10, settings.Preset1Minutes);
        Assert.Equal(30, settings.Preset2Minutes);
        Assert.Equal(SoundCatalog.DefaultSoundId, settings.SoundId);
        Assert.Equal(80, settings.VolumePercent);
        Assert.Equal(60, settings.AutoStopSeconds);
        Assert.Equal(new HotkeySetting(ModifierKeys.Control | ModifierKeys.Alt, Key.D1), settings.Preset1Hotkey);
        Assert.Null(settings.WindowPosition);
    }

    [Fact]
    public void 保存した内容を読み込める()
    {
        var settings = new AppSettings
        {
            Preset1Minutes = 25,
            SoundId = "user:ベル.mp3",
            VolumePercent = 0,
            ToastEnabled = false,
            CancelHotkey = new HotkeySetting(ModifierKeys.Control | ModifierKeys.Shift, Key.F12),
            WindowPosition = new WindowPosition(-1200, 300),
        };

        _service.Save(settings);
        var loaded = _service.Load();

        Assert.Equal(25, loaded.Preset1Minutes);
        Assert.Equal("user:ベル.mp3", loaded.SoundId);
        Assert.Equal(0, loaded.VolumePercent);
        Assert.False(loaded.ToastEnabled);
        Assert.Equal(settings.CancelHotkey, loaded.CancelHotkey);
        Assert.Equal(new WindowPosition(-1200, 300), loaded.WindowPosition);
    }

    [Fact]
    public void 壊れたファイルはbakに退避してデフォルト値で起動する()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_service.FilePath, "{ これは JSON ではない");

        var settings = _service.Load();

        Assert.Equal(10, settings.Preset1Minutes);
        Assert.False(File.Exists(_service.FilePath));
        Assert.Equal("{ これは JSON ではない", File.ReadAllText(_service.BackupFilePath));
    }

    [Fact]
    public void 範囲外の値は範囲内に補正される()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_service.FilePath, """
            { "Preset1Minutes": 0, "Preset2Minutes": 999, "VolumePercent": 150, "AutoStopSeconds": 1,
              "OpacityPercent": 10, "ScalePercent": 1000, "WarningMinutes": -3 }
            """);

        var settings = _service.Load();

        Assert.Equal(AppSettings.PresetMinutesMin, settings.Preset1Minutes);
        Assert.Equal(AppSettings.PresetMinutesMax, settings.Preset2Minutes);
        Assert.Equal(AppSettings.VolumePercentMax, settings.VolumePercent);
        Assert.Equal(AppSettings.AutoStopSecondsMin, settings.AutoStopSeconds);
        Assert.Equal(AppSettings.OpacityPercentMin, settings.OpacityPercent);
        Assert.Equal(AppSettings.ScalePercentMax, settings.ScalePercent);
        Assert.Equal(AppSettings.WarningMinutesMin, settings.WarningMinutes);
    }

    [Fact]
    public void 欠けている項目やnullはデフォルト値になる()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_service.FilePath, """{ "Preset1Minutes": 15, "SoundId": null, "Preset2Hotkey": null }""");

        var settings = _service.Load();

        Assert.Equal(15, settings.Preset1Minutes);
        Assert.Equal(30, settings.Preset2Minutes);
        Assert.Equal(SoundCatalog.DefaultSoundId, settings.SoundId);
        Assert.Equal(new AppSettings().Preset2Hotkey, settings.Preset2Hotkey);
    }

    [Fact]
    public void ウィンドウ位置はモニターの識別情報ごと保存され_古い形式も読める()
    {
        var settings = new AppSettings { WindowPosition = new WindowPosition(-1500, 200, @"\\.\DISPLAY2", -1920, 1) };
        _service.Save(settings);
        Assert.Equal(new WindowPosition(-1500, 200, @"\\.\DISPLAY2", -1920, 1), _service.Load().WindowPosition);

        // ステップ8までの形式（座標のみ）
        File.WriteAllText(_service.FilePath, """{ "WindowPosition": { "X": 100, "Y": 200 } }""");
        Assert.Equal(new WindowPosition(100, 200), _service.Load().WindowPosition);
    }

    [Fact]
    public void 前回指定した時間は保存され_古いファイルや範囲外はデフォルトや範囲内になる()
    {
        _service.Save(new AppSettings { LastCustomSeconds = 5405 });
        Assert.Equal(5405, _service.Load().LastCustomSeconds);

        // 時間指定の機能を追加する前の settings.json（項目が無い）
        File.WriteAllText(_service.FilePath, """{ "Preset1Minutes": 15 }""");
        Assert.Equal(new AppSettings().LastCustomSeconds, _service.Load().LastCustomSeconds);

        File.WriteAllText(_service.FilePath, """{ "LastCustomSeconds": 0 }""");
        Assert.Equal(AppSettings.LastCustomSecondsMin, _service.Load().LastCustomSeconds);

        File.WriteAllText(_service.FilePath, """{ "LastCustomSeconds": 999999 }""");
        Assert.Equal(AppSettings.LastCustomSecondsMax, _service.Load().LastCustomSeconds);
    }

    [Fact]
    public void デフォルトに戻しても前回指定した時間は維持される()
    {
        var settings = new AppSettings { LastCustomSeconds = 90 };

        settings.ResetToDefaults();

        Assert.Equal(90, settings.LastCustomSeconds);
    }

    [Fact]
    public void 前回指定したアラームの時刻は保存され_古いファイルは未指定_範囲外は範囲内になる()
    {
        _service.Save(new AppSettings { LastAlarmMinutes = 7 * 60 + 30 });
        Assert.Equal(7 * 60 + 30, _service.Load().LastAlarmMinutes);

        _service.Save(new AppSettings { LastAlarmMinutes = 0 });
        Assert.Equal(0, _service.Load().LastAlarmMinutes);

        // アラームの機能を追加する前の settings.json（項目が無い）
        File.WriteAllText(_service.FilePath, """{ "Preset1Minutes": 15, "LastCustomSeconds": 90 }""");
        Assert.Null(_service.Load().LastAlarmMinutes);

        File.WriteAllText(_service.FilePath, """{ "LastAlarmMinutes": -5 }""");
        Assert.Equal(AppSettings.LastAlarmMinutesMin, _service.Load().LastAlarmMinutes);

        File.WriteAllText(_service.FilePath, """{ "LastAlarmMinutes": 1440 }""");
        Assert.Equal(AppSettings.LastAlarmMinutesMax, _service.Load().LastAlarmMinutes);
    }

    [Fact]
    public void デフォルトに戻しても前回指定したアラームの時刻は維持される()
    {
        var settings = new AppSettings { LastAlarmMinutes = 600 };

        settings.ResetToDefaults();

        Assert.Equal(600, settings.LastAlarmMinutes);
    }

    [Fact]
    public void デフォルトに戻してもウィンドウ位置は維持される()
    {
        var settings = new AppSettings { Preset1Minutes = 45, VolumePercent = 5, WindowPosition = new WindowPosition(10, 20) };

        settings.ResetToDefaults();

        Assert.Equal(10, settings.Preset1Minutes);
        Assert.Equal(80, settings.VolumePercent);
        Assert.Equal(new WindowPosition(10, 20), settings.WindowPosition);
    }
}
