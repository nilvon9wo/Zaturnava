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

    public static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// A line of the board as SVG path data, from one end to the other: a river waving across its course, bending
    /// one way then the other; any other kind straight.
    /// </summary>
    public static string PathOf(string kind, double fromX, double fromY, double toX, double toY) =>
        kind == RiverKind
            ? Waving(fromX, fromY, toX, toY)
            : $"M{Rounded(fromX)} {Rounded(fromY)}L{Rounded(toX)} {Rounded(toY)}";

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
