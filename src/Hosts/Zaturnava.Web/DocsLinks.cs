namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Where the game's documents are, for the page to link to: in its public repository.</summary>
public static class DocsLinks
{
    private const string Docs = "https://github.com/nilvon9wo/Zaturnava/blob/main/docs/";

    /// <summary>The page itself, where people come to play: the address a player sends others to join a game.</summary>
    public const string Page = "https://nilvon9wo.itch.io/zaturnava";

    /// <summary>Every rule, in full.</summary>
    public const string Rules = Docs + "rules.md";

    /// <summary>
    /// How a mercenary is hired, for its card to link to rather than tell in full (the user's word of 2026-11-19).
    /// </summary>
    public const string Mercenaries = Docs + "grand/specification.md#65-mercenaries";

    /// <summary>For developers: playing with an AI of one's own, and training it.</summary>
    public const string Bots = Docs + "bots/README.md";

    /// <summary>What is kept about the people who play, and how to have it deleted.</summary>
    public const string Privacy = Docs + "privacy.md";
}
