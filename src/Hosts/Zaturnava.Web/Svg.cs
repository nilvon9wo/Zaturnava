using System.Globalization;
using System.Text;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Numbers as SVG reads them, whatever the browser's culture; and the board's lines, drawn.</summary>
internal static class Svg
{
    /// <summary>The kind of line drawn as a river: waving, not straight.</summary>
    public const string RiverKind = "river";

    /// <summary>How long each of a river's bends is, in board units: four to a cell.</summary>
    private const double BendLength = 0.25;

    /// <summary>How far a bend's control point stands off the river's course: twice as far as the bend goes.</summary>
    private const double BendReach = 0.12;

    /// <summary>
    /// The kind of line drawn as a palace's wall: battlemented, its merlons standing out to the left of its way on the
    /// screen, which is the palace's outside, its walls going clockwise about it there.
    /// </summary>
    public const string PalaceKind = "palace";

    /// <summary>How long each merlon, and each gap between, is, in board units: four to a cell.</summary>
    private const double MerlonLength = 0.25;

    /// <summary>How far a merlon stands out from the wall.</summary>
    private const double MerlonHeight = 0.08;

    public static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A line of the board as SVG path data, from one end to the other: a river waving across its course, bending
    /// one way then the other; a palace's wall battlemented; any other kind straight.
    /// </summary>
    public static string PathOf(string kind, double fromX, double fromY, double toX, double toY) =>
        kind switch
        {
            RiverKind => Waving(fromX, fromY, toX, toY),
            PalaceKind => Battlemented(fromX, fromY, toX, toY),
            _ => $"M{Rounded(fromX)} {Rounded(fromY)}L{Rounded(toX)} {Rounded(toY)}",
        };

    /// <summary>
    /// The wall as gaps and merlons in turn, a gap first, each a square wave's step out to the left of its way (as
    /// the screen's y runs down) and back, ending on the wall's line.
    /// </summary>
    private static string Battlemented(double fromX, double fromY, double toX, double toY)
    {
        double alongX = toX - fromX;
        double alongY = toY - fromY;
        double length = Math.Sqrt((alongX * alongX) + (alongY * alongY));
        int steps = Math.Max(1, (int)Math.Round(length / MerlonLength));
        double outX = -alongY / length * MerlonHeight;
        double outY = alongX / length * MerlonHeight;
        StringBuilder path = new($"M{Rounded(fromX)} {Rounded(fromY)}");
        for (int step = 0; step < steps; step++)
        {
            double rise = step % 2 == 0 ? 0 : 1;
            _ = path.Append(At((double)step / steps, rise)).Append(At((step + 1.0) / steps, rise));
        }

        return path.Append(At(1, 0)).ToString();

        string At(double way, double rise) =>
            $"L{Rounded(fromX + (alongX * way) + (rise * outX))} {Rounded(fromY + (alongY * way) + (rise * outY))}";
    }

    private static string Waving(double fromX, double fromY, double toX, double toY)
    {
        double alongX = toX - fromX;
        double alongY = toY - fromY;
        double length = Math.Sqrt((alongX * alongX) + (alongY * alongY));
        int bends = Math.Max(1, (int)Math.Round(length / BendLength));
        double acrossX = -alongY / length * BendReach;
        double acrossY = alongX / length * BendReach;
        StringBuilder path = new($"M{Rounded(fromX)} {Rounded(fromY)}");
        for (int bend = 0; bend < bends; bend++)
        {
            double side = bend % 2 == 0 ? 1 : -1;
            double middle = (bend + 0.5) / bends;
            double end = (bend + 1.0) / bends;
            _ = path.Append(
                CultureInfo.InvariantCulture,
                $"Q{Rounded(fromX + (alongX * middle) + (side * acrossX))} "
                    + $"{Rounded(fromY + (alongY * middle) + (side * acrossY))} "
                    + $"{Rounded(fromX + (alongX * end))} {Rounded(fromY + (alongY * end))}"
            );
        }

        return path.ToString();
    }

    private static string Rounded(double value) => Number(Math.Round(value, 3));
}
