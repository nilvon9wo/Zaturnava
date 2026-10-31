using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class ClassDiceTest
{
    [Theory]
    [InlineData(1, new[] { PieceClass.Artillery })]
    [InlineData(2, new[] { PieceClass.Cavalry })]
    [InlineData(4, new[] { PieceClass.Heavy })]
    [InlineData(6, new[] { PieceClass.Infantry, PieceClass.Leader })]
    [InlineData(7, new PieceClass[0])]
    public void ClassesOf_WhenASixSidedDieShowsAValue_NamesTheClassesThatMayMove(int value, PieceClass[] expected)
    {
        // Act
        ImmutableHashSet<PieceClass> classes = ClassDice.SixSided.ClassesOf(value);

        // Assert
        Assert.True(classes.SetEquals(expected));
    }
}
