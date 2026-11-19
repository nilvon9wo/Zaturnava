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

    /// <summary>
    /// For a power moving another piece of its own side too, as castling moves the Rook: where that piece starts,
    /// its move to <see cref="PartnerTo"/> played after the piece's; none, for any other.
    /// </summary>
    public int? PartnerFrom { get; init; }

    /// <summary>Where the piece from <see cref="PartnerFrom"/> goes; none, if it does not.</summary>
    public int? PartnerTo { get; init; }

    /// <summary>
    /// For a chain of jumps: where the piece lands first, taking the first of <see cref="Taken"/>, before it jumps on
    /// to <see cref="To"/>; none, for a move made in one go.
    /// </summary>
    public int? Via { get; init; }
}
