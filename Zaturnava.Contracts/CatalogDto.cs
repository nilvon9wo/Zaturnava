using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>What the server can host: its rulesets, and the bots that can take a seat.</summary>
public sealed record CatalogDto(ImmutableList<RulesetDto> Rulesets, ImmutableList<BotDto> Bots);
