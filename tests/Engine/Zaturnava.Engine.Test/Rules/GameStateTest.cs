using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Rules;

public sealed class GameStateTest
{
    [Fact]
    public void SeatOf_WhenTheSeatExists_ReturnsIt()
    {
        // Arrange
        Seat left = new(new SeatId(0), LineTopology.Right);
        Seat right = new(new SeatId(1), LineTopology.Left);
        GameState state = new(
            Position.Empty,
            [left, right],
            TeamGraph.FromTeams([[left.Id], [right.Id]]),
            left.Id,
            1,
            [],
            []
        );

        // Act
        Seat seat = state.SeatOf(right.Id);

        // Assert
        Assert.Equal(right, seat);
    }

    [Theory]
    [InlineData("elephant", true)]
    [InlineData("queen", false)]
    public void IsLifted_ForAPieceWhoseAreaRestrictionWasBoughtOffAsAnElephant_HoldsOnlyWhileItIsOne(
        string kind,
        bool expected
    )
    {
        // Arrange - piece 3's area restriction bought off as an Elephant; the piece maybe since promoted
        Seat left = new(new SeatId(0), LineTopology.Right);
        GameState state = new(Position.Empty, [left], TeamGraph.FromTeams([[left.Id]]), left.Id, 1, [], [])
        {
            Lifts = [new Lift(new PieceId(3), new PieceKindId("elephant"), LiftKind.Area)],
        };
        Piece piece = new(new PieceId(3), new PieceKindId(kind), left.Id);

        // Act
        (bool Area, bool Movement) lifted = (
            state.IsLifted(piece, LiftKind.Area),
            state.IsLifted(piece, LiftKind.Movement)
        );

        // Assert
        Assert.Equal((expected, false), lifted);
    }

    [Fact]
    public void EarnedBy_WhenASeatKeptSomeOfItsDraftUnspent_CountsItsScoreWithoutIt()
    {
        // Arrange - the left seat kept 30 of its budget, which began its score of 42; the right drafted nothing
        Seat left = new(new SeatId(0), LineTopology.Right);
        Seat right = new(new SeatId(1), LineTopology.Left);
        GameState state = new(
            Position.Empty,
            [left, right],
            TeamGraph.FromTeams([[left.Id], [right.Id]]),
            left.Id,
            1,
            ImmutableDictionary<SeatId, int>.Empty.Add(left.Id, 42).Add(right.Id, 5),
            []
        )
        {
            Unspent = ImmutableDictionary<SeatId, int>.Empty.Add(left.Id, 30),
        };

        // Act
        (int Left, int Right) earned = (state.EarnedBy(left.Id), state.EarnedBy(right.Id));

        // Assert
        Assert.Equal((12, 5), earned);
    }
}
