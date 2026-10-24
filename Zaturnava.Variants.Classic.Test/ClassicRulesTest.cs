using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Chance;
using Net.NowhereAtAll.Zaturnava.Engine.Events;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;
using Net.NowhereAtAll.Zaturnava.Topology.SquareGrid;

namespace Net.NowhereAtAll.Zaturnava.Variants.Classic.Test;

public sealed class ClassicRulesTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(ClassicSetup.Side, ClassicSetup.Side);

    /// <summary>Yellow's throne, the square its Raja starts on.</summary>
    private static readonly CellId YellowThrone = Grid.CellAt(0, 4);

    /// <summary>Red's throne.</summary>
    private static readonly CellId RedThrone = Grid.CellAt(7, 3);

    private readonly ClassicRules _rules = new();

    [Fact]
    public void Identity_WhenRead_IsTheClassicSquareGridRuleset()
    {
        // Arrange - nothing beyond the rules

        // Act
        (string Id, string Name, CellLayout Corner) identity = (
            this._rules.Id,
            this._rules.Name,
            this._rules.Topology.LayoutOf(new CellId(63))
        );

        // Assert
        Assert.Equal(("classic", "Zaturnava (classic)", new CellLayout(7, 7, "square")), identity);
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
        Assert.Equal([new RollOffWon(Green, 4), new TurnPassed(Green, Red)], transition.Events.TakeLast(2));
    }

    [Fact]
    public void Apply_WhenRollingOffWithFourSidedDice_WaitsOnAFourSidedDie()
    {
        // Arrange
        ClassicRules rules = new(DiceProfile.FourSided, ClassicRules.DefaultIdleRounds);

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
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Pawn, Black)),
            (Grid.CellAt(2, 4), Piece(2, ClassicPieces.Pawn, Yellow)),
            (Grid.CellAt(4, 4), Piece(3, ClassicPieces.Pawn, Green))
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
    public void LegalActions_WhenARajaSitsOnAnEnemyThrone_ItsSeatAlsoMovesThatArmyTheWayItFaces()
    {
        // Arrange - Red's pawns face left; one of Red's own pieces and one of Yellow's are ahead of it
        GameState state = this.StateWith(
            (RedThrone, Piece(1, ClassicPieces.Raja, Black)),
            (Grid.CellAt(6, 1), Piece(2, ClassicPieces.Pawn, Red)),
            (Grid.CellAt(5, 2), Piece(3, ClassicPieces.Elephant, Red)),
            (Grid.CellAt(5, 0), Piece(4, ClassicPieces.Pawn, Yellow))
        );

        // Act
        ImmutableList<GameAction> actions = this._rules.LegalActions(state);

        // Assert
        Assert.Equal(
            [Grid.CellAt(5, 0), Grid.CellAt(5, 1)],
            actions
                .OfType<MoveAction>()
                .Where(move => move.From == Grid.CellAt(6, 1))
                .Select(move => move.To)
                .OrderBy(cell => cell.Value)
        );
    }

    [Theory]
    [InlineData(ClassicPiecesKind.Raja, true)]
    [InlineData(ClassicPiecesKind.Pawn, false)]
    public void LegalActions_WhenAnAllyHasNoRaja_ASeatWithOneMovesTheAllysArmy(ClassicPiecesKind own, bool expected)
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, KindOf(own), Black)),
            (Grid.CellAt(4, 6), Piece(2, ClassicPieces.Pawn, Green)),
            (Grid.CellAt(0, 7), Piece(3, ClassicPieces.Pawn, Yellow))
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
        Assert.Contains("is not legal now", exception.Message);
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
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Pawn, Black)),
            (Grid.CellAt(4, 6), Piece(2, ClassicPieces.Pawn, Green)),
            (Grid.CellAt(6, 1), Piece(3, ClassicPieces.Pawn, Red))
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
        Piece elephant = Piece(1, ClassicPieces.Elephant, Black);
        Piece horse = Piece(2, ClassicPieces.Horse, Yellow);
        GameState state = this.StateWith(
            (Grid.CellAt(0, 0), elephant),
            (Grid.CellAt(0, 5), horse),
            (Grid.CellAt(6, 1), Piece(3, ClassicPieces.Pawn, Red))
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
        Piece raja = Piece(1, ClassicPieces.Raja, Black);
        GameState state = this.StateWith(
            (Grid.CellAt(1, 3), raja),
            (Grid.CellAt(6, 1), Piece(2, ClassicPieces.Pawn, Red))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(1, 3), YellowThrone));

        // Assert
        Assert.Equal((1, new ThroneEntered(raja, Yellow, 1)), (transition.State.Scores[Black], transition.Events[1]));
    }

    [Theory]
    [InlineData(ClassicPiecesKind.Raja, 4, 6)]
    [InlineData(ClassicPiecesKind.Pawn, 0, 3)]
    public void Apply_WhenAnythingButARajaEntersAThroneOrARajaEntersAnAllysThrone_ScoresNoBonus(
        ClassicPiecesKind kind,
        int fromColumn,
        int fromRow
    )
    {
        // Arrange - Green's throne is at e8; Yellow's at a5
        GameState state = this.StateWith(
            (Grid.CellAt(fromColumn, fromRow), Piece(1, KindOf(kind), Black)),
            (Grid.CellAt(6, 1), Piece(2, ClassicPieces.Pawn, Red))
        );
        CellId target = kind == ClassicPiecesKind.Raja ? Grid.CellAt(4, 7) : YellowThrone;
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
        Piece raja = Piece(2, ClassicPieces.Raja, Red);
        GameState state = this.StateWith(
            (Grid.CellAt(7, 0), Piece(1, ClassicPieces.Elephant, Black)),
            (RedThrone, raja),
            (Grid.CellAt(0, 7), Piece(3, ClassicPieces.Pawn, Yellow))
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
            (Grid.CellAt(0, 0), Piece(1, ClassicPieces.Elephant, Black)),
            (Grid.CellAt(0, 5), Piece(2, ClassicPieces.Horse, Yellow))
        );

        // Act
        Transition transition = this._rules.Apply(state, new MoveAction(Black, Grid.CellAt(0, 0), Grid.CellAt(0, 5)));

        // Assert
        GameOutcome won = new([Black, Green], "only one team has pieces left");
        Assert.Equal((null, won), (transition.State.DecidingSeat, transition.State.Outcome));
        Assert.Equal(new GameEnded(won), transition.Events[^1]);
        Assert.Empty(this._rules.LegalActions(transition.State));
    }

    [Fact]
    public void Apply_WhenOnlyRajasAreLeft_EndsTheGameInADraw()
    {
        // Arrange
        GameState state = this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Raja, Black)),
            (Grid.CellAt(4, 4), Piece(2, ClassicPieces.Pawn, Yellow)),
            (Grid.CellAt(7, 0), Piece(3, ClassicPieces.Raja, Red))
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
        Assert.Equal([new DiceRolled(Black, [3, 4]), new TurnPassed(Black, Yellow)], transition.Events);
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
        Assert.Contains("is not legal now", exception.Message);
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
        Assert.Contains("is not legal now", exception.Message);
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
        Assert.Contains("is not legal now", exception.Message);
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
            (ClassicPieces.Pawn, Yellow),
            (twice.State.Position.PieceAt(Grid.CellAt(3, 5))?.Kind, twice.State.DecidingSeat)
        );
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
    public void Apply_WhenTheOccupierPassesForTheSeat_PassesThatSeatsTurn()
    {
        // Arrange
        GameState shared = this._rules.Apply(this.SharedTurn(), new PassAction(Yellow)).State;

        // Act
        Transition transition = this._rules.Apply(shared, new PassAction(Black));

        // Assert
        Assert.Equal([new TurnPassed(Yellow, Green)], transition.Events);
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
            [new TurnShared(Yellow, Black), new TurnPassed(Yellow, Green)],
            transition.Events.TakeLast(2)
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
            (YellowThrone, Piece(1, ClassicPieces.Raja, Black)),
            (Grid.CellAt(3, 3), Piece(2, ClassicPieces.Pawn, Yellow)),
            (Grid.CellAt(4, 3), Piece(3, ClassicPieces.Pawn, Red)),
            (Grid.CellAt(4, 4), Piece(4, ClassicPieces.Pawn, Red))
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
    public void NameOf_WhenGivenAPieceKind_ReturnsItsTraditionalName()
    {
        // Arrange - nothing beyond the rules

        // Act
        string name = this._rules.NameOf(ClassicPieces.Elephant);

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

    private static PieceKindId KindOf(ClassicPiecesKind kind) =>
        kind == ClassicPiecesKind.Raja ? ClassicPieces.Raja : ClassicPieces.Pawn;

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

    /// <summary>Black's pawn in mid-board and its boat in the corner, with a Yellow pawn, on a later turn.</summary>
    private GameState PawnAndBoat() =>
        this.StateWith(
            (Grid.CellAt(3, 3), Piece(1, ClassicPieces.Pawn, Black)),
            (Grid.CellAt(0, 0), Piece(2, ClassicPieces.Boat, Black)),
            (Grid.CellAt(0, 7), Piece(3, ClassicPieces.Pawn, Yellow))
        );

    /// <summary>
    /// Yellow's turn, with dice 5 and 2 rolled, and Black's Raja on Yellow's throne: Yellow has a pawn
    /// facing right and a horse; Green has a pawn to take the next turn.
    /// </summary>
    private GameState SharedTurn() =>
        this.StateWith(
            (YellowThrone, Piece(1, ClassicPieces.Raja, Black)),
            (Grid.CellAt(3, 3), Piece(2, ClassicPieces.Pawn, Yellow)),
            (Grid.CellAt(1, 1), Piece(3, ClassicPieces.Horse, Yellow)),
            (Grid.CellAt(4, 6), Piece(4, ClassicPieces.Pawn, Green))
        ) with
        {
            DecidingSeat = Yellow,
            Dice = [5, 2],
        };

    private GameState StateWith(params (CellId Cell, Piece Piece)[] placements) =>
        this.Later() with
        {
            Position = placements.Aggregate(Position.Empty, Placed),
        };
}

/// <summary>Kinds a theory can name: attributes take no piece kind ids.</summary>
public enum ClassicPiecesKind
{
    Raja,
    Pawn,
}
