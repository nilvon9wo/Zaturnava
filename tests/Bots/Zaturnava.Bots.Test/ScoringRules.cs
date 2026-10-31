using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>
/// Rules in which each action scores whatever <paramref name="points"/> says for it (nothing, if it
/// says nothing), for its actor; all a bot can learn from them is what each action is worth, what each
/// kind of piece is worth (<see cref="Worth"/>), and that the Raja leads. Any seat may offer once its
/// turn is over.
/// </summary>
internal sealed class ScoringRules(ImmutableDictionary<GameAction, int> points) : IRulesApplying
{
    public static readonly SeatId Seat = new(0);

    public static readonly ImmutableDictionary<PieceKindId, int> Worth =
        ImmutableDictionary<PieceKindId, int>.Empty
            .Add(new PieceKindId("pawn"), 1)
            .Add(new PieceKindId("horse"), 3)
            .Add(new PieceKindId("boat"), 4)
            .Add(new PieceKindId("elephant"), 4)
            .Add(new PieceKindId("raja"), 5);

    private readonly ImmutableDictionary<GameAction, int> _points = points;

    public string Id => "scoring";

    public string Name => "Scoring";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones => [];

    public static GameState State() => new(Position.Empty, [], TeamGraph.FromTeams([]), Seat, 1, [], []);

    public GameState CreateInitialState() => State();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => [.. this._points.Keys];

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        new(
            state with
            {
                Scores = state.Scores.SetItem(
                    action.Actor,
                    state.Scores.GetValueOrDefault(action.Actor) + this._points.GetValueOrDefault(action)
                ),
            },
            []
        );

    public bool MayOffer([Required] GameState state) => true;

    /// <summary>The prisoners that may not go home yet; every other may.</summary>
    public ImmutableHashSet<PieceId> Waiting { get; init; } = [];

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        !this.Waiting.Contains(prisoner.Piece.Id);

    /// <summary>Whether a seat may propose ending the game; no one may, unless set.</summary>
    public bool MayPropose { get; init; }

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => this.MayPropose;

    public bool MayRejoin([Required] GameState state, SeatId seat) => false;

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) => false;

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public int WorthOf(PieceKindId kind) => Worth[kind];

    /// <summary>What accepting a ransom scores its receiver, for each prisoner asked for; none, unless set.</summary>
    public int RansomPoints { get; init; }

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        this.RansomPoints * offer.Asked.Count;

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) => [];

    public bool IsLeader(PieceKindId kind) => kind == new PieceKindId("raja");

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => true;
}
