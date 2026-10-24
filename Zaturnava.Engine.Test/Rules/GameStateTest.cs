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
}
