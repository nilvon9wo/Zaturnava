using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Hosting.Itch;

/// <summary>
/// The page as itch.io serves an HTML game: in a frame whose document comes from itch.io's own
/// content domains, never from the game's page address, so those domains are the origins.
/// </summary>
public sealed class ItchPageHost : IPageHosting
{
    /// <summary>One of the domains itch.io serves HTML games from.</summary>
    public const string ClassicOrigin = "https://html-classic.itch.zone";

    /// <summary>The other domain itch.io serves HTML games from.</summary>
    public const string Origin = "https://html.itch.zone";

    public ImmutableList<string> Origins { get; } = [ClassicOrigin, Origin];
}
