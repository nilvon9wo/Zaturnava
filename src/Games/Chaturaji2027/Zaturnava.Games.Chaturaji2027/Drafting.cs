using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// How a game's players may draft their own armies before play (specification §3; Proposed: in turn, in view of each
/// other, a palace first where the kingdom has room for one): each with <paramref name="Budget"/> to spend, at each
/// kind's worth, on the kinds <paramref name="Offered"/> on the game's terrain, placed on the squares of its
/// <paramref name="Kingdom"/> (as seat 0's, from the bottom-left corner); and how the rest of an army is drafted at
/// random (<paramref name="Fill"/>).
/// </summary>
public sealed record Drafting(
    int Budget,
    ImmutableList<ArmySquare> Kingdom,
    Func<int, ImmutableHashSet<PieceKindId>> Offered,
    Func<ArmyFill, ArmyFilled> Fill
)
{
    /// <summary>Every terrain a game may be dealt, as the ruleset numbers them: its own (0) alone, if unset.</summary>
    public ImmutableList<int> Terrains { get; init; } = [0];
}
