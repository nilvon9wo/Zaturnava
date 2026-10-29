using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// How a game ended: the seats that won (none for a draw), the kind of ending as the ruleset's code
/// (in Chaturaji 2027: <c>one-team-left</c>, <c>only-rajas-left</c>, <c>agreement</c>, <c>team-gave-up</c>
/// or <c>idle</c>), and the whole story as text.
/// </summary>
public sealed record OutcomeDto(ImmutableList<int> Winners, string Ending, string Text);
