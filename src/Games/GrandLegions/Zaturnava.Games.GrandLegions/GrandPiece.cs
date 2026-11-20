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
    /// The edition of the catalogue it came with: 0, the first; 1, the Checkers and Tafl pieces (2026-11-13); 4, the
    /// Berolina Pawn, the Manchu Chariot and the Courier (the same day); 11, the Evangelist (2026-11-19). A game
    /// dealt from an edition before it never holds it, so that it replays as it was dealt.
    /// </summary>
    public int Edition { get; init; }

    /// <summary>
    /// What it was worth before a repricing: by the edition it was repriced in, its value in a game of an edition
    /// before that; so that a game stored before scores and buys it as it did.
    /// </summary>
    public ImmutableSortedDictionary<int, int> EarlierValues { get; init; } = ImmutableSortedDictionary<int, int>.Empty;

    public PieceKind Kind => new(this.Id, this.Movement);

    /// <summary>
    /// What it is worth in a game of <paramref name="edition"/>: its earlier value, if it was repriced since.
    /// </summary>
    public int ValueAt(int edition) =>
        this.EarlierValues.Where(earlier => edition < earlier.Key).Select(earlier => (int?)earlier.Value)
            .FirstOrDefault() ?? this.Value;

    /// <summary>
    /// The same piece, worth <paramref name="value"/> in a game of an edition before <paramref name="edition"/>.
    /// </summary>
    public GrandPiece ValuedBefore(int edition, int value) =>
        this with { EarlierValues = this.EarlierValues.SetItem(edition, value) };

    /// <summary>Whether its own promotion is on the last row, as a Western or a Checkers piece's is.</summary>
    public bool PromotesOnLastRow => this.Tradition is Tradition.Western or Tradition.Checkers;

    /// <summary>The same piece, held back by <paramref name="restrictions"/>.</summary>
    public GrandPiece Restricted(params Restriction[] restrictions) => this with { Restrictions = [.. restrictions] };

    /// <summary>The same piece, which its own promotion may make any one of <paramref name="into"/>.</summary>
    public GrandPiece Promoting(params PieceKindId[] into) => this with { PromotesTo = [.. into] };

    /// <summary>The same piece, come with the catalogue's <paramref name="edition"/>.</summary>
    public GrandPiece Since(int edition) => this with { Edition = edition };

    /// <summary>
    /// The other games that play it too, under their own names, as Makruk's Ruea is the Western Rook: the catalogue
    /// holds it once, as its own <see cref="Tradition"/>'s (the user's word of 2026-11-20: pieces left out as
    /// redundant). None, for most.
    /// </summary>
    public IReadOnlyList<Tradition> AlsoIn { get; init; } = [];

    /// <summary>The same piece, played in <paramref name="games"/> too (<see cref="AlsoIn"/>).</summary>
    public GrandPiece Also(params Tradition[] games) => this with { AlsoIn = [.. games] };

    /// <summary>Whether it is a piece of <paramref name="family"/>'s game: its own, or one also playing it.</summary>
    public bool IsOf(string family) =>
        this.Tradition.ToString() == family || this.AlsoIn.Any(game => game.ToString() == family);
}
