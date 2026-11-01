using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// What a river means to the pieces (specification §2): a Xiangqi Elephant may neither cross one nor enter a
/// cell it cuts. A Xiangqi Soldier's sideways step once across is still to come.
/// </summary>
public static class RiverCrossing
{
    public static MoveAllowing On([Required] GrandBoard board) => (grid, move) => MayMove(board, grid, move);

    private static bool MayMove(GrandBoard board, SquareGridTopology grid, Move move)
    {
        Kingdom home = board.Kingdoms[move.Mover.Owner.Value];
        (int column, int row) = (grid.ColumnOf(move.To), grid.RowOf(move.To));
        return move.Mover.Kind != GrandLegionsPieces.XiangqiElephant
            || !(board.IsInRiver(column, row) || board.IsAcrossRiver(home, column, row));
    }
}
