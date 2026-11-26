using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.GrandLegionsPieces;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// How a piece moves once a restriction of its is bought off (specification §7; the designer's word, 2026-11-26, on
/// open question Q-L: "All these lifts sound good to me"). A restriction on where a piece goes is otherwise the board's
/// to lift (<see cref="PalaceConfinement"/>, <see cref="RiverCrossing"/>).
/// </summary>
public static class LiftedMoves
{
    /// <summary>Each kind's movement, its movement restriction bought off, in place of its own.</summary>
    public static ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>> Movement { get; } =
        ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>>.Empty
            // its leg no longer blocks it: it leaps as the Knight, the Zebra, the Alfil
            .Add(Horse, [MovementRule.Leap(DirectionSets.Knight, 1)])
            .Add(JanggiElephant, [MovementRule.Leap(DirectionSets.Leaper(3, 2), 1)])
            .Add(XiangqiElephant, [MovementRule.Leap(DirectionSets.Leaper(2, 2), 1)])
            // leaps on past an occupied landing
            .Add(Knightrider, [MovementRule.Slide(DirectionSets.Knight) with { IsLeaping = true }])
            // steps back too: forward, sideways and back
            .Add(XiangqiSoldier, [MovementRule.Step(DirectionSets.Orthogonal)])
            .Add(JanggiSoldier, [MovementRule.Step(DirectionSets.Orthogonal)])
            // takes straight ahead too
            .Add(WesternPawn, PawnTakingAhead)
            .Add(MakrukPawn, PawnTakingAhead)
            // takes diagonally ahead too
            .Add(
                BerolinaPawn,
                [
                    MovementRule.Step(DirectionSets.ForwardDiagonals),
                    MovementRule.Step(DirectionSets.Forward).CaptureOnly(),
                ]
            )
            // slides on through its own army's pieces, stopping on or before another's
            .Add(Lance, [MovementRule.Slide(DirectionSets.Forward) with { PassesOwn = true }])
            // may hop a Cannon, and take one
            .Add(JanggiCannon, [MovementRule.Hop(DirectionSets.Orthogonal, int.MaxValue)])
            // also steps one square any way, as the King, without hopping
            .Add(Grasshopper, [MovementRule.Hop(DirectionSets.Any, 1), MovementRule.Step(DirectionSets.Any)]);

    /// <summary>
    /// The moves a kind gains once its area restriction is bought off, beyond what the board then lets it do: the
    /// Janggi Soldier steps diagonally forward anywhere, not only along palace lines.
    /// </summary>
    public static ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>> AreaAdds { get; } =
        ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>>.Empty
            .Add(JanggiSoldier, [MovementRule.Step(DirectionSets.ForwardDiagonals)]);

    private static ImmutableList<MovementRule> PawnTakingAhead =>
    [
        MovementRule.Step(DirectionSets.Forward),
        MovementRule.Step(DirectionSets.ForwardDiagonals).CaptureOnly(),
    ];
}
