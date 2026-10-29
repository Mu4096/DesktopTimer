using DesktopTimer.Models;
using DesktopTimer.Services;

namespace DesktopTimer.Tests;

public class TimerServiceTests
{
    /// <summary>手動で進める時刻。</summary>
    private sealed class FakeTimeSource : ITimeSource
    {
        public TimeSpan Now { get; set; } = TimeSpan.FromHours(1);

        public void Advance(TimeSpan delta) => Now += delta;
    }

    private readonly FakeTimeSource _clock = new();
    private readonly TimerService _timer;

    public TimerServiceTests()
    {
        _timer = new TimerService(_clock);
    }

    [Fact]
    public void 初期状態はIdle()
    {
        Assert.Equal(TimerState.Idle, _timer.State);
        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
        Assert.Equal(0, _timer.Progress);
    }

    [Fact]
    public void 開始するとRunningになり残り時間は設定時間()
    {
        Assert.True(_timer.Start(TimeSpan.FromMinutes(10)));

        Assert.Equal(TimerState.Running, _timer.State);
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Remaining);
        Assert.Equal(TimeSpan.Zero, _timer.Elapsed);
    }

    [Fact]
    public void 残り時間は開始時刻との差分で計算される()
    {
        _timer.Start(TimeSpan.FromMinutes(10));
        _clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(TimeSpan.FromMinutes(3), _timer.Elapsed);
        Assert.Equal(TimeSpan.FromMinutes(7), _timer.Remaining);
        Assert.Equal(0.3, _timer.Progress, 6);
    }

    [Fact]
    public void 残り0になるとUpdateでTimeUpに遷移する()
    {
        _timer.Start(TimeSpan.FromMinutes(10));
        var changes = 0;
        _timer.StateChanged += (_, _) => changes++;

        _clock.Advance(TimeSpan.FromMinutes(10) - TimeSpan.FromMilliseconds(1));
        _timer.Update();
        Assert.Equal(TimerState.Running, _timer.State);

        _clock.Advance(TimeSpan.FromMilliseconds(1));
        _timer.Update();
        Assert.Equal(TimerState.TimeUp, _timer.State);
        Assert.Equal(1, changes);
        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
        Assert.Equal(1, _timer.Progress);
    }

    [Fact]
    public void Running中に開始しても無視される()
    {
        _timer.Start(TimeSpan.FromMinutes(10));
        _clock.Advance(TimeSpan.FromMinutes(1));

        Assert.False(_timer.Start(TimeSpan.FromMinutes(30)));
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Duration);
        Assert.Equal(TimeSpan.FromMinutes(9), _timer.Remaining);
    }

    [Fact]
    public void Running中のキャンセルでIdleに戻る()
    {
        _timer.Start(TimeSpan.FromMinutes(10));

        Assert.True(_timer.Cancel());
        Assert.Equal(TimerState.Idle, _timer.State);
        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
    }

    [Fact]
    public void TimeUp中のキャンセルでIdleに戻る()
    {
        _timer.Start(TimeSpan.FromMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(1));
        _timer.Update();

        Assert.True(_timer.Cancel());
        Assert.Equal(TimerState.Idle, _timer.State);
    }

    [Fact]
    public void Idle中のキャンセルは何もしない()
    {
        var changes = 0;
        _timer.StateChanged += (_, _) => changes++;

        Assert.False(_timer.Cancel());
        Assert.Equal(0, changes);
    }

    [Fact]
    public void TimeUp中に開始すると新しいタイマーが始まる()
    {
        _timer.Start(TimeSpan.FromMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(5));
        _timer.Update();

        Assert.True(_timer.Start(TimeSpan.FromMinutes(30)));
        Assert.Equal(TimerState.Running, _timer.State);
        Assert.Equal(TimeSpan.FromMinutes(30), _timer.Remaining);
    }

    [Fact]
    public void 時間を過ぎてもUpdate前の残り時間は負にならない()
    {
        _timer.Start(TimeSpan.FromMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(2));

        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
        Assert.Equal(TimeSpan.FromMinutes(1), _timer.Elapsed);
        Assert.Equal(1, _timer.Progress);
    }

    [Fact]
    public void スリープ中に0を過ぎていれば復帰後のUpdateでTimeUpになる()
    {
        _timer.Start(TimeSpan.FromMinutes(10));
        _clock.Advance(TimeSpan.FromMinutes(2));
        _timer.Update();

        // スリープで2時間分まとめて時刻が進んだ状態
        _clock.Advance(TimeSpan.FromHours(2));
        _timer.Update();

        Assert.Equal(TimerState.TimeUp, _timer.State);
        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
    }

    [Fact]
    public void TimeUpからの経過時間はTimeUpを検出した時点から数える()
    {
        _timer.Start(TimeSpan.FromMinutes(1));
        _clock.Advance(TimeSpan.FromMinutes(30));
        _timer.Update();
        Assert.Equal(TimeSpan.Zero, _timer.TimeSinceTimeUp);

        _clock.Advance(TimeSpan.FromSeconds(15));
        Assert.Equal(TimeSpan.FromSeconds(15), _timer.TimeSinceTimeUp);

        _timer.Cancel();
        Assert.Equal(TimeSpan.Zero, _timer.TimeSinceTimeUp);
    }

    [Fact]
    public void カウントアップは経過時間が増え続け_TimeUpにならない()
    {
        Assert.True(_timer.StartCountUp());
        Assert.Equal(TimerState.Running, _timer.State);
        Assert.Equal(TimerMode.CountUp, _timer.Mode);
        Assert.Equal(TimeSpan.Zero, _timer.Elapsed);

        _clock.Advance(TimeSpan.FromHours(30));
        _timer.Update();

        Assert.Equal(TimerState.Running, _timer.State);
        Assert.Equal(TimeSpan.FromHours(30), _timer.Elapsed);
        Assert.Equal(TimeSpan.Zero, _timer.Remaining);
        Assert.Equal(0, _timer.Progress);
    }

    [Fact]
    public void カウントアップを止めると止めた時点の経過時間を保ってStoppedになる()
    {
        _timer.StartCountUp();
        _clock.Advance(TimeSpan.FromSeconds(754));

        Assert.True(_timer.Stop());
        Assert.Equal(TimerState.Stopped, _timer.State);

        _clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Equal(TimeSpan.FromSeconds(754), _timer.Elapsed);
    }

    [Fact]
    public void Stoppedのキャンセルでidleに戻る()
    {
        _timer.StartCountUp();
        _clock.Advance(TimeSpan.FromMinutes(1));
        _timer.Stop();

        Assert.True(_timer.Cancel());
        Assert.Equal(TimerState.Idle, _timer.State);
        Assert.Equal(TimerMode.CountDown, _timer.Mode);
        Assert.Equal(TimeSpan.Zero, _timer.Elapsed);
    }

    [Fact]
    public void カウントダウン中のStopは何もしない()
    {
        _timer.Start(TimeSpan.FromMinutes(10));

        Assert.False(_timer.Stop());
        Assert.Equal(TimerState.Running, _timer.State);
        Assert.False(new TimerService(_clock).Stop());
    }

    [Fact]
    public void Running中はカウントアップもカウントダウンも開始できない()
    {
        _timer.StartCountUp();
        Assert.False(_timer.Start(TimeSpan.FromMinutes(10)));
        Assert.False(_timer.StartCountUp());
        Assert.Equal(TimerMode.CountUp, _timer.Mode);

        var countDown = new TimerService(_clock);
        countDown.Start(TimeSpan.FromMinutes(10));
        Assert.False(countDown.StartCountUp());
        Assert.Equal(TimerMode.CountDown, countDown.Mode);
    }

    [Fact]
    public void Stoppedからはカウントダウンもカウントアップも新しく始められる()
    {
        _timer.StartCountUp();
        _clock.Advance(TimeSpan.FromMinutes(3));
        _timer.Stop();

        Assert.True(_timer.Start(TimeSpan.FromMinutes(10)));
        Assert.Equal(TimerMode.CountDown, _timer.Mode);
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Remaining);

        _clock.Advance(TimeSpan.FromMinutes(10));
        _timer.Update();
        Assert.Equal(TimerState.TimeUp, _timer.State);

        Assert.True(_timer.StartCountUp());
        Assert.Equal(TimerMode.CountUp, _timer.Mode);
        Assert.Equal(TimeSpan.Zero, _timer.Elapsed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void 時間が0以下なら開始できない(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _timer.Start(TimeSpan.FromSeconds(seconds)));
        Assert.Equal(TimerState.Idle, _timer.State);
    }

    [Fact]
    public void アラームは指定時刻までのカウントダウンとして計り_TimeUpまで時刻を保つ()
    {
        Assert.True(_timer.Start(TimeSpan.FromMinutes(90), new TimeOnly(7, 30)));

        Assert.Equal(TimerState.Running, _timer.State);
        Assert.Equal(TimerMode.CountDown, _timer.Mode);
        Assert.Equal(new TimeOnly(7, 30), _timer.AlarmTime);
        Assert.Equal(TimeSpan.FromMinutes(90), _timer.Remaining);

        _clock.Advance(TimeSpan.FromMinutes(90));
        _timer.Update();

        Assert.Equal(TimerState.TimeUp, _timer.State);
        Assert.Equal(new TimeOnly(7, 30), _timer.AlarmTime);
    }

    [Fact]
    public void アラームの時刻は解除や別の開始で消える()
    {
        _timer.Start(TimeSpan.FromMinutes(5), new TimeOnly(12, 0));
        _timer.Cancel();
        Assert.Null(_timer.AlarmTime);

        _timer.Start(TimeSpan.FromMinutes(5), new TimeOnly(12, 0));
        _clock.Advance(TimeSpan.FromMinutes(5));
        _timer.Update();
        Assert.True(_timer.Start(TimeSpan.FromMinutes(10)));
        Assert.Null(_timer.AlarmTime);

        _timer.Cancel();
        _timer.Start(TimeSpan.FromMinutes(5), new TimeOnly(12, 0));
        _clock.Advance(TimeSpan.FromMinutes(5));
        _timer.Update();
        Assert.True(_timer.StartCountUp());
        Assert.Null(_timer.AlarmTime);
    }

    [Fact]
    public void Running中はアラームも開始できない()
    {
        _timer.Start(TimeSpan.FromMinutes(10));

        Assert.False(_timer.Start(TimeSpan.FromMinutes(5), new TimeOnly(12, 0)));
        Assert.Null(_timer.AlarmTime);
        Assert.Equal(TimeSpan.FromMinutes(10), _timer.Duration);
    }
}
