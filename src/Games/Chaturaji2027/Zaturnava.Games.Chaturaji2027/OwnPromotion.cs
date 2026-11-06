using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// A kind's own promotion: free, into any of <paramref name="Into"/>, for its own army, while it stands within
/// <paramref name="Ranks"/> rows of the far edge (1: on the last row; 3: in the far three) - or, if it has one, in
/// its <see cref="Zone"/>.
/// </summary>
public sealed record OwnPromotion(ImmutableList<PieceKindId> Into, int Ranks)
{
    /// <summary>Where the promotion may be taken instead of the far rows; none, unless set.</summary>
    public PromotionZone? Zone { get; init; }

    /// <summary>Where <see cref="Zone"/> is, for people: "in an enemy kingdom".</summary>
    public string? Where { get; init; }
}
