using DesktopTimer.ViewModels;

namespace DesktopTimer.Views;

/// <summary>
/// 「時刻を指定してアラーム」の小窓。タイマーの右クリックメニューから開き、タイマーのすぐ上（入らなければ下）に出す。
/// </summary>
public partial class AlarmWindow : AnchoredDialogWindow
{
    /// <param name="anchor">近くに表示する基準のウィンドウ（タイマー本体）。</param>
    public AlarmWindow(AlarmViewModel viewModel, IntPtr anchor)
        : base(anchor)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += (_, _) => Close();

        // すぐ打ち込めるよう「時」の欄を選択状態にしておく
        Loaded += (_, _) => HourBox.Focus();
    }
}
