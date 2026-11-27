using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class HuntingTest
{
    [Fact]
    public void ValueOf_WhenAlliedThronesAreGuarded_JudgesAnEnemyLeaderBesideAnAllysEmptyThroneWorse()
    {
        // Arrange - the ally's empty throne on 6, the enemy's Raja on 7 beside it, the ally's Boat on 1
        DecisionRequest request = AllysThrone(7);
        SageWeights unguarded = SageWeights.Default;
        SageWeights guarded = SageWeights.Default with { Hunting = Hunting.OfLeaders with { AlliedThrones = true } };

        // Act
        double withoutGuard = Appraisal.ValueOf(request, request.State, unguarded);
        double withGuard = Appraisal.ValueOf(request, request.State, guarded);

        // Assert
        Assert.True(withGuard < withoutGuard, $"{withGuard} should be below {withoutGuard}");
    }

    [Fact]
    public void ValueOf_WhenThronesCountByStake_JudgesAnEnemyOnAnAllysThroneWorseTheMoreItControls()
    {
        // Arrange - the enemy's Raja on the ally's throne, on 6, the ally's Boat on 1 under its control
        DecisionRequest request = AllysThrone(6);
        SageWeights flat = SageWeights.Default;
        SageWeights byStake = SageWeights.Default with { Hunting = Hunting.OfLeaders with { ThronesByStake = true } };

        // Act
        double counted = Appraisal.ValueOf(request, request.State, flat);
        double staked = Appraisal.ValueOf(request, request.State, byStake);

        // Assert
        Assert.True(staked < counted, $"{staked} should be below {counted}");
    }

    [Fact]
    public void ValueOf_WhenForksAreForeseen_JudgesAPositionAnEnemyCouldForkWorse()
    {
        // Arrange - our Elephants on 2 and 3, which the enemy's long-reaching Horse on 7 could fork from 5
        DecisionRequest request = CarefulTest.Forkable(1, LineRules.Elephant, 1);
        SageWeights blind = SageWeights.Default;
        SageWeights wary = SageWeights.Default with { Hunting = Hunting.OfLeaders with { Forks = true } };

        // Act
        double unseen = Appraisal.ValueOf(request, request.State, blind);
        double foreseen = Appraisal.ValueOf(request, request.State, wary);

        // Assert
        Assert.True(foreseen < unseen, $"{foreseen} should be below {unseen}");
    }

    [Fact]
    public void Equals_WhenOnlyTheHuntingDiffers_IsFalse()
    {
        // Arrange
        SageWeights leaders = SageWeights.Default;
        SageWeights heavies = SageWeights.Default with { Hunting = Hunting.OfHeavies };

        // Act
        bool isEqual = leaders.Equals(heavies);

        // Assert
        Assert.False(isEqual);
    }

    [Fact]
    public void With_WhenAWeightIsChanged_KeepsTheHunting()
    {
        // Arrange
        SageWeights heavies = SageWeights.Default with { Hunting = Hunting.OfHeavies };

        // Act
        SageWeights changed = heavies.With(0, 2);

        // Assert
        Assert.Equal(Hunting.OfHeavies, changed.Hunting);
    }

    /// <summary>The ally's throne on 6 and its Boat on 1, and the enemy's Raja on <paramref name="raja"/>.</summary>
    private static DecisionRequest AllysThrone(int raja)
    {
        DecisionRequest request = LineRules.Request(
            [new PassAction(LineRules.Seat)],
            (raja, LineRules.Raja, LineRules.Enemy),
            (1, LineRules.Boat, new SeatId(2))
        );
        return request with
        {
            Rules = new LineRules([.. request.LegalActions])
            {
                Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(new SeatId(2), new CellId(6)),
            },
        };
    }
}
