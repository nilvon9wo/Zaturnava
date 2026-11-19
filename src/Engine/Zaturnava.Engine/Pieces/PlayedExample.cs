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

    /// <summary>
    /// For a power moving another piece of its own side too, as castling moves the Rook: where that piece starts,
    /// its move to <see cref="PartnerTo"/> played after the piece's (the user's report of 2026-11-19: the castling
    /// drawn showed no Rook moving); none, for any other.
    /// </summary>
    public int? PartnerFrom { get; init; }

    /// <summary>Where the piece from <see cref="PartnerFrom"/> goes; none, if it does not.</summary>
    public int? PartnerTo { get; init; }

    /// <summary>
    /// For a chain of jumps: where the piece lands first, taking the first of <see cref="Taken"/>, before it jumps on
    /// to <see cref="To"/>, taking the rest (the user's report of 2026-11-19: the Checkers pieces drew no chain);
    /// none, for a move made in one go.
    /// </summary>
    public int? Via { get; init; }
}
