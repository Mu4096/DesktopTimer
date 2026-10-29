using System.IO;
using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public sealed class UserSoundLibraryTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "DesktopTimerTests", Guid.NewGuid().ToString("N"));
    private readonly UserSoundLibrary _library;

    public UserSoundLibraryTests()
    {
        _library = new UserSoundLibrary(Path.Combine(_root, "sounds"));
        Directory.CreateDirectory(Path.Combine(_root, "src"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private string CreateSourceFile(string name, string content = "dummy")
    {
        var path = Path.Combine(_root, "src", name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void フォルダが無ければ空()
    {
        Assert.Empty(_library.GetSources());
    }

    [Fact]
    public void 追加するとsoundsフォルダにコピーされ元ファイルは残る()
    {
        var original = CreateSourceFile("ベル.mp3");

        var source = _library.Add(original);

        Assert.Equal(SoundKind.User, source.Kind);
        Assert.Equal("user:ベル.mp3", source.Id);
        Assert.Equal("ベル.mp3", source.DisplayName);
        Assert.Equal(Path.Combine(_library.DirectoryPath, "ベル.mp3"), source.FilePath);
        Assert.True(File.Exists(source.FilePath));
        Assert.True(File.Exists(original));

        // 元ファイルを消しても追加済みの音源は残る
        File.Delete(original);
        Assert.Equal(["user:ベル.mp3"], _library.GetSources().Select(s => s.Id));
    }

    [Fact]
    public void 同名のファイルは番号を付けて追加する()
    {
        var first = _library.Add(CreateSourceFile("alarm.wav", "1"));
        File.Delete(Path.Combine(_root, "src", "alarm.wav"));
        var second = _library.Add(CreateSourceFile("alarm.wav", "2"));

        Assert.Equal("alarm.wav", first.DisplayName);
        Assert.Equal("alarm (2).wav", second.DisplayName);
        Assert.Equal("2", File.ReadAllText(second.FilePath!));
    }

    [Fact]
    public void 対応形式以外は一覧に出さず追加もできない()
    {
        Directory.CreateDirectory(_library.DirectoryPath);
        File.WriteAllText(Path.Combine(_library.DirectoryPath, "memo.txt"), "x");
        File.WriteAllText(Path.Combine(_library.DirectoryPath, "chime.WAV"), "x");

        Assert.Equal(["user:chime.WAV"], _library.GetSources().Select(s => s.Id));
        Assert.Throws<ArgumentException>(() => _library.Add(CreateSourceFile("song.ogg")));
    }

    [Fact]
    public void 削除するとファイルが消える()
    {
        var source = _library.Add(CreateSourceFile("bell.wav"));

        _library.Delete(source);

        Assert.False(File.Exists(source.FilePath));
        Assert.Empty(_library.GetSources());
    }

    [Fact]
    public void 標準の音源は削除できない()
    {
        Assert.Throws<ArgumentException>(() => _library.Delete(SoundCatalog.BuiltInBeep));
    }
}
