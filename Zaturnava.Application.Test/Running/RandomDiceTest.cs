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
}
