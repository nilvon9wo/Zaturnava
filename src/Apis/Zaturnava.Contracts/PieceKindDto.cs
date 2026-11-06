using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Contracts;

/// <summary>
/// A kind of piece as a player looking at one is told of it: its id, its name, and what taking one scores; its
/// class, if the game sorts its pieces into classes; and, for people, how it moves, how it captures, its special
/// powers, and what holds it back. Its moves and captures are drawn as well, if the game draws them
/// (<see cref="Diagram"/>).
/// </summary>
public sealed record PieceKindDto(string Kind, string Name, int Value)
{
    public string? Class { get; init; }

    public ImmutableList<string> Moves { get; init; } = [];

    public ImmutableList<string> Captures { get; init; } = [];

    public ImmutableList<string> Powers { get; init; } = [];

    public ImmutableList<string> Restrictions { get; init; } = [];

    public MoveDiagramDto? Diagram { get; init; }

    /// <summary>How it is marked, if by an emblem and letters; none, for a kind drawn as itself.</summary>
    public PieceMarkDto? Mark { get; init; }
}
