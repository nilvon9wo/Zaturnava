using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
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
public sealed class Chaturaji2027Rules : IRulesApplying
{
    public const string RulesetId = "chaturaji-2027";

    /// <summary>The dice each seat rolls to see who goes first.</summary>
    public const int RollOffDice = 1;

    /// <summary>The dice rolled on a turn.</summary>
    public const int TurnDice = 2;

    /// <summary>How many rounds no one may roll or move before the game ends on points.</summary>
    public const int DefaultIdleRounds = 3;

    /// <summary>The points for a Raja entering an enemy's throne (rules.md §5).</summary>
    public const int ThroneEntryBonus = 1;

    /// <summary>The points, on top of the capture, for taking a Raja on its own throne (rules.md §5).</summary>
    public const int RajaOnThroneBonus = 2;

    /// <summary>The points for creating the empire (rules.md §5).</summary>
    public const int EmpireBonus = 1;

    /// <summary>The points, on top of <see cref="EmpireBonus"/>, for taking the Raja that creates it.</summary>
    public const int EmpireByCaptureBonus = 2;

    /// <summary>The points instead of <see cref="EmpireByCaptureBonus"/> when that Raja was on its throne.</summary>
    public const int EmpireByThroneCaptureBonus = 4;

    /// <summary>The points for commanding every piece on the board, on one's own turn (rules.md §5).</summary>
    public const int EveryPieceBonus = 2;

    /// <summary>The points for accepting a ransom, for each prisoner it sends home (rules.md §8).</summary>
    public const int RansomAcceptedBonus = 1;

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

    private readonly SquareGridTopology _grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);
    private readonly MoveGenerator _moves;
    private readonly DiceProfile _dice;
    private readonly int _idleRounds;
    private readonly int _awayTurns;
    private readonly CommandedCaptures _commandedCaptures;
    private readonly PawnPromotion _pawnPromotion;
    private readonly ThroneControl _control;
    private readonly Command _command;
    private readonly EveryPiece _everyPiece;
    private readonly Ransom _ransom;

    public Chaturaji2027Rules()
        : this(DiceProfile.WeightedSixSided, DefaultIdleRounds, CommandedCaptures.NotOwnTeam, PawnPromotion.OtherArmies)
    {
    }

    /// <summary>
    /// The rules as configured; <paramref name="awayTurns"/> is how many turns in a row a seat away may have
    /// passed before it gives them up.
    /// </summary>
    public Chaturaji2027Rules(
        [Required] DiceProfile dice,
        [Range(1, int.MaxValue)] int idleRounds,
        CommandedCaptures commandedCaptures,
        PawnPromotion pawnPromotion,
        [Range(1, int.MaxValue)] int awayTurns = DefaultAwayTurns
    )
    {
        this._awayTurns = awayTurns;
        this._moves = new MoveGenerator(this._grid, Chaturaji2027Pieces.Kinds);
        this._dice = dice;
        this._idleRounds = idleRounds;
        this._commandedCaptures = commandedCaptures;
        this._pawnPromotion = pawnPromotion;
        this.Thrones = Chaturaji2027Setup.Thrones(this._grid);
        this._control = new ThroneControl(this.Thrones);
        this._command = new Command(this._control);
        this._everyPiece = new EveryPiece(this._control);
        this._ransom = new Ransom(this._grid, Chaturaji2027Setup.InitialPosition(this._grid));
    }

    public string Id => RulesetId;

    public string Name => "Chaturaji 2027";

    public ICellNavigating Topology => this._grid;

    public ImmutableDictionary<SeatId, CellId> Thrones { get; }

    /// <summary>Every seat rolls a die, in seat order, to see who goes first.</summary>
    public GameState CreateInitialState()
    {
        ImmutableList<Seat> seats = Chaturaji2027Setup.Seats();
        GameState state = new(
            Chaturaji2027Setup.InitialPosition(this._grid),
            seats,
            Chaturaji2027Setup.Teams(),
            seats[0].Id,
            0,
            seats.ToImmutableDictionary(seat => seat.Id, _ => 0),
            []
        );
        return state with { RollOff = [.. seats.Select(seat => seat.Id)], AwaitedDice = this.RollOffDie };
    }

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        (state.DecidingSeat, state.AwaitedDice) is ({ } seat, null)
            ? this.ChoicesOf(state, seat)
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        this._command.Told(
            state,
            this._everyPiece.Scored(
                this.Unstalled(
                    GameEnd.AfterGivingUp(
                        Attended(
                            action,
                            action switch
                            {
                                DiceAction dice => this.Rolled(state, dice),
                                OfferAction offer => Offered(state, offer),
                                ProposeEndAction propose => this.Proposed(state, propose),
                                RejoinAction rejoin => this.Rejoined(state, rejoin),
                                VoteAwayAction vote => this.Voted(state, vote),
                                PassAwayAction pass => this.PassedAway(state, pass),
                                _ => this.Decided(state, action),
                            }
                        )
                    )
                )
            )
        );

    public bool MayOffer([Required] GameState state) => Ransom.MayOffer(state);

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => Agreement.MayPropose(state, seat);

    public bool MayRejoin([Required] GameState state, SeatId seat) => Agreement.MayRejoin(state, seat);

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        Away.MayVote(state, voter, absent);

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
    public int WorthOf(PieceKindId kind) => Chaturaji2027Pieces.Points[kind];

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
    [
        .. state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat)
            .SelectMany(occupant => this._moves.AttacksFrom(state.Position, occupant.Key, state.SeatOf(seat))),
    ];

    public bool IsLeader(PieceKindId kind) => kind == Chaturaji2027Pieces.Raja;

    public string NameOf(PieceKindId kind) => Chaturaji2027Pieces.ChaturajiNames[kind];

    public string NameOf(SeatId seat) => Chaturaji2027Setup.SeatNames[seat.Value];

    /// <summary>The kinds the die lets move: "Horse", or "Pawn or Raja"; "Nothing" for a value no die shows.</summary>
    public string NameOfDie(int value)
    {
        ImmutableList<string> names = [.. this._dice.KindsOf(value).Select(this.NameOf).Order(StringComparer.Ordinal)];
        return names.IsEmpty ? "Nothing" : string.Join(" or ", names);
    }

    public bool MayUseDie([Required] GameState state, int value) =>
        state.DecidingSeat is not null && this.MayUse(state, this._dice.KindsOf(value).Contains);

    private DiceRequest RollOffDie => new(RollOffDice, this._dice.Sides);

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
    /// held by the same seat are offered once, the highest-scoring first, then by owner, then by holder.
    /// </summary>
    private IEnumerable<EscapeAction> EscapesOf(GameState state, SeatId seat)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        ImmutableHashSet<PieceKindId> kinds = state.Dice.Count == TurnDice && state.Dice.Distinct().Count() == 1
            ? [.. this._dice.KindsOf(state.Dice[0])]
            : [];
        return state.Prisoners
            .Where(prisoner => armies.Contains(prisoner.Piece.Owner) && kinds.Contains(prisoner.Piece.Kind))
            .DistinctBy(prisoner => (prisoner.Piece.Owner, prisoner.Piece.Kind, prisoner.Holder))
            .OrderByDescending(prisoner => Chaturaji2027Pieces.Points[prisoner.Piece.Kind])
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
                .. this.PromotionsOf(state, seat),
                .. this.HandOversOf(state, seat),
                .. RetirementsOf(state, seat),
            ]
            :
            [
                new RollAction(seat),
                .. this.PromotionsOf(state, seat),
                .. this.HandOversOf(state, seat),
                .. RetirementsOf(state, seat),
            ];

    /// <summary>
    /// A seat left with only its Raja on the board may, at the start of its own turn, end its part in the
    /// game (rules.md §10).
    /// </summary>
    private static IEnumerable<RetireAction> RetirementsOf(GameState state, SeatId seat)
    {
        ImmutableList<Piece> own = [.. state.Position.Occupants.Values.Where(piece => piece.Owner == seat)];
        return state.ActingFor is null && own.Count == 1 && own[0].Kind == Chaturaji2027Pieces.Raja
            ? [new RetireAction(seat)]
            : [];
    }

    /// <summary>
    /// Each Pawn of an army the seat commands that stands on the far edge - with nowhere forward left
    /// to go - may become any captured piece, whoever holds it: the piece goes back to its own army
    /// (rules.md §8). A Pawn of an enemy army waits on a die that moves a Pawn. Pieces alike held by the
    /// same seat are offered once - the first taken - the highest-scoring first, then by owner, then by
    /// holder.
    /// </summary>
    private IEnumerable<PromoteAction> PromotionsOf(GameState state, SeatId seat)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        bool mayMoveAPawn = this.RolledKinds(state).Contains(Chaturaji2027Pieces.Pawn);
        ImmutableList<Piece> pool =
        [
            .. state.Prisoners
                .DistinctBy(prisoner => (prisoner.Piece.Owner, prisoner.Piece.Kind, prisoner.Holder))
                .OrderByDescending(prisoner => Chaturaji2027Pieces.Points[prisoner.Piece.Kind])
                .ThenBy(prisoner => prisoner.Piece.Owner.Value)
                .ThenBy(prisoner => prisoner.Holder.Value)
                .Select(prisoner => prisoner.Piece),
        ];
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Kind == Chaturaji2027Pieces.Pawn && armies.Contains(occupant.Value.Owner))
            .Where(occupant => mayMoveAPawn || state.Teams.AreAllied(occupant.Value.Owner, seat))
            .Where(occupant => this._grid.Step(occupant.Key, state.SeatOf(occupant.Value.Owner).Forward) is null)
            .SelectMany(occupant => pool
                .Where(piece => this.MayBecome(occupant.Value, piece))
                .Select(piece => new PromoteAction(seat, occupant.Key, piece.Id))
            );
    }

    /// <summary>Into any piece but a Pawn of its own army; into another army's Pawn, as the rules are set.</summary>
    private bool MayBecome(Piece pawn, Piece piece) =>
        piece.Kind != Chaturaji2027Pieces.Pawn
        || (this._pawnPromotion == PawnPromotion.OtherArmies && piece.Owner != pawn.Owner);

    /// <summary>Promoting a Pawn of an enemy army takes a die that moves a Pawn; one's own team's, none.</summary>
    private static bool UsesDie(GameState state, PromoteAction promotion) =>
        !state.Teams.AreAllied(state.Position.PieceAt(promotion.Cell)!.Owner, promotion.Actor);

    private ImmutableHashSet<PieceKindId> RolledKinds(GameState state) =>
        [.. state.Dice.SelectMany(this._dice.KindsOf)];

    private IEnumerable<GameAction> MovesOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable)
    {
        ImmutableHashSet<SeatId> armies = this._control.ArmiesOf(state, seat);
        return state.Position.Occupants
            .Where(occupant => armies.Contains(occupant.Value.Owner) && isMovable(occupant.Value.Kind))
            .SelectMany(occupant => this.MovesFrom(state, occupant.Key, occupant.Value, seat))
            .Select(move => new MoveAction(seat, move.From, move.To));
    }

    /// <summary>A piece moves the way its own army faces; a Pawn may also swap with one back to back.</summary>
    private ImmutableList<Move> MovesFrom(GameState state, CellId cell, Piece piece, SeatId mover) =>
        this._moves.MovesFrom(
                state.Position,
                cell,
                state.SeatOf(piece.Owner),
                target => this.IsCapturable(state, piece, target, mover)
            )
            .AddRange(this.SwapsFrom(state, cell, piece));

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
        return piece.Kind == Chaturaji2027Pieces.Pawn
            && other is { } facing
            && facing.Kind == Chaturaji2027Pieces.Pawn
            && facing.Owner != piece.Owner
            && state.Teams.AreAllied(facing.Owner, piece.Owner)
            && this.Ahead(state, target.GetValueOrDefault(), facing) is { } behind
            && behind == cell;
    }

    /// <summary>
    /// A piece of the mover's team takes the mover's enemies only; a piece of an enemy army it commands
    /// takes anything, or anything but the mover's team, as the rules are set (rules.md §6).
    /// </summary>
    private bool IsCapturable(GameState state, Piece piece, Piece target, SeatId mover) =>
        !state.Teams.AreAllied(target.Owner, mover)
        || (this._commandedCaptures == CommandedCaptures.Anything && !state.Teams.AreAllied(piece.Owner, mover));

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
            PromoteAction promote => this.Promoted(state, promote),
            EscapeAction escape => this.Escaped(state, escape),
            HandOverAction handOver => HandedToRegent(state, handOver),
            RetireAction retire => this.Handed(
                new Transition(
                    state with { Retired = state.Retired.Add(retire.Actor) },
                    [new RajaRetired(retire.Actor)]
                )
            ),
            AnswerEndAction answer => Agreement.Answered(state, answer),
            AcceptAction => this._ransom.Accepted(state),
            PlaceAction place => this._ransom.Placed(state, place),
            DeclineAction => Ransom.Declined(state),
            _ when state.PromotionDue is not null =>
                this.GoneOn(new Transition(state with { PromotionDue = null }, [])),
            _ when state.IsRansomWindow => this.Handed(new Transition(state, [])),
            _ when state.Dice.IsEmpty => this.IdlePass(new Transition(state, []), Ransom.MayOffer(state)),
            _ => this.Declined(new Transition(state, []), Ransom.MayOffer(state)),
        };

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

    /// <exception cref="IllegalActionException">The offer may not be made.</exception>
    private static Transition Offered(GameState state, OfferAction offer) =>
        Ransom.IsValid(state, offer)
            ? Ransom.Offered(state, offer)
            : throw new IllegalActionException(offer);

    /// <summary>
    /// The Pawn becomes the captured piece, for that piece's own army - unless that leaves only Rajas on
    /// the board. Promoting one's own team's Pawn uses no die, so the turn goes on as it was; an enemy's
    /// uses a die, as a move does; a Pawn just come to the edge lets the turn go on from that move.
    /// </summary>
    private Transition Promoted(GameState state, PromoteAction promote)
    {
        Piece pawn = state.Position.PieceAt(promote.Cell)!; // the promotion is legal, so the Pawn stands there
        Prisoner prisoner = state.Prisoners.First(held => held.Piece.Id == promote.Into);
        GameState promoted = state with
        {
            Position = state.Position.Place(promote.Cell, prisoner.Piece),
            Prisoners = state.Prisoners.Remove(prisoner),
            PromotionDue = null,
        };
        Transition transition = new(promoted, [new PiecePromoted(pawn, prisoner.Piece, promote.Cell, promote.Actor)]);
        bool usesDie = UsesDie(state, promote);
        Transition used = usesDie
            ? this.WithoutDieFor(transition, Chaturaji2027Pieces.Pawn)
            : transition;
        return (GameEnd.AfterMove(transition), usesDie || state.PromotionDue is not null) switch
        {
            ({ } over, _) => over,
            (_, true) => this.GoneOn(used),
            _ => used,
        };
    }

    /// <summary>
    /// The prisoner goes back onto the board for its own army, taking whatever stands there, and uses up
    /// both dice: the turn is over.
    /// </summary>
    private Transition Escaped(GameState state, EscapeAction escape)
    {
        Prisoner prisoner = state.Prisoners.First(held => held.Piece.Id == escape.Piece);
        Piece? captured = state.Position.PieceAt(escape.Home);
        GameState escaped = state with
        {
            Position = state.Position.Place(escape.Home, prisoner.Piece),
            Prisoners = state.Prisoners.Remove(prisoner),
            Dice = [],
        };
        Transition transition = new(escaped, [new PieceEscaped(prisoner.Piece, escape.Home, escape.Actor)]);
        Transition taken = captured is null
            ? transition
            : this.Captured(transition, captured, escape.Actor, escape.Home);
        return GameEnd.AfterMove(taken) ?? this.GoneOn(taken);
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
            : this.Declined(Forfeited(rolled), mayHaveOffered: false);
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
        Transition moved = this.Moved(state, move);
        Transition used = this.WithoutDieFor(moved, kind);
        bool isDue = this.PromotionsOf(used.State, move.Actor).Any(promotion => promotion.Cell == move.To);
        return (GameEnd.AfterMove(moved), isDue) switch
        {
            ({ } over, _) => over,
            (_, true) => used with { State = used.State with { PromotionDue = move.To } },
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
    /// seat <paramref name="mayHaveOffered"/> before it passed.
    /// </summary>
    private Transition Declined(Transition transition, bool mayHaveOffered)
    {
        GameState state = transition.State;
        SeatId deciding = state.DecidingSeat!.Value; // a seat decides while dice are left
        bool isOccupier = state.ActingFor is { } seat && !state.Teams.AreAllied(deciding, seat);
        return (this.OccupierToHandTo(state), isOccupier) switch
        {
            ({ } occupier, _) => this.HandedOver(transition, occupier),
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

    private Transition HandedOver(Transition transition, SeatId occupier)
    {
        GameState state = transition.State;
        SeatId seat = (state.ActingFor ?? state.DecidingSeat)!.Value; // a seat decides while dice are left
        GameState shared = state with { DecidingSeat = occupier, ActingFor = seat };
        return this.EndTurnIfStuck(new Transition(shared, transition.Events.Add(new TurnShared(seat, occupier))));
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
    /// an enemy Pawn with such a die, or let a piece escape.
    /// </summary>
    private bool MayUse(GameState state, Func<PieceKindId, bool> isRolled)
    {
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return this.MovesOf(state, seat, isRolled).Any()
            || (isRolled(Chaturaji2027Pieces.Pawn)
                && this.PromotionsOf(state, seat).Any(promotion => UsesDie(state, promotion)))
            || this.EscapesOf(state, seat).Any();
    }

    /// <summary>
    /// A turn passed without rolling or moving: enough of those in a row end the game. Having passed it
    /// when it <paramref name="mayHaveOffered"/>, the seat gets no window to offer ransoms.
    /// </summary>
    private Transition IdlePass(Transition transition, bool mayHaveOffered)
    {
        GameState state = transition.State;
        Transition counted = transition with { State = state with { IdlePasses = state.IdlePasses + 1 } };
        return GameEnd.AfterIdlePass(counted, this._idleRounds) ?? this.Ending(counted, mayHaveOffered);
    }

    private Transition PassTurn(Transition transition, bool mayHaveOffered = false) =>
        this.Ending(transition with { State = transition.State with { IdlePasses = 0 } }, mayHaveOffered);

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
    /// The turn goes clockwise to the next seat with pieces left (rules.md §10) - and, if that seat has
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

    private Transition Moved(GameState state, MoveAction move)
    {
        Piece mover = state.Position.PieceAt(move.From)!; // the move is legal, so a piece stands there
        return this.IsSwap(state, move.From, mover, move.To)
            ? Swapped(state, move, mover)
            : this.Displaced(state, move, mover);
    }

    /// <summary>Two Pawns back to back trade places; nothing is taken.</summary>
    private static Transition Swapped(GameState state, MoveAction move, Piece mover)
    {
        Piece other = state.Position.PieceAt(move.To)!; // a swap goes to a Pawn
        GameState swapped = state with { Position = state.Position.Place(move.From, other).Place(move.To, mover) };
        return new Transition(swapped, [new PawnsSwapped(mover, other, move.From, move.To, move.Actor)]);
    }

    private Transition Displaced(GameState state, MoveAction move, Piece mover)
    {
        Piece? captured = state.Position.PieceAt(move.To);
        GameState relocated = state with { Position = state.Position.Relocate(move.From, move.To) };
        Transition moved = new(relocated, [new PieceMoved(mover, move.From, move.To, move.Actor)]);
        Transition taken = captured is null
            ? moved
            : this.Captured(moved, captured, move.Actor, move.To);
        return this.EnteredThrone(taken, mover, move.To);
    }

    /// <summary>
    /// A capture by <paramref name="captor"/> on <paramref name="cell"/> scores by piece, but taking a piece of
    /// one's own team, or a retired seat's Raja, scores no one (rules.md §10).
    /// </summary>
    private Transition Captured(Transition moved, Piece captured, SeatId captor, CellId cell)
    {
        bool isEnemy = !moved.State.Teams.AreAllied(captured.Owner, captor)
            && !moved.State.Retired.Contains(captured.Owner);
        int points = isEnemy
            ? Chaturaji2027Pieces.Points[captured.Kind]
            : 0;
        bool isEnemyRaja = isEnemy && captured.Kind == Chaturaji2027Pieces.Raja;
        bool isOnThrone = isEnemyRaja && this._control.IsThroneOf(cell, captured.Owner);
        Transition scored = Scored(moved, captor, points, new PieceCaptured(captured, captor, points));
        GameState state = scored.State;
        Transition taken = scored with
        {
            State = state with { Prisoners = state.Prisoners.Add(new Prisoner(captured, captor)) },
        };
        RajaCapturedOnThrone bonus = new(captured, captor, RajaOnThroneBonus);
        Transition onThrone = isOnThrone
            ? Scored(taken, captor, RajaOnThroneBonus, bonus)
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
            .Any(piece => piece.Kind == Chaturaji2027Pieces.Raja && state.Teams.AreAllied(piece.Owner, enemy));
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
                int points = EmpirePoints(creator == captor, isOnThrone);
                return Scored(empire, creator, points, new EmpireCreated(creator, points));
            }
        );
    }

    /// <summary>The empire's points: more for its captor, and more still if it took the Raja on its throne.</summary>
    private static int EmpirePoints(bool isCaptor, bool isOnThrone) =>
        (isCaptor, isOnThrone) switch
        {
            (false, _) => EmpireBonus,
            (true, false) => EmpireBonus + EmpireByCaptureBonus,
            (true, true) => EmpireBonus + EmpireByThroneCaptureBonus,
        };

    /// <summary>A Raja entering an enemy's throne scores its owner a bonus (rules.md §5).</summary>
    private Transition EnteredThrone(Transition moved, Piece mover, CellId cell)
    {
        SeatId? throne = mover.Kind == Chaturaji2027Pieces.Raja
            ? this._control.OwnerOf(cell)
            : null;
        return throne is { } enemy && !moved.State.Teams.AreAllied(enemy, mover.Owner)
            ? Scored(moved, mover.Owner, ThroneEntryBonus, new ThroneEntered(mover, enemy, ThroneEntryBonus))
            : moved;
    }

    internal static Transition Scored(Transition transition, SeatId seat, int points, GameEvent happened)
    {
        GameState state = transition.State;
        GameState scored = state with { Scores = state.Scores.SetItem(seat, state.Scores[seat] + points) };
        return new Transition(scored, transition.Events.Add(happened));
    }
}
