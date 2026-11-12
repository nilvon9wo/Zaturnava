using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>The chaturaji piece set (rules.md §3), its capture values (§5) and its names.</summary>
public static class Chaturaji2027Pieces
{
    public static readonly PieceKindId Raja = new("raja");
    public static readonly PieceKindId Elephant = new("elephant");
    public static readonly PieceKindId Horse = new("horse");
    public static readonly PieceKindId Boat = new("boat");
    public static readonly PieceKindId Pawn = new("pawn");

    public static readonly ImmutableDictionary<PieceKindId, PieceKind> Kinds =
        ImmutableDictionary<PieceKindId, PieceKind>.Empty
            .Add(Raja, new PieceKind(Raja, RajaMovement()))
            .Add(Elephant, new PieceKind(Elephant, [MovementRule.Slide(DirectionSets.Orthogonal)]))
            .Add(Horse, new PieceKind(Horse, [MovementRule.Leap(DirectionSets.Knight, 1)]))
            .Add(Boat, new PieceKind(Boat, [MovementRule.Leap(DirectionSets.Diagonal, 2)]))
            .Add(Pawn, new PieceKind(Pawn, PawnMovement()));

    /// <summary>What taking each piece scores, as its table has it (<c>Values/chaturaji-2027.json</c>).</summary>
    public static readonly ImmutableDictionary<PieceKindId, int> Points =
        PieceValues.Of(typeof(Chaturaji2027Pieces).Assembly, Chaturaji2027Rules.RulesetId);

    /// <summary>
    /// What each piece is worth keeping on the board, to one weighing an exchange, as its table has it
    /// (<c>Values/chaturaji-2027-keeping.json</c>; Proposed, the user's ruling of 2026-11-12, that a Pawn, which may
    /// yet be promoted, matters more than a Boat, which reaches few cells, whatever taking each scores): a Pawn as
    /// much as a Horse; every other piece what taking it scores.
    /// </summary>
    public static readonly ImmutableDictionary<PieceKindId, int> Keeping =
        PieceValues.Of(typeof(Chaturaji2027Pieces).Assembly, Chaturaji2027Rules.RulesetId + "-keeping");

    public static readonly ImmutableDictionary<PieceKindId, string> ChaturajiNames =
        ImmutableDictionary<PieceKindId, string>.Empty
            .Add(Raja, "Raja")
            .Add(Elephant, "Elephant")
            .Add(Horse, "Horse")
            .Add(Boat, "Boat")
            .Add(Pawn, "Pawn");

    private static ImmutableList<MovementRule> RajaMovement() =>
        [MovementRule.Step(DirectionSets.Orthogonal), MovementRule.Step(DirectionSets.Diagonal)];

    private static ImmutableList<MovementRule> PawnMovement() =>
    [
        MovementRule.Step(DirectionSets.Forward).MoveOnly(),
        MovementRule.Step(DirectionSets.ForwardDiagonals).CaptureOnly(),
    ];
}
