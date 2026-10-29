using System.Globalization;
using System.Windows.Data;

namespace DesktopTimer.Views;

/// <summary>経過割合（0〜1）を残り割合（1〜0）に変える。進捗バーを「残り時間が減っていく」表示にするため。</summary>
public sealed class RemainingRatioConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double progress ? Math.Clamp(1 - progress, 0, 1) : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
