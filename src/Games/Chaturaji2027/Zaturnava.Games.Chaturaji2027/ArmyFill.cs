using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// An army to fill at random, as drafted so far: on <paramref name="Terrain"/>, with <paramref name="Left"/> to spend,
/// its pieces (<paramref name="Army"/>) and its <paramref name="Throne"/>, if set, as seat 0 would have them; drawn by
/// <paramref name="Random"/>, so that the game replays alike.
/// </summary>
public sealed record ArmyFill(
    int Terrain,
    int Left,
    ImmutableList<ArmyPiece> Army,
    ArmySquare? Throne,
    Random Random
)
{
    /// <summary>The squares of its palace, if placed, as seat 0 would have them; none, if not yet or never.</summary>
    public ImmutableList<ArmySquare>? Palace { get; init; }

    /// <summary>
    /// Whether it may be filled with the pieces a palace holds (<see cref="GameState.HasPalacePieces"/>): never, in a
    /// game dealt before it could be.
    /// </summary>
    public bool HasPalacePieces { get; init; }
}
