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

    [Fact]
    public void Against_WhenTheBotWonAsOftenAsItLostAgainstOneRatedBot_RatesItAsThatBot()
    {
        // Arrange - sage, as second, won 2 and lost 2 against argus at 1500; a game with an unrated bot left out
        ImmutableList<BotMatchup> matchups =
        [
            new("chess", "argus", "sage") { Won = 2, Lost = 2 },
            new("chess", "sage", "zeno") { Won = 9 },
        ];

        // Act
        int? rating = BotRatingFit.Against(
            "sage",
            ImmutableDictionary<string, int>.Empty.Add("argus", 1500),
            matchups
        );

        // Assert
        Assert.Equal(1500, rating);
    }

    [Fact]
    public void Against_WhenTheBotWonEveryGame_RatesItByItsWinsWithTheAddedDraw()
    {
        // Arrange - sage, as first, won 3 of 3 against random at 1000: 3.5 wins to 0.5, so 7 times as strong
        ImmutableList<BotMatchup> matchups = [new("chess", "sage", "random") { Won = 3 }];

        // Act
        int? rating = BotRatingFit.Against(
            "sage",
            ImmutableDictionary<string, int>.Empty.Add("random", 1000),
            matchups
        );

        // Assert
        Assert.Equal(1000 + 338, rating);
    }

    [Fact]
    public void Against_WhenTheBotPlayedNoRatedBot_RatesItNot()
    {
        // Arrange
        ImmutableList<BotMatchup> matchups = [new("chess", "argus", "sage") { Won = 1 }];

        // Act
        int? rating = BotRatingFit.Against(
            "sage",
            ImmutableDictionary<string, int>.Empty.Add("random", 1000),
            matchups
        );

        // Assert
        Assert.Null(rating);
    }
}
