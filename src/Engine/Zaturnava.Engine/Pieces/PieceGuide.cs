using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// What a player looking at a piece may want to know of its kind, beyond its name and worth: how it moves
/// (<paramref name="Kind"/>), what sort of piece it is (<see cref="Class"/>: Grand Legions's leader, infantry and
/// so on; none where the game has no such sorts), its powers - those its sort shares, and its own - and what holds
/// it back.
/// </summary>
public sealed record PieceGuide(PieceKind Kind)
{
    public string? Class { get; init; }

    /// <summary>Where it may move and capture, drawn: none, unless the ruleset draws it.</summary>
    public MoveDiagram Diagram { get; init; } = MoveDiagram.None;

    /// <summary>
    /// What it may do beyond its moves that every piece of its sort may, for people: every leader leads its army,
    /// every Pawn is promoted at the far edge.
    /// </summary>
    public ImmutableList<string> Powers { get; init; } = [];

    /// <summary>What it may do beyond its moves that is its kind's own, for people: "Castles with...".</summary>
    public ImmutableList<string> SpecialPowers { get; init; } = [];

    /// <summary>
    /// What its own promotion may make it, as a Western Pawn's makes it a Queen, Rook, Bishop or Knight: none, for a
    /// kind with no promotion of its own. Whether the rules play it yet is <see cref="IsPromotionPlayed"/>.
    /// </summary>
    public ImmutableList<PieceKindId> PromotesTo { get; init; } = [];

    /// <summary>Whether the rules play its own promotion (<see cref="PromotesTo"/>) yet.</summary>
    public bool IsPromotionPlayed { get; init; }

    /// <summary>What holds it back, for people: "Only its 3 × 3 palace".</summary>
    public ImmutableList<string> Restrictions { get; init; } = [];
}
