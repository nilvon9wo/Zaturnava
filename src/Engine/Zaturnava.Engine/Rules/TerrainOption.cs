namespace Net.NowhereAtAll.Zaturnava.Engine.Rules;

/// <summary>
/// One way to set a <see cref="TerrainAxis"/>: what it is called, and its bits of the terrain's number.
/// </summary>
public sealed record TerrainOption(string Name, int Value);
