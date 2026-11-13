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
/// (<see cref="TinyBoard"/>), where each seat has a Pawn, and the first a King, that never move. If
/// <paramref name="isDealt"/>, each game is dealt anew: its seed is the first seat's score, to tell it by, and its
/// terrain 1 if dealt with the rest. Given a
/// <paramref name="failure"/>, applying any action throws it, as rules that no longer replay a game might. If
/// <paramref name="hasFaceOff"/>, the first seat's King faces the second seat's Pawn, which it may call a face-off
/// against whoever decides, taking the King off the board. Enough to drive the application without any real game.
/// </summary>
internal sealed class TinyRules(int length, bool isDealt = false, Exception? failure = null, bool hasFaceOff = false)
    : IRulesApplying
{
    public const string RulesetId = "tiny";
    public const int SeatCount = 2;
    public const string Timer = "tiny-turn";

    private readonly int _length = length;

    public string Id => RulesetId;

    /// <summary>"Tiny"; "Tiny 3", on a board set to 3 cells a side (<see cref="Sized"/>).</summary>
    public string Name => this.IsSized ? $"Tiny {this.SizableBoard!.Side}" : "Tiny";

    /// <summary>The board a game's creator may set to another size, if set: any of 2 cells a side or more.</summary>
    public BoardSize? SizableBoard { get; init; }

    private bool IsSized { get; init; }

    /// <summary>The rules on <paramref name="board"/>, named for its side, if it may be set to it.</summary>
    public IRulesApplying? Sized([Required] BoardSize board) =>
        this.SizableBoard is null || board.Side < 2
            ? null
            : new TinyRules(this._length, this.IsDealt, failure, hasFaceOff)
            {
                IsDraftable = this.IsDraftable,
                Filled = this.Filled,
                SizableBoard = board,
                IsSized = true,
            };

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

    public bool IsDealt { get; } = isDealt;

    /// <summary>
    /// Whether its players may draft their armies, as every game dealt may, unless <see cref="IsDraftable"/> is set:
    /// a game drafted is told by its draft, kept by the seed it was dealt by, and nothing else.
    /// </summary>
    public bool IsDraftable { get; init; } = isDealt;

    /// <summary>What having a seat's army filled at random makes of the game; unless set, it is not allowed.</summary>
    public Func<GameState, GameState>? Filled { get; init; }

    /// <summary>The draft begun: the first seat with the budget set, 0 if none; and the drafter set, if any.</summary>
    public GameState Drafted([Required] GameState dealt, int seed, DraftSettings? settings = null) =>
        dealt with
        {
            Draft = new ArmyDraft(
                seed,
                ImmutableDictionary<SeatId, int>.Empty.Add(new SeatId(0), settings?.Budget ?? 0),
                []
            )
            {
                Drafter = settings?.Drafter,
            },
        };

    public GameState Deal(
        [Required] GameState initial,
        int seed,
        bool areAlike = false,
        bool dealsTerrain = false,
        bool dealsPalaces = false,
        bool dealsPalacePieces = false,
        bool dealsEdgePalaces = false,
        int edition = 0
    ) =>
        initial with
        {
            Scores = initial.Scores.SetItem(new SeatId(0), seed).SetItem(new SeatId(1), areAlike ? 1 : 0),
            Terrain = dealsTerrain ? 1 : 0,
            Palaces = dealsPalaces ? [] : null,
            HasPalacePieces = dealsPalacePieces,
            HasEdgePalaces = dealsEdgePalaces,
            Edition = edition,
        };

    public ImmutableList<GameAction> LegalActions([Required] GameState state) =>
        state.DecidingSeat is { } seat
            ? [new PassAction(seat)]
            : [];

    public Transition Apply([Required] GameState state, [Required] GameAction action)
    {
        if (failure is not null)
        {
            throw failure;
        }

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
            (FaceOffAction faceOff, _) => state with
            {
                Position = new Position(state.Position.Occupants.Remove(faceOff.Caller)),
            },
            (FillArmyAction, _) when this.Filled is { } fill => fill(state),
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

    public ImmutableList<GameAction> ChancesOf([Required] GameState state, SeatId seat) =>
        hasFaceOff && seat.Value == 0 && state.Position.PieceAt(new CellId(1)) is not null
            ? [new FaceOffAction(seat, new CellId(1), new CellId(3))]
            : [];

    public DecisionTimer? TimerOf([Required] GameState state) =>
        state.DecidingSeat?.Value == 1
            ? new DecisionTimer(Timer, new PassAction(new SeatId(1)))
            : null;

    public GameAction Parse([Required] ActionDescriptor descriptor) => ActionParser.Standard.Parse(descriptor);

    public string NameOf(PieceKindId kind) => kind.Value;

    public string NameOf(SeatId seat) => $"Seat {seat.Value}";

    /// <summary>How many squares each kind's card shows it reaching; none, for a kind not here.</summary>
    public ImmutableDictionary<PieceKindId, int> Reaches { get; init; } = [];

    public PieceGuide GuideOf(PieceKindId kind) =>
        new(new PieceKind(kind, []))
        {
            Diagram = new MoveDiagram(9, [.. Enumerable.Range(0, this.Reaches.GetValueOrDefault(kind))], [], []),
        };

    /// <summary>What a King is worth; every other kind is worth 1.</summary>
    public int KingWorth { get; init; } = 1;

    public int WorthOf(PieceKindId kind) => kind == King ? this.KingWorth : 1;

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
