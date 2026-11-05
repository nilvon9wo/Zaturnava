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

/// <summary>
/// The leader face-off, given here to Chaturaji 2027's Raja: Black's Raja on d1 faces Yellow's on d6, nothing between
/// them, while Yellow decides; Green's Raja on a8 and Red's on h8 face no enemy of Black's, and Black's Elephant on a1
/// and Red's on h4 are there to play on with. Yellow's pieces are Black's enemies, Green's its allies.
/// </summary>
public sealed class FaceOffTest
{
    private static readonly SeatId Black = new(0);
    private static readonly SeatId Yellow = new(1);
    private static readonly SeatId Green = new(2);
    private static readonly SeatId Red = new(3);
    private static readonly SquareGridTopology Grid = new(Chaturaji2027Setup.Side, Chaturaji2027Setup.Side);

    private static readonly Piece BlackRaja = new(new PieceId(1), Chaturaji2027Pieces.Raja, Black);
    private static readonly Piece YellowRaja = new(new PieceId(2), Chaturaji2027Pieces.Raja, Yellow);
    private static readonly Piece GreenRaja = new(new PieceId(3), Chaturaji2027Pieces.Raja, Green);
    private static readonly Piece RedRaja = new(new PieceId(4), Chaturaji2027Pieces.Raja, Red);
    private static readonly Piece YellowElephant = new(new PieceId(5), Chaturaji2027Pieces.Elephant, Yellow);
    private static readonly Piece BlackElephant = new(new PieceId(6), Chaturaji2027Pieces.Elephant, Black);
    private static readonly Piece RedElephant = new(new PieceId(7), Chaturaji2027Pieces.Elephant, Red);

    private static readonly FaceOffAction Called = new(Black, Cell(3, 0), Cell(3, 5));

    private readonly Chaturaji2027Rules _rules = new(
        DiceProfile.WeightedSixSided,
        Chaturaji2027Rules.DefaultIdleRounds,
        PawnPromotion.OtherArmies,
        variant: Chaturaji2027Variant.Standard with { FaceOffCallers = [Chaturaji2027Pieces.Raja] }
    );

    [Fact]
    public void ChancesOf_WhenTheCallerFacesAnEnemyLeaderAlongAnOpenLine_IsTheFaceOffWhoeverDecides()
    {
        // Arrange
        GameState state = YellowDeciding();

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(state, Black);

        // Assert
        Assert.Equal([Called], chances);
    }

    [Fact]
    public void ChancesOf_WhenTheDecidingSeatActsForAnother_IsStillTheFaceOffOfTheCallersOwnArmy()
    {
        // Arrange - Yellow decides for Red
        GameState state = YellowDeciding() with { ActingFor = Red };

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(state, Black);

        // Assert
        Assert.Equal([Called], chances);
    }

    [Fact]
    public void ChancesOf_WhenTheRulesGiveNoCallers_IsNone()
    {
        // Arrange
        GameState state = YellowDeciding();

        // Act
        ImmutableList<GameAction> chances = new Chaturaji2027Rules().ChancesOf(state, Black);

        // Assert
        Assert.Empty(chances);
    }

    public static TheoryData<string, Func<GameState, GameState>> NoFaceOffs =>
        new()
        {
            { "a piece between", state => Placed(state, Cell(3, 3), YellowElephant) },
            { "no leader faced", state => Placed(state, Cell(3, 5), YellowElephant) },
            { "an ally's leader faced", state => Placed(state, Cell(3, 5), GreenRaja) },
            { "the faced leader's seat retired", state => state with { Retired = [Yellow] } },
            { "the caller's seat retired", state => state with { Retired = [Black] } },
            { "the game over", state => state with { Outcome = new GameOutcome([Black], "won", "test") } },
            { "dice awaited", state => state with { AwaitedDice = new DiceRequest(1, 6) } },
            { "a promotion due", state => state with { PromotionDue = Cell(0, 7) } },
            { "prisoners going home", state => state with { Returning = new ReturnsDue([], Yellow) } },
            { "an offer waiting", state => state with { Offer = new RansomOffer(Yellow, Red, [], []) } },
            { "an end proposed", state => state with { Proposal = new EndProposal(Red, false, Yellow, null) } },
            { "the roll for who goes first", state => state with { RollOff = [Black, Yellow] } },
        };

    [Theory]
    [MemberData(nameof(NoFaceOffs), DisableDiscoveryEnumeration = true)]
    public void ChancesOf_WhenNoEnemyLeaderIsFacedOrTheGameWaitsOnSomethingElse_IsNone(
        string why,
        Func<GameState, GameState> changed
    )
    {
        // Arrange
        GameState state = changed(YellowDeciding());

        // Act
        ImmutableList<GameAction> chances = this._rules.ChancesOf(state, Black);

        // Assert
        Assert.Empty(chances);
        Assert.NotEmpty(why);
    }

    [Fact]
    public void Apply_WhenAFaceOffIsCalledOutOfTurn_TakesBothLeadersEachByTheOthersSide()
    {
        // Arrange
        GameState state = YellowDeciding();

        // Act
        Transition transition = this._rules.Apply(state, Called);

        // Assert
        GameState after = transition.State;
        Assert.Equal(
            (null, null, Yellow),
            (after.Position.PieceAt(Cell(3, 0)), after.Position.PieceAt(Cell(3, 5)), after.DecidingSeat)
        );
        Assert.Equal(new LeadersFacedOff(BlackRaja, YellowRaja, Black), transition.Events[0]);
        ImmutableList<(Piece, SeatId)> captures =
        [
            .. transition.Events.OfType<PieceCaptured>().Select(captured => (captured.Piece, captured.CapturedBy)),
        ];
        Assert.Contains((YellowRaja, Black), captures);
        Assert.Contains((BlackRaja, Yellow), captures);
    }

    [Fact]
    public void Apply_WhenAFaceOffTakesTheLeadersPassing_LeavesOnlyOtherPassingsToTakeInPassing()
    {
        // Arrange - Yellow's Raja and Red's both came two cells, Yellow's onto d6, Red's onto h8
        Passage yellow = new(YellowRaja, Cell(3, 7), [Cell(3, 6)], Cell(3, 5));
        Passage red = new(RedRaja, Cell(5, 7), [Cell(6, 7)], Cell(7, 7));
        GameState state = YellowDeciding() with { Passages = [yellow, red] };

        // Act
        GameState after = this._rules.Apply(state, Called).State;

        // Assert
        Assert.Equal([red], after.Passages);
    }

    [Fact]
    public void Apply_WhenAFaceOffTakesTheLastLeaders_EndsTheGame()
    {
        // Arrange - the two Rajas are all the board holds
        GameState state = YellowDeciding() with
        {
            Position = Position.Empty.Place(Cell(3, 0), BlackRaja).Place(Cell(3, 5), YellowRaja),
        };

        // Act
        GameState after = this._rules.Apply(state, Called).State;

        // Assert
        Assert.NotNull(after.Outcome);
    }

    [Fact]
    public void Apply_WhenTheFaceOffIsNotAChanceTheSeatHas_Throws()
    {
        // Arrange - Yellow's Raja has moved away, to c6
        GameState start = YellowDeciding();
        GameState state = start with { Position = start.Position.Relocate(Cell(3, 5), Cell(2, 5)) };

        // Act
        IllegalActionException exception = Assert.Throws<IllegalActionException>(
            () => this._rules.Apply(state, Called)
        );

        // Assert
        Assert.Equal("That face-off is not allowed now.", exception.Message);
    }

    private static CellId Cell(int column, int row) => Grid.CellAt(column, row);

    private static GameState Placed(GameState state, CellId cell, Piece piece) =>
        state with { Position = state.Position.Place(cell, piece) };

    /// <summary>Yellow to decide, with all four Rajas placed, and no dice rolled.</summary>
    private static GameState YellowDeciding() =>
        new Chaturaji2027Rules().CreateInitialState() with
        {
            TurnNumber = 2,
            RollOff = [],
            AwaitedDice = null,
            DecidingSeat = Yellow,
            Dice = [],
            Position = Position.Empty
                .Place(Cell(3, 0), BlackRaja)
                .Place(Cell(3, 5), YellowRaja)
                .Place(Cell(0, 7), GreenRaja)
                .Place(Cell(7, 7), RedRaja)
                .Place(Cell(0, 0), BlackElephant)
                .Place(Cell(7, 3), RedElephant),
        };
}
