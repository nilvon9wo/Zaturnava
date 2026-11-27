using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Bots;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Decisions;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027;

namespace Net.NowhereAtAll.Zaturnava.BotRunner.Test;

/// <summary>
/// The strategic positions of the bots' strategy plan (docs/bots/strategy-plan.md), played by Sage on Chaturaji
/// 2027's own board, without dice, so that every move is Sage's to choose.
/// </summary>
public sealed class StrategicPositionsTest
{
    private const int Side = 8;

    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);

    private readonly Chaturaji2027Rules _rules = new();
    private readonly CancellationToken _cancellation = TestContext.Current.CancellationToken;

    [Fact]
    public async Task Sage_WhenItsPawnIsHeldBackByAnAllysBackToBack_SwapsThemRatherThanShuffle()
    {
        // Arrange - Black's Pawn on d4 faces Green's, its ally's, on d5, neither able to step on; Black's Raja holds
        // Red's throne, which any move of it would give up; Yellow's six Pawns, far off in the a8 corner, keep the game
        // going, and keep it from being an endgame, so that Sage judges its own turn only
        CellId pawn = CellAt(3, 3);
        CellId ahead = CellAt(3, 4);
        GameState state = this.BlackDeciding(
            [
                (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
                (ahead, new Piece(new PieceId(2), Chaturaji2027Pieces.Pawn, Green)),
                (this._rules.Thrones[Red], new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
                .. new[] { (0, 7), (1, 7), (2, 7), (0, 6), (1, 6), (0, 5) }.Select((cell, index) => (
                    CellAt(cell.Item1, cell.Item2),
                    new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Pawn, Yellow)
                )),
            ]
        );
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request, this._cancellation);

        // Assert
        Assert.Equal(new MoveAction(Black, pawn, ahead), action);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Sage_WhenAPromotionIntoAnAllysRajaWouldStandAttackedOrNot_PromotesOnlyIntoWhatSurvives(
        bool isAttacked
    )
    {
        // Arrange - plan step 2, "promotion with eyes open" (seen 2026-11-25: a Pawn promoted into a Raja under
        // attack, taken at once): Black's Pawn on d8, its far edge, may become Green's Raja or Black's own Elephant
        // (the Rook), both held by Red; Red's Elephant on h8 attacks d8 along the empty row; Black's Raja holds Red's
        // throne, and Yellow's
        // Pawns, far off, keep it from being an endgame
        CellId pawn = CellAt(3, 7);
        Piece greenRaja = new(new PieceId(4), Chaturaji2027Pieces.Raja, Green);
        Piece blackRook = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Black);
        GameState state = this.BlackDeciding(
            [
                (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
                (CellAt(7, isAttacked ? 7 : 6), new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, Red)),
                (this._rules.Thrones[Red], new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
                (CellAt(7, 1), new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, Red)),
                .. new[] { (0, 2), (1, 2), (0, 1), (1, 1), (0, 0), (1, 0) }.Select((cell, index) => (
                    CellAt(cell.Item1, cell.Item2),
                    new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Pawn, Yellow)
                )),
            ]
        ) with
        {
            Prisoners = [new Prisoner(greenRaja, Red), new Prisoner(blackRook, Red)],
        };
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request, this._cancellation);

        // Assert - offered either way; the Raja not brought back where it would be taken at once, but promoted into
        // when safe, an ally's leader back on the board (the Elephant on h7 attacks the seventh row, not d8)
        Assert.Equal(
            (true, !isAttacked),
            (
                request.LegalActions.Contains(new PromoteAction(Black, pawn, greenRaja.Id)),
                action == new PromoteAction(Black, pawn, greenRaja.Id)
            )
        );
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    public async Task Sage_WhenAnAllyMovesBeforeTheEnemyAttackingTheCell_PromotesIntoTheAllysPieceNotItsOwn(int seed)
    {
        // Arrange - plan step 2, with step 1's turn order (Stated 2026-11-26: "it might be advantageous for black to
        // promote to green if it can rely on green attacking or defending something before black would get its next
        // move"): Black's Pawn on c8, its far edge, may become Green's Elephant or its own, both held by Red; Red's
        // Horse on b6 forks c8 and Black's Raja on d5. Promoting is not Black's move: it still has one, and must spend
        // it saving its Raja. Then Yellow, Green and Red move before Black does again: Black's own Elephant would wait
        // for Black's turn, and Red would take it first; Green's moves before Red, and gets away. The other Rajas on
        // their thrones; Yellow's Pawns, far off, keep it from being an endgame
        CellId pawn = CellAt(2, 7);
        Piece greenRook = new(new PieceId(4), Chaturaji2027Pieces.Elephant, Green);
        Piece blackRook = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Black);
        GameState state = this.BlackDeciding(
            [
                (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
                (CellAt(1, 5), new Piece(new PieceId(2), Chaturaji2027Pieces.Horse, Red)),
                (CellAt(3, 4), new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, Black)),
                .. new[] { Yellow, Green, Red }.Select((seat, index) => (
                    this._rules.Thrones[seat],
                    new Piece(new PieceId(7 + index), Chaturaji2027Pieces.Raja, seat)
                )),
                .. new[] { (0, 2), (1, 2), (0, 1), (1, 1), (0, 0), (1, 0) }.Select((cell, index) => (
                    CellAt(cell.Item1, cell.Item2),
                    new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Pawn, Yellow)
                )),
            ]
        ) with
        {
            Prisoners = [new Prisoner(blackRook, Red), new Prisoner(greenRook, Red)],
        };
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(seed)).Decide(request, this._cancellation);

        // Assert - both offered; Green's chosen, whatever chance breaks ties
        Assert.Equal(
            (true, new PromoteAction(Black, pawn, greenRook.Id)),
            (request.LegalActions.Contains(new PromoteAction(Black, pawn, blackRook.Id)), action)
        );
    }

    [Fact]
    public async Task Sage_WhenItsOwnRookWouldWinWhereAnAllysRajaWouldTakeBackItsArmy_PromotesIntoTheRook()
    {
        // Arrange - plan step 2, the command a promotion brings (F2-F4, seen 2026-11-25: "into a Raja of another army
        // rather than the Rook that wins, losing control of an ally's pieces"): Black's Pawn on d8, its far edge, may
        // become Green's Raja or Black's own Elephant (the Rook), both held by Red. Green has no Raja, so Black
        // commands its Horse and Boat; Green's Raja back would take them back. Red's Raja on d5, down the empty file,
        // is the Rook's to take this very turn; Yellow's Raja on its throne
        CellId pawn = CellAt(3, 7);
        Piece greenRaja = new(new PieceId(4), Chaturaji2027Pieces.Raja, Green);
        Piece blackRook = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Black);
        GameState state = this.BlackDeciding(
            (pawn, new Piece(new PieceId(1), Chaturaji2027Pieces.Pawn, Black)),
            (CellAt(3, 4), new Piece(new PieceId(2), Chaturaji2027Pieces.Raja, Red)),
            (this._rules.Thrones[Black], new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
            (this._rules.Thrones[Yellow], new Piece(new PieceId(6), Chaturaji2027Pieces.Raja, Yellow)),
            (CellAt(6, 6), new Piece(new PieceId(7), Chaturaji2027Pieces.Horse, Green)),
            (CellAt(5, 5), new Piece(new PieceId(8), Chaturaji2027Pieces.Boat, Green)),
            (CellAt(0, 2), new Piece(new PieceId(9), Chaturaji2027Pieces.Pawn, Yellow))
        ) with
        {
            Prisoners = [new Prisoner(greenRaja, Red), new Prisoner(blackRook, Red)],
        };
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction action = await new SageBot(new Random(7)).Decide(request, this._cancellation);

        // Assert - both offered; the Rook chosen
        Assert.Equal(
            (true, new PromoteAction(Black, pawn, blackRook.Id)),
            (request.LegalActions.Contains(new PromoteAction(Black, pawn, greenRaja.Id)), action)
        );
    }

    [Fact]
    public async Task Sage_WhenTwoElephantsFaceALoneRaja_PlaysItToTheEndAndWins()
    {
        // Arrange - the endgame plan's test (docs/bots/strategy-plan.md, "Endgame tables"): Black's two Elephants (the
        // Rooks) and its Raja against Red's Raja alone in the middle, no one else on the board; Sage plays every side,
        // so the lone Raja flees as well as Sage can
        GameState state = this.BlackDeciding(
            (CellAt(0, 0), new Piece(new PieceId(1), Chaturaji2027Pieces.Elephant, Black)),
            (CellAt(7, 0), new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, Black)),
            (CellAt(4, 0), new Piece(new PieceId(3), Chaturaji2027Pieces.Raja, Black)),
            (CellAt(3, 4), new Piece(new PieceId(4), Chaturaji2027Pieces.Raja, Red))
        );
        // its endgame search ended by its budget alone, not the clock, so that it plays alike on any machine
        // (2026-11-26: on CI, the clock cut it short, every decision, and it played on for eight minutes and lost)
        SageBot sage = new(new Random(7)) { EndgameLimit = TimeSpan.FromMinutes(10) };

        // Act
        int decisions = 0;
        while (state.Outcome is null && decisions < MostEndgameDecisions)
        {
            SeatId seat = state.DecidingSeat!.Value;
            DecisionRequest request = new(this._rules, state, seat, this._rules.LegalActions(state));
            state = this._rules.Apply(state, await sage.Decide(request, this._cancellation)).State;
            decisions++;
        }

        // Assert
        Assert.Equal(
            (true, true),
            (state.Outcome is not null, state.Outcome?.Winners.Contains(Black) == true)
        );
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Exposure_WhenAnEnemyArmyIsCommandedByAnEnemyWhoMovesFirstOrNot_CountsWhatItCouldTake(
        bool hasYellowRaja,
        bool isLost
    )
    {
        // Arrange - plan step 1, whose move is next (seen 2026-11-25: a Yellow Rook, moved on Red's turn by Red's
        // Raja): Green has just moved, so Red moves next, then Black, then Yellow; Yellow's Elephant on c3 attacks
        // Black's on f3 along the empty row. While Yellow has its Raja, only Yellow moves its army, after Black, who
        // saves the Elephant first; without one, Red's Raja commands Yellow's army too, and Red moves before Black
        CellId blackElephant = CellAt(5, 2);
        GameState state = this.Deciding(
            Green,
            [
                (CellAt(2, 2), new Piece(new PieceId(1), Chaturaji2027Pieces.Elephant, Yellow)),
                (blackElephant, new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, Black)),
                .. new[] { Black, Green, Red, Yellow }
                    .Where(seat => hasYellowRaja || seat != Yellow)
                    .Select((seat, index) => (
                        this._rules.Thrones[seat],
                        new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Raja, seat)
                    )),
            ]
        );
        DecisionRequest request = new(this._rules, state, Green, this._rules.LegalActions(state));

        // Act
        int exposure = Careful.Exposure(request, state);

        // Assert
        Assert.Equal(isLost ? this._rules.KeepingWorthOf(Chaturaji2027Pieces.Elephant) : 0, exposure);
    }

    [Fact]
    public void Chances_WhenAnAllyAttacksAnEnemyPieceBeforeOrAfterItsArmyMoves_CountItTakenOrFled()
    {
        // Arrange - plan step 1, whose move is next: Green has just moved, so Red moves next, then Black, then Yellow;
        // Black's Horse on d2 attacks an Elephant on f3, free to flee, and unable to take the Horse back. Red's
        // Elephant flees on Red's turn, before Black's; Yellow's is taken on Black's, before Yellow's
        SageWeights chances = new(0, 0, 0, 0, 0, 0, 1);
        double ValueWith(SeatId owner)
        {
            GameState state = this.Deciding(
                Green,
                [
                    (CellAt(3, 1), new Piece(new PieceId(1), Chaturaji2027Pieces.Horse, Black)),
                    (CellAt(5, 2), new Piece(new PieceId(2), Chaturaji2027Pieces.Elephant, owner)),
                    .. new[] { Black, Green, Red, Yellow }.Select((seat, index) => (
                        this._rules.Thrones[seat],
                        new Piece(new PieceId(10 + index), Chaturaji2027Pieces.Raja, seat)
                    )),
                ]
            );
            return Appraisal.ValueOf(new(this._rules, state, Green, []), state, chances);
        }

        // Act
        double gained = ValueWith(Yellow) - ValueWith(Red);

        // Assert - taken, its whole worth; fled, only the turn it costs, a quarter of it
        Assert.Equal(0.75 * this._rules.WorthOf(Chaturaji2027Pieces.Elephant), gained, 6);
    }

    /// <summary>How many decisions, every seat's, the endgame test allows before calling it not converted.</summary>
    private const int MostEndgameDecisions = 120;

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    public void Ending_WhenAFewPointsBehindInTheOpeningWithEveryPieceLeft_DoesNotProposeEndingIt(int behind)
    {
        // Arrange - a guard, after the user's games of 2026-11-27, in which Argus proposed its defeat within its first
        // turns: every army whole, its pieces boxed in, Yellow a few points ahead
        GameState initial = this._rules.CreateInitialState();
        GameState state = initial with
        {
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Black,
            TurnNumber = 6,
            Dice = [3, 5],
            Scores = initial.Scores.SetItem(Yellow, behind),
        };
        DecisionRequest request = new(this._rules, state, Black, this._rules.LegalActions(state));

        // Act
        GameAction? action = Ending.AnswerTo(request);

        // Assert
        Assert.Null(action);
    }

    /// <summary>
    /// The user's game 7c2b73f15a3946d59c13c61c7ed8d0bf of 2026-11-27, up to Sage's (Green's) second move on a 4 and a
    /// 6, each action as its kind, its actor, then its values or its cells from and to.
    /// </summary>
    private static readonly string[] SagesRook =
    [
        "dice 0 1", "dice 1 2", "dice 2 4", "dice 3 5", "move 3 31 38", "roll 0", "dice 0 3,6", "move 0 3 12",
        "move 0 2 6", "pass 0", "roll 1", "dice 1 4,6", "move 1 32 25", "move 1 40 8", "pass 1", "roll 2",
        "dice 2 1,5", "move 2 55 47", "move 2 63 45", "roll 3", "dice 3 5,1", "move 3 7 21", "move 3 38 45", "pass 3",
        "move 0 10 18", "pass 0", "roll 1", "dice 1 3,6", "move 1 8 0", "move 1 25 26", "pass 1", "move 2 62 45",
        "pass 2", "roll 3", "dice 3 4,3", "move 3 23 47", "move 3 47 45", "pass 3", "move 0 1 16", "pass 0", "roll 1",
        "dice 1 3,3", "move 1 0 6", "move 1 45 37", "pass 1", "roll 2", "dice 2 1,6", "move 2 60 51", "pass 2",
        "move 3 37 53", "pass 3", "move 0 16 26", "pass 0", "roll 1", "dice 1 6,6", "move 1 41 42", "move 1 42 51",
        "pass 1", "move 2 61 53", "pass 2", "roll 3", "dice 3 1,2", "move 3 21 35", "move 3 15 21", "pass 3", "roll 0",
        "dice 0 2,3", "move 0 26 20", "move 0 53 61", "pass 0", "roll 1", "dice 1 2,6", "pass 1", "roll 2",
        "dice 2 4,6", "move 2 54 46",
    ];

    [Theory]
    [InlineData(SageBot.BotId, 1)]
    [InlineData(SageBot.BotId, 2)]
    [InlineData(SageBot.BotId, 3)]
    [InlineData(TacticianBot.BotId, 1)]
    [InlineData(AthenaBot.BotId, 1)]
    public async Task Strong_WhenItsRookCouldTakeAHorseTwoPawnsGuard_LeavesIt(string bot, int seed)
    {
        // Arrange - the user's game: Green's Rook on 61 could take Red's Horse on 21, which Red's Pawns guard
        DecisionRequest request = this.GreenDeciding(SagesRook);

        // Act
        GameAction action = await Strong(bot, seed).Decide(request, this._cancellation);

        // Assert
        Assert.NotEqual(new MoveAction(Green, new CellId(61), new CellId(21)), action);
    }

    /// <summary>The bot of <paramref name="id"/>, one of those strong enough to be a partner, by its seed.</summary>
    private static IBotPlaying Strong(string id, int seed) =>
        id switch
        {
            TacticianBot.BotId => new TacticianBot(new Random(seed)),
            AthenaBot.BotId => new AthenaBot(new Random(seed)),
            _ => new SageBot(new Random(seed)),
        };

    /// <summary>Green to decide, once the game has gone as <paramref name="actions"/> say.</summary>
    private DecisionRequest GreenDeciding(string[] actions)
    {
        GameState state = actions.Aggregate(
            this._rules.CreateInitialState(),
            (sofar, action) => this._rules.Apply(sofar, this._rules.Parse(Described(action))).State
        );
        return new DecisionRequest(this._rules, state, Green, this._rules.LegalActions(state));
    }

    /// <summary>
    /// The user's game dc3893b4ebbd4acfa9d2554461202d67 of 2026-11-27, up to Sage's (Green's) turn on a 2 and a 3; an
    /// escape as its kind, its actor, its home and its piece.
    /// </summary>
    private static readonly string[] SagesGuard =
    [
        "dice 0 5", "dice 1 3", "dice 2 2", "dice 3 1", "move 0 8 16", "roll 1", "dice 1 1,5", "move 1 49 50",
        "move 1 56 42", "move 2 60 51", "roll 3", "dice 3 4,4", "roll 0", "dice 0 3,2", "move 0 1 18", "move 0 2 1",
        "roll 1", "dice 1 1,4", "move 1 42 28", "roll 2", "dice 2 1,2", "move 2 62 47", "move 2 63 45", "roll 3",
        "dice 3 6,2", "move 3 31 38", "move 3 15 21", "move 0 18 28", "pass 0", "roll 1", "dice 1 3,3", "move 2 61 57",
        "pass 2", "roll 3", "dice 3 5,1", "move 3 38 47", "pass 3", "roll 0", "dice 0 5,6", "move 0 10 18",
        "move 0 3 10", "pass 0", "roll 1", "dice 1 6,6", "move 1 41 42", "move 1 42 51", "pass 1", "move 2 54 47",
        "pass 2", "roll 3", "dice 3 5,4", "move 3 14 13", "pass 3", "roll 0", "dice 0 4,3", "move 0 57 41",
        "move 0 41 40", "pass 0", "move 1 32 40", "pass 1", "roll 2", "dice 2 4,4", "escape 2 61 18", "pass 2",
        "move 3 21 4", "pass 3", "move 0 10 17", "pass 0", "roll 1", "dice 1 3,5", "move 1 23 47", "move 1 22 21",
        "pass 1", "roll 2", "dice 2 2,3",
    ];

    [Theory]
    [InlineData(SageBot.BotId, 1)]
    [InlineData(SageBot.BotId, 2)]
    [InlineData(SageBot.BotId, 3)]
    [InlineData(TacticianBot.BotId, 1)]
    [InlineData(AthenaBot.BotId, 1)]
    public async Task Strong_WhenItsRookGuardsAPawnOfALine_DoesNotLeaveItOnlyToThreatenABlockedPawn(
        string bot,
        int seed
    )
    {
        // Arrange - the user's game: Green's Rook on 61 guards its Pawn on 53, while Red's Rook on 47 attacks its
        // Pawn on 55, unguarded, and its Boat on 45; Sage moved the Rook to 59, beside Yellow's Pawn on 50, to
        // threaten a Pawn that could not move, and Red, on two Rook dice, took the Boat and the Pawn on 53
        DecisionRequest request = this.GreenDeciding(SagesGuard);

        // Act
        GameAction action = await Strong(bot, seed).Decide(request, this._cancellation);

        // Assert
        Assert.NotEqual(new MoveAction(Green, new CellId(61), new CellId(59)), action);
    }

    private static ActionDescriptor Described(string action)
    {
        string[] words = action.Split(' ');
        ImmutableSortedDictionary<string, string> arguments = ImmutableSortedDictionary<string, string>.Empty
            .Add(ActionDescriptor.ActorArgument, words[1]);
        return new ActionDescriptor(
            words[0],
            (words[0], words.Length) switch
            {
                (_, 3) => arguments.Add("values", words[2]),
                ("escape", 4) => arguments.Add("home", words[2]).Add("piece", words[3]),
                (_, 4) => arguments.Add("from", words[2]).Add("to", words[3]),
                _ => arguments,
            }
        );
    }

    private static CellId CellAt(int column, int row) => new((row * Side) + column);

    /// <summary>Black to decide, without dice, with only these pieces on the board.</summary>
    private GameState BlackDeciding(params (CellId Cell, Piece Piece)[] pieces) => this.Deciding(Black, pieces);

    /// <summary><paramref name="seat"/> to decide, without dice, with only these pieces on the board.</summary>
    private GameState Deciding(SeatId seat, params (CellId Cell, Piece Piece)[] pieces) =>
        this._rules.WithoutDice(
            this._rules.CreateInitialState() with
            {
                RollOff = [],
                AwaitedDice = null,
                DecidingSeat = seat,
                Position = pieces.Aggregate(Position.Empty, (board, placed) => board.Place(placed.Cell, placed.Piece)),
            }
        );
}
