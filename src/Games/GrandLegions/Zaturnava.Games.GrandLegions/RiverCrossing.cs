using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// What a river means to the pieces (specification §2): a Xiangqi Elephant may neither cross one nor enter a
/// cell it cuts; a Xiangqi Soldier steps sideways only once it stands across a river from its kingdom, across either
/// where there are two, and never where there is none.
/// </summary>
public static class RiverCrossing
{
    /// <summary>
    /// The rivers of <paramref name="game"/>, as each game of it was dealt them (<see cref="QuickTerrain"/>).
    /// </summary>
    public static MoveAllowing On([Required] QuickGame game) =>
        (state, grid, move) => MayMove(QuickTerrain.BoardOf(game, state.Terrain), state, grid, move);

    private static bool MayMove(GrandBoard board, GameState state, SquareGridTopology grid, Move move)
    {
        Kingdom home = board.Kingdoms[move.Mover.Owner.Value];
        (int column, int row) = (grid.ColumnOf(move.To), grid.RowOf(move.To));
        return move.Mover.Kind == GrandLegionsPieces.XiangqiElephant
            ? !(board.IsInRiver(column, row) || board.IsAcrossRiver(home, column, row))
            : move.Mover.Kind != GrandLegionsPieces.XiangqiSoldier
                || !IsSideways(state.SeatOf(move.Mover.Owner).Forward, grid, move)
                || board.IsAcrossRiver(home, grid.ColumnOf(move.From), grid.RowOf(move.From));
    }

    /// <summary>Whether the move goes neither toward nor away from <paramref name="forward"/>.</summary>
    private static bool IsSideways(Direction forward, SquareGridTopology grid, Move move) =>
        ((grid.ColumnOf(move.To) - grid.ColumnOf(move.From)) * forward.First)
        + ((grid.RowOf(move.To) - grid.RowOf(move.From)) * forward.Second) == 0;
}
