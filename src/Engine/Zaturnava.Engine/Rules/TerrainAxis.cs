using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// One thing about a game's terrain its creator may choose rather than leave to chance (the user's request of
/// 2026-11-19): how its rivers part the board, say. It is the bits of <see cref="Mask"/> in the terrain's number
/// (<see cref="GameState.Terrain"/>), and each of <see cref="Options"/> one way to set them.
/// </summary>
public sealed record TerrainAxis(string Name, int Mask, ImmutableList<TerrainOption> Options);
