using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions.Test;

public sealed class RiverCrossingTest
{
    [Theory]
    [InlineData(8, "xiangqi-elephant", 2, true)]
    [InlineData(8, "xiangqi-elephant", 4, false)]
    [InlineData(9, "xiangqi-elephant", 4, false)]
    [InlineData(9, "xiangqi-elephant", 3, true)]
    [InlineData(8, "western-king", 4, true)]
    public void On_WhenARiverRunsNorthToSouth_KeepsOnlyTheXiangqiElephantOnItsOwnSide(
        int side,
        string kind,
        int column,
        bool mayMove
    )
    {
        // Arrange - seat 0's piece, in the west, goes to the column given, in a game dealt a north-south river
        QuickGame game = QuickGame.Medium with { Side = side, KingdomWidth = 3, KingdomDepth = 3 };
        GameState state = new(Position.Empty, [], TeamGraph.FromTeams([]), null, 0, [], []) { Terrain = 1 };
        SquareGridTopology grid = new(side, side);
        Piece piece = new(new PieceId(1), new PieceKindId(kind), new SeatId(0));
        MoveAllowing allowing = RiverCrossing.On(game);

        // Act
        bool isAllowed = allowing(
            state,
            grid,
            new Move(piece, grid.CellAt(column - 2, 1), grid.CellAt(column, 3), null)
        );

        // Assert
        Assert.Equal(mayMove, isAllowed);
    }

    [Theory]
    [InlineData(1, 5, 6, 3, true)]
    [InlineData(1, 5, 4, 3, true)]
    [InlineData(1, 2, 3, 3, false)]
    [InlineData(1, 2, 2, 4, true)]
    [InlineData(0, 5, 6, 3, false)]
    public void On_WhenAXiangqiSoldierStepsSideways_LetsItOnlyOnceItIsAcrossARiver(
        int terrain,
        int fromColumn,
        int toColumn,
        int toRow,
        bool mayMove
    )
    {
        // Arrange - seat 0's Soldier, facing up the 8 × 8 board from the west, on row 3; terrain 1 a north-south
        // river between columns 3 and 4, terrain 0 none
        QuickGame game = QuickGame.Medium with { KingdomWidth = 3, KingdomDepth = 3 };
        GameState state = new(
            Position.Empty,
            [new Seat(new SeatId(0), new Direction(0, 1))],
            TeamGraph.FromTeams([]),
            null,
            0,
            [],
            []
        )
        {
            Terrain = terrain,
        };
        SquareGridTopology grid = new(8, 8);
        Piece soldier = new(new PieceId(1), GrandLegionsPieces.XiangqiSoldier, new SeatId(0));

        // Act
        bool isAllowed = RiverCrossing.On(game)(
            state,
            grid,
            new Move(soldier, grid.CellAt(fromColumn, 3), grid.CellAt(toColumn, toRow), null)
        );

        // Assert
        Assert.Equal(mayMove, isAllowed);
    }
}
