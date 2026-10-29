using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using DesktopTimer.ViewModels;

namespace DesktopTimer.Views;

/// <summary>
/// 時・分・秒の入力欄（TextBox）の操作。Themes/Controls.xaml の TimeFieldBoxStyle から有効にする。
/// <list type="bullet">
/// <item>欄に入ったら（クリックでも）全選択し、そのまま打ち直せるようにする</item>
/// <item>↑↓キー・ホイールで値を増減する。増減する欄は Tag にバインド先のプロパティ名で指定し、
/// DataContext の <see cref="ITimeFieldStepper"/> に任せる</item>
/// </list>
/// </summary>
public static class TimeFieldBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(TimeFieldBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox box)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            box.GotKeyboardFocus += OnGotKeyboardFocus;
            box.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            box.PreviewKeyDown += OnPreviewKeyDown;
            box.PreviewMouseWheel += OnPreviewMouseWheel;
        }
        else
        {
            box.GotKeyboardFocus -= OnGotKeyboardFocus;
            box.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            box.PreviewKeyDown -= OnPreviewKeyDown;
            box.PreviewMouseWheel -= OnPreviewMouseWheel;
        }
    }

    private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        ((TextBox)sender).SelectAll();
    }

    /// <summary>クリックで欄に入った時も全選択する（既定ではクリック位置にカーソルが置かれて選択が外れるため）。</summary>
    private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var box = (TextBox)sender;
        if (!box.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            box.Focus();
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var delta = e.Key switch
        {
            Key.Up => 1,
            Key.Down => -1,
            _ => 0,
        };
        if (delta != 0)
        {
            e.Handled = true;
            Step((TextBox)sender, delta);
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        Step((TextBox)sender, Math.Sign(e.Delta));
    }

    private static void Step(TextBox box, int delta)
    {
        if (box.DataContext is ITimeFieldStepper stepper && box.Tag is string propertyName)
        {
            stepper.Step(propertyName, delta);
            box.SelectAll();
        }
    }
}
