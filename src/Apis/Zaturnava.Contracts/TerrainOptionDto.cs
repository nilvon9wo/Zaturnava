namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// One way to set a <see cref="TerrainAxisDto"/>: what it is called, and its bits of the terrain's number.
/// </summary>
public sealed record TerrainOptionDto(string Name, int Value);
