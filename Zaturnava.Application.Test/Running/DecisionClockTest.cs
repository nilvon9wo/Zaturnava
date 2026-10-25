using Net.NowhereAtAll.Zaturnava.Application.Games;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class DecisionClockTest
{
    private static readonly GameId Game = new("g");
    private static readonly TimeSpan Minute = TimeSpan.FromMinutes(1);

    private readonly SteppedTime _time = new();
    private readonly DecisionClock _clock;

    public DecisionClockTest() => this._clock = new DecisionClock(this._time);

    [Fact]
    public void Since_WhenTheVersionWasSeenBefore_IsWhenItWasFirstSeen()
    {
        // Arrange
        _ = this._clock.Since(Game, 3);
        this._time.Pass(Minute);

        // Act
        DateTimeOffset since = this._clock.Since(Game, 3);

        // Assert
        Assert.Equal(this._time.Now - Minute, since);
    }

    [Fact]
    public void Since_WhenTheGameHasMovedOn_IsNow()
    {
        // Arrange
        _ = this._clock.Since(Game, 3);
        this._time.Pass(Minute);

        // Act
        DateTimeOffset since = this._clock.Since(Game, 4);

        // Assert
        Assert.Equal(this._time.Now, since);
    }
}
