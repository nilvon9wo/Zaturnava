using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// Cells of a drawing marked alike: <paramref name="Mark"/>, the style marking them ("move", "taken capture",
/// "partner", "trail" or "converted"), and <paramref name="Meaning"/>, what the mark means in this drawing.
/// </summary>
public sealed record CellMarkDto(string Mark, string Meaning, ImmutableList<int> Cells);
