using System.ComponentModel;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.ViewModels;

/// <summary>設定画面のホットキー1行（操作名・現在のキー・登録失敗時の警告）。</summary>
public sealed class HotkeyItemViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly HotkeyService _hotkeys;
    private bool _isEditing;

    public HotkeyItemViewModel(HotkeyAction action, string label, AppSettings settings, HotkeyService hotkeys)
    {
        Action = action;
        Label = label;
        _settings = settings;
        _hotkeys = hotkeys;

        _settings.PropertyChanged += OnSettingsChanged;
        _hotkeys.StatusChanged += OnStatusChanged;
    }

    public HotkeyAction Action { get; }

    public string Label { get; }

    /// <summary>キー入力を受け付けている最中かどうか（入力欄にフォーカスがある間）。</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (SetProperty(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(DisplayText));
            }
        }
    }

    /// <summary>現在のキー（例：Ctrl + Alt + 1）。受付中は入力を促す文言。</summary>
    public string DisplayText => IsEditing
        ? "キーを押してください…（Esc で取り消し）"
        : _settings.GetHotkey(Action).ToDisplayString();

    /// <summary>登録できていない場合の警告。問題なければ null。</summary>
    public string? WarningText => _hotkeys.GetStatus(Action) switch
    {
        HotkeyStatus.Conflict => "他のアプリと競合しているため登録できません。このホットキーは無効です。",
        HotkeyStatus.Duplicate => "他の操作と同じキーのため無効です。",
        _ => null,
    };

    public bool HasWarning => WarningText is not null;

    public void SetHotkey(HotkeySetting hotkey) => _settings.SetHotkey(Action, hotkey);

    public void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsChanged;
        _hotkeys.StatusChanged -= OnStatusChanged;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (AppSettings.IsHotkeyProperty(e.PropertyName))
        {
            OnPropertyChanged(nameof(DisplayText));
        }
    }

    private void OnStatusChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(WarningText));
        OnPropertyChanged(nameof(HasWarning));
    }
}
