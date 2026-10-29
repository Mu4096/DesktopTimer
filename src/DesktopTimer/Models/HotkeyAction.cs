namespace DesktopTimer.Models;

/// <summary>ホットキーで行う操作（仕様書 7章）。</summary>
public enum HotkeyAction
{
    StartPreset1,
    StartPreset2,

    /// <summary>キャンセル / TimeUp 解除。</summary>
    Cancel,
}

/// <summary>ホットキーの登録状態。</summary>
public enum HotkeyStatus
{
    /// <summary>登録済み（有効）。</summary>
    Registered,

    /// <summary>キーが割り当てられていない。</summary>
    NotAssigned,

    /// <summary>他のアプリと競合して登録できなかった（無効）。</summary>
    Conflict,

    /// <summary>このアプリの別の操作と同じキー（無効）。</summary>
    Duplicate,
}
