using System.Globalization;
using System.Windows.Controls;

namespace DesktopTimer.Views;

/// <summary>入力が Min〜Max の整数かを検証する。範囲外の値は設定に反映されない。</summary>
public sealed class IntRangeRule : ValidationRule
{
    public int Min { get; set; }

    public int Max { get; set; }

    public override ValidationResult Validate(object? value, CultureInfo cultureInfo)
    {
        return int.TryParse(value as string, NumberStyles.Integer, cultureInfo, out var number) && number >= Min && number <= Max
            ? ValidationResult.ValidResult
            : new ValidationResult(false, $"{Min}〜{Max} の整数を入力してください");
    }
}
