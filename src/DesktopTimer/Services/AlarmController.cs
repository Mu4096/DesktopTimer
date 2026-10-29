using System.Windows.Threading;
using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>
/// タイムアップ時の音を制御する（仕様書 5章）。
/// TimeUp に入ったらループ再生し、自動停止時間が経つか TimeUp を抜けたら止める。
/// </summary>
public sealed class AlarmController
{
    /// <summary>自動停止を判定する周期。</summary>
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(500);

    private readonly TimerService _timer;
    private readonly SoundService _sound;
    private readonly AppSettings _settings;
    private readonly UserSoundLibrary _userSounds;
    private readonly DispatcherTimer _autoStopTimer;

    public AlarmController(TimerService timer, SoundService sound, AppSettings settings, UserSoundLibrary userSounds)
    {
        _userSounds = userSounds;
        _timer = timer;
        _sound = sound;
        _settings = settings;

        _autoStopTimer = new DispatcherTimer { Interval = CheckInterval };
        _autoStopTimer.Tick += (_, _) => CheckAutoStop();

        _timer.StateChanged += (_, _) => OnStateChanged();
    }

    private void OnStateChanged()
    {
        if (_timer.State == TimerState.TimeUp)
        {
            var source = SoundCatalog.Resolve(_settings.SoundId, SoundCatalog.GetAllSources(_userSounds));
            _sound.PlayLoop(source, _settings.VolumePercent);
            _autoStopTimer.Start();
        }
        else
        {
            _autoStopTimer.Stop();
            _sound.Stop();
        }
    }

    private void CheckAutoStop()
    {
        // 経過はタイマーの単調増加時刻で判定する（スリープをまたいでも実時間で止まる）。
        // 音だけを止め、表示は TimeUp のまま維持する
        if (_timer.TimeSinceTimeUp >= TimeSpan.FromSeconds(_settings.AutoStopSeconds))
        {
            _autoStopTimer.Stop();
            _sound.Stop();
        }
    }
}
