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

    /// <summary>What Black's army has lost to Yellow, so it is not mostly whole: two Horses and a Boat, 10.</summary>
    private static readonly ImmutableList<Prisoner> Lost =
    [
        new(new Piece(new PieceId(20), new PieceKindId("horse"), Black), Yellow),
        new(new Piece(new PieceId(21), new PieceKindId("horse"), Black), Yellow),
        new(new Piece(new PieceId(22), new PieceKindId("boat"), Black), Yellow),
    ];

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
    [InlineData(5, true)]
    [InlineData(20, false)]
    public async Task Decide_WhenTheLeadIsNotClearOrItMayNotPropose_PlaysOn(int ourPoints, bool mayPropose)
    {
        // Arrange - Green rests; the lead is too small, or Black may not propose
        DecisionRequest request = Resting([Green], ourPoints);
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
            State = request.State with { Scores = request.State.Scores.SetItem(Yellow, 20), Prisoners = Lost },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(trailing, this._cancellation);

        // Assert
        Assert.Equal(new ProposeEndAction(Black, Resigns: true), action);
    }

    [Fact]
    public void AnswerTo_WhenAnAllyRestsWhileTheTeamTrailsButItsOwnArmyIsWhole_PlaysOn()
    {
        // Arrange - as above, but Black has lost nothing
        DecisionRequest request = Resting(people: [Green], ourPoints: 0);
        DecisionRequest trailing = request with
        {
            State = request.State with { Scores = request.State.Scores.SetItem(Yellow, 20) },
        };

        // Act
        GameAction? action = Ending.AnswerTo(trailing);

        // Assert
        Assert.Null(action);
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
                Prisoners = Lost,
            },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(asked, this._cancellation);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
    }

    [Theory]
    [InlineData("no one rests")]
    [InlineData("a person plays on")]
    public async Task Decide_WhenTheGameIsClearlyWonButNoOneRestsBesideIt_ProposesEndingItPlayingOn(string why)
    {
        // Arrange - a clear lead; no one rests, or Green rests but Yellow, a person, plays on
        bool isNoOneResting = why == "no one rests";
        DecisionRequest resting = Resting(people: isNoOneResting ? [] : [Green, Yellow], ourPoints: 20);
        DecisionRequest request = isNoOneResting
            ? resting with { State = resting.State with { Resigned = [] } }
            : resting;

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new ProposeEndAction(Black, Resigns: false), action);
    }

    [Theory]
    [InlineData(Ending.Patience - 1, false)]
    [InlineData(Ending.Patience, true)]
    public async Task Decide_WhenItsLastProposalWasRefused_ProposesAgainOnlyOnceItsPatienceIsOver(
        int turnsSince,
        bool proposes
    )
    {
        // Arrange - a clear lead, no one resting; Black's last proposal refused some turns ago
        DecisionRequest request = Resting(people: [Yellow], ourPoints: 20);
        GameState state = request.State with { Resigned = [] };
        DecisionRequest refused = request with
        {
            State = state with { RefusedOn = state.RefusedOn.Add(Black, state.TurnNumber - turnsSince) },
        };

        // Act
        GameAction action = await new MidasBot(new Random(1)).Decide(refused, this._cancellation);

        // Assert
        Assert.Equal(proposes ? new ProposeEndAction(Black, Resigns: false) : Pass, action);
    }

    [Theory]
    [InlineData(13, "Realist", true)]
    [InlineData(13, "Even", false)]
    [InlineData(8, "Realist", false)]
    [InlineData(20, "Even", true)]
    [InlineData(20, "Dogged", false)]
    public void AnswerTo_WhenAheadByAsMuchAsTheBotsTemperWants_ProposesEndingIt(
        int ourPoints,
        string temper,
        bool proposes
    )
    {
        // Arrange - no one rests; Black's team leads by 12 on the board, the rules' clear lead 10: Sage and Tactician
        // want three quarters of it, most bots all of it, Random twice it (F6: "each bot by its own threshold")
        DecisionRequest request = Resting(people: [], ourPoints);
        DecisionRequest playing = request with { State = request.State with { Resigned = [] } };
        Temper own = temper switch
        {
            "Realist" => Temper.Realist,
            "Dogged" => Temper.Dogged,
            _ => Temper.Even,
        };

        // Act
        GameAction? action = Ending.AnswerTo(playing, own);

        // Assert
        Assert.Equal(proposes ? new ProposeEndAction(Black, Resigns: false) : null, action);
    }

    [Theory]
    [InlineData("Even", true)]
    [InlineData("Dogged", false)]
    public void AnswerTo_WhenAskedBehindByLittleMoreThanItHopesFor_AgreesUnlessItHopesMoreThanMost(
        string temper,
        bool agrees
    )
    {
        // Arrange - as in the first test: Black's Elephant hopes for 4.5, which most bots count at 5.625, Random at 9;
        // Yellow leads by 6
        ScoringRules rules = new(Points())
        {
            MayPropose = true,
            Moves = Moving((new PieceKindId("elephant"), MovementRule.Slide(DirectionSets.Orthogonal))),
        };
        DecisionRequest asked = Asked(
            rules,
            [(0, Black, "elephant"), (1, Yellow, "raja"), (7, Black, "pawn")],
            theirPoints: 6
        );

        // Act
        GameAction? action = Ending.AnswerTo(asked, temper == "Dogged" ? Temper.Dogged : Temper.Even);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
    }

    [Theory]
    [InlineData("raja", null, true)]
    [InlineData("elephant", null, true)]
    [InlineData("pawn", 2, true)]
    [InlineData("pawn", 3, false)]
    [InlineData("horse", null, false)]
    public void AnswerTo_WhenClearlyAheadButTheEnemyStillHasHope_PlaysOnWithoutProposing(
        string kind,
        int? stepsToPromotion,
        bool hasHope
    )
    {
        // Arrange - a clear lead, no one resting; Yellow keeps one more piece beside its Boat: a Raja, a heavy piece,
        // a Pawn two or three steps from its promotion, or a Horse (the user's word of 2026-11-26)
        DecisionRequest request = Resting(people: [], ourPoints: 20);
        GameState state = request.State with { Resigned = [] };
        CellId cell = new(9);
        DecisionRequest hoping = request with
        {
            Rules = new ScoringRules(Points())
            {
                MayPropose = true,
                ClearLead = 5,
                PromotionSteps = stepsToPromotion is { } steps
                    ? ImmutableDictionary<CellId, int>.Empty.Add(cell, steps)
                    : [],
            },
            State = state with
            {
                Position = state.Position.Place(cell, new Piece(new PieceId(9), new PieceKindId(kind), Yellow)),
            },
        };

        // Act
        GameAction? action = Ending.AnswerTo(hoping);

        // Assert - whichever it keeps, the lead is clear: 7 or more on the board, the rules' clear lead 5
        Assert.Equal(hasHope ? null : new ProposeEndAction(Black, Resigns: false), action);
    }

    [Theory]
    [InlineData("elephant", true, false)]
    [InlineData("raja", true, false)]
    [InlineData("boat", true, true)]
    [InlineData("boat", false, false)]
    public void AnswerTo_WhenHopelessOnPointsButItsTeamKeepsALeaderOrAHeavyPieceOrAWholeArmy_ProposesNoDefeat(
        string kind,
        bool hasLost,
        bool proposes
    )
    {
        // Arrange - Black's one piece, which no move takes anywhere, so hopes for nothing; Yellow leads by 6 (the
        // user's games of 2026-11-27: Argus and Sage gave up with a Raja and a Rook left); Black's army beaten, or
        // whole, if it has lost nothing
        DecisionRequest request = Request([(Black, kind), (Yellow, "boat")], ourPoints: 0, theirPoints: 6);
        DecisionRequest beaten = request with { State = request.State with { Prisoners = hasLost ? Lost : [] } };

        // Act
        GameAction? action = Ending.AnswerTo(beaten);

        // Assert
        Assert.Equal(proposes ? new ProposeEndAction(Black, Resigns: false) : null, action);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void AnswerTo_WhenAskedBehindHopelessly_RefusesWhileItsOwnArmyIsMostlyWhole(int horsesLost, bool agrees)
    {
        // Arrange - as in the first test, Black far behind, by 60; its Elephant and Pawn, 5, left of an army that has
        // lost nothing, or a Horse (5 of 8 left: not three quarters) (the user, 2026-11-27: "red really shouldn't be
        // so willing to concede while it still has its entire army!")
        DecisionRequest request = Asked(
            new ScoringRules(Points()) { MayPropose = true },
            [(0, Black, "elephant"), (1, Yellow, "raja"), (7, Black, "pawn")],
            theirPoints: 60
        );
        DecisionRequest asked = request with
        {
            State = request.State with
            {
                Prisoners = [.. Lost.Take(horsesLost)],
            },
        };

        // Act
        GameAction? action = Ending.AnswerTo(asked);

        // Assert
        Assert.Equal(new AnswerEndAction(Black, agrees), action);
    }

    [Fact]
    public void AnswerTo_WhenAnAllysProposalWasRefusedLately_WaitsAsIfItWereItsOwn()
    {
        // Arrange - a clear lead; Green's proposal refused a turn ago
        DecisionRequest request = Resting(people: [], ourPoints: 20);
        GameState state = request.State with { Resigned = [] };
        DecisionRequest refused = request with
        {
            State = state with { RefusedOn = state.RefusedOn.Add(Green, state.TurnNumber - 1) },
        };

        // Act
        GameAction? action = Ending.AnswerTo(refused);

        // Assert
        Assert.Null(action);
    }

    /// <summary>
    /// Black's turn, Green resting, people at <paramref name="people"/>: Black's team has six Elephants on the board
    /// to Yellow's Boat, 20 ahead on the board, and <paramref name="ourPoints"/> points to none; a retired Red's Raja
    /// counts for nothing.
    /// </summary>
    private static DecisionRequest Resting(ImmutableHashSet<SeatId> people, int ourPoints)
    {
        DecisionRequest request = Request(
            [(Black, "elephant"), (Black, "elephant"), (Black, "elephant"), (Green, "elephant"), (Green, "elephant"),
                (Green, "elephant"), (Yellow, "boat"), (Red, "raja")],
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
            Prisoners = Lost,
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
