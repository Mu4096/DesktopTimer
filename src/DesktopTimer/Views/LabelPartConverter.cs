using System.Globalization;
using System.Windows.Data;

namespace DesktopTimer.Views;

/// <summary>
/// 「10分」のようなラベルを数字と単位に分けて取り出す（数字を大きく、単位を小さく表示するため）。
/// ConverterParameter に "Number" を指定すると先頭の数字部分、"Unit" を指定すると残りを返す。
/// </summary>
public sealed class LabelPartConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var text = value as string ?? "";
        var digits = 0;
        while (digits < text.Length && char.IsAsciiDigit(text[digits]))
        {
            digits++;
        }

        return parameter as string == "Unit" ? text[digits..] : text[..digits];
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
