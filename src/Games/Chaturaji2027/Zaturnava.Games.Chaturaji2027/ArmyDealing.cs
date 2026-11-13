namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// The armies to deal a game by <paramref name="Seed"/>: each seat's the same, if <paramref name="AreAlike"/>; on the
/// terrain dealt it (<paramref name="Terrain"/>, <see cref="Chaturaji2027Variant.TerrainDealer"/>); with the pieces a
/// palace holds, in their palace, if <paramref name="HasPalacePieces"/>, which a game dealt before they could be, or
/// without palaces, never is.
/// </summary>
public sealed record ArmyDealing(int Seed, bool AreAlike, int Terrain, bool HasPalacePieces)
{
    /// <summary>
    /// Whether a kingdom too small for a palace has one standing partly outside it
    /// (<see cref="PalaceSquares.EdgeCentres(System.Collections.Immutable.ImmutableList{ArmySquare}, int)"/>): never,
    /// in a game dealt before it could, or without palaces.
    /// </summary>
    public bool HasEdgePalaces { get; init; }
}
