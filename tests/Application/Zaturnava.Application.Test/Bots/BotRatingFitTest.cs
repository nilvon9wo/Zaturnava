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
    public void Around_WhenTheBotWonAsOftenAsItLostAgainstOneRatedBot_RatesItNearThatBotTheAddedDrawPullingDown()
    {
        // Arrange - sage, as second, won 2 and lost 2 against argus at 1500; its one added draw, against a bot at
        // 1000, pulls it a little below
        ImmutableList<BotMatchup> matchups = [new("chess", "argus", "sage") { Won = 2, Lost = 2 }];

        // Act
        ImmutableDictionary<string, int> ratings =
            BotRatingFit.Around(ImmutableDictionary<string, int>.Empty.Add("argus", 1500), matchups);

        // Assert
        Assert.Equal(ImmutableDictionary<string, int>.Empty.Add("sage", 1426), ratings);
    }

    [Fact]
    public void Around_WhenAWeakBotLostEveryGameToManyStrongBots_RatesItByItsGamesNotNearThem()
    {
        // Arrange (2026-11-26, live: Morpheus, at random but for one turn in 50, rated 1696, by 2 games against each
        // of 20 bots near 2400, each pair with a draw added) - morpheus won 1 and lost 1 against random, and lost both
        // its games to each of the strong
        ImmutableDictionary<string, int> held = Enumerable.Range(0, 20)
            .ToImmutableDictionary(index => $"strong{index}", _ => 2400)
            .Add("random", 1000);
        ImmutableList<BotMatchup> matchups =
        [
            new("chess", "morpheus", "random") { Won = 1, Lost = 1 },
            .. Enumerable.Range(0, 20)
                .Select(index => new BotMatchup("chess", "morpheus", $"strong{index}") { Lost = 2 }),
        ];

        // Act
        ImmutableDictionary<string, int> ratings = BotRatingFit.Around(held, matchups);

        // Assert - level with random, as its games against it say
        Assert.InRange(ratings["morpheus"], 950, 1050);
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

        // Assert - between random and midas, as alone each would rate, but tantalus 158 above morpheus
        Assert.Equal((1250, 1408), (ratings["morpheus"], ratings["tantalus"]));
    }
}
