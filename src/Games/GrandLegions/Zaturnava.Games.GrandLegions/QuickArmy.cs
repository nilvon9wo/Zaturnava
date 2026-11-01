using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The army the program picks for a quick game, and how it lays it out (Proposed). It starts from the cheapest
/// pieces that meet the minimums, then, class by class - heavy, cavalry, artillery, infantry, the leader last -
/// spends what is left of the budget on better pieces of the same class, a piece not yet in the army before one
/// that is, the most valuable first; then, while the kingdom has room, adds the best pieces still affordable,
/// never another leader. Pieces an area holds (a palace, a river) are left out. The leader takes the back rank's
/// inner corner, the other classes the rest of the back ranks, infantry the front ones. Where the kingdoms touch,
/// with no neutral ground between them, the leader takes the far corner instead, as far as can be from the
/// neighbours, so that it is likely to outlive the first moves.
/// </summary>
public static class QuickArmy
{
    private static readonly ImmutableList<PieceClass> UpgradeOrder = [Heavy, Cavalry, Artillery, Infantry, Leader];
    private static readonly ImmutableList<PieceClass> RearOrder = [Leader, Heavy, Artillery, Cavalry];

    /// <summary>The pieces a quick army may have: any no area holds.</summary>
    public static ImmutableList<GrandPiece> Eligible { get; } =
    [
        .. GrandLegionsPieces.Catalogue.Where(piece =>
            piece.Restrictions.All(restriction => restriction.Kind != RestrictionKind.Area)
        ),
    ];

    public static ImmutableList<GrandPiece> Pick([Required] QuickGame game)
    {
        ImmutableList<GrandPiece> cheapest =
        [
            .. UpgradeOrder.SelectMany(pieceClass =>
                Enumerable.Repeat(Cheapest(pieceClass), game.Minimums.GetValueOrDefault(pieceClass))
            ),
        ];
        ImmutableList<GrandPiece> upgraded = Enumerable
            .Range(0, cheapest.Count)
            .Aggregate(cheapest, (army, slot) => Upgraded(army, slot, game.Budget));
        return Filled(upgraded, game);
    }

    /// <summary>The army, as seat 0 lays it out in its kingdom, from the bottom-left corner.</summary>
    public static ImmutableList<ArmyPiece> Layout([Required] QuickGame game, [Required] ImmutableList<GrandPiece> army)
    {
        IEnumerable<int> outward = Enumerable.Range(0, game.KingdomWidth).Reverse();
        IEnumerable<int> columns = game.HasNeutralZone ? outward : Enumerable.Range(0, game.KingdomWidth);
        ImmutableList<(int Column, int Row)> backToFront =
        [
            .. Enumerable.Range(0, game.KingdomDepth)
                .SelectMany(row => columns.Select(column => (column, row))),
        ];
        IEnumerable<GrandPiece> rear =
            RearOrder.SelectMany(pieceClass => army.Where(piece => piece.Class == pieceClass));
        IEnumerable<GrandPiece> infantry = army.Where(piece => piece.Class == Infantry);
        return
        [
            .. rear.Select((piece, index) => At(piece, backToFront[index])),
            .. infantry.Select((piece, index) => At(piece, backToFront[backToFront.Count - 1 - index])),
        ];
    }

    private static GrandPiece Cheapest(PieceClass pieceClass) =>
        Eligible.Where(piece => piece.Class == pieceClass).MinBy(piece => piece.Value)!;

    /// <summary>The army with the piece in <paramref name="slot"/> made better, if the budget allows.</summary>
    private static ImmutableList<GrandPiece> Upgraded(ImmutableList<GrandPiece> army, int slot, int budget)
    {
        GrandPiece current = army[slot];
        int left = budget - army.Sum(piece => piece.Value) + current.Value;
        GrandPiece? better = Best(
            army,
            piece => piece.Class == current.Class && piece.Value > current.Value && piece.Value <= left
        );
        return better is null ? army : army.SetItem(slot, better);
    }

    /// <summary>The army with the best affordable pieces added, but no leader, while its kingdom has room.</summary>
    private static ImmutableList<GrandPiece> Filled(ImmutableList<GrandPiece> army, QuickGame game)
    {
        int left = game.Budget - army.Sum(piece => piece.Value);
        GrandPiece? extra = army.Count < game.Cells
            ? Best(army, piece => piece.Class != Leader && piece.Value <= left)
            : null;
        return extra is null ? army : Filled(army.Add(extra), game);
    }

    /// <summary>Of the pieces that may be had, one not in the army before one that is; the most valuable.</summary>
    private static GrandPiece? Best(ImmutableList<GrandPiece> army, Func<GrandPiece, bool> mayBeHad) =>
        Eligible
            .Where(mayBeHad)
            .OrderBy(army.Contains)
            .ThenByDescending(piece => piece.Value)
            .FirstOrDefault();

    private static ArmyPiece At(GrandPiece piece, (int Column, int Row) cell) => new(piece.Id, cell.Column, cell.Row);
}
