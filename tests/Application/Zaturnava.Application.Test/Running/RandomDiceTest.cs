using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Application.Running;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Running;

public sealed class RandomDiceTest
{
    [Fact]
    public void Roll_WhenAskedForDice_GivesOneValueOnEachDieFromOneToItsSides()
    {
        // Arrange
        RandomDice dice = new(new Random(7));

        // Act
        ImmutableList<int> values = dice.Roll(new DiceRequest(200, 4));

        // Assert
        Assert.Equal((200, 1, 4), (values.Count, values.Min(), values.Max()));
    }

    [Fact]
    public void Roll_WhenTwoSixSidedDiceAreRolledOftenOneAfterAnother_ShowsDoublesOnceInSixAndNoStreaks()
    {
        // Arrange - playtest 2026-11-15: "a bias towards enemy bots rolling doubles ... doubles seem to be rolled
        // in streaks"; 60,000 rolls, as every seat's are rolled, one after another from the one source
        RandomDice dice = new(new Random(2026));
        DiceRequest request = new(2, 6);

        // Act
        ImmutableList<bool> doubles =
        [
            .. Enumerable.Range(0, 60_000).Select(_ => dice.Roll(request)).Select(roll => roll[0] == roll[1]),
        ];
        int afterDoubles = doubles.Skip(1).Where((_, index) => doubles[index]).Count();
        int doublesAfterDoubles = doubles.Skip(1).Where((isDouble, index) => doubles[index] && isDouble).Count();

        // Assert - a sixth of all rolls, and a sixth of those just after doubles, each within about five
        // standard deviations
        Assert.Equal(
            (true, true),
            (
                Math.Abs(doubles.Count(isDouble => isDouble) - 10_000) < 500,
                Math.Abs((doublesAfterDoubles * 6) - afterDoubles) < 1_200
            )
        );
    }
}
