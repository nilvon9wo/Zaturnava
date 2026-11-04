using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class BoardActionsTest
{
    private static readonly ActionDto Unreadable =
        Actions.Of("move", (ActionArguments.From, "here"), (ActionArguments.To, "16"));

    private static readonly ActionDto Far = Actions.Move(8, 24);

    private static readonly ActionDto OneEnded = Actions.Of("drop", (ActionArguments.To, "16"));

    private static readonly ActionDto Promote = Actions.Of("promote", (ActionArguments.Cell, "40"));

    private readonly BoardActions _actions = new(
        [Actions.Pass, Actions.Move(8, 16), Far, Actions.Move(9, 17), Unreadable, OneEnded, Promote]
    );

    [Fact]
    public void OffBoard_WhenAnActionHasNoTwoCells_OffersItApart()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<ActionDto> offBoard = this._actions.OffBoard;

        // Assert
        Assert.Equal([Actions.Pass, Unreadable], offBoard);
    }

    [Fact]
    public void Choices_WhenActionsAreChosenByAButtonThenACell_OffersEachOnceInOrderAndNotApart()
    {
        // Arrange - the boat to 3 or 4, the horse to 5; and one with a choice but no cell, offered apart
        ActionDto boatTo3 = Actions.Escape("Escape the Boat", "3");
        ActionDto boatTo4 = Actions.Escape("Escape the Boat", "4");
        ActionDto horseTo5 = Actions.Escape("Escape the Horse", "5");
        ActionDto nowhere = Actions.Pass with { Choice = "Pass" };
        BoardActions actions = new([boatTo3, horseTo5, boatTo4, nowhere]);

        // Act
        string choices = string.Join('|', actions.Choices);

        // Assert
        Assert.Equal(
            ("Escape the Boat|Escape the Horse", nowhere, "3,4", boatTo4, null),
            (
                choices,
                Assert.Single(actions.OffBoard),
                string.Join(',', actions.PlacesOf("Escape the Boat").Order()),
                actions.PlacementAt("Escape the Boat", 4),
                actions.PlacementAt("Escape the Horse", 4)
            )
        );
    }

    [Fact]
    public void Origins_WhenMovesStartFromCells_AreThoseCells()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<int> origins = this._actions.Origins.Order();

        // Assert
        Assert.Equal([8, 9, 40], origins);
    }

    [Fact]
    public void AtCells_WhenAnActionIsTakenAtACell_AreThoseCells()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<int> cells = this._actions.AtCells;

        // Assert
        Assert.Equal([40], cells);
    }

    [Fact]
    public void At_WhenAnActionIsTakenAtTheCell_ReturnsIt()
    {
        // Arrange - nothing beyond the actions

        // Act
        (ActionDto At, int Elsewhere) actions = (this._actions.At(40).Single(), this._actions.At(8).Count);

        // Assert
        Assert.Equal((Promote, 0), actions);
    }

    [Fact]
    public void TargetsFrom_WhenGivenAnOrigin_AreWhereItsMovesGo()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<int> targets = this._actions.TargetsFrom(8).Order();

        // Assert
        Assert.Equal([16, 24], targets);
    }

    [Fact]
    public void Between_WhenAMoveJoinsTheCells_ReturnsIt()
    {
        // Arrange - nothing beyond the actions

        // Act
        ActionDto? move = this._actions.Between(8, 24);

        // Assert
        Assert.Same(Far, move);
    }

    [Fact]
    public void Between_WhenNoMoveJoinsTheCells_ReturnsNull()
    {
        // Arrange - nothing beyond the actions

        // Act
        ActionDto? move = this._actions.Between(9, 24);

        // Assert
        Assert.Null(move);
    }

    [Theory]
    [InlineData(8, 16, "Swap places with Green's Pawn")]
    [InlineData(8, 24, null)]
    [InlineData(9, 24, null)]
    public void LabelBetween_WhenGivenTwoCells_SaysWhatTheMoveJoiningThemSays(int from, int to, string? expected)
    {
        // Arrange
        BoardActions actions = new([Actions.Move(8, 16) with { Label = "Swap places with Green's Pawn" }, Far]);

        // Act
        string? label = actions.LabelBetween(from, to);

        // Assert
        Assert.Equal(expected, label);
    }

    [Fact]
    public void Drops_WhenAnActionGoesToACellFromNone_AreThoseCells()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<int> drops = this._actions.Drops;

        // Assert
        Assert.Equal([16], drops);
    }

    [Theory]
    [InlineData(16, true)]
    [InlineData(17, false)]
    public void DropAt_WhenGivenACell_ReturnsTheActionDroppingThere(int cell, bool isDrop)
    {
        // Arrange - nothing beyond the actions

        // Act
        ActionDto? drop = this._actions.DropAt(cell);

        // Assert
        Assert.Equal(isDrop ? OneEnded : null, drop);
    }
}
