using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Web;

/// <summary>
/// What the lobby last had from the server, kept while the page stays open, so that coming back to the lobby shows it
/// at once, and only then asks the server again (the user's report of 2026-11-25: returning to the lobby took
/// forever).
/// </summary>
public sealed class LobbyMemory
{
    /// <summary>The games and bots the server offers, as last asked; none, until first asked.</summary>
    public CatalogDto? Catalog { get; set; }

    /// <summary>The page of games last listed, and what was asked for it; none, until first listed.</summary>
    public (GameListQuery Asked, GamePageDto Page)? Listed { get; set; }

    /// <summary>Each game's leaderboards, people's and bots', as last asked, by the game.</summary>
    public ImmutableDictionary<string, Standings> Standings { get; set; } = [];
}

/// <summary>A game's leaderboards: its people's and its bots'.</summary>
public sealed record Standings(ImmutableList<StandingDto> People, ImmutableList<BotStandingDto> Bots);
