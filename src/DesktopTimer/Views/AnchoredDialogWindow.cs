using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DesktopTimer.Services;

namespace DesktopTimer.Views;

/// <summary>
/// タイマーの右クリックメニューから開く入力用の小窓（時間指定・アラーム）の共通部分。
/// タイマーのすぐ上（入らなければ下）に出し、タイトルバーを暗い配色にする。Esc・「キャンセル」で閉じる。
/// 大きさは XAML の Width / Height で固定する。
/// </summary>
public class AnchoredDialogWindow : Window
{
    private readonly IntPtr _anchor;

    /// <summary>XAML のデザイナー用。</summary>
    public AnchoredDialogWindow()
    {
    }

    /// <param name="anchor">近くに表示する基準のウィンドウ（タイマー本体）。</param>
    protected AnchoredDialogWindow(IntPtr anchor)
    {
        _anchor = anchor;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        TitleBarTheme.Apply(this,
            caption: (Color)FindResource("SurfaceBaseColor"),
            text: (Color)FindResource("TextColor"),
            border: (Color)FindResource("MikuTealDeepColor"));

        // 表示前にタイマーの近くへ移す。大きさは固定（Width / Height）なので、置き先モニターのスケールで換算して求める
        var handle = new WindowInteropHelper(this).Handle;
        if (MonitorHelper.GetWindowBounds(_anchor) is { } anchorBounds && MonitorHelper.GetMonitorOf(_anchor) is { } monitor)
        {
            var (x, y) = PlacementCalculator.GetPopupPosition(anchorBounds, monitor, Width, Height);
            MonitorHelper.MoveTo(handle, x, y);
        }
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close();
        }
    }

    /// <summary>「キャンセル」ボタン。</summary>
    protected void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
