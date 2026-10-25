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
/// and Raja-less allies, and turns shared with an enemy Raja on one's throne (§6.1, §6.3); and the
/// end of the game (§10). Voluntary regency, escape, promotion, ransom, boat triumph and the empire
/// bonus follow; docs/rules.md lists what is implemented.
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

    private const int OpeningTurn = 1;

    private readonly SquareGridTopology _grid = new(ClassicSetup.Side, ClassicSetup.Side);
    private readonly MoveGenerator _moves;
    private readonly DiceProfile _dice;
    private readonly int _idleRounds;
    private readonly ThroneControl _control;

    public ClassicRules()
        : this(DiceProfile.WeightedSixSided, DefaultIdleRounds)
    {
    }

    public ClassicRules([Required] DiceProfile dice, [Range(1, int.MaxValue)] int idleRounds)
    {
        this._moves = new MoveGenerator(this._grid, ClassicPieces.Kinds);
        this._dice = dice;
        this._idleRounds = idleRounds;
        this.Thrones = ClassicSetup.Thrones(this._grid);
        this._control = new ThroneControl(this.Thrones);
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
        action is DiceAction dice
            ? this.Rolled(state, dice)
            : this.Decided(state, action);

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => ClassicPieces.TraditionalNames[kind];

    public string NameOf(SeatId seat) => ClassicSetup.SeatNames[seat.Value];

    /// <summary>The kinds the die lets move: "Horse", or "Pawn or Raja"; "Nothing" for a value no die shows.</summary>
    public string NameOfDie(int value)
    {
        ImmutableList<string> names = [.. this._dice.KindsOf(value).Select(this.NameOf).Order(StringComparer.Ordinal)];
        return names.IsEmpty ? "Nothing" : string.Join(" or ", names);
    }

    private DiceRequest RollOffDie => new(RollOffDice, this._dice.Sides);

    private ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat) =>
        state.Dice.IsEmpty
            ? this.TurnChoicesOf(state, seat)
            : [new PassAction(seat), .. this.MovesOf(state, seat, this.RolledKinds(state).Contains)];

    /// <summary>
    /// At the start of a turn: roll, or move any piece, or pass - but only roll while an enemy Raja
    /// sits on the seat's throne (rules.md §6.3).
    /// </summary>
    private ImmutableList<GameAction> TurnChoicesOf(GameState state, SeatId seat) =>
        this._control.OccupierOf(state, seat) is null
            ? [new PassAction(seat), new RollAction(seat), .. this.MovesOf(state, seat, _ => true)]
            : [new RollAction(seat)];

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

    /// <summary>
    /// A piece moves the way its own army faces, and takes the enemies of whoever moves it - never a
    /// piece of its own army.
    /// </summary>
    private ImmutableList<Move> MovesFrom(GameState state, CellId cell, Piece piece, SeatId mover) =>
        this._moves.MovesFrom(
            state.Position,
            cell,
            state.SeatOf(piece.Owner),
            target => target.Owner != piece.Owner && !state.Teams.AreAllied(target.Owner, mover)
        );

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
            _ when state.Dice.IsEmpty => this.IdlePass(new Transition(state, [])),
            _ => this.Declined(new Transition(state, [])),
        };

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
            : this.Declined(rolled);
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

    /// <summary>A deliberate move ends the turn; a rolled one uses up its die.</summary>
    private Transition AfterMove(GameState state, MoveAction move)
    {
        PieceKindId kind = state.Position.PieceAt(move.From)!.Kind; // the move is legal, so a piece stands there
        Transition moved = this.Moved(state, move);
        Transition? ended = GameEnd.AfterMove(moved);
        return (ended, state.Dice.IsEmpty) switch
        {
            ({ } over, _) => over,
            (_, true) => PassTurn(moved),
            _ => this.UsedDie(moved, kind),
        };
    }

    /// <summary>
    /// Uses up the die the move took. While an enemy Raja still sits on the throne of the seat whose
    /// turn it is, its first move is all it gets: the occupier moves for it next (rules.md §6.3).
    /// </summary>
    private Transition UsedDie(Transition moved, PieceKindId kind)
    {
        GameState state = moved.State;
        int die = state.Dice.First(value => this._dice.KindsOf(value).Contains(kind)); // a die allowed the move
        Transition used = moved with { State = state with { Dice = state.Dice.Remove(die) } };
        return used.State.Dice.IsEmpty
            ? PassTurn(used)
            : this.HandOverOrGoOn(used);
    }

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
            : PassTurn(transition);

    private bool HasUsableDie(GameState state)
    {
        SeatId seat = state.DecidingSeat!.Value; // dice are only rolled and used by the deciding seat
        return this.MovesOf(state, seat, this.RolledKinds(state).Contains).Any();
    }

    /// <summary>A turn passed without rolling or moving: enough of those in a row end the game.</summary>
    private Transition IdlePass(Transition transition)
    {
        Transition passed = Handed(transition, transition.State.IdlePasses + 1);
        return GameEnd.AfterIdlePass(passed, this._idleRounds) ?? passed;
    }

    private static Transition PassTurn(Transition transition) => Handed(transition, 0);

    /// <summary>The turn goes clockwise to the next seat with pieces left (rules.md §10).</summary>
    private static Transition Handed(Transition transition, int idlePasses)
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
            IdlePasses = idlePasses,
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

    private Transition Captured(Transition moved, Piece captured, MoveAction move)
    {
        int points = ClassicPieces.Points[captured.Kind];
        bool isOnThrone = captured.Kind == ClassicPieces.Raja && this._control.IsThroneOf(move.To, captured.Owner);
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
