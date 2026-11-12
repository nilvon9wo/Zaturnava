using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Bots;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Bots;

public sealed class BotRatingFitTest
{
    [Theory]
    [InlineData(BotRatingFit.MostRounds)]
    [InlineData(1)]
    public void Fitted_WhenOneBotWonEveryGame_RatesItByItsWinsWithTheAddedDrawAgainstTheAnchor(int mostRounds)
    {
        // Arrange - sage won 3 of 3 against random: 3.5 wins to 0.5, with the draw added, so 7 times as strong
        ImmutableList<BotMatchup> matchups = [new("chess", "random", "sage") { Lost = 3 }];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Fitted("random", matchups, mostRounds);

        // Assert
        Assert.Equal(
            ImmutableSortedDictionary<string, int>.Empty.Add("random", 1000).Add("sage", 1000 + 338),
            ratings.ToImmutableSortedDictionary(StringComparer.Ordinal)
        );
    }

    [Fact]
    public void Fitted_WhenEveryGameWasDrawn_RatesEveryBotAsTheAnchor()
    {
        // Arrange
        ImmutableList<BotMatchup> matchups =
        [
            new("chess", "argus", "random") { Drawn = 2 },
            new("chess", "random", "sage") { Drawn = 4 },
        ];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Fitted("random", matchups);

        // Assert
        Assert.Equal([1000, 1000, 1000], ratings.Values);
    }

    [Fact]
    public void Fitted_WhenTheAnchorPlayedNone_RatesNoOne()
    {
        // Arrange
        ImmutableList<BotMatchup> matchups = [new("chess", "argus", "sage") { Won = 1 }];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Fitted("random", matchups);

        // Assert
        Assert.Empty(ratings);
    }
}
