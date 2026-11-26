using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A piece on the board: its kind's id and display name, its owner's seat, and its cell.</summary>
public sealed record PieceDto(int Id, string Kind, string Name, int Owner, int Cell)
{
    /// <summary>How it is marked, if by an emblem and letters; none, for a kind drawn as itself.</summary>
    public PieceMarkDto? Mark { get; init; }

    /// <summary>
    /// How it moves drawn where it stands, if where it stands changes that, as a palace it stands in does a Guard's;
    /// else none, its kind's drawing telling all.
    /// </summary>
    public MoveDiagramDto? Diagram { get; init; }

    /// <summary>
    /// Whether it is a mercenary, which whoever takes it may hire: by its kind, or as what a mercenary was promoted
    /// into, whatever its kind now.
    /// </summary>
    public bool IsMercenary { get; init; }

    /// <summary>
    /// The restrictions bought off for it (specification §7), each "movement" or "area"; none, if none was.
    /// </summary>
    public ImmutableList<string> Lifted { get; init; } = [];
}
