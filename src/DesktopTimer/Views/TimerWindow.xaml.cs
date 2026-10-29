using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DesktopTimer.Models;
using DesktopTimer.Services;
using DesktopTimer.ViewModels;
using Microsoft.Win32;

namespace DesktopTimer.Views;

/// <summary>デスクトップに常駐表示するタイマー本体のウィンドウ。</summary>
public partial class TimerWindow : Window
{
    /// <summary>基準サイズ（DIP、グリップ含む）。実際のサイズはこれにスケールを掛けたもの（仕様書 4.1, 4.4）。</summary>
    private const double BaseWidth = 240;
    private const double BaseHeight = 110;

    /// <summary>
    /// 長押しでゲージが現れるまでの時間（仕様書 4.6）。普通のクリックでゲージがちらつかないように少し待つ。
    /// </summary>
    private static readonly TimeSpan HoldGaugeDelay = TimeSpan.FromSeconds(0.3);

    /// <summary>ゲージが現れてから満ちるまでの時間。<see cref="HoldGaugeDelay"/> と合わせて長押し1秒。</summary>
    private static readonly TimeSpan HoldGaugeFill = TimeSpan.FromSeconds(0.7);

    /// <summary>ゲージが現れるときのフェードイン時間。</summary>
    private static readonly TimeSpan HoldGaugeFadeIn = TimeSpan.FromSeconds(0.08);

    private readonly AppSettings _settings;

    /// <summary>リサイズ開始時の、マウス位置からウィンドウ右下までの距離。</summary>
    private Vector _resizeGrabOffset;

    /// <summary>長押しの通し番号。中断した長押しのアニメーション完了を無視するのに使う。</summary>
    private int _holdId;

    private bool _isHolding;

    private TimerViewModel? _viewModel;

    public TimerWindow(AppSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        // 位置の復元（OnSourceInitialized）でサイズを使うので、表示前に反映しておく
        ApplyScale();
        ApplyOpacity();
        _settings.PropertyChanged += OnSettingsChanged;

        // 表示中にモニター構成が変わった時も、起動時と同じ判定をする（仕様書 8.3）。
        // 静的イベントなので、閉じる時に必ず解除する
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Closed += (_, _) => SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;

        DataContextChanged += OnDataContextChanged;
    }

    /// <summary>「設定...」や ⚙ ボタンが押されたときに発生する。</summary>
    public event EventHandler? SettingsRequested;

    /// <summary>「タイマー（時間指定）...」が選ばれたときに発生する。</summary>
    public event EventHandler? CustomTimerRequested;

    /// <summary>「アラーム（時間指定）...」が選ばれたときに発生する。</summary>
    public event EventHandler? AlarmRequested;

    /// <summary>「非表示にする」が選ばれたときに発生する。</summary>
    public event EventHandler? HideRequested;

    /// <summary>ウィンドウハンドル（全画面検出で、タイマーがどのモニターにあるかを調べるのに使う）。</summary>
    public IntPtr Handle => new WindowInteropHelper(this).Handle;

    /// <summary>プライマリモニターの右下隅へ移動し、保存位置をデフォルトに戻す。</summary>
    public void ResetPosition()
    {
        var (x, y) = PlacementCalculator.GetDefaultPosition(MonitorHelper.GetMonitors(), Width, Height);
        MonitorHelper.MoveTo(Handle, x, y);
        _settings.WindowPosition = null;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 表示前に保存位置へ移動する。保存位置が無い、またはどのモニターの作業領域にも収まらない
        // （モニターを外した等）場合はデフォルト位置にする
        var saved = _settings.WindowPosition;
        var position = saved is null
            ? null
            : PlacementCalculator.ResolveSavedPosition(saved, MonitorHelper.GetMonitors(), Width, Height);
        if (position is { } p)
        {
            MonitorHelper.MoveTo(Handle, p.X, p.Y);
        }
        else
        {
            ResetPosition();
        }
    }

    private void Grip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // DragMove はドラッグが終わるまで戻らない
        DragMove();

        // 移動完了時に、位置とモニターの識別情報を保存する
        if (MonitorHelper.GetWindowBounds(Handle) is { } bounds && MonitorHelper.GetMonitorOf(Handle) is { } monitor)
        {
            _settings.WindowPosition = new WindowPosition(
                bounds.Left, bounds.Top, monitor.DeviceName, monitor.Bounds.Left, monitor.Bounds.Top);
        }
    }

    /// <summary>
    /// モニター構成が変わった（接続・切断・解像度や配置の変更）。
    /// 今の位置がどのモニターの作業領域にも収まらなければ、デフォルト位置に移す。
    /// </summary>
    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        // SystemEvents は別スレッドから呼ばれることがあるので UI スレッドで処理する
        Dispatcher.BeginInvoke(() =>
        {
            if (MonitorHelper.GetWindowBounds(Handle) is { } bounds
                && !PlacementCalculator.FitsInAnyWorkArea(bounds, MonitorHelper.GetMonitors()))
            {
                ResetPosition();
            }
        });
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(AppSettings.ScalePercent):
                ApplyScale();
                break;
            case nameof(AppSettings.OpacityPercent):
                ApplyOpacity();
                break;
        }
    }

    /// <summary>スケール設定をウィンドウサイズに反映する。左上の位置は変えない。</summary>
    private void ApplyScale()
    {
        var scale = _settings.ScalePercent / 100.0;
        Width = BaseWidth * scale;
        Height = BaseHeight * scale;
    }

    private void ApplyOpacity()
    {
        Opacity = _settings.OpacityPercent / 100.0;
    }

    private void ResizeHandle_DragStarted(object sender, DragStartedEventArgs e)
    {
        var mouse = Mouse.GetPosition(this);
        _resizeGrabOffset = new Vector(ActualWidth - mouse.X, ActualHeight - mouse.Y);
    }

    /// <summary>
    /// 右下ハンドルのドラッグで拡大縮小する。縦横比は固定し、幅・高さのうち大きく動いた方に合わせる。
    /// 位置はウィンドウ左上からのマウス座標で求める（差分の積み上げだとスケールの丸めで少しずつずれるため）。
    /// </summary>
    private void ResizeHandle_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var mouse = Mouse.GetPosition(this);
        var width = mouse.X + _resizeGrabOffset.X;
        var height = mouse.Y + _resizeGrabOffset.Y;
        var scale = Math.Max(width / BaseWidth, height / BaseHeight);

        _settings.ScalePercent = Math.Clamp(
            (int)Math.Round(scale * 100), AppSettings.ScalePercentMin, AppSettings.ScalePercentMax);
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
        _viewModel = e.NewValue as TimerViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 長押し中に状態が変わった（残り0で TimeUp・ホットキーでキャンセル等）ら長押しを中断する
        if (e.PropertyName == nameof(TimerViewModel.State))
        {
            CancelHold();
        }
    }

    /// <summary>
    /// Running 中の本体の左ボタン押下で長押しを始める。少し押し続けると中央にゲージが現れ、一周したらキャンセル（停止）する。
    /// Preview で受けるのは、本体の MouseBinding（DismissCommand）より先に確実に受け取るため。
    /// Running 以外（TimeUp / Stopped の解除、Idle のプリセットボタン）では何もせず、従来どおりに処理させる。
    /// </summary>
    private void Body_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.State != TimerState.Running || _isHolding)
        {
            return;
        }

        // 離した・外へ出たことを確実に知るため、長押しの間はマウスをキャプチャする
        if (!BodyGrid.CaptureMouse())
        {
            return;
        }
        e.Handled = true;
        _isHolding = true;

        var id = ++_holdId;
        // 少し待ってからゲージをフェードインし、満ちたら実行する（待っている間は何も表示しない）
        var fadeIn = new DoubleAnimation(0, 1, new Duration(HoldGaugeFadeIn)) { BeginTime = HoldGaugeDelay };
        var animation = new DoubleAnimation(0, 1, new Duration(HoldGaugeFill)) { BeginTime = HoldGaugeDelay };
        animation.Completed += (_, _) =>
        {
            if (id == _holdId)
            {
                CompleteHold();
            }
        };
        HoldOverlay.Visibility = Visibility.Visible;
        HoldOverlay.BeginAnimation(OpacityProperty, fadeIn);
        HoldGauge.BeginAnimation(ProgressArc.ProgressProperty, animation);
    }

    private void Body_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isHolding)
        {
            e.Handled = true;
        }
        CancelHold();
    }

    /// <summary>長押し中にポインターが本体の外へ出たら中断する。</summary>
    private void Body_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isHolding && !new Rect(BodyGrid.RenderSize).Contains(e.GetPosition(BodyGrid)))
        {
            CancelHold();
        }
    }

    /// <summary>右クリックメニューが開いた・ウィンドウが切り替わった等でキャプチャを失ったら中断する。</summary>
    private void Body_LostMouseCapture(object sender, MouseEventArgs e)
    {
        CancelHold();
    }

    /// <summary>長押しを中断し、ゲージを隠す。長押し中でなければ何もしない。</summary>
    private void CancelHold()
    {
        if (!_isHolding)
        {
            return;
        }
        _isHolding = false;
        _holdId++;
        HoldGauge.BeginAnimation(ProgressArc.ProgressProperty, null);
        HoldOverlay.BeginAnimation(OpacityProperty, null);
        HoldOverlay.Visibility = Visibility.Collapsed;
        if (BodyGrid.IsMouseCaptured)
        {
            BodyGrid.ReleaseMouseCapture();
        }
    }

    /// <summary>ゲージが一周した。右クリックメニューのキャンセル（停止）と同じ操作をする。</summary>
    private void CompleteHold()
    {
        CancelHold();
        if (_viewModel is { State: TimerState.Running } vm && vm.CancelCommand.CanExecute(null))
        {
            vm.CancelCommand.Execute(null);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CustomTimerMenuItem_Click(object sender, RoutedEventArgs e)
    {
        CustomTimerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void AlarmMenuItem_Click(object sender, RoutedEventArgs e)
    {
        AlarmRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ResetPositionMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ResetPosition();
    }

    private void HideMenuItem_Click(object sender, RoutedEventArgs e)
    {
        HideRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}
