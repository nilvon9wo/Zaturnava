using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The armies the program deals a quick game, each seat its own (Stated). Each is picked piece by piece, at random:
/// the minimum of each class, the dearest classes first - the one leader, then heavy, artillery, cavalry (that
/// order Proposed), infantry last. Should the minimum cost more than the budget, the dearest pieces, for their
/// class, are made cheaper, one step at a time, until it does not (how, Proposed). Then, while the kingdom has
/// room and the budget allows, more pieces at random, never another leader. Pieces a palace holds are left out, as
/// quick games have none yet; pieces a river holds, unless the game has rivers (Stated); lifts, which nothing plays
/// yet, are not bought. The leader
/// and the other non-infantry fill the back ranks in a random order, the leader's start its throne; infantry the
/// front ones, in a random order too. Where the kingdoms touch, with no neutral ground between them (the small
/// game), the leader takes the outer corner, as far as can be from the neighbours (Stated).
/// </summary>
public static class QuickArmy
{
    private static readonly ImmutableList<PieceClass> PickOrder = [Leader, Heavy, Artillery, Cavalry, Infantry];

    /// <summary>
    /// The pieces a quick army may have: any no palace holds, as quick games have none yet; and any a river holds
    /// only if the game has rivers.
    /// </summary>
    public static ImmutableList<GrandPiece> Eligible([Required] QuickGame game) =>
    [
        .. GrandLegionsPieces.Catalogue.Where(piece =>
            piece.Restrictions.All(restriction => restriction.Terrain switch
            {
                Terrain.Palace => false,
                Terrain.River => game.Rivers != Rivers.None,
                _ => true,
            })
        ),
    ];

    /// <summary>Every seat's army, as <paramref name="seed"/> deals them, seat by seat.</summary>
    public static ImmutableList<Placement> Dealt([Required] QuickGame game, int seed)
    {
        Random random = new(seed);
        ImmutableList<ImmutableList<ArmyPiece>> armies =
        [
            .. Enumerable.Range(0, Pinwheel.SeatCount).Select(_ => Layout(game, Pick(game, random), random)),
        ];
        return Pinwheel.EachOwn(game.Side, armies);
    }

    public static ImmutableList<GrandPiece> Pick([Required] QuickGame game, [Required] Random random)
    {
        ImmutableList<GrandPiece> eligible = Eligible(game);
        ImmutableList<GrandPiece> drawn =
        [
            .. PickOrder.SelectMany(pieceClass =>
                Enumerable
                    .Range(0, game.Minimums.GetValueOrDefault(pieceClass))
                    .Select(_ => Drawn(eligible.Where(piece => piece.Class == pieceClass), random))
            ),
        ];
        return Filled(eligible, Downgraded(eligible, drawn, game.Budget), game, random);
    }

    /// <summary>The army, as seat 0 lays it out in its kingdom, from the bottom-left corner.</summary>
    public static ImmutableList<ArmyPiece> Layout(
        [Required] QuickGame game,
        [Required] ImmutableList<GrandPiece> army,
        [Required] Random random
    )
    {
        ImmutableList<(int Column, int Row)> backToFront =
        [
            .. Enumerable.Range(0, game.KingdomDepth)
                .SelectMany(row => Enumerable.Range(0, game.KingdomWidth).Select(column => (column, row))),
        ];
        ImmutableList<GrandPiece> others =
            Shuffled(army.Where(piece => piece.Class is not Infantry and not Leader), random);
        int throne = game.HasNeutralZone
            ? random.Next(Math.Min(game.KingdomWidth - 1, others.Count) + 1)
            : 0;
        ImmutableList<GrandPiece> rear = others.Insert(throne, army.Single(piece => piece.Class == Leader));
        ImmutableList<GrandPiece> infantry = Shuffled(army.Where(piece => piece.Class == Infantry), random);
        return
        [
            .. rear.Select((piece, index) => At(piece, backToFront[index])),
            .. infantry.Select((piece, index) => At(piece, backToFront[backToFront.Count - 1 - index])),
        ];
    }

    private static GrandPiece Drawn(IEnumerable<GrandPiece> pieces, Random random)
    {
        ImmutableList<GrandPiece> choices = [.. pieces];
        return choices[random.Next(choices.Count)];
    }

    private static ImmutableList<GrandPiece> Shuffled(IEnumerable<GrandPiece> pieces, Random random)
    {
        GrandPiece[] shuffled = [.. pieces];
        random.Shuffle(shuffled);
        return [.. shuffled];
    }

    /// <summary>
    /// The army, made cheaper while it costs more than <paramref name="budget"/>: each time, the piece dearest
    /// beyond the cheapest of its class is swapped for the next cheaper of its class. One every piece of which is
    /// already the cheapest stays as it is.
    /// </summary>
    private static ImmutableList<GrandPiece> Downgraded(
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        int budget
    )
    {
        GrandPiece? dearest = army
            .Where(piece => piece.Value > Cheapest(eligible, piece.Class).Value)
            .MaxBy(piece => piece.Value - Cheapest(eligible, piece.Class).Value);
        return army.Sum(piece => piece.Value) <= budget || dearest is null
            ? army
            : Downgraded(eligible, army.Replace(dearest, CheaperThan(eligible, dearest)), budget);
    }

    private static GrandPiece Cheapest(ImmutableList<GrandPiece> eligible, PieceClass pieceClass) =>
        eligible.Where(piece => piece.Class == pieceClass).MinBy(piece => piece.Value)!;

    private static GrandPiece CheaperThan(ImmutableList<GrandPiece> eligible, GrandPiece piece) =>
        eligible
            .Where(other => other.Class == piece.Class && other.Value < piece.Value)
            .MaxBy(other => other.Value)!;

    /// <summary>The army, with pieces but leaders added at random while its kingdom has room and it can pay.</summary>
    private static ImmutableList<GrandPiece> Filled(
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        QuickGame game,
        Random random
    )
    {
        int left = game.Budget - army.Sum(piece => piece.Value);
        ImmutableList<GrandPiece> affordable = army.Count < game.Cells
            ? [.. eligible.Where(piece => piece.Class != Leader && piece.Value <= left)]
            : [];
        return affordable.IsEmpty ? army : Filled(eligible, army.Add(Drawn(affordable, random)), game, random);
    }

    private static ArmyPiece At(GrandPiece piece, (int Column, int Row) cell) => new(piece.Id, cell.Column, cell.Row);
}
