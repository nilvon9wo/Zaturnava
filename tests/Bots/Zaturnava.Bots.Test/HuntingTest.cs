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
        // Arrange - the ally's empty throne on 6, the enemy's Raja on 7 beside it
        DecisionRequest request = LineRules.Request(
            [new PassAction(LineRules.Seat)],
            (7, LineRules.Raja, LineRules.Enemy)
        );
        request = request with
        {
            Rules = new LineRules([.. request.LegalActions])
            {
                Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(new SeatId(2), new CellId(6)),
            },
        };
        SageWeights unguarded = SageWeights.Default;
        SageWeights guarded = SageWeights.Default with { Hunting = Hunting.OfHeavies };

        // Act
        double withoutGuard = Appraisal.ValueOf(request, request.State, unguarded);
        double withGuard = Appraisal.ValueOf(request, request.State, guarded);

        // Assert
        Assert.True(withGuard < withoutGuard, $"{withGuard} should be below {withoutGuard}");
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
}
