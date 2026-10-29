using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.ViewModels;

/// <summary>
/// 設定画面。設定値は <see cref="Settings"/> に直接バインドし、変更は即時反映・即時保存される（仕様書 10.2）。
/// </summary>
public sealed class SettingsViewModel : ObservableObject, IDisposable
{
    /// <summary>試聴用。タイムアップ音とは別に鳴らす。</summary>
    private readonly SoundService _preview = new();
    private readonly HotkeyService _hotkeys;
    private readonly UserSoundLibrary _userSounds;
    private SoundItemViewModel? _previewing;

    public SettingsViewModel(AppSettings settings, HotkeyService hotkeys, UserSoundLibrary userSounds)
    {
        Settings = settings;
        _hotkeys = hotkeys;
        _userSounds = userSounds;
        Sounds = new ObservableCollection<SoundItemViewModel>(
            SoundCatalog.GetAllSources(userSounds).Select(source => new SoundItemViewModel(source, TogglePreview)));
        Hotkeys =
        [
            new HotkeyItemViewModel(HotkeyAction.StartPreset1, "プリセット1を開始", settings, hotkeys),
            new HotkeyItemViewModel(HotkeyAction.StartPreset2, "プリセット2を開始", settings, hotkeys),
            new HotkeyItemViewModel(HotkeyAction.Cancel, "キャンセル / 解除", settings, hotkeys),
        ];

        Settings.PropertyChanged += OnSettingsChanged;
    }

    public AppSettings Settings { get; }

    /// <summary>選択できる音源の一覧。</summary>
    public ObservableCollection<SoundItemViewModel> Sounds { get; }

    /// <summary>ホットキーの一覧。</summary>
    public IReadOnlyList<HotkeyItemViewModel> Hotkeys { get; }

    /// <summary>
    /// 音声ファイルを追加し、追加した音源を選択する。
    /// </summary>
    /// <returns>失敗した場合はユーザー向けのエラーメッセージ。成功なら null。</returns>
    public string? AddSound(string path)
    {
        if (!UserSoundLibrary.IsSupported(path))
        {
            return "wav または mp3 ファイルを選んでください。";
        }

        if (!SoundService.CanOpen(path))
        {
            return "このファイルは再生できません。壊れているか、対応していない形式の可能性があります。";
        }

        SoundSource source;
        try
        {
            source = _userSounds.Add(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"ファイルをコピーできませんでした。\n{ex.Message}";
        }

        Sounds.Add(new SoundItemViewModel(source, TogglePreview));
        Settings.SoundId = source.Id;
        return null;
    }

    /// <summary>
    /// ユーザーが追加した音源を削除する。選択中だった場合はデフォルトの音源を選択し直す。
    /// </summary>
    /// <returns>失敗した場合はユーザー向けのエラーメッセージ。成功なら null。</returns>
    public string? DeleteSound(SoundItemViewModel item)
    {
        if (!item.IsDeletable)
        {
            return null;
        }

        // 試聴中だとファイルが開いたままで削除できないので止める
        if (_previewing == item)
        {
            StopPreview();
        }

        try
        {
            _userSounds.Delete(item.Source);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"削除できませんでした。タイムアップ音として再生中の可能性があります。\n{ex.Message}";
        }

        // 一覧から消す前に選択を移す（選択中の行を消すと、一覧の選択解除で SoundId が null になるため）
        if (Settings.SoundId == item.Id)
        {
            Settings.SoundId = SoundCatalog.DefaultSoundId;
        }

        Sounds.Remove(item);
        return null;
    }

    /// <summary>すべての設定をデフォルトに戻す。</summary>
    public void ResetToDefaults()
    {
        Settings.ResetToDefaults();
    }

    /// <summary>
    /// ホットキーの入力欄にフォーカスがある間は、登録済みのホットキーを一時解除する。
    /// そうしないと、割り当て済みのキーを押した時に入力欄へ届く前にタイマーが動いてしまう。
    /// </summary>
    public void BeginHotkeyEdit() => _hotkeys.Suspend();

    public void EndHotkeyEdit() => _hotkeys.Resume();

    public void Dispose()
    {
        Settings.PropertyChanged -= OnSettingsChanged;
        foreach (var hotkey in Hotkeys)
        {
            hotkey.Dispose();
        }

        _hotkeys.Resume();
        StopPreview();
        _preview.Dispose();
    }

    private void TogglePreview(SoundItemViewModel item)
    {
        var wasPreviewingThis = _previewing == item;
        StopPreview();
        if (wasPreviewingThis)
        {
            return;
        }

        _preview.PlayLoop(item.Source, Settings.VolumePercent);
        if (_preview.IsPlaying)
        {
            _previewing = item;
            item.IsPreviewing = true;
        }
    }

    private void StopPreview()
    {
        _preview.Stop();
        if (_previewing is not null)
        {
            _previewing.IsPreviewing = false;
            _previewing = null;
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 試聴中に音量スライダーを動かしたら、そのまま音量を変える
        if (e.PropertyName == nameof(AppSettings.VolumePercent))
        {
            _preview.SetVolume(Settings.VolumePercent);
        }
    }
}
