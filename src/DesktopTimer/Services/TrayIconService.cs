using System.Drawing;
using System.Windows.Forms;

namespace DesktopTimer.Services;

/// <summary>
/// タスクトレイのアイコンとメニュー（仕様書 9章）、タイムアップ時のバルーン通知（仕様書 5章）。
/// 各操作はイベントで通知し、実際の処理は App 側で行う。
/// </summary>
public sealed class TrayIconService : IDisposable
{
    private const string IconResourceName = "DesktopTimer.Assets.app.ico";

    private readonly NotifyIcon _notifyIcon;
    private readonly Icon _icon;
    private readonly ToolStripMenuItem _toggleVisibilityItem;
    private readonly ToolStripMenuItem _cancelItem;
    private readonly Func<bool> _isTimerVisible;
    private readonly Func<bool> _canCancel;
    private readonly Func<string> _cancelLabel;

    /// <param name="isTimerVisible">タイマーウィンドウが表示中か（メニューの文言の切り替えに使う）。</param>
    /// <param name="canCancel">キャンセルできる状態か（Running / TimeUp / Stopped）。</param>
    /// <param name="cancelLabel">キャンセル項目の文言（カウントアップの計測中は「停止」）。</param>
    public TrayIconService(Func<bool> isTimerVisible, Func<bool> canCancel, Func<string> cancelLabel)
    {
        _isTimerVisible = isTimerVisible;
        _canCancel = canCancel;
        _cancelLabel = cancelLabel;

        using (var stream = typeof(TrayIconService).Assembly.GetManifestResourceStream(IconResourceName)
            ?? throw new InvalidOperationException($"埋め込みリソースがありません: {IconResourceName}"))
        {
            // トレイに表示する大きさ（DPI に応じた小アイコンのサイズ）を選ぶ
            _icon = new Icon(stream, SystemInformation.SmallIconSize);
        }

        _toggleVisibilityItem = new ToolStripMenuItem("非表示にする", null, (_, _) => Raise(ToggleVisibilityRequested));
        _cancelItem = new ToolStripMenuItem("キャンセル", null, (_, _) => Raise(CancelRequested));

        var menu = new ContextMenuStrip();
        menu.Items.Add(_toggleVisibilityItem);
        menu.Items.Add(_cancelItem);
        menu.Items.Add("設定...", null, (_, _) => Raise(SettingsRequested));
        menu.Items.Add("位置をリセット", null, (_, _) => Raise(ResetPositionRequested));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了", null, (_, _) => Raise(ExitRequested));
        menu.Opening += (_, _) => UpdateMenu();
        Views.TrayMenuRenderer.Apply(menu);

        _notifyIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "DesktopTimer",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => Raise(ToggleVisibilityRequested);
        _notifyIcon.BalloonTipClicked += (_, _) => Raise(NotificationClicked);
    }

    /// <summary>表示 / 非表示の切り替え（メニュー、またはアイコンのダブルクリック）。</summary>
    public event EventHandler? ToggleVisibilityRequested;

    public event EventHandler? CancelRequested;

    public event EventHandler? SettingsRequested;

    public event EventHandler? ResetPositionRequested;

    public event EventHandler? ExitRequested;

    /// <summary>通知（バルーン）がクリックされた。</summary>
    public event EventHandler? NotificationClicked;

    /// <summary>通知を表示する（Windows 10 / 11 ではトースト通知として表示される）。</summary>
    public void ShowNotification(string title, string text)
    {
        _notifyIcon.ShowBalloonTip(5000, title, text, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        // 終了時にアイコンを消しておかないと、マウスを乗せるまでトレイに残る
        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icon.Dispose();
    }

    private void UpdateMenu()
    {
        _toggleVisibilityItem.Text = _isTimerVisible() ? "非表示にする" : "表示する";
        _cancelItem.Text = _cancelLabel();
        _cancelItem.Enabled = _canCancel();
    }

    private void Raise(EventHandler? handler) => handler?.Invoke(this, EventArgs.Empty);
}
