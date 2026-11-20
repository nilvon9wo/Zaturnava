using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// One thing about a game's terrain its creator may choose (<see cref="RulesetDto.TerrainAxes"/>): its name, the bits
/// of the terrain's number it sets, and each way to set them.
/// </summary>
public sealed record TerrainAxisDto(string Name, int Mask, ImmutableList<TerrainOptionDto> Options);
