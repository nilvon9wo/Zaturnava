using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Numbers as SVG reads them, whatever the browser's culture.</summary>
internal static class Svg
{
    public static string Number(double value) => value.ToString(CultureInfo.InvariantCulture);
}
