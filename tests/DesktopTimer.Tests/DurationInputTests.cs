using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class DurationInputTests
{
    [Theory]
    [InlineData("0", 59, 0)]
    [InlineData("25", 59, 25)]
    [InlineData("05", 59, 5)]
    [InlineData(" 7 ", 59, 7)]
    [InlineData("", 59, 0)]
    [InlineData(null, 59, 0)]
    [InlineData("１２", 59, 12)]
    [InlineData("23", 23, 23)]
    public void 欄の入力を整数として読み_空欄は0(string? text, int max, int expected)
    {
        Assert.True(DurationInput.TryParseField(text, max, out var value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("60", 59)]
    [InlineData("24", 23)]
    [InlineData("-1", 59)]
    [InlineData("+5", 59)]
    [InlineData("1.5", 59)]
    [InlineData("a", 59)]
    public void 範囲外や数字以外は読めない(string text, int max)
    {
        Assert.False(DurationInput.TryParseField(text, max, out _));
    }

    [Fact]
    public void 三つの欄から時間を組み立てる()
    {
        Assert.True(DurationInput.TryCompose("1", "30", "05", out var duration));
        Assert.Equal(new TimeSpan(1, 30, 5), duration);

        Assert.True(DurationInput.TryCompose("", "", "10", out duration));
        Assert.Equal(TimeSpan.FromSeconds(10), duration);
    }

    [Fact]
    public void 合計0や不正な欄があれば組み立てられない()
    {
        Assert.False(DurationInput.TryCompose("0", "0", "00", out _));
        Assert.False(DurationInput.TryCompose("", "", "", out _));
        Assert.False(DurationInput.TryCompose("0", "75", "0", out _));
        Assert.False(DurationInput.TryCompose("x", "5", "0", out _));
    }

    [Fact]
    public void 時間を三つの欄の値に分け_範囲外は収める()
    {
        Assert.Equal((1, 30, 5), DurationInput.Split(new TimeSpan(1, 30, 5)));
        Assert.Equal((0, 0, 1), DurationInput.Split(TimeSpan.Zero));
        Assert.Equal((23, 59, 59), DurationInput.Split(TimeSpan.FromHours(30)));
    }

    [Theory]
    [InlineData("25", 1, 59, 26)]
    [InlineData("25", -1, 59, 24)]
    [InlineData("59", 1, 59, 59)]
    [InlineData("0", -1, 59, 0)]
    [InlineData("", 1, 59, 1)]
    [InlineData("abc", 1, 59, 1)]
    public void 増減は範囲内に収め繰り上がらない(string text, int delta, int max, int expected)
    {
        Assert.Equal(expected, DurationInput.Step(text, delta, max));
    }
}
