using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Games.Chaturaji2027.Test;

public sealed class Chaturaji2027RulesTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    /// <summary>Black's throne, the square its Raja starts on.</summary>
    private static readonly CellId BlackThrone = Grid.CellAt(3, 0);

    /// <summary>Yellow's throne, the square its Raja starts on.</summary>
    private static readonly CellId YellowThrone = Grid.CellAt(0, 4);

    /// <summary>Green's throne.</summary>
    private static readonly CellId GreenThrone = Grid.CellAt(4, 7);

    /// <summary>Red's throne.</summary>
    private static readonly CellId RedThrone = Grid.CellAt(7, 3);

    private readonly Chaturaji2027Rules _rules = new();

    [Fact]
    public void Identity_WhenRead_IsChaturaji2027()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name, CellLayout Corner) identity = (
            this._rules.Id,
            this._rules.Name,
            this._rules.Topology.LayoutOf(new CellId(63))
        );

        // Assert
        Assert.Equal(("chaturaji-2027", "Chaturaji 2027", new CellLayout(7, 7, "square")), identity);
    }

    [Fact]
    public void CreateInitialState_WhenCalled_WaitsOnBlacksDieToSeeWhoGoesFirst()
    {
        // Arrange - nothing beyond the rules

        // Act
        GameState state = this._rules.CreateInitialState();

        // Assert
        Assert.Equal(
            (Black, 0, 0, 0, new DiceRequest(1, 6), 4),
            (
                state.DecidingSeat,
                state.TurnNumber,
                state.Scores.Values.Sum(),
                state.Prisoners.Count,
                state.AwaitedDice,
                state.RollOff.Count
            )
        );
    }

    [Fact]
    public void LegalActions_WhileTheGameWaitsOnTheRollOff_IsEmpty()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void Apply_WhenASeatRollsOff_WaitsOnTheNextSeatsDie()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [4]));

        // Assert
        Assert.Equal(
            (Yellow, new DiceRequest(1, 6), "4"),
            (transition.State.DecidingSeat, transition.State.AwaitedDice, string.Join(',', transition.State.Dice))
        );
        Assert.Equal([new RollOffRolled(Black, 4)], transition.Events);
    }

    [Fact]
    public void Apply_WhenOneSeatRollsOffHighest_ItGoesFirstPlayingThatDie()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition transition = this.RollOff(state, 2, 5, 3, 1);

        // Assert
        GameState opened = transition.State;
        Assert.Equal(
            (Yellow, 1, "5", 0, null),
            (
                opened.DecidingSeat,
                opened.TurnNumber,
                string.Join(',', opened.Dice),
                opened.RollOff.Count,
                opened.AwaitedDice
            )
        );
        Assert.Equal(new RollOffWon(Yellow, 5), transition.Events[^1]);
        Assert.Equal(7, this._rules.LegalActions(opened).Count);
    }

    [Fact]
    public void Apply_WhenSeatsTieForHighest_OnlyTheyRollAgain()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition tied = this.RollOff(state, 5, 5, 2, 5);
        Transition settled = this.RollOff(tied.State, 3, 6, 1);

        // Assert
        Assert.Equal(new RollOffTied([Black, Yellow, Red]), tied.Events[^1]);
        Assert.Equal((Black, 3), (tied.State.DecidingSeat, tied.State.RollOff.Count));
        Assert.Equal(new RollOffWon(Yellow, 6), settled.Events[^1]);
    }

    [Fact]
    public void Apply_WhenTheWinningDieCannotBeUsed_PassesTheOpeningTurn()
    {
        // Arrange - the winner rolled an elephant, still hemmed in by its own pawn
        GameState state = this._rules.CreateInitialState();

        // Act
        Transition transition = this.RollOff(state, 2, 1, 4, 3);

        // Assert
        Assert.Equal((Red, 2), (transition.State.DecidingSeat, transition.State.TurnNumber));
        Assert.Equal(
            [new RollOffWon(Green, 4), new DiceForfeited(Green, [4]), new TurnPassed(Green, Red)],
            transition.Events.TakeLast(3)
        );
    }

    [Fact]
    public void Apply_WhenRollingOffWithFourSidedDice_WaitsOnAFourSidedDie()
    {
        // Arrange
        Chaturaji2027Rules rules = new(
            DiceProfile.FourSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.OtherArmies
        );

        // Act
        GameState state = rules.CreateInitialState();

        // Assert
        Assert.Equal(new DiceRequest(1, 4), state.AwaitedDice);
    }

    [Fact]
    public void LegalActions_WhenATurnStarts_OffersAPassARollAndNineMoves()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            (1, 1, 9),
            (
                actions.OfType<PassAction>().Count(),
                actions.OfType<RollAction>().Count(),
                actions.OfType<MoveAction>().Count()
            )
        );
    }

    [Fact]
    public void LegalActions_WhenTheGameWaitsOnDice_IsEmpty()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void LegalActions_WhenDiceAreRolled_OffersAPassAndMovesOfTheirKindsOnly()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [new PassAction(Black), new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(2, 2))],
            actions
        );
    }

    [Fact]
    public void LegalActions_WhenNoSeatIsDeciding_IsEmpty()
    {
        // Arrange
        GameState state = this.Later() with { DecidingSeat = null };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Empty(actions);
    }

    [Fact]
    public void LegalActions_WhenAPawnHasAnEnemyAndAnAllyDiagonallyAhead_CapturesOnlyTheEnemy()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(2, 4), Piece(2, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(4, 4), Piece(3, Chaturaji2027Pieces.Pawn, Green))
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [
                new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)),
                new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(2, 4)),
            ],
            actions.OfType<MoveAction>().OrderBy(move => move.To.Value).Reverse()
        );
    }

    [Fact]
    public void LegalActions_WhenARajaSitsOnAnEnemyThrone_ItsSeatMovesThatArmyTheWayItFacesTakingAnything()
    {
        // Arrange - Red's pawns face left; one of Red's own pieces and one of Yellow's are ahead of it
        GameState state = this.StateWith(
            (RedThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(6, 1), Piece(2, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(5, 2), Piece(3, Chaturaji2027Pieces.Elephant, Red)),
            (Grid.CellAt(5, 0), Piece(4, Chaturaji2027Pieces.Pawn, Yellow))
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [Grid.CellAt(5, 0), Grid.CellAt(5, 1), Grid.CellAt(5, 2)],
            actions
                .OfType<MoveAction>()
                .Where(move => move.From == Grid.CellAt(6, 1))
                .Select(move => move.To)
                .OrderBy(cell => cell.Value)
        );
    }

    [Theory]
    [InlineData(Chaturaji2027PiecesKind.Raja, true)]
    [InlineData(Chaturaji2027PiecesKind.Pawn, false)]
    public void LegalActions_WhenAnAllyHasNoRaja_ASeatWithOneMovesTheAllysArmy(
        Chaturaji2027PiecesKind own,
        bool expected
    )
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, KindOf(own), Black)),
            (Grid.CellAt(4, 6), Piece(2, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(0, 7), Piece(3, Chaturaji2027Pieces.Pawn, Yellow))
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(expected, actions.Contains(new MoveAction(Black, Grid.CellAt(4, 6), Grid.CellAt(4, 5))));
    }

    [Fact]
    public void LegalActions_WhenAnEnemyRajaSitsOnTheSeatsThrone_OffersOnlyARoll()
    {
        // Arrange
        GameState state = this.SharedTurn();

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state with { Dice = [] });

        // Assert
        Assert.Equal([new RollAction(Yellow)], actions);
    }

    [Fact]
    public void Apply_WhenTheActionIsNotLegal_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this.Later();
        PassAction outOfTurn = new(Yellow);

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, outOfTurn)
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenPassing_HandsTheTurnClockwiseAndCountsTheIdlePass()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Yellow, 3, 1),
            (transition.State.DecidingSeat, transition.State.TurnNumber, transition.State.IdlePasses)
        );
        Assert.Equal([new TurnPassed(Black, Yellow)], transition.Events);
    }

    [Fact]
    public void Apply_WhenTheLastSeatPasses_HandsTheTurnBackToTheFirst()
    {
        // Arrange
        GameState state = this.Later() with { DecidingSeat = Red };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Red));

        // Assert
        Assert.Equal(Black, transition.State.DecidingSeat);
    }

    [Fact]
    public void Apply_WhenTheNextSeatHasNoPiecesLeft_SkipsIt()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(4, 6), Piece(2, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(6, 1), Piece(3, Chaturaji2027Pieces.Pawn, Red))
        );

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal([new TurnPassed(Black, Green)], transition.Events);
    }

    [Fact]
    public void Apply_WhenMovingToAnEmptySquare_MovesThePieceAndScoresNothing()
    {
        // Arrange
        GameState state = this.Later() with { IdlePasses = 5 };
        MoveAction pawnForward = new(Black, Grid.CellAt(0, 1), Grid.CellAt(0, 2));
        Piece pawn = state.Position.PieceAt(pawnForward.From)!;

        // Act
        Transition transition = this._rules.Apply(state, pawnForward);

        // Assert
        Assert.Equal(
            [new PieceMoved(pawn, pawnForward.From, pawnForward.To, Black), new TurnPassed(Black, Yellow)],
            transition.Events
        );
        Assert.Equal(0, transition.State.IdlePasses);
    }

    [Fact]
    public void Apply_WhenCapturing_ScoresThePieceAndTakesItPrisoner()
    {
        // Arrange
        Piece elephant = Piece(1, Chaturaji2027Pieces.Elephant, Black);
        Piece horse = Piece(2, Chaturaji2027Pieces.Horse, Yellow);
        GameState state = this.StateWith(
            (Grid.CellAt(0, 0), elephant),
            (Grid.CellAt(0, 5), horse),
            (Grid.CellAt(6, 1), Piece(3, Chaturaji2027Pieces.Pawn, Red))
        );
        MoveAction capture = new(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5));

        // Act
        Transition transition = this._rules.Apply(state, capture);

        // Assert
        Assert.Equal((3, new PieceCaptured(horse, Black, 3)), (transition.State.Scores[Black], transition.Events[1]));
        Assert.Equal([new Prisoner(horse, Black)], transition.State.Prisoners);
    }

    [Fact]
    public void Apply_WhenARajaEntersAnEnemyThrone_ScoresItsOwnerTheBonus()
    {
        // Arrange
        Piece raja = Piece(1, Chaturaji2027Pieces.Raja, Black);
        GameState state = this.StateWith(
            (Grid.CellAt(1, 3), raja),
            (Grid.CellAt(6, 1), Piece(2, Chaturaji2027Pieces.Pawn, Red))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(1, 3), YellowThrone));

        // Assert
        Assert.Equal((1, new ThroneEntered(raja, Yellow, 1)), (transition.State.Scores[Black], transition.Events[1]));
    }

    [Theory]
    [InlineData(Chaturaji2027PiecesKind.Raja, 4, 6)]
    [InlineData(Chaturaji2027PiecesKind.Pawn, 0, 3)]
    public void Apply_WhenAnythingButARajaEntersAThroneOrARajaEntersAnAllysThrone_ScoresNoBonus(
        Chaturaji2027PiecesKind kind,
        int fromColumn,
        int fromRow
    )
    {
        // Arrange - Green's throne is at e8; Yellow's at a5
        GameState state = this.StateWith(
            (Grid.CellAt(fromColumn, fromRow), Piece(1, KindOf(kind), Black)),
            (Grid.CellAt(6, 1), Piece(2, Chaturaji2027Pieces.Pawn, Red))
        );
        CellId target = kind == Chaturaji2027PiecesKind.Raja ? Grid.CellAt(4, 7) : YellowThrone;
        MoveAction move = new(Black, Grid.CellAt(fromColumn, fromRow), target);

        // Act
        Transition transition = this._rules.Apply(state, move);

        // Assert
        Assert.Equal(0, transition.State.Scores[Black]);
    }

    [Fact]
    public void Apply_WhenTakingARajaOnItsOwnThrone_ScoresTheBonusOnTop()
    {
        // Arrange
        Piece raja = Piece(2, Chaturaji2027Pieces.Raja, Red);
        GameState state = this.StateWith(
            (Grid.CellAt(7, 0), Piece(1, Chaturaji2027Pieces.Elephant, Black)),
            (RedThrone, raja),
            (Grid.CellAt(0, 7), Piece(3, Chaturaji2027Pieces.Pawn, Yellow))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(7, 0), RedThrone));

        // Assert
        Assert.Equal(
            (7, new RajaCapturedOnThrone(raja, Black, 2)),
            (transition.State.Scores[Black], transition.Events[2])
        );
    }

    [Fact]
    public void Apply_WhenTheLastEnemyPieceIsTaken_EndsTheGameWithThatTeamWinning()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(0, 0), Piece(1, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(0, 5), Piece(2, Chaturaji2027Pieces.Horse, Yellow))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        GameOutcome won = new([Black, Green], "only one team has pieces left");
        Assert.Equal((null, won), (transition.State.DecidingSeat, transition.State.Outcome));
        Assert.Equal(new GameEnded(won), transition.Events[^1]);
        Assert.Empty(this._rules.LegalActions(transition.State));
    }

    [Theory]
    [InlineData(5, 8, 3)]
    [InlineData(4, 12, 5)]
    public void Apply_WhenTheLastEnemyRajaIsTakenByOneCommandingAnAlly_CreatesTheEmpire(
        int row,
        int score,
        int empire
    )
    {
        // Arrange - Green has no Raja, so Black commands it; Red's Raja is gone; Yellow's is on a6, or a5, its throne
        GameState state = this.Empire(Grid.CellAt(3, 3), hasGreenRaja: false, yellowRajaRow: row);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, row)));

        // Assert
        Assert.Equal([new EmpireCreated(Black, empire)], transition.Events.OfType<EmpireCreated>());
        Assert.Equal(score, transition.State.Scores[Black]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_WhenTheTurnComesToOneCommandingEveryPiece_ScoresTheBonusOnce(bool hasRetiredRed)
    {
        // Arrange - Green has no Raja, so Black commands it; Red has nothing, or only its Raja, retired
        GameState state = this.EveryPiece(hasRetiredRed);

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Green));

        // Assert
        Assert.Equal([new EveryPieceCommanded(Black, 2)], transition.Events.OfType<EveryPieceCommanded>());
        Assert.Equal((2, true), (transition.State.Scores[Black], transition.State.IsEveryPieceScored));
    }

    [Fact]
    public void Apply_WhenARajaTakesTheThroneThatLeavesItCommandingEveryPieceWithDiceLeft_ScoresTheBonus()
    {
        // Arrange - Black's Raja beside Yellow's throne, with a 5 to move it and a 6 still to use
        GameState every = this.EveryPiece(hasRetiredRed: false);
        GameState state = every with
        {
            DecidingSeat = Black,
            Position = every.Position.Relocate(YellowThrone, Grid.CellAt(1, 4)),
            Dice = [5, 6],
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(1, 4), YellowThrone));

        // Assert
        Assert.Equal([new EveryPieceCommanded(Black, 2)], transition.Events.OfType<EveryPieceCommanded>());
        Assert.Equal((3, Black), (transition.State.Scores[Black], transition.State.DecidingSeat));
    }

    [Fact]
    public void Apply_WhenEveryPieceIsCommandedAgainBeforeEverySeatIsBackOnTheBoard_ScoresNothingMore()
    {
        // Arrange - the bonus already scored, and not every seat on the board since
        GameState state = this.EveryPiece(hasRetiredRed: false) with { IsEveryPieceScored = true };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Green));

        // Assert
        Assert.Empty(transition.Events.OfType<EveryPieceCommanded>());
        Assert.Equal((0, true), (transition.State.Scores[Black], transition.State.IsEveryPieceScored));
    }

    [Fact]
    public void Apply_WhenEverySeatIsOnTheBoard_LetsTheBonusForCommandingEveryPieceBeScoredAgain()
    {
        // Arrange - the bonus scored once, and every seat back on the board
        GameState state = this.Later() with { IsEveryPieceScored = true };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.False(transition.State.IsEveryPieceScored);
    }

    [Fact]
    public void Apply_WhenARajaStepsBackOntoAThroneItLeftIdly_FarmsTheEntrysPoint()
    {
        // Arrange - Black's Raja stepped off Yellow's throne to b6, idly, and has done nothing since
        GameState state = this.Farm(Grid.CellAt(1, 5)) with { SteppedOff = [Black] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(1, 5), YellowThrone));

        // Assert
        Assert.Equal(
            (1, 1, false),
            (
                transition.State.Scores[Black],
                transition.State.Farmed.GetValueOrDefault(Black),
                transition.State.SteppedOff.Contains(Black)
            )
        );
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Apply_WhenARajaStepsOffAnEnemyThrone_IsIdleOnlyIfItAttacksAndGuardsNothingThere(
        bool isGuarding,
        bool isIdle
    )
    {
        // Arrange - Black's Raja on Yellow's throne, with Black's Pawn on c4, beside b5, if guarding
        GameState farm = this.Farm(YellowThrone) with { Farmed = ImmutableDictionary<SeatId, int>.Empty.Add(Black, 2) };
        GameState state = isGuarding
            ? farm with { Position = farm.Position.Place(Grid.CellAt(2, 3), Piece(4, Chaturaji2027Pieces.Pawn, Black)) }
            : farm;

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, YellowThrone, Grid.CellAt(1, 4)));

        // Assert
        Assert.Equal(
            (isIdle, isIdle ? 2 : 0),
            (transition.State.SteppedOff.Contains(Black), transition.State.Farmed.GetValueOrDefault(Black))
        );
    }

    [Fact]
    public void Apply_WhenAFarmerMovesAnythingElse_StopsFarmingAndKeepsWhatItFarmed()
    {
        // Arrange - Black farmed 3 points and stepped off; now it moves its Pawn
        GameState farm = this.Farm(Grid.CellAt(1, 5));
        GameState state = farm with
        {
            Position = farm.Position.Place(Grid.CellAt(6, 1), Piece(4, Chaturaji2027Pieces.Pawn, Black)),
            Scores = farm.Scores.SetItem(Black, 3),
            Farmed = ImmutableDictionary<SeatId, int>.Empty.Add(Black, 3),
            SteppedOff = [Black],
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(6, 1), Grid.CellAt(6, 2)));

        // Assert
        Assert.Equal(
            (3, 0, false),
            (
                transition.State.Scores[Black],
                transition.State.Farmed.Count,
                transition.State.SteppedOff.Contains(Black)
            )
        );
    }

    [Fact]
    public void Apply_WhenAFarmersRajaIsTaken_ItLosesWhatItFarmedAndItsCaptorScoresTwiceThat()
    {
        // Arrange - Black farmed 2 of its 3 points; Yellow's Elephant on c8 takes its Raja on c3
        GameState farm = this.Farm(Grid.CellAt(2, 2));
        GameState state = farm with
        {
            Position = farm.Position.Place(Grid.CellAt(2, 7), Piece(10, Chaturaji2027Pieces.Elephant, Yellow)),
            DecidingSeat = Yellow,
            Scores = farm.Scores.SetItem(Black, 3),
            Farmed = ImmutableDictionary<SeatId, int>.Empty.Add(Black, 2),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Yellow, Grid.CellAt(2, 7), Grid.CellAt(2, 2)));

        // Assert
        Assert.Equal(
            [new FarmedPointsLost(Black, Yellow, 2, 4)],
            transition.Events.OfType<FarmedPointsLost>()
        );
        Assert.Equal(
            (1, 9, 0),
            (transition.State.Scores[Black], transition.State.Scores[Yellow], transition.State.Farmed.Count)
        );
    }

    [Fact]
    public void Apply_WhenAFarmersRajaIsTakenByItsOwnTeam_ItLosesWhatItFarmedAndNoOneScores()
    {
        // Arrange - Green commands Red's Pawn, which takes Black's farming Raja on e7; Yellow's Raja holds Black's
        // throne
        GameState state = this.StateWith(
            (RedThrone, Piece(19, Chaturaji2027Pieces.Raja, Green)),
            (Grid.CellAt(5, 5), Piece(2, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(4, 6), Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (BlackThrone, Piece(11, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            DecidingSeat = Green,
            Scores = this.Later().Scores.SetItem(Black, 2),
            Farmed = ImmutableDictionary<SeatId, int>.Empty.Add(Black, 2),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Green, Grid.CellAt(5, 5), Grid.CellAt(4, 6)));

        // Assert
        Assert.Equal([new FarmedPointsLost(Black, Green, 2, 0)], transition.Events.OfType<FarmedPointsLost>());
        Assert.Equal((0, 0), (transition.State.Scores[Black], transition.State.Scores[Green]));
    }

    [Fact]
    public void Apply_WhenAFarmerLetsAPieceEscape_StopsFarming()
    {
        // Arrange - Black farmed a point
        GameState state = this.Escapes() with
        {
            Dice = [5, 5],
            Farmed = ImmutableDictionary<SeatId, int>.Empty.Add(Black, 1),
            SteppedOff = [Black],
        };

        // Act
        Transition transition = this._rules.Apply(state, new EscapeAction(Black, new PieceId(3), BlackThrone));

        // Assert
        Assert.Equal((0, 0), (transition.State.Farmed.Count, transition.State.SteppedOff.Count));
    }

    [Fact]
    public void Apply_WhenTheLastEnemyRajaIsTakenWithBlacksRajaOnGreensThrone_CreatesTheEmpire()
    {
        // Arrange - Green has its Raja, but Black's sits on Green's throne
        GameState state = this.Empire(GreenThrone, hasGreenRaja: true);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.Equal([new EmpireCreated(Black, 3)], transition.Events.OfType<EmpireCreated>());
    }

    [Fact]
    public void Apply_WhenTheLastEnemyRajaComesBackAndIsTakenAgain_CreatesTheEmpireAgainAndKeepsTheFirst()
    {
        // Arrange - the empire created once; then Yellow's Raja is back on a7, as by a ransom, on Black's next turn
        GameState state = this.Empire(GreenThrone, hasGreenRaja: true);
        GameState first = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5))).State;
        GameState returned = state with
        {
            Position = first.Position.Place(Grid.CellAt(0, 6), Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
            Scores = first.Scores,
        };

        // Act
        Transition transition = this._rules.Apply(
            returned,
            new MoveAction(Black, Grid.CellAt(0, 5), Grid.CellAt(0, 6))
        );

        // Assert - each time 5 for the Raja and 3 for the empire
        Assert.Equal([new EmpireCreated(Black, 3)], transition.Events.OfType<EmpireCreated>());
        Assert.Equal(16, transition.State.Scores[Black]);
    }

    [Fact]
    public void Apply_WhenTheLastEnemyRajaIsTakenByOneCommandingNoAlly_CreatesNoEmpire()
    {
        // Arrange - Black's Raja holds Red's throne, an enemy's; Green has its Raja
        GameState state = this.Empire(RedThrone, hasGreenRaja: true);

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.Equal((5, false), (transition.State.Scores[Black], transition.Events.OfType<EmpireCreated>().Any()));
    }

    [Fact]
    public void Apply_WhenAnotherEnemyRajaIsLeft_CreatesNoEmpire()
    {
        // Arrange - Red's Raja is still on the board
        GameState empire = this.Empire(Grid.CellAt(3, 3), hasGreenRaja: false);
        GameState state = empire with
        {
            Position = empire.Position.Place(Grid.CellAt(7, 7), Piece(27, Chaturaji2027Pieces.Raja, Red)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.DoesNotContain(transition.Events, happened => happened is EmpireCreated);
    }

    [Fact]
    public void Apply_WhenTheCaptorCommandsNoAllyButItsAllyDoes_CreatesTheEmpireForTheAllyOnly()
    {
        // Arrange - Green, with no Raja, takes with its Elephant; Black, with its Raja, commands Green
        GameState empire = this.Empire(Grid.CellAt(3, 3), hasGreenRaja: false);
        GameState state = empire with
        {
            DecidingSeat = Green,
            Position = empire.Position.Place(Grid.CellAt(0, 0), Piece(18, Chaturaji2027Pieces.Elephant, Green)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Green, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.Equal([new EmpireCreated(Black, 1)], transition.Events.OfType<EmpireCreated>());
        Assert.Equal((1, 5), (transition.State.Scores[Black], transition.State.Scores[Green]));
    }

    [Fact]
    public void Apply_WhenOnlyRajasAreLeft_EndsTheGameInADraw()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(4, 4), Piece(2, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(7, 0), Piece(3, Chaturaji2027Pieces.Raja, Red))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(4, 4)));

        // Assert
        Assert.Equal(
            (true, "only Rajas are left, an honourable peace"),
            (transition.State.Outcome?.IsDraw, transition.State.Outcome?.Reason)
        );
    }

    [Theory]
    [InlineData(3, "0,2")]
    [InlineData(0, "")]
    public void Apply_WhenNoOneHasRolledOrMovedForThreeRounds_EndsTheGameOnPoints(int blacksPoints, string winners)
    {
        // Arrange
        GameState state = this.Later() with
        {
            IdlePasses = 11,
            Scores = this.Later().Scores.SetItem(Black, blacksPoints),
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        GameOutcome? outcome = transition.State.Outcome;
        Assert.Equal(
            (winners, "no one rolled or moved for 3 rounds, so points decide"),
            (string.Join(',', outcome!.Winners.Select(seat => seat.Value)), outcome.Reason)
        );
    }

    [Fact]
    public void Apply_WhenRolling_WaitsOnTwoDiceWithoutPassingTheTurn()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        Transition transition = this._rules.Apply(state, new RollAction(Black));

        // Assert
        Assert.Equal(
            (new DiceRequest(2, 6), Black, 0),
            (transition.State.AwaitedDice, transition.State.DecidingSeat, transition.Events.Count)
        );
    }

    [Fact]
    public void Apply_WhenTheDiceComeUpUsable_KeepsThemForTheRoller()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [5, 1]));

        // Assert
        Assert.Equal(
            ("5,1", Black, null),
            (string.Join(',', transition.State.Dice), transition.State.DecidingSeat, transition.State.AwaitedDice)
        );
        Assert.Equal([new DiceRolled(Black, [5, 1])], transition.Events);
    }

    [Fact]
    public void Apply_WhenNoPieceCanUseTheDice_ForfeitsThemAndPassesTheTurn()
    {
        // Arrange - both dice show an elephant, still hemmed in by its own pawn
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [3, 4]));

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.Dice.Count));
        Assert.Equal(
            [new DiceRolled(Black, [3, 4]), new DiceForfeited(Black, [3, 4]), new TurnPassed(Black, Yellow)],
            transition.Events
        );
    }

    [Theory]
    [MemberData(nameof(UnawaitedDice), DisableDiscoveryEnumeration = true)]
    public void Apply_WhenTheDiceAreNotTheOnesAwaited_ThrowsIllegalActionException(
        DiceRequest? awaited,
        DiceAction dice
    )
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = awaited };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, dice)
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenTheGameIsOver_RefusesDice()
    {
        // Arrange
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6), DecidingSeat = null };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new DiceAction(Black, [5, 1]))
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    public static TheoryData<DiceRequest?, DiceAction> UnawaitedDice =>
        new()
        {
            { null, new DiceAction(Black, [5, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Yellow, [5, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [5]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [0, 1]) },
            { new DiceRequest(2, 6), new DiceAction(Black, [5, 7]) },
        };

    [Fact]
    public void Apply_WhenMovingWithOneOfTwoDice_UsesItUpAndKeepsTheTurn()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 1] };
        MoveAction pawnForward = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));

        // Act
        Transition transition = this._rules.Apply(state, pawnForward);

        // Assert
        Assert.Equal(("1", Black), (string.Join(',', transition.State.Dice), transition.State.DecidingSeat));
    }

    [Fact]
    public void Apply_WhenTheLastDieIsUsed_PassesTheTurn()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };
        MoveAction boatLeap = new(Black, Grid.CellAt(0, 0), Grid.CellAt(2, 2));

        // Act
        Transition transition = this._rules.Apply(state, boatLeap);

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.Dice.Count));
    }

    [Fact]
    public void Apply_WhenAPieceNoDieShowsIsMoved_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [1] };
        MoveAction pawnForward = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, pawnForward)
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenDoublesAreRolled_LetsTheSamePieceMoveTwice()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 5] };
        MoveAction first = new(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4));
        GameState once = this._rules.Apply(state, first).State;
        MoveAction second = new(Black, Grid.CellAt(3, 4), Grid.CellAt(3, 5));

        // Act
        Transition twice = this._rules.Apply(once, second);

        // Assert
        Assert.Equal(
            (Chaturaji2027Pieces.Pawn, Yellow),
            (twice.State.Position.PieceAt(Grid.CellAt(3, 5))?.Kind, twice.State.DecidingSeat)
        );
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    public void MayUseDie_WhenAPieceItLetsMoveMayOrNot_SaysSo(int die, bool expected)
    {
        // Arrange - Black has a Pawn and a Boat, but no Horse or Elephant
        GameState state = this.PawnAndBoat() with { Dice = [die] };

        // Act
        bool mayUse = this._rules.MayUseDie(state, die);

        // Assert
        Assert.Equal(expected, mayUse);
    }

    [Fact]
    public void MayUseDie_WhenNoOneDecides_IsFalse()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5], DecidingSeat = null };

        // Act
        bool mayUse = this._rules.MayUseDie(state, 5);

        // Assert
        Assert.False(mayUse);
    }

    [Fact]
    public void Apply_WhenPassingWithDiceLeft_ForfeitsThemWithoutCountingAnIdlePass()
    {
        // Arrange
        GameState state = this.PawnAndBoat() with { Dice = [5, 1], IdlePasses = 5 };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Yellow, 0, 0),
            (transition.State.DecidingSeat, transition.State.Dice.Count, transition.State.IdlePasses)
        );
    }

    [Fact]
    public void Apply_WhenTheSeatWithAnEnemyOnItsThroneHasMoved_HandsTheOtherDieToTheOccupier()
    {
        // Arrange
        GameState state = this.SharedTurn();

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Yellow, Grid.CellAt(3, 3), Grid.CellAt(4, 3)));

        // Assert
        Assert.Equal(
            (Black, Yellow, "2"),
            (transition.State.DecidingSeat, transition.State.ActingFor, string.Join(',', transition.State.Dice))
        );
        Assert.Equal(new TurnShared(Yellow, Black), transition.Events[^1]);
        Assert.All(
            this._rules.LegalActions(transition.State).OfType<MoveAction>(),
            move => Assert.Equal((Black, Grid.CellAt(1, 1)), (move.Actor, move.From))
        );
    }

    [Fact]
    public void Apply_WhenTheOccupierHasMovedForTheSeat_PassesThatSeatsTurn()
    {
        // Arrange
        GameState shared = this._rules.Apply(
            this.SharedTurn(),
            new MoveAction(Yellow, Grid.CellAt(3, 3), Grid.CellAt(4, 3))
        ).State;

        // Act
        Transition transition = this._rules.Apply(shared, new MoveAction(Black, Grid.CellAt(1, 1), Grid.CellAt(2, 3)));

        // Assert
        Assert.Equal(
            (Green, null, new TurnPassed(Yellow, Green)),
            (transition.State.DecidingSeat, transition.State.ActingFor, transition.Events[^1])
        );
    }

    [Fact]
    public void Apply_WhenTheOccupierPassesForTheSeat_LeavesTheSeatTheDiceItDidNotUse()
    {
        // Arrange - Yellow moved its Pawn with the 5; Black, on Yellow's throne, has the 2
        GameState shared = this._rules.Apply(
            this.SharedTurn(),
            new MoveAction(Yellow, Grid.CellAt(3, 3), Grid.CellAt(4, 3))
        ).State;

        // Act
        Transition transition = this._rules.Apply(shared, new PassAction(Black));

        // Assert - Yellow may move its Horse with the 2
        Assert.Equal([new TurnHandedBack(Black, Yellow)], transition.Events);
        Assert.Equal(
            (Yellow, null, "2"),
            (transition.State.DecidingSeat, transition.State.ActingFor, string.Join(',', transition.State.Dice))
        );
        Assert.Contains(
            new MoveAction(Yellow, Grid.CellAt(1, 1), Grid.CellAt(2, 3)),
            this._rules.LegalActions(transition.State)
        );
    }

    [Fact]
    public void Apply_WhenTheSeatPassesOnWhatItsOccupierLeftIt_PassesItsTurn()
    {
        // Arrange - Yellow passed, Black passed, leaving Yellow both dice
        GameState shared = this._rules.Apply(this.SharedTurn(), new PassAction(Yellow)).State;
        GameState back = this._rules.Apply(shared, new PassAction(Black)).State;

        // Act
        Transition transition = this._rules.Apply(back, new PassAction(Yellow));

        // Assert
        Assert.Equal([new TurnPassed(Yellow, Green)], transition.Events);
        Assert.False(transition.State.IsHandedBack);
    }

    [Fact]
    public void Apply_WhenTheSeatWithAnEnemyOnItsThronePasses_HandsBothDiceToTheOccupier()
    {
        // Arrange
        GameState state = this.SharedTurn();

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Yellow));

        // Assert
        Assert.Equal(
            (Black, Yellow, "5,2"),
            (transition.State.DecidingSeat, transition.State.ActingFor, string.Join(',', transition.State.Dice))
        );
    }

    [Fact]
    public void Apply_WhenTheOccupierCannotUseTheDieLeft_PassesTheTurn()
    {
        // Arrange - the die left shows a boat, and Yellow has none
        GameState state = this.SharedTurn() with { Dice = [5, 1] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Yellow, Grid.CellAt(3, 3), Grid.CellAt(4, 3)));

        // Assert
        Assert.Equal(
            [new TurnShared(Yellow, Black), new DiceForfeited(Black, [1]), new TurnPassed(Yellow, Green)],
            transition.Events.TakeLast(3)
        );
    }

    [Fact]
    public void Apply_WhenTheSeatTakesTheEnemyOnItsThrone_KeepsTheRestOfItsTurn()
    {
        // Arrange - Yellow's horse can leap onto its own throne
        GameState state = this.SharedTurn() with
        {
            Position = this.SharedTurn().Position.Relocate(Grid.CellAt(1, 1), Grid.CellAt(1, 2)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Yellow, Grid.CellAt(1, 2), YellowThrone));

        // Assert
        Assert.Equal(
            (Yellow, null, "5"),
            (transition.State.DecidingSeat, transition.State.ActingFor, string.Join(',', transition.State.Dice))
        );
    }

    [Fact]
    public void Apply_WhenTheSeatWithAnEnemyOnItsThroneCannotUseItsDice_HandsThemToTheOccupier()
    {
        // Arrange - Yellow's pawn is blocked by its allies, which Black, moving it, may take
        GameState state = this.StateWith(
            (YellowThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(4, 3), Piece(3, Chaturaji2027Pieces.Elephant, Red)),
            (Grid.CellAt(4, 4), Piece(4, Chaturaji2027Pieces.Pawn, Red))
        ) with
        {
            DecidingSeat = Yellow,
            AwaitedDice = new DiceRequest(2, 6),
        };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Yellow, [5, 5]));

        // Assert
        Assert.Equal(
            (Black, Yellow, new TurnShared(Yellow, Black)),
            (transition.State.DecidingSeat, transition.State.ActingFor, transition.Events[^1])
        );
    }

    [Theory]
    [InlineData(false, new[] { 13, 21 })]
    [InlineData(true, new[] { 5, 13, 21 })]
    public void LegalActions_WhenACommandedArmyMeetsItsCommandersTeam_TakesItOnlyWhileAnEnemyHoldsItsThrone(
        bool isHeld,
        int[] targets
    )
    {
        // Arrange - Black commands Red; Red's pawn faces left, with Red's and Green's pieces ahead of it, and
        // Yellow's Raja on Green's throne, if held
        GameState commanding = this.StateWith(
            (RedThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(6, 1), Piece(2, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(5, 2), Piece(3, Chaturaji2027Pieces.Elephant, Red)),
            (Grid.CellAt(5, 0), Piece(4, Chaturaji2027Pieces.Pawn, Green))
        );
        GameState state = isHeld
            ? commanding with
            {
                Position = commanding.Position.Place(GreenThrone, Piece(5, Chaturaji2027Pieces.Raja, Yellow)),
            }
            : commanding;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            targets.Select(cell => new CellId(cell)),
            actions
                .OfType<MoveAction>()
                .Where(move => move.From == Grid.CellAt(6, 1))
                .Select(move => move.To)
                .OrderBy(cell => cell.Value)
        );
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void LegalActions_WhenAPieceMeetsAnAllysPiece_TakesItOnlyWhileAnEnemyHoldsTheAllysThrone(
        bool isHeld,
        bool isTaken
    )
    {
        // Arrange - Black's pawn on c3 faces up, Green's Horse on d4 ahead of it; Yellow's Raja on Green's throne,
        // if held
        GameState meeting = this.StateWith(
            (Grid.CellAt(2, 2), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Horse, Green))
        );
        GameState state = isHeld
            ? meeting with
            {
                Position = meeting.Position.Place(GreenThrone, Piece(3, Chaturaji2027Pieces.Raja, Yellow)),
            }
            : meeting;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(isTaken, actions.Contains(new MoveAction(Black, Grid.CellAt(2, 2), Grid.CellAt(3, 3))));
    }

    [Fact]
    public void Apply_WhenACommandedArmyTakesThePiecesOfItsCommandersTeam_ScoresNoOne()
    {
        // Arrange - Black commands Red, whose pawn on f7 can take Green's Horse on e6; Yellow's Raja holds Green's
        // throne, e8
        Piece horse = Piece(3, Chaturaji2027Pieces.Horse, Green);
        GameState state = this.StateWith(
            (RedThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(5, 6), Piece(2, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(4, 5), horse),
            (GreenThrone, Piece(13, Chaturaji2027Pieces.Raja, Yellow))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(5, 6), Grid.CellAt(4, 5)));

        // Assert
        Assert.Equal(
            (0, new PieceCaptured(horse, Black, 0)),
            (transition.State.Scores[Black], transition.Events[1])
        );
        Assert.Equal([new Prisoner(horse, Black)], transition.State.Prisoners);
        Assert.Equal(state.TurnNumber, transition.State.TakenByOwnTeam[horse.Id]);
    }

    [Fact]
    public void LegalActions_WhenAPawnStandsOnTheFarEdge_MayBecomeEachCapturedPieceButAPawnOfItsOwnArmy()
    {
        // Arrange - Black's pawns face up: one is on the top edge, one is not
        GameState state = PromotionPool(this.StateWith(
            (Grid.CellAt(2, 7), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(0, 0), Piece(3, Chaturaji2027Pieces.Raja, Yellow))
        ));

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert - the highest-scoring first, then by owner, then by holder; Red holds two Elephants alike
        Assert.Equal(
            [
                new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(15)),
                new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(11)),
                new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(10)),
                new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(13)),
                new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(16)),
            ],
            actions.OfType<PromoteAction>()
        );
    }

    [Fact]
    public void LegalActions_WhenPawnsMayNeverBePromotedIntoPawns_OffersNoPawn()
    {
        // Arrange
        Chaturaji2027Rules rules = new(
            DiceProfile.WeightedSixSided,
            Chaturaji2027Rules.DefaultIdleRounds,
            PawnPromotion.Never
        );
        GameState state = PromotionPool(this.StateWith(
            (Grid.CellAt(2, 7), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(0, 0), Piece(3, Chaturaji2027Pieces.Raja, Yellow))
        ));

        // Act
        ImmutableList<GameAction> actions = rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [new PieceId(15), new PieceId(11), new PieceId(10), new PieceId(13)],
            actions.OfType<PromoteAction>().Select(promotion => promotion.Into)
        );
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(2, false)]
    public void LegalActions_WhenAnEnemyMovesForTheSeat_PromotesItsPawnsOnlyWithADieThatMovesAPawn(
        int die,
        bool mayPromote
    )
    {
        // Arrange - Black moves for Yellow, whose pawns face right; one is on the right edge
        GameState state = this.SharedTurn() with
        {
            DecidingSeat = Black,
            ActingFor = Yellow,
            Dice = [die],
            Position = this.SharedTurn().Position.Place(Grid.CellAt(7, 2), Piece(5, Chaturaji2027Pieces.Pawn, Yellow)),
            Prisoners =
            [
                new Prisoner(Piece(10, Chaturaji2027Pieces.Horse, Black), Yellow),
                new Prisoner(Piece(11, Chaturaji2027Pieces.Elephant, Yellow), Green),
            ],
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            mayPromote ? [new PieceId(11), new PieceId(10)] : [],
            actions.OfType<PromoteAction>().Select(promotion => promotion.Into)
        );
    }

    [Fact]
    public void LegalActions_WhenAnEnemyRajaSitsOnTheSeatsThroneAtTheStartOfItsTurn_MayRollOrPromote()
    {
        // Arrange - Yellow's Raja on Black's throne
        Piece elephant = Piece(11, Chaturaji2027Pieces.Elephant, Green);
        GameState state = this.StateWith(
            (this._rules.Thrones[Black], Piece(3, Chaturaji2027Pieces.Raja, Yellow)),
            (Grid.CellAt(2, 7), Piece(1, Chaturaji2027Pieces.Pawn, Black))
        ) with
        {
            Prisoners = [new Prisoner(elephant, Red)],
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal([new RollAction(Black), new PromoteAction(Black, Grid.CellAt(2, 7), elephant.Id)], actions);
    }

    [Fact]
    public void Apply_WhenAPawnMovesToTheFarEdge_WaitsOnItsPromotionOrAPassBeforeTheTurnGoesOn()
    {
        // Arrange - a deliberate move, which would otherwise end the turn
        Piece elephant = Piece(11, Chaturaji2027Pieces.Elephant, Green);
        GameState state = this.FarEdgeAhead();

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(2, 6), Grid.CellAt(2, 7)));

        // Assert
        Assert.Equal(
            (Black, Grid.CellAt(2, 7), false),
            (transition.State.DecidingSeat, transition.State.PromotionDue, this._rules.MayOffer(transition.State))
        );
        Assert.Equal(
            [new PassAction(Black), new PromoteAction(Black, Grid.CellAt(2, 7), elephant.Id)],
            this._rules.LegalActions(transition.State)
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_WhenAPromotionDueAfterADeliberateMoveIsDecided_ThePawnsTurnPasses(bool isPromoted)
    {
        // Arrange
        GameState moved = this._rules.Apply(
            this.FarEdgeAhead(),
            new MoveAction(Black, Grid.CellAt(2, 6), Grid.CellAt(2, 7))
        ).State;
        GameAction decision = isPromoted
            ? new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(11))
            : new PassAction(Black);

        // Act
        Transition transition = this._rules.Apply(moved, decision);

        // Assert
        Assert.Equal(
            (Yellow, null, isPromoted ? Chaturaji2027Pieces.Elephant : Chaturaji2027Pieces.Pawn),
            (
                transition.State.DecidingSeat,
                transition.State.PromotionDue,
                transition.State.Position.PieceAt(Grid.CellAt(2, 7))?.Kind
            )
        );
    }

    [Fact]
    public void Apply_WhenAPromotionDueAfterARolledMoveIsPassed_TheTurnGoesOnWithTheDiceLeft()
    {
        // Arrange - Black rolled a Pawn and a Horse
        GameState state = this.FarEdgeAhead() with
        {
            Dice = [5, 2],
            Position = this.FarEdgeAhead()
                .Position.Place(Grid.CellAt(0, 0), Piece(4, Chaturaji2027Pieces.Horse, Black)),
        };
        GameState moved = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(2, 6), Grid.CellAt(2, 7))).State;

        // Act
        Transition transition = this._rules.Apply(moved, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Grid.CellAt(2, 7), "2", Black, null, "2"),
            (
                moved.PromotionDue,
                string.Join(',', moved.Dice),
                transition.State.DecidingSeat,
                transition.State.PromotionDue,
                string.Join(',', transition.State.Dice)
            )
        );
    }

    [Fact]
    public void Apply_WhenOnlyAnEnemysPawnCanUseTheDice_PromotingItUsesADie()
    {
        // Arrange - Black's Raja on Red's throne is boxed in by its Elephants; Red's pawn faces left
        Piece elephant = Piece(11, Chaturaji2027Pieces.Elephant, Green);
        GameState state = this.StateWith(
            (RedThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(6, 2), Piece(2, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(6, 3), Piece(3, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(6, 4), Piece(4, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(7, 2), Piece(5, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(7, 4), Piece(6, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(0, 5), Piece(7, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(0, 0), Piece(8, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            AwaitedDice = new DiceRequest(2, 6),
            Prisoners = [new Prisoner(elephant, Yellow)],
        };
        GameState rolled = this._rules.Apply(state, new DiceAction(Black, [5, 6])).State;

        // Act
        Transition transition = this._rules.Apply(rolled, new PromoteAction(Black, Grid.CellAt(0, 5), elephant.Id));

        // Assert - one die is left, which nothing can use, so the turn passes
        Assert.Equal(
            (Black, "5,6", elephant, Yellow),
            (
                rolled.DecidingSeat,
                string.Join(',', rolled.Dice),
                transition.State.Position.PieceAt(Grid.CellAt(0, 5)),
                transition.State.DecidingSeat
            )
        );
    }

    [Fact]
    public void Apply_WhenAnEnemyMovingForTheSeatPromotes_ItUsesADieAndGoesOn()
    {
        // Arrange - Black moves for Yellow, with a Pawn's die and a Horse's
        GameState state = this.SharedTurn() with
        {
            DecidingSeat = Black,
            ActingFor = Yellow,
            Position = this.SharedTurn().Position.Place(Grid.CellAt(7, 2), Piece(5, Chaturaji2027Pieces.Pawn, Yellow)),
            Prisoners = [new Prisoner(Piece(10, Chaturaji2027Pieces.Horse, Black), Yellow)],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PromoteAction(Black, Grid.CellAt(7, 2), new PieceId(10)));

        // Assert
        Assert.Equal(
            (Black, Yellow, "2"),
            (transition.State.DecidingSeat, transition.State.ActingFor, string.Join(',', transition.State.Dice))
        );
    }

    [Fact]
    public void Apply_WhenPromoting_ThePawnBecomesTheCapturedPieceForItsOwnArmyAndTheTurnGoesOn()
    {
        // Arrange
        Piece pawn = Piece(1, Chaturaji2027Pieces.Pawn, Black);
        Piece elephant = Piece(11, Chaturaji2027Pieces.Elephant, Green);
        GameState state = this.StateWith(
            (Grid.CellAt(2, 7), pawn),
            (Grid.CellAt(0, 0), Piece(3, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            Prisoners = [new Prisoner(elephant, Red)],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PromoteAction(Black, Grid.CellAt(2, 7), elephant.Id));

        // Assert
        Assert.Equal(
            (elephant, 0, Black, new PiecePromoted(pawn, elephant, Grid.CellAt(2, 7), Black)),
            (
                transition.State.Position.PieceAt(Grid.CellAt(2, 7)),
                transition.State.Prisoners.Count,
                transition.State.DecidingSeat,
                transition.Events.Single()
            )
        );
    }

    [Fact]
    public void Apply_WhenAPromotionLeavesOnlyRajas_EndsTheGameInADraw()
    {
        // Arrange
        Piece raja = Piece(11, Chaturaji2027Pieces.Raja, Green);
        GameState state = this.StateWith(
            (Grid.CellAt(2, 7), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(0, 0), Piece(3, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            Prisoners = [new Prisoner(raja, Yellow)],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PromoteAction(Black, Grid.CellAt(2, 7), raja.Id));

        // Assert
        Assert.Equal(true, transition.State.Outcome?.IsDraw);
    }

    [Fact]
    public void Apply_WhenAnOfferIsMadeAgainInTheSameTurn_RefusesIt()
    {
        // Arrange
        OfferAction offer = new(Black, Yellow, [new PieceId(12)], [new PieceId(1)]);
        GameState offered = this._rules.Apply(this.Ransoms(), offer).State;
        GameState declined = this._rules.Apply(offered, new DeclineAction(Yellow)).State;

        // Act
        Transition Offering() => this._rules.Apply(declined, offer);

        // Assert
        _ = Assert.Throws<IllegalActionException>(Offering);
    }

    [Fact]
    public void Apply_WhenTheTurnPasses_ForgetsTheOffersMade()
    {
        // Arrange
        GameState state = this.Ransoms() with
        {
            IsRansomWindow = true,
            OffersMade = [new RansomOffer(Black, Yellow, [new PieceId(12)], [])],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal((Yellow, 0), (transition.State.DecidingSeat, transition.State.OffersMade.Count));
    }

    [Fact]
    public void AttacksOf_WhenAPawnStandsBesideAFriend_AttacksBothForwardDiagonalsButNotAhead()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(4, 4), Piece(2, Chaturaji2027Pieces.Pawn, Black))
        );

        // Act
        ImmutableList<Attack> attacks = this._rules.AttacksOf(state, Black);

        // Assert
        Assert.Equal(
            [Grid.CellAt(2, 4), Grid.CellAt(4, 4)],
            attacks
                .Where(attack => attack.From == Grid.CellAt(3, 3))
                .Select(attack => attack.Target)
                .OrderBy(cell => cell.Value)
        );
    }

    [Fact]
    public void AttacksOf_WhenOtherArmiesStandOnTheBoard_CountsOnlyTheSeatsOwnPieces()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(5, 5), Piece(2, Chaturaji2027Pieces.Raja, Yellow)),
            (Grid.CellAt(1, 1), Piece(3, Chaturaji2027Pieces.Raja, Green))
        );

        // Act
        ImmutableList<Attack> attacks = this._rules.AttacksOf(state, Black);

        // Assert
        Assert.Equal([new PieceId(1)], attacks.Select(attack => attack.Attacker.Id).Distinct());
    }

    [Theory]
    [InlineData(Chaturaji2027PiecesKind.Raja, true)]
    [InlineData(Chaturaji2027PiecesKind.Pawn, false)]
    public void IsLeader_WhenGivenAKind_IsTrueOnlyForTheRaja(Chaturaji2027PiecesKind kind, bool expected)
    {
        // Arrange - nothing beyond the rules

        // Act
        bool isLeader = this._rules.IsLeader(KindOf(kind));

        // Assert
        Assert.Equal(expected, isLeader);
    }

    [Fact]
    public void WorthOf_WhenAsked_IsWhatTakingThePieceScores()
    {
        // Arrange - nothing beyond the rules

        // Act
        int worth = this._rules.WorthOf(Chaturaji2027Pieces.Elephant);

        // Assert
        Assert.Equal(4, worth);
    }

    [Fact]
    public void MayOffer_WhenTheSeatHoldsAnotherSeatsPieceOnItsOwnTurn_IsTrue()
    {
        // Arrange
        GameState state = this.Ransoms();

        // Act
        bool mayOffer = this._rules.MayOffer(state);

        // Assert
        Assert.True(mayOffer);
    }

    [Theory]
    [InlineData("offer waiting")]
    [InlineData("dice awaited")]
    [InlineData("acting for")]
    [InlineData("rolling off")]
    [InlineData("game over")]
    [InlineData("promotion due")]
    [InlineData("own piece, taken lately")]
    public void MayOffer_WhenNotOnItsOwnTurnOrHoldingNothingThatMayGoBack_IsFalse(string why)
    {
        // Arrange
        GameState ransoms = this.Ransoms();
        GameState state = why switch
        {
            "offer waiting" => ransoms with { Offer = new RansomOffer(Black, Yellow, [new PieceId(12)], []) },
            "dice awaited" => ransoms with { AwaitedDice = new DiceRequest(2, 6) },
            "acting for" => ransoms with { ActingFor = Yellow },
            "rolling off" => ransoms with { RollOff = [Black, Yellow] },
            "game over" => ransoms with { DecidingSeat = null },
            "promotion due" => ransoms with { PromotionDue = Grid.CellAt(2, 7) },
            _ => ransoms with
            {
                Prisoners = [new Prisoner(Piece(20, Chaturaji2027Pieces.Pawn, Black), Black)],
                TakenByOwnTeam = ImmutableDictionary<PieceId, int>.Empty.Add(new PieceId(20), ransoms.TurnNumber),
            },
        };

        // Act
        bool mayOffer = this._rules.MayOffer(state);

        // Assert
        Assert.False(mayOffer);
    }

    [Fact]
    public void Apply_WhenOffering_WaitsOnTheReceiversAnswerAgainstTheClock()
    {
        // Arrange
        GameState state = this.Ransoms();
        OfferAction offer = new(Black, Yellow, [new PieceId(12)], [new PieceId(1)]);

        // Act
        Transition transition = this._rules.Apply(state, offer);

        // Assert
        Assert.Equal(
            (Yellow, offer.Offer, new RansomOffered(offer.Offer)),
            (transition.State.DecidingSeat, transition.State.Offer, transition.Events.Single())
        );
        Assert.Equal([new AcceptAction(Yellow), new DeclineAction(Yellow)], this._rules.LegalActions(transition.State));
        DecisionTimer answer = new("ransom-answer", new DeclineAction(Yellow));
        Assert.Equal(answer, this._rules.TimerOf(transition.State));
    }

    [Theory]
    [InlineData(new int[0], true)]
    [InlineData(new[] { 5 }, true)]
    [InlineData(new[] { 2 }, false)]
    public void LegalActions_WhenAPawnFacesAnAlliedPawnBackToBack_MaySwapPlacesAsAPawnMove(int[] dice, bool maySwap)
    {
        // Arrange - Black faces up, Green down
        GameState state = this.BackToBack() with { Dice = [.. dice] };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(maySwap, actions.Contains(new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4))));
    }

    [Fact]
    public void Apply_WhenAPawnSwapsOntoTheFarEdge_OffersItsPromotionToTheSwapper()
    {
        // Arrange - Black's Pawn on c7, Green's back to back with it on c8; Red holds Green's Elephant 11
        GameState farEdge = this.FarEdgeAhead();
        GameState state = farEdge with
        {
            Position = farEdge.Position.Place(Grid.CellAt(2, 7), Piece(20, Chaturaji2027Pieces.Pawn, Green)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(2, 6), Grid.CellAt(2, 7)));

        // Assert
        Assert.Equal(Grid.CellAt(2, 7), transition.State.PromotionDue);
        Assert.Contains(
            new PromoteAction(Black, Grid.CellAt(2, 7), new PieceId(11)),
            this._rules.LegalActions(transition.State)
        );
    }

    public static TheoryData<string, Piece, Piece, int> NoSwaps =>
        new()
        {
            { "not a pawn", Piece(1, Chaturaji2027Pieces.Boat, Black), Piece(2, Chaturaji2027Pieces.Pawn, Green), 3 },
            {
                "facing no pawn",
                Piece(1, Chaturaji2027Pieces.Pawn, Black),
                Piece(2, Chaturaji2027Pieces.Horse, Green),
                3
            },
            { "same army", Piece(1, Chaturaji2027Pieces.Pawn, Black), Piece(2, Chaturaji2027Pieces.Pawn, Black), 3 },
            { "an enemy", Piece(1, Chaturaji2027Pieces.Pawn, Black), Piece(2, Chaturaji2027Pieces.Pawn, Yellow), 3 },
            { "facing away", Piece(1, Chaturaji2027Pieces.Pawn, Black), Piece(2, Chaturaji2027Pieces.Pawn, Yellow), 3 },
            {
                "facing off the board",
                Piece(1, Chaturaji2027Pieces.Pawn, Black),
                Piece(2, Chaturaji2027Pieces.Pawn, Yellow),
                7
            },
        };

    [Theory]
    [MemberData(nameof(NoSwaps), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenTheCellAheadHoldsNoAllyBackToBack_OffersNoSwap(
        string why,
        Piece piece,
        Piece ahead,
        int file
    )
    {
        // Arrange - Yellow, facing right, is Black's ally only where it would face away
        GameState placed = this.StateWith((Grid.CellAt(file, 3), piece), (Grid.CellAt(file, 4), ahead));
        GameState state = why.StartsWith("facing ", StringComparison.Ordinal) && ahead.Owner == Yellow
            ? placed with { Teams = TeamGraph.FromTeams([[Black, Yellow], [Green, Red]]) }
            : placed;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.DoesNotContain(new MoveAction(Black, Grid.CellAt(file, 3), Grid.CellAt(file, 4)), actions);
    }

    [Fact]
    public void Apply_WhenAPawnSwapsBackToBack_TradesPlacesAndTakesNothing()
    {
        // Arrange
        GameState state = this.BackToBack();
        Piece black = state.Position.PieceAt(Grid.CellAt(3, 3))!;
        Piece green = state.Position.PieceAt(Grid.CellAt(3, 4))!;

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)));

        // Assert
        Assert.Equal(
            new PawnsSwapped(black, green, Grid.CellAt(3, 3), Grid.CellAt(3, 4), Black),
            transition.Events[0]
        );
        Assert.Equal(
            (green, black, 0, 0),
            (
                transition.State.Position.PieceAt(Grid.CellAt(3, 3)),
                transition.State.Position.PieceAt(Grid.CellAt(3, 4)),
                transition.State.Scores[Black],
                transition.State.Prisoners.Count
            )
        );
    }

    public static TheoryData<string, Position, SeatId, bool> Retirements =>
        new()
        {
            { "only its Raja", Alone(), Black, true },
            { "its Raja and a Pawn", Alone().Place(Grid.CellAt(5, 1), BlackPawn), Black, false },
            { "only a Pawn", Alone().Place(Grid.CellAt(3, 3), BlackPawn), Black, false },
            { "enemy on throne", Alone().Place(BlackThrone, Piece(11, Chaturaji2027Pieces.Raja, Yellow)), Black, true },
            { "a regent", new Position(Alone().Occupants.Remove(Grid.CellAt(2, 6))), Green, false },
        };

    private static readonly Piece BlackPawn = Piece(5, Chaturaji2027Pieces.Pawn, Black);

    /// <summary>Black's Raja alone in mid-board; a Pawn each of the others, and Green's Raja.</summary>
    private static Position Alone() =>
        Position.Empty
            .Place(Grid.CellAt(3, 3), Piece(3, Chaturaji2027Pieces.Raja, Black))
            .Place(Grid.CellAt(0, 6), Piece(12, Chaturaji2027Pieces.Pawn, Yellow))
            .Place(Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green))
            .Place(Grid.CellAt(6, 2), Piece(19, Chaturaji2027Pieces.Raja, Green))
            .Place(Grid.CellAt(6, 6), Piece(29, Chaturaji2027Pieces.Pawn, Red));

    [Theory]
    [MemberData(nameof(Retirements), DisableDiscoveryEnumeration = true)]
    public void LegalActions_WhenOnlyItsRajaIsLeftOnItsOwnTurn_MayRetire(
        string why,
        Position position,
        SeatId deciding,
        bool mayRetire
    )
    {
        // Arrange
        GameState placed = this.Later() with { Position = position };
        GameState state = why == "a regent"
            ? placed with { DecidingSeat = Green, ActingFor = Black }
            : placed;

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(mayRetire, actions.Contains(new RetireAction(deciding)));
    }

    [Fact]
    public void Apply_WhenASeatRetires_PassesItsTurnsFromThenOn()
    {
        // Arrange
        GameState state = this.Later() with { Position = Alone() };

        // Act
        Transition retired = this._rules.Apply(state, new RetireAction(Black));
        Transition round = this._rules.Apply(
            retired.State with { DecidingSeat = Red },
            new PassAction(Red)
        );

        // Assert
        Assert.Equal([new RajaRetired(Black), new TurnPassed(Black, Yellow)], retired.Events);
        Assert.Equal([Black], retired.State.Retired);
        Assert.Equal(Yellow, round.State.DecidingSeat);
    }

    [Fact]
    public void Apply_WhenARetiredSeatsRajaIsTaken_ScoresNoOne()
    {
        // Arrange - Yellow's Raja on a6 is the last enemy Raja; Black's Raja holds Green's throne
        GameState empire = this.Empire(GreenThrone, hasGreenRaja: true);
        GameState state = empire with { Retired = [Yellow] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.Equal((0, false), (transition.State.Scores[Black], transition.Events.OfType<EmpireCreated>().Any()));
    }

    [Fact]
    public void Apply_WhenOnlyARetiredEnemyRajaIsLeft_CreatesTheEmpire()
    {
        // Arrange - Red's Raja on h8 has retired
        GameState empire = this.Empire(GreenThrone, hasGreenRaja: true);
        GameState state = empire with
        {
            Position = empire.Position.Place(Grid.CellAt(7, 7), Piece(27, Chaturaji2027Pieces.Raja, Red)),
            Retired = [Red],
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.Equal([new EmpireCreated(Black, 3)], transition.Events.OfType<EmpireCreated>());
        Assert.DoesNotContain(transition.Events, happened => happened is CommandBegun); // Red, retired, commands none
    }

    [Fact]
    public void Apply_WhenARetiredRajaSitsOnAThrone_CommandsNothingThereAndForcesNoRoll()
    {
        // Arrange - Yellow, retired, has its Raja on Black's throne; Black's Raja is beside it
        GameState state = this.Later() with
        {
            Position = Alone().Place(BlackThrone, Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
            Retired = [Yellow],
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)));

        // Assert
        Assert.Contains(new PassAction(Black), this._rules.LegalActions(state));
        Assert.DoesNotContain(transition.Events, happened => happened is CommandBegun or CommandEnded);
    }

    [Fact]
    public void Apply_WhenARajaEntersAnAllysThroneWithOnlyARetiredEnemyRajaLeft_TellsOfTheEmpire()
    {
        // Arrange - Black's Raja beside Green's throne; Red's Raja, the enemies' only one, has retired
        GameState commands = this.Commands(Grid.CellAt(4, 6), Grid.CellAt(6, 2), hasEnemyRajas: false);
        GameState state = commands with
        {
            Position = commands.Position.Place(Grid.CellAt(7, 0), Piece(27, Chaturaji2027Pieces.Raja, Red)),
            Retired = [Red],
        };

        // Act
        Transition entered = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(4, 6), GreenThrone));

        // Assert
        Assert.Equal(new EmpireBegun([Black, Green]), entered.Events[^1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Apply_WhenTheOnlyCommandOfAnAllyIsARetiredSeatsOrOverOne_CreatesNoEmpire(bool isBlackRajaless)
    {
        // Arrange - either Black has no Raja, and retired Green has; or Black's Raja holds retired Green's throne
        GameState held = this.Empire(GreenThrone, hasGreenRaja: true);
        GameState empire = isBlackRajaless
            ? held with { Position = new Position(held.Position.Occupants.Remove(GreenThrone)) }
            : held;
        GameState state = empire with { Retired = [Green] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        Assert.DoesNotContain(transition.Events, happened => happened is EmpireCreated);
    }

    [Fact]
    public void LegalActions_WhenARetiredAllyHasNoRaja_CommandsNoneOfItsArmy()
    {
        // Arrange - Green, retired, has lost its Raja; its Pawn is on c7
        GameState state = this.Later() with
        {
            Position = Alone().Place(Grid.CellAt(5, 1), Piece(5, Chaturaji2027Pieces.Pawn, Black)),
            Retired = [Green],
        };
        GameState rajaless = state with
        {
            Position = new Position(state.Position.Occupants.Remove(Grid.CellAt(6, 2))),
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(rajaless);

        // Assert
        Assert.DoesNotContain(actions, action => action is MoveAction move && move.From == Grid.CellAt(2, 6));
    }

    public static TheoryData<string, OfferAction> InvalidOffers =>
        new()
        {
            { "game over", new OfferAction(Black, Yellow, [new PieceId(12)], []) },
            { "dice awaited", new OfferAction(Black, Yellow, [new PieceId(12)], []) },
            { "not deciding", new OfferAction(Yellow, Black, [new PieceId(1)], []) },
            { "to oneself, another's prisoner", new OfferAction(Black, Black, [new PieceId(12)], []) },
            { "nothing given", new OfferAction(Black, Yellow, [], [new PieceId(1)]) },
            { "not held", new OfferAction(Black, Yellow, [new PieceId(12), new PieceId(99)], []) },
            { "asked for the enemy's", new OfferAction(Black, Yellow, [new PieceId(12)], [new PieceId(14)]) },
            { "to a retired seat", new OfferAction(Black, Yellow, [new PieceId(12)], []) },
        };

    [Theory]
    [MemberData(nameof(InvalidOffers), DisableDiscoveryEnumeration = true)]
    public void Apply_WhenTheOfferMayNotBeMade_ThrowsIllegalActionException(string why, OfferAction offer)
    {
        // Arrange
        GameState ransoms = this.Ransoms();
        GameState state = why switch
        {
            "game over" => ransoms with { DecidingSeat = null },
            "dice awaited" => ransoms with { AwaitedDice = new DiceRequest(2, 6) },
            "to a retired seat" => ransoms with { Retired = [Yellow] },
            _ => ransoms,
        };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(() => this._rules.Apply(state, offer));

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenASeatReleasesItsOwnPiece_SendsItHomeAtOnce()
    {
        // Arrange - Black holds its own Elephant, taken long ago
        GameState state = this.Releasing(Piece(2, Chaturaji2027Pieces.Elephant, Black), Black);

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Black, [new PieceId(2)], []));

        // Assert
        Assert.Equal(
            (Black, null, true, 0),
            (
                transition.State.DecidingSeat,
                transition.State.Offer,
                transition.Events[0] is PrisonersReleased,
                transition.State.Prisoners.Count
            )
        );
        Assert.Equal(Chaturaji2027Pieces.Elephant, transition.State.Position.PieceAt(Grid.CellAt(2, 0))!.Kind);
    }

    [Theory]
    [InlineData(3, false)]
    [InlineData(4, true)]
    public void Apply_WhenASeatReleasesAPieceItsOwnTeamTook_WaitsUntilEverySeatHasHadATurn(
        int turnsSince,
        bool isReleased
    )
    {
        // Arrange - Black took its own Elephant on turn 10
        GameState state = this.Releasing(Piece(2, Chaturaji2027Pieces.Elephant, Black), Black) with
        {
            TurnNumber = 10 + turnsSince,
            TakenByOwnTeam = ImmutableDictionary<PieceId, int>.Empty.Add(new PieceId(2), 10),
        };
        OfferAction release = new(Black, Black, [new PieceId(2)], []);

        // Act
        bool mayGoBack = this._rules.MayGoBack(state, state.Prisoners[0]);
        bool mayOffer = this._rules.MayOffer(state);
        Exception? refused = Record.Exception(() => this._rules.Apply(state, release));

        // Assert
        Assert.Equal(
            (isReleased, isReleased, !isReleased),
            (mayGoBack, mayOffer, refused is IllegalActionException)
        );
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void Apply_WhenASeatReleasesAnAllysPiece_AsksTheAllyOnlyIfAnEnemyHoldsItsThrone(bool isHeld, bool isAtOnce)
    {
        // Arrange - Black holds Green's Horse; Yellow's Raja on Green's throne, if held
        GameState releasing = this.Releasing(Piece(17, Chaturaji2027Pieces.Horse, Green), Black);
        GameState state = isHeld
            ? releasing with
            {
                Position = releasing.Position.Place(GreenThrone, Piece(12, Chaturaji2027Pieces.Raja, Yellow)),
            }
            : releasing;

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Green, [new PieceId(17)], []));

        // Assert
        Assert.Equal(
            (isAtOnce, isAtOnce ? Black : Green, isAtOnce ? 0 : 1),
            (transition.Events[0] is PrisonersReleased, transition.State.DecidingSeat, transition.State.Prisoners.Count)
        );
    }

    [Fact]
    public void Apply_WhenASeatAsksSomethingOfItsOwnTeam_ThrowsIllegalActionException()
    {
        // Arrange - Black holds Green's Horse; Green holds Black's Boat
        GameState releasing = this.Releasing(Piece(17, Chaturaji2027Pieces.Horse, Green), Black);
        GameState state = releasing with
        {
            Prisoners = releasing.Prisoners.Add(new Prisoner(Piece(0, Chaturaji2027Pieces.Boat, Black), Green)),
        };

        // Act
        Exception? refused = Record.Exception(() =>
            this._rules.Apply(state, new OfferAction(Black, Green, [new PieceId(17)], [new PieceId(0)]))
        );

        // Assert
        _ = Assert.IsType<IllegalActionException>(refused);
    }

    [Fact]
    public void Apply_WhenAReleasedRajaFindsAnEnemyOnItsThrone_TakesIt()
    {
        // Arrange - Black holds its own Raja; Yellow's Pawn on Black's throne
        Piece pawn = Piece(13, Chaturaji2027Pieces.Pawn, Yellow);
        GameState releasing = this.Releasing(Piece(3, Chaturaji2027Pieces.Raja, Black), Black);
        GameState state = releasing with { Position = releasing.Position.Place(BlackThrone, pawn) };

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Black, [new PieceId(3)], []));

        // Assert
        Assert.Equal(
            (new PieceCaptured(pawn, Black, 1), Chaturaji2027Pieces.Raja, state.Scores[Black] + 1),
            (
                transition.Events.OfType<PieceCaptured>().Single(),
                transition.State.Position.PieceAt(BlackThrone)!.Kind,
                transition.State.Scores[Black]
            )
        );
    }

    [Fact]
    public void Apply_WhenAPieceIsReleasedWhileItsRajaIsOnItsThrone_TakesTheEnemyOnItsSquare()
    {
        // Arrange - Black's Raja on its throne; Black holds its own Elephant; Yellow's Pawn on c1, its square
        Piece pawn = Piece(13, Chaturaji2027Pieces.Pawn, Yellow);
        GameState releasing = this.Releasing(Piece(2, Chaturaji2027Pieces.Elephant, Black), Black);
        GameState state = releasing with
        {
            Position = releasing.Position
                .Place(BlackThrone, Piece(3, Chaturaji2027Pieces.Raja, Black))
                .Place(Grid.CellAt(2, 0), pawn),
        };

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Black, [new PieceId(2)], []));

        // Assert
        Assert.Equal(
            (new PieceCaptured(pawn, Black, 1), Chaturaji2027Pieces.Elephant),
            (
                transition.Events.OfType<PieceCaptured>().Single(),
                transition.State.Position.PieceAt(Grid.CellAt(2, 0))!.Kind
            )
        );
    }

    [Theory]
    [InlineData("no one on the throne")]
    [InlineData("a Pawn on the throne")]
    [InlineData("an enemy Raja on the throne")]
    [InlineData("an ally on its square")]
    public void Apply_WhenAPieceIsReleasedButNotStriking_TakesNothing(string why)
    {
        // Arrange - Black holds its own Elephant, whose square, c1, someone stands on
        GameState releasing = this.Releasing(Piece(2, Chaturaji2027Pieces.Elephant, Black), Black);
        (Piece onThrone, Piece onSquare) = why switch
        {
            "a Pawn on the throne" =>
                (Piece(4, Chaturaji2027Pieces.Pawn, Black), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            "an enemy Raja on the throne" =>
                (Piece(12, Chaturaji2027Pieces.Raja, Yellow), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            "an ally on its square" =>
                (Piece(3, Chaturaji2027Pieces.Raja, Black), Piece(20, Chaturaji2027Pieces.Pawn, Green)),
            _ => (Piece(5, Chaturaji2027Pieces.Pawn, Black), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
        };
        Position position = releasing.Position.Place(Grid.CellAt(2, 0), onSquare);
        GameState state = releasing with
        {
            Position = why == "no one on the throne" ? position : position.Place(BlackThrone, onThrone),
        };

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Black, [new PieceId(2)], []));

        // Assert
        Assert.Equal(
            (0, onSquare),
            (transition.Events.OfType<PieceCaptured>().Count(), transition.State.Position.PieceAt(Grid.CellAt(2, 0)))
        );
    }

    [Fact]
    public void Apply_WhenAnOfferIsAcceptedAndAPiecesHomeIsTaken_WaitsOnItsOwnerToChooseAmongItsKindsSquares()
    {
        // Arrange - Yellow's pawn 12 starts on b8, where Green's pawn stands; Yellow's other Pawns' are free
        GameState state = this.AcceptingRansom();

        // Act
        Transition transition = this._rules.Apply(state, new AcceptAction(Yellow));

        // Assert - nearest its own square first
        Assert.Equal(
            [
                new PlaceAction(Yellow, Grid.CellAt(1, 6)),
                new PlaceAction(Yellow, Grid.CellAt(1, 5)),
                new PlaceAction(Yellow, Grid.CellAt(1, 4)),
            ],
            this._rules.LegalActions(transition.State)
        );
    }

    [Fact]
    public void Apply_WhenARansomSendingARajaHomeIsAccepted_ScoresTheReceiverTwoForIt()
    {
        // Arrange - Black gives Yellow's pawn 12 back for its Raja, which Yellow holds
        GameState ransoms = this.AcceptingRansom();
        GameState state = ransoms with
        {
            Prisoners = ransoms.Prisoners.Add(new Prisoner(Piece(3, Chaturaji2027Pieces.Raja, Black), Yellow)),
            Offer = new RansomOffer(Black, Yellow, [new PieceId(12)], [new PieceId(3)]),
        };

        // Act
        Transition transition = this._rules.Apply(state, new AcceptAction(Yellow));

        // Assert
        Assert.Equal([new RansomRewarded(Yellow, 1, 2)], transition.Events.OfType<RansomRewarded>());
        Assert.Equal(state.Scores[Yellow] + 2, transition.State.Scores[Yellow]);
    }

    [Fact]
    public void Apply_WhenAGiftIsAccepted_ScoresTheReceiverNothing()
    {
        // Arrange - Black gives Yellow's pawn 12 back, asking nothing
        GameState state = this.AcceptingRansom() with
        {
            Offer = new RansomOffer(Black, Yellow, [new PieceId(12)], []),
        };

        // Act
        Transition transition = this._rules.Apply(state, new AcceptAction(Yellow));

        // Assert
        Assert.Equal(
            (0, state.Scores[Yellow]),
            (transition.Events.OfType<RansomRewarded>().Count(), transition.State.Scores[Yellow])
        );
    }

    [Fact]
    public void Apply_WhenAnOfferIsAcceptedAndEverySquareOfAPiecesKindIsTaken_WaitsOnItsOwnerToChooseAmongTheNearest()
    {
        // Arrange - Yellow's pawn 12 starts on b8, where Green's pawn stands; Yellow faces right
        GameState accepting = this.AcceptingRansom();
        GameState state = accepting with
        {
            Position = accepting.Position
                .Place(Grid.CellAt(1, 6), Piece(20, Chaturaji2027Pieces.Pawn, Green))
                .Place(Grid.CellAt(1, 5), Piece(21, Chaturaji2027Pieces.Pawn, Green))
                .Place(Grid.CellAt(1, 4), Piece(22, Chaturaji2027Pieces.Pawn, Green)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new AcceptAction(Yellow));

        // Assert - nearest Yellow's back rank, then nearest its left
        Assert.Equal([new RansomAccepted(state.Offer!), new RansomRewarded(Yellow, 1, 1)], transition.Events);
        Assert.Equal(state.Scores[Yellow] + 1, transition.State.Scores[Yellow]);
        Assert.Equal(
            (Yellow, null, 2, Black, false),
            (
                transition.State.DecidingSeat,
                transition.State.Offer,
                transition.State.Returning!.Prisoners.Count,
                transition.State.Returning.Resume,
                this._rules.MayOffer(transition.State)
            )
        );
        Assert.Equal(
            [
                new PlaceAction(Yellow, Grid.CellAt(0, 7)),
                new PlaceAction(Yellow, Grid.CellAt(0, 6)),
                new PlaceAction(Yellow, Grid.CellAt(2, 7)),
                new PlaceAction(Yellow, Grid.CellAt(2, 6)),
            ],
            this._rules.LegalActions(transition.State)
        );
        Assert.Equal(
            new DecisionTimer("ransom-return", new PlaceAction(Yellow, Grid.CellAt(0, 7))),
            this._rules.TimerOf(transition.State)
        );
    }

    [Fact]
    public void Apply_WhenAPieceGoingHomeIsPlaced_TheRestFollowAndTheOffererDecidesAgain()
    {
        // Arrange - Black's horse 1 follows Yellow's pawn, to its own free square, b1
        GameState waiting = this._rules.Apply(this.AcceptingRansom(), new AcceptAction(Yellow)).State;

        // Act
        Transition transition = this._rules.Apply(waiting, new PlaceAction(Yellow, Grid.CellAt(1, 5)));

        // Assert
        Piece pawn = Piece(12, Chaturaji2027Pieces.Pawn, Yellow);
        Piece horse = Piece(1, Chaturaji2027Pieces.Horse, Black);
        Assert.Equal(
            [new PieceReturned(pawn, Grid.CellAt(1, 5)), new PieceReturned(horse, Grid.CellAt(1, 0))],
            transition.Events
        );
        Assert.Equal(
            (Black, null, 1, pawn, horse),
            (
                transition.State.DecidingSeat,
                transition.State.Returning,
                transition.State.Prisoners.Count,
                transition.State.Position.PieceAt(Grid.CellAt(1, 5)),
                transition.State.Position.PieceAt(Grid.CellAt(1, 0))
            )
        );
    }

    [Theory]
    [InlineData(5, 5, true)]
    [InlineData(5, 6, false)]
    public void LegalActions_WhenDoublesAreRolled_LetACapturedPieceOfTheirKindEscapeHome(
        int first,
        int second,
        bool mayEscape
    )
    {
        // Arrange - Yellow holds Black's Raja 3, which starts on d1, and Black's Boat 0
        GameState state = this.Escapes() with { Dice = [first, second] };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            mayEscape ? [new EscapeAction(Black, new PieceId(3), BlackThrone)] : [],
            actions.OfType<EscapeAction>()
        );
    }

    [Fact]
    public void Apply_WhenAPieceEscapes_PutsItHomeForItsArmyAndEndsTheTurn()
    {
        // Arrange
        GameState state = this.Escapes() with { Dice = [5, 5] };
        Piece raja = Piece(3, Chaturaji2027Pieces.Raja, Black);

        // Act
        Transition transition = this._rules.Apply(state, new EscapeAction(Black, raja.Id, BlackThrone));

        // Assert
        Assert.Equal(new PieceEscaped(raja, BlackThrone, Black), transition.Events[0]);
        Assert.Equal(
            (raja, 1, Yellow, true),
            (
                transition.State.Position.PieceAt(BlackThrone),
                transition.State.Prisoners.Count,
                transition.State.DecidingSeat,
                transition.State.Dice.IsEmpty
            )
        );
    }

    [Fact]
    public void Apply_WhenAnEnemyStandsWhereAPieceEscapesTo_TakesIt()
    {
        // Arrange - Yellow's Pawn stands on d1
        GameState escapes = this.Escapes();
        Piece pawn = Piece(20, Chaturaji2027Pieces.Pawn, Yellow);
        GameState state = escapes with { Dice = [5, 5], Position = escapes.Position.Place(BlackThrone, pawn) };

        // Act
        Transition transition = this._rules.Apply(state, new EscapeAction(Black, new PieceId(3), BlackThrone));

        // Assert
        Assert.Equal(new PieceCaptured(pawn, Black, 1), transition.Events[1]);
        Assert.Equal(
            (1, new Prisoner(pawn, Black)),
            (transition.State.Scores[Black], transition.State.Prisoners[^1])
        );
    }

    [Fact]
    public void Apply_WhenAnEscapeTakesTheLastEnemyPiece_EndsTheGameWithThatTeamWinning()
    {
        // Arrange - Yellow's Pawn on d1 is the last enemy piece
        GameState state = this.Escapes() with
        {
            Dice = [5, 5],
            Position = Position.Empty
                .Place(Grid.CellAt(3, 3), Piece(4, Chaturaji2027Pieces.Pawn, Black))
                .Place(BlackThrone, Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new EscapeAction(Black, new PieceId(3), BlackThrone));

        // Assert
        GameOutcome won = new([Black, Green], "only one team has pieces left");
        Assert.Equal((null, won), (transition.State.DecidingSeat, transition.State.Outcome));
    }

    [Fact]
    public void LegalActions_WhenAnAllyStandsWhereAPieceWouldEscapeTo_OffersTheNearestFreeSquares()
    {
        // Arrange - Green's Pawn stands on d1, and Black has no other Raja's square to go to
        GameState escapes = this.Escapes();
        GameState state = escapes with
        {
            Dice = [5, 5],
            Position = escapes.Position.Place(BlackThrone, Piece(21, Chaturaji2027Pieces.Pawn, Green)),
        };

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert - on its own row first, then nearest the back rank, then nearest the left
        Assert.Equal(
            [Grid.CellAt(2, 0), Grid.CellAt(4, 0), Grid.CellAt(2, 1), Grid.CellAt(3, 1), Grid.CellAt(4, 1)],
            actions.OfType<EscapeAction>().Select(escape => escape.Home)
        );
    }

    [Fact]
    public void Apply_WhenOnlyAnEscapeCanUseTheDoubles_DoesNotForfeitThem()
    {
        // Arrange - Boats rolled, and Black has no Boat on the board, but Yellow holds one of Black's
        GameState state = this.Escapes();
        int[] boats = [1, 1];

        // Act
        Transition transition =
            this._rules.Apply(state with { AwaitedDice = new DiceRequest(2, 6) }, new DiceAction(Black, [.. boats]));

        // Assert
        Assert.Equal(Black, transition.State.DecidingSeat);
        Assert.Equal(
            [new PassAction(Black), new EscapeAction(Black, new PieceId(0), Grid.CellAt(0, 0))],
            this._rules.LegalActions(transition.State)
        );
    }

    [Theory]
    [InlineData(true, "elsewhere", false)]
    [InlineData(false, "elsewhere", true)]
    [InlineData(true, "on Black's throne", true)]
    [InlineData(false, "nowhere", false)]
    public void LegalActions_WhenAnAllyMayBeRegent_LetTheSeatHandItsTurnOver(
        bool hasBlackRaja,
        string greenRaja,
        bool mayHandOver
    )
    {
        // Arrange - Green may be regent while it has a Raja, and Black has none, or Green's is on Black's throne
        CellId? green = greenRaja switch
        {
            "elsewhere" => Grid.CellAt(6, 2),
            "on Black's throne" => BlackThrone,
            _ => null,
        };
        GameState state = this.Regency(hasBlackRaja, green);

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(mayHandOver ? [new HandOverAction(Black, Green)] : [], actions.OfType<HandOverAction>());
    }

    [Fact]
    public void Apply_WhenASeatHandsItsTurnOver_LetsTheRegentMoveTheSeatsOwnArmyOnly()
    {
        // Arrange - Black has no Raja; Green has
        GameState state = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2));

        // Act
        Transition transition = this._rules.Apply(state, new HandOverAction(Black, Green));

        // Assert
        ImmutableList<GameAction> actions = this._rules.LegalActions(transition.State);
        Assert.Equal((Green, Black), (transition.State.DecidingSeat, transition.State.ActingFor));
        Assert.Equal(
            [new TurnHandedOver(Black, Green), new CommandBegun(Green, Black, CommandBasis.Regency)],
            transition.Events
        );
        Assert.Equal(
            (true, false),
            (actions.Contains(new RollAction(Green)), actions.OfType<HandOverAction>().Any())
        );
        Assert.All(
            actions.OfType<MoveAction>(),
            move => Assert.Equal((Green, Grid.CellAt(0, 0)), (move.Actor, move.From))
        );
    }

    [Fact]
    public void Apply_WhenTheRegentHasUsedTheDice_PassesTheTurnOnFromTheSeat()
    {
        // Arrange - Black rolled two Elephants, then handed the turn to Green
        GameState rolled = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2)) with { Dice = [3, 4] };
        GameState state = this._rules.Apply(rolled, new HandOverAction(Black, Green)).State;

        // Act
        GameState once = this._rules.Apply(state, new MoveAction(Green, Grid.CellAt(0, 0), Grid.CellAt(0, 1))).State;
        Transition twice = this._rules.Apply(once, new MoveAction(Green, Grid.CellAt(0, 1), Grid.CellAt(0, 2)));

        // Assert
        Assert.Equal((Green, Black), (once.DecidingSeat, once.ActingFor));
        Assert.Equal(
            (Yellow, null, new TurnPassed(Black, Yellow)),
            (twice.State.DecidingSeat, twice.State.ActingFor, twice.Events.OfType<TurnPassed>().Single())
        );
        Assert.Equal(new CommandBegun(Green, Black, CommandBasis.LeaderlessAlly), twice.Events[^1]);
    }

    [Fact]
    public void Apply_WhenTheRegentHasMovedForASeatWithAnEnemyOnItsThrone_HandsTheOtherDieToTheOccupier()
    {
        // Arrange - Black has no Raja, and Yellow's sits on Black's throne; Black handed its turn to Green
        GameState regency = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2));
        GameState rolled = regency with
        {
            Dice = [3, 4],
            Position = regency.Position.Place(BlackThrone, Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
        };
        GameState state = this._rules.Apply(rolled, new HandOverAction(Black, Green)).State;

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Green, Grid.CellAt(0, 0), Grid.CellAt(0, 1)));

        // Assert
        Assert.Equal(
            (Yellow, Black, new TurnShared(Black, Yellow)),
            (transition.State.DecidingSeat, transition.State.ActingFor, transition.Events.OfType<TurnShared>().Single())
        );
    }

    [Fact]
    public void Apply_WhenASeatWithAnEnemyOnItsThroneHandsOverBeforeRolling_LeavesTheRegentOnlyTheRoll()
    {
        // Arrange - Black has no Raja, and Yellow's sits on Black's throne
        GameState regency = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2));
        GameState state = regency with
        {
            Position = regency.Position.Place(BlackThrone, Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new HandOverAction(Black, Green));

        // Assert
        Assert.Equal([new RollAction(Black), new HandOverAction(Black, Green)], this._rules.LegalActions(state));
        Assert.Equal([new RollAction(Green)], this._rules.LegalActions(transition.State));
    }

    [Fact]
    public void Apply_WhenASeatProposesEndingTheGame_AsksTheNextSeatClockwiseAgainstTheClock()
    {
        // Arrange - Black's turn; Green proposes, out of turn
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2));

        // Act
        Transition transition = this._rules.Apply(state, new ProposeEndAction(Green, Resigns: false));

        // Assert
        Assert.Equal(
            (Red, new EndProposal(Green, false, Black, null), new EndProposed(Green, false)),
            (transition.State.DecidingSeat, transition.State.Proposal, transition.Events.Single())
        );
        Assert.Equal(
            [new AnswerEndAction(Red, true), new AnswerEndAction(Red, false)],
            this._rules.LegalActions(transition.State)
        );
        Assert.Equal(
            (new DecisionTimer("end-answer", new AnswerEndAction(Red, false)), false),
            (this._rules.TimerOf(transition.State), this._rules.MayProposeEnd(transition.State, Yellow))
        );
    }

    [Fact]
    public void Apply_WhenEverySeatStillPlayingAgrees_EndsTheGameOnPoints()
    {
        // Arrange - Red has no pieces left, so is not asked; Black and Green lead
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green))
        ) with
        {
            Scores = this.Later().Scores.SetItem(Green, 4),
        };

        // Act
        GameState proposed = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: false)).State;
        GameState once = this._rules.Apply(proposed, new AnswerEndAction(Yellow, true)).State;
        Transition twice = this._rules.Apply(once, new AnswerEndAction(Green, true));

        // Assert
        GameOutcome won = new([Black, Green], "every player agreed to end it, so points decide");
        Assert.Equal((Green, null), (once.DecidingSeat, once.Outcome));
        Assert.Equal(
            (null, null, won),
            (twice.State.DecidingSeat, twice.State.Proposal, twice.State.Outcome)
        );
        Assert.Equal(new EndAgreed(Green), twice.Events[0]);
    }

    [Fact]
    public void Apply_WhenASeatRefusesToEndTheGame_ResumesWhereTheGameWas()
    {
        // Arrange - Green moves for Black, which has no Raja, with dice rolled; Yellow proposes
        GameState rolled = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2)) with { Dice = [3, 4] };
        GameState regency = this._rules.Apply(rolled, new HandOverAction(Black, Green)).State;
        GameState state = this._rules.Apply(regency, new ProposeEndAction(Yellow, Resigns: false)).State;

        // Act
        Transition transition = this._rules.Apply(state, new AnswerEndAction(Green, false));

        // Assert
        Assert.Equal(
            (Green, Black, null, "3,4", new EndRefused(Green)),
            (
                transition.State.DecidingSeat,
                transition.State.ActingFor,
                transition.State.Proposal,
                string.Join(',', transition.State.Dice),
                transition.Events.Single()
            )
        );
    }

    [Fact]
    public void Apply_WhenASeatThatGaveUpItsTurnsWouldBeAsked_TakesItsAgreementAsGiven()
    {
        // Arrange - Yellow gave up its turns earlier
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with { Resigned = [Yellow] };

        // Act
        Transition transition = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: false));

        // Assert
        Assert.Equal(Green, transition.State.DecidingSeat);
    }

    [Fact]
    public void Apply_WhenTheLastOfATeamGivesUpItsTurns_EndsTheGameOnPoints()
    {
        // Arrange - Green gave up its turns earlier; Black leads Yellow and Red by ten points, and 15 to 2 on the board
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with
        {
            Resigned = [Green],
            Scores = this.Later().Scores.SetItem(Black, 10),
        };

        // Act
        Transition transition = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: true));

        // Assert
        GameOutcome outcome = new([Black, Green], "a whole team gave up their turns, so points decide");
        Assert.Equal(
            (outcome, null, null),
            (transition.State.Outcome, transition.State.Proposal, transition.State.DecidingSeat)
        );
        Assert.Equal([new EndProposed(Black, true, IsAhead: true), new GameEnded(outcome)], transition.Events);
    }

    [Theory]
    [InlineData(true, 9, 0, "on")]
    [InlineData(false, 10, 0, "on")]
    [InlineData(true, 0, 0, "on")]
    [InlineData(true, 0, 1, "Yellow")]
    public void Apply_WhenTheLastOfATeamGivesUpItsTurns_EndsTheGameOnlyIfItTrailsOrLeadsByFar(
        bool hasBlackRaja,
        int black,
        int yellow,
        string expected
    )
    {
        // Arrange - Green gave up its turns earlier; on the board, Black and Green have 15, or 10 without
        // Black's Raja, to Yellow and Red's 2
        GameState state = this.Regency(hasBlackRaja, Grid.CellAt(6, 2)) with
        {
            Resigned = [Green],
            Scores = this.Later().Scores.SetItem(Black, black).SetItem(Yellow, yellow),
        };

        // Act
        Transition transition = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: true));

        // Assert
        Assert.Equal(
            expected == "Yellow" ? [Yellow, Red] : null,
            transition.State.Outcome?.Winners
        );
    }

    [Fact]
    public void Apply_WhenTheEnemiesOfATeamThatGaveUpCatchUp_EndsTheGameOnPoints()
    {
        // Arrange - Black and Green gave up their turns earlier, a point ahead; Red has since drawn ahead
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with
        {
            Resigned = [Black, Green],
            Scores = this.Later().Scores.SetItem(Black, 1).SetItem(Red, 2),
            DecidingSeat = Yellow,
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Yellow));

        // Assert
        Assert.Equal([Yellow, Red], transition.State.Outcome?.Winners);
    }

    [Fact]
    public void Apply_WhenOneOfATeamGivesUpItsTurnsWithAnAllyStillPlaying_AsksTheOthers()
    {
        // Arrange - Green, Black's ally, still plays
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2));

        // Act
        Transition transition = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: true));

        // Assert
        Assert.Equal((null, Yellow), (transition.State.Outcome, transition.State.DecidingSeat));
    }

    [Fact]
    public void Apply_WhenTheProposerGaveUpItsTurnsAndIsRefused_PassesItsTurn()
    {
        // Arrange - Black, deciding, gives up its turns
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2));
        GameState proposed = this._rules.Apply(state, new ProposeEndAction(Black, Resigns: true)).State;

        // Act
        Transition transition = this._rules.Apply(proposed, new AnswerEndAction(Yellow, false));

        // Assert
        Assert.Equal(
            (Yellow, true),
            (transition.State.DecidingSeat, transition.State.Resigned.Contains(Black))
        );
        Assert.Equal(
            [new EndRefused(Yellow), new TurnGivenUp(Black), new TurnPassed(Black, Yellow)],
            transition.Events
        );
    }

    [Fact]
    public void Apply_WhenASeatGivesUpItsTurnsWhileItMayOfferRansoms_PassesOnOfferingOnceRefused()
    {
        // Arrange - Black's turn is over, and it may still offer ransoms; it gives up its turns
        GameState window = this.Ransoms() with { IsRansomWindow = true };
        GameState proposed = this._rules.Apply(window, new ProposeEndAction(Black, Resigns: true)).State;

        // Act
        Transition transition = this._rules.Apply(proposed, new AnswerEndAction(Yellow, false));

        // Assert
        Assert.Equal(
            (Yellow, false, new TurnGivenUp(Black)),
            (transition.State.DecidingSeat, transition.State.IsRansomWindow, transition.Events[1])
        );
    }

    [Fact]
    public void Apply_WhenTheTurnComesToASeatThatGaveUpWithAnAllyToRule_HandsItToTheRegent()
    {
        // Arrange - Black, with no Raja, gave up its turns; Green has a Raja
        GameState state = this.Regency(hasBlackRaja: false, Grid.CellAt(6, 2)) with
        {
            DecidingSeat = Red,
            Resigned = [Black],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Red));

        // Assert
        Assert.Equal((Green, Black), (transition.State.DecidingSeat, transition.State.ActingFor));
        Assert.Equal(
            [
                new TurnPassed(Red, Black),
                new TurnHandedOver(Black, Green),
                new CommandBegun(Green, Black, CommandBasis.Regency),
            ],
            transition.Events
        );
    }

    [Fact]
    public void Apply_WhenTheTurnComesToASeatThatGaveUpWithNoRegent_PassesItOn()
    {
        // Arrange - Black, with its Raja, gave up its turns
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with
        {
            DecidingSeat = Red,
            Resigned = [Black],
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Red));

        // Assert
        Assert.Equal(Yellow, transition.State.DecidingSeat);
        Assert.Equal(
            [new TurnPassed(Red, Black), new TurnGivenUp(Black), new TurnPassed(Black, Yellow)],
            transition.Events
        );
    }

    [Fact]
    public void Apply_WhenASeatThatGaveUpMustAnswerAgainstTheClock_LeavesItToTheClock()
    {
        // Arrange - Yellow gave up its turns; Red, its ally, still plays
        GameState ransoms = this.Ransoms();
        GameState state = ransoms with
        {
            Position = ransoms.Position.Place(Grid.CellAt(7, 7), Piece(27, Chaturaji2027Pieces.Pawn, Red)),
            Resigned = [Yellow],
        };

        // Act
        Transition transition = this._rules.Apply(state, new OfferAction(Black, Yellow, [new PieceId(12)], []));

        // Assert
        Assert.Equal(
            (Yellow, false),
            (transition.State.DecidingSeat, transition.Events.OfType<TurnGivenUp>().Any())
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Apply_WhenPassingWhileItMayOffer_PassesTheTurnWithoutARansomWindow(bool hasRolled)
    {
        // Arrange - Black holds prisoners, so could have offered before passing
        GameState state = this.Ransoms() with { Dice = hasRolled ? [5] : [] };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Yellow, false, false),
            (
                transition.State.DecidingSeat,
                transition.State.IsRansomWindow,
                transition.Events.OfType<RansomWindowOpened>().Any()
            )
        );
    }

    [Fact]
    public void Apply_WhenARollLetsNothingMoveWithPrisonersHeld_OpensTheRansomWindow()
    {
        // Arrange - Black, with only its Raja, waits on its dice
        GameState state = this.Ransoms() with { AwaitedDice = new DiceRequest(2, 6) };

        // Act
        Transition transition = this._rules.Apply(state, new DiceAction(Black, [2, 3]));

        // Assert
        Assert.Equal((Black, true), (transition.State.DecidingSeat, transition.State.IsRansomWindow));
    }

    [Fact]
    public void Apply_WhenATurnEndsWithPrisonersHeldByASeatThatGaveUp_PassesTheTurnWithoutARansomWindow()
    {
        // Arrange - Black holds prisoners, but gave up its turns while deciding; its last die moves its Raja
        GameState state = this.Ransoms() with { Resigned = [Black], Dice = [5] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)));

        // Assert
        Assert.Equal((Yellow, false), (transition.State.DecidingSeat, transition.State.IsRansomWindow));
    }

    [Theory]
    [InlineData("game over")]
    [InlineData("proposal waiting")]
    [InlineData("dice awaited")]
    [InlineData("rolling off")]
    [InlineData("no pieces")]
    [InlineData("gave up")]
    public void MayProposeEnd_WhenTheGameCannotPauseOrTheSeatGaveUp_IsFalse(string situation)
    {
        // Arrange - Red has no pieces left in the ransoms' position
        GameState playing = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2));
        (GameState state, SeatId seat) = situation switch
        {
            "game over" => (playing with { Outcome = new GameOutcome([], "over") }, Black),
            "proposal waiting" => (playing with { Proposal = new EndProposal(Yellow, false, Black, null) }, Black),
            "dice awaited" => (playing with { AwaitedDice = new DiceRequest(2, 6) }, Black),
            "rolling off" => (this._rules.CreateInitialState() with { AwaitedDice = null }, Black),
            "gave up" => (playing with { Resigned = [Black] }, Black),
            _ => (this.Ransoms(), Red),
        };

        // Act
        bool mayPropose = this._rules.MayProposeEnd(state, seat);

        // Assert
        Assert.False(mayPropose);
    }

    [Fact]
    public void Apply_WhenASeatThatGaveUpRejoins_PlaysItsOwnTurnsAgain()
    {
        // Arrange - Black's turn; Yellow gave up its turns
        GameState state = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with { Resigned = [Yellow] };

        // Act
        Transition rejoined = this._rules.Apply(state, new RejoinAction(Yellow));
        Transition passed = this._rules.Apply(rejoined.State, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Black, true, new Rejoined(Yellow)),
            (rejoined.State.DecidingSeat, rejoined.State.Resigned.IsEmpty, rejoined.Events.Single())
        );
        Assert.Equal([new TurnPassed(Black, Yellow)], passed.Events);
    }

    [Theory]
    [InlineData("playing")]
    [InlineData("proposal waiting")]
    [InlineData("game over")]
    public void MayRejoin_WhenTheSeatPlaysOnOrMayNotYet_IsFalse(string situation)
    {
        // Arrange - Black gave up its turns, unless still playing
        GameState gaveUp = this.Regency(hasBlackRaja: true, Grid.CellAt(6, 2)) with { Resigned = [Black] };
        GameState state = situation switch
        {
            "playing" => gaveUp with { Resigned = [] },
            "proposal waiting" => gaveUp with { Proposal = new EndProposal(Black, true, Black, null) },
            _ => gaveUp with { Outcome = new GameOutcome([], "over") },
        };

        // Act
        (bool May, bool Before) mayRejoin = (this._rules.MayRejoin(state, Black), this._rules.MayRejoin(gaveUp, Black));

        // Assert
        Assert.Equal((false, true), mayRejoin);
    }

    [Fact]
    public void Apply_WhenASeatMayNotRejoin_ThrowsIllegalActionException()
    {
        // Arrange - Yellow never gave up its turns
        GameState state = this.Later();

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new RejoinAction(Yellow))
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenASeatMayNotProposeEndingTheGame_ThrowsIllegalActionException()
    {
        // Arrange - the dice are awaited
        GameState state = this.Later() with { AwaitedDice = new DiceRequest(2, 6) };
        ProposeEndAction propose = new(Yellow, Resigns: false);

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, propose)
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenARajaEntersAndLeavesAnEnemysThrone_TellsOfTheControlItGainsAndLoses()
    {
        // Arrange - Black's Raja beside Yellow's throne; every seat has its Raja
        GameState state = this.Commands(Grid.CellAt(1, 4), Grid.CellAt(6, 2));

        // Act
        Transition entered = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(1, 4), YellowThrone));
        Transition left = this._rules.Apply(
            entered.State with { DecidingSeat = Black },
            new MoveAction(Black, YellowThrone, Grid.CellAt(1, 4))
        );

        // Assert
        Assert.Equal(new CommandBegun(Black, Yellow, CommandBasis.EnemyThrone), entered.Events[^1]);
        Assert.Equal(new CommandEnded(Black, Yellow), left.Events[^1]);
    }

    [Fact]
    public void Apply_WhenARajaEntersAnAllysThroneWithNoEnemyRajaLeft_TellsOfTheControlAndTheEmpire()
    {
        // Arrange - Black's Raja beside Green's throne; Yellow and Red have no Raja
        GameState state = this.Commands(Grid.CellAt(4, 6), Grid.CellAt(6, 2), hasEnemyRajas: false);

        // Act
        Transition entered = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(4, 6), GreenThrone));
        Transition left = this._rules.Apply(
            entered.State with { DecidingSeat = Black },
            new MoveAction(Black, GreenThrone, Grid.CellAt(4, 6))
        );

        // Assert
        Assert.Equal(
            [new CommandBegun(Black, Green, CommandBasis.AllyThrone), new EmpireBegun([Black, Green])],
            entered.Events.TakeLast(2)
        );
        Assert.Equal([new CommandEnded(Black, Green), new EmpireEnded([Black, Green])], left.Events.TakeLast(2));
    }

    [Fact]
    public void Apply_WhenTakingTheLastEnemyRajaCreatesTheEmpire_TellsOfItOnlyOnce()
    {
        // Arrange - Green has no Raja, so Black commands its army; Black's Raja is beside Yellow's, the last
        GameState commands = this.Commands(Grid.CellAt(3, 3), greenRaja: null, hasEnemyRajas: false);
        GameState state = commands with
        {
            Position = commands.Position.Place(Grid.CellAt(4, 4), Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
        };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(4, 4)));

        // Assert
        Assert.Equal(
            (1, false),
            (transition.Events.OfType<EmpireCreated>().Count(), transition.Events.OfType<EmpireBegun>().Any())
        );
    }

    [Fact]
    public void Apply_WhenAnAllyLosesItsRaja_TellsThatTheOtherNowHasPartialControlOfItsArmy()
    {
        // Arrange - Yellow's Elephant may take Green's Raja
        GameState state = this.Commands(Grid.CellAt(3, 3), Grid.CellAt(6, 2)) with { DecidingSeat = Yellow };
        GameState armed = state with
        {
            Position = state.Position.Place(Grid.CellAt(6, 0), Piece(12, Chaturaji2027Pieces.Elephant, Yellow)),
            Dice = [3],
        };

        // Act
        Transition transition = this._rules.Apply(armed, new MoveAction(Yellow, Grid.CellAt(6, 0), Grid.CellAt(6, 2)));

        // Assert
        Assert.Contains(new CommandBegun(Black, Green, CommandBasis.LeaderlessAlly), transition.Events);
    }

    [Fact]
    public void Apply_WhenASeatVotesOnWaitingForAnother_StandsByItUntilChanged()
    {
        // Arrange - Black's turn
        GameState state = this.Commands(Grid.CellAt(3, 3), Grid.CellAt(6, 2));

        // Act
        Transition stopped = this._rules.Apply(state, new VoteAwayAction(Yellow, Black, Stops: true));
        Transition waits = this._rules.Apply(stopped.State, new VoteAwayAction(Yellow, Black, Stops: false));

        // Assert
        Assert.Equal([Yellow], stopped.State.Absence.StoppingFor(Black));
        Assert.Equal(
            (new AwayVoted(Yellow, Black, true), Black),
            (stopped.Events.Single(), stopped.State.DecidingSeat)
        );
        Assert.Equal(
            (true, new AwayVoted(Yellow, Black, false)),
            (waits.State.Absence.StopVotes.IsEmpty, waits.Events.Single())
        );
    }

    [Theory]
    [InlineData("game over")]
    [InlineData("oneself")]
    [InlineData("voter has no pieces")]
    [InlineData("voter gave up")]
    [InlineData("absent has no pieces")]
    public void MayVoteAway_WhenEitherSeatIsOutOrTheyAreOne_IsFalse(string why)
    {
        // Arrange - in the ransoms' position, Red has no pieces
        GameState playing = this.Ransoms();
        (GameState state, SeatId voter, SeatId absent) = why switch
        {
            "game over" => (playing with { Outcome = new GameOutcome([], "over") }, Yellow, Black),
            "oneself" => (playing, Black, Black),
            "voter has no pieces" => (playing, Red, Black),
            "voter gave up" => (playing with { Resigned = [Yellow] }, Yellow, Black),
            _ => (playing, Yellow, Red),
        };

        // Act
        (bool May, bool Otherwise) mayVote =
            (this._rules.MayVoteAway(state, voter, absent), this._rules.MayVoteAway(playing, Yellow, Black));

        // Assert
        Assert.Equal((false, true), mayVote);
    }

    [Fact]
    public void Apply_WhenASeatMayNotVoteOnWaiting_ThrowsIllegalActionException()
    {
        // Arrange
        GameState state = this.Later();

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new VoteAwayAction(Black, Black, Stops: true))
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenASeatAwayPasses_CountsTheTurnItMissed()
    {
        // Arrange - Black's turn; it had said it was back
        GameState state = this.Commands(Grid.CellAt(3, 3), Grid.CellAt(6, 2)) with
        {
            Absence = Absence.None with { Returning = [Black] },
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAwayAction(Black));

        // Assert
        Assert.Equal(
            (Yellow, 1, false, false),
            (
                transition.State.DecidingSeat,
                transition.State.Absence.MissedBy(Black),
                transition.State.Absence.Returning.Contains(Black),
                transition.State.Resigned.Contains(Black)
            )
        );
        Assert.Equal([new TurnPassedAway(Black, 1, 3), new TurnPassed(Black, Yellow)], transition.Events);
    }

    [Fact]
    public void Apply_WhenASeatAwayMissesItsLastTurnAllowed_GivesUpItsTurns()
    {
        // Arrange - Black missed two turns already
        GameState state = this.Commands(Grid.CellAt(3, 3), Grid.CellAt(6, 2)) with
        {
            Absence = Absence.None with { Missed = Absence.None.Missed.Add(Black, 2) },
        };

        // Act
        Transition transition = this._rules.Apply(state, new PassAwayAction(Black));

        // Assert
        Assert.Equal(
            (true, true, new TurnPassedAway(Black, 3, 3)),
            (
                transition.State.Resigned.Contains(Black),
                this._rules.MayRejoin(transition.State, Black),
                transition.Events[0]
            )
        );
    }

    [Theory]
    [InlineData("not deciding")]
    [InlineData("game over")]
    [InlineData("dice awaited")]
    [InlineData("timed")]
    public void Apply_WhenASeatMayNotPassAsAway_ThrowsIllegalActionException(string why)
    {
        // Arrange
        GameState state = why switch
        {
            "game over" => this.Later() with { DecidingSeat = null, Outcome = new GameOutcome([], "over") },
            "dice awaited" => this.Later() with { AwaitedDice = new DiceRequest(2, 6) },
            "timed" => this.Ransoms() with { IsRansomWindow = true },
            _ => this.Later(),
        };
        SeatId seat = why == "not deciding" ? Yellow : Black;

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, new PassAwayAction(seat))
        );

        // Assert
        Assert.Contains("is not allowed now", exception.Message);
    }

    [Fact]
    public void Apply_WhenASeatThatWasAwayPlays_ForgetsItsAbsence()
    {
        // Arrange - Black missed a turn, and Yellow would stop waiting for it
        GameState state = this.Later() with
        {
            Absence = new Absence([new KeyValuePair<SeatId, int>(Black, 1)], [new AwayVote(Yellow, Black)], []),
        };

        // Act
        Transition transition = this._rules.Apply(state, new RollAction(Black));

        // Assert
        Assert.Equal((0, true), (transition.State.Absence.MissedBy(Black), transition.State.Absence.StopVotes.IsEmpty));
    }

    [Fact]
    public void Apply_WhenASeatThatMissedTurnsSaysItIsBack_IsWaitedForAgainButStillCountsThem()
    {
        // Arrange - Black missed a turn; Yellow decides
        GameState state = this.Later() with
        {
            DecidingSeat = Yellow,
            Absence = Absence.None with { Missed = Absence.None.Missed.Add(Black, 1) },
        };

        // Act
        Transition transition = this._rules.Apply(state, new RejoinAction(Black));

        // Assert
        Assert.Equal(
            (true, 1, false, new Rejoined(Black)),
            (
                transition.State.Absence.Returning.Contains(Black),
                transition.State.Absence.MissedBy(Black),
                this._rules.MayRejoin(transition.State, Black),
                transition.Events.Single()
            )
        );
    }

    [Fact]
    public void Apply_WhenAnOfferIsDeclined_KeepsThePrisonersAndTheOffererDecidesAgain()
    {
        // Arrange
        GameState state = this.Ransoms() with
        {
            DecidingSeat = Yellow,
            Offer = new RansomOffer(Black, Yellow, [new PieceId(12)], []),
        };

        // Act
        Transition transition = this._rules.Apply(state, new DeclineAction(Yellow));

        // Assert
        Assert.Equal(
            (Black, null, 3, new RansomDeclined(state.Offer)),
            (
                transition.State.DecidingSeat,
                transition.State.Offer,
                transition.State.Prisoners.Count,
                transition.Events.Single()
            )
        );
    }

    [Fact]
    public void Apply_WhenATurnEndsWithPrisonersHeld_OpensTheRansomWindowAgainstTheClock()
    {
        // Arrange - Black's last die moves its Raja, which ends its turn
        GameState state = this.Ransoms() with { Dice = [5] };

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(3, 3), Grid.CellAt(3, 4)));

        // Assert
        Assert.Equal(
            (Black, true, 0, 2, new RansomWindowOpened(Black)),
            (
                transition.State.DecidingSeat,
                transition.State.IsRansomWindow,
                transition.State.IdlePasses,
                transition.State.TurnNumber,
                transition.Events[^1]
            )
        );
        Assert.Equal([new PassAction(Black)], this._rules.LegalActions(transition.State));
        Assert.Equal(
            (new DecisionTimer("ransom-window", new PassAction(Black)), true),
            (this._rules.TimerOf(transition.State), this._rules.MayOffer(transition.State))
        );
    }

    [Fact]
    public void Apply_WhenDoneOffering_PassesTheTurnKeepingTheIdlePasses()
    {
        // Arrange
        GameState state = this.Ransoms() with { IsRansomWindow = true, IdlePasses = 1 };

        // Act
        Transition transition = this._rules.Apply(state, new PassAction(Black));

        // Assert
        Assert.Equal(
            (Yellow, false, 1, new TurnPassed(Black, Yellow)),
            (
                transition.State.DecidingSeat,
                transition.State.IsRansomWindow,
                transition.State.IdlePasses,
                transition.Events.Single()
            )
        );
    }

    [Fact]
    public void TimerOf_WhenNoOfferWaitsAndTheTurnGoesOn_IsNull()
    {
        // Arrange
        GameState state = this.Ransoms();

        // Act
        DecisionTimer? timer = this._rules.TimerOf(state);

        // Assert
        Assert.Null(timer);
    }

    [Fact]
    public void Parse_WhenGivenADescribedMove_ReturnsTheMove()
    {
        // Arrange
        MoveAction move = new(Black, Grid.CellAt(0, 1), Grid.CellAt(0, 2));

        // Act
        GameAction parsed = this._rules.Parse(move.Describe());

        // Assert
        Assert.Equal(move, parsed);
    }

    [Fact]
    public void NameOf_WhenGivenAPieceKind_ReturnsItsChaturajiName()
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOf(Chaturaji2027Pieces.Elephant);

        // Assert
        Assert.Equal("Elephant", name);
    }

    [Fact]
    public void NameOf_WhenGivenASeat_ReturnsItsColour()
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOf(Green);

        // Assert
        Assert.Equal("Green", name);
    }

    [Theory]
    [InlineData(1, "Boat")]
    [InlineData(2, "Horse")]
    [InlineData(3, "Elephant")]
    [InlineData(6, "Pawn or Raja")]
    [InlineData(7, "Nothing")]
    public void NameOfDie_WhenGivenAValue_NamesTheKindsItLetsMove(int value, string expected)
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOfDie(value);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void Thrones_WhenRead_AreWhereEachRajaStarts()
    {
        // Arrange
        GameState state = this._rules.CreateInitialState();

        // Act
        ImmutableDictionary<SeatId, CellId> thrones = this._rules.Thrones;

        // Assert
        Assert.Equal(
            "raja 0|raja 1|raja 2|raja 3",
            string.Join(
                "|",
                state.Seats.Select(seat => state.Position.PieceAt(thrones[seat.Id]))
                    .Select(raja => $"{raja?.Kind.Value} {raja?.Owner.Value}")
            )
        );
    }

    private static Position Placed(Position position, (CellId Cell, Piece Piece) placed) =>
        position.Place(placed.Cell, placed.Piece);

    private static Piece Piece(int id, PieceKindId kind, SeatId owner) => new(new PieceId(id), kind, owner);

    private static PieceKindId KindOf(Chaturaji2027PiecesKind kind) =>
        kind == Chaturaji2027PiecesKind.Raja ? Chaturaji2027Pieces.Raja : Chaturaji2027Pieces.Pawn;

    /// <summary>Each seat in turn rolls one value, in the order the roll-off asks for them.</summary>
    private Transition RollOff(GameState state, params int[] values) =>
        values.Aggregate(
            new Transition(state, []),
            (rolled, value) =>
            {
                SeatId roller = rolled.State.DecidingSeat!.Value;
                Transition next = this._rules.Apply(rolled.State, new DiceAction(roller, [value]));
                return next with { Events = rolled.Events.AddRange(next.Events) };
            }
        );

    /// <summary>The starting position on the second turn, past the roll-off.</summary>
    private GameState Later() =>
        this._rules.CreateInitialState() with { TurnNumber = 2, RollOff = [], AwaitedDice = null };

    /// <summary>Black's pawn and Green's, back to back in mid-board, with a Yellow pawn, on a later turn.</summary>
    private GameState BackToBack() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(3, 4), Piece(2, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(0, 7), Piece(3, Chaturaji2027Pieces.Pawn, Yellow))
        );

    /// <summary>Black's pawn in mid-board and its boat in the corner, with a Yellow pawn, on a later turn.</summary>
    private GameState PawnAndBoat() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(0, 0), Piece(2, Chaturaji2027Pieces.Boat, Black)),
            (Grid.CellAt(0, 7), Piece(3, Chaturaji2027Pieces.Pawn, Yellow))
        );

    /// <summary>
    /// Yellow's turn, with dice 5 and 2 rolled, and Black's Raja on Yellow's throne: Yellow has a pawn
    /// facing right and a horse; Green has a pawn to take the next turn.
    /// </summary>
    private GameState SharedTurn() =>
        this.StateWith(
            (YellowThrone, Piece(1, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(1, 1), Piece(3, Chaturaji2027Pieces.Horse, Yellow)),
            (Grid.CellAt(4, 6), Piece(4, Chaturaji2027Pieces.Pawn, Green))
        ) with
        {
            DecidingSeat = Yellow,
            Dice = [5, 2],
        };

    /// <summary>
    /// Black's turn, before rolling: Black holds Yellow's pawn 12 (which starts on b8, where Green's
    /// pawn stands) and Red's pawn 14; Yellow holds Black's horse 1, which starts on b1.
    /// </summary>
    private GameState Ransoms() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(2, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(1, 7), Piece(3, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(5, 5), Piece(4, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            Prisoners =
            [
                new Prisoner(Piece(12, Chaturaji2027Pieces.Pawn, Yellow), Black),
                new Prisoner(Piece(14, Chaturaji2027Pieces.Pawn, Red), Black),
                new Prisoner(Piece(1, Chaturaji2027Pieces.Horse, Black), Yellow),
            ],
        };

    /// <summary>
    /// Captured pieces to promote into: Black's Horse and Pawn, Green's Elephant (twice held by Red, once
    /// by Black), Yellow's Boat and Pawn.
    /// </summary>
    private static GameState PromotionPool(GameState state) =>
        state with
        {
            Prisoners =
            [
                new Prisoner(Piece(10, Chaturaji2027Pieces.Horse, Black), Yellow),
                new Prisoner(Piece(11, Chaturaji2027Pieces.Elephant, Green), Red),
                new Prisoner(Piece(12, Chaturaji2027Pieces.Pawn, Black), Yellow),
                new Prisoner(Piece(13, Chaturaji2027Pieces.Boat, Yellow), Black),
                new Prisoner(Piece(15, Chaturaji2027Pieces.Elephant, Green), Black),
                new Prisoner(Piece(16, Chaturaji2027Pieces.Pawn, Yellow), Green),
                new Prisoner(Piece(17, Chaturaji2027Pieces.Elephant, Green), Red),
            ],
        };

    /// <summary>
    /// Black's turn, before rolling: its pawn is a step from the top edge; Red holds Green's Elephant
    /// 11, and Yellow has a Raja to play on.
    /// </summary>
    private GameState FarEdgeAhead() =>
        this.StateWith(
            (Grid.CellAt(2, 6), Piece(1, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(5, 5), Piece(3, Chaturaji2027Pieces.Raja, Yellow))
        ) with
        {
            Prisoners = [new Prisoner(Piece(11, Chaturaji2027Pieces.Elephant, Green), Red)],
        };

    /// <summary>Yellow answering Black's offer of Yellow's pawn 12 back, for Black's horse 1.</summary>
    /// <summary>
    /// Black's turn, with a Pawn in mid-board: Yellow holds Black's Raja 3 (which starts on d1, Black's
    /// throne) and Black's Boat 0 (which starts on a1); Yellow has a Pawn to take the next turn.
    /// </summary>
    private GameState Escapes() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(4, Chaturaji2027Pieces.Pawn, Black)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow))
        ) with
        {
            Prisoners =
            [
                new Prisoner(Piece(3, Chaturaji2027Pieces.Raja, Black), Yellow),
                new Prisoner(Piece(0, Chaturaji2027Pieces.Boat, Black), Yellow),
            ],
        };

    /// <summary>
    /// Black's Elephant on a1, with Black's Raja on <paramref name="blackRaja"/>; Yellow's Raja on the a-file, the
    /// last enemy Raja, beside a Pawn each of Yellow's and Red's; and Green's Pawn, and its Raja if
    /// <paramref name="hasGreenRaja"/>.
    /// </summary>
    private GameState Empire(CellId blackRaja, bool hasGreenRaja, int yellowRajaRow = 5)
    {
        GameState state = this.StateWith(
            (Grid.CellAt(0, 0), Piece(2, Chaturaji2027Pieces.Elephant, Black)),
            (blackRaja, Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(0, yellowRajaRow), Piece(11, Chaturaji2027Pieces.Raja, Yellow)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(6, 6), Piece(29, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green))
        );
        return hasGreenRaja
            ? state with
            {
                Position = state.Position.Place(Grid.CellAt(6, 2), Piece(19, Chaturaji2027Pieces.Raja, Green)),
            }
            : state;
    }

    /// <summary>
    /// Black's turn, its Raja on <paramref name="blackRaja"/>, nothing near it; a Pawn each of Yellow's, Green's
    /// and Red's far away.
    /// </summary>
    private GameState Farm(CellId blackRaja) =>
        this.StateWith(
            (blackRaja, Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(7, 7), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(6, 7), Piece(21, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(7, 0), Piece(29, Chaturaji2027Pieces.Pawn, Red))
        );

    /// <summary>
    /// Green's turn, with no Raja of its own but a Pawn; Black's Raja on Yellow's throne; Yellow's Pawn; and, if
    /// <paramref name="hasRetiredRed"/>, Red's Raja, Red retired, else nothing of Red's.
    /// </summary>
    private GameState EveryPiece(bool hasRetiredRed)
    {
        GameState state = this.StateWith(
            (YellowThrone, Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow))
        ) with
        {
            DecidingSeat = Green,
        };
        return hasRetiredRed
            ? state with
            {
                Position = state.Position.Place(Grid.CellAt(7, 7), Piece(27, Chaturaji2027Pieces.Raja, Red)),
                Retired = [Red],
            }
            : state;
    }

    /// <summary>
    /// Black's turn, with its Elephant on a1, and its Raja on d4 if <paramref name="hasBlackRaja"/>; Green's
    /// Pawn, and Green's Raja on <paramref name="greenRaja"/> if any; a Pawn each of Yellow's and Red's.
    /// </summary>
    private GameState Regency(bool hasBlackRaja, CellId? greenRaja)
    {
        GameState state = this.StateWith(
            (Grid.CellAt(0, 0), Piece(2, Chaturaji2027Pieces.Elephant, Black)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(6, 6), Piece(29, Chaturaji2027Pieces.Pawn, Red)),
            (Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green))
        );
        Position black = hasBlackRaja
            ? state.Position.Place(Grid.CellAt(3, 3), Piece(3, Chaturaji2027Pieces.Raja, Black))
            : state.Position;
        return state with
        {
            Position = greenRaja is { } cell ? black.Place(cell, Piece(19, Chaturaji2027Pieces.Raja, Green)) : black,
        };
    }

    /// <summary>
    /// Black's turn, before rolling, with Black's Raja on <paramref name="blackRaja"/>, and Green's Pawn, and
    /// its Raja on <paramref name="greenRaja"/> if any; Yellow and Red each have a Pawn, and a Raja if
    /// <paramref name="hasEnemyRajas"/>.
    /// </summary>
    private GameState Commands(CellId blackRaja, CellId? greenRaja, bool hasEnemyRajas = true)
    {
        GameState state = this.StateWith(
            (blackRaja, Piece(3, Chaturaji2027Pieces.Raja, Black)),
            (Grid.CellAt(2, 6), Piece(21, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(5, 5), Piece(13, Chaturaji2027Pieces.Pawn, Yellow)),
            (Grid.CellAt(6, 6), Piece(29, Chaturaji2027Pieces.Pawn, Red))
        );
        Position green = greenRaja is { } cell
            ? state.Position.Place(cell, Piece(19, Chaturaji2027Pieces.Raja, Green))
            : state.Position;
        Position all = hasEnemyRajas
            ? green
                .Place(Grid.CellAt(1, 1), Piece(11, Chaturaji2027Pieces.Raja, Yellow))
                .Place(Grid.CellAt(7, 7), Piece(27, Chaturaji2027Pieces.Raja, Red))
            : green;
        return state with { Position = all };
    }

    /// <summary>
    /// Black's turn, on turn 10, <paramref name="holder"/> holding <paramref name="prisoner"/>; Green's Pawn and
    /// Yellow's in mid-board, on no one's starting square.
    /// </summary>
    private GameState Releasing(Piece prisoner, SeatId holder) =>
        this.StateWith(
            (Grid.CellAt(3, 4), Piece(21, Chaturaji2027Pieces.Pawn, Green)),
            (Grid.CellAt(4, 3), Piece(14, Chaturaji2027Pieces.Pawn, Yellow))
        ) with
        {
            TurnNumber = 10,
            Prisoners = [new Prisoner(prisoner, holder)],
        };

    private GameState AcceptingRansom() =>
        this.Ransoms() with
        {
            DecidingSeat = Yellow,
            Offer = new RansomOffer(Black, Yellow, [new PieceId(12)], [new PieceId(1)]),
        };

    private GameState StateWith(params (CellId Cell, Piece Piece)[] placements) =>
        this.Later() with
        {
            Position = placements.Aggregate(Position.Empty, Placed),
        };
}

/// <summary>Kinds a theory can name: attributes take no piece kind ids.</summary>
public enum Chaturaji2027PiecesKind
{
    Raja,
    Pawn,
}
