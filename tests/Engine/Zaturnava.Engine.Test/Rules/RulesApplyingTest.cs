using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Moves;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Positions;
using Net.NowhereAtAll.Zaturnava.Engine.Rules;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Rules;

public sealed class RulesApplyingTest
{
    private static readonly SeatId Seat = new(0);

    [Fact]
    public void Game_WhenTheRulesetSaysNoOther_IsItsOwnAtNoSizeAndNoPreview()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        (string Id, string Name, string? Size, bool IsPreview) game = GameOf(rules);

        // Assert
        Assert.Equal(("solo", "Solo", null, false), game);
    }

    [Fact]
    public void MarkOf_WhenTheRulesetSaysNoOther_IsNone()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        PieceMark? mark = MarkOf(rules, new PieceKindId("pawn"));

        // Assert
        Assert.Null(mark);
    }

    [Fact]
    public void ClearLead_WhenTheRulesetSaysNoOther_IsChaturajis()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        int clear = ClearLeadOf(rules);

        // Assert
        Assert.Equal(10, clear);
    }

    [Fact]
    public void GuideOf_WhenTheRulesetSaysNoOther_TellsNothingButTheKind()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        PieceGuide guide = GuideOf(rules, new PieceKindId("pawn"));

        // Assert
        Assert.Equal(
            ("pawn", 0, (string?)null, 0, 0),
            (guide.Kind.Id.Value, guide.Kind.Movement.Count, guide.Class, guide.Powers.Count, guide.Restrictions.Count)
        );
    }

    [Fact]
    public void MovesPerTurn_WhenTheRulesetSaysNoOther_IsOne()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        double moves = MovesPerTurnOf(rules, new PieceKindId("pawn"));

        // Assert
        Assert.Equal(1, moves);
    }

    [Fact]
    public void LinesOfAndTerrainOf_WhenTheRulesetSaysNoOther_AreNone()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        (int Lines, string? Terrain) board = BoardOf(rules, OneSizeRules.State);

        // Assert
        Assert.Equal((0, null), board);
    }

    [Fact]
    public void Deal_WhenTheRulesetSaysNoOther_DealsNothingAndLeavesTheGameAsItIs()
    {
        // Arrange
        OneSizeRules rules = new();
        GameState initial = OneSizeRules.State;

        // Act
        (bool IsDealt, GameState Dealt) dealt = DealOf(rules, initial);

        // Assert
        Assert.Equal((false, initial), dealt);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(true, 2)]
    public void ThronesOf_WhenTheGameWasOrWasNotDealtItsOwn_IsItsOwnElseTheRulesets(bool isDealt, int expected)
    {
        // Arrange - the ruleset's throne is cell 0; the game's own, if dealt, cell 2
        OneSizeRules rules = new();
        GameState state = isDealt
            ? OneSizeRules.State with { Thrones = ImmutableDictionary<SeatId, CellId>.Empty.Add(Seat, new CellId(2)) }
            : OneSizeRules.State;

        // Act
        ImmutableDictionary<SeatId, CellId> thrones = ThronesOf(rules, state);

        // Assert
        Assert.Equal(expected, thrones[Seat].Value);
    }

    [Fact]
    public void ChancesOf_WhenTheRulesetSaysNoOther_IsNone()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        ImmutableList<GameAction> chances = ChancesOf(rules, OneSizeRules.State);

        // Assert
        Assert.Empty(chances);
    }

    [Fact]
    public void KeepingWorthOf_WhenTheRulesetSaysNoOther_IsWhatThePieceIsWorth()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        int worth = KeepingWorthOf(rules, new PieceKindId("pawn"));

        // Assert
        Assert.Equal(4, worth);
    }

    [Fact]
    public void Drafted_WhenTheRulesetSaysNoOther_IsNotDraftableAndLeavesTheGameAsDealtOfferingNothing()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        (bool IsDraftable, int? Budget, GameState Drafted, int Offered) drafted = DraftedOf(rules, OneSizeRules.State);

        // Assert
        Assert.Equal((false, (int?)null, OneSizeRules.State, 0), drafted);
    }

    [Fact]
    public void Sized_WhenTheRulesetSaysNoOther_MayNotBeSetToAnotherSize()
    {
        // Arrange
        OneSizeRules rules = new();

        // Act
        (bool HasBoard, bool IsSized) sized = SizedOf(rules, new BoardSize(10, 4, 3));

        // Assert
        Assert.Equal((false, false), sized);
    }

    private static (bool HasBoard, bool IsSized) SizedOf<TRules>(TRules rules, BoardSize board)
        where TRules : IRulesApplying =>
        (rules.SizableBoard is not null, rules.Sized(board) is not null);

    private static (bool IsDraftable, int? Budget, GameState Drafted, int Offered) DraftedOf<TRules>(
        TRules rules,
        GameState dealt
    )
        where TRules : IRulesApplying =>
        (rules.IsDraftable, rules.DraftBudget, rules.Drafted(dealt, 7), rules.DraftOffered(dealt).Count);

    private static int KeepingWorthOf<TRules>(TRules rules, PieceKindId kind)
        where TRules : IRulesApplying =>
        rules.KeepingWorthOf(kind);

    private static ImmutableList<GameAction> ChancesOf<TRules>(TRules rules, GameState state)
        where TRules : IRulesApplying =>
        rules.ChancesOf(state, Seat);

    private static ImmutableDictionary<SeatId, CellId> ThronesOf<TRules>(TRules rules, GameState state)
        where TRules : IRulesApplying =>
        rules.ThronesOf(state);

    private static (bool IsDealt, GameState Dealt) DealOf<TRules>(TRules rules, GameState initial)
        where TRules : IRulesApplying =>
        (rules.IsDealt, rules.Deal(initial, 7));

    private static PieceMark? MarkOf<TRules>(TRules rules, PieceKindId kind)
        where TRules : IRulesApplying =>
        rules.MarkOf(kind);

    private static int ClearLeadOf<TRules>(TRules rules)
        where TRules : IRulesApplying =>
        rules.ClearLead;

    private static PieceGuide GuideOf<TRules>(TRules rules, PieceKindId kind)
        where TRules : IRulesApplying =>
        rules.GuideOf(kind);

    private static (int Lines, string? Terrain) BoardOf<TRules>(TRules rules, GameState state)
        where TRules : IRulesApplying =>
        (rules.LinesOf(state).Count, rules.TerrainOf(state));

    private static double MovesPerTurnOf<TRules>(TRules rules, PieceKindId kind)
        where TRules : IRulesApplying =>
        rules.MovesPerTurn(kind);

    /// <summary>The game any rules play, as the interface tells it.</summary>
    private static (string Id, string Name, string? Size, bool IsPreview) GameOf<TRules>(TRules rules)
        where TRules : IRulesApplying =>
        (rules.GameId, rules.GameName, rules.Size, rules.IsPreview);
}

/// <summary>Rules of a game played at one size, by one seat with its throne on cell 0, asked little else.</summary>
file sealed class OneSizeRules : IRulesApplying
{
    /// <summary>A game of these rules, its one seat to decide.</summary>
    public static GameState State { get; } = new(
        Position.Empty,
        [new Seat(new SeatId(0), new Direction(0, 1))],
        TeamGraph.FromTeams([[new SeatId(0)]]),
        new SeatId(0),
        1,
        [],
        []
    );

    public string Id => "solo";

    public string Name => "Solo";

    public ICellNavigating Topology => throw new NotSupportedException();

    public ImmutableDictionary<SeatId, CellId> Thrones { get; } =
        ImmutableDictionary<SeatId, CellId>.Empty.Add(new SeatId(0), new CellId(0));

    public GameState CreateInitialState() => throw new NotSupportedException();

    public ImmutableList<GameAction> LegalActions([Required] GameState state) => throw new NotSupportedException();

    public Transition Apply([Required] GameState state, [Required] GameAction action) =>
        throw new NotSupportedException();

    public bool MayOffer([Required] GameState state) => throw new NotSupportedException();

    public bool MayGoBack([Required] GameState state, [Required] Prisoner prisoner) =>
        throw new NotSupportedException();

    public bool MayProposeEnd([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayRejoin([Required] GameState state, SeatId seat) => throw new NotSupportedException();

    public bool MayVoteAway([Required] GameState state, SeatId voter, SeatId absent) =>
        throw new NotSupportedException();

    public DecisionTimer TimerOf([Required] GameState state) => throw new NotSupportedException();

    public GameAction Parse([Required] ActionDescriptor descriptor) => throw new NotSupportedException();

    public string NameOf(PieceKindId kind) => throw new NotSupportedException();

    public string NameOf(SeatId seat) => throw new NotSupportedException();

    /// <summary>Every piece is worth 4.</summary>
    public int WorthOf(PieceKindId kind) => 4;

    public int RansomPointsOf([Required] GameState state, [Required] RansomOffer offer) =>
        throw new NotSupportedException();

    public ImmutableList<Attack> AttacksOf([Required] GameState state, SeatId seat) =>
        throw new NotSupportedException();

    public bool IsLeader(PieceKindId kind) => throw new NotSupportedException();

    public bool IsHeavy(PieceKindId kind) => throw new NotSupportedException();

    public string NameOfDie(int value) => throw new NotSupportedException();

    public bool MayUseDie([Required] GameState state, int value) => throw new NotSupportedException();
}
