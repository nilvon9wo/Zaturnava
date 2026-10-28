using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Where each request goes, relative to the server's base address, and how a seat's token is sent:
/// as <c>Authorization: Bearer {token}</c>.
/// </summary>
public static class GameRoutes
{
    public const string Catalog = "api/catalog";
    public const string Games = "api/games";
    public const string TokenScheme = "Bearer";

    public static string Game([Required] string gameId) => $"{Games}/{Uri.EscapeDataString(gameId)}";

    public static string Seat([Required] string gameId, int seat) =>
        $"{Game(gameId)}/seats/{seat.ToString(CultureInfo.InvariantCulture)}";

    public static string Actions([Required] string gameId) => $"{Game(gameId)}/actions";

    public static string Advance([Required] string gameId) => $"{Game(gameId)}/advance";
}
