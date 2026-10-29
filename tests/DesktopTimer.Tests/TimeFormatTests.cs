using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class TimeFormatTests
{
    [Theory]
    [InlineData(0, "00:00")]
    [InlineData(462, "07:42")]
    [InlineData(3599, "59:59")]
    [InlineData(3600, "1:00:00")]
    [InlineData(5400, "1:30:00")]
    [InlineData(10800, "3:00:00")]
    public void 秒数をmmssまたはhmmssで表示する(int seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void 経過時間は秒未満を切り捨てる()
    {
        Assert.Equal("02:18", TimeFormat.Format(TimeSpan.FromSeconds(138.9)));
    }

    [Theory]
    [InlineData(600_000, "10:00")]
    [InlineData(599_001, "10:00")]
    [InlineData(599_000, "09:59")]
    [InlineData(1, "00:01")]
    [InlineData(0, "00:00")]
    [InlineData(-500, "00:00")]
    public void 残り時間は秒未満を切り上げる(int milliseconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.FormatRemaining(TimeSpan.FromMilliseconds(milliseconds)));
    }

    [Fact]
    public void 残り時間と経過時間の秒の合計は設定時間と一致する()
    {
        var duration = TimeSpan.FromMinutes(10);
        var elapsed = TimeSpan.FromMilliseconds(138_400);

        Assert.Equal("02:18", TimeFormat.Format(elapsed));
        Assert.Equal("07:42", TimeFormat.FormatRemaining(duration - elapsed));
    }

    [Theory]
    [InlineData(600, "10分")]
    [InlineData(5400, "90分")]
    [InlineData(10, "10秒")]
    [InlineData(90, "90秒")]
    public void プリセットのラベル(int seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.PresetLabel(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(1500, "25分")]
    [InlineData(5400, "1時間30分")]
    [InlineData(90, "1分30秒")]
    [InlineData(45, "45秒")]
    [InlineData(7200, "2時間")]
    [InlineData(3605, "1時間5秒")]
    [InlineData(0, "0秒")]
    public void 通知用の時間の長さの表記(int seconds, string expected)
    {
        Assert.Equal(expected, TimeFormat.DurationLabel(TimeSpan.FromSeconds(seconds)));
    }

    [Theory]
    [InlineData(7, 30, "7:30")]
    [InlineData(0, 0, "0:00")]
    [InlineData(23, 5, "23:05")]
    [InlineData(12, 0, "12:00")]
    public void アラームの時刻の表記(int hour, int minute, string expected)
    {
        Assert.Equal(expected, TimeFormat.ClockLabel(new TimeOnly(hour, minute)));
    }
}
