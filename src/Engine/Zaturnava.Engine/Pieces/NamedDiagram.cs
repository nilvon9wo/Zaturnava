namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// Another drawing of a piece (<see cref="MoveDiagram.Alternates"/>), named for the switch that shows it.
/// </summary>
public sealed record NamedDiagram(string Name, MoveDiagram Diagram);
