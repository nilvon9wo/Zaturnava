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

    /// <summary>The history of every finished game.</summary>
    public const string Histories = "api/history";
    public const string TokenScheme = "Bearer";

    /// <summary>The names of the services a person may sign in with here; none, if none is set up.</summary>
    public const string SignInProviders = "api/signin";

    /// <summary>Who the bearer of a player's token (<see cref="SignedInDto"/>) is.</summary>
    public const string Me = "api/players/me";

    /// <summary>
    /// Where a page sends a person, in a window of its own, to sign in with a service; once they have, that
    /// window posts a <see cref="SignedInDto"/> to the page, if its origin is one the server serves, and closes.
    /// </summary>
    public static string SignIn([Required] string provider, [Required] string origin) =>
        $"signin/{Uri.EscapeDataString(provider)}?origin={Uri.EscapeDataString(origin)}";

    public static string Game([Required] string gameId) => $"{Games}/{Uri.EscapeDataString(gameId)}";

    public static string Seat([Required] string gameId, int seat) =>
        $"{Game(gameId)}/seats/{seat.ToString(CultureInfo.InvariantCulture)}";

    public static string Actions([Required] string gameId) => $"{Game(gameId)}/actions";

    public static string Advance([Required] string gameId) => $"{Game(gameId)}/advance";

    public static string History([Required] string gameId) => $"{Game(gameId)}/history";

    /// <summary>Where to say something at a game's table.</summary>
    public static string Chat([Required] string gameId) => $"{Game(gameId)}/chat";

    /// <summary>What was said at a game's table after message number <paramref name="after"/>.</summary>
    public static string Chat([Required] string gameId, int after) =>
        $"{Chat(gameId)}?after={after.ToString(CultureInfo.InvariantCulture)}";
}
