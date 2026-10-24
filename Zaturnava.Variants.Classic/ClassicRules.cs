using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic;

/// <summary>
/// Zaturnava's base rules. So far: deliberate moves and passing (rules.md §4), captures scored by
/// piece (§5), and clockwise turns from seat 0. Dice, throne control, doubles and escape, promotion
/// and ransom, boat triumph, bonuses and game end follow; docs/rules.md lists what is implemented.
/// </summary>
public sealed class ClassicRules : IRulesApplying
{
    public const string RulesetId = "classic";

    private readonly SquareGridTopology _grid = new(ClassicSetup.Side, ClassicSetup.Side);
    private readonly MoveGenerator _moves;

    public ClassicRules() => this._moves = new MoveGenerator(this._grid, ClassicPieces.Kinds);

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
            1,
            seats.ToImmutableDictionary(seat => seat.Id, _ => 0),
            []
        );
    }

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        state.DecidingSeat is { } seat
            ? [new PassAction(seat), .. this.MovesOf(state, seat)]
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action)
    {
        bool isLegal = this.LegalActions(state).Contains(action);
        Transition acted = isLegal
            ? Act(state, action)
            : throw new IllegalActionException(action);
        return PassTurn(acted);
    }

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => ClassicPieces.TraditionalNames[kind];

    public string NameOf(SeatId seat) => ClassicSetup.SeatNames[seat.Value];

    private static Transition PassTurn(Transition transition)
    {
        GameState state = transition.State;
        SeatId current = state.DecidingSeat!.Value; // a legal action was just taken, so someone was deciding
        SeatId next = new((current.Value + 1) % state.Seats.Count);
        GameState passed = state with { DecidingSeat = next, TurnNumber = state.TurnNumber + 1 };
        return new Transition(passed, transition.Events.Add(new TurnPassed(current, next)));
    }

    private IEnumerable<GameAction> MovesOf(GameState state, SeatId seat)
    {
        Seat ownerSeat = state.SeatOf(seat);
        Func<Piece, bool> isEnemy = IsEnemyOf(state, seat);
        return state.Position.Occupants
            .Where(occupant => occupant.Value.Owner == seat)
            .SelectMany(occupant => this._moves.MovesFrom(state.Position, occupant.Key, ownerSeat, isEnemy))
            .Select(move => new MoveAction(seat, move.From, move.To));
    }

    private static Func<Piece, bool> IsEnemyOf(GameState state, SeatId seat) =>
        piece => !state.Teams.AreAllied(piece.Owner, seat);

    private static Transition Act(GameState state, GameAction action) =>
        action is MoveAction move
            ? Moved(state, move)
            : new Transition(state, []);

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
