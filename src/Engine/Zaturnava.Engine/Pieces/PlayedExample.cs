using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// A power at work, played (<see cref="MoveDiagram.Example"/>): where the piece goes, and what it takes.
/// </summary>
public sealed record PlayedExample(int To, ImmutableList<int> Taken)
{
    /// <summary>
    /// For a power answering an enemy's move, as <i>en passant</i> does: where the enemy came from, its move to the
    /// first of <see cref="Taken"/> played first (the user's report of 2026-11-19); none, for any other.
    /// </summary>
    public int? EnemyFrom { get; init; }
}
