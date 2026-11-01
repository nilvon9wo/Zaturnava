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

    /// <summary>
    /// Who the bearer of a player's token (<see cref="SignedInDto"/>) is; deleted, their account is forgotten.
    /// </summary>
    public const string Me = "api/players/me";

    /// <summary>The session a player's token is; deleted, the player is signed out, and the token is no more.</summary>
    public const string Session = Me + "/session";

    /// <summary>Where a player asks for a code to link another service with (<see cref="LinkCodeDto"/>).</summary>
    public const string LinkCode = Me + "/link-code";

    /// <summary>The services a player may sign in with (<see cref="Login"/>).</summary>
    public const string Logins = Me + "/logins";

    /// <summary>The links on a player's profile (<see cref="LinksRequest"/>); put, they replace those before.</summary>
    public const string Links = Me + "/links";

    /// <summary>Where a player asks for a token an AI may play for them with (<see cref="AiTokenDto"/>).</summary>
    public const string AiToken = Me + "/ai-token";

    /// <summary>The players by id: each one's profile, as anyone may see it.</summary>
    public const string Players = "api/players";

    /// <summary>A player's profile, as anyone may see it (<see cref="ProfileDto"/>).</summary>
    public static string Profile([Required] string playerId) => $"{Players}/{Uri.EscapeDataString(playerId)}";

    /// <summary>A player's login with a service; deleted, they may no longer sign in with it.</summary>
    public static string Login([Required] string provider) => $"{Logins}/{Uri.EscapeDataString(provider)}";

    /// <summary>
    /// Where a page sends a person, in a window of its own, to sign in with a service; once they have, that
    /// window posts a <see cref="SignedInDto"/> to the page, if its origin is one the server serves, and closes.
    /// With a <paramref name="link"/> code (<see cref="LinkCodeDto"/>), the service is linked to the player who asked
    /// for the code, who is signed in.
    /// </summary>
    public static string SignIn([Required] string provider, [Required] string origin, string? link = null) =>
        $"signin/{Uri.EscapeDataString(provider)}?origin={Uri.EscapeDataString(origin)}"
            + (link is null ? string.Empty : $"&link={Uri.EscapeDataString(link)}");

    public static string Game([Required] string gameId) => $"{Games}/{Uri.EscapeDataString(gameId)}";

    public static string Seat([Required] string gameId, int seat) =>
        $"{Game(gameId)}/seats/{seat.ToString(CultureInfo.InvariantCulture)}";

    public static string Actions([Required] string gameId) => $"{Game(gameId)}/actions";

    public static string Advance([Required] string gameId) => $"{Game(gameId)}/advance";

    /// <summary>Where a player waiting for others to take a game's open seats has bots take them instead.</summary>
    public static string Fill([Required] string gameId) => $"{Game(gameId)}/fill";

    /// <summary>Where a player has a bot take the seat of one away, for the rest of the game.</summary>
    public static string Replace([Required] string gameId) => $"{Game(gameId)}/replace";

    public static string History([Required] string gameId) => $"{Game(gameId)}/history";

    /// <summary>Where to say something at a game's table.</summary>
    public static string Chat([Required] string gameId) => $"{Game(gameId)}/chat";

    /// <summary>What was said at a game's table after message number <paramref name="after"/>.</summary>
    public static string Chat([Required] string gameId, int after) =>
        $"{Chat(gameId)}?after={after.ToString(CultureInfo.InvariantCulture)}";
}
