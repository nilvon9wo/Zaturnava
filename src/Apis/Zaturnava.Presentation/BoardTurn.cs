using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation;

/// <summary>
/// How far the board is turned on screen, in quarter turns clockwise (0 to 3), about its centre. Each
/// seat sees its own corner bottom left by default (<see cref="HomeOf"/>), and may turn the board
/// either way, and back.
/// </summary>
public sealed record BoardTurn(int QuarterTurns)
{
    public const int Degrees = 90;
    private const int Full = 4;

    public static readonly BoardTurn None = new(0);

    public int Angle => this.QuarterTurns * Degrees;

    /// <summary>
    /// The turn that brings <paramref name="seat"/>'s home - its throne, else the middle of its pieces -
    /// closest to the bottom left; no turn for a seat with neither, or when watching.
    /// </summary>
    public static BoardTurn HomeOf(int? seat, [Required] GameViewDto view)
    {
        BoardFrame frame = BoardFrame.Of(view.Cells);
        (double X, double Y)? home = seat is { } held ? HomeCell(held, view) : null;
        return home is { } point
            ? new BoardTurn(
                Enumerable.Range(0, Full).MaxBy(turns => TowardsBottomLeft(Turned(Offset(point, frame), turns)))
            )
            : None;
    }

    public BoardTurn Clockwise() => new((this.QuarterTurns + 1) % Full);

    public BoardTurn Anticlockwise() => new((this.QuarterTurns + Full - 1) % Full);

    /// <summary>The SVG <c>transform</c> turning the board about the centre of <paramref name="frame"/>.</summary>
    public string Transform([Required] BoardFrame frame) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"rotate({this.Angle} {frame.Left + (frame.Width / 2)} {frame.Top + (frame.Height / 2)})"
        );

    /// <summary>The SVG <c>transform</c> turning a label at (x, y) back upright.</summary>
    public string Upright(double x, double y) =>
        string.Create(CultureInfo.InvariantCulture, $"rotate({-this.Angle} {x} {y})");

    private static (double X, double Y)? HomeCell(int seat, GameViewDto view)
    {
        CellDto? throne = view.Cells.FirstOrDefault(cell => cell.Throne == seat);
        ILookup<int, CellDto> cells = view.Cells.ToLookup(cell => cell.Id);
        CellDto[] occupied =
            [.. view.Pieces.Where(piece => piece.Owner == seat).SelectMany(piece => cells[piece.Cell])];
        return throne is not null
            ? (throne.X, BoardFrame.ScreenY(throne.Y))
            : occupied.Length > 0
                ? (occupied.Average(cell => cell.X), occupied.Average(cell => BoardFrame.ScreenY(cell.Y)))
                : null;
    }

    private static (double X, double Y) Offset((double X, double Y) point, BoardFrame frame) =>
        (point.X - (frame.Left + (frame.Width / 2)), point.Y - (frame.Top + (frame.Height / 2)));

    /// <summary>A point turned a quarter clockwise on screen (y down) <paramref name="turns"/> times.</summary>
    private static (double X, double Y) Turned((double X, double Y) point, int turns) =>
        Enumerable.Range(0, turns).Aggregate(point, (turned, _) => (-turned.Y, turned.X));

    private static double TowardsBottomLeft((double X, double Y) point) => point.Y - point.X;
}
