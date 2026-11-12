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

    /// <summary>Which way it goes, at a glance: "Forward", "Orthogonal", "Diagonal", "Every way" or "Other".</summary>
    public string? Bearing { get; init; }

    /// <summary>What sort of beast it is, among its class: "Horse"; none, if not told.</summary>
    public string? Kin { get; init; }

    /// <summary>Which game it comes from: "Shogi"; none, if not told.</summary>
    public string? Origin { get; init; }

    public ImmutableList<string> Moves { get; init; } = [];

    public ImmutableList<string> Captures { get; init; } = [];

    /// <summary>What every piece of its sort may do beyond its moves: every leader leads its army.</summary>
    public ImmutableList<string> Powers { get; init; } = [];

    /// <summary>What it may do beyond its moves that is its kind's own: castling, say.</summary>
    public ImmutableList<string> SpecialPowers { get; init; } = [];

    /// <summary>
    /// The names of what its own promotion may make it, as a Western Pawn's makes it a Queen: none, for a kind
    /// with none. <see cref="IsPromotionPlayed"/> says whether the game plays it yet.
    /// </summary>
    public ImmutableList<string> PromotesTo { get; init; } = [];

    public bool IsPromotionPlayed { get; init; }

    /// <summary>Where its own promotion may be taken: "on the last row"; none, if not told.</summary>
    public string? PromotesWhere { get; init; }

    public ImmutableList<string> Restrictions { get; init; } = [];

    public MoveDiagramDto? Diagram { get; init; }

    /// <summary>How it is marked, if by an emblem and letters; none, for a kind drawn as itself.</summary>
    public PieceMarkDto? Mark { get; init; }
}
