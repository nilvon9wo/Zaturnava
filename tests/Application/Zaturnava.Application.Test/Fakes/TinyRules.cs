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
/// The smallest ruleset that still ends: two seats take turns passing until <paramref name="length"/>
/// turns have been played, with no outcome; the second seat has only so long to pass, and may end the game
/// whenever it likes, by proposing so, as its winner. Either may vote on waiting for the other while it
/// decides; a seat passed while away may say it is back. Its board is a row of four cells
/// (<see cref="TinyBoard"/>), where each seat has a Pawn, and the first a King, that never move.
/// Enough to drive the application without any real game.
/// </summary>
internal sealed class TinyRules(int length) : IRulesApplying
{
    public const string RulesetId = "tiny";
    public const int SeatCount = 2;
    public const string Timer = "tiny-turn";

    private readonly int _length = length;

    public string Id => RulesetId;

    public string Name => "Tiny";

    public static readonly PieceKindId Pawn = new("Pawn");
    public static readonly PieceKindId King = new("King");

    public ICellNavigating Topology { get; } = new TinyBoard();

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    public GameState CreateInitialState() =>
        new(
            Position.Empty
                .Place(new CellId(0), new Piece(new PieceId(0), Pawn, new SeatId(0)))
                .Place(new CellId(1), new Piece(new PieceId(1), King, new SeatId(0)))
                .Place(new CellId(3), new Piece(new PieceId(2), Pawn, new SeatId(1))),
            [new Seat(new SeatId(0), new Direction(0, 1)), new Seat(new SeatId(1), new Direction(0, -1))],
            TeamGraph.FromTeams([[new SeatId(0)], [new SeatId(1)]]),
            new SeatId(0),
            1,
            [],
            []
        );

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        state.DecidingSeat is { } seat
            ? [new PassAction(seat)]
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action)
    {
        bool isLegal = this.LegalActions(state).Contains(action);
        GameState next = (action, isLegal) switch
        {
            (ProposeEndAction end, _) => state with
            {
                DecidingSeat = null,
                Outcome = new GameOutcome([end.Actor], "agreement", "the second seat ended it"),
            },
            (VoteAwayAction vote, _) => state with { Absence = Voted(state.Absence, vote) },
            (PassAwayAction pass, _) => this.After(state) with { Absence = Missing(state.Absence, pass.Actor) },
            (RejoinAction rejoin, _) => state with
            {
                Absence = state.Absence with { Returning = state.Absence.Returning.Add(rejoin.Actor) },
            },
            (_, true) => this.After(state),
            _ => throw new IllegalActionException(action),
        };
        return new Transition(next, []);
    }

    public bool MayOffer([Required] GameState state) => false;

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) => true;

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => seat.Value == SeatCount - 1;

    public bool MayRejoin([Required] GameState state, SeatId seat) => state.Absence.MissedBy(seat) > 0;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        voter != absent && state.DecidingSeat is not null;

    public DecisionTimer? TimerOf([Required] GameState state) =>
        state.DecidingSeat?.Value == 1
            ? new DecisionTimer(Timer, new PassAction(new SeatId(1)))
            : null;

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

    private static Absence Voted(Absence absence, VoteAwayAction vote)
    {
        AwayVote stop = new(vote.Actor, vote.Absent);
        return absence with { StopVotes = vote.Stops ? absence.StopVotes.Add(stop) : absence.StopVotes.Remove(stop) };
    }

    private static Absence Missing(Absence absence, SeatId seat) =>
        absence with
        {
            Missed = absence.Missed.SetItem(seat, absence.MissedBy(seat) + 1),
            Returning = absence.Returning.Remove(seat),
        };

    private GameState After(GameState state)
    {
        int turn = state.TurnNumber + 1;
        SeatId? next = turn > this._length
            ? null
            : new SeatId((state.DecidingSeat!.Value.Value + 1) % SeatCount);
        return state with { TurnNumber = turn, DecidingSeat = next };
    }
}
