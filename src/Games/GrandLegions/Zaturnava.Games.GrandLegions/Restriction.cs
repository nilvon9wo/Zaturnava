namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A limit on a piece, told for people, and what lifting it costs; none, if it cannot be lifted. An area restriction
/// says which terrain it is about (<see cref="Terrain"/>), and whether it keeps the piece within it
/// (<see cref="IsConfining"/>): a piece confined to a palace is played only where there is one.
/// </summary>
public sealed record Restriction(RestrictionKind Kind, string Description, int? Lift = null)
{
    public Terrain? Terrain { get; init; }

    /// <summary>Whether it keeps the piece within its <see cref="Terrain"/>, as a palace keeps a Guard.</summary>
    public bool IsConfining { get; init; }

    /// <summary>
    /// Whether it keeps the piece to the colour of square it starts on, as a Bishop's move does; a Threeleaper's
    /// share of the board, one cell in nine, has squares of both colours.
    /// </summary>
    public bool IsColourBinding { get; init; }
}
