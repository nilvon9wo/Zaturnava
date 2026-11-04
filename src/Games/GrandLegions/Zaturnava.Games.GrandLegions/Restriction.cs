namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A limit on a piece, told for people, and what lifting it costs; none, if it cannot be lifted. An area restriction
/// says which terrain it is about (<see cref="Terrain"/>): a piece held by one is played only where there is such.
/// </summary>
public sealed record Restriction(RestrictionKind Kind, string Description, int? Lift = null)
{
    public Terrain? Terrain { get; init; }
}
