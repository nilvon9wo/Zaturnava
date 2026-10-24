using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class BoardActionsTest
{
    private static readonly ActionDto Unreadable =
        Actions.Of("move", (ActionArguments.From, "here"), (ActionArguments.To, "16"));

    private static readonly ActionDto Far = Actions.Move(8, 24);

    private static readonly ActionDto OneEnded = Actions.Of("drop", (ActionArguments.To, "16"));

    private readonly BoardActions _actions = new(
        [Actions.Pass, Actions.Move(8, 16), Far, Actions.Move(9, 17), Unreadable, OneEnded]
    );

    [Fact]
    public void OffBoard_WhenAnActionHasNoTwoCells_OffersItApart()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<ActionDto> offBoard = this._actions.OffBoard;

        // Assert
        Assert.Equal([Actions.Pass, Unreadable, OneEnded], offBoard);
    }

    [Fact]
    public void Origins_WhenMovesStartFromCells_AreThoseCells()
    {
        // Arrange - nothing beyond the actions

        // Act
        IEnumerable<int> origins = this._actions.Origins.Order();

        // Assert
        Assert.Equal([8, 9], origins);
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
}
