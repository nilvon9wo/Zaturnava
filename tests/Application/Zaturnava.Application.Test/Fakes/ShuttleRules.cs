using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Application.Test.Fakes;

/// <summary>
/// One seat's Rook on a1, which may step to b1 and back: each turn two steps (as two dice), or a pass ending it,
/// until <paramref name="length"/> turns have been played. A step offered before a pass, so a bot taking the first
/// choice would shuttle back and forth; no pass at all, if <paramref name="mayPass"/> is false.
/// </summary>
internal sealed class ShuttleRules(int length, bool mayPass = true) : IRulesApplying
{
    public static readonly Piece Rook = new(new PieceId(1), new PieceKindId("rook"), new SeatId(0));

    private readonly int _length = length;
    private readonly bool _mayPass = mayPass;

    public string Id => "shuttle";

    public string Name => "Shuttle";

    public bool IsPreview => true;

    public ICellNavigating Topology { get; } = new TinyBoard();

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    public GameState CreateInitialState() =>
        new(
            Position.Empty.Place(new CellId(0), Rook),
            [new Seat(new SeatId(0), new Direction(0, 1))],
            TeamGraph.FromTeams([[new SeatId(0)]]),
            new SeatId(0),
            1,
            ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(0), 0),
            []
        )
        {
            Dice = [1, 1],
        };

    public ImmutableList<GameAction> LegalActions([Required] GameState state)
    {
        if (state.DecidingSeat is not { } seat)
        {
            return [];
        }

        CellId at = state.Position.Occupants.Single().Key;
        MoveAction step = new(seat, at, new CellId(1 - at.Value));
        return this._mayPass ? [step, new PassAction(seat)] : [step];
    }

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        action switch
        {
            MoveAction move => new Transition(
                this.Stepped(state with { Position = new Position(state.Position.Occupants.Remove(move.From)) }, move),
                []
            ),
            PassAction => new Transition(this.Next(state), []),
            _ => throw new IllegalActionException(action),
        };

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => false;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer? TimerOf([Required] GameState state) => null;

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => kind.Value;

    public string NameOf(SeatId seat) => $"Seat {seat.Value}";

    public int WorthOf(PieceKindId kind) => 1;

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) => 0;

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => false;

    public bool IsHeavy(PieceKindId kind) => false;

    public string NameOfDie(int value) => $"Die {value}";

    public bool MayUseDie([Required] GameState state, int value) => true;

    private GameState Stepped(GameState state, MoveAction move)
    {
        GameState moved = state with { Position = state.Position.Place(move.To, Rook), Dice = state.Dice.RemoveAt(0) };
        return moved.Dice.IsEmpty ? this.Next(moved) : moved;
    }

    private GameState Next(GameState state) =>
        state with
        {
            TurnNumber = state.TurnNumber + 1,
            Dice = [1, 1],
            DecidingSeat = state.TurnNumber >= this._length ? null : state.DecidingSeat,
        };
}
