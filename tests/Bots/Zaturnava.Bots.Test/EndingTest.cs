using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
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
    [InlineData(5, 1, false)]
    [InlineData(6, 1, true)]
    [InlineData(1, 0.1, true)]
    public async Task Decide_WhenAskedBehind_AgreesOnlyIfItTrailsByMoreThanItCouldHopeToTakeBack(
        int theirPoints,
        double pace,
        bool agrees
    )
    {
        // Arrange - Black's Elephant slides next to Yellow's Raja, worth 5: a move away, of the ten it may expect, so
        // hoped for at 4.5, counted as 5.625; Black's Pawn, which does not move, hopes for nothing; at a tenth the
        // pace, the Elephant may expect but one move, so hopes for nothing either
        ScoringRules rules = new(Points())
        {
            MayPropose = true,
            Moves = Moving((new PieceKindId("elephant"), MovementRule.Slide(DirectionSets.Orthogonal))),
            Pace = pace,
        };
        DecisionRequest asked = Asked(
            rules,
            [(0, Black, "elephant"), (1, Yellow, "raja"), (7, Black, "pawn")],
            theirPoints
        );

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Decide_WhenItsLeaderCouldReachAnEnemyThrone_HopesForItsArmyUnlessTheThroneIsGuarded(
        bool isGuarded,
        bool agrees
    )
    {
        // Arrange - Black's Raja steps along to Yellow's throne, three steps off, its army worth 8 (hoped for at 5.6,
        // counted as 7), but no farther: retired Red's Pawn stands in its way. Retired Green's Pawn, next to
        // Yellow's Elephants, hopes for nothing. Black trails by 6.
        ScoringRules moving = new(Points())
        {
            MayPropose = true,
            Moves = Moving(
                (new PieceKindId("raja"), MovementRule.Step(DirectionSets.Orthogonal)),
                (new PieceKindId("pawn"), MovementRule.Step(DirectionSets.Orthogonal))
            ),
        };
        Piece guard = new(new PieceId(4), new PieceKindId("elephant"), Yellow);
        ScoringRules rules = isGuarded
            ? new(Points())
            {
                MayPropose = moving.MayPropose,
                Moves = moving.Moves,
                Attacks = [new Attack(guard, new CellId(6), new CellId(3))],
            }
            : moving;
        DecisionRequest request = Asked(
            rules,
            [
                (0, Black, "raja"), (4, Red, "pawn"), (5, Green, "pawn"), (6, Yellow, "elephant"),
                (7, Yellow, "elephant"),
            ],
            theirPoints: 6
        );
        DecisionRequest asked = request with
        {
            State = request.State with
            {
                Thrones = ImmutableDictionary<SeatId, CellId>.Empty
                    .Add(Black, new CellId(1))
                    .Add(Yellow, new CellId(3)),
                Retired = [Green, Red],
            },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
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
    public async Task Decide_WhenAnAllyRestsWhileTheTeamTrailsClearly_GivesUpItsTurnsToo()
    {
        // Arrange - Green, a person, rests; Black's team trails by 20 points, though ahead on the board
        DecisionRequest request = Resting(people: [Green], ourPoints: 0);
        DecisionRequest trailing = request with
        {
            State = request.State with { Scores = request.State.Scores.SetItem(Yellow, 20) },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(trailing, this._cancellation);

        // Assert
        Assert.Equal(new ProposeEndAction(Black, Resigns: true), action);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, false)]
    public async Task Decide_WhenAskedWhileAnAllyRestsAndTheTeamTrails_AgreesIfTheRulesCallItClear(
        int clearLead,
        bool agrees
    )
    {
        // Arrange - Green rests; Black's team trails by 2 points, though not hopelessly: Green's Elephant slides
        // next to Yellow's, worth 4
        DecisionRequest request = Resting(people: [Green], ourPoints: 0);
        DecisionRequest asked = request with
        {
            Rules = new ScoringRules(Points())
            {
                MayPropose = true,
                ClearLead = clearLead,
                Moves = Moving((new PieceKindId("elephant"), MovementRule.Slide(DirectionSets.Orthogonal))),
            },
            State = request.State with
            {
                Seats = [new Seat(Black, new Direction(1, 0)), new Seat(Green, new Direction(1, 0))],
                Scores = request.State.Scores.SetItem(Yellow, 2),
                Proposal = new EndProposal(Yellow, false, Black, null),
            },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
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

    /// <summary>
    /// Black asked to end the game, which Yellow proposed, under <paramref name="rules"/>, with each piece on its
    /// cell, Black facing up the line and Yellow down it, and Yellow ahead by <paramref name="theirPoints"/>.
    /// </summary>
    private static DecisionRequest Asked(
        ScoringRules rules,
        ImmutableList<(int Cell, SeatId Owner, string Kind)> pieces,
        int theirPoints
    )
    {
        GameState state = ScoringRules.State() with
        {
            Seats = [new Seat(Black, new Direction(1, 0)), new Seat(Yellow, new Direction(-1, 0))],
            Teams = TeamGraph.FromTeams([[Black, Green], [Yellow, Red]]),
            Scores = ImmutableDictionary<SeatId, int>.Empty.Add(Yellow, theirPoints),
            Proposal = new EndProposal(Yellow, false, Black, null),
            Position = pieces
                .Select((piece, index) => (piece, index))
                .Aggregate(
                    Position.Empty,
                    (position, placed) => position.Place(
                        new CellId(placed.piece.Cell),
                        new Piece(new PieceId(placed.index), new PieceKindId(placed.piece.Kind), placed.piece.Owner)
                    )
                ),
        };
        return new DecisionRequest(rules, state, Black, rules.LegalActions(state));
    }

    private static ImmutableDictionary<PieceKindId, ImmutableList<MovementRule>> Moving(
        params (PieceKindId Kind, MovementRule Rule)[] moves
    ) =>
        moves.ToImmutableDictionary(move => move.Kind, move => ImmutableList.Create(move.Rule));

    private static ImmutableDictionary<GameAction, int> Points() =>
        ImmutableDictionary<GameAction, int>.Empty.Add(Pass, 0);
}
