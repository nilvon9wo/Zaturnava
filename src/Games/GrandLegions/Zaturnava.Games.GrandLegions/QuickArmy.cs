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
/// others did not spend, and no piece comes the more for being the next cheaper than another; from
/// <see cref="EvenSpreadEdition"/> on, within the slot's even share of what is left (Stated). The class's cheapest,
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
    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which each slot may spend only its even share of what the
    /// budget leaves; a game dealt before replays as it was dealt.
    /// </summary>
    public const int EvenSpreadEdition = 3;

    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which an army's budget is what the designer's formula gives
    /// (<see cref="FormulaBudget"/>), and each slot may spend its share of what is left in proportion to its class's
    /// average price, not an even share (Stated 2026-11-13: "the budget ... was intended to reflect the sum of the
    /// average cost of [each] class of piece ... multiplied by the minimum frequency ..., with a bit of extra points
    /// left over"). A game dealt before replays as it was dealt.
    /// </summary>
    public const int FormulaEdition = GrandLegionsPieces.MultipleCapturesRepriced;

    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which a leader kept to one colour of square stays on the
    /// throne it was laid out on, not spread over both colours with the rest: the palace is placed about its throne, so
    /// moving it left the pieces a palace holds outside (the user's report of 2026-11-19, of a Guard on d2 outside the
    /// palace a1:c3); and from which an army filled at random keeps the palace placed before
    /// (<see cref="QuickDraft.PalaceKeptEdition"/>). A game dealt before replays as it was dealt.
    /// </summary>
    public const int ThroneKeptEdition = 9;

    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which an army aims at a composition (playtest 2026-11-15,
    /// E1 to E3; <see cref="TargetOf"/>): a leader for each <see cref="PiecesPerLeader"/> pieces, about half infantry,
    /// the rest even among heavies, cavalry and artillery, filling at least <see cref="OccupiedPercent"/> of its
    /// kingdom; its budget what that composition costs at its classes' average prices
    /// (<see cref="CompositionBudget"/>); and each piece of a class drawn the less often the further its price lies
    /// from the class's commonest (<see cref="WeightOf"/>): dear pieces seldom, the cheapest seldom too. A game dealt
    /// before replays as it was.
    /// </summary>
    public const int CompositionEdition = 10;

    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which a leader a palace holds, as a Xiangqi or Janggi
    /// General, starts in its palace when its army has another leader on the throne already; before it, one started
    /// anywhere, outside the palace that should hold it (the user's report of 2026-11-19, of flying generals starting
    /// outside the palace). A game dealt before replays as it was.
    /// </summary>
    public const int HeldLeadersEdition = 12;

    /// <summary>
    /// The edition (<see cref="QuickGame.Edition"/>) from which an army dealt a palace has it placed first, anywhere
    /// in its kingdom it may stand, at random (<see cref="PalacesDealt"/>), then its throne on any square of it, as an
    /// army filled at random in a palace placed before is (the user's words of 2026-11-19: in a kingdom wide or deep
    /// enough, a palace need not stand against any edge of the board; the palace first, then the throne in it);
    /// before it, the throne was laid out on the back rank, and the palace about it stood against the board's edge. A
    /// game dealt before replays as it was.
    /// </summary>
    public const int PalacesAnywhereEdition = 13;

    /// <summary>How much of its kingdom, in hundredths, an army aims to fill (Stated, E2: at least 80%).</summary>
    public const int OccupiedPercent = 80;

    /// <summary>How many pieces an army has for each leader, at least one (Stated, E1).</summary>
    public const int PiecesPerLeader = 20;

    /// <summary>
    /// Where, as a part of its class's median price, a piece is drawn most often (Proposed: a little under the median,
    /// so that the dearer a piece the rarer, past it).
    /// </summary>
    private const double CommonestShare = 0.8;

    /// <summary>
    /// How fast a piece grows rarer as its price leaves the commonest, in the log of their ratio (Proposed: a piece at
    /// twice or half the commonest price drawn about three times less often).
    /// </summary>
    private const double Spread = 0.45;

    /// <summary>The bit left over, past the formula's sum, in hundredths of it (Proposed: a tenth).</summary>
    public const int LeftOverPercent = 10;

    /// <summary>
    /// What each army of the game may spend: from <see cref="CompositionEdition"/> on, <see cref="CompositionBudget"/>;
    /// from <see cref="FormulaEdition"/>, <see cref="FormulaBudget"/>; before, the game's own budget.
    /// </summary>
    public static int BudgetOf([Required] QuickGame game) =>
        game.Edition switch
        {
            >= CompositionEdition => CompositionBudget(game),
            >= FormulaEdition => FormulaBudget(game),
            _ => game.Budget,
        };

    /// <summary>
    /// How many pieces an army aims at (E2): <see cref="OccupiedPercent"/> of its kingdom, rounded up; or, if more,
    /// the game's minimums.
    /// </summary>
    public static int AimOf([Required] QuickGame game) =>
        Math.Max(game.Minimums.Values.Sum(), ((game.Cells * OccupiedPercent) + 99) / 100);

    /// <summary>
    /// How many of each class an army of <paramref name="count"/> pieces aims at (E1): a leader for each
    /// <see cref="PiecesPerLeader"/>, rounded up; half infantry, rounded up; the rest even among heavies, cavalry and
    /// artillery, any odd one to the heavies first, then the cavalry; and never fewer than the game's minimums. For an
    /// army of a family's pieces, which may lack whole classes (<see cref="OfFamily"/>), only the classes
    /// <paramref name="present"/>: what the others would have had shared evenly among the classes present but the
    /// leaders, in the order of <see cref="Classes"/>; among the leaders, if it has nothing else, as an Othello army of
    /// Evangelists (Proposed, 2026-11-20: so that its army fills its kingdom as another would, the budget allowing; a
    /// Checkers army, its Kings and a host of Men).
    /// </summary>
    public static ImmutableDictionary<PieceClass, int> TargetOf(
        [Required] QuickGame game,
        int count,
        ImmutableHashSet<PieceClass>? present = null
    )
    {
        int leaders = (count + PiecesPerLeader - 1) / PiecesPerLeader;
        int infantry = (count + 1) / 2;
        int rest = Math.Max(0, count - leaders - infantry);
        PieceClass[] others = [Heavy, Cavalry, Artillery];
        ImmutableDictionary<PieceClass, int> target = Classes.ToImmutableDictionary(
            pieceClass => pieceClass,
            pieceClass => Math.Max(
                game.Minimums.GetValueOrDefault(pieceClass),
                pieceClass switch
                {
                    Leader => leaders,
                    Infantry => infantry,
                    _ => (rest / others.Length) + (Array.IndexOf(others, pieceClass) < rest % others.Length ? 1 : 0),
                }
            )
        );
        return present is null ? target : Shared(target, present);
    }

    /// <summary>
    /// <paramref name="target"/>, the classes not <paramref name="present"/> none, what they had shared as
    /// <see cref="TargetOf"/> tells.
    /// </summary>
    private static ImmutableDictionary<PieceClass, int> Shared(
        ImmutableDictionary<PieceClass, int> target,
        ImmutableHashSet<PieceClass> present
    )
    {
        ImmutableList<PieceClass> others = [.. Classes.Where(each => each != Leader && present.Contains(each))];
        ImmutableList<PieceClass> takers = others.IsEmpty ? [Leader] : others;
        int spare = target.Where(each => !present.Contains(each.Key)).Sum(each => each.Value);
        return target.ToImmutableDictionary(
            each => each.Key,
            each => (present.Contains(each.Key), takers.IndexOf(each.Key)) switch
            {
                (false, _) => 0,
                (_, < 0) => each.Value,
                var (_, at) => each.Value + (spare / takers.Count) + (at < spare % takers.Count ? 1 : 0),
            }
        );
    }

    /// <summary>
    /// What the composition an army aims at (<see cref="TargetOf"/>, of <see cref="AimOf"/> pieces) costs, each piece
    /// at its class's average price (with no rivers, nor pieces a palace holds; at the board's prices), and
    /// <see cref="LeftOverPercent"/> more, to the nearest point (E2).
    /// </summary>
    public static int CompositionBudget([Required] QuickGame game)
    {
        ImmutableList<GrandPiece> eligible =
            Eligible(game with { Rivers = Rivers.None, HasPalacePieces = false, HasEdgePalaces = false });
        double sum = TargetOf(game, AimOf(game)).Sum(target => target.Value * Average(eligible, target.Key));
        return (int)Math.Round(sum * (100 + LeftOverPercent) / 100, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The designer's formula: for each class, the average price of the pieces a quick army of the game may have of it
    /// (with no rivers, nor pieces a palace holds; at the board's prices), times the least the army has of the class;
    /// summed, and <see cref="LeftOverPercent"/> more, to the nearest point.
    /// </summary>
    public static int FormulaBudget([Required] QuickGame game)
    {
        ImmutableList<GrandPiece> eligible =
            Eligible(game with { Rivers = Rivers.None, HasPalacePieces = false, HasEdgePalaces = false });
        double sum = game.Minimums.Sum(minimum => minimum.Value * Average(eligible, minimum.Key));
        return (int)Math.Round(sum * (100 + LeftOverPercent) / 100, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// The average price of the pieces of the class among <paramref name="eligible"/>, but those that may promote into
    /// a leader, priced as nearly one (the Shogi Pawn, the Lance, the Man; Proposed: they would make a class's average
    /// what almost none of its pieces costs); 0, if none.
    /// </summary>
    private static double Average(ImmutableList<GrandPiece> eligible, PieceClass pieceClass) =>
        eligible
            .Where(piece => piece.Class == pieceClass && !IsLeaderBound(piece))
            .Select(piece => (double)piece.Value)
            .DefaultIfEmpty()
            .Average();

    private static bool IsLeaderBound(GrandPiece piece) =>
        piece.PromotesTo.Any(into => GrandLegionsPieces.ById[into].Class == Leader);

    /// <summary>
    /// The most a piece added past the minimums may cost, from <see cref="FormulaEdition"/> on: its class's average
    /// and the bit left over (Proposed), so that the dearest pieces, as the Amazon, come very seldom.
    /// </summary>
    private static double MostAdded(ImmutableList<GrandPiece> eligible, PieceClass pieceClass) =>
        Average(eligible, pieceClass) * (100 + LeftOverPercent) / 100;

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
            .. BoardPrices.Catalogue(QuickTerrain.BoardOf(game, 0), game.Edition).Where(piece =>
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
    /// kingdom. Each army spends <paramref name="budget"/>, if the game's creator set one (Stated 2026-11-19: "I still
    /// can't set the points to spend without manually drafting the armies"); else <see cref="BudgetOf"/>. A seat
    /// <paramref name="families"/> names a family for (a bot's, which plays that game's pieces) is dealt that family's
    /// pieces only (<see cref="OfFamily"/>; the user's report of 2026-11-20: Naresuan dealt an army of every game's
    /// pieces); but not if the armies are alike, which no bot's family overrides (the user's word of 2026-11-20).
    /// </summary>
    public static ImmutableList<Placement> Dealt(
        [Required] QuickGame game,
        int seed,
        bool areAlike = false,
        int? budget = null,
        ImmutableList<string?>? families = null
    )
    {
        game = game with { Budget = budget ?? BudgetOf(game) };
        Random random = new(seed);
        ImmutableList<ArmySquare> palaces = PalacesDealt(game, seed, areAlike);
        ImmutableList<ImmutableList<ArmyPiece>> armies =
        [
            .. Enumerable.Range(0, Pinwheel.SeatCount).Select(seat =>
                Layout(
                    game,
                    Pick(game, random, areAlike ? null : families?.ElementAtOrDefault(seat)),
                    random,
                    palaces.IsEmpty ? null : PalaceSquares.Window(palaces[seat])
                )
            ),
        ];
        return Pinwheel.EachOwn(game.Width, game.Height, areAlike ? [.. armies.Select(_ => armies[0])] : armies);
    }

    /// <summary>
    /// The centre of each seat's palace, in seat order, as seat 0 sees its kingdom, as <paramref name="seed"/> deals
    /// them from <see cref="PalacesAnywhereEdition"/> on: any square of the kingdom a palace may stand about, at
    /// random, the first seat's for every seat if <paramref name="areAlike"/>; none, in a game dealt no palaces, or
    /// before.
    /// Drawn apart from the armies, so that the rules find the same palaces the armies were laid out in.
    /// </summary>
    public static ImmutableList<ArmySquare> PalacesDealt([Required] QuickGame game, int seed, bool areAlike)
    {
        if (!game.HasPalaces || game.Edition < PalacesAnywhereEdition)
        {
            return [];
        }

        Random random = new(~seed);
        ImmutableList<ArmySquare> centres = Centres(game);
        ImmutableList<ArmySquare> drawn =
            [.. Enumerable.Range(0, Pinwheel.SeatCount).Select(_ => centres[random.Next(centres.Count)])];
        return areAlike ? [.. drawn.Select(_ => drawn[0])] : drawn;
    }

    public static ImmutableList<GrandPiece> Pick(
        [Required] QuickGame game,
        [Required] Random random,
        string? family = null
    )
    {
        if (game.Edition >= CompositionEdition)
        {
            ImmutableList<GrandPiece> ofFamily = OfFamily(Eligible(game), family);
            return Composed(
                game,
                ofFamily,
                [],
                random,
                family is null ? null : [.. ofFamily.Select(piece => piece.Class)]
            );
        }

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
                            Fitting(eligible, army, room, game.Edition),
                            slot.Class,
                            LeftFor(game, eligible, army, slot.Class, [.. slot.Later])
                        ),
                        random
                    )
                )
            );
        return Filled(eligible, drawn, game, random);
    }

    /// <summary>
    /// Of <paramref name="eligible"/>, for an army of <paramref name="family"/>'s pieces, only that family's game's
    /// (<see cref="GrandPiece.IsOf"/>), none of a class it has none of: a Western army has no artillery, a Checkers
    /// army only Kings and Men (the user's word of 2026-11-20: "This game should reflect that"). All, for no family.
    /// </summary>
    public static ImmutableList<GrandPiece> OfFamily([Required] ImmutableList<GrandPiece> eligible, string? family) =>
        family is null ? eligible : [.. eligible.Where(piece => piece.IsOf(family))];

    /// <summary>
    /// <paramref name="army"/>, and the pieces <see cref="Next"/> adds to it, one at a time, while it adds one.
    /// </summary>
    private static ImmutableList<GrandPiece> Composed(
        QuickGame game,
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        Random random,
        ImmutableHashSet<PieceClass>? present
    ) =>
        Next(game, eligible, army, game.Budget - army.Sum(each => each.Value), random, present) is { } next
            ? Composed(game, eligible, army.Add(next), random, present)
            : army;

    /// <summary>
    /// The next piece for <paramref name="army"/>, from <see cref="CompositionEdition"/> on, with
    /// <paramref name="left"/> still to spend; none, once its kingdom is full or nothing more fits. While the army
    /// falls
    /// short of the composition it aims at (<see cref="TargetOf"/> of <see cref="AimOf"/>), a class it lacks, drawn
    /// as often as it is lacking; past that, a class it lacks for a kingdom full. The piece may cost what is left once
    /// the classes still lacking are paid for at their cheapest, and no more than its class's share of what is left,
    /// in proportion to the classes' average prices; past the aim, no more than its class's average and the bit left
    /// over. Among the pieces of the class that fit, one drawn by <see cref="WeightOf"/>; if none fits, another class
    /// lacking; the class's cheapest, whatever it costs, while the army has fewer than the game's minimum of it.
    /// </summary>
    internal static GrandPiece? Next(
        [Required] QuickGame game,
        [Required] ImmutableList<GrandPiece> eligible,
        [Required] ImmutableList<GrandPiece> army,
        int left,
        [Required] Random random,
        ImmutableHashSet<PieceClass>? present = null
    )
    {
        ImmutableList<GrandPiece> fitting = Fitting(eligible, army, RoomOf(game), game.Edition);
        ImmutableList<PieceClass> aimed = Lacking(game, fitting, army, AimOf(game), present);
        // an army of a family's pieces, which the budget, reckoned from every class, may not buy a whole aim of, buys
        // as one past its aim does, keeping nothing back for pieces it may never afford (Proposed, 2026-11-20)
        bool isPast = aimed.IsEmpty || present is not null;
        ImmutableList<PieceClass> wanted = isPast ? Lacking(game, fitting, army, game.Cells, present) : aimed;
        if (army.Count >= game.Cells || wanted.IsEmpty)
        {
            return null;
        }

        PieceClass drawn = wanted[random.Next(wanted.Count)];
        double prices = wanted.Sum(each => Math.Max(1, Average(eligible, each)));
        return wanted
            .Distinct()
            .OrderBy(each => each != drawn)
            .Select(Of)
            .FirstOrDefault(found => found is not null);

        GrandPiece? Of(PieceClass pieceClass)
        {
            ImmutableList<PieceClass> later = wanted.Remove(pieceClass);
            int reserve = isPast ? 0 : later.Sum(each => Cheapest(fitting, each).Value);
            int share = (int)Math.Round(
                left * Math.Max(1, Average(eligible, pieceClass)) / prices,
                MidpointRounding.AwayFromZero
            );
            int most = Math.Min(left - reserve, Math.Max(Cheapest(fitting, pieceClass).Value, share));
            ImmutableList<GrandPiece> fits =
            [
                .. fitting.Where(piece =>
                    piece.Class == pieceClass
                    && piece.Value <= most
                    && (!isPast || piece.Value <= MostAdded(eligible, pieceClass))
                ),
            ];
            bool isWanting =
                army.Count(piece => piece.Class == pieceClass) < game.Minimums.GetValueOrDefault(pieceClass);
            return (fits.IsEmpty, isWanting) switch
            {
                (false, _) => Weighed(fits, Typical(eligible, pieceClass), random),
                (true, true) => Cheapest(fitting, pieceClass),
                _ => null,
            };
        }
    }

    /// <summary>
    /// Each piece <paramref name="army"/> lacks of the composition of <paramref name="count"/> it aims at, a class
    /// once for each piece of it lacking, in the order of <see cref="Classes"/>; but no class none of whose pieces it
    /// may still be given.
    /// </summary>
    private static ImmutableList<PieceClass> Lacking(
        QuickGame game,
        ImmutableList<GrandPiece> fitting,
        ImmutableList<GrandPiece> army,
        int count,
        ImmutableHashSet<PieceClass>? present
    ) =>
    [
        .. TargetOf(game, count, present)
            .OrderBy(target => Classes.IndexOf(target.Key))
            .Where(target => fitting.Any(piece => piece.Class == target.Key))
            .SelectMany(target => Enumerable.Repeat(
                target.Key,
                Math.Max(0, target.Value - army.Count(piece => piece.Class == target.Key))
            )),
    ];

    /// <summary>
    /// The median price of the class's pieces among <paramref name="eligible"/>, those that may promote into a leader
    /// aside (<see cref="Average"/>), as the outliers would skew an average (E3); 1, if none.
    /// </summary>
    private static double Typical(ImmutableList<GrandPiece> eligible, PieceClass pieceClass)
    {
        ImmutableList<int> prices =
        [
            .. eligible
                .Where(piece => piece.Class == pieceClass && !IsLeaderBound(piece))
                .Select(piece => piece.Value)
                .Order()
                .DefaultIfEmpty(1),
        ];
        return (prices[(prices.Count - 1) / 2] + prices[prices.Count / 2]) / 2.0;
    }

    /// <summary>
    /// How often, relatively, a piece is drawn among its class (E3): most often at <see cref="CommonestShare"/> of the
    /// class's <paramref name="typical"/> price, less often the further its price lies from that, either way, by the
    /// ratio of the two - so that dear pieces are seldom drawn, and the cheapest seldom too.
    /// </summary>
    internal static double WeightOf([Required] GrandPiece piece, double typical)
    {
        double away = Math.Log(Math.Max(1, piece.Value) / (typical * CommonestShare));
        return Math.Exp(-(away * away) / (2 * Spread * Spread));
    }

    /// <summary>One of <paramref name="pieces"/>, drawn by <see cref="WeightOf"/>.</summary>
    private static GrandPiece Weighed(ImmutableList<GrandPiece> pieces, double typical, Random random)
    {
        ImmutableList<double> weights = [.. pieces.Select(piece => WeightOf(piece, typical))];
        double at = random.NextDouble() * weights.Sum();
        for (int index = 0; index < pieces.Count - 1; index++)
        {
            at -= weights[index];
            if (at < 0)
            {
                return pieces[index];
            }
        }

        return pieces[^1];
    }

    /// <summary>
    /// The most a slot may spend: what the budget leaves once the slots still to fill are paid for at their classes'
    /// cheapest. From <see cref="EvenSpreadEdition"/> on, only the slot's even share of that, past its class's
    /// cheapest (Stated 2026-11-13, the user's answer to OQ13: so that the first slots do not spend what the last
    /// needed, and dear pieces, like the Amazon, come very seldom).
    /// </summary>
    private static int LeftFor(
        QuickGame game,
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        PieceClass pieceClass,
        ImmutableList<PieceClass> later
    )
    {
        int spent = army.Sum(piece => piece.Value);
        int left = game.Budget - spent - later.Sum(each => Cheapest(eligible, each).Value);
        int cheapest = Cheapest(eligible, pieceClass).Value;
        return game.Edition switch
        {
            < EvenSpreadEdition => left,
            < FormulaEdition => cheapest + (Math.Max(0, left - cheapest) / (later.Count + 1)),
            _ => Math.Min(
                left,
                Math.Max(
                    cheapest,
                    (int)Math.Round(
                        (game.Budget - spent) * Average(eligible, pieceClass)
                            / later.Add(pieceClass).Sum(each => Average(eligible, each)),
                        MidpointRounding.AwayFromZero
                    )
                )
            ),
        };
    }

    /// <summary>Every square of the game's kingdom a palace may stand about.</summary>
    private static ImmutableList<ArmySquare> Centres(QuickGame game) =>
        game.HasEdgePalaces
            ? PalaceSquares.EdgeCentres(game.Kingdom, game.Width, game.Height)
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
    /// place it: on its squares furthest back, then nearest the throne's file, then leftmost (Proposed). If its palace
    /// is already <paramref name="placed"/> (a draft's palace, placed before the army is filled at random), the throne
    /// is any square of that palace, at random, and the pieces a palace holds stand in that palace.
    /// </summary>
    public static ImmutableList<ArmyPiece> Layout(
        [Required] QuickGame game,
        [Required] ImmutableList<GrandPiece> army,
        [Required] Random random,
        ImmutableList<ArmySquare>? placed = null
    )
    {
        ImmutableList<ArmySquare> backToFront = game.Kingdom;
        int crowned = army.FindIndex(piece => piece.Class == Leader);
        bool Held(GrandPiece piece, int index) => IsHeld(piece, game.Edition, index != crowned);
        ImmutableList<GrandPiece> others = Shuffled(
            army.Where((piece, index) => index != crowned && piece.Class != Infantry && !Held(piece, index)),
            random
        );
        ArmySquare? kept = placed is null ? null : ThroneIn(placed, game, random);
        int dealt = kept is null && game.HasNeutralZone
            ? random.Next(Math.Min(game.KingdomWidth - 2, others.Count) + 1) // never the inner side's square
            : 0;
        ImmutableList<GrandPiece> infantry =
            Shuffled(army.Where(piece => piece.Class == Infantry && !IsHeld(piece)), random);
        ImmutableList<GrandPiece> held = Shuffled(army.Where(Held), random);
        ArmySquare seat = kept ?? backToFront[dealt];
        ImmutableList<ArmySquare> palace = held.IsEmpty
            ? []
            : [.. Nearest(placed ?? PalaceAbout(game, seat), game, seat).Take(held.Count)];
        int throne = kept is null
            ? dealt
            : Math.Min(
                others.Count,
                backToFront.TakeWhile(square => square != seat).Count(square => !palace.Contains(square))
            );
        ImmutableList<GrandPiece> rear = others.Insert(throne, army[crowned]);
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
            .. held.Zip(palace).Select(inPalace => At(inPalace.First, inPalace.Second)),
        ];
        ImmutableList<ArmyPiece> balanced = Enumerable.Range(0, laid.Count).Aggregate(
            laid,
            (spread, index) => game.Edition >= ThroneKeptEdition && ClassOf(spread[index]) == Leader
                ? spread
                : Balanced(spread, index)
        );
        return game.Edition >= CompositionEdition ? Crowned(balanced, seat) : balanced;
    }

    /// <summary>
    /// The army, the leader on <paramref name="seat"/> first: the throne is where an army's first leader starts, and
    /// from <see cref="CompositionEdition"/> on an army may have several.
    /// </summary>
    private static ImmutableList<ArmyPiece> Crowned(ImmutableList<ArmyPiece> army, ArmySquare seat)
    {
        int at = army.FindIndex(piece => piece.Column == seat.Column && piece.Row == seat.Row);
        return [army[at], .. army.RemoveAt(at)];
    }

    /// <summary>The palace about <paramref name="seat"/>, as the program would place it.</summary>
    private static ImmutableList<ArmySquare> PalaceAbout(QuickGame game, ArmySquare seat) =>
        PalaceSquares.Window(PalaceSquares.CentreAbout(Centres(game), seat));

    /// <summary>
    /// The squares of <paramref name="palace"/> in the kingdom, the throne aside: those furthest back, then nearest the
    /// throne's file, then leftmost.
    /// </summary>
    private static IEnumerable<ArmySquare> Nearest(IEnumerable<ArmySquare> palace, QuickGame game, ArmySquare seat) =>
        palace
            .Where(square => square != seat && game.Kingdom.Contains(square))
            .OrderBy(square => square.Row)
            .ThenBy(square => Math.Abs(square.Column - seat.Column))
            .ThenBy(square => square.Column);

    /// <summary>
    /// A square of the palace in the kingdom, at random, off its borders: the throne, in a palace placed before
    /// (Stated 2026-11-19: a throne may be anywhere in its palace; the user's word of 2026-11-20: never, as the
    /// program deals it, on the kingdom's front rank or its inner side, even with the palace there).
    /// </summary>
    private static ArmySquare ThroneIn(ImmutableList<ArmySquare> palace, QuickGame game, Random random)
    {
        ImmutableList<ArmySquare> inside =
            [.. palace.Where(square => game.Kingdom.Contains(square) && IsInland(square, game))];
        return inside[random.Next(inside.Count)];
    }

    /// <summary>
    /// Whether the square, as seat 0 sees its kingdom in the board's corner, is off the kingdom's two borders with
    /// the rest of the board: its front rank and its inner side. Every kingdom being at least 3 × 3, every palace in
    /// it has such a square.
    /// </summary>
    private static bool IsInland(ArmySquare square, QuickGame game) =>
        square.Row < game.KingdomDepth - 1 && square.Column < game.KingdomWidth - 1;

    /// <summary>Whether the piece is one a palace holds, and not the leader, who holds the throne.</summary>
    internal static bool IsHeld(GrandPiece piece) =>
        piece.Class != Leader && PalaceConfinement.Confined.Contains(piece.Id);

    /// <summary>
    /// Whether the piece is one a palace holds, in an army that has <paramref name="hasLeader"/> a leader on its throne
    /// already: from <see cref="HeldLeadersEdition"/> on, a leader a palace holds too, but for the first, which holds
    /// the throne.
    /// </summary>
    internal static bool IsHeld(GrandPiece piece, int edition, bool hasLeader) =>
        IsHeld(piece)
        || (edition >= HeldLeadersEdition
            && hasLeader
            && piece.Class == Leader
            && PalaceConfinement.Confined.Contains(piece.Id));

    /// <summary>
    /// The pieces <paramref name="army"/> may still be given: those a palace holds only while its palace has room.
    /// </summary>
    private static ImmutableList<GrandPiece> Fitting(
        ImmutableList<GrandPiece> eligible,
        ImmutableList<GrandPiece> army,
        int room,
        int edition
    )
    {
        int held = army
            .Where((piece, index) => IsHeld(piece, edition, army.Take(index).Any(each => each.Class == Leader)))
            .Count();
        bool hasLeader = army.Any(piece => piece.Class == Leader);
        return held < room ? eligible : [.. eligible.Where(piece => !IsHeld(piece, edition, hasLeader))];
    }

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
            ?
            [
                .. Fitting(eligible, army, RoomOf(game), game.Edition)
                    .Where(piece => piece.Class != Leader && piece.Value <= left)
                    .Where(piece => game.Edition < FormulaEdition || piece.Value <= MostAdded(eligible, piece.Class)),
            ]
            : [];
        return affordable.IsEmpty ? army : Filled(eligible, army.Add(Drawn(affordable, random)), game, random);
    }

    private static ArmyPiece At(GrandPiece piece, ArmySquare square) => new(piece.Id, square.Column, square.Row);
}
