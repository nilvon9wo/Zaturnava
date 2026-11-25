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
    public void Around_WhenTheBotWonAsOftenAsItLostAgainstOneRatedBot_RatesItAsThatBot()
    {
        // Arrange - sage, as second, won 2 and lost 2 against argus at 1500
        ImmutableList<BotMatchup> matchups = [new("chess", "argus", "sage") { Won = 2, Lost = 2 }];

        // Act
        ImmutableDictionary<string, int> ratings =
            BotRatingFit.Around(ImmutableDictionary<string, int>.Empty.Add("argus", 1500), matchups);

        // Assert
        Assert.Equal(ImmutableDictionary<string, int>.Empty.Add("sage", 1500), ratings);
    }

    [Theory]
    [InlineData(BotRatingFit.MostRounds, 1000 + 338)]
    [InlineData(1, 1097)]
    public void Around_WhenTheBotWonEveryGame_RatesItByItsWinsWithTheAddedDraw(int mostRounds, int expected)
    {
        // Arrange - sage, as first, won 3 of 3 against random at 1000: 3.5 wins to 0.5, so 7 times as strong, once
        // the fit settles; one round falls short of it
        ImmutableList<BotMatchup> matchups = [new("chess", "sage", "random") { Won = 3 }];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Around(
            ImmutableDictionary<string, int>.Empty.Add("random", 1000),
            matchups,
            mostRounds
        );

        // Assert
        Assert.Equal(expected, ratings["sage"]);
    }

    [Fact]
    public void Around_WhenABotPlayedNoRatedBotNorAnyThatDid_LeavesItOut()
    {
        // Arrange - argus and sage played only each other
        ImmutableList<BotMatchup> matchups =
        [
            new("chess", "argus", "sage") { Won = 1 },
            new("chess", "midas", "random") { Won = 1 },
        ];

        // Act
        ImmutableDictionary<string, int> ratings =
            BotRatingFit.Around(ImmutableDictionary<string, int>.Empty.Add("random", 1000), matchups);

        // Assert
        Assert.Equal(["midas"], ratings.Keys);
    }

    [Fact]
    public void Around_WhenTwoBotsFareAlikeAgainstTheRatedButOneBeatsTheOther_RatesItAbove()
    {
        // Arrange - morpheus and tantalus each beat random 9 of 9 and lost 9 of 9 to midas at 1727, which alone
        // rates them alike; but tantalus beat morpheus 8 of 10
        ImmutableDictionary<string, int> held = ImmutableDictionary<string, int>.Empty
            .Add("random", 1000)
            .Add("midas", 1727);
        ImmutableList<BotMatchup> matchups =
        [
            new("chess", "morpheus", "random") { Won = 9 },
            new("chess", "midas", "morpheus") { Won = 9 },
            new("chess", "random", "tantalus") { Lost = 9 },
            new("chess", "midas", "tantalus") { Won = 9 },
            new("chess", "morpheus", "tantalus") { Won = 2, Lost = 8 },
        ];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Around(held, matchups);

        // Assert - about the middle of random and midas, as alone each would rate, but tantalus 145 above morpheus
        Assert.Equal((1291, 1436), (ratings["morpheus"], ratings["tantalus"]));
    }
}
