using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DesktopTimer.Interop;

namespace DesktopTimer.Views;

/// <summary>
/// 標準のタイトルバーをアプリの配色に合わせる。
/// Windows 11 では背景・文字・枠の色を指定でき、Windows 10 では暗い配色にするだけになる。
/// </summary>
public static class TitleBarTheme
{
    /// <summary>ウィンドウハンドルができた後（SourceInitialized 以降）に呼ぶ。</summary>
    public static void Apply(Window window, Color caption, Color text, Color border)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var darkMode = 1;
        NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
        SetColor(hwnd, NativeMethods.DWMWA_CAPTION_COLOR, caption);
        SetColor(hwnd, NativeMethods.DWMWA_TEXT_COLOR, text);
        SetColor(hwnd, NativeMethods.DWMWA_BORDER_COLOR, border);
    }

    private static void SetColor(IntPtr hwnd, int attribute, Color color)
    {
        // COLORREF は 0x00BBGGRR
        var colorRef = color.R | (color.G << 8) | (color.B << 16);
        NativeMethods.DwmSetWindowAttribute(hwnd, attribute, ref colorRef, sizeof(int));
    }
}
