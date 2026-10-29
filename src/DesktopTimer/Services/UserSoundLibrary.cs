using System.IO;
using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>
/// ユーザーが追加した音源（仕様書 6.2）。選択したファイルを sounds フォルダにコピーして管理するので、
/// 元ファイルを移動・削除しても影響を受けない。
/// </summary>
public sealed class UserSoundLibrary
{
    public const string IdPrefix = "user:";

    /// <summary>追加できる形式。</summary>
    public static IReadOnlyList<string> SupportedExtensions { get; } = [".wav", ".mp3"];

    public UserSoundLibrary(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    /// <summary>%APPDATA%\DesktopTimer\sounds</summary>
    public static string DefaultDirectoryPath { get; } = Path.Combine(SettingsService.DefaultDirectoryPath, "sounds");

    public string DirectoryPath { get; }

    public static bool IsSupported(string path) =>
        SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>追加済みの音源（ファイル名順）。</summary>
    public IReadOnlyList<SoundSource> GetSources()
    {
        if (!Directory.Exists(DirectoryPath))
        {
            return [];
        }

        return Directory.EnumerateFiles(DirectoryPath)
            .Where(IsSupported)
            .Order(StringComparer.CurrentCultureIgnoreCase)
            .Select(ToSource)
            .ToList();
    }

    /// <summary>
    /// ファイルを sounds フォルダにコピーして追加する。同名のファイルがあれば「名前 (2).mp3」のように番号を付ける。
    /// </summary>
    public SoundSource Add(string sourcePath)
    {
        if (!IsSupported(sourcePath))
        {
            throw new ArgumentException($"対応していない形式です: {sourcePath}", nameof(sourcePath));
        }

        Directory.CreateDirectory(DirectoryPath);

        var destinationPath = GetUniquePath(Path.GetFileName(sourcePath));
        File.Copy(sourcePath, destinationPath);
        return ToSource(destinationPath);
    }

    /// <summary>追加した音源を削除する。</summary>
    public void Delete(SoundSource source)
    {
        if (source.Kind != SoundKind.User || source.FilePath is null)
        {
            throw new ArgumentException("ユーザーが追加した音源だけ削除できます", nameof(source));
        }

        File.Delete(source.FilePath);
    }

    private string GetUniquePath(string fileName)
    {
        var path = Path.Combine(DirectoryPath, fileName);
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var i = 2; File.Exists(path); i++)
        {
            path = Path.Combine(DirectoryPath, $"{baseName} ({i}){extension}");
        }

        return path;
    }

    private static SoundSource ToSource(string path)
    {
        var fileName = Path.GetFileName(path);
        return new SoundSource(SoundKind.User, IdPrefix + fileName, fileName, path);
    }
}
