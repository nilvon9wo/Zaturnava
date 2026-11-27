using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class CarefulTest
{
    private static readonly SeatId Mine = LineRules.Seat;
    private static readonly SeatId First = LineRules.Enemy;
    private static readonly SeatId Second = new(3);

    [Theory]
    [InlineData(3, 6)]
    [InlineData(1, 5)]
    public void Exposure_WhenTwoPiecesHang_CountsEachInFullIfTwoEnemiesCouldTakeThemButHalfTheSecondIfOneMust(
        int secondTaker,
        int exposure
    )
    {
        // Arrange - Horses on 2 and 5, unguarded, the one attacked by a Pawn of the enemy moving first, the other by one
        // of the enemy moving second, or of the first again: each enemy may take its own, but one enemy takes twice only
        // if its dice fall right (the games played, 2026-11-27: with one piece hanging, another cost nothing, and
        // Yellow's Boat, then its Raja, went)
        DecisionRequest request = LineRules.Request(
            [new PassAction(Mine)],
            (2, LineRules.Horse, Mine),
            (3, LineRules.Pawn, First),
            (5, LineRules.Horse, Mine),
            (6, LineRules.Pawn, new SeatId(secondTaker))
        );

        // Act
        int counted = Careful.Exposure(request, request.State);

        // Assert
        Assert.Equal(exposure, counted);
    }

    [Fact]
    public void Exposure_WhenAnEnemyCouldTakeOnlyAPieceAnotherHasTaken_CountsItOnce()
    {
        // Arrange - one Horse, on 4, attacked by Pawns of both enemies: it can be taken only once
        DecisionRequest request = LineRules.Request(
            [new PassAction(Mine)],
            (3, LineRules.Pawn, First),
            (4, LineRules.Horse, Mine),
            (5, LineRules.Pawn, Second)
        );

        // Act
        int counted = Careful.Exposure(request, request.State);

        // Assert
        Assert.Equal(3, counted);
    }
}
