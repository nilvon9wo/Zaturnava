using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// Where a Makruk Pawn promotes (Stated 2026-11-06): in any kingdom of a seat its army is not allied with, which
/// follows the board's and the kingdoms' sizes.
/// </summary>
public static class EnemyKingdoms
{
    public const string Where = "in an enemy kingdom";

    public static PromotionZone On([Required] GrandBoard board) =>
        (state, grid, piece, cell) => board.Kingdoms.Any(kingdom =>
            kingdom.Contains(grid.ColumnOf(cell), grid.RowOf(cell))
            && !state.Teams.AreAllied(kingdom.Seat, piece.Owner)
        );
}
