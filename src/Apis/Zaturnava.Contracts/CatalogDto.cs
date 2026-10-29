using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// What the server can host: its rulesets, the bots that can take a seat, and, in development, the scenarios a
/// game can start from.
/// </summary>
public sealed record CatalogDto(ImmutableList<RulesetDto> Rulesets, ImmutableList<BotDto> Bots)
{
    public ImmutableList<ScenarioDto> Scenarios { get; init; } = [];
}
