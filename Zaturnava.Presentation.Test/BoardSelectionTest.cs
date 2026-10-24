using Net.NowhereAtAll.Zaturnava.Contracts;

namespace Net.NowhereAtAll.Zaturnava.Presentation.Test;

public sealed class BoardSelectionTest
{
    private static readonly ActionDto Advance = Actions.Move(8, 16);

    private readonly BoardActions _actions = new([Actions.Pass, Advance, Actions.Move(9, 17)]);

    [Fact]
    public void Click_WhenNothingIsSelectedAndAPieceCanMove_SelectsIt()
    {
        // Arrange - nothing beyond the actions

        // Act
        (BoardSelection next, ActionDto? chosen) = BoardSelection.None.Click(8, this._actions);

        // Assert
        Assert.Equal((new BoardSelection(8), null), (next, chosen));
    }

    [Fact]
    public void Click_WhenNothingIsSelectedAndNothingCanMoveFromTheCell_SelectsNothing()
    {
        // Arrange - nothing beyond the actions

        // Act
        (BoardSelection next, ActionDto? chosen) = BoardSelection.None.Click(30, this._actions);

        // Assert
        Assert.Equal((BoardSelection.None, null), (next, chosen));
    }

    [Fact]
    public void Click_WhenTheSelectedPieceCanGoThere_ChoosesThatMove()
    {
        // Arrange
        BoardSelection selected = new(8);

        // Act
        (BoardSelection next, ActionDto? chosen) = selected.Click(16, this._actions);

        // Assert
        Assert.Equal((BoardSelection.None, Advance), (next, chosen));
    }

    [Fact]
    public void Click_WhenAnotherPieceThatCanMoveIsClicked_SelectsItInstead()
    {
        // Arrange
        BoardSelection selected = new(8);

        // Act
        (BoardSelection next, ActionDto? chosen) = selected.Click(9, this._actions);

        // Assert
        Assert.Equal((new BoardSelection(9), null), (next, chosen));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(40)]
    public void Click_WhenTheSelectedPieceCannotGoThere_PutsItBack(int cell)
    {
        // Arrange
        BoardSelection selected = new(8);

        // Act
        (BoardSelection next, ActionDto? chosen) = selected.Click(cell, this._actions);

        // Assert
        Assert.Equal((BoardSelection.None, null), (next, chosen));
    }
}
