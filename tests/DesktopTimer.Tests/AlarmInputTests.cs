using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class AlarmInputTests
{
    [Theory]
    [InlineData("7", "30", 7, 30)]
    [InlineData("0", "00", 0, 0)]
    [InlineData("23", "59", 23, 59)]
    [InlineData("", "", 0, 0)]
    [InlineData("１７", "０５", 17, 5)]
    public void 時と分から時刻を組み立てる_0時0分も指定できる(string hour, string minute, int expectedHour, int expectedMinute)
    {
        Assert.True(AlarmInput.TryCompose(hour, minute, out var time));
        Assert.Equal(new TimeOnly(expectedHour, expectedMinute), time);
    }

    [Theory]
    [InlineData("24", "00")]
    [InlineData("7", "60")]
    [InlineData("a", "00")]
    [InlineData("7", "-1")]
    public void 範囲外や数字以外の欄があれば組み立てられない(string hour, string minute)
    {
        Assert.False(AlarmInput.TryCompose(hour, minute, out _));
    }

    [Fact]
    public void 今日まだ来ていない時刻なら今日のその時刻まで()
    {
        var now = new DateTime(2026, 10, 14, 6, 17, 30);

        Assert.Equal(new TimeSpan(1, 12, 30), AlarmInput.GetDelay(now, new TimeOnly(7, 30)));
    }

    [Fact]
    public void 過ぎた時刻なら翌日のその時刻まで()
    {
        var now = new DateTime(2026, 10, 14, 8, 0, 0);

        Assert.Equal(new TimeSpan(23, 30, 0), AlarmInput.GetDelay(now, new TimeOnly(7, 30)));
    }

    [Fact]
    public void ちょうどその時刻なら翌日_秒の途中でも同じ分なら翌日()
    {
        Assert.Equal(TimeSpan.FromDays(1), AlarmInput.GetDelay(new DateTime(2026, 10, 14, 7, 30, 0), new TimeOnly(7, 30)));
        Assert.Equal(new TimeSpan(23, 59, 40),
            AlarmInput.GetDelay(new DateTime(2026, 10, 14, 7, 30, 20), new TimeOnly(7, 30)));
    }

    [Fact]
    public void 日付をまたぐ指定と月末()
    {
        Assert.Equal(TimeSpan.FromMinutes(30), AlarmInput.GetDelay(new DateTime(2026, 10, 31, 23, 30, 0), new TimeOnly(0, 0)));
    }

    [Theory]
    [InlineData(14, 24, 0, 15, 0, "今日の 15:00 に鳴ります（あと 36分）")]
    [InlineData(14, 23, 30, 15, 0, "今日の 15:00 に鳴ります（あと 37分）")]
    [InlineData(14, 59, 59, 15, 0, "今日の 15:00 に鳴ります（あと 1分）")]
    [InlineData(6, 0, 0, 7, 30, "今日の 7:30 に鳴ります（あと 1時間30分）")]
    [InlineData(15, 9, 0, 7, 30, "明日の 7:30 に鳴ります（あと 16時間21分）")]
    [InlineData(7, 30, 0, 7, 30, "明日の 7:30 に鳴ります（あと 24時間）")]
    [InlineData(23, 30, 0, 0, 0, "明日の 0:00 に鳴ります（あと 30分）")]
    public void 鳴る時刻の案内_残りは分単位に切り上げる(int nowH, int nowM, int nowS, int hour, int minute, string expected)
    {
        var now = new DateTime(2026, 10, 14, nowH, nowM, nowS);

        Assert.Equal(expected, AlarmInput.DescribeSchedule(now, new TimeOnly(hour, minute)));
    }

    [Theory]
    [InlineData(14, 23, 15)]
    [InlineData(14, 0, 15)]
    [InlineData(23, 10, 0)]
    public void 初期値は次の正時(int nowH, int nowM, int expectedHour)
    {
        var now = new DateTime(2026, 10, 14, nowH, nowM, 0);

        Assert.Equal(new TimeOnly(expectedHour, 0), AlarmInput.GetInitialTime(now));
    }
}
