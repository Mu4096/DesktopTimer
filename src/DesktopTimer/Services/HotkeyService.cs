using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Interop;
using DesktopTimer.Models;
using static DesktopTimer.Interop.NativeMethods;

namespace DesktopTimer.Services;

/// <summary>
/// グローバルホットキー（仕様書 7章）。RegisterHotKey で登録し、WM_HOTKEY を受けて <see cref="Pressed"/> を発生させる。
/// 設定のホットキーが変わったら登録し直す。
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private static readonly HotkeyAction[] Actions = Enum.GetValues<HotkeyAction>();

    private readonly AppSettings _settings;
    private readonly HwndSource _source;
    private readonly Dictionary<HotkeyAction, HotkeyStatus> _statuses = new();
    private readonly HashSet<HotkeyAction> _registered = new();
    private bool _suspended;

    public HotkeyService(AppSettings settings)
    {
        _settings = settings;

        // メッセージ専用ウィンドウで受け取る（タイマーウィンドウの表示/非表示に左右されない）
        _source = new HwndSource(new HwndSourceParameters("DesktopTimer.Hotkeys")
        {
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0,
        });
        _source.AddHook(WndProc);

        _settings.PropertyChanged += OnSettingsChanged;
        RegisterAll();
    }

    /// <summary>ホットキーが押されたときに発生する。</summary>
    public event EventHandler<HotkeyAction>? Pressed;

    /// <summary>登録状態が変わったときに発生する。</summary>
    public event EventHandler? StatusChanged;

    public HotkeyStatus GetStatus(HotkeyAction action) => _statuses[action];

    /// <summary>
    /// ホットキーを一時的にすべて解除する（設定画面でキー入力を受け付けている間）。
    /// 解除中に設定が変わった場合も、登録を試して状態（競合の有無）だけは更新する。
    /// </summary>
    public void Suspend()
    {
        if (_suspended)
        {
            return;
        }

        _suspended = true;
        UnregisterAll();
    }

    /// <summary><see cref="Suspend"/> で解除したホットキーを登録し直す。</summary>
    public void Resume()
    {
        if (!_suspended)
        {
            return;
        }

        _suspended = false;
        RegisterAll();
    }

    public void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsChanged;
        UnregisterAll();
        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    private void RegisterAll()
    {
        UnregisterAll();

        var assigned = new List<HotkeySetting>();
        foreach (var action in Actions)
        {
            var hotkey = _settings.GetHotkey(action);
            HotkeyStatus status;
            if (hotkey.Key == Key.None)
            {
                status = HotkeyStatus.NotAssigned;
            }
            else if (assigned.Contains(hotkey))
            {
                status = HotkeyStatus.Duplicate;
            }
            else
            {
                assigned.Add(hotkey);
                var id = ToId(action);
                var vk = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);
                if (RegisterHotKey(_source.Handle, id, ToNativeModifiers(hotkey.Modifiers) | MOD_NOREPEAT, vk))
                {
                    status = HotkeyStatus.Registered;
                    if (_suspended)
                    {
                        // 一時解除中は競合の確認だけして、すぐに解除する
                        UnregisterHotKey(_source.Handle, id);
                    }
                    else
                    {
                        _registered.Add(action);
                    }
                }
                else
                {
                    status = HotkeyStatus.Conflict;
                }
            }

            _statuses[action] = status;
        }

        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UnregisterAll()
    {
        foreach (var action in _registered)
        {
            UnregisterHotKey(_source.Handle, ToId(action));
        }

        _registered.Clear();
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (AppSettings.IsHotkeyProperty(e.PropertyName))
        {
            RegisterAll();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var action = (HotkeyAction)(wParam.ToInt32() - 1);
            if (Enum.IsDefined(action))
            {
                Pressed?.Invoke(this, action);
                handled = true;
            }
        }

        return IntPtr.Zero;
    }

    /// <summary>RegisterHotKey の ID（0 は避けて 1 から）。</summary>
    private static int ToId(HotkeyAction action) => (int)action + 1;

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            result |= MOD_ALT;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            result |= MOD_CONTROL;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            result |= MOD_SHIFT;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            result |= MOD_WIN;
        }

        return result;
    }
}
