using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// What a river means to the pieces (specification §2): a Xiangqi Elephant may neither cross one nor enter a
/// cell it cuts; a Xiangqi Soldier steps sideways only once it stands across a river from its kingdom, across either
/// where there are two, and never where there is none. Neither, once that restriction is bought off (§7).
/// </summary>
public static class RiverCrossing
{
    /// <summary>
    /// The edition from which a Xiangqi Elephant's river is the one between where it stands and where it would go, so
    /// that one dealt across a river from its kingdom's corner moves about on that side (playtest 2026-11-15: "Movement
    /// restrictions must be defined relative to the actual river"); before it, the Elephant could go nowhere across a
    /// river from its kingdom's corner, and so, dealt there, only back over the river.
    /// </summary>
    public const int RiverWhereItStandsEdition = 8;

    /// <summary>
    /// The rivers of <paramref name="game"/>, as each game of it was dealt them (<see cref="QuickTerrain"/>).
    /// </summary>
    public static MoveAllowing On([Required] QuickGame game) =>
        (state, grid, move) => MayMove(QuickTerrain.BoardOf(game, state.Terrain), state, grid, move);

    private static bool MayMove(GrandBoard board, GameState state, SquareGridTopology grid, Move move)
    {
        Kingdom home = board.Kingdoms[move.Mover.Owner.Value];
        (int column, int row) = (grid.ColumnOf(move.To), grid.RowOf(move.To));
        bool isAcross = state.Edition >= RiverWhereItStandsEdition
            ? board.Crosses((grid.ColumnOf(move.From), grid.RowOf(move.From)), (column, row))
            : board.IsAcrossRiver(home, column, row);
        return state.IsLifted(move.Mover, LiftKind.Area)
            || (move.Mover.Kind == GrandLegionsPieces.XiangqiElephant
            ? !(board.IsInRiver(column, row) || isAcross)
            : move.Mover.Kind != GrandLegionsPieces.XiangqiSoldier
                || !IsSideways(state.SeatOf(move.Mover.Owner).Forward, grid, move)
                || board.IsAcrossRiver(home, grid.ColumnOf(move.From), grid.RowOf(move.From)));
    }

    /// <summary>Whether the move goes neither toward nor away from <paramref name="forward"/>.</summary>
    private static bool IsSideways(Direction forward, SquareGridTopology grid, Move move) =>
        ((grid.ColumnOf(move.To) - grid.ColumnOf(move.From)) * forward.First)
        + ((grid.RowOf(move.To) - grid.RowOf(move.From)) * forward.Second) == 0;
}
