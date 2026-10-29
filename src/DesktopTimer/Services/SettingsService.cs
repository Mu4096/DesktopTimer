using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Threading;
using DesktopTimer.Models;

namespace DesktopTimer.Services;

/// <summary>設定ファイル（settings.json）の読み書き（仕様書 10.3）。</summary>
public sealed class SettingsService
{
    /// <summary>変更が続く間（スライダー操作など）は保存を待ち、落ち着いてから1回だけ書く。</summary>
    private static readonly TimeSpan AutoSaveDelay = TimeSpan.FromMilliseconds(500);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        // ユーザー音源のファイル名などの日本語をエスケープせずに書く
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private DispatcherTimer? _autoSaveTimer;
    private AppSettings? _autoSaveTarget;

    public SettingsService(string directoryPath)
    {
        DirectoryPath = directoryPath;
    }

    /// <summary>%APPDATA%\DesktopTimer</summary>
    public static string DefaultDirectoryPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DesktopTimer");

    public string DirectoryPath { get; }

    public string FilePath => Path.Combine(DirectoryPath, "settings.json");

    public string BackupFilePath => FilePath + ".bak";

    /// <summary>
    /// 設定を読み込む。ファイルが無ければデフォルト値。
    /// 壊れている場合はデフォルト値を返し、壊れたファイルを settings.json.bak に退避する。
    /// </summary>
    public AppSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(FilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                ?? throw new JsonException("設定ファイルの中身が null です");
            settings.Normalize();
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            Debug.WriteLine($"設定ファイルが壊れているためデフォルト値で起動します: {ex.Message}");
            File.Move(FilePath, BackupFilePath, overwrite: true);
            return new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 読めないだけで壊れているとは限らないので退避はしない
            Debug.WriteLine($"設定ファイルを読めないためデフォルト値で起動します: {ex.Message}");
            return new AppSettings();
        }
    }

    /// <summary>設定を保存する。書き込み途中で落ちても壊れないよう、一時ファイルに書いてから置き換える。</summary>
    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(DirectoryPath);

        var tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(tempPath, FilePath, overwrite: true);
    }

    /// <summary>設定が変わるたびに自動保存する（UI スレッドから呼ぶ）。</summary>
    public void EnableAutoSave(AppSettings settings)
    {
        _autoSaveTarget = settings;
        _autoSaveTimer = new DispatcherTimer { Interval = AutoSaveDelay };
        _autoSaveTimer.Tick += (_, _) => Flush();
        settings.PropertyChanged += OnSettingsChanged;
    }

    /// <summary>保存待ちの変更があればすぐに保存する。終了時に呼ぶ。</summary>
    public void Flush()
    {
        if (_autoSaveTimer is not { IsEnabled: true } || _autoSaveTarget is null)
        {
            return;
        }

        _autoSaveTimer.Stop();
        try
        {
            Save(_autoSaveTarget);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 保存できなくてもタイマーとしては動き続ける
            Debug.WriteLine($"設定を保存できません: {ex.Message}");
        }
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 最後の変更から一定時間後に保存する
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }
}
