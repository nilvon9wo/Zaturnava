using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class FakedDiceTest
{
    private static readonly DiceRequest TwoDice = new(2, 6);

    [Fact]
    public void Roll_WhenSomeFacesAreFaked_ComesUpAsFakedOnceAndRollsTheRestAndAfterward()
    {
        // Arrange - the user's request of 2026-11-19: to "roll" a 6 on the first die, in a scenario
        FakedDice dice = new(new LoadedDice(4), [6, null]);

        // Act
        ImmutableList<int> first = dice.Roll(TwoDice);
        ImmutableList<int> second = dice.Roll(TwoDice);

        // Assert
        Assert.Equal(("6 4", "4 4"), (string.Join(' ', first), string.Join(' ', second)));
    }
}
