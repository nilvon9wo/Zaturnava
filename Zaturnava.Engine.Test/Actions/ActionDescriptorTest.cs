using System.Collections.Immutable;
using Net.NowhereAtAll.Zaturnava.Engine.Actions;
using Net.NowhereAtAll.Zaturnava.Engine.Boards;
using Net.NowhereAtAll.Zaturnava.Engine.Seats;

namespace Net.NowhereAtAll.Zaturnava.Engine.Test.Actions;

public sealed class ActionDescriptorTest
{
    private readonly ActionDescriptor _descriptor = new(
        "test",
        ImmutableSortedDictionary<string, string>.Empty
            .Add(ActionDescriptor.ActorArgument, "2")
            .Add("cell", "17")
            .Add("word", "seventeen")
    );

    [Fact]
    public void Actor_WhenPresent_ReturnsTheSeat()
    {
        // Arrange - nothing beyond the descriptor

        // Act
        SeatId actor = this._descriptor.Actor();

        // Assert
        Assert.Equal(new SeatId(2), actor);
    }

    [Fact]
    public void CellArgument_WhenPresent_ReturnsTheCell()
    {
        // Arrange - nothing beyond the descriptor

        // Act
        CellId cell = this._descriptor.CellArgument("cell");

        // Assert
        Assert.Equal(new CellId(17), cell);
    }

    [Fact]
    public void BooleanArgument_WhenNeitherTrueNorFalse_ThrowsMalformedActionException()
    {
        // Arrange
        ActionDescriptor descriptor = new(
            "answer-end",
            ImmutableSortedDictionary<string, string>.Empty.Add("agrees", "yes")
        );

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => descriptor.BooleanArgument("agrees")
        );

        // Assert
        Assert.Equal("The 'answer-end' action needs 'agrees' to be true or false.", exception.Message);
    }

    [Fact]
    public void IntegerArgument_WhenMissing_ThrowsMalformedActionException()
    {
        // Arrange - nothing beyond the descriptor

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => this._descriptor.IntegerArgument("missing")
        );

        // Assert
        Assert.Equal("The 'test' action needs an integer 'missing'.", exception.Message);
    }

    [Fact]
    public void IntegerArgument_WhenNotAnInteger_ThrowsMalformedActionException()
    {
        // Arrange - nothing beyond the descriptor

        // Act
        MalformedActionException exception = Assert.Throws<MalformedActionException>(
            () => this._descriptor.IntegerArgument("word")
        );

        // Assert
        Assert.Equal("The 'test' action needs an integer 'word'.", exception.Message);
    }
}
