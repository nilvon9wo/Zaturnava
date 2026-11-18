using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// Whether a board set to a size can hold its kingdoms, told before a game is asked for (playtest 2026-11-15, H3). The
/// kingdoms stand in the four corners, turning about the board: two along the bottom and top edges, their back ranks
/// along them; two along the sides, turned. So a kingdom must fit both ways round, and two neighbours together, its
/// width and its depth, must fit along every side. The server refuses any board that cannot be played all the same.
/// </summary>
internal static class BoardFit
{
    private const int MostSide = 26;
    private const int LeastKingdomSide = 3;

    /// <summary>What is first wrong with <paramref name="board"/>, to show the player; none, if nothing.</summary>
    public static string? ProblemOf([Required] BoardSizeDto board)
    {
        int shorter = Math.Min(board.Width, board.Height);
        int together = board.KingdomWidth + board.KingdomDepth;
        ImmutableList<(bool IsWrong, string Problem)> checks =
        [
            (Math.Max(board.Width, board.Height) > MostSide, $"A board may be at most {MostSide} × {MostSide}."),
            (
                Math.Min(board.KingdomWidth, board.KingdomDepth) < LeastKingdomSide,
                $"A kingdom must be at least {LeastKingdomSide} × {LeastKingdomSide}, for its palace."
            ),
            (
                Math.Max(board.KingdomWidth, board.KingdomDepth) > shorter,
                $"A kingdom of {board.KingdomWidth} × {board.KingdomDepth} does not fit on a board of "
                    + $"{board.Width} × {board.Height}: the kingdoms at the sides lie turned, so each of its sides "
                    + $"must be at most {shorter}."
            ),
            (
                together > shorter,
                $"Neighbouring kingdoms would overlap: a kingdom's width and height together ({together}) must be "
                    + $"at most the board's shorter side ({shorter})."
            ),
        ];
        return checks.FirstOrDefault(check => check.IsWrong).Problem;
    }
}
