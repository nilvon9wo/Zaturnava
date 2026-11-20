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

    /// <summary>Which way it goes, at a glance (<see cref="MovementBearing"/>); none, unless told.</summary>
    public string? Bearing { get; init; }

    /// <summary>What sort of beast it is, among its class, for sorting pieces: "Horse"; none, if not told.</summary>
    public string? Kin { get; init; }

    /// <summary>Which game it comes from: "Shogi"; none, if not told.</summary>
    public string? Origin { get; init; }

    /// <summary>
    /// The other games that play it too, under their own names, as Makruk plays the Western Rook: none, unless told.
    /// </summary>
    public ImmutableList<string> AlsoIn { get; init; } = [];

    /// <summary>Whether it is of <paramref name="family"/>'s game: its <see cref="Origin"/>, or one else.</summary>
    public bool IsOf(string family) => this.Origin == family || this.AlsoIn.Contains(family);

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
    /// Whether it is a mercenary, which whoever holds it prisoner may hire: not a power, as nothing it helps its
    /// player do with it, but what it is (the user's word of 2026-11-19); the rules tell how hiring goes.
    /// </summary>
    public bool IsMercenary { get; init; }

    /// <summary>
    /// What may turn against its player, for people: going berserk, say. Not powers, as they help no one use it (the
    /// user's word of 2026-11-19); none, for most.
    /// </summary>
    public ImmutableList<string> Drawbacks { get; init; } = [];

    /// <summary>
    /// What its own promotion may make it, as a Western Pawn's makes it a Queen, Rook, Bishop or Knight: none, for a
    /// kind with no promotion of its own. Whether the rules play it yet is <see cref="IsPromotionPlayed"/>.
    /// </summary>
    public ImmutableList<PieceKindId> PromotesTo { get; init; } = [];

    /// <summary>Whether the rules play its own promotion (<see cref="PromotesTo"/>) yet.</summary>
    public bool IsPromotionPlayed { get; init; }

    /// <summary>Where its own promotion may be taken, for people: "on the last row"; none, if not told.</summary>
    public string? PromotesWhere { get; init; }

    /// <summary>
    /// What the game's rules hold it back by, which a lift may remove, for people: "Only its 3 × 3 palace".
    /// </summary>
    public ImmutableList<string> Restrictions { get; init; } = [];

    /// <summary>
    /// What its own move holds it back by, which nothing lifts, for people: "One colour" (the user's distinction of
    /// 2026-11-19, from <see cref="Restrictions"/>).
    /// </summary>
    public ImmutableList<string> Weaknesses { get; init; } = [];
}
