using Net.NowhereAtAll.Zaturnava.Engine.Boards;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Boards;

public sealed class DirectionSetsTest
{
    [Fact]
    public void Leaper_WhenGivenItsDistances_IsNamedForThem()
    {
        // Arrange - nothing beyond the arguments

        // Act
        DirectionSet zebra = DirectionSets.Leaper(3, 2);

        // Assert
        Assert.Equal("leaper-3-2", zebra.Name);
    }
}
