using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// A piece of the catalogue (pieces.md): its key, its name for people, its class and tradition, its value (its
/// price in the draft, and what capturing it scores), and how it moves, with its restrictions as they stand
/// before any is lifted, and what it may be promoted into by its own promotion.
/// </summary>
public sealed record GrandPiece(
    PieceKindId Id,
    string Name,
    PieceClass Class,
    Tradition Tradition,
    int Value,
    ImmutableList<MovementRule> Movement
)
{
    public IReadOnlyList<Restriction> Restrictions { get; init; } = [];

    public IReadOnlyList<PieceKindId> PromotesTo { get; init; } = [];

    /// <summary>
    /// The edition of the catalogue it came with: 0, the first; 1, the Checkers and Tafl pieces (2026-11-13). A game
    /// dealt from an edition before it never holds it, so that it replays as it was dealt.
    /// </summary>
    public int Edition { get; init; }

    public PieceKind Kind => new(this.Id, this.Movement);

    /// <summary>Whether its own promotion is on the last row, as a Western or a Checkers piece's is.</summary>
    public bool PromotesOnLastRow => this.Tradition is Tradition.Western or Tradition.Checkers;

    /// <summary>The same piece, held back by <paramref name="restrictions"/>.</summary>
    public GrandPiece Restricted(params Restriction[] restrictions) => this with { Restrictions = [.. restrictions] };

    /// <summary>The same piece, which its own promotion may make any one of <paramref name="into"/>.</summary>
    public GrandPiece Promoting(params PieceKindId[] into) => this with { PromotesTo = [.. into] };

    /// <summary>The same piece, come with the catalogue's <paramref name="edition"/>.</summary>
    public GrandPiece Since(int edition) => this with { Edition = edition };
}
