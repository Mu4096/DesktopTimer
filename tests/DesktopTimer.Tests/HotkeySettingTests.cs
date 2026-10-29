using System.Windows.Input;
using DesktopTimer.Models;

namespace DesktopTimer.Tests;

public class HotkeySettingTests
{
    [Fact]
    public void 修飾キーはCtrl_Alt_Shift_Winの順で表示する()
    {
        var hotkey = new HotkeySetting(ModifierKeys.Windows | ModifierKeys.Shift | ModifierKeys.Alt | ModifierKeys.Control, Key.A);
        Assert.Equal("Ctrl + Alt + Shift + Win + A", hotkey.ToDisplayString());
    }

    [Theory]
    [InlineData(Key.D1, "Ctrl + Alt + 1")]
    [InlineData(Key.D0, "Ctrl + Alt + 0")]
    [InlineData(Key.NumPad5, "Ctrl + Alt + Num 5")]
    [InlineData(Key.F12, "Ctrl + Alt + F12")]
    public void キー名を読みやすく表示する(Key key, string expected)
    {
        Assert.Equal(expected, new HotkeySetting(ModifierKeys.Control | ModifierKeys.Alt, key).ToDisplayString());
    }

    [Fact]
    public void 割り当てなしはなしと表示する()
    {
        Assert.Equal("なし", HotkeySetting.None.ToDisplayString());
    }
}
