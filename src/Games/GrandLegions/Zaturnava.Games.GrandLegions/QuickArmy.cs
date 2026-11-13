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
/// room and the budget allows, more pieces at random, never another leader. Pieces a palace holds are left out unless
/// the game's armies may hold them, and then no more than its palace has room for; pieces a river holds, unless the
/// game has rivers (Stated); lifts, which nothing plays yet, are not bought. The leader
/// and the other non-infantry fill the back ranks in a random order, the leader's start its throne; infantry the
/// front ones, in a random order too. Where the kingdoms touch, with no neutral ground between them (the small
/// game), the leader takes the outer corner, as far as can be from the neighbours (Stated). Pieces kept to one colour
/// of square are then spread over both colours, as evenly as may be (Proposed).
/// </summary>
public static class QuickArmy
{
    private static readonly ImmutableList<PieceClass> Classes = [Leader, Heavy, Artillery, Cavalry, Infantry];

    /// <summary>
    /// The pieces a quick army may have: any not confined to a palace, though one may have powers along palace lines
    /// (Stated); those confined to one too, if the game's armies may hold them
    /// (<see cref="QuickGame.HasPalacePieces"/>, Stated 2026-11-13) and its kingdoms have room for a palace, or for one
    /// partly outside, if it may stand so (<see cref="QuickGame.HasEdgePalaces"/>); and any a river holds only if the
    /// game has rivers; and none come with an edition of the catalogue after the game's. Each at its price on the
    /// game's board (<see cref="BoardPrices"/>).
    /// </summary>
    public static ImmutableList<GrandPiece> Eligible([Required] QuickGame game)
    {
        bool palaced = game.HasPalacePieces && !Centres(game).IsEmpty;
        return
        [
            .. BoardPrices.Catalogue(QuickTerrain.BoardOf(game, 0)).Where(piece =>
                piece.Edition <= game.Edition
                && piece.Restrictions.All(restriction => restriction.Terrain switch
                {
                    Terrain.Palace => !restriction.IsConfining || palaced,
                    Terrain.River => game.Rivers != Rivers.None,
                    _ => true,
                })
            ),
        ];
    }

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
        int room = RoomOf(game);
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
                            Fitting(eligible, army, room),
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

    /// <summary>Every square of the game's kingdom a palace may stand about.</summary>
    private static ImmutableList<ArmySquare> Centres(QuickGame game) =>
        game.HasEdgePalaces
            ? PalaceSquares.EdgeCentres(game.Kingdom, game.Side)
            : PalaceSquares.Centres(game.Kingdom);

    /// <summary>
    /// The most pieces a palace holds an army may have: its palace's squares in its kingdom but its throne (8, in a
    /// palace all inside; 5, in one partly outside a 4 × 2 kingdom).
    /// </summary>
    private static int RoomOf(QuickGame game)
    {
        ImmutableHashSet<ArmySquare> kingdom = [.. game.Kingdom];
        return Centres(game) is [var centre, ..] ? PalaceSquares.Window(centre).Count(kingdom.Contains) - 1 : 0;
    }

    /// <summary>
    /// The army, as seat 0 lays it out in its kingdom, from the bottom-left corner. The pieces a palace holds, but the
    /// leader, stand in the palace about its throne (<see cref="PalaceSquares.CentreAbout"/>), as the draft would
    /// place it: on its squares furthest back, then nearest the throne's file, then leftmost (Proposed).
    /// </summary>
    public static ImmutableList<ArmyPiece> Layout(
        [Required] QuickGame game,
        [Required] ImmutableList<GrandPiece> army,
        [Required] Random random
    )
    {
        ImmutableList<ArmySquare> backToFront = game.Kingdom;
        ImmutableList<GrandPiece> others =
            Shuffled(army.Where(piece => piece.Class is not Infantry and not Leader && !IsHeld(piece)), random);
        int throne = game.HasNeutralZone
            ? random.Next(Math.Min(game.KingdomWidth - 1, others.Count) + 1)
            : 0;
        ImmutableList<GrandPiece> rear = others.Insert(throne, army.Single(piece => piece.Class == Leader));
        ImmutableList<GrandPiece> infantry =
            Shuffled(army.Where(piece => piece.Class == Infantry && !IsHeld(piece)), random);
        ImmutableList<GrandPiece> held = Shuffled(army.Where(IsHeld), random);
        ArmySquare seat = backToFront[throne];
        ImmutableList<ArmySquare> palace = held.IsEmpty ? [] : PalaceAbout(game, seat, held.Count);
        ImmutableList<ArmySquare> squares =
        [
            .. backToFront.Where(square => square != seat && !palace.Contains(square)).Take(throne),
            seat,
            .. backToFront.Where(square => square != seat && !palace.Contains(square)).Skip(throne),
        ];
        ImmutableList<ArmyPiece> laid =
        [
            .. rear.Select((piece, index) => At(piece, squares[index])),
            .. infantry.Select((piece, index) => At(piece, squares[squares.Count - 1 - index])),
            .. held.Zip(palace).Select(placed => At(placed.First, placed.Second)),
        ];
        return Enumerable.Range(0, laid.Count).Aggregate(laid, Balanced);
    }

    /// <summary>
    /// The first <paramref name="count"/> squares of the palace about <paramref name="seat"/> in the kingdom, the
    /// throne aside: those furthest back, then nearest the throne's file, then leftmost.
    /// </summary>
    private static ImmutableList<ArmySquare> PalaceAbout(QuickGame game, ArmySquare seat, int count) =>
    [
        .. PalaceSquares.Window(PalaceSquares.CentreAbout(Centres(game), seat))
            .Where(square => square != seat && game.Kingdom.Contains(square))
            .OrderBy(square => square.Row)
            .ThenBy(square => Math.Abs(square.Column - seat.Column))
            .ThenBy(square => square.Column)
            .Take(count),
    ];

    /// <summary>Whether the piece is one a palace holds, and not the leader, who holds the throne.</summary>
    internal static bool IsHeld(GrandPiece piece) =>
        piece.Class != Leader && PalaceConfinement.Confined.Contains(piece.Id);

    /// <summary>
    /// The pieces <paramref name="army"/> may still be given: those a palace holds only while its palace has room.
    /// </summary>
    private static ImmutableList<GrandPiece> Fitting(
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        int room
    ) =>
        army.Count(IsHeld) < room ? eligible : [.. eligible.Where(piece => !IsHeld(piece))];

    /// <summary>
    /// The army with the piece at <paramref name="index"/>, if one kept to the colour of square it starts on, moved
    /// to the other colour should more such pieces before it stand on its colour than on the other: so that they are
    /// spread over both colours as evenly as may be (Proposed; the user's report of 2026-11-12, of an army mostly
    /// bound to dark squares). It changes places with the first piece of its own part of the army - the rear, or the
    /// infantry - on the other colour, neither the leader nor kept to one colour itself; it stays, if there is none.
    /// </summary>
    private static ImmutableList<ArmyPiece> Balanced(ImmutableList<ArmyPiece> army, int index)
    {
        ArmyPiece piece = army[index];
        ImmutableList<ArmyPiece> before = [.. army.Take(index).Where(IsSpread)];
        int same = before.Count(other => IsDark(other) == IsDark(piece));
        int? partner = IsSpread(piece) && same > before.Count - same
            ? Enumerable.Range(0, army.Count)
                .Where(other => IsDark(army[other]) != IsDark(piece))
                .Where(other => !IsColourBound(army[other]) && ClassOf(army[other]) != Leader)
                .Where(other => !IsHeld(GrandLegionsPieces.ById[army[other].Kind]))
                .Where(other => IsInfantry(army[other]) == IsInfantry(piece))
                .Select(other => (int?)other)
                .FirstOrDefault()
            : null;
        return partner is { } at
            ? army.SetItem(index, piece with { Kind = army[at].Kind }).SetItem(at, army[at] with { Kind = piece.Kind })
            : army;
    }

    /// <summary>Whether the piece is kept to one colour of square, and not one a palace holds.</summary>
    private static bool IsSpread(ArmyPiece piece) =>
        IsColourBound(piece) && !IsHeld(GrandLegionsPieces.ById[piece.Kind]);

    private static bool IsColourBound(ArmyPiece piece) =>
        GrandLegionsPieces.ById[piece.Kind].Restrictions.Any(restriction => restriction.IsColourBinding);

    private static bool IsDark(ArmyPiece piece) => int.IsEvenInteger(piece.Column + piece.Row);

    private static PieceClass ClassOf(ArmyPiece piece) => GrandLegionsPieces.ById[piece.Kind].Class;

    private static bool IsInfantry(ArmyPiece piece) => ClassOf(piece) == Infantry;

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
            ? [.. Fitting(eligible, army, RoomOf(game)).Where(piece => piece.Class != Leader && piece.Value <= left)]
            : [];
        return affordable.IsEmpty ? army : Filled(eligible, army.Add(Drawn(affordable, random)), game, random);
    }

    private static ArmyPiece At(GrandPiece piece, ArmySquare square) => new(piece.Id, square.Column, square.Row);
}
