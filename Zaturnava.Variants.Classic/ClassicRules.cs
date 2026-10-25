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

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// Zaturnava's base rules. So far: a roll-off for who goes first, whose winning die is the opening
/// move (rules.md §4, §11); then turns of a deliberate move, a pass, or a roll of the dice (§4);
/// captures scored by piece, with the throne bonuses (§5); command of other armies through thrones
/// and Raja-less allies, and turns shared with an enemy Raja on one's throne (§6.1, §6.3); promotion
/// and ransom (§8); and the end of the game (§10). Voluntary regency, escape, boat triumph and the
/// empire bonus follow; docs/rules.md lists what is implemented.
/// </summary>
public sealed class ClassicRules : IRulesApplying
{
    public const string RulesetId = "classic";

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

    /// <summary>The timer on a seat whose turn is over, while it may still offer ransoms.</summary>
    public const string RansomWindowTimer = "ransom-window";

    /// <summary>The timer on a seat offered a ransom; declining is its default.</summary>
    public const string RansomAnswerTimer = "ransom-answer";

    private const int OpeningTurn = 1;

    private readonly SquareGridTopology _grid = new(ClassicSetup.Side, ClassicSetup.Side);
    private readonly MoveGenerator _moves;
    private readonly DiceProfile _dice;
    private readonly int _idleRounds;
    private readonly CommandedCaptures _commandedCaptures;
    private readonly PawnPromotion _pawnPromotion;
    private readonly ThroneControl _control;
    private readonly Ransom _ransom;

    public ClassicRules()
        : this(DiceProfile.WeightedSixSided, DefaultIdleRounds, CommandedCaptures.NotOwnTeam, PawnPromotion.OtherArmies)
    {
    }

    public ClassicRules(
        [Required] DiceProfile dice,
        [Range(1, int.MaxValue)] int idleRounds,
        CommandedCaptures commandedCaptures,
        PawnPromotion pawnPromotion
    )
    {
        this._moves = new MoveGenerator(this._grid, ClassicPieces.Kinds);
        this._dice = dice;
        this._idleRounds = idleRounds;
        this._commandedCaptures = commandedCaptures;
        this._pawnPromotion = pawnPromotion;
        this.Thrones = ClassicSetup.Thrones(this._grid);
        this._control = new ThroneControl(this.Thrones);
        ImmutableDictionary<PieceId, CellId> homes = ClassicSetup.InitialPosition(this._grid)
            .Occupants.ToImmutableDictionary(occupant => occupant.Value.Id, occupant => occupant.Key);
        this._ransom = new Ransom(this._grid, homes);
    }

    public string Id => RulesetId;

    public string Name => "Zaturnava (classic)";

    public ICellNavigating Topology => this._grid;

    public ImmutableDictionary<SeatId, CellId> Thrones { get; }

    /// <summary>Every seat rolls a die, in seat order, to see who goes first.</summary>
    public GameState CreateInitialState()
    {
        ImmutableList<Seat> seats = ClassicSetup.Seats();
        GameState state = new(
            ClassicSetup.InitialPosition(this._grid),
            seats,
            ClassicSetup.Teams(),
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
        action switch
        {
            DiceAction dice => this.Rolled(state, dice),
            OfferAction offer => Offered(state, offer),
            _ => this.Decided(state, action),
        };

    public bool MayOffer([Required] GameState state) => Ransom.MayOffer(state);

    /// <summary>A seat answering an offer, or offering once its turn is over, has only so long.</summary>
    public DecisionTimer? TimerOf([Required] GameState state) =>
        (state.Offer, state.IsRansomWindow, state.DecidingSeat) switch
        {
            ({ } offer, _, _) => new DecisionTimer(RansomAnswerTimer, new DeclineAction(offer.To)),
            (_, true, { } seat) => new DecisionTimer(RansomWindowTimer, new PassAction(seat)),
            _ => null,
        };

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    /// <summary>A piece is worth what taking it scores (rules.md §5).</summary>
    public int WorthOf(PieceKindId kind) => ClassicPieces.Points[kind];

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
    [
        .. state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat)
            .SelectMany(occupant => this._moves.AttacksFrom(state.Position, occupant.Key, state.SeatOf(seat))),
    ];

    public bool IsLeader(PieceKindId kind) => kind == ClassicPieces.Raja;

    public string NameOf(PieceKindId kind) => ClassicPieces.TraditionalNames[kind];

    public string NameOf(SeatId seat) => ClassicSetup.SeatNames[seat.Value];

    /// <summary>The kinds the die lets move: "Horse", or "Pawn or Raja"; "Nothing" for a value no die shows.</summary>
    public string NameOfDie(int value)
    {
        ImmutableList<string> names = [.. this._dice.KindsOf(value).Select(this.NameOf).Order(StringComparer.Ordinal)];
        return names.IsEmpty ? "Nothing" : string.Join(" or ", names);
    }

    private DiceRequest RollOffDie => new(RollOffDice, this._dice.Sides);

    /// <summary>
    /// An offer waiting is answered; once a turn is over, its seat may only offer, or be done; a Pawn just
    /// come to the far edge is promoted, or left for later (a pass); with dice rolled, the seat moves what
    /// they show, promotes, or passes.
    /// </summary>
    private ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat) =>
        (state.Offer, state.IsRansomWindow, state.PromotionDue, state.Dice.IsEmpty) switch
        {
            ({ }, _, _, _) => [new AcceptAction(seat), new DeclineAction(seat)],
            (_, true, _, _) => [new PassAction(seat)],
            (_, _, { } cell, _) =>
            [
                new PassAction(seat),
                .. this.PromotionsOf(state, seat).Where(promotion => promotion.Cell == cell),
            ],
            (_, _, _, true) => this.TurnChoicesOf(state, seat),
            _ =>
            [
                new PassAction(seat),
                .. this.MovesOf(state, seat, this.RolledKinds(state).Contains),
                .. this.PromotionsOf(state, seat),
            ],
        };

    /// <summary>
    /// At the start of a turn: roll, or move any piece, or promote, or pass - but only roll, or promote,
    /// while an enemy Raja sits on the seat's throne (rules.md §6.3).
    /// </summary>
    private ImmutableList<GameAction> TurnChoicesOf(GameState state, SeatId seat) =>
        this._control.OccupierOf(state, seat) is null
            ?
            [
                new PassAction(seat),
                new RollAction(seat),
                .. this.MovesOf(state, seat, _ => true),
                .. this.PromotionsOf(state, seat),
            ]
            : [new RollAction(seat), .. this.PromotionsOf(state, seat)];

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
        bool mayMoveAPawn = this.RolledKinds(state).Contains(ClassicPieces.Pawn);
        ImmutableList<Piece> pool =
        [
            .. state.Prisoners
                .DistinctBy(prisoner => (prisoner.Piece.Owner, prisoner.Piece.Kind, prisoner.Holder))
                .OrderByDescending(prisoner => ClassicPieces.Points[prisoner.Piece.Kind])
                .ThenBy(prisoner => prisoner.Piece.Owner.Value)
                .ThenBy(prisoner => prisoner.Holder.Value)
                .Select(prisoner => prisoner.Piece),
        ];
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Kind == ClassicPieces.Pawn && armies.Contains(occupant.Value.Owner))
            .Where(occupant => mayMoveAPawn || state.Teams.AreAllied(occupant.Value.Owner, seat))
            .Where(occupant => this._grid.Step(occupant.Key, state.SeatOf(occupant.Value.Owner).Forward) is null)
            .SelectMany(occupant => pool
                .Where(piece => this.MayBecome(occupant.Value, piece))
                .Select(piece => new PromoteAction(seat, occupant.Key, piece.Id))
            );
    }

    /// <summary>Into any piece but a Pawn of its own army; into another army's Pawn, as the rules are set.</summary>
    private bool MayBecome(Piece pawn, Piece piece) =>
        piece.Kind != ClassicPieces.Pawn
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

    /// <summary>A piece moves the way its own army faces.</summary>
    private ImmutableList<Move> MovesFrom(GameState state, CellId cell, Piece piece, SeatId mover) =>
        this._moves.MovesFrom(
            state.Position,
            cell,
            state.SeatOf(piece.Owner),
            target => this.IsCapturable(state, piece, target, mover)
        );

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
            AcceptAction => this._ransom.Accepted(state),
            DeclineAction => Ransom.Declined(state),
            _ when state.PromotionDue is not null =>
                this.GoneOn(new Transition(state with { PromotionDue = null }, [])),
            _ when state.IsRansomWindow => Handed(new Transition(state, [])),
            _ when state.Dice.IsEmpty => this.IdlePass(new Transition(state, [])),
            _ => this.Declined(new Transition(state, [])),
        };

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
            ? this.WithoutDieFor(transition, ClassicPieces.Pawn)
            : transition;
        return (GameEnd.AfterMove(transition), usesDie || state.PromotionDue is not null) switch
        {
            ({ } over, _) => over,
            (_, true) => this.GoneOn(used),
            _ => used,
        };
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
            : this.Declined(Forfeited(rolled));
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
            ? PassTurn(transition)
            : this.HandOverOrGoOn(transition);

    private Transition HandOverOrGoOn(Transition transition) =>
        this.OccupierToHandTo(transition.State) is { } occupier
            ? this.HandedOver(transition, occupier)
            : this.EndTurnIfStuck(transition);

    /// <summary>Dice left unused: the occupier of the seat's throne may use them, if any; else the turn ends.</summary>
    private Transition Declined(Transition transition) =>
        this.OccupierToHandTo(transition.State) is { } occupier
            ? this.HandedOver(transition, occupier)
            : PassTurn(transition);

    /// <summary>The enemy on the deciding seat's throne, before it has moved for that seat this turn.</summary>
    private SeatId? OccupierToHandTo(GameState state) =>
        state.ActingFor is null
            ? this._control.OccupierOf(state, state.DecidingSeat!.Value) // a seat decides while dice are left
            : null;

    private Transition HandedOver(Transition transition, SeatId occupier)
    {
        GameState state = transition.State;
        SeatId seat = state.DecidingSeat!.Value; // a seat decides while dice are left
        GameState shared = state with { DecidingSeat = occupier, ActingFor = seat };
        return this.EndTurnIfStuck(new Transition(shared, transition.Events.Add(new TurnShared(seat, occupier))));
    }

    /// <summary>A die no piece can use is forfeit; once no die is left to use, the turn passes.</summary>
    private Transition EndTurnIfStuck(Transition transition) =>
        this.HasUsableDie(transition.State)
            ? transition
            : PassTurn(Forfeited(transition));

    /// <summary>The deciding seat loses the dice it has left, which nothing it commands can use.</summary>
    private static Transition Forfeited(Transition transition)
    {
        GameState state = transition.State;
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return transition with { Events = transition.Events.Add(new DiceForfeited(seat, state.Dice)) };
    }

    private bool HasUsableDie(GameState state)
    {
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return this.MovesOf(state, seat, this.RolledKinds(state).Contains).Any()
            || this.PromotionsOf(state, seat).Any(promotion => UsesDie(state, promotion));
    }

    /// <summary>A turn passed without rolling or moving: enough of those in a row end the game.</summary>
    private Transition IdlePass(Transition transition)
    {
        GameState state = transition.State;
        Transition counted = transition with { State = state with { IdlePasses = state.IdlePasses + 1 } };
        return GameEnd.AfterIdlePass(counted, this._idleRounds) ?? Ending(counted);
    }

    private static Transition PassTurn(Transition transition) =>
        Ending(transition with { State = transition.State with { IdlePasses = 0 } });

    /// <summary>
    /// The turn is over. A seat holding prisoners may still offer ransoms (rules.md §8) before it
    /// passes; any other seat's turn passes now.
    /// </summary>
    private static Transition Ending(Transition transition)
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
        return Ransom.HoldsRansom(state, seat)
            ? new Transition(window, transition.Events.Add(new RansomWindowOpened(seat)))
            : Handed(transition);
    }

    /// <summary>The turn goes clockwise to the next seat with pieces left (rules.md §10).</summary>
    private static Transition Handed(Transition transition)
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
        };
        return new Transition(passed, transition.Events.Add(new TurnPassed(current, next)));
    }

    private Transition Moved(GameState state, MoveAction move)
    {
        Piece mover = state.Position.PieceAt(move.From)!; // the move is legal, so a piece stands there
        Piece? captured = state.Position.PieceAt(move.To);
        GameState relocated = state with { Position = state.Position.Relocate(move.From, move.To) };
        Transition moved = new(relocated, [new PieceMoved(mover, move.From, move.To, move.Actor)]);
        Transition taken = captured is null
            ? moved
            : this.Captured(moved, captured, move);
        return this.EnteredThrone(taken, mover, move.To);
    }

    /// <summary>A capture scores by piece, but taking a piece of one's own team scores no one.</summary>
    private Transition Captured(Transition moved, Piece captured, MoveAction move)
    {
        bool isEnemy = !moved.State.Teams.AreAllied(captured.Owner, move.Actor);
        int points = isEnemy
            ? ClassicPieces.Points[captured.Kind]
            : 0;
        bool isOnThrone = isEnemy
            && captured.Kind == ClassicPieces.Raja
            && this._control.IsThroneOf(move.To, captured.Owner);
        Transition scored = Scored(moved, move.Actor, points, new PieceCaptured(captured, move.Actor, points));
        GameState state = scored.State;
        Transition taken = scored with
        {
            State = state with { Prisoners = state.Prisoners.Add(new Prisoner(captured, move.Actor)) },
        };
        RajaCapturedOnThrone bonus = new(captured, move.Actor, RajaOnThroneBonus);
        return isOnThrone
            ? Scored(taken, move.Actor, RajaOnThroneBonus, bonus)
            : taken;
    }

    /// <summary>A Raja entering an enemy's throne scores its owner a bonus (rules.md §5).</summary>
    private Transition EnteredThrone(Transition moved, Piece mover, CellId cell)
    {
        SeatId? throne = mover.Kind == ClassicPieces.Raja
            ? this._control.OwnerOf(cell)
            : null;
        return throne is { } enemy && !moved.State.Teams.AreAllied(enemy, mover.Owner)
            ? Scored(moved, mover.Owner, ThroneEntryBonus, new ThroneEntered(mover, enemy, ThroneEntryBonus))
            : moved;
    }

    private static Transition Scored(Transition transition, SeatId seat, int points, GameEvent happened)
    {
        GameState state = transition.State;
        GameState scored = state with { Scores = state.Scores.SetItem(seat, state.Scores[seat] + points) };
        return new Transition(scored, transition.Events.Add(happened));
    }
}
