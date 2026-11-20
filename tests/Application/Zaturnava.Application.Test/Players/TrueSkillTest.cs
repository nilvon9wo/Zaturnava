using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Players;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Players;

public sealed class TrueSkillTest
{
    private static readonly ImmutableList<TrueSkill> Pair = [TrueSkill.First, TrueSkill.First];

    [Fact]
    public void After_WhenEvenPairsPlayAndOneWins_MovesEachAsMuchUpAsDownAndNarrowsEvery()
    {
        // Arrange - four new players, two a side

        // Act
        (ImmutableList<TrueSkill> won, ImmutableList<TrueSkill> lost) = TrueSkill.After(Pair, Pair, 1);

        // Assert
        Assert.True(
            won[0].Mean > TrueSkill.First.Mean
                && Math.Abs(won[0].Mean - TrueSkill.First.Mean - (TrueSkill.First.Mean - lost[0].Mean)) < 1e-9
                && won.Concat(lost).All(rating => rating.Deviation < TrueSkill.First.Deviation),
            $"{won[0]} {lost[0]}"
        );
    }

    [Fact]
    public void After_WhenTheSideLoses_IsAsTheOthersWinning()
    {
        // Arrange - the same game, told from the losers' side

        // Act
        (ImmutableList<TrueSkill> lost, ImmutableList<TrueSkill> won) = TrueSkill.After(Pair, Pair, 0);

        // Assert
        (ImmutableList<TrueSkill> winners, ImmutableList<TrueSkill> losers) = TrueSkill.After(Pair, Pair, 1);
        Assert.Equal([.. winners, .. losers], [.. won, .. lost]);
    }

    [Fact]
    public void After_WhenEvenPairsDraw_KeepsEveryMeanAndNarrowsEvery()
    {
        // Arrange - four new players, two a side

        // Act
        (ImmutableList<TrueSkill> side, ImmutableList<TrueSkill> others) = TrueSkill.After(Pair, Pair, 0.5);

        // Assert
        Assert.True(side.Concat(others).All(IsEvenAndSurer), $"{side[0]} {others[0]}");
    }

    private static bool IsEvenAndSurer(TrueSkill rating) =>
        Math.Abs(rating.Mean - TrueSkill.First.Mean) < 1e-9 && rating.Deviation < TrueSkill.First.Deviation;

    [Fact]
    public void After_WhenTheWeakerWin_GainMoreThanTheStrongerWould()
    {
        // Arrange - a pair rated 200 Elo below another, both sure
        ImmutableList<TrueSkill> weaker = [TrueSkill.FromElo(1400, 2), TrueSkill.FromElo(1400, 2)];
        ImmutableList<TrueSkill> stronger = [TrueSkill.FromElo(1600, 2), TrueSkill.FromElo(1600, 2)];

        // Act
        double upset = TrueSkill.After(weaker, stronger, 1).Side[0].Mean - weaker[0].Mean;
        double expected = TrueSkill.After(stronger, weaker, 1).Side[0].Mean - stronger[0].Mean;

        // Assert
        Assert.True(upset > expected && expected > 0, $"{upset} {expected}");
    }

    [Fact]
    public void FromElo_WhenAsked_TakesTwoHundredEloAsOneBetaFromTheFirstMean()
    {
        // Arrange - 1700 Elo, 200 above the first

        // Act
        TrueSkill rating = TrueSkill.FromElo(1700, 1);

        // Assert - and shown as the mean less three deviations
        Assert.Equal((25 + TrueSkill.Beta, 25 + TrueSkill.Beta - 3), (rating.Mean, rating.Conservative));
    }
}
