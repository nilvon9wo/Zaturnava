using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Pieces;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class ActionParserTest
{
    [Fact]
    public void Parse_WhenGivenADescribedMove_ReturnsTheSameMove()
    {
        // Arrange
        MoveAction move = new(new SeatId(1), new CellId(8), new CellId(16));

        // Act
        GameAction parsed = ActionParser.Standard.Parse(move.Describe());

        // Assert
        Assert.Equal(move, parsed);
    }

    [Fact]
    public void Parse_WhenGivenADescribedPass_ReturnsTheSamePass()
    {
        // Arrange
        PassAction pass = new(new SeatId(3));

        // Act
        GameAction parsed = ActionParser.Standard.Parse(pass.Describe());

        // Assert
        Assert.Equal(pass, parsed);
    }

    [Fact]
    public void Parse_WhenGivenADescribedRoll_ReturnsTheSameRoll()
    {
        // Arrange
        RollAction roll = new(new SeatId(2));

        // Act
        GameAction parsed = ActionParser.Standard.Parse(roll.Describe());

        // Assert
        Assert.Equal(roll, parsed);
    }

    [Fact]
    public void Parse_WhenGivenDescribedDice_ReturnsTheSameDice()
    {
        // Arrange
        DiceAction dice = new(new SeatId(2), [5, 3]);

        // Act
        GameAction parsed = ActionParser.Standard.Parse(dice.Describe());

        // Assert
        Assert.Equal(dice, parsed);
    }

    public static TheoryData<GameAction> RansomAndPromotion =>
    [
        new PromoteAction(new SeatId(1), new CellId(7), new PieceId(12)),
        new PromoteOwnAction(new SeatId(1), new CellId(7), new PieceKindId("queen")),
        new OfferAction(new SeatId(1), new SeatId(2), [new PieceId(16), new PieceId(17)], [new PieceId(4)]),
        new OfferAction(new SeatId(1), new SeatId(2), [new PieceId(16)], []),
        new AcceptAction(new SeatId(2)),
        new DeclineAction(new SeatId(2)),
        new PlaceAction(new SeatId(2), new CellId(9)),
        new EscapeAction(new SeatId(3), new PieceId(11), new CellId(32)),
        new FleeAction(new SeatId(3), new CellId(63)),
        new HireAction(new SeatId(2), new PieceId(9)),
        new DeployAction(new SeatId(2), new PieceId(9), new CellId(10)),
        new HandOverAction(new SeatId(1), new SeatId(3)),
        new ProposeEndAction(new SeatId(2), Resigns: true),
        new AnswerEndAction(new SeatId(0), Agrees: false),
        new RejoinAction(new SeatId(3)),
        new VoteAwayAction(new SeatId(1), new SeatId(2), Stops: true),
        new PassAwayAction(new SeatId(2)),
        new RetireAction(new SeatId(1)),
        new FaceOffAction(new SeatId(2), new CellId(3), new CellId(43)),
        new PalaceAction(new SeatId(1), new CellId(9)),
        new ThroneAction(new SeatId(1), new CellId(2)),
        new DraftAction(new SeatId(1), new PieceKindId("rook"), new CellId(3)),
        new UndraftAction(new SeatId(2), new CellId(4)),
        new FillArmyAction(new SeatId(3)),
    ];

    [Theory]
    [MemberData(nameof(RansomAndPromotion), DisableDiscoveryEnumeration = true)]
    public void Parse_WhenGivenADescribedPromotionOrRansomAction_ReturnsTheSameAction(GameAction action)
    {
        // Arrange - nothing beyond the action

        // Act
        GameAction parsed = ActionParser.Standard.Parse(action.Describe());

        // Assert
        Assert.Equal(action, parsed);
    }

    [Fact]
    public void Parse_WhenAnOwnPromotionNamesNoKind_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new(
            PromoteOwnAction.Kind,
            ImmutableSortedDictionary<string, string>.Empty.Add("actor", "1").Add("cell", "7")
        );

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => ActionParser.Standard.Parse(descriptor)
        );

        // Assert
        Assert.Equal("The 'promote-own' action needs a kind for 'becomes'.", exception.Message);
    }

    [Fact]
    public void Parse_WhenADraftNamesNoPiece_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new(
            DraftAction.Kind,
            ImmutableSortedDictionary<string, string>.Empty.Add("actor", "1").Add("home", "7")
        );

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => ActionParser.Standard.Parse(descriptor)
        );

        // Assert
        Assert.Equal("The 'draft' action needs a kind for 'piece'.", exception.Message);
    }

    [Fact]
    public void Parse_WhenTheKindIsUnknown_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new("castle", ImmutableSortedDictionary<string, string>.Empty);

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => ActionParser.Standard.Parse(descriptor)
        );

        // Assert
        Assert.Equal("There is no 'castle' action.", exception.Message);
    }
}
