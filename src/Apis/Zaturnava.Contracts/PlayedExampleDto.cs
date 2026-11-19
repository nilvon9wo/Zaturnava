using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A power at work, played: where the piece goes, and what it takes.</summary>
public sealed record PlayedExampleDto(int To, ImmutableList<int> Taken);
