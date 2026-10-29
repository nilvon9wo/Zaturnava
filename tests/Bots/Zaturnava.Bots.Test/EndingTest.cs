using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Bots.Test;

/// <summary>How every bot ends a game, shown through Midas; seats 0 and 2 are allied against 1 and 3.</summary>
public sealed class EndingTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly PassAction Pass = new(Black);

    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("pawn", "elephant", true)]
    [InlineData("pawn", "raja", true)]
    [InlineData("pawn", "horse", false)]
    [InlineData("elephant", "elephant", false)]
    [InlineData("raja", "elephant", false)]
    public async Task Decide_WhenAskedFarBehind_AgreesOnlyIfItsTeamHasNoMajorPieceLeftAndTheEnemyHas(
        string ours,
        string theirs,
        bool agrees
    )
    {
        // Arrange - Black's team trails by 20 points, and by 10 or more on the board: four of theirs to one
        DecisionRequest request = Request(
            [(Black, ours), (Yellow, theirs), (Yellow, theirs), (Red, theirs), (Red, theirs)],
            ourPoints: 0,
            theirPoints: 20
        );
        DecisionRequest asked = request with
        {
            State = request.State with { Proposal = new EndProposal(Yellow, false, Black, null) },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
    }

    [Theory]
    [InlineData(0, 4, 0)]
    [InlineData(0, 20, 0)]
    [InlineData(5, 0, 0)]
    public async Task Decide_WhenAskedButNotFarEnoughBehind_RefusesThoughItHasNoMajorPiece(
        int ourPoints,
        int theirPoints,
        int extraEnemyElephants
    )
    {
        // Arrange - Black has a Pawn; Yellow an Elephant, and maybe more
        ImmutableList<(SeatId, string)> pieces =
        [
            (Black, "pawn"),
            (Yellow, "elephant"),
            .. Enumerable.Repeat((Red, "elephant"), extraEnemyElephants / 4),
        ];
        DecisionRequest request = Request(pieces, ourPoints, theirPoints);
        DecisionRequest asked = request with
        {
            State = request.State with { Proposal = new EndProposal(Yellow, false, Black, null) },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, ourPoints > theirPoints), action);
    }

    [Fact]
    public async Task Decide_WhenAnAllyRestsOnAClearLeadAndNoOtherPersonPlays_GivesUpItsTurnsToo()
    {
        // Arrange - Green, a person, rests; Black's team leads by 20 points and 12 on the board
        DecisionRequest request = Resting(people: [Green], ourPoints: 20);

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new ProposeEndAction(Black, Resigns: true), action);
    }

    [Theory]
    [InlineData("person", 20, true)]
    [InlineData("lead", 5, true)]
    [InlineData("rule", 20, false)]
    public async Task Decide_WhenAnotherPersonPlaysOrTheLeadIsNotClearOrItMayNotPropose_PlaysOn(
        string why,
        int ourPoints,
        bool mayPropose
    )
    {
        // Arrange - Green rests; Yellow is a person too, or the lead is too small, or Black may not propose
        ImmutableHashSet<SeatId> people = why == "person" ? [Green, Yellow] : [Green];
        DecisionRequest request = Resting(people, ourPoints);
        DecisionRequest asked = request with { Rules = new ScoringRules(Points()) { MayPropose = mayPropose } };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(Pass, action);
    }

    [Fact]
    public async Task Decide_WhenNoAllyRests_PlaysOn()
    {
        // Arrange - a clear lead, but no one rests
        DecisionRequest request = Resting(people: [], ourPoints: 20);
        DecisionRequest playing = request with { State = request.State with { Resigned = [] } };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(playing, this._cancellation);

        // Assert
        Assert.Equal(Pass, action);
    }

    /// <summary>
    /// Black's turn, Green resting, people at <paramref name="people"/>: Black's team has four Elephants on
    /// the board to Yellow's one, and <paramref name="ourPoints"/> points to none; a retired Red's Raja counts
    /// for nothing.
    /// </summary>
    private static DecisionRequest Resting(ImmutableHashSet<SeatId> people, int ourPoints)
    {
        DecisionRequest request = Request(
            [(Black, "elephant"), (Black, "elephant"), (Green, "elephant"), (Green, "elephant"), (Yellow, "elephant"),
                (Red, "raja")],
            ourPoints,
            0
        );
        return request with
        {
            State = request.State with { Resigned = [Green], Retired = [Red] },
            People = people,
        };
    }

    /// <summary>Black to decide, with these pieces on the board and points; it may propose ending the game.</summary>
    private static DecisionRequest Request(
        ImmutableList<(SeatId Owner, string Kind)> pieces,
        int ourPoints,
        int theirPoints
    )
    {
        ScoringRules rules = new(Points()) { MayPropose = true };
        GameState state = ScoringRules.State() with
        {
            Teams = TeamGraph.FromTeams([[Black, Green], [Yellow, Red]]),
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Green, ourPoints).Add(Yellow, theirPoints),
            Position = pieces
                .Select((piece, index) => (piece, index))
                .Aggregate(
                    Position.Empty,
                    (position, placed) => position.Place(
                        new CellId(placed.index),
                        new Piece(new PieceId(placed.index), new PieceKindId(placed.piece.Kind), placed.piece.Owner)
                    )
                ),
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }

    private static ImmutableDictionary<GameAction, int> Points() =>
        ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0);
}
