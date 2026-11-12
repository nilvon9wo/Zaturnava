using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// What is drawn of each palace (specification §5): its wall, every edge of its cells that no other of its cells
/// shares, each going counter-clockwise about the palace, so that its outside is to the right of its way (a client may
/// draw battlements out that way); and, once it holds its throne, a line from each other cell of it to the throne.
/// </summary>
internal static class PalaceLines
{
    /// <summary>A stretch of a palace's wall.</summary>
    public const string WallKind = "palace";

    /// <summary>A line from a cell of a palace to its throne.</summary>
    public const string LineKind = "palace-line";

    private const double Half = 0.5;

    public static ImmutableList<BoardLine> Of(GameState state, ICellNavigating grid) =>
    [
        .. (state.Palaces ?? []).OrderBy(palace => palace.Key.Value).SelectMany(palace => Lines(
            palace.Value,
            state.Thrones!.TryGetValue(palace.Key, out CellId throne) ? throne : null,
            grid
        )),
    ];

    private static IEnumerable<BoardLine> Lines(ImmutableList<CellId> palace, CellId? throne, ICellNavigating grid)
    {
        ImmutableHashSet<(double X, double Y)> centres =
            [.. palace.Select(grid.LayoutOf).Select(layout => (layout.X, layout.Y))];
        IEnumerable<BoardLine> walls = centres
            .OrderBy(centre => centre.Y)
            .ThenBy(centre => centre.X)
            .SelectMany(centre => Walls(centre, centres));
        return throne is { } held
            ? walls.Concat(ToThrone(centres, grid.LayoutOf(held)))
            : walls;
    }

    private static IEnumerable<BoardLine> Walls((double X, double Y) centre, ImmutableHashSet<(double X, double Y)> all)
    {
        (double x, double y) = centre;
        return new (double Dx, double Dy, BoardLine Wall)[]
            {
                (0, -1, new BoardLine(WallKind, x - Half, y - Half, x + Half, y - Half)),
                (1, 0, new BoardLine(WallKind, x + Half, y - Half, x + Half, y + Half)),
                (0, 1, new BoardLine(WallKind, x + Half, y + Half, x - Half, y + Half)),
                (-1, 0, new BoardLine(WallKind, x - Half, y + Half, x - Half, y - Half)),
            }
            .Where(side => !all.Contains((x + side.Dx, y + side.Dy)))
            .Select(side => side.Wall);
    }

    private static IEnumerable<BoardLine> ToThrone(ImmutableHashSet<(double X, double Y)> centres, CellLayout throne) =>
        centres
            .Where(centre => centre != (throne.X, throne.Y))
            .OrderBy(centre => centre.Y)
            .ThenBy(centre => centre.X)
            .Select(centre => new BoardLine(LineKind, centre.X, centre.Y, throne.X, throne.Y));
}
