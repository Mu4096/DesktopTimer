using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopTimer.Models;
using DesktopTimer.ViewModels;

namespace DesktopTimer.Views;

/// <summary>設定画面。変更は即時反映・即時保存するので OK / キャンセルボタンは無い。</summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        // 開いた時に、選択中の音源（一覧の下のほうにあることもある）が見えるようにする。
        // Loaded の時点では一覧の選択と行の生成がまだ終わっていないので、描画後に行う
        Loaded += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, () =>
        {
            if (SoundList.SelectedItem is not null)
            {
                SoundList.ScrollIntoView(SoundList.SelectedItem);
            }
        });
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // タイトルバーを画面の下地と同じ色にし、枠はティールにする
        TitleBarTheme.Apply(this,
            caption: (Color)FindResource("SurfaceBaseColor"),
            text: (Color)FindResource("TextColor"),
            border: (Color)FindResource("MikuTealDeepColor"));
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this, "すべての設定をデフォルトに戻しますか？", "DesktopTimer",
            MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        if (result == MessageBoxResult.OK)
        {
            ViewModel.ResetToDefaults();
        }
    }

    private void AddSoundButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "タイムアップ音を追加",
            Filter = "音声ファイル (*.wav;*.mp3)|*.wav;*.mp3",
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var error = ViewModel.AddSound(dialog.FileName);
        if (error is not null)
        {
            MessageBox.Show(this, error, "DesktopTimer", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 追加した音源（選択済み）が見えるようにスクロールする
        SoundList.ScrollIntoView(SoundList.SelectedItem);
    }

    private void DeleteSoundButton_Click(object sender, RoutedEventArgs e)
    {
        var item = (SoundItemViewModel)((FrameworkElement)sender).DataContext;
        var result = MessageBox.Show(this, $"「{item.DisplayName}」を削除しますか？", "DesktopTimer",
            MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel);
        if (result != MessageBoxResult.OK)
        {
            return;
        }

        var error = ViewModel.DeleteSound(item);
        if (error is not null)
        {
            MessageBox.Show(this, error, "DesktopTimer", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    /// <summary>入力欄にフォーカスが来たらキー入力の受付を始める。</summary>
    private void HotkeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        GetItem(sender).IsEditing = true;
        ViewModel.BeginHotkeyEdit();
    }

    /// <summary>フォーカスが外れたら受付を終える（登録・取り消し・他の欄のクリックのいずれでも）。</summary>
    private void HotkeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        GetItem(sender).IsEditing = false;
        ViewModel.EndHotkeyEdit();
    }

    /// <summary>押されたキーの組み合わせをホットキーとして登録する。</summary>
    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var item = GetItem(sender);
        var modifiers = Keyboard.Modifiers;

        // Alt を含むと Key.System、IME 処理中は Key.ImeProcessed になるので実際のキーを取り出す
        var key = e.Key switch
        {
            Key.System => e.SystemKey,
            Key.ImeProcessed => e.ImeProcessedKey,
            _ => e.Key,
        };

        // 修飾キーなしの Tab はフォーカス移動に使う
        if (key == Key.Tab && modifiers == ModifierKeys.None)
        {
            return;
        }

        e.Handled = true;

        if (modifiers == ModifierKeys.None && key == Key.Escape)
        {
            // 取り消し：何も変えずに受付を終える
            FinishHotkeyInput((TextBox)sender);
            return;
        }

        if (modifiers == ModifierKeys.None && key is Key.Delete or Key.Back)
        {
            item.SetHotkey(HotkeySetting.None);
            FinishHotkeyInput((TextBox)sender);
            return;
        }

        if (IsModifierKey(key))
        {
            // 修飾キーだけの間は、続くキーを待つ
            return;
        }

        // 普段の入力を妨げないよう、Ctrl / Alt / Win のいずれかとの組み合わせか、F キー単体に限る
        var hasRequiredModifier = (modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0;
        var isFunctionKey = key is >= Key.F1 and <= Key.F24;
        if (!hasRequiredModifier && !isFunctionKey)
        {
            return;
        }

        item.SetHotkey(new HotkeySetting(modifiers, key));

        // 登録したら受付を終え、新しいキーを表示する
        FinishHotkeyInput((TextBox)sender);
    }

    /// <summary>
    /// 入力欄からフォーカスを外して受付を終える。
    /// 論理フォーカスも外さないと、ウィンドウを切り替えて戻った時に入力欄へフォーカスが戻り、受付が再開してしまう。
    /// </summary>
    private static void FinishHotkeyInput(TextBox box)
    {
        FocusManager.SetFocusedElement(FocusManager.GetFocusScope(box), null);
        Keyboard.ClearFocus();
    }

    private static HotkeyItemViewModel GetItem(object sender) => (HotkeyItemViewModel)((TextBox)sender).DataContext;

    private static bool IsModifierKey(Key key) => key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin;
}
