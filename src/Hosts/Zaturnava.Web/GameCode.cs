namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// The code a player gives others to find a game by in the lobby (the user's report of 2026-11-15, H2): the first
/// letters of its id, enough to tell it from every other game. On itch.io the page runs in a frame that is never told
/// the address of the page it is on, so no link can open a game there; the game server serves the page too, at
/// <see cref="PagePath"/>, where a link can (Stated 2026-11-19, OQ9: itch.io is for being found).
/// </summary>
public static class GameCode
{
    /// <summary>How many letters of a game's id its code has.</summary>
    public const int Length = 8;

    /// <summary>Where on the game server the page is served.</summary>
    public const string PagePath = "play/";

    /// <summary>The name of the part of the page's address giving the code of the game to find.</summary>
    public const string Parameter = "game";

    /// <summary>The code of <paramref name="gameId"/>'s game: its id's first letters, or all of a short one.</summary>
    public static string Of(string gameId) => gameId.Length > Length ? gameId[..Length] : gameId;

    /// <summary>
    /// What a player sends others to join the game: a link opening the page on <paramref name="server"/> at the game;
    /// and, for those who come by itch.io, its address and the code to enter in its lobby.
    /// </summary>
    public static string InvitationTo(string gameId, string game, Uri server) =>
        $"Join my game of {game}: {LinkTo(gameId, server)} (or open {DocsLinks.Page} and enter the game code "
        + $"{Of(gameId)} in the lobby).";

    /// <summary>The page on <paramref name="server"/>, opening at <paramref name="gameId"/>'s game.</summary>
    public static Uri LinkTo(string gameId, Uri server) => new(server, $"{PagePath}?{Parameter}={Of(gameId)}");

    /// <summary>
    /// The code of the game the page's <paramref name="address"/> opens at, as a link from an invitation gives it;
    /// none, if it gives none.
    /// </summary>
    public static string? InvitedBy(string address) =>
        new Uri(address).Query.TrimStart('?')
            .Split('&')
            .Select(part => part.Split('=', 2))
            .Where(pair => pair is [Parameter, { Length: > 0 }])
            .Select(pair => Uri.UnescapeDataString(pair[1]))
            .FirstOrDefault();
}
