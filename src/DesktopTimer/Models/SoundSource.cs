namespace DesktopTimer.Models;

/// <summary>音源の種類。</summary>
public enum SoundKind
{
    /// <summary>アプリに埋め込んだビープ音。</summary>
    BuiltIn,

    /// <summary>Windows 標準のアラーム音（C:\Windows\Media\AlarmXX.wav）。</summary>
    System,

    /// <summary>ユーザーが追加した音源（ステップ6）。</summary>
    User,
}

/// <summary>タイムアップ音の音源。</summary>
/// <param name="Id">設定に保存する識別子。</param>
/// <param name="DisplayName">一覧に表示する名前。</param>
/// <param name="FilePath">音声ファイルのパス。内蔵ビープでは null。</param>
public sealed record SoundSource(SoundKind Kind, string Id, string DisplayName, string? FilePath);
