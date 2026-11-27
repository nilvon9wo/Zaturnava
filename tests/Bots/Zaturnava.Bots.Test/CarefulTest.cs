using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

public sealed class CarefulTest
{
    private static readonly SeatId Mine = LineRules.Seat;
    private static readonly SeatId First = LineRules.Enemy;
    private static readonly SeatId Second = new(3);
    private static readonly SeatId Ally = new(2);
    private static readonly (int, PieceKindId, SeatId)[] NoPieces = [];

    [Theory]
    [InlineData(3, 6)]
    [InlineData(1, 5)]
    public void Exposure_WhenTwoPiecesHang_CountsEachInFullIfTwoEnemiesCouldTakeThemButHalfTheSecondIfOneMust(
        int secondTaker,
        int exposure
    )
    {
        // Arrange - Horses on 2 and 5, unguarded, the one attacked by a Pawn of the enemy moving first, the other by
        // one of the enemy moving second, or of the first again: each enemy may take its own, but one enemy takes
        // twice only if its dice fall right (the games played, 2026-11-27: with one piece hanging, another cost
        // nothing, and Yellow's Boat, then its Raja, went)
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
    public void Exposure_WhenAnEnemyPieceStandsBehindTheTaker_CountsItTakingBackTheRetaker()
    {
        // Arrange - a Horse (3) on 4, guarded by a Pawn on 3, attacked by an enemy Pawn on 5, with the enemy's Boat on
        // 6 behind it: once the Pawn takes, the Boat sees 4, and takes back the Pawn that took back, so the Horse is
        // lost for a Pawn, not for nothing (the games played, 2026-11-27: Green's Rook behind its Raja)
        DecisionRequest request = LineRules.Request(
            [new PassAction(Mine)],
            (3, LineRules.Pawn, Mine),
            (4, LineRules.Horse, Mine),
            (5, LineRules.Pawn, First),
            (6, LineRules.Boat, First)
        );

        // Act
        int counted = Careful.Exposure(request, request.State);

        // Assert
        Assert.Equal(3, counted);
    }

    [Theory]
    [InlineData(2, false, true)]
    [InlineData(2, true, false)]
    [InlineData(3, false, false)]
    [InlineData(0, false, false)]
    public void AlliedThroneThreat_WhenAnEnemyLeaderCouldStepOntoAnAllysEmptyThrone_IsWhatThatAllysPiecesAreWorth(
        int owner,
        bool isHeld,
        bool isThreat
    )
    {
        // Arrange - a throne on 6, the ally's (2), an enemy's (3) or the bot's own (0, Exposure's to count), with the
        // enemy's Raja on 7 beside it; empty, or held by a Pawn of the ally's; and the ally's last Boat far off, on 1,
        // which an enemy on the throne would take control of (the user, 2026-11-27)
        DecisionRequest request = LineRules.Request(
            [new PassAction(Mine)],
            [
                (7, LineRules.Raja, First), (1, LineRules.Boat, Ally),
                .. isHeld ? [(6, LineRules.Pawn, Ally)] : NoPieces,
            ]
        );
        request = request with
        {
            Rules = new LineRules([.. request.LegalActions])
            {
                Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(new SeatId(owner), new CellId(6)),
            },
        };

        // Act
        int threat = Careful.AlliedThroneThreat(
            request,
            request.State,
            Careful.AttacksBy(request, request.State, isAllied: false)
        );

        // Assert
        Assert.Equal(isThreat ? request.Rules.WorthOf(LineRules.Boat) : 0, threat);
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
