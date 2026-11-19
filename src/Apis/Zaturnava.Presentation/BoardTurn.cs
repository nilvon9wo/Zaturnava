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
    /// The board as <paramref name="seat"/> sees it from its own side: turned so that its army's forward points up
    /// the screen, or as nearly as a quarter turn can; each army's corner lies to its left, so its own corner is
    /// bottom left. Not where its throne is: on a board of long kingdoms, a throne far along the back rank lies
    /// nearer another corner (playtest 2026-11-19, 26 × 26, where Black was turned to the top right). No turn when
    /// watching.
    /// </summary>
    public static BoardTurn HomeOf(int? seat, [Required] GameViewDto view) =>
        view.Seats.FirstOrDefault(each => each.Id == seat) is { } own
            ? new BoardTurn(
                Enumerable.Range(0, Full).MinBy(turns => OffUpright(new BoardTurn(turns).AngleOf(own.Forward)))
            )
            : None;

    public BoardTurn Clockwise() => new((this.QuarterTurns + 1) % Full);

    public BoardTurn Anticlockwise() => new((this.QuarterTurns + Full - 1) % Full);

    /// <summary>
    /// The rectangle <paramref name="frame"/> fills once turned about its centre: itself, turned a half turn or none;
    /// on its side, its width and height swapped, turned a quarter, as a board other than square shows.
    /// </summary>
    public BoardFrame Framing([Required] BoardFrame frame) =>
        int.IsEvenInteger(this.QuarterTurns)
            ? frame
            : new BoardFrame(
                frame.Left + ((frame.Width - frame.Height) / 2),
                frame.Top + ((frame.Height - frame.Width) / 2),
                frame.Height,
                frame.Width
            );

    /// <summary>The SVG <c>transform</c> turning the board about the centre of <paramref name="frame"/>.</summary>
    public string Transform([Required] BoardFrame frame) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"rotate({this.Angle} {frame.Left + (frame.Width / 2)} {frame.Top + (frame.Height / 2)})"
        );

    /// <summary>
    /// How far, in degrees clockwise on screen, something drawn with forward up is turned to show
    /// <paramref name="forward"/> as the board, turned so, shows it: 0 for forward up the board, unturned; 90 for
    /// forward to the right, or forward up, the board turned a quarter clockwise. From 0 to 359.
    /// </summary>
    public int AngleOf([Required] DirectionDto forward) =>
        ((int)Math.Round(Math.Atan2(forward.X, forward.Y) * 180 / Math.PI) + this.Angle + (Full * Degrees))
        % (Full * Degrees);

    /// <summary>The SVG <c>transform</c> turning a label at (x, y) back upright.</summary>
    public string Upright(double x, double y) =>
        string.Create(CultureInfo.InvariantCulture, $"rotate({-this.Angle} {x} {y})");

    /// <summary>
    /// Where on the board a label goes, <paramref name="inset"/> in from the corner of the cell centred at
    /// (<paramref name="x"/>, <paramref name="y"/>) that is top left on screen, whichever way the board is turned.
    /// </summary>
    public (double X, double Y) TopLeftOf(double x, double y, double inset)
    {
        double near = BoardFrame.HalfCell - inset;
        (double across, double down) = this.QuarterTurns switch
        {
            0 => (-near, -near),
            1 => (-near, near),
            2 => (near, near),
            _ => (near, -near),
        };
        return (x + across, y + down);
    }

    /// <summary>How far an angle on screen (0 to 359) is from straight up, either way.</summary>
    private static int OffUpright(int angle) => Math.Min(angle, (Full * Degrees) - angle);
}
