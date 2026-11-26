using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// How a piece whose restrictions were bought off moves, drawn as <see cref="MoveDiagrams"/> draws a kind, for its card
/// to show (2026-11-26: "There should be a toggle on the cards to also apply the lifts"): its movement lifted, as the
/// variant says it then moves, by its river or in its palace as ever; its area lifted, on an open board, nothing to
/// hold it back, with any moves that adds.
/// </summary>
internal sealed class LiftedDiagrams(Chaturaji2027Variant variant)
{
    private readonly ImmutableHashSet<PieceKindId> _freeable = [.. variant.MovementLifted.Keys];
    private readonly MoveDiagrams _freed = DiagramsOf(variant, variant.MovementLifted, []);
    private readonly MoveDiagrams _loosed = DiagramsOf(variant, [], variant.AreaLiftAdds);
    private readonly MoveDiagrams _both = DiagramsOf(variant, variant.MovementLifted, variant.AreaLiftAdds);

    /// <summary>
    /// The kind drawn with the lifts said bought (a movement lift only where the variant says how it then moves); none,
    /// with none.
    /// </summary>
    public MoveDiagram? Of(PieceKindId kind, bool isFreed, bool isLoosed) =>
        (isFreed && this._freeable.Contains(kind), isLoosed) switch
        {
            (false, false) => null,
            (true, false) => this._freed.Of(kind),
            (false, true) => this._loosed.Open(kind),
            _ => this._both.Open(kind),
        };

    private static MoveDiagrams DiagramsOf(
        Chaturaji2027Variant variant,
        ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>> freed,
        ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>> added
    ) =>
        new(
            variant.Kinds.ToImmutableDictionary(
                kind => kind.Key,
                kind => kind.Value with
                {
                    Movement =
                    [
                        .. freed.GetValueOrDefault(kind.Key) ?? kind.Value.Movement,
                        .. added.GetValueOrDefault(kind.Key) ?? [],
                    ],
                }
            ),
            variant.RiverBound,
            variant.PalaceSteppers,
            variant.ForwardPalaceSteppers,
            variant.Custodians,
            variant.SidewaysAcrossRiver,
            variant.Drafting?.PalaceBound ?? []
        );
}
