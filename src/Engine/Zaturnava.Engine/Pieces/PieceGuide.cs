using System.Collections.Immutable;

namespace Net.NowhereAtAll.Zaturnava.Engine.Pieces;

/// <summary>
/// What a player looking at a piece may want to know of its kind, beyond its name and worth: how it moves
/// (<paramref name="Kind"/>), what sort of piece it is (<see cref="Class"/>: Grand Legions's leader, infantry and
/// so on; none where the game has no such sorts), its special powers, and what holds it back.
/// </summary>
public sealed record PieceGuide(PieceKind Kind)
{
    public string? Class { get; init; }

    /// <summary>Where it may move and capture, drawn: none, unless the ruleset draws it.</summary>
    public MoveDiagram Diagram { get; init; } = MoveDiagram.None;

    /// <summary>What it may do beyond its moves, for people: "Castles with a Rook of its own...".</summary>
    public ImmutableList<string> Powers { get; init; } = [];

    /// <summary>What holds it back, for people: "Only its 3 × 3 palace".</summary>
    public ImmutableList<string> Restrictions { get; init; } = [];
}
