using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// The rectangle a board's cells fill, for drawing them in any coordinate system whose y grows
/// downwards (SVG, most screens). Each cell is a unit centred on its layout point, and the layout's
/// y grows upwards, so it is negated on screen: the first seat's edge ends up at the bottom.
/// </summary>
public sealed record BoardFrame(double Left, double Top, double Width, double Height)
{
    /// <summary>Half a cell: the margin from a cell's centre to its edge.</summary>
    public const double HalfCell = 0.5;

    public static readonly BoardFrame Empty = new(0, 0, 0, 0);

    /// <summary>An SVG <c>viewBox</c> showing exactly the board.</summary>
    public string ViewBox =>
        string.Create(CultureInfo.InvariantCulture, $"{this.Left} {this.Top} {this.Width} {this.Height}");

    public static BoardFrame Of([Required] ImmutableList<CellDto> cells) =>
        cells.IsEmpty
            ? Empty
            : new BoardFrame(
                cells.Min(cell => cell.X) - HalfCell,
                ScreenY(cells.Max(cell => cell.Y)) - HalfCell,
                cells.Max(cell => cell.X) - cells.Min(cell => cell.X) + 1,
                cells.Max(cell => cell.Y) - cells.Min(cell => cell.Y) + 1
            );

    /// <summary>Where a layout's <paramref name="y"/> is drawn.</summary>
    public static double ScreenY(double y) => -y;

    /// <summary>Whether a cell takes the board's darker shade, alternating like a chessboard's.</summary>
    public static bool IsDark([Required] CellDto cell) =>
        int.IsEvenInteger((int)Math.Floor(cell.X)) == int.IsEvenInteger((int)Math.Floor(cell.Y));
}
