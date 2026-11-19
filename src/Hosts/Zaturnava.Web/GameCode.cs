namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// The code a player gives others to find a game by in the lobby (the user's report of 2026-11-15, H2): the first
/// letters of its id, enough to tell it from every other game. The page has no address of its own for a game: on
/// itch.io, it runs in a frame that is never told the address of the page it is on.
/// </summary>
public static class GameCode
{
    /// <summary>How many letters of a game's id its code has.</summary>
    public const int Length = 8;

    /// <summary>The code of <paramref name="gameId"/>'s game: its id's first letters, or all of a short one.</summary>
    public static string Of(string gameId) => gameId.Length > Length ? gameId[..Length] : gameId;

    /// <summary>
    /// What a player sends others to join the game: the page's address, and the code to enter in its lobby.
    /// </summary>
    public static string InvitationTo(string gameId, string game) =>
        $"Join my game of {game}: open {DocsLinks.Page} and enter the game code {Of(gameId)} in the lobby.";
}
