using Net.NowhereAtAll.Zaturnava.Application.Players;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class StandingCacheTest
{
    [Theory]
    [InlineData(59, false, "first")]
    [InlineData(60, false, "second")]
    [InlineData(1, true, "second")]
    public async Task Of_WhenAskedAgain_GivesWhatWasCountedWhileFreshAndUnforgotten(
        int seconds,
        bool isForgotten,
        string expected
    )
    {
        // Arrange
        SteppedTime time = new();
        StandingCache cache = new(time);
        _ = await cache.Of("people/chaturaji-2027", () => Task.FromResult("first"));
        time.Pass(TimeSpan.FromSeconds(seconds));
        if (isForgotten)
        {
            cache.Forget();
        }

        // Act
        string given = await cache.Of("people/chaturaji-2027", () => Task.FromResult("second"));

        // Assert
        Assert.Equal(expected, given);
    }

    [Fact]
    public async Task Of_WhenAskedForAnother_CountsItApart()
    {
        // Arrange
        StandingCache cache = new(new SteppedTime());
        _ = await cache.Of("people/chaturaji-2027", () => Task.FromResult("people"));

        // Act
        string given = await cache.Of("bots/chaturaji-2027", () => Task.FromResult("bots"));

        // Assert
        Assert.Equal("bots", given);
    }
}
