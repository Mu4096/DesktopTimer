using System.Diagnostics;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace DesktopTimer.Services;

/// <summary>
/// Windows 起動時の自動起動（仕様書 9章）。HKCU\Software\Microsoft\Windows\CurrentVersion\Run に登録する。
/// </summary>
public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DesktopTimer";

    /// <summary>
    /// 自動起動の登録 / 解除を行う。有効なら現在の exe のパスで登録し直す（exe を移動した場合も追従する）。
    /// </summary>
    public static void Apply(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
            {
                key.SetValue(ValueName, $"\"{GetExecutablePath()}\"");
            }
            else
            {
                key.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            // 登録できなくてもタイマーとしては動き続ける
            Debug.WriteLine($"自動起動の設定に失敗しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 起動する exe のパス。`dotnet DesktopTimer.dll` で起動された場合は dotnet.exe になるので、隣の DesktopTimer.exe を使う。
    /// </summary>
    private static string GetExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (processPath is not null
            && Path.GetFileName(processPath).Equals("DesktopTimer.exe", StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        return Path.Combine(AppContext.BaseDirectory, "DesktopTimer.exe");
    }
}
