using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class PieceValuesTest
{
    [Fact]
    public void Of_ForChaturaji2027_IsWhatTheRulesSayTakingEachPieceScores()
    {
        // Arrange - rules.md §5

        // Act
        ImmutableDictionary<PieceKindId, int> values =
            PieceValues.Of(typeof(PieceValues).Assembly, Chaturaji2027Rules.RulesetId);

        // Assert
        Assert.Equal(
            (1, 2, 3, 4, 5, 5),
            (
                values[Chaturaji2027Pieces.Pawn],
                values[Chaturaji2027Pieces.Boat],
                values[Chaturaji2027Pieces.Horse],
                values[Chaturaji2027Pieces.Elephant],
                values[Chaturaji2027Pieces.Raja],
                values.Count
            )
        );
    }

    [Fact]
    public void Of_WhenTheAssemblyKeepsNoTableForTheGame_ThrowsArgumentException()
    {
        // Arrange - nothing beyond the assembly

        // Act
        static ImmutableDictionary<PieceKindId, int> Reading() =>
            PieceValues.Of(typeof(PieceValues).Assembly, "no-such-game");

        // Assert
        _ = Assert.Throws<ArgumentException>(Reading);
    }
}
