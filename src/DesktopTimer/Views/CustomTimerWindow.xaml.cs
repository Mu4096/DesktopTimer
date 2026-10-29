using DesktopTimer.ViewModels;

namespace DesktopTimer.Views;

/// <summary>
/// 「時間を指定して開始」の小窓。タイマーの右クリックメニューから開き、タイマーのすぐ上（入らなければ下）に出す。
/// </summary>
public partial class CustomTimerWindow : AnchoredDialogWindow
{
    /// <param name="anchor">近くに表示する基準のウィンドウ（タイマー本体）。</param>
    public CustomTimerWindow(CustomTimerViewModel viewModel, IntPtr anchor)
        : base(anchor)
    {
        DataContext = viewModel;
        InitializeComponent();

        viewModel.CloseRequested += (_, _) => Close();

        // すぐ打ち込めるよう、よく使う「分」の欄を選択状態にしておく
        Loaded += (_, _) => MinutesBox.Focus();
    }
}
