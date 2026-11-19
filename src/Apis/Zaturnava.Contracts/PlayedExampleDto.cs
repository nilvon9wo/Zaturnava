using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A power at work, played: where the piece goes, and what it takes.</summary>
public sealed record PlayedExampleDto(int To, ImmutableList<int> Taken)
{
    /// <summary>
    /// For a power answering an enemy's move: where the enemy came from, its move to the first of
    /// <see cref="Taken"/> played first; none, for any other.
    /// </summary>
    public int? EnemyFrom { get; init; }
}
