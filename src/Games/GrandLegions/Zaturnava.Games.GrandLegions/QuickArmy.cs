using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;
using static Net.NowhereAtAll.Zaturnava.Games.GrandLegions.PieceClass;

namespace Net.NowhereAtAll.Zaturnava.Games.GrandLegions;

/// <summary>
/// The armies the program deals a quick game, each seat its own (Stated). Each is picked at random, in one pass: the
/// minimum of each class, the one leader among them, slot by slot in a random order, each piece drawn evenly from
/// those of its class the budget can still pay for, leaving enough for the slots still to fill at their classes'
/// cheapest (Proposed); so that dear pieces come seldom, as they seldom fit, but no class is always left what the
/// others did not spend, and no piece comes the more for being the next cheaper than another. The class's cheapest,
/// if even that does not fit. Then, while the kingdom has
/// room and the budget allows, more pieces at random, never another leader. Pieces a palace holds are left out, as
/// quick games have none yet; pieces a river holds, unless the game has rivers (Stated); lifts, which nothing plays
/// yet, are not bought. The leader
/// and the other non-infantry fill the back ranks in a random order, the leader's start its throne; infantry the
/// front ones, in a random order too. Where the kingdoms touch, with no neutral ground between them (the small
/// game), the leader takes the outer corner, as far as can be from the neighbours (Stated).
/// </summary>
public static class QuickArmy
{
    private static readonly ImmutableList<PieceClass> Classes = [Leader, Heavy, Artillery, Cavalry, Infantry];

    /// <summary>
    /// The pieces a quick army may have: any not confined to a palace, as quick games have none yet, though one
    /// may have powers along palace lines (Stated); and any a river holds only if the game has rivers.
    /// </summary>
    public static ImmutableList<GrandPiece> Eligible([Required] QuickGame game) =>
    [
        .. GrandLegionsPieces.Catalogue.Where(piece =>
            piece.Restrictions.All(restriction => restriction.Terrain switch
            {
                Terrain.Palace => !restriction.IsConfining,
                Terrain.River => game.Rivers != Rivers.None,
                _ => true,
            })
        ),
    ];

    /// <summary>
    /// Every seat's army, as <paramref name="seed"/> deals them, seat by seat; or, if <paramref name="areAlike"/>
    /// (Stated: only if the game's creator asks), the first seat's for every seat, laid out alike, each in its own
    /// kingdom.
    /// </summary>
    public static ImmutableList<Placement> Dealt([Required] QuickGame game, int seed, bool areAlike = false)
    {
        Random random = new(seed);
        ImmutableList<ImmutableList<ArmyPiece>> armies =
        [
            .. Enumerable.Range(0, Pinwheel.SeatCount).Select(_ => Layout(game, Pick(game, random), random)),
        ];
        return Pinwheel.EachOwn(game.Side, areAlike ? [.. armies.Select(_ => armies[0])] : armies);
    }

    public static ImmutableList<GrandPiece> Pick([Required] QuickGame game, [Required] Random random)
    {
        ImmutableList<GrandPiece> eligible = Eligible(game);
        PieceClass[] order =
        [
            .. Classes.SelectMany(pieceClass =>
                Enumerable.Repeat(pieceClass, game.Minimums.GetValueOrDefault(pieceClass))
            ),
        ];
        random.Shuffle(order);
        ImmutableList<PieceClass> slots = [.. order];
        ImmutableList<GrandPiece> drawn = slots
            .Select((pieceClass, index) => (Class: pieceClass, Later: slots.Skip(index + 1)))
            .Aggregate(
                ImmutableList<GrandPiece>.Empty,
                (army, slot) => army.Add(
                    Drawn(
                        Within(
                            eligible,
                            slot.Class,
                            game.Budget
                                - army.Sum(piece => piece.Value)
                                - slot.Later.Sum(later => Cheapest(eligible, later).Value)
                        ),
                        random
                    )
                )
            );
        return Filled(eligible, drawn, game, random);
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
    /// The pieces of the class that cost no more than <paramref name="left"/>, what the budget leaves once the
    /// slots still to fill are paid for at their classes' cheapest; or, if none does, the class's cheapest alone.
    /// </summary>
    private static ImmutableList<GrandPiece> Within(
        ImmutableList<GrandPiece> eligible,
        PieceClass pieceClass,
        int left
    )
    {
        ImmutableList<GrandPiece> affordable =
            [.. eligible.Where(piece => piece.Class == pieceClass && piece.Value <= left)];
        return affordable.IsEmpty ? [Cheapest(eligible, pieceClass)] : affordable;
    }

    private static GrandPiece Cheapest(ImmutableList<GrandPiece> eligible, PieceClass pieceClass) =>
        eligible.Where(piece => piece.Class == pieceClass).MinBy(piece => piece.Value)!;

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
