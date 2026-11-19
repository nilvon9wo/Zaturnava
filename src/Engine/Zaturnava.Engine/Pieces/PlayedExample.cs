using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// A power at work, played (<see cref="MoveDiagram.Example"/>): where the piece goes, and what it takes.
/// </summary>
public sealed record PlayedExample(int To, ImmutableList<int> Taken);
