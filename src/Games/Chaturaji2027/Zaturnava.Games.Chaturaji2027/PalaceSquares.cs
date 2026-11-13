using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Where a palace may stand in a kingdom, as seat 0 sees it (specification §5): a 3 × 3 of its squares, about a centre
/// whose 3 × 3 is all in the kingdom; and the one that holds a throne, wherever the throne is first set, so that
/// whoever works it out - the draft, or the program dealing an army - finds the same.
/// </summary>
public static class PalaceSquares
{
    /// <summary>A palace's width, and its depth.</summary>
    private const int Width = 3;

    private static readonly PieceKindId Square = new("kingdom-square");

    /// <summary>Every square of <paramref name="kingdom"/> a palace may stand about: none, if it has no room.</summary>
    public static ImmutableList<ArmySquare> Centres([Required] ImmutableList<ArmySquare> kingdom)
    {
        ImmutableHashSet<ArmySquare> all = [.. kingdom];
        return [.. kingdom.Where(square => Window(square).All(all.Contains))];
    }

    /// <summary>
    /// Where a palace may stand in <paramref name="kingdom"/>, if it may stand partly outside one too small for it
    /// (specification §5, Stated): about a square of its, as <see cref="Centres"/> says, if any; else about one of the
    /// row next to the board's edge, its back row along that edge, so long as most of it is in the kingdom (with a
    /// 4 × 2 kingdom, 6 squares in, 3 out), none of it beside the kingdom's sides, and none of it in another army's
    /// kingdom, each seat's placed on a board <paramref name="width"/> squares across and <paramref name="height"/>
    /// up, as the pinwheel places them: where a palace may stand for every seat, each seeing the board from its own
    /// corner (<see cref="Pinwheel.FrameOf"/>), so that every army has the same choice.
    /// </summary>
    public static ImmutableList<ArmySquare> EdgeCentres(
        [Required] ImmutableList<ArmySquare> kingdom,
        int width,
        int height
    )
    {
        ImmutableList<ArmyPiece> squares =
            [.. kingdom.Select(square => new ArmyPiece(Square, square.Column, square.Row))];
        ImmutableHashSet<ArmySquare> OthersOn(int across, int up) =>
        [
            .. Pinwheel
                .EachOwn(across, up, [[], .. Enumerable.Repeat(squares, Pinwheel.SeatCount - 1)])
                .Select(placed => new ArmySquare(placed.Column, placed.Row)),
        ];
        ImmutableHashSet<ArmySquare> others = OthersOn(width, height).Union(OthersOn(height, width));
        return EdgeCentres(kingdom, square => !others.Contains(square));
    }

    /// <summary>
    /// As <see cref="EdgeCentres(ImmutableList{ArmySquare}, int, int)"/> says, a square outside the kingdom free if
    /// <paramref name="isFree"/> says so.
    /// </summary>
    public static ImmutableList<ArmySquare> EdgeCentres(
        [Required] ImmutableList<ArmySquare> kingdom,
        [Required] Func<ArmySquare, bool> isFree
    )
    {
        ImmutableList<ArmySquare> inside = Centres(kingdom);
        ImmutableHashSet<ArmySquare> all = [.. kingdom];
        int least = kingdom.Select(square => square.Column).DefaultIfEmpty().Min();
        int most = kingdom.Select(square => square.Column).DefaultIfEmpty().Max();
        return inside.IsEmpty
            ?
            [
                .. kingdom
                    .Where(square => square.Row == 1 && square.Column > least && square.Column < most)
                    .Where(square => Window(square).Count(all.Contains) * 2 > Width * Width)
                    .Where(square => Window(square).Where(each => !all.Contains(each)).All(isFree)),
            ]
            : inside;
    }

    /// <summary>
    /// The centre of the palace holding <paramref name="throne"/>: of <paramref name="centres"/>, the nearest it
    /// (Proposed), the lowest, then leftmost, of those as near.
    /// </summary>
    public static ArmySquare CentreAbout([Required] ImmutableList<ArmySquare> centres, [Required] ArmySquare throne) =>
        centres
            .OrderBy(centre => Math.Max(Math.Abs(centre.Column - throne.Column), Math.Abs(centre.Row - throne.Row)))
            .ThenBy(centre => centre.Row)
            .ThenBy(centre => centre.Column)
            .First();

    /// <summary>The nine squares of the palace about <paramref name="centre"/>, row by row from the back.</summary>
    public static ImmutableList<ArmySquare> Window([Required] ArmySquare centre) =>
    [
        .. Enumerable.Range(-1, 3).SelectMany(row =>
            Enumerable.Range(-1, 3).Select(column => new ArmySquare(centre.Column + column, centre.Row + row))
        ),
    ];
}
