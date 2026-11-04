namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>A piece on the board: its kind's id and display name, its owner's seat, and its cell.</summary>
public sealed record PieceDto(int Id, string Kind, string Name, int Owner, int Cell)
{
    /// <summary>How it is marked, if by an emblem and letters; none, for a kind drawn as itself.</summary>
    public PieceMarkDto? Mark { get; init; }
}
