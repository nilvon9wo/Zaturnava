namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>Another drawing of a piece, named for the switch that shows it.</summary>
public sealed record NamedDiagramDto(string Name, MoveDiagramDto Diagram);
