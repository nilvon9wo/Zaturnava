using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>The traditional piece set (rules.md §3), its capture values (§5) and its names.</summary>
public static class ClassicPieces
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

    public static readonly ImmutableDictionary<PieceKindId, int> Points =
        ImmutableDictionary<PieceKindId, int>.Empty
            .Add(Pawn, 1)
            .Add(Boat, 2)
            .Add(Horse, 3)
            .Add(Elephant, 4)
            .Add(Raja, 5);

    public static readonly ImmutableDictionary<PieceKindId, string> TraditionalNames =
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
