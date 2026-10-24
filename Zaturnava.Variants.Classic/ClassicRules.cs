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
/// Zaturnava's base rules. So far: turns of a deliberate move, a pass, or a roll of the dice
/// (rules.md §4), captures scored by piece (§5), and clockwise turns from seat 0. Doubles and escape,
/// throne control, promotion and ransom, boat triumph, bonuses and game end follow; docs/rules.md
/// lists what is implemented.
/// </summary>
public sealed class ClassicRules : IRulesApplying
{
    public const string RulesetId = "classic";

    /// <summary>The dice rolled on the opening turn, which has no deliberate move (rules.md §4).</summary>
    public const int OpeningDice = 1;

    /// <summary>The dice rolled on every later turn.</summary>
    public const int TurnDice = 2;

    private const int OpeningTurn = 1;

    private readonly SquareGridTopology _grid = new(ClassicSetup.Side, ClassicSetup.Side);
    private readonly MoveGenerator _moves;
    private readonly DiceProfile _dice;

    public ClassicRules()
        : this(DiceProfile.WeightedSixSided)
    {
    }

    public ClassicRules([Required] DiceProfile dice)
    {
        this._moves = new MoveGenerator(this._grid, ClassicPieces.Kinds);
        this._dice = dice;
    }

    public string Id => RulesetId;

    public string Name => "Zaturnava (classic)";

    public ICellNavigating Topology => this._grid;

    public GameState CreateInitialState()
    {
        ImmutableList<Seat> seats = ClassicSetup.Seats();
        return new GameState(
            ClassicSetup.InitialPosition(this._grid),
            seats,
            ClassicSetup.Teams(),
            seats[0].Id,
            OpeningTurn,
            seats.ToImmutableDictionary(seat => seat.Id, _ => 0),
            []
        );
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

    private static bool IsOpening(GameState state) => state.TurnNumber == OpeningTurn;

    private ImmutableList<GameAction> ChoicesOf(GameState state, SeatId seat) =>
        state.Dice.IsEmpty
            ? this.TurnChoicesOf(state, seat)
            : [new PassAction(seat), .. this.MovesOf(state, seat, this.RolledKinds(state).Contains)];

    /// <summary>At the start of a turn: roll, or move any piece, or pass - except on the opening turn.</summary>
    private ImmutableList<GameAction> TurnChoicesOf(GameState state, SeatId seat) =>
        IsOpening(state)
            ? [new RollAction(seat)]
            : [new PassAction(seat), new RollAction(seat), .. this.MovesOf(state, seat, _ => true)];

    private ImmutableHashSet<PieceKindId> RolledKinds(GameState state) =>
        [.. state.Dice.SelectMany(this._dice.KindsOf)];

    private IEnumerable<GameAction> MovesOf(GameState state, SeatId seat, Func<PieceKindId, bool> isMovable)
    {
        Seat ownerSeat = state.SeatOf(seat);
        Func<Piece, bool> isEnemy = IsEnemyOf(state, seat);
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat && isMovable(occupant.Value.Kind))
            .SelectMany(occupant => this._moves.MovesFrom(state.Position, occupant.Key, ownerSeat, isEnemy))
            .Select(move => new MoveAction(seat, move.From, move.To));
    }

    private static Func<Piece, bool> IsEnemyOf(GameState state, SeatId seat) =>
        piece => !state.Teams.AreAllied(piece.Owner, seat);

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
            RollAction => new Transition(state with { AwaitedDice = this.DiceFor(state) }, []),
            MoveAction move => this.AfterMove(state, move),
            _ => PassTurn(new Transition(state, [])),
        };

    private DiceRequest DiceFor(GameState state)
    {
        int count = IsOpening(state)
            ? OpeningDice
            : TurnDice;
        return new DiceRequest(count, this._dice.Sides);
    }

    /// <exception cref="IllegalActionException">The game is not waiting on these dice.</exception>
    private Transition Rolled(GameState state, DiceAction dice)
    {
        GameState rolled = IsAwaited(state, dice)
            ? state with { AwaitedDice = null, Dice = dice.Values }
            : throw new IllegalActionException(dice);
        return this.EndTurnIfStuck(new Transition(rolled, [new DiceRolled(dice.Actor, dice.Values)]));
    }

    private static bool IsAwaited(GameState state, DiceAction dice) =>
        state.AwaitedDice is { } request
        && dice.Actor == state.DecidingSeat
        && dice.Values.Count == request.Count
        && dice.Values.All(value => value >= 1 && value <= request.Sides);

    /// <summary>A deliberate move ends the turn; a rolled one uses up its die.</summary>
    private Transition AfterMove(GameState state, MoveAction move)
    {
        PieceKindId kind = state.Position.PieceAt(move.From)!.Kind; // the move is legal, so a piece stands there
        Transition moved = Moved(state, move);
        return state.Dice.IsEmpty
            ? PassTurn(moved)
            : this.UsedDie(moved, kind);
    }

    private Transition UsedDie(Transition moved, PieceKindId kind)
    {
        GameState state = moved.State;
        int die = state.Dice.First(value => this._dice.KindsOf(value).Contains(kind)); // a die allowed the move
        Transition used = moved with { State = state with { Dice = state.Dice.Remove(die) } };
        return this.EndTurnIfStuck(used);
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

    private static Transition PassTurn(Transition transition)
    {
        GameState state = transition.State;
        SeatId current = state.DecidingSeat!.Value; // a legal action was just taken, so someone was deciding
        SeatId next = new((current.Value + 1) % state.Seats.Count);
        GameState passed = state with
        {
            DecidingSeat = next,
            TurnNumber = state.TurnNumber + 1,
            AwaitedDice = null,
            Dice = [],
        };
        return new Transition(passed, transition.Events.Add(new TurnPassed(current, next)));
    }

    private static Transition Moved(GameState state, MoveAction move)
    {
        Piece mover = state.Position.PieceAt(move.From)!; // the move is legal, so a piece stands there
        Piece? captured = state.Position.PieceAt(move.To);
        GameState relocated = state with { Position = state.Position.Relocate(move.From, move.To) };
        Transition moved = new(relocated, [new PieceMoved(mover, move.From, move.To)]);
        return captured is null
            ? moved
            : Captured(moved, captured, move.Actor);
    }

    private static Transition Captured(Transition moved, Piece captured, SeatId captor)
    {
        GameState state = moved.State;
        int points = ClassicPieces.Points[captured.Kind];
        GameState scored = state with
        {
            Scores = state.Scores.SetItem(captor, state.Scores[captor] + points),
            Prisoners = state.Prisoners.Add(captured),
        };
        return new Transition(scored, moved.Events.Add(new PieceCaptured(captured, captor, points)));
    }
}
