using System.IO;
using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>選択できる音源の一覧と、ID から音源への解決（仕様書 6章）。</summary>
public static class SoundCatalog
{
    public const string BuiltInBeepId = "builtin:beep";

    /// <summary>デフォルトのタイムアップ音（Alarm01.wav。存在しなければ解決時に内蔵ビープになる）。</summary>
    public const string DefaultSoundId = "system:Alarm01.wav";

    public static SoundSource BuiltInBeep { get; } = new(SoundKind.BuiltIn, BuiltInBeepId, "ビープ（内蔵）", null);

    /// <summary>Windows 標準のアラーム音のうち存在するものと、内蔵ビープ。</summary>
    public static IReadOnlyList<SoundSource> GetDefaultSources()
    {
        var mediaDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
        var sources = new List<SoundSource>();

        for (var i = 1; i <= 10; i++)
        {
            var fileName = $"Alarm{i:00}.wav";
            var path = Path.Combine(mediaDir, fileName);
            if (File.Exists(path))
            {
                sources.Add(new SoundSource(SoundKind.System, $"system:{fileName}", $"アラーム {i:00}", path));
            }
        }

        sources.Add(BuiltInBeep);
        return sources;
    }

    /// <summary>選択できるすべての音源（標準アラーム、内蔵ビープ、ユーザーが追加した音源の順）。</summary>
    public static IReadOnlyList<SoundSource> GetAllSources(UserSoundLibrary userSounds)
    {
        return [.. GetDefaultSources(), .. userSounds.GetSources()];
    }

    /// <summary>
    /// ID に対応する音源を返す。見つからない場合やファイルが存在しない場合は内蔵ビープで代替する。
    /// </summary>
    public static SoundSource Resolve(string soundId, IEnumerable<SoundSource> sources)
    {
        var source = sources.FirstOrDefault(s => s.Id == soundId);
        if (source is null || (source.FilePath is not null && !File.Exists(source.FilePath)))
        {
            return BuiltInBeep;
        }

        return source;
    }
}
