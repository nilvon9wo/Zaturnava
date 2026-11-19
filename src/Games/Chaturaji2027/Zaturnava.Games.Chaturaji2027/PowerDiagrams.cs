using System.Collections.Concurrent;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Each special power a kind has, drawn at work beside its moves (the user's report of 2026-11-19: "all pieces with
/// special powers need their powers illustrated"), on the same <see cref="MoveDiagrams.Side"/>-cell board, the piece
/// on its centre, facing up: the two-cell first move and <i>en passant</i>, castling, the flying general, the
/// face-off, converting, escape, the back-to-back swap, and berserk - a drawback, not a power, drawn apart from its
/// captures, its own side's pieces and its enemies in their colours (the user's report of 2026-11-19: berserk drawn
/// with its captures only seemed to repeat them). Each is one example, its cells marked with what they mean; where
/// the piece goes in it, played.
/// </summary>
internal sealed class PowerDiagrams(Chaturaji2027Variant variant, Func<PieceKindId, string> nameOf)
{
    private const int Side = MoveDiagrams.Side;
    private const int Middle = Side / 2;

    /// <summary>
    /// The cell <paramref name="right"/> cells to the centre's right and <paramref name="ahead"/> ahead.
    /// </summary>
    private static int At(int right, int ahead) => ((Middle + ahead) * Side) + Middle + right;

    private readonly ConcurrentDictionary<PieceKindId, MoveDiagram> _drawn = new();

    /// <summary>
    /// <paramref name="drawn"/>, with each special power of <paramref name="kind"/> drawn beside it, drawn once.
    /// </summary>
    public MoveDiagram Of(PieceKindId kind, MoveDiagram drawn) =>
        this._drawn.GetOrAdd(
            kind,
            _ => drawn with
            {
                Alternates =
                [
                    .. drawn.Alternates,
                    .. this.PowersOf(kind, drawn).Where(power => power.Has).Select(power => power.Drawn),
                ],
            }
        );

    private IEnumerable<(bool Has, NamedDiagram Drawn)> PowersOf(PieceKindId kind, MoveDiagram drawn)
    {
        string rooks = string.Join(" or ", variant.CastlingRooks.Select(nameOf).Order(StringComparer.Ordinal));
        int ahead = At(0, 1);
        int leader = At(0, Middle);
        yield return (variant.StartRowPowers.Contains(kind), Power(
            "First move",
            "From its start rows, its first move may go two cells straight ahead.",
            new PlayedExample(At(0, 2), []),
            new CellMark(CellMark.Lands, "its first move may land here", [At(0, 2)])
        ));
        yield return (variant.StartRowPowers.Contains(kind), Power(
            "En passant",
            "Any enemy piece, not just a pawn, that just went two cells or more past it, it may take as it lands "
                + "on the first cell passed, even out of turn, until its own turn ends.",
            new PlayedExample(At(1, 1), [At(1, 0)]) { EnemyFrom = At(1, 2) },
            new CellMark(CellMark.Trail, "where the enemy came from, two cells past it", [At(1, 2)]),
            new CellMark(CellMark.Taken, "any enemy piece, gone past it: taken", [At(1, 0)]),
            new CellMark(CellMark.Lands, "it lands on the first cell the enemy passed", [At(1, 1)])
        ));
        yield return (variant.Castlers.Contains(kind), Power(
            "Castling",
            Castling.Told(rooks),
            new PlayedExample(At(2, 0), []) { PartnerFrom = At(Middle, 0), PartnerTo = At(1, 0) },
            new CellMark(CellMark.Standing, $"its own {rooks}, however far, moved or not", [At(Middle, 0)]),
            new CellMark(CellMark.Unattacked, "no enemy may be able to capture here", [At(0, 0), At(1, 0), At(2, 0)]),
            new CellMark(CellMark.Lands, "it lands here", [At(2, 0)]),
            new CellMark(CellMark.Trail, $"the {rooks} lands here, over it", [At(1, 0)])
        ));
        yield return (variant.FlyingCapturers.Contains(kind), new NamedDiagram(
            "Flying general",
            Plain(new PlayedExample(leader, [leader])) with
            {
                Told = "An enemy leader in its row or column, with nothing between, however far: it flies the whole "
                    + "way and takes it, as its move, out of its palace if need be.",
                Marks = [new CellMark(CellMark.Taken, "an enemy leader, nothing between: flown to, taken", [leader])],
                Palace = drawn.Palace,
                IsPalaceLined = drawn.IsPalaceLined,
            }
        ));
        yield return (variant.FaceOffCallers.Contains(kind), Power(
            "Face-off",
            "Facing an enemy leader in its row or column, nothing between, its player may call a face-off, even out "
                + "of turn: both leaders are taken at once, and both sides score. Neither moves.",
            null,
            new CellMark(CellMark.Taken, "it and the enemy leader facing it, both taken", [At(0, 0), leader])
        ));
        yield return (variant.Converters.Contains(kind), Power(
            "Converting",
            "It takes nothing: after it moves, every unbroken line of one enemy's pieces, none a leader, from next to "
                + "it up to another of its player's pieces, becomes its player's.",
            new PlayedExample(ahead, []),
            new CellMark(CellMark.Lands, "it moves here", [ahead]),
            new CellMark(CellMark.Converted, "an enemy's pieces, won over", [At(0, 2), At(0, 3)]),
            new CellMark(CellMark.Standing, "another of its player's pieces", [leader])
        ));
        yield return (variant.Fleers.Contains(kind), Power(
            "Escape",
            "On its own far corner, the board's corner across from its army's, it may leave the board as its move, "
                + "where no one can take it, to come back on doubles.",
            null,
            new CellMark(CellMark.Lands, "its own far corner: from here it may leave the board", [At(Middle, Middle)])
        ));
        yield return (variant.Custodians.Contains(kind), Power(
            "Berserk",
            "While an enemy holds its throne, it goes berserk: landing, it takes every piece held between it and "
                + "any other piece, friend or foe, its own side's too.",
            new PlayedExample(At(0, 2), [At(0, 3), At(1, 2)]),
            new CellMark(CellMark.OnThrone, "an enemy on its throne", [At(0, -3)]),
            new CellMark(CellMark.Lands, "it lands here", [At(0, 2)]),
            new CellMark(CellMark.TakenFriend, "a piece of its own side, held: taken", [At(0, 3)]),
            new CellMark(CellMark.Taken, "an enemy, held: taken", [At(1, 2)]),
            new CellMark(CellMark.StandingFoe, "enemies holding them with it", [At(0, 4), At(2, 2)])
        ));
        yield return (!drawn.Jumps.IsEmpty, Power(
            "Chain of jumps",
            "Having jumped an enemy, taking it, it may jump again from where it landed, and again, with no die, while "
                + "it can, each jump picked on its own, in any way it jumps; or stop.",
            new PlayedExample(At(-2, 4), [At(-1, 1), At(-1, 3)]) { Via = At(-2, 2) },
            new CellMark(CellMark.Taken, "enemies jumped: each taken", [At(-1, 1), At(-1, 3)]),
            new CellMark(CellMark.Trail, "it lands, then jumps on", [At(-2, 2)]),
            new CellMark(CellMark.Lands, "it ends here, or jumps on, if it can", [At(-2, 4)])
        ));
        yield return (variant.Swappers.Contains(kind), Power(
            "Swap",
            "Swaps places with an ally's piece straight ahead of it that moves only forward too, the two back to back.",
            new PlayedExample(ahead, []),
            new CellMark(CellMark.Standing, "an ally's piece straight ahead: the two swap places", [ahead])
        ));
    }

    /// <summary>A power at work, named, told, its example played and its cells marked.</summary>
    private static NamedDiagram Power(string name, string told, PlayedExample? example, params CellMark[] marks) =>
        new(name, Plain(example) with { Told = told, Marks = [.. marks] });

    /// <summary>The board alone, the piece on its centre, no move nor capture drawn but the example's.</summary>
    private static MoveDiagram Plain(PlayedExample? example) => new(Side, [], [], []) { Example = example };
}
