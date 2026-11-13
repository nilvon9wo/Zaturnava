using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

/// <summary>
/// Zaturnava's base rules. So far: a roll-off for who goes first, whose winning die is the opening
/// move (rules.md §4, §11); then turns of a deliberate move, a pass, or a roll of the dice (§4);
/// captures scored by piece, with the throne bonuses, the empire and commanding every piece (§5); command
/// of other armies through thrones and Raja-less allies, an ally's turn taken with its consent, and turns
/// shared with an enemy Raja on one's throne (§6); promotion and ransom (§8); escape on doubles (§7); and
/// the end of the game (§10). Boat triumph follows; docs/rules.md lists what is implemented.
/// </summary>
public sealed class Chaturaji2027Rules : IRulesApplying, IPositionMeasuring
{
    public const string RulesetId = "chaturaji-2027";

    /// <summary>The dice each seat rolls to see who goes first.</summary>
    public const int RollOffDice = 1;

    /// <summary>The dice rolled on a turn.</summary>
    public const int TurnDice = 2;

    /// <summary>How many rounds no one may move before the game ends on points.</summary>
    public const int DefaultIdleRounds = 3;

    /// <summary>
    /// How many rounds may pass with no piece taken before the game ends on points: fifty, each seat's fifty
    /// turns, as chess counts fifty moves (Stated 2026-11-06).
    /// </summary>
    public const int DefaultQuietRounds = 50;

    /// <summary>How many rounds every turn may end where the game has been before, ere it ends on points.</summary>
    public const int DefaultRepeatRounds = 3;

    /// <summary>The timer on a seat whose turn is over, while it may still offer ransoms.</summary>
    public const string RansomWindowTimer = "ransom-window";

    /// <summary>The timer on choosing where a piece going home from a ransom goes.</summary>
    public const string ReturnTimer = "ransom-return";

    /// <summary>The timer on a seat offered a ransom; declining is its default.</summary>
    public const string RansomAnswerTimer = "ransom-answer";

    /// <summary>How many turns in a row a seat away may have passed, by default, before it gives them up.</summary>
    public const int DefaultAwayTurns = 3;

    /// <summary>The timer on answering a proposal to end the game.</summary>
    public const string EndAnswerTimer = "end-answer";

    private const int OpeningTurn = 1;

    private readonly Chaturaji2027Variant _variant;
    private readonly SquareGridTopology _grid;
    private readonly ArmyMeasures _measures;
    private readonly MoveGenerator _moves;
    private readonly DiceProfile _dice;
    private readonly int _idleRounds;
    private readonly int _quietRounds;
    private readonly int _repeatRounds;
    private readonly int _awayTurns;
    private readonly PawnPromotion _pawnPromotion;
    private readonly ThroneControl _control;
    private readonly Command _command;
    private readonly EveryPiece _everyPiece;
    private readonly Farming _farming;
    private readonly Ransom _ransom;
    private readonly StartRowMoves _startRows;
    private readonly Castling _castling;
    private readonly FlyingCapture _flying;
    private readonly Custody _custody;
    private readonly Flight _flight;
    private readonly Mercenaries _mercenaries;
    private readonly PalaceSteps _palaceSteps;
    private readonly FaceOff _faceOff;
    private readonly MoveDiagrams _diagrams;
    private readonly ArmyDrafting? _drafting;

    /// <summary>The rules on each board size a game has been set to, made once each.</summary>
    private readonly ConcurrentDictionary<BoardSize, Chaturaji2027Rules?> _sized = new();

    public Chaturaji2027Rules()
        : this(SafetyNets.Default)
    {
    }

    /// <summary>Chaturaji 2027's own rules, the game ending on points at <paramref name="nets"/>' limits.</summary>
    public Chaturaji2027Rules([Required] SafetyNets nets)
        : this(
            DiceProfile.WeightedSixSided,
            nets.IdleRounds,
            PawnPromotion.OtherArmies,
            quietRounds: nets.QuietRounds,
            repeatRounds: nets.RepeatRounds
        )
    {
    }

    /// <summary>
    /// The rules as configured; <paramref name="awayTurns"/> is how many turns in a row a seat away may have
    /// passed before it gives them up; <paramref name="variant"/> what the game is played with, Chaturaji 2027's
    /// own if none; <paramref name="quietRounds"/> how many rounds may pass with no piece taken;
    /// <paramref name="repeatRounds"/> how many may each turn end where the game has been before.
    /// </summary>
    public Chaturaji2027Rules(
        [Required] DiceProfile dice,
        [Range(1, int.MaxValue)] int idleRounds,
        PawnPromotion pawnPromotion,
        [Range(1, int.MaxValue)] int awayTurns = DefaultAwayTurns,
        Chaturaji2027Variant? variant = null,
        [Range(1, int.MaxValue)] int quietRounds = DefaultQuietRounds,
        [Range(1, int.MaxValue)] int repeatRounds = DefaultRepeatRounds
    )
    {
        this._variant = variant ?? Chaturaji2027Variant.Standard;
        this._quietRounds = quietRounds;
        this._repeatRounds = repeatRounds;
        this._grid = new SquareGridTopology(this._variant.Side, this._variant.Side);
        this._awayTurns = awayTurns;
        this._moves = new MoveGenerator(this._grid, this._variant.Kinds);
        this._diagrams = new MoveDiagrams(
            this._variant.Kinds,
            this._variant.RiverBound,
            this._variant.PalaceSteppers,
            this._variant.ForwardPalaceSteppers
        );
        this._dice = dice;
        this._idleRounds = idleRounds;
        this._pawnPromotion = pawnPromotion;
        this.Thrones = this._variant.Thrones(this._grid);
        this._control = new ThroneControl(this.Thrones, this._variant.Leaders);
        this._command = new Command(this._control);
        this._everyPiece = new EveryPiece(this._control, this._variant.Scoring.EveryPieceBonus);
        this._farming = new Farming(this._control, this._moves, this._variant.Scoring.ThroneEntryBonus);
        this._ransom = new Ransom(
            this._grid,
            this._variant.InitialPosition(this._grid),
            this._variant.Leaders,
            this._variant.Scoring
        );
        this._measures = new ArmyMeasures(
            this._grid,
            this._variant.Promotables,
            (state, cell, piece) => this.MovesFrom(state, cell, piece, piece.Owner),
            this.AttacksOf,
            this._control,
            this._ransom.HomeOf,
            this.WorthOf
        );
        this._startRows = new StartRowMoves(
            this._grid,
            this._variant.InitialPosition(this._grid),
            this._variant.StartRowPowers,
            this._variant.StartRows
        );
        this._castling = new Castling(
            this._grid,
            this._variant.Castlers,
            this._variant.CastlingRooks,
            this.AttackedAgainst
        );
        this._flying = new FlyingCapture(this._grid, this._variant.FlyingCapturers, this._variant.Leaders);
        this._custody = new Custody(this._grid, this._variant.Custodians);
        this._flight = new Flight(this._grid, this._variant.Side, this._variant.Fleers);
        this._mercenaries = new Mercenaries(
            this._grid,
            this._variant.Mercenaries,
            this._variant.Leaders,
            this._variant.PointsIn,
            this._variant.InitialPosition(this._grid),
            this._control
        );
        this._palaceSteps = new PalaceSteps(
            this._grid,
            this._variant.PalaceSteppers,
            this._variant.ForwardPalaceSteppers
        );
        this._faceOff = new FaceOff(this._grid, this._variant.FaceOffCallers, this._variant.Leaders);
        this._drafting = this._variant.Drafting is { } drafting
            ? new ArmyDrafting(
                drafting,
                this._grid,
                this._variant.Side,
                (edition, kind) => this._variant.PointsIn(edition)[kind],
                this.RollOffDie
            )
            : null;
    }

    public string Id => this._variant.Id;

    public string Name => this._variant.Name;

    public string GameId => this._variant.Game?.Id ?? this.Id;

    public string GameName => this._variant.Game?.Name ?? this.Name;

    public string? Size => this._variant.Game?.Size;

    public bool IsPreview => this._variant.Game?.IsPreview == true;

    public ICellNavigating Topology => this._grid;

    public BoardSize? SizableBoard => this._variant.Resized is null ? null : this._variant.Board;

    public BoardSize? Board => this._variant.Board;

    /// <summary>The rules on a board of <paramref name="board"/>'s size, as the variant resizes, alike else.</summary>
    public IRulesApplying? Sized([Required] BoardSize board) =>
        this._variant.Resized is { } resized
            ? this._sized.GetOrAdd(
                board,
                size => resized(size) is { } variant
                    ? new Chaturaji2027Rules(
                        this._dice,
                        this._idleRounds,
                        this._pawnPromotion,
                        this._awayTurns,
                        variant,
                        this._quietRounds,
                        this._repeatRounds
                    )
                    : null
            )
            : null;

    public ImmutableDictionary<SeatId, CellId> Thrones { get; }

    /// <summary>Every seat rolls a die, in seat order, to see who goes first.</summary>
    public GameState CreateInitialState()
    {
        ImmutableList<Seat> seats = Chaturaji2027Setup.Seats();
        GameState state = new(
            this._variant.InitialPosition(this._grid),
            seats,
            Chaturaji2027Setup.Teams(),
            seats[0].Id,
            0,
            seats.ToImmutableDictionary(seat => seat.Id, _ => 0),
            []
        );
        return state with { RollOff = [.. seats.Select(seat => seat.Id)], AwaitedDice = this.RollOffDie };
    }

    public bool IsDealt => this._variant.Dealer is not null;

    public ImmutableDictionary<SeatId, CellId> ThronesOf([Required] GameState state) => this._control.ThronesOf(state);

    /// <summary>
    /// The game dealt its own armies by <paramref name="seed"/> - or one army, the same for every seat, if
    /// <paramref name="areAlike"/> - if the variant deals them: each leader's start its throne, and every piece's its
    /// home; on its own terrain too, if <paramref name="dealsTerrain"/> and the variant deals terrain; each army with
    /// its palace about its throne, if <paramref name="dealsPalaces"/> and its kingdom has room for one
    /// (<see cref="ArmyDrafting.HasPalaces"/>), or, if <paramref name="dealsEdgePalaces"/>, room for one standing
    /// partly outside it; and the pieces a palace holds in it, if <paramref name="dealsPalacePieces"/> too; from the
    /// pieces as they were at <paramref name="edition"/>; else as it is.
    /// </summary>
    public GameState Deal(
        [Required] GameState initial,
        int seed,
        bool areAlike = false,
        bool dealsTerrain = false,
        bool dealsPalaces = false,
        bool dealsPalacePieces = false,
        bool dealsEdgePalaces = false,
        int edition = 0
    )
    {
        if (this._variant.Dealer is not { } dealer)
        {
            return initial;
        }

        int terrain = dealsTerrain && this._variant.TerrainDealer is { } terrains ? terrains(seed) : 0;
        ArmyDrafting? palaced = dealsPalaces && this._drafting is { } drafting && drafting.HasPalaces(dealsEdgePalaces)
            ? drafting
            : null;
        bool hasPalacePieces = dealsPalacePieces && palaced is not null;
        Chaturaji2027Variant dealt = this._variant with
        {
            Start = dealer(
                new ArmyDealing(seed, areAlike, terrain, hasPalacePieces)
                {
                    HasEdgePalaces = palaced is not null && dealsEdgePalaces,
                    Edition = edition,
                }
            ),
        };
        Position position = dealt.InitialPosition(this._grid);
        ImmutableDictionary<SeatId, CellId> thrones = dealt.Thrones(this._grid);
        return initial with
        {
            Position = position,
            Thrones = thrones,
            Palaces = palaced?.PalacesAround(thrones, dealsEdgePalaces),
            HasPalacePieces = hasPalacePieces,
            HasEdgePalaces = palaced is not null && dealsEdgePalaces,
            Edition = edition,
            Start = position,
            Terrain = terrain,
        };
    }

    public bool IsDraftable => this._drafting is not null;

    public int? DraftBudget => this._variant.Drafting?.BudgetIn(int.MaxValue);

    public int? DraftBudgetLimit => this._drafting?.Limit;

    /// <summary>
    /// The game <paramref name="dealt"/>, its armies to be drafted by its players (<see cref="ArmyDrafting"/>) as
    /// <paramref name="settings"/> set, if the variant lets them; else as it is.
    /// </summary>
    public GameState Drafted([Required] GameState dealt, int seed, DraftSettings? settings = null) =>
        this._drafting?.Begun(dealt, seed, settings ?? DraftSettings.Default) ?? dealt;

    /// <summary>While the armies are drafted, every kind offered on the game's terrain.</summary>
    public ImmutableList<PieceKindId> DraftOffered([Required] GameState state) =>
        this.DraftingOf(state)?.Offered(state) ?? [];

    /// <summary>The variant's lines, such as its rivers; then each palace's (<see cref="PalaceLines"/>).</summary>
    public ImmutableList<BoardLine> LinesOf([Required] GameState state) =>
    [
        .. this._variant.Lines?.Invoke(state, this._grid) ?? [],
        .. PalaceLines.Of(state, this._grid),
    ];

    public string? TerrainOf([Required] GameState state) => this._variant.Terrain?.Invoke(state);

    /// <summary>The deciding seat's choices: while the armies are drafted, those of the draft.</summary>
    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        (state.DecidingSeat, state.AwaitedDice, this.DraftingOf(state)) switch
        {
            ({ } seat, _, { } drafting) => drafting.ChoicesOf(state, seat),
            ({ } seat, null, _) => this.ChoicesOf(state, seat),
            _ => [],
        };

    /// <summary>
    /// The action, and what follows from it; while the armies are drafted, the draft's, before any game is played to
    /// end or count.
    /// </summary>
    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        this.DraftingOf(state) is { } drafting ? drafting.Applied(state, action) : this.Played(state, action);

    /// <summary>The draft, while the game's armies are being drafted.</summary>
    private ArmyDrafting? DraftingOf(GameState state) => state.Draft is null ? null : this._drafting;

    private Transition Played(GameState state, GameAction action) =>
        this._command.Told(
            state,
            this._everyPiece.Scored(
                this.Unstalled(
                    GameEnd.AfterGivingUp(
                        GameEnd.AfterRepeats(
                            GameEnd.AfterQuietTurns(
                                state,
                                GameEnd.Unidled(state, this.Acted(state, action)),
                                this._quietRounds
                            ),
                            this._repeatRounds
                        ),
                        this._variant
                    )
                )
            )
        );

    /// <summary>The action itself, before the counts and endings that follow any action.</summary>
    private Transition Acted(GameState state, GameAction action) =>
        Attended(
            action,
            action switch
            {
                DiceAction dice => this.Rolled(state, dice),
                OfferAction offer => this.Offered(state, offer),
                ProposeEndAction propose => this.Proposed(state, propose),
                RejoinAction rejoin => this.Rejoined(state, rejoin),
                VoteAwayAction vote => this.Voted(state, vote),
                PassAwayAction pass => this.PassedAway(state, pass),
                FaceOffAction faceOff => this.FacedOff(state, faceOff),
                MoveAction move when state.DecidingSeat != move.Actor => this.TakenInPassing(state, move),
                _ => this.Decided(state, action),
            }
        );

    public bool MayOffer([Required] GameState state) => Ransom.MayOffer(state);

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        Ransom.MayGoBack(state, prisoner);

    /// <summary>As <see cref="Agreement.MayPropose"/> says; never while the armies are drafted.</summary>
    public bool MayProposeEnd([Required] GameState state, SeatId seat) =>
        state.Draft is null && Agreement.MayPropose(state, seat);

    /// <summary>
    /// A seat may rejoin as <see cref="Agreement.MayRejoin"/> says; and a retired one, once it has a piece besides
    /// its Raja on the board again, to play it itself (rules.md §10).
    /// </summary>
    public bool MayRejoin([Required] GameState state, SeatId seat) =>
        Agreement.MayRejoin(state, seat)
        || (state.Outcome is null
            && state.Proposal is null
            && state.Retired.Contains(seat)
            && this._control.HasOtherThanRaja(state, seat));

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        Away.MayVote(state, voter, absent);

    /// <summary>
    /// Each leader face-off <paramref name="seat"/> may call (<see cref="FaceOff"/>), with a caller of an army it
    /// commands; and, while it does not decide, each <i>en passant</i> it may take with a piece of such an army, for
    /// no die (Stated 2026-11-06), as on its own turn it would be an ordinary move. None while the game waits on
    /// something else first (Proposed): dice, a promotion, prisoners going home, an offer, a proposal to end, or the
    /// roll for who goes first; nor once it is over, or the seat retired. While the armies are drafted all at once, a
    /// seat whose turn it is not has its choices of the draft (<see cref="ArmyDrafting.ChoicesOf"/>), and only those.
    /// </summary>
    public ImmutableList<GameAction> ChancesOf([Required] GameState state, SeatId seat) =>
        state.Draft switch
        {
            { IsSimultaneous: true } when seat != state.DecidingSeat!.Value => this._drafting!.ChoicesOf(state, seat),
            null => this.PlayChancesOf(state, seat),
            _ => [],
        };

    private ImmutableList<GameAction> PlayChancesOf(GameState state, SeatId seat) =>
        state.Outcome is null
        && state.AwaitedDice is null
        && state.PromotionDue is null
        && state.Returning is null
        && state.Offer is null
        && state.Proposal is null
        && state.RollOff.IsEmpty
        && !state.Retired.Contains(seat)
            ? this.ChancesFor(state, seat, this._control.ArmiesOf(state with { ActingFor = null }, seat))
            : [];

    private ImmutableList<GameAction> ChancesFor(GameState state, SeatId seat, ImmutableHashSet<SeatId> armies) =>
    [
        .. this._faceOff.ChancesOf(state, seat, armies),
        .. state.DecidingSeat == seat
            ? []
            : state.Position.Occupants
                .Where(occupant => armies.Contains(occupant.Value.Owner))
                .SelectMany(occupant => this._startRows.InPassingFrom(
                    state,
                    occupant.Key,
                    occupant.Value,
                    seat,
                    target => this.IsCapturable(state, target, seat)
                ))
                .Where(move => this.MayGo(state, move))
                .Select(move => new MoveAction(seat, move.From, move.To)),
    ];

    /// <summary>
    /// A seat choosing where a piece goes home, answering an offer, or offering once its turn is over, has
    /// only so long.
    /// </summary>
    public DecisionTimer? TimerOf([Required] GameState state) =>
        (state.Proposal, state.Returning, state.DecidingSeat) switch
        {
            ({ }, _, { } seat) => new DecisionTimer(EndAnswerTimer, new AnswerEndAction(seat, false)),
            (_, { }, _) => new DecisionTimer(ReturnTimer, this._ransom.PlacesOf(state)[0]),
            _ => RansomTimerOf(state),
        };

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    private static DecisionTimer? RansomTimerOf(GameState state) =>
        (state.Offer, state.IsRansomWindow, state.DecidingSeat) switch
        {
            ({ } offer, _, _) => new DecisionTimer(RansomAnswerTimer, new DeclineAction(offer.To)),
            (_, true, { } seat) => new DecisionTimer(RansomWindowTimer, new PassAction(seat)),
            _ => null,
        };

    /// <summary>A piece is worth what taking it scores (rules.md §5).</summary>
    public int WorthOf(PieceKindId kind) => this._variant.Points[kind];

    public int KeepingWorthOf(PieceKindId kind) => this._variant.Keeping[kind];

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        this._ransom.PointsOf(state, offer);

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
    [
        .. state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat)
            .SelectMany(occupant => this._moves.AttacksFrom(state.Position, occupant.Key, state.SeatOf(seat))
                .Concat(this._flying.AttacksFrom(state, occupant.Key, occupant.Value))
                .Concat(this._palaceSteps.AttacksFrom(state, occupant.Key, occupant.Value))
            )
            .Distinct()
            .Where(attack => this.MayGo(state, new Move(attack.Attacker, attack.From, attack.Target, null))),
    ];

    /// <summary>The lead that wins by giving up its turns (rules.md §10), as the variant scores it.</summary>
    public int ClearLead => this._variant.Scoring.ClearLead;

    /// <summary>The dice of a turn, each letting the kind move as often as its faces name it.</summary>
    public double MovesPerTurn(PieceKindId kind) =>
        TurnDice * (double)this._dice.Kinds.Count(face => face.Value.Contains(kind)) / this._dice.Sides;

    public bool IsLeader(PieceKindId kind) => this._control.IsLeader(kind);

    public bool IsHeavy(PieceKindId kind) => this._variant.Heavies.Contains(kind);

    /// <summary>
    /// What a bot may weigh beyond what every ruleset has: how far the army's Pawns have come and how near they are
    /// to promoting, its pieces' reach where they stand and how much of it goes back, how safe its leaders and thrones
    /// are, and how many other armies' pieces it commands (<see cref="ArmyMeasures"/>).
    /// </summary>
    public ImmutableList<string> Measures => ArmyMeasures.Names;

    public ImmutableList<double> MeasuresOf([Required] GameState state, SeatId seat) =>
        this._measures.Of(state, seat);

    public string NameOf(PieceKindId kind) => this._variant.Names[kind];

    public PieceMark? MarkOf(PieceKindId kind) => this._variant.Marks.GetValueOrDefault(kind);

    /// <summary>
    /// How a piece of <paramref name="kind"/> moves, drawn as well (<see cref="MoveDiagrams"/>), and its class and
    /// what holds it back as the variant tells them, with the powers these rules give it first: those every piece of
    /// its sort has, and its own - and its own promotion, where these rules play it, and where it may be taken.
    /// </summary>
    public PieceGuide GuideOf(PieceKindId kind)
    {
        PieceGuide told = this._variant.Guides.GetValueOrDefault(kind) ?? new PieceGuide(this._variant.Kinds[kind]);
        PieceGuide drawn = told with
        {
            Powers = [.. this.PowersOf(kind), .. told.Powers],
            SpecialPowers = [.. this.SpecialPowersOf(kind), .. told.SpecialPowers],
            Diagram = this._diagrams.Of(kind),
        };
        return this._variant.OwnPromotions.TryGetValue(kind, out OwnPromotion? own)
            ? drawn with
            {
                PromotesTo = own.Into,
                IsPromotionPlayed = true,
                PromotesWhere = own.Where ?? (own.Ranks == 1 ? "on the last row" : $"in the far {own.Ranks} rows"),
            }
            : drawn;
    }

    public string NameOf(SeatId seat) => Chaturaji2027Setup.SeatNames[seat.Value];

    /// <summary>The kinds the die lets move: "Horse", or "Pawn or Raja"; "Nothing" for a value no die shows.</summary>
    public string NameOfDie(int value)
    {
        ImmutableList<string> names = [.. this._dice.KindsOf(value).Select(this.NameOf).Order(StringComparer.Ordinal)];
        return (this._variant.DieNames.TryGetValue(value, out string? name), names.IsEmpty) switch
        {
            (true, _) => name!,
            (_, true) => "Nothing",
            _ => string.Join(" or ", names),
        };
    }

    /// <summary>
    /// Whether the die moves or promotes anything now. Letting a piece escape does not count: that takes both
    /// dice of doubles, not either die, so a die that only lets a captured piece escape moves nothing.
    /// </summary>
    public bool MayUseDie([Required] GameState state, int value) =>
        state.DecidingSeat is not null && this.MayMove(state, this._dice.KindsOf(value).Contains);

    private DiceRequest RollOffDie => new(RollOffDice, this._dice.Sides);

    /// <summary>
    /// The powers these rules give every piece of the sort a piece of <paramref name="kind"/> is, for people: every
    /// leader's, every Pawn's. A piece a palace holds is promoted at the far edge as any of its sort, but only if it
    /// ever gets out (the user's word of 2026-11-13), so its card says so.
    /// </summary>
    private IEnumerable<string> PowersOf(PieceKindId kind)
    {
        bool isHeld = this._variant.Drafting?.PalaceBound.Contains(kind) ?? false;
        return new (bool Has, string Power)[]
            {
                (this._variant.Leaders.Contains(kind),
                    "Leads its army; standing on another army's throne, it takes command of that army too."),
                (this._variant.Promotables.Contains(kind) && !isHeld,
                    "Promoted, on reaching the far edge, into a captured piece."),
                (this._variant.Promotables.Contains(kind) && isHeld,
                    "Promoted, should it ever leave its palace and reach the far edge, into a captured piece."),
                (this._variant.Pawns.Contains(kind),
                    "Swaps places with an allied Pawn straight ahead of it, the two back to back."),
            }
            .Where(power => power.Has)
            .Select(power => power.Power);
    }

    /// <summary>The powers these rules give <paramref name="kind"/> alone among its sort, for people.</summary>
    private IEnumerable<string> SpecialPowersOf(PieceKindId kind)
    {
        string rooks = string.Join(
            " or ",
            this._variant.CastlingRooks.Select(this.NameOf).Order(StringComparer.Ordinal)
        );
        return new (bool Has, string Power)[]
            {
                (this._variant.StartRowPowers.Contains(kind),
                    "From its start rows, its first move may go two cells straight ahead."),
                (this._variant.StartRowPowers.Contains(kind),
                    "Takes en passant a piece that just went two cells past it, even out of turn, until its own turn "
                    + "ends."),
                (this._variant.Castlers.Contains(kind),
                    $"Castles with its own {rooks}: two cells towards it, the {rooks} jumping over to its other side."),
                (this._variant.FlyingCapturers.Contains(kind),
                    "Flying general: when it and an enemy leader stand in the same row or column, with nothing "
                    + "between them, however far apart, it may fly the whole way and take that leader, as its move. "
                    + "For example, from e1 it takes a leader on e9 if e2 to e8 are empty."),
                (this._variant.FaceOffCallers.Contains(kind),
                    "Face-off: when it and an enemy leader stand in the same row or column, with nothing between "
                    + "them, its player may call a face-off, even out of turn, for as long as they so face each "
                    + "other: both leaders are taken at once, each by the other's side, and both sides score. "
                    + "Neither piece moves."),
                (this._variant.Custodians.Contains(kind),
                    "Takes by custody: it takes nothing where it lands. Instead, when it moves next to an enemy "
                    + "piece, in the same row or column, and just beyond that enemy, on the same line, stands any "
                    + "piece its player commands then (one of their own, or of an army they command), that enemy "
                    + "is taken: held between the two. For example, an enemy on d4 and your Pawn on d5: moving to "
                    + "d3, it takes the enemy. Every enemy held so on its other sides is taken in the same move, "
                    + "up to three at once."),
                (this._variant.Mercenaries.Contains(kind) && !this._variant.Leaders.Contains(kind),
                    "Mercenary: whoever holds it prisoner may hire it on their turn, for 110% of what it cost last "
                    + "(its value, the first time), paid from their points: it becomes theirs. On a later step they "
                    + "place it on a free square of their palace. Each step is a turn's move, or uses a die that "
                    + "moves it. It stays a mercenary whatever it is promoted into, and never gets cheaper."),
                (this._variant.Fleers.Contains(kind),
                    "Escape: on its own far corner, the board's corner across from its army's, it may leave the board "
                    + "as its move, where no one can take it. It comes back only on doubles that let a captured "
                    + "piece of its kind escape, as one would."),
            }
            .Where(power => power.Has)
            .Select(power => power.Power);
    }

    /// <summary>
    /// A proposal to end the game is answered; a piece going home is placed; an offer waiting is answered;
    /// once a turn is over, its seat may only offer, or be done; a Pawn just
    /// come to the far edge is promoted, or left for later (a pass); with dice rolled, the seat moves what
    /// they show, promotes, or passes.
    /// </summary>
    private ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat) =>
        state.Proposal is null
            ? this.PlayChoicesOf(state, seat)
            : [new AnswerEndAction(seat, true), new AnswerEndAction(seat, false)];

    private ImmutableList<GameAction> PlayChoicesOf(GameState state, SeatId seat) =>
        (state.Returning, state.Offer, state.IsRansomWindow, state.PromotionDue, state.Dice.IsEmpty) switch
        {
            ({ }, _, _, _, _) => [.. this._ransom.PlacesOf(state)],
            (_, { }, _, _, _) => [new AcceptAction(seat), new DeclineAction(seat)],
            (_, _, true, _, _) => [new PassAction(seat)],
            (_, _, _, { } cell, _) =>
            [
                new PassAction(seat),
                .. this.PromotionsOf(state, seat).Where(promotion => promotion.Cell == cell),
            ],
            (_, _, _, _, true) => this.TurnChoicesOf(state, seat),
            _ =>
            [
                new PassAction(seat),
                .. this.MovesOf(state, seat, this.RolledKinds(state).Contains),
                .. this.FleesOf(state, seat, this.RolledKinds(state).Contains),
                .. this.HiringsOf(state, seat, this.RolledKinds(state).Contains),
                .. this.PromotionsOf(state, seat),
                .. this.EscapesOf(state, seat),
                .. this.HandOversOf(state, seat),
            ],
        };

    /// <summary>
    /// A seat deciding its own turn may hand the rest of it to an ally who may be its regent (rules.md
    /// §6.2), at the start or between moves; the regent moves the seat's own army only.
    /// </summary>
    private IEnumerable<HandOverAction> HandOversOf(GameState state, SeatId seat) =>
        state.ActingFor is null
            ? this._control.RegentsFor(state, seat).Select(regent => new HandOverAction(seat, regent))
            : [];

    /// <summary>
    /// On doubles, not yet used, instead of moving: a captured piece of the kind they show, of an army the
    /// seat commands, whoever holds it, may escape (rules.md §7) - to each square it may go to. Pieces alike
    /// held by the same seat are offered once, the highest-scoring first, then by owner, then by holder. Never to
    /// an enemy moving for the seat whose throne it holds, on dice that seat rolled (Stated 2026-11-06). A piece that
    /// fled the board comes back so too, as though its own army held it (<see cref="Flight"/>).
    /// </summary>
    private IEnumerable<EscapeAction> EscapesOf(GameState state, SeatId seat)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        bool isOccupier = state.ActingFor is { } occupied && !state.Teams.AreAllied(seat, occupied);
        ImmutableHashSet<PieceKindId> kinds = !isOccupier
            && state.Dice.Count == TurnDice
            && state.Dice.Distinct().Count() == 1
            ? [.. this._dice.KindsOf(state.Dice[0])]
            : [];
        return state.Prisoners
            .Concat(state.Fled.Select(piece => new Prisoner(piece, piece.Owner)))
            .Where(prisoner => armies.Contains(prisoner.Piece.Owner) && kinds.Contains(prisoner.Piece.Kind))
            .DistinctBy(prisoner => (prisoner.Piece.Owner, prisoner.Piece.Kind, prisoner.Holder))
            .OrderByDescending(prisoner => this._variant.PointsIn(state.Edition)[prisoner.Piece.Kind])
            .ThenBy(prisoner => prisoner.Piece.Owner.Value)
            .ThenBy(prisoner => prisoner.Holder.Value)
            .SelectMany(prisoner => this._ransom.EscapesFor(state, prisoner)
                .Select(home => new EscapeAction(seat, prisoner.Piece.Id, home))
            );
    }

    /// <summary>
    /// At the start of a turn: roll, or move any piece, or promote, or pass - but only roll, or promote,
    /// while an enemy Raja sits on the throne of the seat whose turn it is (rules.md §6.3); or hand the turn
    /// to a regent (§6.2).
    /// </summary>
    private ImmutableList<GameAction> TurnChoicesOf(GameState state, SeatId seat) =>
        this._control.OccupierOf(state, state.ActingFor ?? seat) is null
            ?
            [
                new PassAction(seat),
                new RollAction(seat),
                .. this.MovesOf(state, seat, _ => true),
                .. this.FleesOf(state, seat, _ => true),
                .. this.HiringsOf(state, seat, _ => true),
                .. this.PromotionsOf(state, seat),
                .. this.HandOversOf(state, seat),
                .. this.RetirementsOf(state, seat),
            ]
            :
            [
                new RollAction(seat),
                .. this.PromotionsOf(state, seat),
                .. this.HandOversOf(state, seat),
                .. this.RetirementsOf(state, seat),
            ];

    /// <summary>The flights from the far corner the seat may make (<see cref="Flight"/>), as moves of a kind.</summary>
    private IEnumerable<FleeAction> FleesOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable) =>
        this._flight.FleesOf(state, seat, this._control.ArmiesOf(state, seat), isMovable);

    /// <summary>
    /// The mercenaries the seat may hire, and those hired it may place (<see cref="Mercenaries"/>), each as a move of
    /// its kind.
    /// </summary>
    private IEnumerable<GameAction> HiringsOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable) =>
    [
        .. this._mercenaries.HiresOf(state, seat, isMovable),
        .. this._mercenaries.DeploysOf(state, seat, isMovable),
    ];

    /// <summary>
    /// The mercenary is hired, or placed, using a die that moves its kind, if any was rolled; the turn goes on.
    /// </summary>
    private Transition AfterHiring(GameState state, GameAction hiring)
    {
        (Transition done, PieceKindId kind) = hiring switch
        {
            HireAction hire => (
                this._mercenaries.Hired(state, hire),
                state.Prisoners.First(held => held.Piece.Id == hire.Piece).Piece.Kind
            ),
            _ => (
                Mercenaries.Deployed(state, (DeployAction)hiring),
                state.Hired.First(hired => hired.Id == ((DeployAction)hiring).Piece).Kind
            ),
        };
        return this.GoneOn(this.WithoutDieFor(done, kind));
    }

    /// <summary>
    /// The piece flees, using a die that moves it, if any was rolled; the turn goes on, unless the game is over.
    /// </summary>
    private Transition AfterFlight(GameState state, FleeAction flee)
    {
        PieceKindId kind = state.Position.PieceAt(flee.Cell)!.Kind; // the flight is legal, so a piece stands there
        Transition used = this.WithoutDieFor(Flight.Fled(state, flee), kind);
        return GameEnd.AfterMove(used, this._variant) ?? this.GoneOn(used);
    }

    /// <summary>
    /// A seat left with only its Raja on the board may, at the start of its own turn, end its part in the
    /// game (rules.md §10).
    /// </summary>
    private IEnumerable<RetireAction> RetirementsOf(GameState state, SeatId seat)
    {
        ImmutableList<Piece> own = [.. state.Position.Occupants.Values.Where(piece => piece.Owner == seat)];
        return state.ActingFor is null && own.Count == 1 && this.IsLeader(own[0].Kind)
            ? [new RetireAction(seat)]
            : [];
    }

    /// <summary>
    /// The promotions of the pieces of the armies the seat commands, each piece's own (if it has one) first, then
    /// into a captured piece. A piece of an enemy army waits on a die that moves its kind, unless the seat has just
    /// brought it where it may be promoted (Stated 2026-11-06).
    /// </summary>
    private IEnumerable<PromotionAction> PromotionsOf(GameState state, SeatId seat)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        ImmutableHashSet<PieceKindId> rolled = this.RolledKinds(state);
        ImmutableList<Piece> pool =
        [
            .. state.Prisoners
                .DistinctBy(prisoner => (prisoner.Piece.Owner, prisoner.Piece.Kind, prisoner.Holder))
                .OrderByDescending(prisoner => this._variant.PointsIn(state.Edition)[prisoner.Piece.Kind])
                .ThenBy(prisoner => prisoner.Piece.Owner.Value)
                .ThenBy(prisoner => prisoner.Holder.Value)
                .Select(prisoner => prisoner.Piece),
        ];
        return state.Position.Occupants
            .Where(occupant => armies.Contains(occupant.Value.Owner))
            .Where(occupant =>
                rolled.Contains(occupant.Value.Kind)
                || state.Teams.AreAllied(occupant.Value.Owner, seat)
                || state.PromotionDue == occupant.Key
            )
            .SelectMany(occupant => (IEnumerable<PromotionAction>)
            [
                .. this.OwnPromotionsOf(state, seat, occupant.Key, occupant.Value),
                .. this.FarEdgePromotionsOf(state, seat, occupant.Key, occupant.Value, pool),
            ]);
    }

    /// <summary>
    /// A piece with a promotion of its own (<see cref="Chaturaji2027Variant.OwnPromotions"/>), standing within its
    /// rows of the far edge, or its zone, may become any kind it names, for its own army, free (specification
    /// §6.2); a retired seat's, which no one moves, may not.
    /// </summary>
    private IEnumerable<PromoteOwnAction> OwnPromotionsOf(GameState state, SeatId seat, CellId cell, Piece piece) =>
        this._variant.OwnPromotions.TryGetValue(piece.Kind, out OwnPromotion? own)
        && this.IsWhereOwnPromotes(state, own, cell, piece)
        && !state.Retired.Contains(piece.Owner)
            ? own.Into.Select(kind => new PromoteOwnAction(seat, cell, kind))
            : [];

    private bool IsWhereOwnPromotes(GameState state, OwnPromotion own, CellId cell, Piece piece) =>
        own.Zone is { } zone
            ? zone(state, this._grid, piece, cell)
            : this.RowsAhead(cell, state.SeatOf(piece.Owner).Forward) < own.Ranks;

    /// <summary>
    /// Each Pawn (each piece of the variant's <see cref="Chaturaji2027Variant.Promotables"/>) that stands on the far
    /// edge - with nowhere forward left to go - may become any captured piece, whoever holds it: the piece goes back
    /// to its own army (rules.md §8). Pieces alike held by the same seat are offered once - the first taken - the
    /// highest-scoring first, then by owner, then by holder.
    /// </summary>
    private IEnumerable<PromoteAction> FarEdgePromotionsOf(
        GameState state,
        SeatId seat,
        CellId cell,
        Piece piece,
        ImmutableList<Piece> pool
    ) =>
        this._variant.Promotables.Contains(piece.Kind)
        && this._grid.Step(cell, state.SeatOf(piece.Owner).Forward) is null
            ? pool.Where(into => this.MayBecome(piece, into)).Select(into => new PromoteAction(seat, cell, into.Id))
            : [];

    /// <summary>How many rows lie ahead of <paramref name="cell"/>, facing <paramref name="forward"/>: 0 on the edge.
    /// </summary>
    private int RowsAhead(CellId cell, Direction forward) =>
        this._grid.Step(cell, forward) is { } next ? 1 + this.RowsAhead(next, forward) : 0;

    /// <summary>Into any piece but a Pawn of its own army; into another army's Pawn, as the rules are set.</summary>
    private bool MayBecome(Piece pawn, Piece piece) =>
        !this.IsPawn(piece) || (this._pawnPromotion == PawnPromotion.OtherArmies && piece.Owner != pawn.Owner);

    private bool IsPawn(Piece piece) => this._variant.Pawns.Contains(piece.Kind);

    /// <summary>
    /// Promoting a piece of an enemy army takes a die that moves its kind; one's own team's, none; nor one just
    /// brought where it may be promoted, whoever's it is (Stated 2026-11-06).
    /// </summary>
    private static bool UsesDie(GameState state, PromotionAction promotion) =>
        state.PromotionDue != promotion.Cell
        && !state.Teams.AreAllied(state.Position.PieceAt(promotion.Cell)!.Owner, promotion.Actor);

    private ImmutableHashSet<PieceKindId> RolledKinds(GameState state) =>
        [.. state.Dice.SelectMany(this._dice.KindsOf)];

    /// <summary>The moves of the armies the seat commands, but a retired Raja's: no one moves it (§10).</summary>
    private IEnumerable<GameAction> MovesOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        return state.Position.Occupants
            .Where(occupant => armies.Contains(occupant.Value.Owner) && isMovable(occupant.Value.Kind))
            .Where(occupant =>
                !state.Retired.Contains(occupant.Value.Owner) || !this.IsLeader(occupant.Value.Kind)
            )
            .SelectMany(occupant => this.MovesFrom(state, occupant.Key, occupant.Value, seat))
            .Select(move => new MoveAction(seat, move.From, move.To))
            .Distinct();
    }

    /// <summary>
    /// A piece moves the way its own army faces, as the board lets it, with its start-row powers, castling, the
    /// flying-general capture and steps along palace lines, if it has them; a Pawn may also swap with one back to back.
    /// </summary>
    private IEnumerable<Move> MovesFrom(GameState state, CellId cell, Piece piece, SeatId mover)
    {
        bool MayTake(Piece target) => this.IsCapturable(state, target, mover);
        return this._moves.MovesFrom(state.Position, cell, state.SeatOf(piece.Owner), MayTake)
            .Concat(this._startRows.MovesFrom(state, cell, piece, mover, MayTake))
            .Concat(this._castling.MovesFrom(state, cell, piece))
            .Concat(this._flying.MovesFrom(state, cell, piece, MayTake))
            .Concat(this._palaceSteps.MovesFrom(state, cell, piece, MayTake))
            .Distinct()
            .Where(move => this.MayGo(state, move))
            .Concat(this.SwapsFrom(state, cell, piece));
    }

    /// <summary>
    /// The cells a piece of an army not allied with <paramref name="seat"/>'s could capture a leader on, as a castling
    /// leader would be: every cell any piece could capture on, and those a flyer could take a leader on.
    /// </summary>
    private ImmutableHashSet<CellId> AttackedAgainst(GameState state, SeatId seat)
    {
        ImmutableList<KeyValuePair<CellId, Piece>> enemies =
        [
            .. state.Position.Occupants.Where(occupant => !state.Teams.AreAllied(occupant.Value.Owner, seat)),
        ];
        return
        [
            .. enemies
                .Select(enemy => enemy.Value.Owner)
                .Distinct()
                .SelectMany(enemy => this.AttacksOf(state, enemy))
                .Select(attack => attack.Target),
            .. enemies
                .SelectMany(enemy => this._flying.ReachFrom(state, enemy.Key, enemy.Value)
                    .Where(target => this.MayGo(state, new Move(enemy.Value, enemy.Key, target, null)))
                ),
        ];
    }

    /// <summary>Whether the board lets a piece make the move its kind allows.</summary>
    private bool MayGo(GameState state, Move move) => this._variant.MayMove?.Invoke(state, this._grid, move) ?? true;

    /// <summary>
    /// A Pawn facing an allied Pawn of another army, back to back, may swap places with it: its move
    /// (rules.md §4).
    /// </summary>
    private IEnumerable<Move> SwapsFrom(GameState state, CellId cell, Piece piece) =>
        this.IsSwap(state, cell, piece, this.Ahead(state, cell, piece))
            ? [new Move(piece, cell, this.Ahead(state, cell, piece).GetValueOrDefault(), null)]
            : [];

    /// <summary>The cell a piece faces, if the board goes on that way.</summary>
    private CellId? Ahead(GameState state, CellId cell, Piece piece) =>
        this._grid.Step(cell, state.SeatOf(piece.Owner).Forward);

    /// <summary>Whether a Pawn on <paramref name="cell"/> going to <paramref name="target"/> swaps there.</summary>
    private bool IsSwap(GameState state, CellId cell, Piece piece, CellId? target)
    {
        Piece? other = target is { } ahead ? state.Position.PieceAt(ahead) : null;
        return this.IsPawn(piece)
            && other is { } facing
            && this.IsPawn(facing)
            && facing.Owner != piece.Owner
            && state.Teams.AreAllied(facing.Owner, piece.Owner)
            && this.Ahead(state, target.GetValueOrDefault(), facing) is { } behind
            && behind == cell;
    }

    /// <summary>
    /// Any piece the mover moves takes the mover's enemies, and a piece of the mover's own team too, while an
    /// enemy holds that piece's throne, commanding it (rules.md §6.4): the page's safety keeps a person from it.
    /// </summary>
    private bool IsCapturable(GameState state, Piece target, SeatId mover) =>
        !state.Teams.AreAllied(target.Owner, mover) || this._control.OccupierOf(state, target.Owner) is not null;

    private Transition Decided(GameState state, GameAction action)
    {
        bool isLegal = this.LegalActions(state).Contains(action);
        return isLegal
            ? this.Act(state, action)
            : throw new IllegalActionException(action);
    }

    private Transition Act(GameState state, GameAction action) =>
        action switch
        {
            RollAction => new Transition(state with { AwaitedDice = new DiceRequest(TurnDice, this._dice.Sides) }, []),
            MoveAction move => this.AfterMove(state, move),
            PromoteAction promote => this.Promoted(state, promote, FromPrisoner(state, promote)),
            PromoteOwnAction promote => this.Promoted(state, promote, Become(state, promote)),
            EscapeAction escape => this.Escaped(state, escape),
            FleeAction flee => this.AfterFlight(state, flee),
            HireAction or DeployAction => this.AfterHiring(state, action),
            HandOverAction handOver => HandedToRegent(state, handOver),
            RetireAction retire => this.Retired(state, retire),
            AnswerEndAction answer => Agreement.Answered(state, answer),
            AcceptAction accept => this.Accepted(state, accept),
            PlaceAction place => this._ransom.Placed(state, place),
            DeclineAction => Ransom.Declined(state),
            _ when state.PromotionDue is not null =>
                this.GoneOn(new Transition(state with { PromotionDue = null }, [])),
            _ when state.IsRansomWindow => this.Handed(new Transition(state, [])),
            _ when state.Dice.IsEmpty => this.IdlePass(new Transition(state, []), Ransom.MayOffer(state)),
            _ => this.Declined(new Transition(state, []), Ransom.MayOffer(state), hasPassed: true),
        };

    /// <summary>The seat retires; if its team has only retired Rajas left, the game ends (rules.md §10).</summary>
    private Transition Retired(GameState state, RetireAction retire)
    {
        Transition retired = new(
            state with { Retired = state.Retired.Add(retire.Actor) },
            [new RajaRetired(retire.Actor)]
        );
        return GameEnd.AfterMove(retired, this._variant) ?? this.Handed(retired);
    }

    /// <summary>
    /// The offer is accepted; a seat that had given up its turns, still offered ransoms, rejoins the game by
    /// accepting one (rules.md §10).
    /// </summary>
    private Transition Accepted(GameState state, AcceptAction accept)
    {
        Transition rejoined = state.Resigned.Contains(accept.Actor)
            ? Agreement.Rejoined(state, new RejoinAction(accept.Actor))
            : new Transition(state, []);
        Transition accepted = this._ransom.Accepted(rejoined.State);
        return accepted with { Events = [.. rejoined.Events, .. accepted.Events] };
    }

    /// <summary>The regent decides the rest of the turn, for the seat whose turn it is (rules.md §6.2).</summary>
    private static Transition HandedToRegent(GameState state, HandOverAction handOver) =>
        new(
            state with { DecidingSeat = handOver.Regent, ActingFor = handOver.Actor },
            [new TurnHandedOver(handOver.Actor, handOver.Regent)]
        );

    /// <exception cref="IllegalActionException">The seat may not propose ending the game now.</exception>
    private Transition Proposed(GameState state, ProposeEndAction propose) =>
        this.MayProposeEnd(state, propose.Actor)
            ? Agreement.Proposed(state, propose)
            : throw new IllegalActionException(propose);

    /// <exception cref="IllegalActionException">The seat may not vote so now.</exception>
    private Transition Voted(GameState state, VoteAwayAction vote) =>
        this.MayVoteAway(state, vote.Actor, vote.Absent)
            ? Away.Voted(state, vote)
            : throw new IllegalActionException(vote);

    /// <summary>The deciding seat, away, passes, and has missed one more turn.</summary>
    /// <exception cref="IllegalActionException">The seat does not decide, or its decision is timed.</exception>
    private Transition PassedAway(GameState state, PassAwayAction pass)
    {
        SeatId seat = pass.Actor;
        bool mayPass = state.DecidingSeat == seat && state.AwaitedDice is null && this.TimerOf(state) is null;
        Transition missed = mayPass
            ? Away.Missed(state, seat, this._awayTurns)
            : throw new IllegalActionException(pass);
        Transition passed = this.Act(missed.State, new PassAction(seat));
        return passed with { Events = [.. missed.Events, .. passed.Events] };
    }

    /// <summary>
    /// The face-off: the faced leader taken by the caller's seat, the caller by the faced leader's, both scoring;
    /// whoever decided still does, unless the game is over.
    /// </summary>
    /// <exception cref="IllegalActionException">The seat may not call this face-off now.</exception>
    private Transition FacedOff(GameState state, FaceOffAction faceOff)
    {
        Piece caller = this.ChancesOf(state, faceOff.Actor).Contains(faceOff)
            ? state.Position.PieceAt(faceOff.Caller)!
            : throw new IllegalActionException(faceOff);
        Piece faced = state.Position.PieceAt(faceOff.Faced)!;
        Position position = new(state.Position.Occupants.Remove(faceOff.Caller).Remove(faceOff.Faced));
        Transition called = new(
            state with
            {
                Position = position,
                Passages = [.. state.Passages.Where(passage => position.PieceAt(passage.To) is not null)],
            },
            [new LeadersFacedOff(caller, faced, faceOff.Actor)]
        );
        Transition taken = this.Captured(
            this.Captured(called, faced, faceOff.Actor, faceOff.Faced),
            caller,
            faced.Owner,
            faceOff.Caller
        );
        return GameEnd.AfterMove(taken, this._variant) ?? taken;
    }

    /// <summary>
    /// A piece taken <i>en passant</i> out of turn (Stated 2026-11-06): no die used, and whoever decided still does,
    /// unless the game is over.
    /// </summary>
    /// <exception cref="IllegalActionException">The seat may not take so now.</exception>
    private Transition TakenInPassing(GameState state, MoveAction move)
    {
        Transition taken = this.ChancesOf(state, move.Actor).Contains(move)
            ? this.Moved(state, move)
            : throw new IllegalActionException(move);
        return GameEnd.AfterMove(taken, this._variant) ?? taken;
    }

    /// <summary>Whatever a seat plays itself shows it is not away.</summary>
    private static Transition Attended(GameAction action, Transition transition) =>
        Away.IsPlaying(action) ? Away.Present(transition, action.Actor) : transition;

    /// <exception cref="IllegalActionException">The seat may not rejoin the game now.</exception>
    private Transition Rejoined(GameState state, RejoinAction rejoin) =>
        this.MayRejoin(state, rejoin.Actor)
            ? Agreement.Rejoined(state, rejoin)
            : throw new IllegalActionException(rejoin);

    /// <summary>
    /// A seat that has given up its turns passes on whatever it is left to decide that no timer decides
    /// for it (rules.md §10): its turn, or its share of another's; and on offering ransoms once its turn is
    /// over, which the clock would only make the others wait for.
    /// </summary>
    private Transition Unstalled(Transition transition)
    {
        GameState state = transition.State;
        bool isItsOwn = this.TimerOf(state) is null || state is { IsRansomWindow: true, Offer: null };
        return state.DecidingSeat is { } seat && state.Resigned.Contains(seat) && isItsOwn
            ? this.Unstalled(Followed(transition, this.Act(state, new PassAction(seat)), new TurnGivenUp(seat)))
            : transition;
    }

    private static Transition Followed(Transition first, Transition then, GameEvent between) =>
        then with { Events = [.. first.Events, between, .. then.Events] };

    /// <summary>
    /// The offer waits on its receiver's answer; but prisoners given back to their own team, asking nothing, go
    /// home at once, unless they are an ally's whose throne an enemy holds: that ally is asked first (rules.md §8).
    /// </summary>
    /// <exception cref="IllegalActionException">The offer may not be made.</exception>
    private Transition Offered(GameState state, OfferAction offer) =>
        (Ransom.IsValid(state, offer), this.IsAsked(state, offer)) switch
        {
            (false, _) => throw new IllegalActionException(offer),
            (_, true) => Ransom.Offered(state, offer),
            _ => this._ransom.Released(state, offer),
        };

    private bool IsAsked(GameState state, OfferAction offer) =>
        !state.Teams.AreAllied(offer.Receiver, offer.Actor)
        || !offer.Asked.IsEmpty
        || (offer.Receiver != offer.Actor && this._control.OccupierOf(state, offer.Receiver) is not null);

    /// <summary>The Pawn becomes the captured piece, for that piece's own army: the prisoner is freed.</summary>
    private static (Piece Into, GameState State) FromPrisoner(GameState state, PromoteAction promote)
    {
        Prisoner prisoner = state.Prisoners.First(held => held.Piece.Id == promote.Into);
        return (prisoner.Piece, state with { Prisoners = state.Prisoners.Remove(prisoner) });
    }

    /// <summary>The piece becomes the kind its own promotion names, still itself, and its army's.</summary>
    private static (Piece Into, GameState State) Become(GameState state, PromoteOwnAction promote) =>
        (state.Position.PieceAt(promote.Cell)! with { Kind = promote.Becomes }, state);

    /// <summary>
    /// The piece becomes <paramref name="becoming"/>'s piece - unless that leaves only Rajas on the board.
    /// Promoting one's own team's piece uses no die, so the turn goes on as it was; an enemy's uses a die, as a
    /// move does; a piece just come to where it may be promoted lets the turn go on from that move.
    /// </summary>
    private Transition Promoted(GameState state, PromotionAction promote, (Piece Into, GameState State) becoming)
    {
        Piece pawn = state.Position.PieceAt(promote.Cell)!; // the promotion is legal, so the piece stands there
        GameState promoted = this._mercenaries.Promoted(becoming.State, pawn, becoming.Into) with
        {
            Position = state.Position.Place(promote.Cell, becoming.Into),
            PromotionDue = null,
        };
        Transition transition = new(promoted, [new PiecePromoted(pawn, becoming.Into, promote.Cell, promote.Actor)]);
        bool usesDie = UsesDie(state, promote);
        Transition used = usesDie
            ? this.WithoutDieFor(transition, pawn.Kind)
            : transition;
        return (GameEnd.AfterMove(transition, this._variant), usesDie || state.PromotionDue is not null) switch
        {
            ({ } over, _) => over,
            (_, true) => this.GoneOn(used),
            _ => used,
        };
    }

    /// <summary>
    /// The prisoner goes back onto the board for its own army, taking whatever stands there, and uses up
    /// both dice: the turn is over. So does a piece that fled, held by no one.
    /// </summary>
    private Transition Escaped(GameState state, EscapeAction escape)
    {
        Prisoner? prisoner = state.Prisoners.FirstOrDefault(held => held.Piece.Id == escape.Piece);
        Piece piece = prisoner?.Piece ?? state.Fled.First(fled => fled.Id == escape.Piece);
        Piece? captured = state.Position.PieceAt(escape.Home);
        GameState escaped = Farming.Stopped(state, escape.Actor) with
        {
            Position = state.Position.Place(escape.Home, piece),
            Prisoners = prisoner is null ? state.Prisoners : state.Prisoners.Remove(prisoner),
            Fled = state.Fled.Remove(piece),
            Dice = [],
        };
        Transition transition = new(
            escaped,
            [new PieceEscaped(piece, escape.Home, escape.Actor, captured, prisoner?.Holder)]
        );
        Transition taken = captured is null
            ? transition
            : this.Captured(transition, captured, escape.Actor, escape.Home);
        return GameEnd.AfterMove(taken, this._variant) ?? this.GoneOn(taken);
    }

    /// <exception cref="IllegalActionException">The game is not waiting on these dice.</exception>
    private Transition Rolled(GameState state, DiceAction dice)
    {
        GameState rolled = IsAwaited(state, dice)
            ? state with { AwaitedDice = null }
            : throw new IllegalActionException(dice);
        return rolled.RollOff.IsEmpty
            ? this.TurnRolled(rolled, dice)
            : this.RolledOff(rolled, dice.Values[0]);
    }

    private static bool IsAwaited(GameState state, DiceAction dice) =>
        state.AwaitedDice is { } request
        && dice.Actor == state.DecidingSeat
        && dice.Values.Count == request.Count
        && dice.Values.All(value => value >= 1 && value <= request.Sides);

    private Transition TurnRolled(GameState state, DiceAction dice)
    {
        Transition rolled = new(state with { Dice = dice.Values }, [new DiceRolled(dice.Actor, dice.Values)]);
        return this.HasUsableDie(rolled.State)
            ? rolled
            : this.Declined(Forfeited(rolled), mayHaveOffered: false, hasPassed: false);
    }

    /// <summary>
    /// One more roll to see who goes first. Once every contender has rolled, those who rolled highest
    /// roll again, until one alone has: their die is the opening move (rules.md §4, §11).
    /// </summary>
    private Transition RolledOff(GameState state, int value)
    {
        SeatId seat = state.DecidingSeat!.Value; // the game awaited this seat's die
        GameState rolled = state with { Dice = state.Dice.Add(value) };
        ImmutableList<GameEvent> events = [new RollOffRolled(seat, value)];
        return rolled.Dice.Count < rolled.RollOff.Count
            ? new Transition(this.NextToRollOff(rolled, rolled.RollOff[rolled.Dice.Count], rolled.Dice), events)
            : this.RollOffSettled(rolled, events);
    }

    private Transition RollOffSettled(GameState state, ImmutableList<GameEvent> events)
    {
        int highest = state.Dice.Max();
        ImmutableList<SeatId> leaders = [.. state.RollOff.Where((_, index) => state.Dice[index] == highest)];
        GameState opened = state with
        {
            RollOff = [],
            DecidingSeat = leaders[0],
            TurnNumber = OpeningTurn,
            Dice = [highest],
        };
        GameState again = this.NextToRollOff(state with { RollOff = leaders }, leaders[0], []);
        return leaders.Count > 1
            ? new Transition(again, events.Add(new RollOffTied(leaders)))
            : this.EndTurnIfStuck(new Transition(opened, events.Add(new RollOffWon(leaders[0], highest))));
    }

    private GameState NextToRollOff(GameState state, SeatId seat, ImmutableList<int> rolled) =>
        state with { DecidingSeat = seat, Dice = rolled, AwaitedDice = this.RollOffDie };

    /// <summary>
    /// A deliberate move ends the turn; a rolled one uses up its die. A Pawn come to the far edge, if it
    /// may be promoted, waits on that first (rules.md §8).
    /// </summary>
    private Transition AfterMove(GameState state, MoveAction move)
    {
        PieceKindId kind = state.Position.PieceAt(move.From)!.Kind; // the move is legal, so a piece stands there
        Transition moved = this._farming.Tallied(state, move, this.Moved(state, move));
        Transition used = this.WithoutDieFor(moved, kind);
        GameState due = used.State with { PromotionDue = move.To };
        bool isDue = this.PromotionsOf(due, move.Actor).Any(promotion => promotion.Cell == move.To);
        return (GameEnd.AfterMove(moved, this._variant), isDue) switch
        {
            ({ } over, _) => over,
            (_, true) => used with { State = due },
            _ => this.GoneOn(used),
        };
    }

    /// <summary>
    /// Uses up a die that lets <paramref name="kind"/> move, if any was rolled: a deliberate move uses
    /// none (and removes 0, which no die shows).
    /// </summary>
    private Transition WithoutDieFor(Transition transition, PieceKindId kind)
    {
        GameState state = transition.State;
        int die = state.Dice.FirstOrDefault(value => this._dice.KindsOf(value).Contains(kind));
        return transition with { State = state with { Dice = state.Dice.Remove(die) } };
    }

    /// <summary>
    /// The turn goes on with the dice left, if any; else it is over. While an enemy Raja still sits on
    /// the throne of the seat whose turn it is, its first move is all it gets: the occupier moves for it
    /// next (rules.md §6.3).
    /// </summary>
    private Transition GoneOn(Transition transition) =>
        transition.State.Dice.IsEmpty
            ? this.PassTurn(transition)
            : this.HandOverOrGoOn(transition);

    private Transition HandOverOrGoOn(Transition transition) =>
        this.OccupierToHandTo(transition.State) is { } occupier
            ? this.HandedOver(transition, occupier)
            : this.EndTurnIfStuck(transition);

    /// <summary>
    /// Dice left unused: the occupier of the seat's throne may use them, if any; the occupier leaves those it
    /// does not use to the seat (rules.md §6.3); else the turn ends - with no window to offer ransoms if the
    /// seat <paramref name="mayHaveOffered"/> before it passed. The log says whether the seat
    /// <paramref name="hasPassed"/>, or could use none of its dice.
    /// </summary>
    private Transition Declined(Transition transition, bool mayHaveOffered, bool hasPassed)
    {
        GameState state = transition.State;
        SeatId deciding = state.DecidingSeat!.Value; // a seat decides while dice are left
        bool isOccupier = state.ActingFor is { } seat && !state.Teams.AreAllied(deciding, seat);
        return (this.OccupierToHandTo(state), isOccupier) switch
        {
            ({ } occupier, _) => this.HandedOver(transition, occupier, hasPassed),
            (_, true) => this.HandedBack(transition),
            _ => this.PassTurn(transition, mayHaveOffered),
        };
    }

    /// <summary>The seat whose turn it is has the dice its occupier left, to use or not, as it will.</summary>
    private Transition HandedBack(Transition transition)
    {
        GameState state = transition.State;
        SeatId occupier = state.DecidingSeat!.Value; // the occupier decided to leave them
        SeatId seat = state.ActingFor!.Value; // for the seat it moved for
        GameState back = state with { DecidingSeat = seat, ActingFor = null, IsHandedBack = true };
        return this.EndTurnIfStuck(new Transition(back, transition.Events.Add(new TurnHandedBack(occupier, seat))));
    }

    /// <summary>
    /// The enemy on the throne of the seat whose turn it is, before it has moved for that seat this turn -
    /// while the seat, or its regent, decides - unless it has left the seat the rest.
    /// </summary>
    private SeatId? OccupierToHandTo(GameState state)
    {
        SeatId deciding = state.DecidingSeat!.Value; // a seat decides while dice are left
        SeatId seat = state.ActingFor ?? deciding;
        return state.Teams.AreAllied(deciding, seat) && !state.IsHandedBack
            ? this._control.OccupierOf(state, seat)
            : null;
    }

    private Transition HandedOver(Transition transition, SeatId occupier, bool hasPassed = false)
    {
        GameState state = transition.State;
        SeatId seat = (state.ActingFor ?? state.DecidingSeat)!.Value; // a seat decides while dice are left
        GameState shared = state with { DecidingSeat = occupier, ActingFor = seat };
        TurnShared handed = new(seat, occupier) { HasPassed = hasPassed };
        return this.EndTurnIfStuck(new Transition(shared, transition.Events.Add(handed)));
    }

    /// <summary>A die no piece can use is forfeit; once no die is left to use, the turn passes.</summary>
    private Transition EndTurnIfStuck(Transition transition) =>
        this.HasUsableDie(transition.State)
            ? transition
            : this.PassTurn(Forfeited(transition));

    /// <summary>The deciding seat loses the dice it has left, which nothing it commands can use.</summary>
    private static Transition Forfeited(Transition transition)
    {
        GameState state = transition.State;
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return transition with { Events = transition.Events.Add(new DiceForfeited(seat, state.Dice)) };
    }

    private bool HasUsableDie(GameState state) => this.MayUse(state, this.RolledKinds(state).Contains);

    /// <summary>
    /// Whether the deciding seat may move a piece of a kind <paramref name="isRolled"/> lets move, promote
    /// an enemy Pawn with such a die, let a piece escape, or hire or place a mercenary of such a kind.
    /// </summary>
    private bool MayUse(GameState state, Func<PieceKindId, bool> isRolled) =>
        this.MayMove(state, isRolled)
        || this.EscapesOf(state, state.DecidingSeat!.Value).Any()
        || this.HiringsOf(state, state.DecidingSeat!.Value, isRolled).Any();

    /// <summary>
    /// Whether the deciding seat may move a piece of a kind <paramref name="isRolled"/> lets move, or promote
    /// an enemy Pawn with such a die.
    /// </summary>
    private bool MayMove(GameState state, Func<PieceKindId, bool> isRolled)
    {
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return this.MovesOf(state, seat, isRolled).Any()
            || this.PromotionsOf(state, seat)
                .Any(promotion =>
                    UsesDie(state, promotion) && isRolled(state.Position.PieceAt(promotion.Cell)!.Kind)
                );
    }

    /// <summary>
    /// A turn passed without moving: enough of those in a row end the game. Having passed it
    /// when it <paramref name="mayHaveOffered"/>, the seat gets no window to offer ransoms.
    /// </summary>
    private Transition IdlePass(Transition transition, bool mayHaveOffered)
    {
        GameState state = transition.State;
        SeatId seat = (state.ActingFor ?? state.DecidingSeat)!.Value; // someone just took a turn
        Transition counted = transition with { State = state with { IdlePasses = state.IdlePasses + 1 } };
        return GameEnd.AfterIdlePass(counted, this._idleRounds) ?? Warned(
            this.Ending(counted, mayHaveOffered),
            GameEnd.IsLastIdleRound(counted.State, this._idleRounds),
            new IdleWarned(seat, this._idleRounds - 1)
        );
    }

    /// <summary>The transition, telling everyone the game may soon end, if it <paramref name="isNear"/>.</summary>
    private static Transition Warned(Transition transition, bool isNear, GameEvent warning) =>
        isNear
            ? transition with { Events = transition.Events.Add(warning) }
            : transition;

    /// <summary>
    /// The turn passes: idly, if no die rolled was used, as a roll and then a pass is a pass (rules.md §10).
    /// </summary>
    private Transition PassTurn(Transition transition, bool mayHaveOffered = false) =>
        transition.State.Dice.Count == TurnDice
            ? this.IdlePass(transition, mayHaveOffered)
            : this.Ending(transition with { State = transition.State with { IdlePasses = 0 } }, mayHaveOffered);

    /// <summary>
    /// The turn is over. A seat holding prisoners may still offer ransoms (rules.md §8) before it
    /// passes, unless it has given up its turns, or chose to pass when it <paramref name="mayHaveOffered"/>
    /// instead; any other seat's turn passes now.
    /// </summary>
    private Transition Ending(Transition transition, bool mayHaveOffered = false)
    {
        GameState state = transition.State;
        SeatId seat = (state.ActingFor ?? state.DecidingSeat)!.Value; // someone just took a turn
        GameState window = state with
        {
            DecidingSeat = seat,
            ActingFor = null,
            AwaitedDice = null,
            Dice = [],
            IsRansomWindow = true,
        };
        return Ransom.HoldsRansom(state, seat) && !state.Resigned.Contains(seat) && !mayHaveOffered
            ? new Transition(window, transition.Events.Add(new RansomWindowOpened(seat)))
            : this.Handed(transition);
    }

    /// <summary>
    /// The turn goes clockwise to the next seat still playing (rules.md §10) - and, if that seat has
    /// given up its turns, on to its ally, if the ally may be its regent (§6.2).
    /// </summary>
    private Transition Handed(Transition transition)
    {
        GameState state = transition.State;
        SeatId current = (state.ActingFor ?? state.DecidingSeat)!.Value; // someone just took a turn
        SeatId next = Enumerable
            .Range(1, state.Seats.Count)
            .Select(offset => new SeatId((current.Value + offset) % state.Seats.Count))
            .First(seat => GameEnd.IsPlaying(state, seat));
        GameState passed = state with
        {
            DecidingSeat = next,
            ActingFor = null,
            TurnNumber = state.TurnNumber + 1,
            AwaitedDice = null,
            Dice = [],
            IsRansomWindow = false,
            OffersMade = [],
            PromotionDue = null,
            IsHandedBack = false,
            Passages = Closed(state, current),
        };
        SeatId? regent = passed.Resigned.Contains(next)
            ? this._control.RegentsFor(passed, next).Cast<SeatId?>().FirstOrDefault()
            : null;
        Transition handed = new(passed, transition.Events.Add(new TurnPassed(current, next)));
        return regent is { } ally
            ? HandedToRegent(passed, new HandOverAction(next, ally)) with
            {
                Events = handed.Events.Add(new TurnHandedOver(next, ally)),
            }
            : handed;
    }

    /// <summary>
    /// The pieces that may still be taken <i>en passant</i> once <paramref name="ended"/>'s turn is over: no longer by
    /// that seat; and none at all once no seat still playing that is an enemy of its piece's may take it.
    /// </summary>
    private static ImmutableList<Passage> Closed(GameState state, SeatId ended) =>
    [
        .. state.Passages
            .Select(passage => passage with { Closed = passage.Closed.Add(ended) })
            .Where(passage => state.Seats.Any(seat => !state.Teams.AreAllied(seat.Id, passage.Piece.Owner)
                && GameEnd.IsPlaying(state, seat.Id)
                && !passage.Closed.Contains(seat.Id)
            )),
    ];

    private Transition Moved(GameState state, MoveAction move)
    {
        Piece mover = state.Position.PieceAt(move.From)!; // the move is legal, so a piece stands there
        Transition moved = this.IsSwap(state, move.From, mover, move.To)
            ? Swapped(state, move, mover)
            : this.Displaced(state, move, mover);
        bool MayStop(CellId cell) =>
            this.MovesFrom(state, move.From, mover, move.Actor).Any(other => other.To == cell);
        return moved with { State = this._startRows.Passed(moved.State, move, mover, MayStop) };
    }

    /// <summary>Two Pawns back to back trade places; nothing is taken.</summary>
    private static Transition Swapped(GameState state, MoveAction move, Piece mover)
    {
        Piece other = state.Position.PieceAt(move.To)!; // a swap goes to a Pawn
        GameState swapped = state with { Position = state.Position.Place(move.From, other).Place(move.To, mover) };
        return new Transition(swapped, [new PawnsSwapped(mover, other, move.From, move.To, move.Actor)]);
    }

    /// <summary>
    /// The piece moves, taking what stands where it goes, or what it takes <i>en passant</i>; or castles, its rook
    /// moving too; and takes what it jumps over, and what it then holds in custody.
    /// </summary>
    private Transition Displaced(GameState state, MoveAction move, Piece mover)
    {
        bool MayTake(Piece target) => this.IsCapturable(state, target, move.Actor);
        Passage? passing = this._startRows.TakenBy(state, move, mover, MayTake);
        Piece? captured = state.Position.PieceAt(move.To) ?? passing?.Piece;
        (CellId From, CellId To)? castled = captured is null ? this._castling.RookOf(state, move, mover) : null;
        Position relocated = state.Position.Relocate(move.From, move.To);
        Position position = passing is { } passage
            ? new Position(relocated.Occupants.Remove(passage.To))
            : castled is { } rook
                ? relocated.Relocate(rook.From, rook.To)
                : relocated;
        ImmutableList<CellId> taken =
        [
            .. this.JumpedBy(state, move, mover, MayTake),
            .. this._custody.TakenBy(
                position,
                move.To,
                mover,
                target => !state.Teams.AreAllied(target.Owner, move.Actor) && MayTake(target),
                holder => this._control.ArmiesOf(state, move.Actor).Contains(holder.Owner)
            ),
        ];
        PieceMoved went = new(mover, move.From, move.To, move.Actor);
        ImmutableList<GameEvent> events = castled is { } rookMove
            ? [went, new PieceMoved(state.Position.PieceAt(rookMove.From)!, rookMove.From, rookMove.To, move.Actor)]
            : [went];
        Transition moved = new(state with { Position = new Position(position.Occupants.RemoveRange(taken)) }, events);
        Transition onArrival = captured is null
            ? moved
            : this.Captured(moved, captured, move.Actor, passing?.To ?? move.To);
        Transition all = taken.Aggregate(
            onArrival,
            (sofar, cell) => this.Captured(sofar, position.PieceAt(cell)!, move.Actor, cell)
        );
        return this.EnteredThrone(all, mover, move.To);
    }

    /// <summary>The cells of the pieces the move jumps over, and takes, in order; none unless it jumps.</summary>
    private ImmutableList<CellId> JumpedBy(GameState state, MoveAction move, Piece mover, Func<Piece, bool> mayTake) =>
        this._variant.Kinds[mover.Kind].Movement.Any(rule => rule.IsJumping)
            ? this._moves.MovesFrom(state.Position, move.From, state.SeatOf(mover.Owner), mayTake)
                .FirstOrDefault(jump => jump.To == move.To && !jump.Jumped.IsEmpty)?.Jumped ?? []
            : [];

    /// <summary>
    /// A capture by <paramref name="captor"/> on <paramref name="cell"/> scores by piece, but taking a piece of
    /// one's own team, or a retired seat's Raja, scores no one (rules.md §10).
    /// </summary>
    private Transition Captured(Transition moved, Piece captured, SeatId captor, CellId cell)
    {
        bool isEnemy = !moved.State.Teams.AreAllied(captured.Owner, captor)
            && !moved.State.Retired.Contains(captured.Owner);
        int points = isEnemy
            ? this._variant.PointsIn(moved.State.Edition)[captured.Kind]
            : 0;
        bool isEnemyRaja = isEnemy && this.IsLeader(captured.Kind);
        bool isOnThrone = isEnemyRaja && this._control.IsThroneOf(moved.State, cell, captured.Owner);
        Transition scored = this._farming.Forfeited(
            Scored(moved, captor, points, new PieceCaptured(captured, captor, points)),
            captured,
            captor,
            isEnemy
        );
        GameState state = scored.State;
        bool isOwnTeam = moved.State.Teams.AreAllied(captured.Owner, captor);
        Transition taken = scored with
        {
            State = state with
            {
                Prisoners = state.Prisoners.Add(new Prisoner(captured, captor)),
                TakenByOwnTeam = isOwnTeam
                    ? state.TakenByOwnTeam.SetItem(captured.Id, state.TurnNumber)
                    : state.TakenByOwnTeam,
            },
        };
        int onThronePoints = this._variant.Scoring.RajaOnThroneBonus;
        RajaCapturedOnThrone bonus = new(captured, captor, onThronePoints);
        Transition onThrone = isOnThrone
            ? Scored(taken, captor, onThronePoints, bonus)
            : taken;
        return isEnemyRaja
            ? this.Empire(onThrone, captured.Owner, captor, isOnThrone)
            : onThrone;
    }

    /// <summary>
    /// Taking the last Raja of <paramref name="enemy"/>'s team creates the empire (rules.md §5) for every
    /// seat of the other team that commands an ally's army then: the captor first, then by seat. A retired
    /// seat's Raja counts for nothing, and a retired seat creates nothing (§10).
    /// </summary>
    private Transition Empire(Transition taken, SeatId enemy, SeatId captor, bool isOnThrone)
    {
        GameState state = taken.State;
        bool isLastRaja = !state.Position.Occupants.Values
            .Where(piece => !state.Retired.Contains(piece.Owner))
            .Any(piece => this.IsLeader(piece.Kind) && state.Teams.AreAllied(piece.Owner, enemy));
        IEnumerable<SeatId> creators = isLastRaja
            ? state.Seats
                .Select(seat => seat.Id)
                .Where(seat => !state.Teams.AreAllied(seat, enemy) && !state.Retired.Contains(seat))
                .Where(seat => this._control.CommandsAnAlly(state, seat))
                .OrderBy(seat => seat != captor)
                .ThenBy(seat => seat.Value)
            : [];
        return creators.Aggregate(
            taken,
            (empire, creator) =>
            {
                int points = this.EmpirePoints(creator == captor, isOnThrone);
                return Scored(empire, creator, points, new EmpireCreated(creator, points));
            }
        );
    }

    /// <summary>The empire's points: more for its captor, and more still if it took the Raja on its throne.</summary>
    private int EmpirePoints(bool isCaptor, bool isOnThrone)
    {
        Scoring scoring = this._variant.Scoring;
        return (isCaptor, isOnThrone) switch
        {
            (false, _) => scoring.EmpireBonus,
            (true, false) => scoring.EmpireBonus + scoring.EmpireByCaptureBonus,
            (true, true) => scoring.EmpireBonus + scoring.EmpireByThroneCaptureBonus,
        };
    }

    /// <summary>A Raja entering an enemy's throne scores its owner a bonus (rules.md §5).</summary>
    private Transition EnteredThrone(Transition moved, Piece mover, CellId cell)
    {
        SeatId? throne = this.IsLeader(mover.Kind)
            ? this._control.OwnerOf(moved.State, cell)
            : null;
        int points = this._variant.Scoring.ThroneEntryBonus;
        return throne is { } enemy && !moved.State.Teams.AreAllied(enemy, mover.Owner)
            ? Scored(moved, mover.Owner, points, new ThroneEntered(mover, enemy, points))
            : moved;
    }

    internal static Transition Scored(Transition transition, SeatId seat, int points, GameEvent happened)
    {
        GameState state = transition.State;
        GameState scored = state with { Scores = state.Scores.SetItem(seat, state.Scores[seat] + points) };
        return new Transition(scored, transition.Events.Add(happened));
    }
}
