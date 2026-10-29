using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class PlacementCalculatorTests
{
    private const double Width = 240;
    private const double Height = 110;

    // メイン：1920×1080（100%）、タスクバー 48px
    private static readonly MonitorInfo Primary = new(
        @"\\.\DISPLAY1", new ScreenRect(0, 0, 1920, 1080), new ScreenRect(0, 0, 1920, 1032), 1.0, IsPrimary: true);

    // サブ：メインの右、2560×1440（150%）
    private static readonly MonitorInfo Sub150 = new(
        @"\\.\DISPLAY2", new ScreenRect(1920, 0, 4480, 1440), new ScreenRect(1920, 0, 4480, 1440), 1.5, IsPrimary: false);

    [Fact]
    public void デフォルト位置はプライマリの作業領域の右下から16DIP内側()
    {
        var (x, y) = PlacementCalculator.GetDefaultPosition([Sub150, Primary], Width, Height);

        Assert.Equal(1920 - 240 - 16, x);
        Assert.Equal(1032 - 110 - 16, y);
    }

    [Fact]
    public void デフォルト位置の大きさと余白はプライマリのスケールで換算する()
    {
        var primary125 = Primary with { Scale = 1.25 };

        var (x, y) = PlacementCalculator.GetDefaultPosition([primary125], Width, Height);

        Assert.Equal(1920 - 300 - 20, x);
        Assert.Equal(1032 - 138 - 20, y); // 110×1.25=137.5 → 138、16×1.25=20
    }

    [Fact]
    public void 保存位置が作業領域に収まればその位置()
    {
        var saved = new WindowPosition(100, 200, @"\\.\DISPLAY1", 0, 0);

        Assert.Equal((100, 200), PlacementCalculator.ResolveSavedPosition(saved, [Primary, Sub150], Width, Height));
    }

    [Fact]
    public void サブモニターを外すとnullでデフォルト位置になる()
    {
        var saved = new WindowPosition(3000, 500, @"\\.\DISPLAY2", 1920, 0);

        Assert.Null(PlacementCalculator.ResolveSavedPosition(saved, [Primary], Width, Height));
    }

    [Fact]
    public void はみ出す位置はnull()
    {
        // 右端・タスクバーにかかる
        Assert.Null(PlacementCalculator.ResolveSavedPosition(new WindowPosition(1800, 100), [Primary], Width, Height));
        Assert.Null(PlacementCalculator.ResolveSavedPosition(new WindowPosition(100, 1000), [Primary], Width, Height));
    }

    [Fact]
    public void 収まるかの判定は置き先モニターのDPIで行う()
    {
        // 150% のモニターでは 360×165 になる。右端から 300px の位置には収まらない
        var saved = new WindowPosition(4480 - 300, 100, @"\\.\DISPLAY2", 1920, 0);

        Assert.Null(PlacementCalculator.ResolveSavedPosition(saved, [Primary, Sub150], Width, Height));

        var fits = saved with { X = 4480 - 360 };
        Assert.Equal((4480 - 360, 100), PlacementCalculator.ResolveSavedPosition(fits, [Primary, Sub150], Width, Height));
    }

    [Fact]
    public void モニターの配置が変わっても同じモニター上の同じ位置に追従する()
    {
        // 保存時はメインの右にあったサブが、メインの左に並べ替えられた
        var saved = new WindowPosition(2020, 300, @"\\.\DISPLAY2", 1920, 0);
        var movedSub = Sub150 with
        {
            Bounds = new ScreenRect(-2560, 0, 0, 1440),
            WorkArea = new ScreenRect(-2560, 0, 0, 1440),
        };

        Assert.Equal((-2460, 300), PlacementCalculator.ResolveSavedPosition(saved, [Primary, movedSub], Width, Height));
    }

    [Fact]
    public void 識別情報の無い古い保存位置は座標だけで判定する()
    {
        Assert.Equal((2000, 50), PlacementCalculator.ResolveSavedPosition(new WindowPosition(2000, 50), [Primary, Sub150], Width, Height));
    }

    [Fact]
    public void 小窓はタイマーの上に右端をそろえて置く()
    {
        // 右下のデフォルト位置にあるタイマー（小窓の大きさは計算を追いやすい値にしている）
        var anchor = new ScreenRect(1664, 906, 1904, 1016);

        Assert.Equal((1904 - 300, 906 - 8 - 200), PlacementCalculator.GetPopupPosition(anchor, Primary, 300, 200));
    }

    [Fact]
    public void 上に入らなければ小窓はタイマーの下に置く()
    {
        var anchor = new ScreenRect(500, 50, 740, 160);

        Assert.Equal((440, 160 + 8), PlacementCalculator.GetPopupPosition(anchor, Primary, 300, 200));
    }

    [Fact]
    public void 上下どちらにも入らなければ小窓を作業領域の中に収める()
    {
        // 作業領域の高さ 300 に、高さ 110 のタイマーが中ほどにある
        var low = Primary with { WorkArea = new ScreenRect(0, 0, 1920, 300) };
        var anchor = new ScreenRect(10, 100, 250, 210);

        // 左端からはみ出す分も右へ寄せる
        Assert.Equal((0, 0), PlacementCalculator.GetPopupPosition(anchor, low, 300, 200));
    }

    [Fact]
    public void 小窓の大きさと間隔はモニターのスケールで換算する()
    {
        var anchor = new ScreenRect(4000, 1000, 4360, 1165);

        // 300×200 DIP → 450×300、間隔 8 → 12
        Assert.Equal((4360 - 450, 1000 - 12 - 300), PlacementCalculator.GetPopupPosition(anchor, Sub150, 300, 200));
    }

    [Fact]
    public void いずれかの作業領域に完全に収まるかを判定する()
    {
        Assert.True(PlacementCalculator.FitsInAnyWorkArea(new ScreenRect(10, 10, 250, 120), [Primary]));
        // 2台にまたがるのは収まっていない扱い
        Assert.False(PlacementCalculator.FitsInAnyWorkArea(new ScreenRect(1800, 10, 2040, 120), [Primary, Sub150]));
        Assert.False(PlacementCalculator.FitsInAnyWorkArea(new ScreenRect(5000, 10, 5240, 120), [Primary, Sub150]));
    }
}
