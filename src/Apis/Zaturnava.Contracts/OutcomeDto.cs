using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>How a game ended: the seats that won (none for a draw), and the whole story as text.</summary>
public sealed record OutcomeDto(ImmutableList<int> Winners, string Text);
