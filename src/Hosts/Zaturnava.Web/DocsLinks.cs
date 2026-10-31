namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>Where the game's documents are, for the page to link to: in its public repository.</summary>
public static class DocsLinks
{
    private const string Docs = "https://github.com/nilvon9wo/Zaturnava/blob/main/docs/";

    /// <summary>Every rule, in full.</summary>
    public const string Rules = Docs + "rules.md";

    /// <summary>For developers: playing with an AI of one's own, and training it.</summary>
    public const string Bots = Docs + "bots/README.md";

    /// <summary>What is kept about the people who play, and how to have it deleted.</summary>
    public const string Privacy = Docs + "privacy.md";
}
