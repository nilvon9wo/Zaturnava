using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A game as a lobby lists it: enough to choose one to watch or join.</summary>
public sealed record GameSummaryDto(string Id, string RulesetId, int Version, ImmutableList<int> OpenSeats);
